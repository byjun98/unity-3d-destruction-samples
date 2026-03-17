using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class FireGridSimulator : MonoBehaviour
{
    private enum CellState
    {
        Normal,
        Burning,
        BurnedOut
    }

    private enum FireMaterial
    {
        Wood,
        Stone,
        Water,
        Oil
    }

    private sealed class FireCell
    {
        public int x;
        public int y;
        public float hp;
        public float temperature;
        public float wetness;
        public float burnTime;
        public CellState state;
        public FireMaterial material;
        public GameObject visual;
        public GameObject materialVisual;
        public GameObject fireEffect;
        public GameObject burnedOverlay;
        public Renderer renderer;
        public Renderer[] visualRenderers;
    }

    [Header("Grid")]
    [SerializeField, Min(2)] private int width = 14;
    [SerializeField, Min(2)] private int height = 9;
    [SerializeField, Min(0.2f)] private float cellSize = 1f;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject fireEffectPrefab;
    [SerializeField] private GameObject[] woodVisualPrefabs;
    [SerializeField] private GameObject[] stoneVisualPrefabs;
    [SerializeField] private GameObject oilVisualPrefab;
    [SerializeField] private GameObject burnedOverlayPrefab;
    [SerializeField] private bool showDebugCells;

    [Header("Materials")]
    [SerializeField] private Material woodMaterial;
    [SerializeField, FormerlySerializedAs("wetWoodMaterial")] private Material waterMaterial;
    [SerializeField] private Material stoneMaterial;
    [SerializeField] private Material oilMaterial;
    [SerializeField] private Material burningMaterial;
    [SerializeField] private Material burnedMaterial;
    [SerializeField] private Material charredMaterial;

    [Header("Fire Rules")]
    [SerializeField, Min(1f)] private float initialHp = 100f;
    [SerializeField, Min(0f)] private float burnDamagePerTick = 10f;
    [SerializeField, Min(0.05f)] private float tickInterval = 0.4f;
    [SerializeField, Range(0f, 1f)] private float baseSpreadChance = 0.26f;
    [SerializeField, Min(0f)] private float heatPerTick = 18f;
    [SerializeField, Min(1f)] private float ignitionTemperature = 80f;
    [SerializeField] private bool igniteCenterOnStart = true;

    [Header("Presentation")]
    [SerializeField, Min(0f)] private float visualJitter = 0.12f;
    [SerializeField, Min(0.1f)] private float visualScale = 1f;
    [SerializeField] private Vector2 visualScaleRange = new Vector2(0.85f, 1.12f);
    [SerializeField, Range(0f, 1f)] private float woodPropChance = 0.35f;
    [SerializeField, Min(0f)] private float fireEffectHeightOffset = 0.45f;
    [SerializeField, Min(0.1f)] private float fireEffectScale = 0.85f;
    [SerializeField, Min(0f)] private float fireLightRange = 2.2f;
    [SerializeField, Min(0f)] private float fireLightIntensity = 1.6f;

    private readonly List<FireCell> activeCells = new List<FireCell>();
    private readonly List<FireCell> pendingIgnitions = new List<FireCell>();
    private readonly Vector2Int[] directions =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    private FireCell[,] cells;
    private Coroutine tickRoutine;

    private void Start()
    {
        Time.timeScale = 1f;
        CreateGrid();

        if (igniteCenterOnStart)
        {
            Ignite(width / 2, height / 2);
        }

        tickRoutine = StartCoroutine(FireTickLoop());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetGrid();
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryIgniteFromMouse();
        }
    }

    public void Ignite(int x, int y)
    {
        if (!IsInside(x, y))
        {
            return;
        }

        FireCell cell = cells[x, y];

        if (cell.state != CellState.Normal || !CanBurn(cell.material))
        {
            return;
        }

        cell.state = CellState.Burning;
        cell.temperature = Mathf.Max(cell.temperature, ignitionTemperature);
        cell.burnTime = 0f;
        activeCells.Add(cell);
        ApplyVisual(cell);
    }

    private void CreateGrid()
    {
        ClearGrid();
        cells = new FireCell[width, height];

        Vector3 originOffset = new Vector3((width - 1) * cellSize * -0.5f, 0f, (height - 1) * cellSize * -0.5f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                FireMaterial material = PickMaterial(x, y);
                Vector3 position = transform.position + originOffset + new Vector3(x * cellSize, 0f, y * cellSize);
                GameObject visual = Instantiate(cellPrefab, position, Quaternion.identity, transform);
                visual.name = "FireCell_" + x + "_" + y + "_" + material;
                visual.transform.localScale = new Vector3(cellSize * 0.96f, 0.035f, cellSize * 0.96f);
                Renderer renderer = visual.GetComponent<Renderer>();

                if (renderer != null)
                {
                    renderer.enabled = showDebugCells;
                }

                FireCellHitbox hitbox = visual.GetComponent<FireCellHitbox>();

                if (hitbox == null)
                {
                    hitbox = visual.AddComponent<FireCellHitbox>();
                }

                hitbox.SetCoordinates(x, y);

                FireCell cell = new FireCell
                {
                    x = x,
                    y = y,
                    hp = GetMaxHp(material),
                    temperature = 0f,
                    wetness = material == FireMaterial.Water ? 1f : 0f,
                    burnTime = 0f,
                    state = CellState.Normal,
                    material = material,
                    visual = visual,
                    renderer = renderer
                };

                cells[x, y] = cell;
                SpawnMaterialVisual(cell, position);
                ApplyVisual(cell);
            }
        }
    }

    private void ClearGrid()
    {
        activeCells.Clear();
        pendingIgnitions.Clear();

        if (cells == null)
        {
            return;
        }

        for (int y = 0; y < cells.GetLength(1); y++)
        {
            for (int x = 0; x < cells.GetLength(0); x++)
            {
                FireCell cell = cells[x, y];

                if (cell != null && cell.visual != null)
                {
                    Destroy(cell.visual);
                }

                if (cell != null && cell.materialVisual != null)
                {
                    Destroy(cell.materialVisual);
                }

                if (cell != null && cell.fireEffect != null)
                {
                    Destroy(cell.fireEffect);
                }

                if (cell != null && cell.burnedOverlay != null)
                {
                    Destroy(cell.burnedOverlay);
                }
            }
        }
    }

    private void ResetGrid()
    {
        if (tickRoutine != null)
        {
            StopCoroutine(tickRoutine);
        }

        CreateGrid();
        Ignite(width / 2, height / 2);
        tickRoutine = StartCoroutine(FireTickLoop());
    }

    private IEnumerator FireTickLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(tickInterval);
            SimulateFireTick();
        }
    }

    private void SimulateFireTick()
    {
        pendingIgnitions.Clear();

        for (int i = activeCells.Count - 1; i >= 0; i--)
        {
            FireCell cell = activeCells[i];

            if (cell.state != CellState.Burning)
            {
                activeCells.RemoveAt(i);
                continue;
            }

            cell.burnTime += tickInterval;
            cell.hp -= burnDamagePerTick * GetBurnDamageMultiplier(cell.material);

            if (cell.hp <= 0f)
            {
                cell.hp = 0f;
                cell.state = CellState.BurnedOut;
                activeCells.RemoveAt(i);
                ApplyVisual(cell);
                continue;
            }

            SpreadHeatFrom(cell);
            ApplyVisual(cell);
        }

        for (int i = 0; i < pendingIgnitions.Count; i++)
        {
            FireCell cell = pendingIgnitions[i];
            Ignite(cell.x, cell.y);
        }
    }

    private void SpreadHeatFrom(FireCell source)
    {
        for (int i = 0; i < directions.Length; i++)
        {
            int nx = source.x + directions[i].x;
            int ny = source.y + directions[i].y;

            if (!IsInside(nx, ny))
            {
                continue;
            }

            FireCell neighbor = cells[nx, ny];

            if (neighbor.state != CellState.Normal || !CanBurn(neighbor.material))
            {
                continue;
            }

            float wetnessPenalty = 1f - neighbor.wetness;
            float heatGain = heatPerTick * GetSpreadHeatMultiplier(source.material) * GetHeatGainMultiplier(neighbor.material) * wetnessPenalty;
            neighbor.temperature += heatGain;

            float chance = baseSpreadChance * GetIgnitionChanceMultiplier(neighbor.material) * wetnessPenalty;
            float heatRatio = Mathf.Clamp01(neighbor.temperature / ignitionTemperature);

            if (neighbor.temperature >= ignitionTemperature || Random.value <= chance * heatRatio)
            {
                if (!pendingIgnitions.Contains(neighbor))
                {
                    pendingIgnitions.Add(neighbor);
                }
            }

            ApplyVisual(neighbor);
        }
    }

    private void TryIgniteFromMouse()
    {
        Camera camera = Camera.main;

        if (camera == null)
        {
            return;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (!Physics.Raycast(ray, out hit, 100f))
        {
            return;
        }

        FireCellHitbox hitbox = hit.collider.GetComponent<FireCellHitbox>();

        if (hitbox != null)
        {
            Ignite(hitbox.X, hitbox.Y);
        }
    }

    private FireMaterial PickMaterial(int x, int y)
    {
        if (x == width / 2 + 2 && y >= 1 && y <= height - 2)
        {
            return FireMaterial.Stone;
        }

        if (y == 1 && x > 1 && x < width - 2)
        {
            return FireMaterial.Oil;
        }

        if ((x == 2 && y > 2) || (x == width - 3 && y < height - 2))
        {
            return FireMaterial.Water;
        }

        return FireMaterial.Wood;
    }

    private void ApplyVisual(FireCell cell)
    {
        if (cell.renderer != null)
        {
            cell.renderer.enabled = showDebugCells;
            cell.renderer.sharedMaterial = ResolveMaterial(cell);
        }

        bool shouldShowFire = cell.state == CellState.Burning;
        bool shouldShowBurnedOverlay = cell.state == CellState.BurnedOut;

        if (shouldShowFire && cell.fireEffect == null && fireEffectPrefab != null)
        {
            Vector3 offset = Vector3.up * fireEffectHeightOffset;
            cell.fireEffect = Instantiate(fireEffectPrefab, cell.visual.transform.position + offset, Quaternion.identity, transform);
            cell.fireEffect.transform.localScale = Vector3.one * fireEffectScale;
            EnsureFireLight(cell.fireEffect);
        }
        else if (!shouldShowFire && cell.fireEffect != null)
        {
            Destroy(cell.fireEffect);
            cell.fireEffect = null;
        }

        if (shouldShowBurnedOverlay)
        {
            RemoveMaterialVisual(cell);

            if (cell.burnedOverlay == null && burnedOverlayPrefab != null)
            {
                Vector3 position = cell.visual.transform.position + Vector3.up * 0.012f;
                cell.burnedOverlay = Instantiate(burnedOverlayPrefab, position, Quaternion.identity, transform);
                cell.burnedOverlay.transform.localScale = new Vector3(cellSize * 0.92f, 0.012f, cellSize * 0.92f);
            }
        }
        else if (cell.burnedOverlay != null)
        {
            Destroy(cell.burnedOverlay);
            cell.burnedOverlay = null;
        }
    }

    private void SpawnMaterialVisual(FireCell cell, Vector3 position)
    {
        if (cell.material == FireMaterial.Wood && Random.value > woodPropChance)
        {
            return;
        }

        GameObject prefab = PickVisualPrefab(cell.material);

        if (prefab == null)
        {
            return;
        }

        Vector3 jitter = new Vector3(Random.Range(-visualJitter, visualJitter), 0f, Random.Range(-visualJitter, visualJitter));
        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject instance = Instantiate(prefab, position + jitter, rotation, transform);
        instance.name = "FireCellVisual_" + cell.x + "_" + cell.y + "_" + cell.material;
        cell.materialVisual = instance;
        cell.visualRenderers = instance.GetComponentsInChildren<Renderer>();

        FitVisualToCell(instance, GetVisualFitSize(cell.material));
        PlaceVisualOnGround(instance, position.y + 0.025f);
    }

    private GameObject PickVisualPrefab(FireMaterial material)
    {
        switch (material)
        {
            case FireMaterial.Stone:
                return PickFrom(stoneVisualPrefabs);
            case FireMaterial.Water:
                return null;
            case FireMaterial.Oil:
                return null;
            default:
                return PickFrom(woodVisualPrefabs);
        }
    }

    private static GameObject PickFrom(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];

            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    private float GetVisualFitSize(FireMaterial material)
    {
        switch (material)
        {
            case FireMaterial.Stone:
                return cellSize * 0.9f;
            case FireMaterial.Oil:
                return cellSize * 0.72f;
            default:
                return cellSize * 0.86f;
        }
    }

    private void FitVisualToCell(GameObject instance, float targetSize)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        float largestHorizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);

        if (largestHorizontalSize <= 0.001f)
        {
            return;
        }

        float minScale = Mathf.Min(visualScaleRange.x, visualScaleRange.y);
        float maxScale = Mathf.Max(visualScaleRange.x, visualScaleRange.y);
        float randomScale = Random.Range(minScale, maxScale);
        float scaleMultiplier = targetSize / largestHorizontalSize * visualScale * randomScale;
        instance.transform.localScale *= scaleMultiplier;
    }

    private static void PlaceVisualOnGround(GameObject instance, float groundY)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        instance.transform.position += Vector3.up * (groundY - bounds.min.y);
    }

    private void ApplyCharredMaterial(FireCell cell)
    {
        if (charredMaterial == null || cell.visualRenderers == null)
        {
            return;
        }

        for (int i = 0; i < cell.visualRenderers.Length; i++)
        {
            Renderer renderer = cell.visualRenderers[i];

            if (renderer != null)
            {
                renderer.sharedMaterial = charredMaterial;
            }
        }
    }

    private void RemoveMaterialVisual(FireCell cell)
    {
        if (cell.materialVisual == null)
        {
            return;
        }

        Destroy(cell.materialVisual);
        cell.materialVisual = null;
        cell.visualRenderers = null;
    }

    private void EnsureFireLight(GameObject fireEffect)
    {
        if (fireLightIntensity <= 0f || fireLightRange <= 0f || fireEffect.GetComponentInChildren<Light>() != null)
        {
            return;
        }

        GameObject lightObject = new GameObject("CellFireLight");
        lightObject.transform.SetParent(fireEffect.transform, false);
        lightObject.transform.localPosition = Vector3.up * 0.25f;

        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.45f, 0.12f);
        light.range = fireLightRange;
        light.intensity = fireLightIntensity;
        light.shadows = LightShadows.None;
    }

    private Material ResolveMaterial(FireCell cell)
    {
        if (cell.state == CellState.BurnedOut && burnedMaterial != null)
        {
            return burnedMaterial;
        }

        switch (cell.material)
        {
            case FireMaterial.Stone:
                return stoneMaterial;
            case FireMaterial.Water:
                return waterMaterial;
            case FireMaterial.Oil:
                return oilMaterial;
            default:
                return woodMaterial;
        }
    }

    private static bool CanBurn(FireMaterial material)
    {
        return material != FireMaterial.Water;
    }

    private float GetMaxHp(FireMaterial material)
    {
        switch (material)
        {
            case FireMaterial.Stone:
                return initialHp * 2.75f;
            case FireMaterial.Oil:
                return initialHp * 0.65f;
            default:
                return initialHp;
        }
    }

    private static float GetBurnDamageMultiplier(FireMaterial material)
    {
        switch (material)
        {
            case FireMaterial.Stone:
                return 0.28f;
            case FireMaterial.Water:
                return 0f;
            case FireMaterial.Oil:
                return 1.8f;
            default:
                return 1f;
        }
    }

    private static float GetSpreadHeatMultiplier(FireMaterial material)
    {
        switch (material)
        {
            case FireMaterial.Stone:
                return 0.04f;
            case FireMaterial.Oil:
                return 1.7f;
            default:
                return 1f;
        }
    }

    private static float GetHeatGainMultiplier(FireMaterial material)
    {
        switch (material)
        {
            case FireMaterial.Stone:
                return 0.04f;
            case FireMaterial.Water:
                return 0f;
            default:
                return 1f;
        }
    }

    private static float GetIgnitionChanceMultiplier(FireMaterial material)
    {
        switch (material)
        {
            case FireMaterial.Stone:
                return 0.015f;
            case FireMaterial.Water:
                return 0f;
            case FireMaterial.Oil:
                return 1.8f;
            default:
                return 1f;
        }
    }

    private bool IsInside(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
