using UnityEngine;

[DisallowMultipleComponent]
public sealed class StressEffectsBinder : MonoBehaviour
{
    [SerializeField] private StressCollapseBuilding building;
    [SerializeField] private bool spawnSparks = true;
    [SerializeField] private bool spawnGroundImpact = true;
    [SerializeField] private bool spawnSmokeColumn = true;

    [SerializeField, Min(0.1f)] private float pillarDustScale = 1f;
    [SerializeField, Min(0.1f)] private float layerDustScale = 1.6f;
    [SerializeField, Min(0.5f)] private float pillarLifetime = 4f;
    [SerializeField, Min(0.5f)] private float layerLifetime = 7f;

    private void Awake()
    {
        if (building == null)
        {
            building = FindObjectOfType<StressCollapseBuilding>();
        }
    }

    private void OnEnable()
    {
        if (building != null)
        {
            building.PillarFailed += HandlePillarFailed;
            building.LayerCollapsed += HandleLayerCollapsed;
        }
    }

    private void OnDisable()
    {
        if (building != null)
        {
            building.PillarFailed -= HandlePillarFailed;
            building.LayerCollapsed -= HandleLayerCollapsed;
        }
    }

    public void Bind(StressCollapseBuilding target)
    {
        if (building != null)
        {
            building.PillarFailed -= HandlePillarFailed;
            building.LayerCollapsed -= HandleLayerCollapsed;
        }

        building = target;

        if (building != null && enabled)
        {
            building.PillarFailed += HandlePillarFailed;
            building.LayerCollapsed += HandleLayerCollapsed;
        }
    }

    private void HandlePillarFailed(int layerIndex, StressCollapseBuilding.StressPillarRef pillar)
    {
        if (pillar == null || pillar.pillarRoot == null)
        {
            return;
        }

        Bounds bounds = ComputeBounds(pillar.pillarRoot);
        Vector3 burstPoint = bounds.center;

        StressFxRuntime.SpawnDustBurst(burstPoint, pillarDustScale, pillarLifetime);

        if (spawnSparks)
        {
            StressFxRuntime.SpawnSparks(burstPoint, pillarDustScale * 0.6f, 1.6f);
        }
    }

    private void HandleLayerCollapsed(int layerIndex, StressCollapseBuilding.StressLayer layer)
    {
        if (layer == null || layer.layerRoot == null)
        {
            return;
        }

        Bounds bounds = ComputeBounds(layer.layerRoot.transform);
        Vector3 center = bounds.center;
        float scale = layerDustScale * Mathf.Lerp(0.85f, 1.55f, Mathf.InverseLerp(0f, 4f, layerIndex));

        StressFxRuntime.SpawnDustBurst(center, scale, layerLifetime);

        if (spawnSmokeColumn)
        {
            StressFxRuntime.SpawnSmokeColumn(new Vector3(center.x, bounds.min.y + 0.3f, center.z), scale * 1.2f, layerLifetime + 2f);
        }

        if (spawnGroundImpact)
        {
            float groundY = building != null ? building.transform.position.y + 0.05f : center.y - bounds.extents.y;
            Vector3 groundPos = new Vector3(center.x, groundY, center.z);
            StressFxRuntime.SpawnGroundImpactRing(groundPos, scale * 1.4f, layerLifetime);
        }
    }

    private static Bounds ComputeBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return new Bounds(root.position, Vector3.one);
        }

        Bounds b = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        return b;
    }
}
