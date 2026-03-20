using UnityEngine;

public static class UrpMaterialFixer
{
    private static Shader urpUnlitParticleShader;
    private static Shader urpLitShader;
    private static bool resolved;

    private static void Resolve()
    {
        if (resolved)
        {
            return;
        }

        urpUnlitParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        resolved = true;
    }

    public static void FixHierarchy(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        Resolve();

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            FixRenderer(renderers[i]);
        }
    }

    private static void FixRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        Material[] sourceMaterials = renderer.sharedMaterials;

        if (sourceMaterials == null || sourceMaterials.Length == 0)
        {
            return;
        }

        bool changed = false;
        Material[] result = new Material[sourceMaterials.Length];

        for (int i = 0; i < sourceMaterials.Length; i++)
        {
            Material original = sourceMaterials[i];

            if (original == null)
            {
                result[i] = null;
                continue;
            }

            if (!IsLegacyShader(original.shader))
            {
                result[i] = original;
                continue;
            }

            bool isParticleLike = renderer is ParticleSystemRenderer || ShaderLooksLikeParticle(original.shader);
            Shader replacement = isParticleLike ? urpUnlitParticleShader : urpLitShader;

            if (replacement == null)
            {
                result[i] = original;
                continue;
            }

            Material clone = new Material(replacement);
            clone.name = original.name + "_URP";
            CopyMaterialProperties(original, clone, isParticleLike);
            result[i] = clone;
            changed = true;
        }

        if (changed)
        {
            renderer.sharedMaterials = result;
        }
    }

    private static bool IsLegacyShader(Shader shader)
    {
        if (shader == null)
        {
            return false;
        }

        string name = shader.name;

        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (name.StartsWith("Universal Render Pipeline/", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (name == "Hidden/InternalErrorShader")
        {
            return true;
        }

        if (name.StartsWith("Particles/", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (name.StartsWith("Mobile/Particles/", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (name.StartsWith("Legacy Shaders/", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (name == "Standard" || name == "Standard (Specular setup)")
        {
            return true;
        }

        if (name.StartsWith("Mobile/", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static bool ShaderLooksLikeParticle(Shader shader)
    {
        if (shader == null)
        {
            return false;
        }

        string n = shader.name.ToLowerInvariant();
        return n.Contains("particle") || n.Contains("additive") || n.Contains("alpha") || n.Contains("blend");
    }

    private static void CopyMaterialProperties(Material source, Material dest, bool particleLike)
    {
        Texture mainTex = null;

        if (source.HasProperty("_MainTex"))
        {
            mainTex = source.GetTexture("_MainTex");
        }
        else if (source.HasProperty("_BaseMap"))
        {
            mainTex = source.GetTexture("_BaseMap");
        }

        if (mainTex != null)
        {
            if (dest.HasProperty("_BaseMap"))
            {
                dest.SetTexture("_BaseMap", mainTex);
            }

            if (dest.HasProperty("_MainTex"))
            {
                dest.SetTexture("_MainTex", mainTex);
            }
        }

        Color color = Color.white;

        if (source.HasProperty("_TintColor"))
        {
            color = source.GetColor("_TintColor");
        }
        else if (source.HasProperty("_Color"))
        {
            color = source.GetColor("_Color");
        }
        else if (source.HasProperty("_BaseColor"))
        {
            color = source.GetColor("_BaseColor");
        }

        if (dest.HasProperty("_BaseColor"))
        {
            dest.SetColor("_BaseColor", color);
        }

        if (dest.HasProperty("_Color"))
        {
            dest.SetColor("_Color", color);
        }

        if (particleLike)
        {
            ConfigureUrpParticleAdditive(dest);
        }
    }

    private static void ConfigureUrpParticleAdditive(Material material)
    {
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 1f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", 0f);
        }

        material.renderQueue = 3000;
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_BLENDMODE_ADD");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
    }
}
