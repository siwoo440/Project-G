using System;
using System.Collections.Generic;

namespace ProjectG
{
    [Serializable]
    public struct FlightStoryPoint
    {
        public double X;
        public double Y;
        public double Z;

        public FlightStoryPoint(double x, double y, double z) { X = x; Y = y; Z = z; }
        internal bool IsFinite => FlightStoryValue.Finite(X) && FlightStoryValue.Finite(Y) && FlightStoryValue.Finite(Z);
    }

    public static class FlightStorySweep
    {
        /// <summary>Earliest intersection of a finite segment with an expanded protection sphere.</summary>
        public static bool TrySphere(FlightStoryPoint start, FlightStoryPoint end, FlightStoryPoint center, double radius, out double fraction)
        {
            fraction = 0d;
            if (!start.IsFinite || !end.IsFinite || !center.IsFinite || !FlightStoryValue.Finite(radius) || radius < 0d) return false;
            double x = start.X - center.X, y = start.Y - center.Y, z = start.Z - center.Z;
            double dx = end.X - start.X, dy = end.Y - start.Y, dz = end.Z - start.Z;
            double c = x * x + y * y + z * z - radius * radius;
            double a = dx * dx + dy * dy + dz * dz;
            if (!FlightStoryValue.Finite(c) || !FlightStoryValue.Finite(a)) return false;
            if (c <= 0d) return true;
            if (a <= 0d) return false;
            double b = x * dx + y * dy + z * dz;
            double discriminant = b * b - a * c;
            if (!FlightStoryValue.Finite(discriminant) || discriminant < 0d) return false;
            double entry = (-b - Math.Sqrt(discriminant)) / a;
            if (!FlightStoryValue.Finite(entry) || entry < 0d || entry > 1d) return false;
            fraction = entry;
            return true;
        }
    }

    [Serializable]
    public struct FlightStoryManifestEntry
    {
        public string Id;
        public string GroupId;
        public string DestinationId;
    }

    [Serializable]
    public struct FlightStoryManifestState
    {
        public FlightStoryManifestEntry Entry;
        public bool Departed;
        public bool Arrived;
    }

    /// <summary>Approved identities never disappear; discovery reopens the seal without moving safe people.</summary>
    public sealed class FlightStoryManifest
    {
        private readonly Dictionary<string, FlightStoryManifestState> people = new Dictionary<string, FlightStoryManifestState>(StringComparer.Ordinal);
        private readonly List<string> order = new List<string>();

        public FlightStoryManifest(string runId, IEnumerable<FlightStoryManifestEntry> expected)
        {
            if (!FlightStoryValue.Id(runId)) throw new ArgumentException("A sortie ID is required.", nameof(runId));
            RunId = runId;
            if (expected == null) return;
            foreach (FlightStoryManifestEntry entry in expected)
                if (!AddDiscovered(entry)) throw new ArgumentException("Manifest identities, groups and destinations must be valid and unique.", nameof(expected));
        }

        public string RunId { get; }
        public bool IsSealed { get; private set; }
        public int ExpectedCount => people.Count;
        public int UnconfirmedCount
        {
            get
            {
                int result = 0;
                foreach (FlightStoryManifestState state in people.Values) if (!state.Arrived) result++;
                return result;
            }
        }
        public bool AllArrived => IsSealed && people.Count > 0 && UnconfirmedCount == 0;

        public bool AddDiscovered(FlightStoryManifestEntry entry)
        {
            if (!FlightStoryValue.Id(entry.Id) || !FlightStoryValue.Id(entry.GroupId) || !FlightStoryValue.Id(entry.DestinationId) || people.ContainsKey(entry.Id)) return false;
            people.Add(entry.Id, new FlightStoryManifestState { Entry = entry });
            order.Add(entry.Id);
            IsSealed = false;
            return true;
        }

        public bool Seal()
        {
            if (people.Count == 0) return false;
            IsSealed = true;
            return true;
        }

        public bool ConfirmDeparture(string runId, string identity)
        {
            if (runId != RunId || !FlightStoryValue.Id(identity) || !people.TryGetValue(identity, out FlightStoryManifestState state)) return false;
            state.Departed = true;
            people[identity] = state;
            return true;
        }

        public bool ConfirmArrival(string runId, string identity, string destinationId)
        {
            if (runId != RunId || !FlightStoryValue.Id(identity) || !people.TryGetValue(identity, out FlightStoryManifestState state) || !state.Departed || state.Entry.DestinationId != destinationId) return false;
            state.Arrived = true;
            people[identity] = state;
            return true;
        }

        public bool HasArrived(string identity) => FlightStoryValue.Id(identity) && people.TryGetValue(identity, out FlightStoryManifestState state) && state.Arrived;

        public FlightStoryManifestState[] Snapshot()
        {
            var result = new FlightStoryManifestState[order.Count];
            for (int i = 0; i < order.Count; i++) result[i] = people[order[i]];
            return result;
        }
    }

    public sealed class FlightStoryProtectedTarget
    {
        private readonly HashSet<string> seenHits = new HashSet<string>(StringComparer.Ordinal);
        private double lastHit = double.NegativeInfinity;
        private double lastObservedTime;

        public FlightStoryProtectedTarget(string runId, string id, double maximum)
        {
            if (!FlightStoryValue.Id(runId)) throw new ArgumentException("A sortie ID is required.", nameof(runId));
            if (!FlightStoryValue.Id(id)) throw new ArgumentException("A protected-target ID is required.", nameof(id));
            if (!FlightStoryValue.Finite(maximum) || maximum <= 0d) throw new ArgumentOutOfRangeException(nameof(maximum));
            RunId = runId;
            Id = id;
            Maximum = maximum;
            Durability = maximum;
        }

        public string RunId { get; }
        public string Id { get; }
        public double Maximum { get; }
        public double Durability { get; private set; }
        public bool IsFailed => Durability <= 0d;

        public bool TryHit(string runId, string eventId, double damage, double now, bool paused = false)
        {
            if (paused || runId != RunId || !FlightStoryValue.Id(eventId) || !FlightStoryValue.Finite(damage) || damage <= 0d || !FlightStoryValue.Finite(now) || now < lastObservedTime || IsFailed) return false;
            lastObservedTime = now;
            if (!seenHits.Add(eventId) || now - lastHit < 1d) return false;
            lastHit = now;
            Durability = Math.Max(0d, Durability - damage);
            return true;
        }
    }

    /// <summary>Work retains completed effort; a continuous hold loses its window when its live condition breaks.</summary>
    public sealed class FlightStoryProgress
    {
        private readonly bool continuous;

        public FlightStoryProgress(double requiredSeconds, bool continuous)
        {
            if (!FlightStoryValue.Finite(requiredSeconds) || requiredSeconds <= 0d) throw new ArgumentOutOfRangeException(nameof(requiredSeconds));
            RequiredSeconds = requiredSeconds;
            this.continuous = continuous;
        }

        public double RequiredSeconds { get; }
        public double Seconds { get; private set; }
        public bool Complete => Seconds >= RequiredSeconds;

        public bool Step(double delta, bool conditionsMet, bool paused = false)
        {
            if (paused || !FlightStoryValue.Finite(delta) || delta < 0d) return false;
            if (!conditionsMet)
            {
                if (continuous) Seconds = 0d;
                return false;
            }
            Seconds = Math.Min(RequiredSeconds, Seconds + delta);
            return true;
        }
    }
}
