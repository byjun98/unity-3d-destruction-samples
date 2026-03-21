using UnityEngine;

namespace Slicing
{
    // Centralized shader lookup that survives both URP and Built-in render pipelines.
    // When URP isn't actually active (RenderPipelineAsset is None in GraphicsSettings),
    // URP shaders render magenta — so we detect that and fall back to Standard / built-in.
    public static class SliceShaders
    {
        static bool? _hasUrpCached;

        public static bool HasUrp
        {
            get
            {
                if (_hasUrpCached.HasValue) return _hasUrpCached.Value;
                var rp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                _hasUrpCached = rp != null && rp.GetType().FullName.Contains("Universal");
                return _hasUrpCached.Value;
            }
        }

        public static Shader Lit()
        {
            if (HasUrp)
            {
                var u = Shader.Find("Universal Render Pipeline/Lit");
                if (u != null) return u;
            }
            return Shader.Find("Standard") ?? Shader.Find("Diffuse");
        }

        public static Shader Unlit()
        {
            if (HasUrp)
            {
                var u = Shader.Find("Universal Render Pipeline/Unlit");
                if (u != null) return u;
            }
            return Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
        }

        public static Shader ParticleUnlit()
        {
            if (HasUrp)
            {
                var u = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (u != null) return u;
            }
            return Shader.Find("Particles/Standard Unlit")
                   ?? Shader.Find("Mobile/Particles/Alpha Blended")
                   ?? Shader.Find("Sprites/Default");
        }

        // Apply a base color regardless of which shader was chosen.
        public static void SetTint(Material mat, Color color)
        {
            if (mat == null) return;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", color);
        }

        public static void SetEmission(Material mat, Color color)
        {
            if (mat == null) return;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.AnyEmissive;
            }
        }

        public static void SetMetallicSmoothness(Material mat, float metallic, float smoothness)
        {
            if (mat == null) return;
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            else if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
        }
    }
}
