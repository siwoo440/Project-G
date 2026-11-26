using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectG
{
    /// <summary>Connects authored world evidence to the pure attempt rules and the existing atomic save.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class FlightStoryDirector : MonoBehaviour
    {
        private sealed class Actor
        {
            internal FlightStoryCombatant Unit;
            internal FlightStoryWave Wave;
            internal FlightStoryWavePlacement Placement;
            internal double SpawnedAt, OutsideSince = double.NaN;
            internal bool Defeated, Resolved, RetreatStarted;
        }

        public static FlightStoryDirector Active { get; private set; }
        public FlightStoryAttempt Attempt { get; private set; }
        public FlightStoryManifest Manifest { get; private set; }
        public FlightStoryStability Stability { get; private set; }
        public FlightStoryStability Backup { get; private set; }
        public FlightStoryCatalog Catalog { get; private set; }
        public FlightStoryStage Stage { get; private set; }
        public FlightStoryPlayMode PlayMode => playMode;
        public string Error { get; private set; }
        public bool Ready { get; private set; }
        public double DamageTime => Math.Max(0d, Time.timeAsDouble - damageEpoch);
        public bool Finished { get; private set; }
        public bool Failed => failed;
        public bool CombatSettled => receipt != null;
        public bool NeedsReceipt => RewardEligibleMode && Attempt != null && Attempt.CombatStarted && receipt == null;
        public bool FinalBossActive => actors.Any(x => x.Unit != null && x.Unit.CanThreaten && FlightStoryCombatProfiles.FindEnemy(x.Unit.ProfileId).RewardClass == "FINAL");
        public bool MidBossActive => actors.Any(x => x.Unit != null && x.Unit.CanThreaten && FlightStoryCombatProfiles.FindEnemy(x.Unit.ProfileId).RewardClass == "MID");
        public int ActiveCount => actors.Count(x => x.Unit != null && !x.Defeated && !x.Unit.HasExited);
        public int WaveCount { get; private set; }
        public event Action<double> ObservationTick;
        public event Action<string> SceneEntered;

        private FlightStoryStageBinding binding;
        private FlightStoryStageBinding authoredTemplate;
        private bool usedBinding;
        private FlightEnemyEncounter encounter;
        private FlightPilotVitals pilot;
        private FlightThreatPool pool;
        private FlightWeapon weapon;
        private FlightStoryPlayMode playMode;
        private readonly Dictionary<string, FlightStoryProtectedTarget> protections = new Dictionary<string, FlightStoryProtectedTarget>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> spawned = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly List<Actor> actors = new List<Actor>();
        private readonly HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> savedScenes = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> cancelledWaves = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> attemptScenes = new List<string>();
        private FlightProgressReceipt receipt;
        private FlightStoryCheckpoint pendingPatch;
        private string pendingAdvanceScene;
        private bool pendingSettlement, advanceRequested, stopping, failed;
        private bool RewardEligibleMode => playMode == FlightStoryPlayMode.Story || playMode == FlightStoryPlayMode.StoryReplay;
        private double sceneStartedAt, remainder;
        private double damageEpoch;
        private bool savePaused;
        private float beforeSaveScale;
        private bool recoveryPending;
        private float nextRecoveryAt;

        internal bool Initialize(FlightStoryCatalog catalog, FlightStoryStageBinding sceneBinding, FlightEnemyEncounter owner, FlightPilotVitals pilotVitals, FlightThreatPool threatPool, FlightStoryPlayMode mode, FlightStoryStageBinding sourceTemplate = null)
        {
            Catalog = catalog;
            binding = sceneBinding;
            authoredTemplate = sourceTemplate;
            encounter = owner;
            pilot = pilotVitals;
            pool = threatPool;
            playMode = mode;
            weapon = pilot != null ? pilot.GetComponent<FlightWeapon>() : null;
            Error = null;
            string[] problems = catalog?.Validate() ?? new[] { "The approved story catalog is unavailable." };
            if (problems.Length == 0 && binding != null) problems = binding.Validate(catalog);
            if (binding == null || owner == null || pilot == null || pool == null || weapon == null || problems.Length != 0)
            {
                Error = problems.Length != 0 ? string.Join("\n", problems) : "The story scene is missing required runtime references.";
                Debug.LogError(Error, this);
                return false;
            }
            Stage = catalog.Stage(binding.stageId);
            Active = this;
            if (!RecoverPrevious()) { recoveryPending = true; return true; }
            RestartAttempt();
            return true;
        }

        private void RestartAttempt()
        {
            ReleaseSavePause();
            ClearActors();
            if (usedBinding)
            {
                if (authoredTemplate == null) { Ready = false; Error = "재시작할 원본 장면 연결을 확인하지 못했습니다."; return; }
                binding.gameObject.SetActive(false);
                Destroy(binding.gameObject);
                binding = Instantiate(authoredTemplate);
            }
            usedBinding = true;
            protections.Clear(); spawned.Clear(); savedScenes.Clear(); attemptScenes.Clear(); seen.Clear(); cancelledWaves.Clear();
            receipt = null; pendingPatch = null; pendingAdvanceScene = null;
            pendingSettlement = false; Finished = false; stopping = false; failed = false; advanceRequested = false;
            WaveCount = 0; remainder = 0;
            damageEpoch = Time.timeAsDouble;
            string[] ordered = FlightStoryRunPlan.RequiredScenes(Stage);
            FlightStoryProgressData progress = FlightProgress.Current.StoryProgress;
            // C8 is always rebuilt at S01. Other groups may resume only from the durable safe checkpoint.
            if (Stage.id != "ST-8-OP" && playMode != FlightStoryPlayMode.StoryReplay)
            {
                foreach (string missionId in Stage.missionIds)
                {
                    FlightStoryCheckpointEntry checkpoint = progress.Checkpoints.FirstOrDefault(x => x.MissionId == missionId);
                    int index = checkpoint == null ? -1 : Array.IndexOf(ordered, checkpoint.NextSceneId);
                    if (index >= 0) { ordered = ordered.Skip(index).ToArray(); break; }
                }
            }
            string runId = Guid.NewGuid().ToString("N");
            Attempt = new FlightStoryAttempt(runId, Stage.id, ordered, playMode);
            foreach (FlightStoryProtectionBinding p in binding.protections)
                protections.Add(p.id, new FlightStoryProtectedTarget(runId, p.id, p.maximum));
            if (binding.expectedRoster.Length != 0)
            {
                Manifest = new FlightStoryManifest(runId, binding.expectedRoster);
                if (binding.rosterReviewed) Manifest.Seal();
            }
            else Manifest = null;
            if (Stage.id == "ST-8-OP")
            {
                Stability = new FlightStoryStability(runId, binding.requiredSites);
                Backup = new FlightStoryStability(runId, binding.backupSites);
                Stability.BeginPhase(new FlightStoryPhase(runId, "maintenance", 0, "main_route"), 0);
                Backup.BeginPhase(new FlightStoryPhase(runId, "backup", 0, "backup_route"), 0);
            }
            else { Stability = null; Backup = null; }
            foreach (string sceneId in ordered)
            {
                FlightStorySceneBinding source = binding.Scene(sceneId);
                var facts = new List<string>(Catalog.Objective(sceneId).RequiredEvidence);
                facts.AddRange(source.readinessFacts);
                if (SceneHasThreatGate(sceneId)) facts.Add("__threats_safe");
                if (Stage.id == "ST-8-OP" && sceneId == "C08-S07") facts.AddRange(FlightStoryAttempt.FinalFacts);
                Attempt.DefineScene(sceneId, new FlightStorySceneRequirements
                {
                    Facts = facts.Distinct().ToArray(),
                    ProtectedTargets = Stage.id == "ST-8-OP" ? protections.Keys.ToArray() : source.protectedTargetIds,
                    AllArrivals = source.allArrivals || sceneId == "C08-S06" || sceneId == "C08-S07",
                    SustainedStability = source.sustainedStability || sceneId == "C08-S05" || sceneId == "C08-S06" || sceneId == "C08-S07",
                    BackupStability = sceneId == "C08-S04"
                });
            }
            foreach (FlightStoryWave w in Catalog.waves.Where(w => w.stageId == Stage.id && ordered.Contains(w.sceneId)))
            {
                Attempt.RegisterWave(w.sceneId, w.id, w.SlotCount);
                spawned.Add(w.id, 0);
            }
            foreach (FlightStorySceneBinding source in binding.scenes)
                foreach (FlightStoryEvidenceSignal signal in source.evidence) signal?.Bind(this);
            foreach (FlightStoryProtectionBinding p in binding.protections)
                if (!p.body.Bind(this)) { Error = "보호 대상의 실제 판정 연결을 확인하지 못했습니다."; Ready = false; return; }
            foreach (FlightStoryArrivalGate gate in binding.arrivalGates) gate?.Bind(this);
            foreach (FlightStoryRosterMember member in binding.GetComponentsInChildren<FlightStoryRosterMember>(true)) member.Bind(this);
            Ready = true;
            EnterScene();
        }

        public FlightStoryProtectedTarget Protection(string id) => id != null && protections.TryGetValue(id, out FlightStoryProtectedTarget value) ? value : null;
        public bool IsProtectionActive(string id) => Attempt != null && binding != null && binding.protections.Any(p => p.id == id && (Stage.id == "ST-8-OP" || p.sceneIds.Contains(Attempt.CurrentSceneId)));

        public bool ReportEvidence(string runId, string sceneId, string factId, bool present)
        {
            if (!Ready || stopping || pendingPatch != null || factId == null || factId.StartsWith("__", StringComparison.Ordinal) || factId == "backup_verified" || factId == "maintenance_handoff") return false;
            return Attempt.SetFact(runId, sceneId, factId, present);
        }

        public bool ReportDeparture(string runId, string identity) => Ready && !stopping && Manifest != null && runId == Attempt.RunId && Manifest.ConfirmDeparture(runId, identity);
        public bool ReportArrival(string runId, string identity, string destination) => Ready && !stopping && Manifest != null && runId == Attempt.RunId && Manifest.ConfirmArrival(runId, identity, destination);
        public bool ReportSite(FlightStorySiteReport report, bool backup = false) => CanObserve(backup) && (backup ? Backup : Stability)?.Report(report, Attempt.Now) == true;
        public bool ObserveSite(FlightStorySiteSample sample, bool backup = false) => CanObserve(backup) && Math.Abs(sample.At - Attempt.Now) < 0.000001 && (backup ? Backup : Stability)?.Sample(sample) == true;
        public bool BeginHandoff(FlightStoryPhase capturedPhase, string site) => CanObserve(false) && Stability != null && capturedPhase.Equals(Stability.CurrentPhase) && Stability.BeginHandoff(site, Attempt.Now);
        public bool CompleteHandoff(FlightStoryPhase capturedPhase, string site) => CanObserve(false) && Stability != null && capturedPhase.Equals(Stability.CurrentPhase) && Stability.CompleteHandoff(site, Attempt.Now);

        private bool CanObserve(bool backup)
        {
            if (!Ready || stopping || pendingPatch != null || Stage.id != "ST-8-OP") return false;
            string scene = Attempt.CurrentSceneId;
            return scene == "C08-S05" || scene == "C08-S06" || scene == "C08-S07" || backup && scene == "C08-S04";
        }

        public bool MarkDialogueSeen(string sceneId)
        {
            if (!Ready || sceneId != Attempt.CurrentSceneId) return false;
            seen.Add(sceneId);
            // A read/skip flag changes no physical evidence or world state.
            return FlightProgress.Current.SaveStoryCheckpoint(new FlightStoryCheckpoint { MissionId = Catalog.Scene(sceneId).missionId, SeenScenes = new[] { sceneId }, RecollectionOnly = true });
        }

        public void RequestAdvance() { if (Ready && !Finished && !stopping) advanceRequested = true; }

        private void LateUpdate()
        {
            if (recoveryPending)
            {
                if (Time.unscaledTime >= nextRecoveryAt)
                {
                    nextRecoveryAt = Time.unscaledTime + 1f;
                    if (RecoverPrevious()) { recoveryPending = false; RestartAttempt(); }
                }
                return;
            }
            if (!Ready || Finished || stopping) return;
            if (pendingPatch != null) { CommitPending(); return; }
            if (Time.timeScale <= 0f) return;
            // All projectile/contact Updates have run before this gate. Damage wins a same-frame tie.
            if (pilot.IsDefeated || protections.Values.Any(x => x.IsFailed))
            { Fail(FlightStoryFailureOrigin.Normal); return; }
            remainder += Time.deltaTime;
            if (remainder >= 0.25d)
            {
                double ticks = Math.Floor(remainder / 0.25d);
                remainder -= ticks * 0.25d;
                Attempt.AdvanceTime(ticks * 0.25d);
                // A long frame samples once; it never invents missing intermediate observations.
                ObservationTick?.Invoke(Attempt.Now);
            }
            SpawnDueWaves();
            UpdateActors();
            string scene = Attempt.CurrentSceneId;
            if (SceneHasThreatGate(scene)) Attempt.SetFact(Attempt.RunId, scene, "__threats_safe", ThreatsSafe(scene));
            if (scene == "C08-S07")
            {
                Attempt.SetFact(Attempt.RunId, scene, "backup_verified", Backup != null && Backup.IsBackupQualified(Attempt.Now));
                Attempt.SetFact(Attempt.RunId, scene, "maintenance_handoff", Stability != null && Stability.IsSustainedQualified(Attempt.Now));
            }
            if (!advanceRequested) return;
            advanceRequested = false;
            if ((binding.Scene(scene).backupStability || scene == "C08-S04" || scene == "C08-S07") && (Backup == null || Backup.RunId != Attempt.RunId || !Backup.IsBackupQualified(Attempt.Now))) return;
            FlightStoryStability check = scene == "C08-S04" ? Backup : Stability;
            if (!Attempt.TryAdvance(Manifest, check, protections.Values)) return;
            attemptScenes.Add(scene);
            pendingAdvanceScene = scene;
            bool boundary = scene == Stage.settlementScene;
            if (Stage.id == "ST-8-OP" && !boundary) { EnterScene(); return; }
            bool safeWrite = !Attempt.CombatStarted || receipt != null || boundary;
            if (!safeWrite) { EnterScene(); return; }
            pendingPatch = BuildPatch(scene, boundary);
            pendingSettlement = boundary && Attempt.CombatStarted && receipt == null && RewardEligibleMode;
            CommitPending();
        }

        private void SpawnDueWaves()
        {
            if (receipt != null || Attempt.Status == FlightStoryRunStatus.Failure) return;
            string scene = Attempt.CurrentSceneId;
            foreach (FlightStoryWave wave in Catalog.waves.Where(x => x.stageId == Stage.id && x.sceneId == scene))
            {
                if (!spawned.TryGetValue(wave.id, out int next) || next >= wave.SlotCount || Attempt.Now - sceneStartedAt < wave.offset) continue;
                if (cancelledWaves.Contains(wave.id)) continue;
                if (wave.id == "W_C07_02")
                {
                    if (Manifest != null && Manifest.AllArrived) { cancelledWaves.Add(wave.id); continue; }
                    if (!actors.Where(x => x.Wave.id == "W_C07_01").Any() || actors.Any(x => x.Wave.id == "W_C07_01" && !x.Defeated && !HasSafeExit(x))) continue;
                }
                FlightStoryWavePlacement place = binding.Wave(wave.id);
                if (!Attempt.HasFact(scene, place.activationFact)) continue;
                int active = actors.Count(x => x.Unit != null && x.Unit.CanThreaten);
                if (active >= wave.maxAlive) continue;
                string profileId = Expand(wave).ElementAt(next);
                bool boss = profileId.StartsWith("B-", StringComparison.Ordinal);
                if (boss && active != 0 || !boss && actors.Any(x => x.Unit != null && x.Unit.CanThreaten && x.Unit.ProfileId.StartsWith("B-", StringComparison.Ordinal))) continue;
                Vector3 local = binding.stageAnchor.InverseTransformPoint(place.spawnPoints[next].position);
                if (binding.protections.Any(p => IsProtectionActive(p.id) && p.body != null && Vector3.Distance(place.spawnPoints[next].position, p.body.transform.TransformPoint(p.body.LocalCenter)) < 20f + p.body.Radius)) continue;
                FlightStoryCombatant unit = FlightStoryCombatant.Create(binding.enemyTemplate, profileId, pool, pilot, binding.stageAnchor, local);
                if (unit == null) continue;
                if (!Attempt.CombatStarted)
                {
                    string started = RewardEligibleMode ? FlightProgress.Current.BeginStory(Attempt.RunId, weapon.ActivePilot, Stage.id) : Attempt.RunId;
                    if (started != Attempt.RunId) { unit.StopCombat(); Destroy(unit.gameObject); Error = "참여 기록 저장을 기다리고 있습니다."; return; }
                    Attempt.BeginCombat(Attempt.RunId);
                    weapon.StartFirstTask();
                }
                if (!Attempt.TryClaimWaveSlot(Attempt.RunId, scene, wave.id, next)) { unit.StopCombat(); Destroy(unit.gameObject); continue; }
                if (next == 0) WaveCount++;
                spawned[wave.id] = next + 1;
                if (next < place.combatPoints.Length && place.combatPoints[next] != null)
                    unit.SetEntryRoute(binding.stageAnchor.InverseTransformPoint(place.combatPoints[next].position), place.entrySeconds);
                if (place.retreatTarget != null) unit.SetRetreatRoute(binding.stageAnchor.InverseTransformPoint(place.retreatTarget.position), place.retreatSeconds);
                if (profileId == "H-ROCK") unit.ConfigureRock(wave.id == "W_C02_03", next == 0 ? 0f : 8f);
                var actor = new Actor { Unit = unit, Wave = wave, Placement = place, SpawnedAt = Attempt.Now };
                actors.Add(actor);
                FlightStoryEnemyProfile profile = FlightStoryCombatProfiles.FindEnemy(profileId);
                Attempt.RegisterEncounter(unit.InstanceId, RewardClass(profile.RewardClass));
                unit.Defeated += OnDefeated;
                unit.ContactSweep += OnContact;
                if (profile.RewardClass == "NORMAL" && RewardEligibleMode) encounter.RegisterStoryRewardSource(unit.Enemy);
                else encounter.RegisterStoryCombatTarget(unit.Enemy);
                LimitAttackers();
            }
        }

        private void UpdateActors()
        {
            LimitAttackers();
            foreach (Actor actor in actors)
            {
                FlightStoryCombatant unit = actor.Unit;
                if (unit == null || actor.Defeated || actor.Resolved) continue;
                FlightStoryWavePlacement place = actor.Placement;
                bool safety = place.safetyFacts.Length > 0 && place.safetyFacts.All(f => Attempt.HasFact(place.safetySceneId, f));
                bool permission = string.IsNullOrWhiteSpace(place.retreatPermissionFact) || Attempt.HasFact(Attempt.CurrentSceneId, place.retreatPermissionFact);
                bool timedExit = place.automaticExitSeconds > 0 && Attempt.Now - actor.SpawnedAt >= place.automaticExitSeconds;
                if (!actor.RetreatStarted && permission && (timedExit || unit.IsSuppressed && unit.ProfileId != "B-KARDEN"))
                { actor.RetreatStarted = true; unit.BeginRetreat(); }
                bool karden = unit.ProfileId == "B-KARDEN";
                bool outside = HasSafeExit(actor);
                if (outside) { if (double.IsNaN(actor.OutsideSince)) actor.OutsideSince = Attempt.Now; }
                else actor.OutsideSince = double.NaN;
                bool safeExit = outside && Attempt.Now - actor.OutsideSince >= place.outsideHoldSeconds;
                bool nonlethal = FlightStoryCombatProfiles.FindEnemy(unit.ProfileId).IsNonlethal;
                if (nonlethal && safety && (karden ? unit.IsSuppressed : safeExit) && protections.Values.All(x => !x.IsFailed))
                    actor.Resolved = Attempt.TryResolveEncounter(Attempt.RunId, unit.InstanceId, true);
            }
        }

        private bool HasSafeExit(Actor actor)
        {
            FlightStoryWavePlacement place = actor.Placement;
            if (actor.Unit == null || !actor.Unit.HasExited) return false;
            float radius = actor.Unit.Profile.Radius;
            return FlightStorySpatial.ContainsSphere(place.approvedExitAirspace, actor.Unit.transform.position, radius)
                && place.forbiddenExitVolumes.All(x => !FlightStorySpatial.OverlapsSphere(x, actor.Unit.transform.position, radius));
        }

        private void LimitAttackers()
        {
            Actor boss = actors.FirstOrDefault(x => x.Unit != null && x.Unit.CanThreaten && x.Unit.ProfileId.StartsWith("B-", StringComparison.Ordinal));
            int ordinary = 0;
            foreach (Actor actor in actors)
            {
                if (actor.Unit == null || !actor.Unit.CanThreaten) continue;
                bool enabled = boss != null ? actor == boss : actor.Unit.ProfileId == "H-ROCK" || ordinary++ < 2;
                actor.Unit.SetAttackEnabled(enabled);
            }
        }

        private void OnDefeated(FlightStoryCombatant unit)
        {
            Actor actor = actors.Find(x => x.Unit == unit);
            if (!Ready || stopping || actor == null || actor.Defeated) return;
            actor.Defeated = true;
            actor.Resolved = Attempt.TryResolveEncounter(Attempt.RunId, unit.InstanceId, true);
        }

        private void OnContact(FlightStoryCombatant source, Vector3 start, Vector3 end, float radius, int damage)
        {
            if (Ready && source.CanThreaten)
                FlightStoryProtectedBody.TryIntercept(this, start, end, radius, damage, source.InstanceId + ":contact:" + Time.frameCount);
        }

        private bool SceneHasThreatGate(string scene)
        {
            return Catalog.waves.Any(w => w.stageId == Stage.id && w.sceneId == scene && scene != "C07-S05") || scene == "C07-S06";
        }

        private bool ThreatsSafe(string scene)
        {
            FlightStoryWave[] required = Catalog.waves.Where(w => w.stageId == Stage.id && w.sceneId == scene).ToArray();
            if (required.Any(w => !cancelledWaves.Contains(w.id) && (!spawned.TryGetValue(w.id, out int n) || n != w.SlotCount))) return false;
            IEnumerable<Actor> relevant = actors.Where(x => x.Wave.sceneId == scene || x.Placement.safetySceneId == scene);
            foreach (Actor actor in relevant)
            {
                if (actor.Unit == null) return false;
                if (scene == "C02-S02" && actor.Unit.ProfileId == "B-KARDEN") { if (!actor.Unit.IsSuppressed) return false; continue; }
                if (actor.Unit.ProfileId == "H-ROCK") { if (!actor.Unit.IsSuppressed && !actor.Defeated) return false; continue; }
                if (FlightStoryCombatProfiles.FindEnemy(actor.Unit.ProfileId).IsNonlethal) { if (!actor.Resolved) return false; }
                else if (!actor.Defeated && !HasSafeExit(actor)) return false;
            }
            return true;
        }

        private FlightStoryCheckpoint BuildPatch(string completedScene, bool segment)
        {
            FlightStoryProgressData old = FlightProgress.Current.StoryProgress;
            var completed = new HashSet<string>(old.SceneEvents, StringComparer.Ordinal);
            foreach (string scene in attemptScenes) completed.Add(Catalog.Objective(scene).completionEvent);
            var missions = new HashSet<string>(old.CompletedMissions, StringComparer.Ordinal);
            foreach (string id in Stage.missionIds)
                if (Catalog.Mission(id).sceneIds.Where(s => s != "E-S05").All(s => completed.Contains(Catalog.Objective(s).completionEvent))) missions.Add(id);
            string[] newMissions = missions.Where(x => !old.CompletedMissions.Contains(x)).ToArray();
            bool chapterComplete = Catalog.missions.Where(m => m.chapter == Stage.chapter).All(m => missions.Contains(m.id));
            bool done = Attempt.Status == FlightStoryRunStatus.Success;
            return new FlightStoryCheckpoint
            {
                MissionId = Catalog.Scene(completedScene).missionId,
                NextSceneId = done ? null : Attempt.CurrentSceneId,
                SeenScenes = seen.ToArray(),
                SceneEvents = attemptScenes.Select(s => Catalog.Objective(s).completionEvent).ToArray(),
                Records = attemptScenes.Select(s => Catalog.Objective(s).record).ToArray(),
                CompletedMissions = Stage.id == "ST-8-OP" && segment ? Stage.missionIds : newMissions,
                CompletedChapters = chapterComplete ? new[] { Stage.chapter } : Array.Empty<string>(),
                CompletedSegments = segment ? new[] { Stage.id } : Array.Empty<string>(),
                OperationClear = Stage.id == "ST-8-OP" && segment,
                RecollectionOnly = playMode == FlightStoryPlayMode.Recollection || playMode == FlightStoryPlayMode.Practice
            };
        }

        private void CommitPending()
        {
            if (pendingPatch == null) return;
            bool committed;
            if (pendingSettlement)
            {
                FlightBroomCatalog equipment = weapon.GetComponent<FlightBroomEquipment>()?.Catalog;
                receipt = FlightProgress.Current.CompleteStory(Attempt.RunId, true, weapon.FirstTaskReady, Attempt.NormalCount, Attempt.EliteCount, Attempt.MidCount > 0, Attempt.FinalCount > 0, equipment, pendingPatch);
                committed = receipt != null;
            }
            else committed = FlightProgress.Current.SaveStoryCheckpoint(pendingPatch);
            if (!committed)
            {
                Error = "진행 기록을 저장하지 못했습니다. 같은 결과로 다시 저장을 시도합니다.";
                PauseActors();
                if (!savePaused) { beforeSaveScale = Time.timeScale; Time.timeScale = 0f; savePaused = true; }
                return;
            }
            ReleaseSavePause();
            foreach (string scene in attemptScenes) savedScenes.Add(scene);
            pendingPatch = null; pendingSettlement = false; Error = null;
            if (receipt != null) { PauseActors(); weapon.StopFirstTask(); }
            if (Attempt.Status == FlightStoryRunStatus.Success)
            {
                Finished = true;
                Attempt.MarkSettled();
                encounter.StoryFinished();
            }
            else EnterScene();
        }

        private void EnterScene()
        {
            if (Attempt.Status == FlightStoryRunStatus.Success) return;
            sceneStartedAt = Attempt.Now;
            pendingAdvanceScene = null;
            if (Attempt.CurrentSceneId == "C08-S04") Backup.BeginPhase(new FlightStoryPhase(Attempt.RunId, "backup_transition", 1, "backup_route"), Attempt.Now);
            if (Attempt.CurrentSceneId == "C08-S05") Stability.BeginPhase(new FlightStoryPhase(Attempt.RunId, "maintenance_handoff", 1, "main_route"), Attempt.Now);
            SceneEntered?.Invoke(Attempt.CurrentSceneId);
        }

        internal FlightProgressReceipt Conclude(bool success, FlightStoryFailureOrigin origin = FlightStoryFailureOrigin.Abandon)
        {
            if (receipt != null) return receipt;
            if (Attempt == null) return null;
            if (origin == FlightStoryFailureOrigin.Crash)
            {
                Fail(origin);
                pendingPatch = null;
                pendingSettlement = false;
                if (RewardEligibleMode && Attempt.CombatStarted && FlightProgress.Current.PendingId == Attempt.RunId) receipt = FlightProgress.Current.Recover();
                if (receipt != null) Attempt.MarkSettled();
                ReleaseSavePause();
                return receipt;
            }
            if (pendingPatch != null && pendingSettlement)
            {
                CommitPending();
                return receipt;
            }
            if (success) { if (pendingPatch != null) CommitPending(); return receipt; }
            Fail(origin);
            if (!Attempt.CombatStarted || !RewardEligibleMode) return null;
            FlightBroomCatalog equipment = weapon.GetComponent<FlightBroomEquipment>()?.Catalog;
            receipt = FlightProgress.Current.CompleteStory(Attempt.RunId, false, weapon.FirstTaskReady, Attempt.NormalCount, Attempt.EliteCount, Attempt.MidCount > 0, Attempt.FinalCount > 0, equipment, null);
            if (receipt != null) { Attempt.MarkSettled(); weapon.StopFirstTask(); pendingPatch = null; ReleaseSavePause(); }
            else Error = "정산 저장을 기다리고 있습니다. 재시도 전에 저장 상태를 확인하세요.";
            return receipt;
        }

        internal bool RestartStory()
        {
            if (recoveryPending) return false;
            Conclude(false, FlightStoryFailureOrigin.Retry);
            if (NeedsReceipt) return false;
            RestartAttempt();
            return true;
        }

        private void Fail(FlightStoryFailureOrigin origin)
        {
            if (receipt == null) Attempt.Fail(origin);
            failed = true;
            stopping = true;
            StopActors();
            encounter.StoryStopped();
        }

        internal void StopStory()
        {
            stopping = true;
            StopActors();
        }

        private void StopActors()
        {
            foreach (Actor actor in actors) if (actor.Unit != null) actor.Unit.StopCombat();
            pool.ReturnAll();
        }

        private void PauseActors()
        {
            foreach (Actor actor in actors) if (actor.Unit != null) actor.Unit.SetAttackEnabled(false);
            pool.ReturnAll();
        }

        private void ClearActors()
        {
            foreach (Actor actor in actors)
            {
                if (actor.Unit == null) continue;
                actor.Unit.Defeated -= OnDefeated; actor.Unit.ContactSweep -= OnContact;
                if (encounter != null) encounter.UnregisterStoryRewardSource(actor.Unit.Enemy);
                actor.Unit.StopCombat(); Destroy(actor.Unit.gameObject);
            }
            actors.Clear();
        }

        public void CopyPositions(List<Vector3> destination)
        {
            destination.Clear();
            foreach (Actor actor in actors) if (actor.Unit != null && !actor.Defeated && !actor.Unit.HasExited) destination.Add(actor.Unit.transform.position);
        }

        private void OnDestroy()
        {
            ReleaseSavePause();
            if (Active == this) Active = null;
            ClearActors();
            if (binding != null) { binding.gameObject.SetActive(false); Destroy(binding.gameObject); }
        }

        private void ReleaseSavePause()
        {
            if (!savePaused) return;
            Time.timeScale = beforeSaveScale;
            savePaused = false;
        }

        private bool RecoverPrevious()
        {
            if (string.IsNullOrEmpty(FlightProgress.Current.PendingId)) { Error = null; return true; }
            if (FlightProgress.Current.Recover() != null) { Error = null; return true; }
            Error = "이전 출격 기록 복구를 기다리고 있습니다.";
            return false;
        }

        private static IEnumerable<string> Expand(FlightStoryWave wave)
        {
            foreach (FlightStoryEnemySlot slot in wave.composition) for (int n = 0; n < slot.count; n++) yield return slot.enemy;
        }

        private static FlightStoryRewardClass RewardClass(string value)
        {
            switch (value)
            {
                case "NORMAL": return FlightStoryRewardClass.Normal;
                case "ELITE": return FlightStoryRewardClass.Elite;
                case "MID": return FlightStoryRewardClass.Mid;
                case "FINAL": return FlightStoryRewardClass.Final;
                default: return FlightStoryRewardClass.None;
            }
        }
    }
}
