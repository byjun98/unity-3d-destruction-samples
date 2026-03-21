using System.Collections.Generic;
using UnityEngine;

namespace Slicing
{
    public static class MeshSlicer
    {
        const float Epsilon = 1e-5f;

        public static SliceResult Slice(MeshFilter source, MeshRenderer renderer, SlicePlane worldPlane, Material capMaterial)
        {
            if (source == null || source.sharedMesh == null) return null;
            var mesh = source.sharedMesh;
            var localPlane = worldPlane.ToLocal(source.transform);

            var verts = mesh.vertices;
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            var uvs = mesh.uv;
            var colors = mesh.colors;

            bool hasNormals = normals != null && normals.Length == verts.Length;
            bool hasTangents = tangents != null && tangents.Length == verts.Length;
            bool hasUvs = uvs != null && uvs.Length == verts.Length;
            bool hasColors = colors != null && colors.Length == verts.Length;

            int submeshCount = mesh.subMeshCount;
            int capSubmesh = submeshCount;

            var upper = new SliceMeshBuilder();
            var lower = new SliceMeshBuilder();
            var capEdges = new List<CapEdge>(256);

            for (int s = 0; s < submeshCount; s++)
            {
                var tris = mesh.GetTriangles(s);
                for (int i = 0; i < tris.Length; i += 3)
                {
                    int i0 = tris[i + 0];
                    int i1 = tris[i + 1];
                    int i2 = tris[i + 2];

                    SliceVertex v0 = MakeVertex(verts, normals, tangents, uvs, colors, hasNormals, hasTangents, hasUvs, hasColors, i0);
                    SliceVertex v1 = MakeVertex(verts, normals, tangents, uvs, colors, hasNormals, hasTangents, hasUvs, hasColors, i1);
                    SliceVertex v2 = MakeVertex(verts, normals, tangents, uvs, colors, hasNormals, hasTangents, hasUvs, hasColors, i2);

                    SliceTriangle(localPlane, s, v0, v1, v2, upper, lower, capEdges);
                }
            }

            if (upper.IsEmpty() || lower.IsEmpty())
                return null;

            CapBuilder.Build(localPlane, capEdges, upper, lower, capSubmesh);

            var result = new SliceResult
            {
                UpperMesh = upper.ToMesh(mesh.name + "_Upper"),
                LowerMesh = lower.ToMesh(mesh.name + "_Lower"),
                CutNormal = worldPlane.Normal,
                CutCenter = worldPlane.Normal * worldPlane.Distance
            };

            Material[] sourceMats = renderer != null ? renderer.sharedMaterials : new Material[submeshCount];
            result.UpperMaterials = BuildMaterialArray(sourceMats, submeshCount, capMaterial);
            result.LowerMaterials = BuildMaterialArray(sourceMats, submeshCount, capMaterial);

            return result;
        }

        static Material[] BuildMaterialArray(Material[] src, int submeshCount, Material capMat)
        {
            var arr = new Material[submeshCount + 1];
            for (int i = 0; i < submeshCount; i++)
                arr[i] = (src != null && i < src.Length) ? src[i] : null;
            arr[submeshCount] = capMat;
            return arr;
        }

        static SliceVertex MakeVertex(Vector3[] verts, Vector3[] normals, Vector4[] tangents, Vector2[] uvs, Color[] colors,
            bool hasN, bool hasT, bool hasUv, bool hasC, int idx)
        {
            return new SliceVertex
            {
                Position = verts[idx],
                Normal = hasN ? normals[idx] : Vector3.up,
                Tangent = hasT ? tangents[idx] : new Vector4(1, 0, 0, -1),
                Uv = hasUv ? uvs[idx] : Vector2.zero,
                Color = hasC ? colors[idx] : Color.white
            };
        }

        static void SliceTriangle(SlicePlane plane, int submesh,
            SliceVertex v0, SliceVertex v1, SliceVertex v2,
            SliceMeshBuilder upper, SliceMeshBuilder lower, List<CapEdge> capEdges)
        {
            float d0 = plane.SignedDistance(v0.Position);
            float d1 = plane.SignedDistance(v1.Position);
            float d2 = plane.SignedDistance(v2.Position);

            int s0 = SideOf(d0);
            int s1 = SideOf(d1);
            int s2 = SideOf(d2);

            if (s0 == s1 && s1 == s2)
            {
                var dst = s0 > 0 ? upper : lower;
                AddTriangle(dst, submesh, v0, v1, v2);
                return;
            }

            // Cut. Find which vertex is alone on one side.
            // Order the triangle so v_alone has different sign from v_a, v_b (which share a side).
            SliceVertex va, vb, vc;
            float da, db, dc;
            int sa, sb, sc;

            if (s0 != s1 && s0 != s2)
            {
                va = v0; vb = v1; vc = v2;
                da = d0; db = d1; dc = d2;
                sa = s0; sb = s1; sc = s2;
            }
            else if (s1 != s0 && s1 != s2)
            {
                va = v1; vb = v2; vc = v0;
                da = d1; db = d2; dc = d0;
                sa = s1; sb = s2; sc = s0;
            }
            else
            {
                va = v2; vb = v0; vc = v1;
                da = d2; db = d0; dc = d1;
                sa = s2; sb = s0; sc = s1;
            }

            // va is alone (sign sa). vb and vc are on the opposite side.
            // Edge va->vb crosses plane. Edge va->vc crosses plane.
            float tAB = da / (da - db);
            float tAC = da / (da - dc);
            SliceVertex iAB = SliceVertex.Lerp(va, vb, tAB);
            SliceVertex iAC = SliceVertex.Lerp(va, vc, tAC);

            // alone-side gets one triangle: va, iAB, iAC (preserve winding)
            // shared-side gets quad: iAB, vb, vc, iAC -> two tris
            SliceMeshBuilder aloneSide = sa > 0 ? upper : lower;
            SliceMeshBuilder otherSide = sa > 0 ? lower : upper;

            AddTriangle(aloneSide, submesh, va, iAB, iAC);
            AddTriangle(otherSide, submesh, iAB, vb, vc);
            AddTriangle(otherSide, submesh, iAB, vc, iAC);

            // Cap edge. The intersection segment is iAB -> iAC.
            // From the upper side perspective, the cap polygon should wind so that its normal
            // points along -plane.Normal (cap fills the hole on the upper piece, facing down into the cut).
            // We collect undirected edges; CapBuilder handles winding.
            capEdges.Add(new CapEdge { A = iAB, B = iAC });
        }

        static int SideOf(float d) => d >= -Epsilon ? 1 : -1;

        static void AddTriangle(SliceMeshBuilder b, int submesh, SliceVertex v0, SliceVertex v1, SliceVertex v2)
        {
            int i0 = b.Add(v0);
            int i1 = b.Add(v1);
            int i2 = b.Add(v2);
            b.AddTriangle(submesh, i0, i1, i2);
        }
    }
}
