using UnityEngine;

namespace ProjectG
{
    public static class FlightStorySpatial
    {
        // Authoring supports actual spheres and oriented boxes, never their larger world AABBs.
        public static bool ContainsSphere(Collider volume, Vector3 center, float radius)
        {
            if (volume == null || !volume.enabled || !volume.gameObject.activeInHierarchy || radius < 0 || float.IsNaN(radius) || float.IsInfinity(radius)) return false;
            Vector3 scale = Abs(volume.transform.lossyScale);
            if (volume is SphereCollider sphere)
            {
                float available = sphere.radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z)) - radius;
                return available >= 0 && (center - sphere.transform.TransformPoint(sphere.center)).sqrMagnitude <= available * available;
            }
            if (volume is BoxCollider box)
            {
                Vector3 half = Vector3.Scale(box.size * 0.5f, scale) - Vector3.one * radius;
                if (half.x < 0 || half.y < 0 || half.z < 0) return false;
                Vector3 local = Quaternion.Inverse(box.transform.rotation) * (center - box.transform.TransformPoint(box.center));
                return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
            }
            return false;
        }

        public static bool OverlapsSphere(Collider volume, Vector3 center, float radius)
        {
            if (volume == null || !volume.enabled || !volume.gameObject.activeInHierarchy) return true;
            return (volume.ClosestPoint(center) - center).sqrMagnitude <= radius * radius;
        }

        private static Vector3 Abs(Vector3 value) => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }
}
