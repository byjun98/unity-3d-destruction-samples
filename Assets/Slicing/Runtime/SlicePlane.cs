using UnityEngine;

namespace Slicing
{
    public struct SlicePlane
    {
        public Vector3 Normal;
        public float Distance;

        public static SlicePlane FromNormalAndPoint(Vector3 worldNormal, Vector3 worldPoint)
        {
            worldNormal.Normalize();
            return new SlicePlane
            {
                Normal = worldNormal,
                Distance = Vector3.Dot(worldNormal, worldPoint)
            };
        }

        public SlicePlane ToLocal(Transform t)
        {
            Vector3 worldPointOnPlane = Normal * Distance;
            Vector3 localNormal = t.InverseTransformDirection(Normal).normalized;
            Vector3 localPoint = t.InverseTransformPoint(worldPointOnPlane);
            return new SlicePlane
            {
                Normal = localNormal,
                Distance = Vector3.Dot(localNormal, localPoint)
            };
        }

        public float SignedDistance(Vector3 p) => Vector3.Dot(Normal, p) - Distance;

        public bool LineIntersection(Vector3 a, Vector3 b, out float t)
        {
            Vector3 dir = b - a;
            float denom = Vector3.Dot(Normal, dir);
            if (Mathf.Abs(denom) < 1e-7f)
            {
                t = 0f;
                return false;
            }
            t = (Distance - Vector3.Dot(Normal, a)) / denom;
            return true;
        }
    }
}
