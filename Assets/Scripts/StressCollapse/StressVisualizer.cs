using UnityEngine;

[DisallowMultipleComponent]
public sealed class StressVisualizer : MonoBehaviour
{
    [SerializeField] private StressCollapseBuilding building;

    [Header("Tint")]
    [SerializeField] private Color safeTint = new Color(0.95f, 0.95f, 0.95f, 1f);
    [SerializeField] private Color warningTint = new Color(1f, 0.78f, 0.32f, 1f);
    [SerializeField] private Color criticalTint = new Color(1f, 0.32f, 0.18f, 1f);
    [SerializeField, Range(0f, 1f)] private float warningRatio = 0.55f;

    [Header("Emission Pulse")]
    [SerializeField] private bool emissionPulse = true;
    [SerializeField] private Color emissionColor = new Color(1f, 0.32f, 0.18f, 1f);
    [SerializeField, Range(0f, 8f)] private float emissionStrength = 1.6f;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 4f;

    [Header("Update")]
    [SerializeField, Min(0.02f)] private float refreshInterval = 0.08f;

    private MaterialPropertyBlock propertyBlock;
    private float nextRefresh;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (building == null)
        {
            building = FindObjectOfType<StressCollapseBuilding>();
        }
    }

    public void Configure(StressCollapseBuilding owner)
    {
        building = owner;
    }

    private void LateUpdate()
    {
        if (Time.time < nextRefresh || building == null)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        nextRefresh = Time.time + refreshInterval;

        for (int i = 0; i < building.LayerCount; i++)
        {
            StressCollapseBuilding.StressLayer layer = building.GetLayer(i);

            if (layer == null || layer.layerRoot == null)
            {
                continue;
            }

            if (layer.collapsed)
            {
                continue;
            }

            float layerRatio = layer.strength > 0.001f ? Mathf.Clamp01(layer.currentLoad / layer.strength) : 0f;

            if (layer.pillars == null || layer.pillars.Length == 0)
            {
                ApplyToLayerRoot(layer.layerRoot, layerRatio);
                continue;
            }

            ApplyToLayerRoot(layer.layerRoot, layerRatio * 0.55f);

            for (int p = 0; p < layer.pillars.Length; p++)
            {
                StressCollapseBuilding.StressPillarRef pillar = layer.pillars[p];

                if (pillar == null || pillar.failed)
                {
                    continue;
                }

                float ratio = pillar.pillarStrength > 0.001f
                    ? Mathf.Clamp01(pillar.currentLoad / pillar.pillarStrength)
                    : 0f;

                ApplyToPillar(pillar, ratio);
            }
        }
    }

    private void ApplyToLayerRoot(GameObject root, float ratio)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Color tint = ResolveTint(ratio);
        float emission = ratio < warningRatio ? 0f : Mathf.Lerp(0f, emissionStrength, Mathf.InverseLerp(warningRatio, 1f, ratio));

        if (emissionPulse && emission > 0f)
        {
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * pulseSpeed);
            emission *= pulse;
        }

        Color emit = emissionColor * emission;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];

            if (r == null)
            {
                continue;
            }

            r.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, tint);
            propertyBlock.SetColor(ColorId, tint);
            propertyBlock.SetColor(EmissionColorId, emit);
            r.SetPropertyBlock(propertyBlock);
        }
    }

    private void ApplyToPillar(StressCollapseBuilding.StressPillarRef pillar, float ratio)
    {
        if (pillar.cachedRenderers == null || pillar.cachedRenderers.Length == 0)
        {
            if (pillar.pillarRoot == null)
            {
                return;
            }

            pillar.cachedRenderers = pillar.pillarRoot.GetComponentsInChildren<Renderer>(true);
        }

        Color tint = ResolveTint(ratio);
        float emission = ratio < warningRatio ? 0f : Mathf.Lerp(0f, emissionStrength, Mathf.InverseLerp(warningRatio, 1f, ratio));

        if (emissionPulse && emission > 0f)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
            emission *= pulse;
        }

        Color emit = emissionColor * emission;

        for (int i = 0; i < pillar.cachedRenderers.Length; i++)
        {
            Renderer r = pillar.cachedRenderers[i];

            if (r == null)
            {
                continue;
            }

            r.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, tint);
            propertyBlock.SetColor(ColorId, tint);
            propertyBlock.SetColor(EmissionColorId, emit);
            r.SetPropertyBlock(propertyBlock);
        }
    }

    private Color ResolveTint(float ratio)
    {
        if (ratio < warningRatio)
        {
            float t = warningRatio > 0.001f ? Mathf.Clamp01(ratio / warningRatio) : 0f;
            return Color.Lerp(safeTint, warningTint, t * 0.65f);
        }

        float c = Mathf.InverseLerp(warningRatio, 1f, ratio);
        return Color.Lerp(warningTint, criticalTint, c);
    }
}
