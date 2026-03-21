using UnityEngine;

namespace Slicing
{
    public struct SliceVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 Tangent;
        public Vector2 Uv;
        public Color Color;

        public static SliceVertex Lerp(SliceVertex a, SliceVertex b, float t)
        {
            return new SliceVertex
            {
                Position = Vector3.Lerp(a.Position, b.Position, t),
                Normal = Vector3.Slerp(a.Normal, b.Normal, t).normalized,
                Tangent = Vector4.Lerp(a.Tangent, b.Tangent, t),
                Uv = Vector2.Lerp(a.Uv, b.Uv, t),
                Color = Color.Lerp(a.Color, b.Color, t)
            };
        }
    }
}
