using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectG
{
    public enum FlightStoryPlayMode { Story, StoryReplay, Side, Recollection, Practice }
    public enum FlightStoryRunStatus { Prepared, Active, Success, Failure, Settled }
    public enum FlightStoryFailureOrigin { None, Normal, Retry, Abandon, Crash }
    public enum FlightStoryRewardClass { None, Normal, Elite, Mid, Final }

    [Serializable]
    public struct FlightStoryPhase : IEquatable<FlightStoryPhase>
    {
        public string RunId;
        public string PhaseId;
        public int Sequence;
        public string RouteId;

        public FlightStoryPhase(string runId, string phaseId, int sequence, string routeId)
        {
            RunId = runId;
            PhaseId = phaseId;
            Sequence = sequence;
            RouteId = routeId;
        }

        public bool IsValid => FlightStoryValue.Id(RunId) && FlightStoryValue.Id(PhaseId) && Sequence >= 0 && FlightStoryValue.Id(RouteId);
        public bool Equals(FlightStoryPhase other) => RunId == other.RunId && PhaseId == other.PhaseId && Sequence == other.Sequence && RouteId == other.RouteId;
        public override bool Equals(object other) => other is FlightStoryPhase phase && Equals(phase);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RunId == null ? 0 : StringComparer.Ordinal.GetHashCode(RunId);
                hash = hash * 397 ^ (PhaseId == null ? 0 : StringComparer.Ordinal.GetHashCode(PhaseId));
                hash = hash * 397 ^ Sequence;
                return hash * 397 ^ (RouteId == null ? 0 : StringComparer.Ordinal.GetHashCode(RouteId));
            }
        }
    }

    [Serializable]
    public sealed class FlightStorySceneRequirements
    {
        public string[] Facts = Array.Empty<string>();
        public string[] Encounters = Array.Empty<string>();
        public string[] ProtectedTargets = Array.Empty<string>();
        public bool AllArrivals;
        public bool SustainedStability;
        public bool BackupStability;
    }

    [Serializable]
    public sealed class FlightStorySettlementProposal
    {
        public string RunId;
        public string StageId;
        public bool Success;
        public bool CombatStarted;
        public bool ParticipationEligible;
        public bool AwardEarnedRewards;
        public bool FinalClear;
        public FlightStoryFailureOrigin FailureOrigin;
        public int NormalCount;
        public int EliteCount;
        public int MidCount;
        public int FinalCount;
        public string[] CompletedSceneIds = Array.Empty<string>();
        public string[] ProposedMissionIds = Array.Empty<string>();
        public string[] ResolvedEncounterIds = Array.Empty<string>();

        internal FlightStorySettlementProposal Copy()
        {
            var copy = (FlightStorySettlementProposal)MemberwiseClone();
            copy.CompletedSceneIds = (string[])CompletedSceneIds.Clone();
            copy.ProposedMissionIds = (string[])ProposedMissionIds.Clone();
            copy.ResolvedEncounterIds = (string[])ResolvedEncounterIds.Clone();
            return copy;
        }
    }

    /// <summary>Current-sortie rules only. The progress store commits the proposal and receipt atomically.</summary>
    public sealed class FlightStoryAttempt
    {
        private sealed class Encounter
        {
            internal FlightStoryRewardClass RewardClass;
            internal bool Eligible;
            internal bool Resolved;
        }

        private sealed class Wave
        {
            internal string SceneId;
            internal bool[] Claimed;
        }

        private static readonly ReadOnlyCollection<string> finalFacts = Array.AsReadOnly(new[]
        {
            "backup_verified", "management_route_open", "local_closure_verified",
            "maintenance_handoff", "crosscheck_complete", "commander_approved"
        });
        private static readonly string[] c8Scenes = { "C08-S01", "C08-S02", "C08-S03", "C08-S04", "C08-S05", "C08-S06", "C08-S07" };
        private readonly string[] scenes;
        private readonly HashSet<string> sceneIds;
        private readonly Dictionary<string, FlightStorySceneRequirements> requirements = new Dictionary<string, FlightStorySceneRequirements>(StringComparer.Ordinal);
        private readonly Dictionary<string, HashSet<string>> facts = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        private readonly Dictionary<string, Encounter> encounters = new Dictionary<string, Encounter>(StringComparer.Ordinal);
        private readonly Dictionary<string, Wave> waves = new Dictionary<string, Wave>(StringComparer.Ordinal);
        private readonly HashSet<string> satisfied = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> completedOrder = new List<string>();
        private readonly List<string> proposedMissions = new List<string>();
        private readonly List<string> resolvedOrder = new List<string>();
        private readonly FlightStoryPlayMode mode;
        private int sceneIndex;
        private FlightStorySettlementProposal settledProposal;

        public FlightStoryAttempt(string runId, string stageId, IEnumerable<string> orderedSceneIds, FlightStoryPlayMode mode = FlightStoryPlayMode.Story)
        {
            if (!FlightStoryValue.Id(runId)) throw new ArgumentException("A sortie ID is required.", nameof(runId));
            if (!FlightStoryValue.Id(stageId)) throw new ArgumentException("A stage ID is required.", nameof(stageId));
            if (!Enum.IsDefined(typeof(FlightStoryPlayMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            scenes = FlightStoryValue.RequiredIds(orderedSceneIds, nameof(orderedSceneIds));
            if (stageId == "ST-8-OP")
            {
                if (scenes.Length != c8Scenes.Length) throw new ArgumentException("ST-8-OP requires all seven ordered scenes.", nameof(orderedSceneIds));
                for (int i = 0; i < scenes.Length; i++)
                    if (scenes[i] != c8Scenes[i]) throw new ArgumentException("ST-8-OP scene order must be S01 through S07.", nameof(orderedSceneIds));
            }
            sceneIds = new HashSet<string>(scenes, StringComparer.Ordinal);
            RunId = runId;
            StageId = stageId;
            this.mode = mode;
        }

        public static IReadOnlyList<string> FinalFacts => finalFacts;
        public string RunId { get; }
        public string StageId { get; }
        public string CurrentSceneId => scenes[sceneIndex];
        public double Now { get; private set; }
        public FlightStoryRunStatus Status { get; private set; } = FlightStoryRunStatus.Prepared;
        public FlightStoryFailureOrigin FailureOrigin { get; private set; }
        public bool CombatStarted { get; private set; }
        public int NormalCount { get; private set; }
        public int EliteCount { get; private set; }
        public int MidCount { get; private set; }
        public int FinalCount { get; private set; }
        public IReadOnlyList<string> SatisfiedSceneIds => completedOrder.AsReadOnly();
        public IReadOnlyList<string> ProposedMissionIds => proposedMissions.AsReadOnly();
        public bool CanCommitMissionCompletions => Status == FlightStoryRunStatus.Success;
        private bool Mutable => Status == FlightStoryRunStatus.Prepared || Status == FlightStoryRunStatus.Active;
        private bool RewardMode => mode == FlightStoryPlayMode.Story || mode == FlightStoryPlayMode.StoryReplay;

        public bool DefineScene(string sceneId, FlightStorySceneRequirements definition)
        {
            if (!Mutable || !FlightStoryValue.Id(sceneId) || !sceneIds.Contains(sceneId) || requirements.ContainsKey(sceneId) || definition == null) return false;
            if (!FlightStoryValue.TryIds(definition.Facts, out string[] factIds) || !FlightStoryValue.TryIds(definition.Encounters, out string[] encounterIds) || !FlightStoryValue.TryIds(definition.ProtectedTargets, out string[] targetIds)) return false;
            if (StageId == "ST-8-OP" && sceneId == "C08-S07")
            {
                var mandatory = new List<string>(factIds);
                foreach (string fact in finalFacts) if (!mandatory.Contains(fact)) mandatory.Add(fact);
                factIds = mandatory.ToArray();
            }
            if (factIds.Length + encounterIds.Length + targetIds.Length == 0 && !definition.AllArrivals && !definition.SustainedStability && !definition.BackupStability) return false;
            requirements.Add(sceneId, new FlightStorySceneRequirements
            {
                Facts = factIds,
                Encounters = encounterIds,
                ProtectedTargets = targetIds,
                AllArrivals = definition.AllArrivals,
                SustainedStability = definition.SustainedStability,
                BackupStability = definition.BackupStability
            });
            facts.Add(sceneId, new HashSet<string>(StringComparer.Ordinal));
            return true;
        }

        public bool SetFact(string runId, string sceneId, string factId, bool value)
        {
            if (!Mutable || runId != RunId || sceneId != CurrentSceneId || !requirements.TryGetValue(sceneId, out FlightStorySceneRequirements required) || Array.IndexOf(required.Facts, factId) < 0) return false;
            if (value) facts[sceneId].Add(factId); else facts[sceneId].Remove(factId);
            return true;
        }

        public bool HasFact(string sceneId, string factId) => FlightStoryValue.Id(sceneId) && FlightStoryValue.Id(factId) && facts.TryGetValue(sceneId, out HashSet<string> values) && values.Contains(factId);
        public bool IsSceneSatisfied(string sceneId) => FlightStoryValue.Id(sceneId) && satisfied.Contains(sceneId);

        public bool BeginCombat(string runId)
        {
            if (!Mutable || runId != RunId || CombatStarted) return false;
            CombatStarted = true;
            Status = FlightStoryRunStatus.Active;
            return true;
        }

        public bool AdvanceTime(double delta, bool paused = false)
        {
            if (!Mutable || paused || !FlightStoryValue.Finite(delta) || delta <= 0d || !FlightStoryValue.Finite(Now + delta)) return false;
            Now += delta;
            return true;
        }

        public bool TryAdvance(FlightStoryManifest manifest = null, FlightStoryStability stability = null, IEnumerable<FlightStoryProtectedTarget> protectedTargets = null)
        {
            if (!Mutable || !requirements.TryGetValue(CurrentSceneId, out FlightStorySceneRequirements required)) return false;
            foreach (string fact in required.Facts) if (!facts[CurrentSceneId].Contains(fact)) return false;
            foreach (string id in required.Encounters) if (!encounters.TryGetValue(id, out Encounter encounter) || !encounter.Resolved) return false;
            if (required.ProtectedTargets.Length != 0)
            {
                if (protectedTargets == null) return false;
                var targets = new Dictionary<string, FlightStoryProtectedTarget>(StringComparer.Ordinal);
                foreach (FlightStoryProtectedTarget target in protectedTargets)
                {
                    if (target == null || target.RunId != RunId || targets.ContainsKey(target.Id)) return false;
                    targets.Add(target.Id, target);
                }
                foreach (string id in required.ProtectedTargets) if (!targets.TryGetValue(id, out FlightStoryProtectedTarget target) || target.IsFailed) return false;
            }
            bool c8Final = StageId == "ST-8-OP" && CurrentSceneId == "C08-S07";
            if ((required.AllArrivals || c8Final) && (manifest == null || manifest.RunId != RunId || !manifest.AllArrived)) return false;
            if ((required.SustainedStability || c8Final) && (stability == null || stability.RunId != RunId || !stability.IsSustainedQualified(Now))) return false;
            if (required.BackupStability && (stability == null || stability.RunId != RunId || !stability.IsBackupQualified(Now))) return false;
            if (c8Final && completedOrder.Count != 6) return false;
            satisfied.Add(CurrentSceneId);
            completedOrder.Add(CurrentSceneId);
            if (sceneIndex + 1 < scenes.Length)
            {
                sceneIndex++;
                Status = FlightStoryRunStatus.Active;
            }
            else Status = FlightStoryRunStatus.Success;
            return true;
        }

        public bool RegisterWave(string sceneId, string waveId, int slotCount)
        {
            if (!Mutable || !FlightStoryValue.Id(sceneId) || !sceneIds.Contains(sceneId) || !FlightStoryValue.Id(waveId) || slotCount <= 0 || waves.ContainsKey(waveId)) return false;
            waves.Add(waveId, new Wave { SceneId = sceneId, Claimed = new bool[slotCount] });
            return true;
        }

        public bool TryClaimWaveSlot(string runId, string sceneId, string waveId, int slotIndex)
        {
            if (!Mutable || !CombatStarted || runId != RunId || sceneId != CurrentSceneId || !FlightStoryValue.Id(waveId) || !waves.TryGetValue(waveId, out Wave wave) || wave.SceneId != sceneId || slotIndex < 0 || slotIndex >= wave.Claimed.Length || wave.Claimed[slotIndex]) return false;
            wave.Claimed[slotIndex] = true;
            return true;
        }

        public bool RegisterEncounter(string encounterInstanceId, FlightStoryRewardClass rewardClass, bool eligible = true)
        {
            if (!Mutable || !FlightStoryValue.Id(encounterInstanceId) || encounters.ContainsKey(encounterInstanceId) || !Enum.IsDefined(typeof(FlightStoryRewardClass), rewardClass)) return false;
            encounters.Add(encounterInstanceId, new Encounter { RewardClass = rewardClass, Eligible = eligible && rewardClass != FlightStoryRewardClass.None });
            return true;
        }

        public bool TryResolveEncounter(string runId, string encounterInstanceId, bool safetyConfirmed)
        {
            if (!Mutable || runId != RunId || !safetyConfirmed || !FlightStoryValue.Id(encounterInstanceId) || !encounters.TryGetValue(encounterInstanceId, out Encounter encounter) || encounter.Resolved || (encounter.RewardClass != FlightStoryRewardClass.None && !CombatStarted)) return false;
            encounter.Resolved = true;
            resolvedOrder.Add(encounterInstanceId);
            if (encounter.Eligible && RewardMode)
            {
                switch (encounter.RewardClass)
                {
                    case FlightStoryRewardClass.Normal: NormalCount++; break;
                    case FlightStoryRewardClass.Elite: EliteCount++; break;
                    case FlightStoryRewardClass.Mid: MidCount++; break;
                    case FlightStoryRewardClass.Final: FinalCount++; break;
                }
            }
            return true;
        }

        public bool TryProposeMissionCompletion(string missionId, IEnumerable<string> requiredSceneIds)
        {
            if ((!Mutable && Status != FlightStoryRunStatus.Success) || !FlightStoryValue.Id(missionId) || !FlightStoryValue.TryIds(requiredSceneIds, out string[] needed) || needed.Length == 0) return false;
            foreach (string id in needed) if (!satisfied.Contains(id)) return false;
            if (!proposedMissions.Contains(missionId)) proposedMissions.Add(missionId);
            return true;
        }

        public bool Fail(FlightStoryFailureOrigin origin)
        {
            if (!Mutable || origin == FlightStoryFailureOrigin.None || !Enum.IsDefined(typeof(FlightStoryFailureOrigin), origin)) return false;
            FailureOrigin = origin;
            Status = FlightStoryRunStatus.Failure;
            return true;
        }

        public FlightStorySettlementProposal CreateSettlementProposal()
        {
            if (settledProposal != null) return settledProposal.Copy();
            if (Status != FlightStoryRunStatus.Success && Status != FlightStoryRunStatus.Failure) return null;
            bool success = Status == FlightStoryRunStatus.Success;
            bool participation = CombatStarted && RewardMode;
            bool earned = participation && FailureOrigin != FlightStoryFailureOrigin.Crash;
            return new FlightStorySettlementProposal
            {
                RunId = RunId,
                StageId = StageId,
                Success = success,
                CombatStarted = CombatStarted,
                ParticipationEligible = participation,
                AwardEarnedRewards = earned,
                FinalClear = success && earned && FinalCount > 0,
                FailureOrigin = FailureOrigin,
                NormalCount = earned ? NormalCount : 0,
                EliteCount = earned ? EliteCount : 0,
                MidCount = earned ? MidCount : 0,
                FinalCount = earned ? FinalCount : 0,
                CompletedSceneIds = success ? completedOrder.ToArray() : Array.Empty<string>(),
                ProposedMissionIds = success ? proposedMissions.ToArray() : Array.Empty<string>(),
                ResolvedEncounterIds = earned ? resolvedOrder.ToArray() : Array.Empty<string>()
            };
        }

        /// <summary>Call only after the real progress store has durably committed this sortie's receipt.</summary>
        public bool MarkSettled()
        {
            if (Status == FlightStoryRunStatus.Settled) return true;
            FlightStorySettlementProposal proposal = CreateSettlementProposal();
            if (proposal == null) return false;
            settledProposal = proposal.Copy();
            Status = FlightStoryRunStatus.Settled;
            if (!proposal.Success)
            {
                facts.Clear();
                satisfied.Clear();
                completedOrder.Clear();
                proposedMissions.Clear();
                encounters.Clear();
                resolvedOrder.Clear();
                waves.Clear();
            }
            return true;
        }
    }

    internal static class FlightStoryValue
    {
        internal static bool Id(string value) => !string.IsNullOrWhiteSpace(value);
        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        internal static string[] RequiredIds(IEnumerable<string> values, string parameter)
        {
            if (!TryIds(values, out string[] result) || result.Length == 0) throw new ArgumentException("A nonempty set of unique IDs is required.", parameter);
            return result;
        }

        internal static bool TryIds(IEnumerable<string> values, out string[] result)
        {
            var items = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            if (values != null)
            {
                foreach (string value in values)
                {
                    if (!Id(value) || !unique.Add(value)) { result = Array.Empty<string>(); return false; }
                    items.Add(value);
                }
            }
            result = items.ToArray();
            return true;
        }
    }
}
