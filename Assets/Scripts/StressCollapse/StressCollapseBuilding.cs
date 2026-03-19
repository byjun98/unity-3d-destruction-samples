using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StressCollapseBuilding : MonoBehaviour
{
    [Serializable]
    public sealed class StressPillarRef
    {
        public string pillarName;
        public Transform pillarRoot;
        [Min(0f)] public float share = 1f;

        [NonSerialized] public bool failed;
        [NonSerialized] public float currentLoad;
        [NonSerialized] public float pillarStrength;
        [NonSerialized] public Renderer[] cachedRenderers;
    }

    [Serializable]
    public sealed class StressLayer
    {
        public string layerName;
        public GameObject layerRoot;
        [Min(0f)] public float weight = 10f;
        [Min(0f)] public float strength = 50f;

        [Tooltip("Optional: load-bearing pillars in this layer. If empty, the whole layer acts as a single support.")]
        public StressPillarRef[] pillars;

        [Tooltip("Layer is treated as collapsed when surviving pillars <= this count.")]
        [Min(0)] public int minPillarsForSupport = 1;

        [NonSerialized] public bool collapsed;
        [NonSerialized] public float currentLoad;
        [NonSerialized] public float startingStrength;
        [NonSerialized] public int survivingPillars;
    }

    [Header("Layers, bottom to top")]
    [SerializeField] private StressLayer[] layers;

    [Header("Stress Evaluation")]
    [SerializeField] private bool prepareRigidbodiesOnStart = true;
    [SerializeField] private bool evaluateOnStart = true;
    [SerializeField, Min(0f)] private float evaluationDelayMin = 0.2f;
    [SerializeField, Min(0f)] private float evaluationDelayMax = 0.55f;

    [Header("Cascade Collapse")]
    [SerializeField, Min(0f)] private float cascadeDelayPerLayer = 0.18f;
    [SerializeField, Min(0f)] private float pillarFailToCollapseDelay = 0.35f;

    [Header("Collapse Physics")]
    [SerializeField, Min(0f)] private float collapseExplosionForce = 4.5f;
    [SerializeField, Min(0.1f)] private float collapseExplosionRadius = 6f;
    [SerializeField, Min(0f)] private float randomSideForce = 2.5f;
    [SerializeField, Min(0f)] private float randomTorque = 1.8f;
    [SerializeField, Min(0f)] private float upwardModifier = 0.15f;

    [Header("Pillar Failure Physics")]
    [SerializeField, Min(0f)] private float pillarBurstForce = 2f;
    [SerializeField, Min(0f)] private float pillarBurstRadius = 2.2f;

    [Header("Feedback")]
    [SerializeField] private GameObject collapseEffectPrefab;
    [SerializeField] private GameObject pillarFailEffectPrefab;
    [SerializeField] private GameObject groundImpactEffectPrefab;
    [SerializeField, Min(0.1f)] private float collapseEffectLifetime = 8f;

    public event Action<int, StressLayer> LayerCollapsed;
    public event Action<int, StressPillarRef> PillarFailed;
    public event Action StressEvaluated;

    private Coroutine queuedEvaluation;
    private readonly List<int> pendingCollapseQueue = new List<int>();
    private bool cascadeRunning;

    public int LayerCount
    {
        get { return layers != null ? layers.Length : 0; }
    }

    public GameObject CollapseEffectPrefab
    {
        get { return collapseEffectPrefab; }
    }

    public GameObject PillarFailEffectPrefab
    {
        get { return pillarFailEffectPrefab; }
    }

    public GameObject GroundImpactEffectPrefab
    {
        get { return groundImpactEffectPrefab; }
    }

    private void Awake()
    {
        CacheStartingStrengths();
    }

    private void Start()
    {
        if (prepareRigidbodiesOnStart)
        {
            PrepareLayersAsStatic();
        }

        if (evaluateOnStart)
        {
            EvaluateStressNow();
        }
    }

    private void OnValidate()
    {
        if (evaluationDelayMax < evaluationDelayMin)
        {
            evaluationDelayMax = evaluationDelayMin;
        }
    }

    public StressLayer GetLayer(int layerIndex)
    {
        if (layers == null || layerIndex < 0 || layerIndex >= layers.Length)
        {
            return null;
        }

        return layers[layerIndex];
    }

    public void ConfigureForDemo(StressLayer[] configuredLayers, GameObject collapseEffect)
    {
        layers = configuredLayers;
        collapseEffectPrefab = collapseEffect;
        CacheStartingStrengths();
    }

    public void RefreshConfiguration()
    {
        CacheStartingStrengths();
    }

    public void SetCollapseEffects(GameObject collapsePrefab, GameObject pillarFailPrefab, GameObject groundImpactPrefab)
    {
        if (collapsePrefab != null)
        {
            collapseEffectPrefab = collapsePrefab;
        }

        if (pillarFailPrefab != null)
        {
            pillarFailEffectPrefab = pillarFailPrefab;
        }

        if (groundImpactPrefab != null)
        {
            groundImpactEffectPrefab = groundImpactPrefab;
        }
    }

    public void DamageLayer(int layerIndex, float damage)
    {
        DamageLayerAt(layerIndex, damage, Vector3.zero);
    }

    public void DamageLayerAt(int layerIndex, float damage, Vector3 damagePoint)
    {
        if (layers == null || layerIndex < 0 || layerIndex >= layers.Length)
        {
            return;
        }

        StressLayer layer = layers[layerIndex];

        if (layer == null || layer.collapsed)
        {
            return;
        }

        float clampedDamage = Mathf.Max(0f, damage);
        layer.strength = Mathf.Max(0f, layer.strength - clampedDamage);

        TryDamageNearestPillar(layer, layerIndex, clampedDamage, damagePoint);
        QueueStressEvaluation();
    }

    private void TryDamageNearestPillar(StressLayer layer, int layerIndex, float damage, Vector3 damagePoint)
    {
        if (layer.pillars == null || layer.pillars.Length == 0 || damage <= 0f)
        {
            return;
        }

        StressPillarRef closest = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < layer.pillars.Length; i++)
        {
            StressPillarRef pillar = layer.pillars[i];

            if (pillar == null || pillar.failed || pillar.pillarRoot == null)
            {
                continue;
            }

            float distance = (pillar.pillarRoot.position - damagePoint).sqrMagnitude;

            if (distance < closestDistance)
            {
                closest = pillar;
                closestDistance = distance;
            }
        }

        if (closest != null)
        {
            closest.pillarStrength = Mathf.Max(0f, closest.pillarStrength - damage * 0.6f);

            if (closest.pillarStrength <= 0f)
            {
                FailPillar(layerIndex, closest, damagePoint);
            }
        }
    }

    public void EvaluateStressNow()
    {
        if (layers == null || layers.Length == 0)
        {
            return;
        }

        RecalculateLoads();
        StressEvaluated?.Invoke();

        for (int i = 0; i < layers.Length; i++)
        {
            StressLayer layer = layers[i];

            if (layer == null || layer.collapsed)
            {
                continue;
            }

            bool overloaded = layer.strength < layer.currentLoad;
            bool noSupport = layer.pillars != null
                && layer.pillars.Length > 0
                && layer.survivingPillars < Mathf.Max(1, layer.minPillarsForSupport);

            if (overloaded || noSupport)
            {
                ScheduleCascadeCollapseFrom(i);
                return;
            }

            if (layer.pillars != null)
            {
                for (int p = 0; p < layer.pillars.Length; p++)
                {
                    StressPillarRef pillar = layer.pillars[p];

                    if (pillar == null || pillar.failed)
                    {
                        continue;
                    }

                    if (pillar.pillarStrength > 0f && pillar.currentLoad > pillar.pillarStrength)
                    {
                        FailPillar(i, pillar, pillar.pillarRoot != null ? pillar.pillarRoot.position : transform.position);
                    }
                }
            }
        }
    }

    [ContextMenu("Collapse From Bottom")]
    private void CollapseFromBottom()
    {
        ScheduleCascadeCollapseFrom(0);
    }

    [ContextMenu("Fail All Bottom Pillars")]
    private void FailAllBottomPillars()
    {
        if (layers == null || layers.Length == 0)
        {
            return;
        }

        StressLayer layer = layers[0];

        if (layer.pillars == null)
        {
            return;
        }

        for (int i = 0; i < layer.pillars.Length; i++)
        {
            StressPillarRef pillar = layer.pillars[i];

            if (pillar != null && !pillar.failed && pillar.pillarRoot != null)
            {
                FailPillar(0, pillar, pillar.pillarRoot.position);
            }
        }
    }

    private void QueueStressEvaluation()
    {
        if (!isActiveAndEnabled)
        {
            EvaluateStressNow();
            return;
        }

        if (queuedEvaluation != null)
        {
            return;
        }

        queuedEvaluation = StartCoroutine(EvaluateAfterDelay());
    }

    private IEnumerator EvaluateAfterDelay()
    {
        float delay = UnityEngine.Random.Range(evaluationDelayMin, evaluationDelayMax);

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        queuedEvaluation = null;
        EvaluateStressNow();
    }

    private void CacheStartingStrengths()
    {
        if (layers == null)
        {
            return;
        }

        for (int i = 0; i < layers.Length; i++)
        {
            StressLayer layer = layers[i];

            if (layer == null)
            {
                continue;
            }

            layer.startingStrength = layer.strength;
            layer.currentLoad = 0f;
            layer.collapsed = false;

            if (layer.pillars != null && layer.pillars.Length > 0)
            {
                int alive = 0;
                float totalShare = 0f;

                for (int p = 0; p < layer.pillars.Length; p++)
                {
                    if (layer.pillars[p] != null)
                    {
                        totalShare += Mathf.Max(0.0001f, layer.pillars[p].share);
                    }
                }

                for (int p = 0; p < layer.pillars.Length; p++)
                {
                    StressPillarRef pillar = layer.pillars[p];

                    if (pillar == null)
                    {
                        continue;
                    }

                    pillar.failed = false;
                    pillar.currentLoad = 0f;

                    float weightedShare = Mathf.Max(0.0001f, pillar.share) / Mathf.Max(0.0001f, totalShare);
                    pillar.pillarStrength = layer.strength * weightedShare;

                    if (pillar.cachedRenderers == null && pillar.pillarRoot != null)
                    {
                        pillar.cachedRenderers = pillar.pillarRoot.GetComponentsInChildren<Renderer>(true);
                    }

                    alive++;
                }

                layer.survivingPillars = alive;
            }
        }
    }

    private void RecalculateLoads()
    {
        float load = 0f;

        for (int i = layers.Length - 1; i >= 0; i--)
        {
            StressLayer layer = layers[i];

            if (layer == null)
            {
                continue;
            }

            if (layer.collapsed)
            {
                layer.currentLoad = 0f;

                if (layer.pillars != null)
                {
                    for (int p = 0; p < layer.pillars.Length; p++)
                    {
                        if (layer.pillars[p] != null)
                        {
                            layer.pillars[p].currentLoad = 0f;
                        }
                    }
                }

                continue;
            }

            load += Mathf.Max(0f, layer.weight);
            layer.currentLoad = load;

            if (layer.pillars != null && layer.pillars.Length > 0)
            {
                float totalShare = 0f;
                int alive = 0;

                for (int p = 0; p < layer.pillars.Length; p++)
                {
                    StressPillarRef pillar = layer.pillars[p];

                    if (pillar != null && !pillar.failed)
                    {
                        totalShare += Mathf.Max(0.0001f, pillar.share);
                        alive++;
                    }
                }

                layer.survivingPillars = alive;

                for (int p = 0; p < layer.pillars.Length; p++)
                {
                    StressPillarRef pillar = layer.pillars[p];

                    if (pillar == null)
                    {
                        continue;
                    }

                    if (pillar.failed)
                    {
                        pillar.currentLoad = 0f;
                        continue;
                    }

                    float share = Mathf.Max(0.0001f, pillar.share) / Mathf.Max(0.0001f, totalShare);
                    pillar.currentLoad = load * share;
                }
            }
        }
    }

    private void PrepareLayersAsStatic()
    {
        if (layers == null)
        {
            return;
        }

        for (int i = 0; i < layers.Length; i++)
        {
            StressLayer layer = layers[i];

            if (layer == null || layer.layerRoot == null)
            {
                continue;
            }

            Rigidbody[] bodies = layer.layerRoot.GetComponentsInChildren<Rigidbody>(true);

            for (int j = 0; j < bodies.Length; j++)
            {
                bodies[j].isKinematic = true;
                bodies[j].useGravity = false;
                bodies[j].interpolation = RigidbodyInterpolation.Interpolate;
                bodies[j].collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
    }

    private void ScheduleCascadeCollapseFrom(int startLayerIndex)
    {
        if (startLayerIndex < 0)
        {
            startLayerIndex = 0;
        }

        for (int i = startLayerIndex; i < layers.Length; i++)
        {
            if (!pendingCollapseQueue.Contains(i))
            {
                pendingCollapseQueue.Add(i);
            }
        }

        if (!cascadeRunning && isActiveAndEnabled)
        {
            StartCoroutine(RunCascade());
        }
    }

    private IEnumerator RunCascade()
    {
        cascadeRunning = true;

        if (pillarFailToCollapseDelay > 0f)
        {
            yield return new WaitForSeconds(pillarFailToCollapseDelay);
        }

        pendingCollapseQueue.Sort();

        for (int i = pendingCollapseQueue.Count - 1; i >= 0; i--)
        {
            int idx = pendingCollapseQueue[i];
            StressLayer layer = GetLayer(idx);

            if (layer != null && !layer.collapsed)
            {
                CollapseLayer(idx);

                if (cascadeDelayPerLayer > 0f && i > 0)
                {
                    yield return new WaitForSeconds(cascadeDelayPerLayer);
                }
            }
        }

        pendingCollapseQueue.Clear();
        cascadeRunning = false;
        RecalculateLoads();
    }

    private void FailPillar(int layerIndex, StressPillarRef pillar, Vector3 hitPoint)
    {
        if (pillar == null || pillar.failed || pillar.pillarRoot == null)
        {
            return;
        }

        pillar.failed = true;
        pillar.currentLoad = 0f;

        Vector3 burstCenter = pillar.pillarRoot.position;
        Rigidbody[] bodies = pillar.pillarRoot.GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody body = bodies[i];

            if (body == null)
            {
                continue;
            }

            body.isKinematic = false;
            body.useGravity = true;
            body.WakeUp();

            Vector3 dir = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-0.05f, 0.2f),
                UnityEngine.Random.Range(-1f, 1f)
            );

            if (dir.sqrMagnitude < 0.001f)
            {
                dir = Vector3.right;
            }

            dir.Normalize();
            body.AddExplosionForce(pillarBurstForce, burstCenter, pillarBurstRadius, 0.05f, ForceMode.Impulse);
            body.AddForce(dir * pillarBurstForce * 0.4f, ForceMode.Impulse);
            body.AddTorque(UnityEngine.Random.onUnitSphere * randomTorque * 0.5f, ForceMode.Impulse);
        }

        SpawnEffect(pillarFailEffectPrefab, hitPoint);

        StressLayer layer = GetLayer(layerIndex);

        if (layer != null && layer.pillars != null)
        {
            int alive = 0;

            for (int i = 0; i < layer.pillars.Length; i++)
            {
                if (layer.pillars[i] != null && !layer.pillars[i].failed)
                {
                    alive++;
                }
            }

            layer.survivingPillars = alive;
        }

        PillarFailed?.Invoke(layerIndex, pillar);
        QueueStressEvaluation();
    }

    private void CollapseLayer(int layerIndex)
    {
        StressLayer layer = GetLayer(layerIndex);

        if (layer == null || layer.layerRoot == null || layer.collapsed)
        {
            return;
        }

        layer.collapsed = true;

        Vector3 collapseCenter = GetLayerCenter(layer.layerRoot);
        Rigidbody[] bodies = layer.layerRoot.GetComponentsInChildren<Rigidbody>(true);

        SpawnEffect(collapseEffectPrefab, collapseCenter);

        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody body = bodies[i];

            if (body == null)
            {
                continue;
            }

            body.isKinematic = false;
            body.useGravity = true;
            body.WakeUp();

            Vector3 randomDirection = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(0f, 0.35f),
                UnityEngine.Random.Range(-1f, 1f)
            );

            if (randomDirection.sqrMagnitude < 0.001f)
            {
                randomDirection = Vector3.right;
            }

            randomDirection.Normalize();
            body.AddExplosionForce(collapseExplosionForce, collapseCenter, collapseExplosionRadius, upwardModifier, ForceMode.Impulse);
            body.AddForce(randomDirection * randomSideForce, ForceMode.Impulse);
            body.AddTorque(UnityEngine.Random.onUnitSphere * randomTorque, ForceMode.Impulse);
        }

        if (groundImpactEffectPrefab != null)
        {
            Vector3 ground = collapseCenter;
            ground.y = transform.position.y + 0.05f;
            SpawnEffect(groundImpactEffectPrefab, ground);
        }

        LayerCollapsed?.Invoke(layerIndex, layer);
    }

    private void SpawnEffect(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(prefab, position, Quaternion.identity);
        StressUrpMaterialFixer.Fix(effect);
        Destroy(effect, collapseEffectLifetime);
    }

    private static Vector3 GetLayerCenter(GameObject layerRoot)
    {
        Renderer[] renderers = layerRoot.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return layerRoot.transform.position;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center;
    }
}
