using System;
using System.Collections.Generic;

namespace ProjectG
{
    public readonly struct FlightStoryHitResult
    {
        public readonly bool Changed, PhaseChanged, Ended;
        public FlightStoryHitResult(bool changed, bool phaseChanged, bool ended)
        {
            Changed = changed;
            PhaseChanged = phaseChanged;
            Ended = ended;
        }
    }

    public sealed class FlightStoryGauge
    {
        public const double TransitionSeconds = 0.9d;
        public int Maximum { get; }
        public int PhaseCount { get; }
        public int Health { get; private set; }
        public int Phase { get; private set; } = 1;
        public double ProtectedUntil { get; private set; }
        public bool IsSuppressed => nonlethal && Health == 0;
        private readonly bool nonlethal;

        public FlightStoryGauge(int maximum, int phaseCount, bool isNonlethal)
        {
            if (maximum <= 0 || phaseCount < 1 || phaseCount > maximum) throw new ArgumentOutOfRangeException(nameof(maximum));
            Maximum = Health = maximum;
            PhaseCount = phaseCount;
            nonlethal = isNonlethal;
        }

        public void ProtectUntil(double time)
        {
            if (!double.IsNaN(time) && !double.IsInfinity(time)) ProtectedUntil = Math.Max(ProtectedUntil, time);
        }

        public FlightStoryHitResult Apply(int damage, double time, float multiplier = 1f)
        {
            if (Health == 0 || damage <= 0 || double.IsNaN(time) || double.IsInfinity(time) || time + 0.0000001d < ProtectedUntil || float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier <= 0f) return default;
            int amount = (int)Math.Min(int.MaxValue, Math.Round(damage * (double)multiplier, MidpointRounding.ToEven));
            if (amount <= 0) return default;
            int next = Math.Max(0, Health - amount);
            bool transition = false;
            if (Phase < PhaseCount)
            {
                int floor = (int)((long)Maximum * (PhaseCount - Phase) / PhaseCount);
                if (next <= floor)
                {
                    next = floor;
                    Phase++;
                    ProtectUntil(time + TransitionSeconds);
                    transition = true;
                }
            }
            bool changed = next != Health;
            Health = next;
            return new FlightStoryHitResult(changed, transition, changed && next == 0);
        }
    }

    public sealed class FlightStoryReservation
    {
        internal readonly FlightStoryBurstBudget Owner;
        public int Epoch { get; }
        public int Remaining { get; internal set; }
        internal FlightStoryReservation(FlightStoryBurstBudget owner, int epoch, int count)
        {
            Owner = owner;
            Epoch = epoch;
            Remaining = count;
        }
    }

    public sealed class FlightStoryBurstBudget
    {
        private readonly HashSet<FlightStoryReservation> reservations = new HashSet<FlightStoryReservation>();
        public int Capacity { get; }
        public int Limit { get; }
        public int Pending { get; private set; }
        public int Epoch { get; private set; }

        public FlightStoryBurstBudget(int capacity, int limit)
        {
            if (capacity <= 0 || limit <= 0 || limit > capacity) throw new ArgumentOutOfRangeException(nameof(limit));
            Capacity = capacity;
            Limit = limit;
        }

        public FlightStoryReservation TryReserve(int count, int activeCount)
        {
            if (count <= 0 || activeCount < 0 || (long)activeCount + Pending + count > Limit || (long)activeCount + Pending + count > Capacity) return null;
            var reservation = new FlightStoryReservation(this, Epoch, count);
            reservations.Add(reservation);
            Pending += count;
            return reservation;
        }

        public bool Commit(FlightStoryReservation reservation, int count) => Release(reservation, count);

        public bool Release(FlightStoryReservation reservation, int count)
        {
            if (reservation == null || reservation.Owner != this || reservation.Epoch != Epoch || count <= 0 || reservation.Remaining < count || !reservations.Contains(reservation)) return false;
            reservation.Remaining -= count;
            Pending -= count;
            if (reservation.Remaining == 0) reservations.Remove(reservation);
            return true;
        }

        public void Clear()
        {
            foreach (FlightStoryReservation reservation in reservations) reservation.Remaining = 0;
            reservations.Clear();
            Pending = 0;
            unchecked { Epoch++; }
        }
    }

    public enum FlightStoryAimAxis { Locked, ForwardCrossing, LocalCrossing }

    public enum FlightStoryAttackStage { Idle, Telegraph, Active, Recovery }

    public sealed class FlightStoryAttackClock
    {
        public FlightStoryAttackStage Stage { get; private set; }
        public int Burst { get; private set; }
        public double AttackStarted { get; private set; }
        public double RecoveryStarted { get; private set; }
        private double readyAt, activeUntil;
        private FlightStoryPatternProfile pattern;

        public void Start(FlightStoryPatternProfile profile, double now)
        {
            if (profile == null || double.IsNaN(now) || double.IsInfinity(now)) throw new ArgumentException(nameof(profile));
            pattern = profile;
            Burst = 0;
            AttackStarted = RecoveryStarted = 0d;
            readyAt = now + profile.Telegraph;
            activeUntil = 0d;
            Stage = FlightStoryAttackStage.Telegraph;
        }

        public bool IsReady(double now) => pattern != null && Burst < pattern.Bursts && (Stage == FlightStoryAttackStage.Telegraph || Stage == FlightStoryAttackStage.Active) && now + 0.0000001d >= readyAt;

        public bool AcceptBurst(double now)
        {
            if (!IsReady(now)) return false;
            if (Burst == 0) AttackStarted = now;
            activeUntil = Math.Max(activeUntil, now + Math.Max(0d, pattern.ActiveSeconds - Burst * (double)pattern.Interval));
            Burst++;
            readyAt = now + pattern.Interval;
            Stage = FlightStoryAttackStage.Active;
            return true;
        }

        public void RefuseBurst(double now)
        {
            if (!IsReady(now)) return;
            if (Burst > 0)
            {
                double delay = now + pattern.Telegraph - readyAt;
                AttackStarted += delay;
                activeUntil += delay;
            }
            readyAt = now + pattern.Telegraph;
            Stage = FlightStoryAttackStage.Telegraph;
        }

        public void Advance(double now)
        {
            if (pattern == null) return;
            if (Stage == FlightStoryAttackStage.Active && Burst == pattern.Bursts && now + 0.0000001d >= activeUntil)
            {
                Stage = FlightStoryAttackStage.Recovery;
                RecoveryStarted = activeUntil;
            }
            if (Stage == FlightStoryAttackStage.Recovery && now + 0.0000001d >= RecoveryStarted + pattern.RecoverySeconds) Stage = FlightStoryAttackStage.Idle;
        }

        public bool IsWeak(double now) => Stage == FlightStoryAttackStage.Recovery && now + 0.0000001d < RecoveryStarted + 1d;

        public void Delay(double seconds)
        {
            if (pattern == null || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0d) return;
            readyAt += seconds;
            activeUntil += seconds;
            AttackStarted += seconds;
            RecoveryStarted += seconds;
        }

        public void Stop()
        {
            Stage = FlightStoryAttackStage.Idle;
            pattern = null;
            Burst = 0;
        }
    }

    [Serializable]
    public sealed class FlightStoryShotPlan
    {
        public float MuzzleX, Yaw, Pitch, Roll, Cone, Speed, LaunchDelay, TurnDegrees, TurnSeconds, SplitAfter;
        public int Damage, Visual;
        public float ChildSpeed = 12f;
        public float[] ChildYaw = Array.Empty<float>();
        public FlightStoryAimAxis Axis;
        public int ReservationCount => 1 + ChildYaw.Length;
    }

    public static class FlightStoryPatternRules
    {
        public static IReadOnlyList<FlightStoryShotPlan> Build(FlightStoryPatternProfile pattern, int burst)
        {
            var shots = new List<FlightStoryShotPlan>();
            if (pattern == null || burst < 0 || burst >= pattern.Bursts) return shots;
            switch (pattern.Geometry)
            {
                case FlightStoryGeometry.Aimed:
                    shots.Add(Shot(pattern.Speed, pattern.Damage));
                    break;
                case FlightStoryGeometry.Fan:
                case FlightStoryGeometry.FanCharge:
                    Fan(shots, pattern, 0f);
                    break;
                case FlightStoryGeometry.AlternatingFan:
                    Fan(shots, pattern, burst % 2 == 0 ? -3f : 3f);
                    break;
                case FlightStoryGeometry.Ring:
                    Ring(shots, pattern);
                    break;
                case FlightStoryGeometry.CrossSpiral:
                    for (int side = 0; side < 2; side++)
                    {
                        for (int index = 0; index < 4; index++)
                        {
                            var shot = Shot(pattern.Speed, pattern.Damage);
                            shot.MuzzleX = side == 0 ? -6f : 6f;
                            shot.Cone = 8f;
                            shot.Roll = index * 90f + (side == 0 ? burst * 26f : 45f - burst * 26f);
                            shot.Axis = FlightStoryAimAxis.ForwardCrossing;
                            shot.Visual = 3;
                            shots.Add(shot);
                        }
                    }
                    break;
                case FlightStoryGeometry.SplitPair:
                case FlightStoryGeometry.SplitTriple:
                case FlightStoryGeometry.SplitWide:
                    int parents = pattern.Geometry == FlightStoryGeometry.SplitTriple ? 3 : 2;
                    for (int index = 0; index < parents; index++)
                    {
                        var shot = Shot(8f, pattern.Damage);
                        shot.SplitAfter = 0.8f;
                        shot.Visual = 1;
                        if (pattern.Geometry == FlightStoryGeometry.SplitPair)
                        {
                            shot.MuzzleX = -6f;
                            shot.ChildYaw = new[] { -16f, 0f, 16f };
                        }
                        else if (pattern.Geometry == FlightStoryGeometry.SplitTriple)
                        {
                            shot.Yaw = (index - 1) * 16f;
                            shot.ChildYaw = new[] { -12f, 0f, 12f };
                        }
                        else
                        {
                            shot.MuzzleX = index == 0 ? -6f : 6f;
                            shot.ChildYaw = new[] { -24f, -12f, 0f, 12f, 24f };
                        }
                        shots.Add(shot);
                    }
                    break;
                case FlightStoryGeometry.CrossLines:
                    Cross(shots, pattern, false);
                    break;
                case FlightStoryGeometry.RingCrossSequence:
                    if (burst == 1) Cross(shots, pattern, true);
                    else Ring(shots, pattern);
                    break;
            }
            return shots;
        }

        public static IReadOnlyList<FlightStoryShotPlan> BuildOrdinary(FlightStoryEnemyProfile profile)
        {
            var shots = new List<FlightStoryShotPlan>();
            if (profile == null) return shots;
            if (profile.Id == "E-CHARGE" || profile.Id == "E-ARCHIVE" || profile.Id == "H-ROCK" || profile.Id.StartsWith("B-", StringComparison.Ordinal)) return shots;
            if (profile.Id == "E-FAN" || profile.Id == "E-EAGLE")
            {
                for (int index = 0; index < 3; index++)
                {
                    var shot = Shot(profile.Speed, profile.Damage);
                    shot.Yaw = profile.Id == "E-FAN" ? (index - 1) * 20f : profile.DiagonalYaw[index];
                    shot.Pitch = profile.Id == "E-FAN" ? 0f : profile.DiagonalPitch[index];
                    shot.Visual = 1;
                    shots.Add(shot);
                }
            }
            else if (profile.Id == "E-EEL")
            {
                for (int index = 0; index < 2; index++)
                {
                    var shot = Shot(profile.Speed, profile.Damage);
                    shot.TurnDegrees = index == 0 ? -18f : 18f;
                    shot.TurnSeconds = 1f;
                    shots.Add(shot);
                }
            }
            else
            {
                var shot = Shot(profile.Speed, profile.Damage);
                if (profile.Id == "E-SWALLOW") shot.LaunchDelay = 1.2f;
                shots.Add(shot);
            }
            return shots;
        }

        private static FlightStoryShotPlan Shot(float speed, int damage) => new FlightStoryShotPlan { Speed = speed, Damage = damage };

        private static void Fan(List<FlightStoryShotPlan> shots, FlightStoryPatternProfile profile, float muzzle)
        {
            for (int index = 0; index < 7; index++)
            {
                var shot = Shot(profile.Speed, profile.Damage);
                shot.MuzzleX = muzzle;
                shot.Yaw = -32f + index * (64f / 6f);
                shot.Pitch = index % 2 == 0 ? -6f : 6f;
                shot.Visual = 1;
                shots.Add(shot);
            }
        }

        private static void Ring(List<FlightStoryShotPlan> shots, FlightStoryPatternProfile profile)
        {
            for (int index = 0; index < 12; index++)
            {
                var shot = Shot(profile.Speed, profile.Damage);
                shot.Cone = 24f;
                shot.Roll = index * 30f;
                shot.Visual = 2;
                shots.Add(shot);
            }
        }

        private static void Cross(List<FlightStoryShotPlan> shots, FlightStoryPatternProfile profile, bool spread)
        {
            for (int side = 0; side < 2; side++)
            {
                for (int index = 0; index < (spread ? 6 : 1); index++)
                {
                    var shot = Shot(profile.Speed, profile.Damage);
                    shot.MuzzleX = side == 0 ? -6f : 6f;
                    shot.Yaw = spread ? -10f + index * 4f : 0f;
                    shot.Axis = FlightStoryAimAxis.LocalCrossing;
                    shots.Add(shot);
                }
            }
        }
    }
}
