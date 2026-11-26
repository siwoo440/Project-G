using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectG
{
    public static class FlightStorySignals
    {
        public static string[] ValidateHierarchy(Transform root)
        {
            if (root == null) return new[] { "A story authoring root is required." };
            var errors = new List<string>();
            var evidence = new HashSet<string>(StringComparer.Ordinal);
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (FlightStoryEvidenceSignal signal in root.GetComponentsInChildren<FlightStoryEvidenceSignal>(true))
            {
                Add(errors, signal.ValidateConfiguration());
                if (!evidence.Add(signal.SceneId + "\n" + signal.FactId)) errors.Add("Duplicate scene evidence: " + signal.SceneId + " / " + signal.FactId);
            }
            foreach (FlightStoryRosterMember member in root.GetComponentsInChildren<FlightStoryRosterMember>(true))
            {
                Add(errors, member.ValidateConfiguration());
                if (!identities.Add(member.EntityId)) errors.Add("Duplicate roster identity: " + member.EntityId);
            }
            foreach (FlightStoryArrivalGate gate in root.GetComponentsInChildren<FlightStoryArrivalGate>(true)) Add(errors, gate.ValidateConfiguration());
            foreach (FlightStoryProtectedBody body in root.GetComponentsInChildren<FlightStoryProtectedBody>(true))
            {
                Add(errors, body.ValidateConfiguration());
                if (!targets.Add(body.TargetId)) errors.Add("Duplicate protected-target identity: " + body.TargetId);
            }
            return errors.ToArray();
        }

        internal static bool Current(FlightStoryDirector director, string runId)
        {
            if (director == null || director.Attempt == null || string.IsNullOrEmpty(runId) || director.Attempt.RunId != runId) return false;
            return director.Attempt.Status == FlightStoryRunStatus.Prepared || director.Attempt.Status == FlightStoryRunStatus.Active;
        }

        internal static bool Finite(Vector3 value) => FlightStoryValue.Finite(value.x) && FlightStoryValue.Finite(value.y) && FlightStoryValue.Finite(value.z);
        private static void Add(List<string> errors, string issue) { if (!string.IsNullOrEmpty(issue)) errors.Add(issue); }
    }
}
