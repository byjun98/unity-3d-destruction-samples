using UnityEngine;

public static class StressUrpMaterialFixer
{
    private static Shader cachedUnlitShader;
    private static Shader cachedAdditiveShader;
    private static bool resolved;

    private static void Resolve()
    {
        if (resolved)
        {
            return;
        }

        resolved = true;

        UnityEngine.Rendering.RenderPipelineAsset activePipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;

        if (!IsUniversalRenderPipeline(activePipeline))
        {
            return;
        }

        cachedUnlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        cachedAdditiveShader = cachedUnlitShader;
    }

    private static bool IsUniversalRenderPipeline(UnityEngine.Rendering.RenderPipelineAsset pipeline)
    {
        return pipeline != null
            && pipeline.GetType().FullName.IndexOf("UnityEngine.Rendering.Universal", System.StringComparison.Ordinal) >= 0;
    }

    public static void Fix(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Resolve();

        if (cachedUnlitShader == null)
        {
            return;
        }

        ParticleSystemRenderer[] particleRenderers = root.GetComponentsInChildren<ParticleSystemRenderer>(true);

        for (int i = 0; i < particleRenderers.Length; i++)
        {
            FixRenderer(particleRenderers[i]);
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];

            if (r is ParticleSystemRenderer)
            {
                continue;
            }

            FixRenderer(r);
        }
    }

    private static void FixRenderer(Renderer r)
    {
        if (r == null)
        {
            return;
        }

        Material[] mats = r.sharedMaterials;

        if (mats == null || mats.Length == 0)
        {
            return;
        }

        bool changed = false;
        Material[] copy = new Material[mats.Length];

        for (int i = 0; i < mats.Length; i++)
        {
            Material m = mats[i];

            if (m == null || m.shader == null)
            {
                copy[i] = m;
                continue;
            }

            string name = m.shader.name;

            if (NeedsUpgrade(name))
            {
                Material upgraded = new Material(cachedUnlitShader);
                upgraded.hideFlags = HideFlags.HideAndDontSave;
                CopyTexturesAndColors(m, upgraded);
                ApplyAdditiveBlendIfNeeded(name, upgraded);
                copy[i] = upgraded;
                changed = true;
            }
            else
            {
                copy[i] = m;
            }
        }

        if (changed)
        {
            r.sharedMaterials = copy;
        }
    }

    private static bool NeedsUpgrade(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName))
        {
            return false;
        }

        if (shaderName.IndexOf("Universal Render Pipeline", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        if (shaderName == "Hidden/InternalErrorShader")
        {
            return true;
        }

        if (shaderName.StartsWith("Particles/", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (shaderName.StartsWith("Legacy Shaders/Particles/", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (shaderName.StartsWith("Mobile/Particles/", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (shaderName == "Standard" || shaderName == "Standard (Specular setup)")
        {
            return true;
        }

        return false;
    }

    private static void CopyTexturesAndColors(Material from, Material to)
    {
        Texture mainTex = null;

        if (from.HasProperty("_MainTex"))
        {
            mainTex = from.GetTexture("_MainTex");
        }

        if (mainTex == null && from.HasProperty("_BaseMap"))
        {
            mainTex = from.GetTexture("_BaseMap");
        }

        if (to.HasProperty("_BaseMap"))
        {
            to.SetTexture("_BaseMap", mainTex);
        }

        if (to.HasProperty("_MainTex"))
        {
            to.SetTexture("_MainTex", mainTex);
        }

        Color color = Color.white;

        if (from.HasProperty("_TintColor"))
        {
            color = from.GetColor("_TintColor");
        }
        else if (from.HasProperty("_Color"))
        {
            color = from.GetColor("_Color");
        }
        else if (from.HasProperty("_BaseColor"))
        {
            color = from.GetColor("_BaseColor");
        }

        if (to.HasProperty("_BaseColor"))
        {
            to.SetColor("_BaseColor", color);
        }

        if (to.HasProperty("_Color"))
        {
            to.SetColor("_Color", color);
        }
    }

    private static void ApplyAdditiveBlendIfNeeded(string originalShaderName, Material to)
    {
        bool additive = originalShaderName.IndexOf("Additive", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool blended = originalShaderName.IndexOf("Alpha Blended", System.StringComparison.OrdinalIgnoreCase) >= 0
            || originalShaderName.IndexOf("AlphaBlended", System.StringComparison.OrdinalIgnoreCase) >= 0
            || originalShaderName.IndexOf("Alpha", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (to.HasProperty("_Surface"))
        {
            to.SetFloat("_Surface", 1f);
        }

        to.SetOverrideTag("RenderType", "Transparent");
        to.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        to.DisableKeyword("_ALPHATEST_ON");

        if (additive)
        {
            if (to.HasProperty("_Blend"))
            {
                to.SetFloat("_Blend", 1f);
            }

            if (to.HasProperty("_SrcBlend"))
            {
                to.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (to.HasProperty("_DstBlend"))
            {
                to.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            }

            to.EnableKeyword("_ALPHABLEND_ON");
            to.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            to.renderQueue = 3050;
        }
        else
        {
            if (to.HasProperty("_Blend"))
            {
                to.SetFloat("_Blend", 0f);
            }

            if (to.HasProperty("_SrcBlend"))
            {
                to.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (to.HasProperty("_DstBlend"))
            {
                to.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            to.EnableKeyword("_ALPHABLEND_ON");
            to.renderQueue = 3000;
        }

        if (to.HasProperty("_ZWrite"))
        {
            to.SetFloat("_ZWrite", 0f);
        }

        if (to.HasProperty("_Cull"))
        {
            to.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        }
    }
}
