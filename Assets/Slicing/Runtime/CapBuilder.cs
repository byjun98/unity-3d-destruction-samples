using System.Collections.Generic;
using UnityEngine;

namespace Slicing
{
    internal struct CapEdge
    {
        public SliceVertex A;
        public SliceVertex B;
    }

    internal static class CapBuilder
    {
        // Builds a flat polygon cap on both halves at the slice plane and adds it as a new submesh.
        // Triangulation: fan from centroid. Works cleanly for convex cross-sections; on concave shapes
        // the cap may overlap, but stays watertight enough for impact-FX scale demos.
        public static void Build(SlicePlane plane, List<CapEdge> edges,
            SliceMeshBuilder upper, SliceMeshBuilder lower, int capSubmesh)
        {
            if (edges.Count == 0) return;

            Vector3 normal = plane.Normal;
            Vector3 centroid = Vector3.zero;
            int totalPoints = 0;
            for (int i = 0; i < edges.Count; i++)
            {
                centroid += edges[i].A.Position + edges[i].B.Position;
                totalPoints += 2;
            }
            centroid /= totalPoints;

            // Build a UV basis on the cut plane
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < 1e-5f)
                tangent = Vector3.Cross(normal, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
            Vector4 capTangent = new Vector4(tangent.x, tangent.y, tangent.z, -1f);

            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];

                // Upper cap: face -normal (cap is the floor of the upper half).
                AddCapTriangle(upper, capSubmesh, centroid, e.A.Position, e.B.Position,
                    -normal, capTangent, tangent, bitangent, centroid, true);

                // Lower cap: face +normal (cap is the ceiling of the lower half).
                // Reverse winding to flip facing.
                AddCapTriangle(lower, capSubmesh, centroid, e.B.Position, e.A.Position,
                    normal, capTangent, tangent, bitangent, centroid, false);
            }
        }

        static void AddCapTriangle(SliceMeshBuilder b, int submesh,
            Vector3 p0, Vector3 p1, Vector3 p2,
            Vector3 capNormal, Vector4 capTangent,
            Vector3 uTangent, Vector3 vBitangent, Vector3 origin,
            bool flipUv)
        {
            int i0 = b.Add(MakeCapVertex(p0, capNormal, capTangent, uTangent, vBitangent, origin, flipUv));
            int i1 = b.Add(MakeCapVertex(p1, capNormal, capTangent, uTangent, vBitangent, origin, flipUv));
            int i2 = b.Add(MakeCapVertex(p2, capNormal, capTangent, uTangent, vBitangent, origin, flipUv));
            b.AddTriangle(submesh, i0, i1, i2);
        }

        static SliceVertex MakeCapVertex(Vector3 pos,
            Vector3 capNormal, Vector4 capTangent,
            Vector3 uTangent, Vector3 vBitangent, Vector3 origin, bool flipUv)
        {
            Vector3 d = pos - origin;
            float u = Vector3.Dot(d, uTangent);
            float v = Vector3.Dot(d, vBitangent);
            if (flipUv) u = -u;

            return new SliceVertex
            {
                Position = pos,
                Normal = capNormal,
                Tangent = capTangent,
                Uv = new Vector2(u * 0.5f + 0.5f, v * 0.5f + 0.5f),
                Color = Color.white
            };
        }
    }
}
