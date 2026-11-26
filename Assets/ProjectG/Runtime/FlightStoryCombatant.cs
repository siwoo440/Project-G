using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectG
{
    [DefaultExecutionOrder(105)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(FlightEnemy))]
    public sealed class FlightStoryCombatant : MonoBehaviour
    {
        private FlightStoryEnemyProfile profile;
        private IReadOnlyList<FlightStoryPatternProfile> patterns;
        private FlightStoryGauge gauge;
        private readonly FlightStoryAttackClock clock = new FlightStoryAttackClock();
        private FlightStoryPatternProfile currentPattern;
        private FlightThreatPool pool;
        private FlightPilotVitals pilot;
        private Transform anchor;
        private FlightEnemy enemy;
        private Collider[] combatShapes;
        private GameObject weakPoint;
        private LineRenderer warning;
        private Material warningMaterial;
        private Vector3 home, entryStart, previousPilot, lockedDirection, lockedOrigin, chargeOrigin;
        private Vector3 retreatStart, retreatLocalTarget;
        private float enteredAt, entrySeconds = 1.5f, nextCycle, rootedUntil, chargeElapsed, retreatStarted, retreatSeconds = 2f, rockLandingY, rockTelegraphUntil;
        private bool attackEnabled, stopped, suppressed, retreating, retreatConfigured, chargeStarted, returning, rockLanded, rockHit;

        public string ProfileId => profile == null ? string.Empty : profile.Id;
        public string InstanceId { get; private set; }
        public bool IsSuppressed => suppressed;
        public bool HasExited { get; private set; }
        public FlightEnemy Enemy => enemy;
        public FlightStoryEnemyProfile Profile => profile;
        public int Phase => gauge == null ? 1 : gauge.Phase;
        public bool CanThreaten => !stopped && !suppressed && !retreating && !HasExited && isActiveAndEnabled;
        public bool IsAttacking => CanThreaten && attackEnabled && Time.time >= enteredAt + entrySeconds;
        internal int GaugeHealth => gauge == null ? (profile == null ? 1 : profile.Gauge) : gauge.Health;
        public event Action<FlightStoryCombatant> Suppressed, Exited, Defeated;
        public event Action<FlightStoryCombatant, Vector3, Vector3, float, int> ContactSweep;

        public static FlightStoryCombatant Create(FlightEnemy template, string enemyId, FlightThreatPool sharedPool, FlightPilotVitals target, Transform stageAnchor, Vector3 localSpawn)
        {
            FlightStoryEnemyProfile definition = FlightStoryCombatProfiles.FindEnemy(enemyId);
            if (template == null || definition == null || sharedPool == null || target == null) return null;
            Vector3 position = World(stageAnchor, localSpawn);
            // A blocked spawn is retried by the finite-wave scheduler, never moved onto a target.
            if ((position - target.transform.position).sqrMagnitude < 400f || Physics.CheckSphere(position, definition.Radius, 1 << 8, QueryTriggerInteraction.Ignore)) return null;
            var holder = new GameObject("StorySpawnPreparation");
            holder.SetActive(false);
            FlightEnemy clone = Instantiate(template, holder.transform);
            clone.gameObject.SetActive(false);
            foreach (FlightStageBoss legacy in clone.GetComponents<FlightStageBoss>()) legacy.enabled = false;
            foreach (FlightEnemyRoute legacy in clone.GetComponents<FlightEnemyRoute>()) legacy.enabled = false;
            FlightStoryCombatant actor = clone.GetComponent<FlightStoryCombatant>();
            if (actor == null) actor = clone.gameObject.AddComponent<FlightStoryCombatant>();
            actor.Initialize(clone, definition, sharedPool, target, stageAnchor, localSpawn);
            clone.gameObject.name = definition.Name + " [" + actor.InstanceId + "]";
            clone.transform.SetParent(null, true);
            Destroy(holder);
            clone.gameObject.SetActive(true);
            return actor;
        }

        private void Initialize(FlightEnemy life, FlightStoryEnemyProfile definition, FlightThreatPool sharedPool, FlightPilotVitals target, Transform stageAnchor, Vector3 localSpawn)
        {
            enemy = life;
            profile = definition;
            rockLanded = profile.Id == "H-ROCK";
            pool = sharedPool;
            pilot = target;
            anchor = stageAnchor;
            InstanceId = Guid.NewGuid().ToString("N");
            patterns = FlightStoryCombatProfiles.PatternsFor(profile.Id);
            gauge = new FlightStoryGauge(profile.Gauge, Mathf.Max(1, patterns.Count), profile.IsNonlethal);
            enteredAt = Time.time;
            gauge.ProtectUntil(enteredAt + entrySeconds);
            home = entryStart = World(anchor, localSpawn);
            transform.SetPositionAndRotation(home, Yaw(anchor) * Quaternion.Euler(0f, 180f, 0f));
            previousPilot = pilot.transform.position;
            enemy.ConfigureStoryCombat(this, definition);
            enemy.Defeated += OnEnemyDefeated;
            foreach (FlightWeakPoint old in GetComponentsInChildren<FlightWeakPoint>(true)) old.gameObject.SetActive(false);
            combatShapes = GetComponents<Collider>();
            foreach (Collider shape in combatShapes)
            {
                shape.enabled = true;
                if (shape is SphereCollider sphere) { sphere.radius = profile.Radius; sphere.center = Vector3.zero; }
            }
            if (combatShapes.Length == 0)
            {
                SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = profile.Radius;
                combatShapes = new Collider[] { sphere };
            }
            gameObject.layer = 9;
            CreatePresentation();
            SetDefaultRetreat(localSpawn);
        }

        private void CreatePresentation()
        {
            var lineObject = new GameObject("StoryDirectionWarning");
            lineObject.transform.SetParent(transform, false);
            warning = lineObject.AddComponent<LineRenderer>();
            warning.useWorldSpace = true;
            warning.positionCount = 2;
            warning.startWidth = warning.endWidth = 0.12f;
            warning.startColor = warning.endColor = new Color(1f, 0.65f, 0.1f, 0.85f);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                warningMaterial = new Material(shader);
                warningMaterial.SetColor("_BaseColor", new Color(1f, 0.65f, 0.1f));
                warning.sharedMaterial = warningMaterial;
            }
            warning.enabled = false;
            if (patterns.Count > 0)
            {
                weakPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                weakPoint.name = "StoryWeakPoint";
                weakPoint.transform.SetParent(transform, false);
                weakPoint.transform.localPosition = Vector3.up * profile.Radius;
                weakPoint.transform.localScale = Vector3.one * 0.7f;
                weakPoint.layer = 9;
                if (warningMaterial != null) weakPoint.GetComponent<Renderer>().sharedMaterial = warningMaterial;
                weakPoint.AddComponent<FlightWeakPoint>().Configure(enemy, 0.5f);
                weakPoint.SetActive(false);
            }
            if (profile.Id == "H-ROCK")
            {
                Transform model = transform.Find("Model");
                if (model != null) model.gameObject.SetActive(false);
                var rubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rubble.name = "StoryRock";
                rubble.transform.SetParent(transform, false);
                rubble.transform.localScale = Vector3.one * profile.Radius * 2f;
                Destroy(rubble.GetComponent<Collider>());
                Renderer original = enemy.GetComponentInChildren<Renderer>(true);
                if (original != null) rubble.GetComponent<Renderer>().sharedMaterial = original.sharedMaterial;
            }
        }

        public void SetAttackEnabled(bool enabled)
        {
            if (attackEnabled == enabled || stopped || suppressed || HasExited || retreating) return;
            attackEnabled = enabled;
            if (!enabled)
            {
                clock.Stop();
                chargeStarted = false;
                returning = (transform.position - home).sqrMagnitude > 0.001f;
                ShowWeak(false);
                if (warning != null) warning.enabled = false;
            }
        }

        public void SetEntryRoute(Vector3 localCombatPoint, float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 1.5f || Time.time > enteredAt + entrySeconds) return;
            home = World(anchor, localCombatPoint);
            entrySeconds = seconds;
            gauge.ProtectUntil(enteredAt + seconds);
        }

        public void SetRetreatRoute(Vector3 localTarget, float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f || !Finite(localTarget)) return;
            retreatLocalTarget = localTarget;
            retreatSeconds = seconds;
            retreatConfigured = true;
        }

        public void ConfigureRock(bool falling, float localLandingY)
        {
            if (profile == null || profile.Id != "H-ROCK" || suppressed || float.IsNaN(localLandingY) || float.IsInfinity(localLandingY)) return;
            rockLandingY = localLandingY;
            rockLanded = !falling;
            rockHit = false;
            rockTelegraphUntil = Mathf.Max(enteredAt + entrySeconds, Time.time + 2f);
            if (warning != null) warning.enabled = false;
        }

        public void BeginRetreat()
        {
            if (HasExited || retreating || enemy == null || (!profile.IsNonlethal && enemy.Health <= 0)) return;
            StopCombat();
            // Karden remains on site until the director supplies the rescue route.
            if (profile.Id == "B-KARDEN" && !retreatConfigured) return;
            retreating = true;
            retreatStart = transform.position;
            retreatStarted = Time.time;
        }

        public void StopCombat()
        {
            stopped = true;
            attackEnabled = false;
            retreating = false;
            chargeStarted = returning = false;
            clock.Stop();
            ShowWeak(false);
            if (warning != null) warning.enabled = false;
            if (combatShapes != null) foreach (Collider shape in combatShapes) if (shape != null) shape.enabled = false;
            if (pool != null) pool.ReturnStoryOwner(this);
        }

        internal int ResolveHealth(int damage)
        {
            if (stopped || retreating || HasExited || gauge == null) return GaugeHealth;
            float multiplier = profile.Id == "B-RAY" && gauge.Phase == 1 && clock.Stage != FlightStoryAttackStage.Recovery ? 0.5f : 1f;
            FlightStoryHitResult result = gauge.Apply(damage, Time.time, multiplier);
            if (result.PhaseChanged)
            {
                pool.ReturnStoryOwner(this);
                clock.Stop();
                ShowWeak(false);
                warning.enabled = false;
                chargeStarted = false;
                returning = (transform.position - home).sqrMagnitude > 0.001f;
            }
            return gauge.Health;
        }

        internal void OnSuppressed()
        {
            if (suppressed) return;
            suppressed = true;
            StopCombat();
            Suppressed?.Invoke(this);
            if (profile.Id == "H-ROCK") gameObject.SetActive(false);
        }

        private void OnEnemyDefeated(FlightEnemy value)
        {
            StopCombat();
            Defeated?.Invoke(this);
        }

        private void Update()
        {
            if (profile == null || Time.timeScale == 0f) return;
            float now = Time.time;
            if (retreating)
            {
                Vector3 target = World(anchor, retreatLocalTarget);
                float progress = Mathf.Clamp01((now - retreatStarted) / retreatSeconds);
                Vector3 next = Vector3.Lerp(retreatStart, target, progress);
                if (!PathClear(transform.position, next, profile.Radius)) { retreatStarted += Time.deltaTime; return; }
                transform.position = next;
                // Arrival is physical evidence; the director owns the separate outside-area hold.
                if (now >= retreatStarted + retreatSeconds)
                {
                    HasExited = true;
                    retreating = false;
                    Exited?.Invoke(this);
                    gameObject.SetActive(false);
                }
                return;
            }
            if (!CanThreaten || pilot == null || pilot.IsDefeated) { if (pilot != null && pilot.IsDefeated) StopCombat(); return; }
            if (now < enteredAt + entrySeconds)
            {
                Vector3 next = Vector3.Lerp(entryStart, home, Mathf.Clamp01((now - enteredAt) / entrySeconds));
                if (PathClear(transform.position, next, profile.Radius)) transform.position = next;
                previousPilot = pilot.transform.position;
                return;
            }
            if (returning)
            {
                if (now >= rootedUntil)
                {
                    Vector3 next = Vector3.MoveTowards(transform.position, home, profile.Speed * Time.deltaTime);
                    if (PathClear(transform.position, next, profile.Radius)) transform.position = next;
                    returning = (transform.position - home).sqrMagnitude > 0.001f;
                }
                previousPilot = pilot.transform.position;
                return;
            }
            if (!attackEnabled || now + 0.00001f < gauge.ProtectedUntil) { previousPilot = pilot.transform.position; return; }
            if (profile.Id == "H-ROCK") { UpdateRock(now); previousPilot = pilot.transform.position; return; }
            Vector3 beforeCharge = transform.position;
            bool charging = UpdateCharge(now);
            if (charging) Contact(beforeCharge, transform.position, 15);
            if (!CanThreaten) return;
            clock.Advance(now);
            ShowWeak(patterns.Count > 0 && clock.IsWeak(now));
            if (clock.Stage == FlightStoryAttackStage.Recovery && chargeStarted)
            {
                chargeStarted = false;
                returning = (transform.position - home).sqrMagnitude > 0.001f;
            }
            if (clock.Stage == FlightStoryAttackStage.Idle && now >= nextCycle)
            {
                currentPattern = patterns.Count > 0 ? patterns[gauge.Phase - 1] : OrdinaryCycle();
                lockedDirection = Vector3.ProjectOnPlane(pilot.transform.position - transform.position, Vector3.up).normalized;
                if (lockedDirection.sqrMagnitude < 0.0001f) lockedDirection = Yaw(anchor) * Vector3.back;
                lockedOrigin = patterns.Count > 0 ? World(anchor, new Vector3(0f, 4f, 44f)) + (transform.position - home) : transform.position;
                if (!AttackRouteClear(currentPattern)) { nextCycle = now + 0.1f; return; }
                clock.Start(currentPattern, now);
                chargeOrigin = transform.position;
                chargeElapsed = 0f;
                chargeStarted = false;
                nextCycle = now + profile.Period;
                warning.SetPosition(0, lockedOrigin);
                warning.SetPosition(1, lockedOrigin + lockedDirection * (currentPattern.Geometry == FlightStoryGeometry.Charge ? 28f : 32f));
            }
            warning.enabled = clock.Stage == FlightStoryAttackStage.Telegraph;
            if (clock.IsReady(now))
            {
                IReadOnlyList<FlightStoryShotPlan> plans = patterns.Count > 0 ? FlightStoryPatternRules.Build(currentPattern, clock.Burst) : FlightStoryPatternRules.BuildOrdinary(profile);
                bool accepted = plans.Count == 0 || Fire(plans);
                if (accepted) clock.AcceptBurst(now);
                else clock.RefuseBurst(now);
            }
            bool ordinaryContact = patterns.Count == 0 && profile.Id != "E-CHARGE" && profile.Id != "E-ARCHIVE";
            if (ordinaryContact || (profile.Id == "B-RAY" && clock.Stage != FlightStoryAttackStage.Telegraph)) Contact(transform.position, transform.position, 15);
            previousPilot = pilot.transform.position;
        }

        private FlightStoryPatternProfile OrdinaryCycle()
        {
            bool charge = profile.Id == "E-CHARGE" || profile.Id == "E-ARCHIVE";
            float active = charge ? 24f / profile.Speed : 0f;
            return new FlightStoryPatternProfile
            {
                Id = profile.Id, BossId = profile.Id, Phase = 1,
                Telegraph = profile.Telegraph, Bursts = 1, Count = charge ? 0 : 1,
                Speed = profile.Speed, Damage = profile.Damage, ActiveSeconds = active,
                RecoverySeconds = charge ? 1.3f : Mathf.Max(0f, profile.Period - profile.Telegraph),
                Geometry = charge ? FlightStoryGeometry.Charge : FlightStoryGeometry.Aimed
            };
        }

        private bool Fire(IReadOnlyList<FlightStoryShotPlan> plans)
        {
            var shots = new List<FlightStoryThreatShot>(plans.Count);
            foreach (FlightStoryShotPlan plan in plans)
            {
                Vector3 position = lockedOrigin + Yaw(anchor) * Vector3.right * plan.MuzzleX;
                Vector3 direction = lockedDirection;
                if (plan.Axis == FlightStoryAimAxis.ForwardCrossing) direction = (lockedOrigin + lockedDirection * 14f - position).normalized;
                else if (plan.Axis == FlightStoryAimAxis.LocalCrossing) direction = (World(anchor, new Vector3(0f, 4f, 30f)) - position).normalized;
                Quaternion basis = Quaternion.LookRotation(direction, Vector3.up);
                Vector3 spread = plan.Cone > 0f
                    ? Quaternion.AngleAxis(plan.Roll, Vector3.forward) * (Quaternion.AngleAxis(plan.Cone, Vector3.up) * Vector3.forward)
                    : Quaternion.Euler(plan.Pitch, plan.Yaw, 0f) * Vector3.forward;
                shots.Add(new FlightStoryThreatShot { Position = position, Direction = basis * spread, Plan = plan, Owner = this, CanAbsorb = profile.ControlPercent > 0 });
            }
            return pool.TryFireStoryBurst(shots);
        }

        private bool UpdateCharge(float now)
        {
            if (currentPattern == null || clock.Stage != FlightStoryAttackStage.Active || clock.Burst == 0) return false;
            bool charge = currentPattern.Geometry == FlightStoryGeometry.Charge;
            bool delayed = currentPattern.Geometry == FlightStoryGeometry.FanCharge;
            if (!charge && !delayed) return false;
            if (delayed && now < clock.AttackStarted + 1.1d) return false;
            float distance = patterns.Count == 0 ? 24f : delayed ? 21f : 28f;
            float speed = patterns.Count == 0 ? profile.Speed : 14f;
            chargeStarted = true;
            if (now < rootedUntil)
            {
                clock.Delay(Time.deltaTime);
                return false;
            }
            if (chargeElapsed * speed >= distance) return false;
            chargeElapsed = Mathf.Clamp((float)(now - clock.AttackStarted) - (delayed ? 1.1f : 0f), 0f, distance / speed);
            Vector3 next = chargeOrigin + lockedDirection * (chargeElapsed * speed);
            if (!InsideArena(next) || !PathClear(transform.position, next, profile.Radius)) return false;
            transform.position = next;
            return true;
        }

        private void UpdateRock(float now)
        {
            if (rockLanded) return;
            Vector3 bottom = World(anchor, new Vector3(Local(transform.position).x, rockLandingY, Local(transform.position).z));
            warning.enabled = now < rockTelegraphUntil;
            warning.SetPosition(0, transform.position);
            warning.SetPosition(1, bottom);
            if (warning.enabled) return;
            Vector3 before = transform.position;
            float step = profile.Speed * Time.deltaTime;
            if (Physics.SphereCast(before, profile.Radius, Vector3.down, out RaycastHit hit, step, 1 << 8, QueryTriggerInteraction.Ignore))
            {
                transform.position = before + Vector3.down * hit.distance;
                rockLanded = true;
            }
            else
            {
                Vector3 next = Vector3.MoveTowards(before, bottom, step);
                transform.position = next;
                rockLanded = (next - bottom).sqrMagnitude < 0.001f;
            }
            if (!rockHit) rockHit = Contact(before, transform.position, 10);
        }

        private bool Contact(Vector3 from, Vector3 to, int damage)
        {
            if (!CanThreaten) return false;
            ContactSweep?.Invoke(this, from, to, profile.Radius, damage);
            if (!CanThreaten) return false;
            bool near = false, awarded = true;
            return pilot.ResolveThreat(from - previousPilot, to - pilot.transform.position, profile.Radius, damage, ref near, ref awarded);
        }

        internal bool TryKnockback(Vector3 direction, float distance)
        {
            if (!CanThreaten || profile.ControlPercent <= 0 || !Finite(direction) || float.IsNaN(distance) || float.IsInfinity(distance) || distance <= 0f || direction.sqrMagnitude < 0.0001f) return false;
            Vector3 delta = direction.normalized * (distance * profile.ControlPercent / 100f);
            if (Physics.SphereCast(transform.position, profile.Radius, delta.normalized, out RaycastHit hit, delta.magnitude, 1 << 8, QueryTriggerInteraction.Ignore)) delta = delta.normalized * Mathf.Max(0f, hit.distance - 0.01f);
            Vector3 local = Local(transform.position + delta);
            float radius = profile.Radius;
            local = new Vector3(Mathf.Clamp(local.x, -36f + radius, 36f - radius), Mathf.Clamp(local.y, -8f + radius, 24f - radius), Mathf.Clamp(local.z, -20f + radius, 80f - radius));
            delta = World(anchor, local) - transform.position;
            transform.position += delta;
            home += delta;
            entryStart += delta;
            chargeOrigin += delta;
            // Control never changes retreat targets, gauge state, or encounter resolution.
            return true;
        }

        internal bool TryRoot(float duration)
        {
            if (!CanThreaten || profile.ControlPercent <= 0 || float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f) return false;
            rootedUntil = Mathf.Max(rootedUntil, Time.time + duration * profile.ControlPercent / 100f);
            return true;
        }

        private bool AttackRouteClear(FlightStoryPatternProfile pattern)
        {
            float distance = pattern.Geometry == FlightStoryGeometry.FanCharge ? 21f : patterns.Count > 0 ? 28f : 24f;
            if (pattern.Geometry == FlightStoryGeometry.Charge || pattern.Geometry == FlightStoryGeometry.FanCharge)
            {
                Vector3 end = transform.position + lockedDirection * distance;
                return InsideArena(end) && PathClear(transform.position, end, profile.Radius);
            }
            return true;
        }

        private void SetDefaultRetreat(Vector3 spawn)
        {
            retreatLocalTarget = new Vector3(spawn.x < 0f ? -34f : 34f, 20f, 76f);
            if (profile.Id == "B-PROLOGUE") { retreatLocalTarget = new Vector3(24, 16, 76); retreatSeconds = 5f; }
            else if (profile.Id == "B-RAY") { retreatLocalTarget = new Vector3(-24, 14, 76); retreatSeconds = 4f; }
            else if (profile.Id == "B-VARKAN") { retreatLocalTarget = spawn + Vector3.forward * 24f; retreatSeconds = 6f; }
            else if (profile.Id == "B-RESONANCE") { retreatLocalTarget = spawn + Vector3.forward * 32f; retreatSeconds = 8f; }
            else if (profile.Id == "E-EEL") retreatLocalTarget = new Vector3(24, 4, 40);
            else if (profile.Id == "E-EAGLE") retreatLocalTarget = new Vector3(-28, 12, 56);
            else if (profile.Id == "E-ARCHIVE") retreatLocalTarget = new Vector3(24, 0, 36);
        }

        private void ShowWeak(bool open) { if (weakPoint != null) weakPoint.SetActive(open); }
        private static bool Finite(Vector3 value) => !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z);
        private static Quaternion Yaw(Transform value) => Quaternion.Euler(0f, value == null ? 0f : value.eulerAngles.y, 0f);
        private static Vector3 World(Transform value, Vector3 local) => (value == null ? Vector3.zero : value.position) + Yaw(value) * local;
        private Vector3 Local(Vector3 world) => Quaternion.Inverse(Yaw(anchor)) * (world - (anchor == null ? Vector3.zero : anchor.position));
        private bool InsideArena(Vector3 world) { Vector3 value = Local(world); return value.x >= -36f + profile.Radius && value.x <= 36f - profile.Radius && value.y >= -8f + profile.Radius && value.y <= 24f - profile.Radius && value.z >= -20f + profile.Radius && value.z <= 80f - profile.Radius; }
        private static bool PathClear(Vector3 start, Vector3 end, float radius) { Vector3 offset = end - start; return offset.sqrMagnitude < 0.000001f || !Physics.SphereCast(start, radius, offset.normalized, out _, offset.magnitude, 1 << 8, QueryTriggerInteraction.Ignore); }

        private void OnDisable() { if (profile != null) StopCombat(); }
        private void OnDestroy()
        {
            if (enemy != null) enemy.Defeated -= OnEnemyDefeated;
            if (pool != null) pool.ReturnStoryOwner(this);
            if (warningMaterial != null) Destroy(warningMaterial);
        }
    }
}
