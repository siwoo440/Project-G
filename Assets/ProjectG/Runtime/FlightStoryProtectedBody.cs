using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectG
{
    public sealed class FlightStoryProtectedBody : MonoBehaviour
    {
        [SerializeField] private string targetId;
        [SerializeField] private float radius;
        [SerializeField] private Vector3 localCenter;
        private static readonly HashSet<FlightStoryProtectedBody> activeBodies = new HashSet<FlightStoryProtectedBody>();
        private FlightStoryDirector director;
        private FlightStoryProtectedTarget model;
        private string runId;

        public string TargetId => targetId;
        public float Radius => radius;
        public Vector3 LocalCenter => localCenter;
        public string BoundRunId => runId;

        public void Configure(string identity, float bodyRadius, Vector3 center)
        {
            targetId = identity;
            radius = bodyRadius;
            localCenter = center;
            director = null;
            model = null;
            runId = null;
        }

        public string ValidateConfiguration()
        {
            if (!FlightStoryValue.Id(targetId)) return "A protected body needs an approved target ID.";
            if (!FlightStoryValue.Finite(radius) || radius <= 0f || !FlightStorySignals.Finite(localCenter)) return "A protected body needs finite authored geometry and a positive radius.";
            return string.Empty;
        }

        public bool Bind(FlightStoryDirector owner)
        {
            director = null;
            model = null;
            runId = null;
            if (owner == null || owner.Attempt == null || ValidateConfiguration().Length != 0) return false;
            FlightStoryProtectedTarget target = owner.Protection(targetId);
            if (target == null || target.RunId != owner.Attempt.RunId) return false;
            director = owner;
            model = target;
            runId = owner.Attempt.RunId;
            return true;
        }

        public bool TryDamage(float damage, string eventId, bool paused = false)
        {
            if (paused || Time.timeScale <= 0f || !BoundCurrent(director)) return false;
            return model.TryHit(runId, eventId, damage, director.DamageTime);
        }

        /// <summary>A geometric hit consumes a shot even when this target's one-second damage interval is active.</summary>
        public static bool TryIntercept(FlightStoryDirector owner, Vector3 start, Vector3 end, float shotRadius, float damage, string eventId, bool paused = false)
        {
            if (paused || Time.timeScale <= 0f || owner == null || owner.Attempt == null || !owner.Attempt.CombatStarted || !FlightStorySignals.Current(owner, owner.Attempt.RunId) || !FlightStorySignals.Finite(start) || !FlightStorySignals.Finite(end) || !FlightStoryValue.Finite(shotRadius) || shotRadius < 0f || !FlightStoryValue.Finite(damage) || damage <= 0f || !FlightStoryValue.Id(eventId)) return false;
            FlightStoryProtectedBody nearest = null;
            double first = double.PositiveInfinity;
            var a = new FlightStoryPoint(start.x, start.y, start.z);
            var b = new FlightStoryPoint(end.x, end.y, end.z);
            foreach (FlightStoryProtectedBody body in activeBodies)
            {
                if (body == null || !body.BoundCurrent(owner)) continue;
                Vector3 center = body.transform.TransformPoint(body.localCenter);
                Vector3 scale = body.transform.lossyScale;
                double expanded = body.radius * Math.Max(Math.Abs(scale.x), Math.Max(Math.Abs(scale.y), Math.Abs(scale.z))) + shotRadius;
                if (!FlightStorySignals.Finite(center) || !FlightStoryValue.Finite(expanded) || expanded <= 0d) continue;
                if (!FlightStorySweep.TrySphere(a, b, new FlightStoryPoint(center.x, center.y, center.z), expanded, out double entry)) continue;
                if (entry < first || (entry == first && nearest != null && StringComparer.Ordinal.Compare(body.TargetId, nearest.TargetId) < 0))
                {
                    first = entry;
                    nearest = body;
                }
            }
            if (nearest == null) return false;
            nearest.TryDamage(damage, eventId);
            return true;
        }

        private bool BoundCurrent(FlightStoryDirector owner)
            => isActiveAndEnabled && director == owner && model != null && model.RunId == runId && FlightStorySignals.Current(owner, runId) && owner.IsProtectionActive(targetId);

        private void OnEnable() { activeBodies.Add(this); }
        private void OnDisable() { activeBodies.Remove(this); }
        private void OnDestroy() { activeBodies.Remove(this); }
    }
}
