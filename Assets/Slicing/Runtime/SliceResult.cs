using UnityEngine;

namespace Slicing
{
    public sealed class SliceResult
    {
        public Mesh UpperMesh;
        public Mesh LowerMesh;
        public Material[] UpperMaterials;
        public Material[] LowerMaterials;
        public Vector3 CutCenter;
        public Vector3 CutNormal;

        public bool DidSlice => UpperMesh != null && LowerMesh != null;
    }
}
