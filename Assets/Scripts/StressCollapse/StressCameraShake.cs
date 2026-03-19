using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public sealed class StressCameraShake : MonoBehaviour
{
    [SerializeField] private Transform shakeTarget;
    [SerializeField] private StressCollapseBuilding building;

    [Header("Per-Pillar Failure")]
    [SerializeField, Min(0f)] private float pillarShakeMagnitude = 0.06f;
    [SerializeField, Min(0f)] private float pillarShakeDuration = 0.25f;

    [Header("Layer Collapse")]
    [SerializeField, Min(0f)] private float layerShakeMagnitude = 0.22f;
    [SerializeField, Min(0f)] private float layerShakeDuration = 0.7f;

    [Header("Tuning")]
    [SerializeField, Min(0.01f)] private float frequency = 28f;
    [SerializeField] private bool affectRotation = true;
    [SerializeField, Range(0f, 5f)] private float rotationDegrees = 1.6f;

    private float currentMagnitude;
    private float currentRemaining;
    private float totalDuration;
    private float seed;

    private void Awake()
    {
        if (shakeTarget == null)
        {
            shakeTarget = transform;
        }

        seed = Random.value * 100f;

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

        if (building != null && enabled && gameObject.activeInHierarchy)
        {
            building.PillarFailed += HandlePillarFailed;
            building.LayerCollapsed += HandleLayerCollapsed;
        }
    }

    private void HandlePillarFailed(int layerIndex, StressCollapseBuilding.StressPillarRef pillar)
    {
        TriggerShake(pillarShakeMagnitude, pillarShakeDuration);
    }

    private void HandleLayerCollapsed(int layerIndex, StressCollapseBuilding.StressLayer layer)
    {
        float scale = 1f + layerIndex * 0.18f;
        TriggerShake(layerShakeMagnitude * scale, layerShakeDuration);
    }

    public void TriggerShake(float magnitude, float duration)
    {
        if (magnitude > currentMagnitude || currentRemaining <= 0f)
        {
            currentMagnitude = magnitude;
            totalDuration = Mathf.Max(0.0001f, duration);
        }

        if (duration > currentRemaining)
        {
            currentRemaining = duration;
            totalDuration = Mathf.Max(totalDuration, duration);
        }
    }

    private void LateUpdate()
    {
        if (currentRemaining <= 0f)
        {
            return;
        }

        float falloff = Mathf.Clamp01(currentRemaining / Mathf.Max(0.0001f, totalDuration));
        float magnitude = currentMagnitude * falloff;

        float t = Time.time * frequency;
        float ox = (Mathf.PerlinNoise(seed + t, 0f) - 0.5f) * 2f * magnitude;
        float oy = (Mathf.PerlinNoise(0f, seed + t) - 0.5f) * 2f * magnitude;
        float oz = (Mathf.PerlinNoise(seed + t, seed + t) - 0.5f) * 2f * magnitude;

        Vector3 offset = new Vector3(ox, oy, oz);
        shakeTarget.position += offset;

        if (affectRotation)
        {
            Vector3 euler = new Vector3(oy, ox, oz) * rotationDegrees * 4f;
            shakeTarget.rotation = shakeTarget.rotation * Quaternion.Euler(euler);
        }

        currentRemaining -= Time.deltaTime;
    }
}
