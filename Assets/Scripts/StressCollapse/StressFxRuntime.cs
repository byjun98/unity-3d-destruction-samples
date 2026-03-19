using UnityEngine;

public static class StressFxRuntime
{
    private static Material cachedDustMaterial;
    private static Material cachedSparkMaterial;
    private static Texture2D cachedDustTex;
    private static Texture2D cachedSparkTex;

    private static ParticleSystem.MinMaxCurve Curve(float min, float max)
    {
        return new ParticleSystem.MinMaxCurve(min, max);
    }

    private static ParticleSystem CreatePS(string name, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    public static GameObject SpawnDustBurst(Vector3 position, float scale, float lifetime)
    {
        ParticleSystem ps = CreatePS("StressFx_DustBurst", position);

        var main = ps.main;
        main.duration = 0.6f;
        main.loop = false;
        main.startLifetime = Curve(1.4f, 2.4f);
        main.startSpeed = Curve(2.4f * scale, 4.6f * scale);
        main.startSize = Curve(0.9f * scale, 2.0f * scale);
        main.startRotation = Curve(0f, 6.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.78f, 0.74f, 0.66f, 1f), new Color(0.55f, 0.52f, 0.48f, 1f));
        main.gravityModifier = 0.04f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 220;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 70 + Mathf.RoundToInt(40f * scale)) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.6f * scale;

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = Curve(0f, 0f);
        velocityOverLifetime.y = Curve(1.2f * scale, 2.6f * scale);
        velocityOverLifetime.z = Curve(0f, 0f);
        velocityOverLifetime.radial = Curve(0.5f * scale, 1.8f * scale);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.6f),
            new Keyframe(0.35f, 1.0f),
            new Keyframe(1f, 1.4f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.85f, 0.78f, 0.7f, 1f), 0f),
                new GradientColorKey(new Color(0.55f, 0.5f, 0.46f, 1f), 0.7f),
                new GradientColorKey(new Color(0.4f, 0.38f, 0.36f, 1f), 1f),
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.18f),
                new GradientAlphaKey(0.6f, 0.55f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.55f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.4f;
        noise.damping = true;

        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = Curve(-0.6f, 0.6f);

        ApplyDustRenderer(ps);
        ps.Play();

        Object.Destroy(ps.gameObject, lifetime);
        return ps.gameObject;
    }

    public static GameObject SpawnSmokeColumn(Vector3 position, float scale, float lifetime)
    {
        ParticleSystem ps = CreatePS("StressFx_SmokeColumn", position);

        var main = ps.main;
        main.duration = 1.2f;
        main.loop = false;
        main.startLifetime = Curve(2.8f, 4.2f);
        main.startSpeed = Curve(0.8f * scale, 1.8f * scale);
        main.startSize = Curve(2.4f * scale, 4.5f * scale);
        main.startRotation = Curve(0f, 6.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.65f, 0.6f, 0.55f, 1f), new Color(0.4f, 0.38f, 0.35f, 1f));
        main.gravityModifier = -0.035f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 160;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 30 + Mathf.RoundToInt(25f * scale)),
            new ParticleSystem.Burst(0.25f, 18 + Mathf.RoundToInt(15f * scale)),
            new ParticleSystem.Burst(0.6f, 12 + Mathf.RoundToInt(10f * scale)),
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.4f * scale;

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = Curve(0f, 0f);
        velocityOverLifetime.y = Curve(1.6f * scale, 3.2f * scale);
        velocityOverLifetime.z = Curve(0f, 0f);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.3f, 1f),
            new Keyframe(1f, 1.7f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.7f, 0.65f, 0.6f, 1f), 0f),
                new GradientColorKey(new Color(0.45f, 0.42f, 0.4f, 1f), 0.6f),
                new GradientColorKey(new Color(0.3f, 0.28f, 0.27f, 1f), 1f),
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.7f, 0.2f),
                new GradientAlphaKey(0.45f, 0.6f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.85f;
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.6f;
        noise.damping = true;

        ApplyDustRenderer(ps);
        ps.Play();

        Object.Destroy(ps.gameObject, lifetime);
        return ps.gameObject;
    }

    public static GameObject SpawnGroundImpactRing(Vector3 position, float scale, float lifetime)
    {
        ParticleSystem ps = CreatePS("StressFx_GroundImpact", position);

        var main = ps.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = Curve(0.9f, 1.8f);
        main.startSpeed = Curve(4.5f * scale, 8.5f * scale);
        main.startSize = Curve(0.6f * scale, 1.5f * scale);
        main.startRotation = Curve(0f, 6.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.78f, 0.72f, 0.64f, 1f), new Color(0.55f, 0.5f, 0.46f, 1f));
        main.gravityModifier = 0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 160;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 60 + Mathf.RoundToInt(40f * scale)) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.6f * scale;
        shape.arc = 360f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = Curve(0f, 0f);
        velocityOverLifetime.y = Curve(0.4f * scale, 1.0f * scale);
        velocityOverLifetime.z = Curve(0f, 0f);
        velocityOverLifetime.radial = Curve(2.5f * scale, 4.5f * scale);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(0.4f, 1.2f),
            new Keyframe(1f, 1.6f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.92f, 0.86f, 0.74f, 1f), 0f),
                new GradientColorKey(new Color(0.62f, 0.56f, 0.5f, 1f), 0.6f),
                new GradientColorKey(new Color(0.4f, 0.38f, 0.36f, 1f), 1f),
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.15f),
                new GradientAlphaKey(0.55f, 0.55f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        ApplyDustRenderer(ps);
        ps.Play();

        Object.Destroy(ps.gameObject, lifetime);
        return ps.gameObject;
    }

    public static GameObject SpawnSparks(Vector3 position, float scale, float lifetime)
    {
        ParticleSystem ps = CreatePS("StressFx_Sparks", position);

        var main = ps.main;
        main.duration = 0.25f;
        main.loop = false;
        main.startLifetime = Curve(0.4f, 0.9f);
        main.startSpeed = Curve(3.5f * scale, 7f * scale);
        main.startSize = Curve(0.04f * scale, 0.08f * scale);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.88f, 0.5f, 1f), new Color(1f, 0.6f, 0.22f, 1f));
        main.gravityModifier = 1.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 24 + Mathf.RoundToInt(8f * scale)) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.05f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.95f, 0.6f, 1f), 0f),
                new GradientColorKey(new Color(1f, 0.55f, 0.18f, 1f), 0.6f),
                new GradientColorKey(new Color(0.5f, 0.18f, 0.05f, 1f), 1f),
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.6f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        ApplySparkRenderer(ps);
        ps.Play();

        Object.Destroy(ps.gameObject, lifetime);
        return ps.gameObject;
    }

    private static void ApplyDustRenderer(ParticleSystem ps)
    {
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();

        if (renderer == null)
        {
            return;
        }

        renderer.material = GetDustMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 10;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void ApplySparkRenderer(ParticleSystem ps)
    {
        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();

        if (renderer == null)
        {
            return;
        }

        renderer.material = GetSparkMaterial();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2.5f;
        renderer.velocityScale = 0.3f;
        renderer.sortingOrder = 12;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static Shader FindRobustParticleShader()
    {
        // Sprites/Default is the most reliable across pipelines and never strips.
        Shader s = Shader.Find("Sprites/Default");
        if (s != null) return s;

        s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s != null) return s;

        s = Shader.Find("Particles/Standard Unlit");
        if (s != null) return s;

        return Shader.Find("Unlit/Transparent");
    }

    private static Material GetDustMaterial()
    {
        if (cachedDustMaterial != null && cachedDustMaterial.shader != null)
        {
            return cachedDustMaterial;
        }

        Shader shader = FindRobustParticleShader();
        cachedDustMaterial = new Material(shader);
        cachedDustMaterial.hideFlags = HideFlags.HideAndDontSave;
        cachedDustMaterial.renderQueue = 3000;

        Texture2D tex = GetDustTexture();

        if (cachedDustMaterial.HasProperty("_MainTex"))
        {
            cachedDustMaterial.SetTexture("_MainTex", tex);
        }

        if (cachedDustMaterial.HasProperty("_BaseMap"))
        {
            cachedDustMaterial.SetTexture("_BaseMap", tex);
        }

        if (cachedDustMaterial.HasProperty("_Color"))
        {
            cachedDustMaterial.SetColor("_Color", Color.white);
        }

        if (cachedDustMaterial.HasProperty("_BaseColor"))
        {
            cachedDustMaterial.SetColor("_BaseColor", Color.white);
        }

        return cachedDustMaterial;
    }

    private static Material GetSparkMaterial()
    {
        if (cachedSparkMaterial != null && cachedSparkMaterial.shader != null)
        {
            return cachedSparkMaterial;
        }

        Shader shader = FindRobustParticleShader();
        cachedSparkMaterial = new Material(shader);
        cachedSparkMaterial.hideFlags = HideFlags.HideAndDontSave;
        cachedSparkMaterial.renderQueue = 3050;

        Texture2D tex = GetSparkTexture();

        if (cachedSparkMaterial.HasProperty("_MainTex"))
        {
            cachedSparkMaterial.SetTexture("_MainTex", tex);
        }

        if (cachedSparkMaterial.HasProperty("_BaseMap"))
        {
            cachedSparkMaterial.SetTexture("_BaseMap", tex);
        }

        if (cachedSparkMaterial.HasProperty("_Color"))
        {
            cachedSparkMaterial.SetColor("_Color", Color.white);
        }

        if (cachedSparkMaterial.HasProperty("_BaseColor"))
        {
            cachedSparkMaterial.SetColor("_BaseColor", Color.white);
        }

        return cachedSparkMaterial;
    }

    private static Texture2D GetDustTexture()
    {
        if (cachedDustTex != null)
        {
            return cachedDustTex;
        }

        const int size = 128;
        cachedDustTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        cachedDustTex.hideFlags = HideFlags.HideAndDontSave;
        cachedDustTex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = Vector2.Distance(p, center) / maxDist;
                d = Mathf.Clamp01(d);

                float n = Mathf.PerlinNoise(x * 0.06f, y * 0.06f) * 0.55f
                    + Mathf.PerlinNoise(x * 0.18f, y * 0.18f) * 0.3f
                    + Mathf.PerlinNoise(x * 0.5f, y * 0.5f) * 0.15f;

                float alpha = Mathf.Pow(1f - d, 2.4f) * (0.55f + 0.45f * n);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        cachedDustTex.SetPixels(pixels);
        cachedDustTex.Apply();
        return cachedDustTex;
    }

    private static Texture2D GetSparkTexture()
    {
        if (cachedSparkTex != null)
        {
            return cachedSparkTex;
        }

        const int size = 64;
        cachedSparkTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        cachedSparkTex.hideFlags = HideFlags.HideAndDontSave;
        cachedSparkTex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - center.x) / center.x;
                float dy = (y + 0.5f - center.y) / center.y;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - r);
                alpha = Mathf.Pow(alpha, 1.8f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        cachedSparkTex.SetPixels(pixels);
        cachedSparkTex.Apply();
        return cachedSparkTex;
    }
}
