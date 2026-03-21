using System.Collections.Generic;
using UnityEngine;

namespace Slicing
{
    internal sealed class SliceMeshBuilder
    {
        private readonly List<SliceVertex> _verts = new List<SliceVertex>(2048);
        private readonly Dictionary<int, List<int>> _submeshTris = new Dictionary<int, List<int>>(8);

        public int VertexCount => _verts.Count;

        public int Add(SliceVertex v)
        {
            _verts.Add(v);
            return _verts.Count - 1;
        }

        public void AddTriangle(int submesh, int a, int b, int c)
        {
            if (!_submeshTris.TryGetValue(submesh, out var list))
            {
                list = new List<int>(1024);
                _submeshTris[submesh] = list;
            }
            list.Add(a);
            list.Add(b);
            list.Add(c);
        }

        public bool IsEmpty()
        {
            if (_verts.Count == 0) return true;
            foreach (var kv in _submeshTris)
                if (kv.Value.Count > 0) return false;
            return true;
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };

            int n = _verts.Count;
            if (n > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            var positions = new Vector3[n];
            var normals = new Vector3[n];
            var tangents = new Vector4[n];
            var uvs = new Vector2[n];
            var colors = new Color[n];

            for (int i = 0; i < n; i++)
            {
                var v = _verts[i];
                positions[i] = v.Position;
                normals[i] = v.Normal;
                tangents[i] = v.Tangent;
                uvs[i] = v.Uv;
                colors[i] = v.Color;
            }

            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.tangents = tangents;
            mesh.uv = uvs;
            mesh.colors = colors;

            int maxSubmesh = -1;
            foreach (var kv in _submeshTris)
                if (kv.Key > maxSubmesh) maxSubmesh = kv.Key;

            int submeshCount = maxSubmesh + 1;
            if (submeshCount <= 0) submeshCount = 1;
            mesh.subMeshCount = submeshCount;

            for (int s = 0; s < submeshCount; s++)
            {
                if (_submeshTris.TryGetValue(s, out var list) && list.Count > 0)
                    mesh.SetTriangles(list, s, false);
                else
                    mesh.SetTriangles(System.Array.Empty<int>(), s, false);
            }

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
