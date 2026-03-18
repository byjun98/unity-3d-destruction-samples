using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public sealed class VoxelDestructibleBuilding : MonoBehaviour
{
    private enum VoxelMaterialKind
    {
        Concrete,
        Brick,
        Wood,
        Roof,
        Glass
    }

    private sealed class VoxelCell
    {
        public bool active;
        public float hp;
        public VoxelMaterialKind material;
    }

    private readonly struct Face
    {
        public readonly Vector3Int normal;
        public readonly Vector3Int[] corners;

        public Face(Vector3Int normal, Vector3Int a, Vector3Int b, Vector3Int c, Vector3Int d)
        {
            this.normal = normal;
            corners = new[] { a, b, c, d };
        }
    }

    [Header("Grid")]
    [SerializeField, Min(4)] private int width = 12;
    [SerializeField, Min(4)] private int height = 8;
    [SerializeField, Min(4)] private int depth = 8;
    [SerializeField, Min(0.2f)] private float cellSize = 0.45f;

    [Header("Voxel Materials")]
    [SerializeField] private Material concreteMaterial;
    [SerializeField] private Material brickMaterial;
    [SerializeField] private Material woodMaterial;
    [SerializeField] private Material roofMaterial;
    [SerializeField] private Material glassMaterial;

    [Header("Effects")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private GameObject dustEffectPrefab;
    [SerializeField, Min(0)] private int maxDebrisPerBlast = 18;
    [SerializeField, Min(0f)] private float debrisLifetime = 4f;
    [SerializeField, Range(0.05f, 0.5f)] private float debrisScale = 0.22f;

    [Header("Chunk Physics")]
    [SerializeField, Min(0)] private int minDetachedVoxels = 2;
    [SerializeField, Min(1)] private int maxActiveDetachedChunks = 8;
    [SerializeField, Min(1)] private int maxCompoundColliders = 48;
    [SerializeField, Min(0f)] private float detachedChunkLifetime = 10f;
    [SerializeField, Min(0f)] private float detachedChunkMassPerVoxel = 0.08f;
    [SerializeField, Min(0f)] private float detachedChunkDrag = 0.05f;
    [SerializeField, Min(0f)] private float detachedChunkAngularDrag = 0.15f;

    private static readonly Vector3Int[] NeighborOffsets =
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 0, -1)
    };

    private static readonly Face[] Faces =
    {
        new Face(
            new Vector3Int(1, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, 1, 1),
            new Vector3Int(1, 0, 1)),
        new Face(
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 1, 1),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, 0, 0)),
        new Face(
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, 1, 1),
            new Vector3Int(1, 1, 1),
            new Vector3Int(1, 1, 0),
            new Vector3Int(0, 1, 0)),
        new Face(
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(1, 0, 1),
            new Vector3Int(0, 0, 1)),
        new Face(
            new Vector3Int(0, 0, 1),
            new Vector3Int(1, 0, 1),
            new Vector3Int(1, 1, 1),
            new Vector3Int(0, 1, 1),
            new Vector3Int(0, 0, 1)),
        new Face(
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(1, 0, 0))
    };

    private readonly List<GameObject> detachedChunks = new List<GameObject>();
    private readonly List<Vector3Int> floodBuffer = new List<Vector3Int>();
    private readonly Queue<Vector3Int> floodQueue = new Queue<Vector3Int>();

    private VoxelCell[,,] cells;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Material[] runtimeMaterials;
    private Mesh generatedMesh;
    private int explosionSequence;

    public int ActiveVoxelCount { get; private set; }
    public int DetachedChunkCount => detachedChunks.Count;

    private void OnEnable()
    {
        EnsureComponents();
        ResetBuilding();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            ClearGeneratedMesh();
        }
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            ResetBuilding();
        }
    }

    [ContextMenu("Reset Voxel Building")]
    public void ResetBuilding()
    {
        EnsureComponents();
        EnsureMaterials();
        CreateInitialCells();
        RebuildMainMesh();
    }

    public void ApplyExplosion(Vector3 worldPoint, float radius, float damage, float impulse)
    {
        if (cells == null || radius <= 0f || damage <= 0f)
        {
            return;
        }

        SpawnEffect(explosionEffectPrefab, worldPoint, radius);

        List<Vector3Int> removedCells = new List<Vector3Int>();
        float radiusSqr = radius * radius;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    VoxelCell cell = cells[x, y, z];

                    if (cell == null || !cell.active)
                    {
                        continue;
                    }

                    Vector3 cellWorld = transform.TransformPoint(GetCellCenterLocal(new Vector3Int(x, y, z), Vector3.zero));
                    float distanceSqr = (cellWorld - worldPoint).sqrMagnitude;

                    if (distanceSqr > radiusSqr)
                    {
                        continue;
                    }

                    float distance = Mathf.Sqrt(distanceSqr);
                    float falloff = Mathf.Clamp01(1f - distance / radius);
                    float materialResponse = GetDamageResponse(cell.material);
                    cell.hp -= damage * Mathf.Lerp(0.35f, 1f, falloff) * materialResponse;

                    if (cell.hp <= 0f)
                    {
                        cell.active = false;
                        removedCells.Add(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        if (removedCells.Count == 0)
        {
            RebuildMainMesh();
            return;
        }

        SpawnRemovedVoxelDebris(removedCells, worldPoint, impulse);
        DetachUnsupportedGroups(worldPoint, impulse);
        RebuildMainMesh();
    }

    private void EnsureComponents()
    {
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (meshCollider == null)
        {
            meshCollider = GetComponent<MeshCollider>();
        }
    }

    private void EnsureMaterials()
    {
        runtimeMaterials = new[]
        {
            ResolveMaterial(concreteMaterial, "Voxel_Concrete_Runtime", new Color(0.46f, 0.48f, 0.48f)),
            ResolveMaterial(brickMaterial, "Voxel_Brick_Runtime", new Color(0.56f, 0.28f, 0.22f)),
            ResolveMaterial(woodMaterial, "Voxel_Wood_Runtime", new Color(0.55f, 0.36f, 0.18f)),
            ResolveMaterial(roofMaterial, "Voxel_Roof_Runtime", new Color(0.18f, 0.21f, 0.24f)),
            ResolveMaterial(glassMaterial, "Voxel_Glass_Runtime", new Color(0.54f, 0.8f, 0.9f, 0.55f))
        };

        meshRenderer.sharedMaterials = runtimeMaterials;
    }

    private Material ResolveMaterial(Material material, string materialName, Color fallbackColor)
    {
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Standard");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Lit");
        }

        Material fallback = new Material(shader)
        {
            name = materialName
        };

        if (fallback.HasProperty("_BaseColor"))
        {
            fallback.SetColor("_BaseColor", fallbackColor);
        }
        else if (fallback.HasProperty("_Color"))
        {
            fallback.SetColor("_Color", fallbackColor);
        }

        return fallback;
    }

    private void CreateInitialCells()
    {
        cells = new VoxelCell[width, height, depth];

        int roofY = Mathf.Clamp(height - 3, 3, height - 2);
        int beamY = roofY - 1;
        int leftX = 2;
        int rightX = width - 3;
        int frontZ = 2;
        int backZ = depth - 3;

        for (int y = 0; y <= beamY; y++)
        {
            SetCell(leftX, y, frontZ, VoxelMaterialKind.Concrete);
            SetCell(rightX, y, frontZ, VoxelMaterialKind.Concrete);
            SetCell(leftX, y, backZ, VoxelMaterialKind.Concrete);
            SetCell(rightX, y, backZ, VoxelMaterialKind.Concrete);
        }

        for (int x = leftX; x <= rightX; x++)
        {
            SetCell(x, beamY, frontZ, VoxelMaterialKind.Wood);
            SetCell(x, beamY, backZ, VoxelMaterialKind.Wood);
        }

        for (int z = frontZ; z <= backZ; z++)
        {
            SetCell(leftX, beamY, z, VoxelMaterialKind.Wood);
            SetCell(rightX, beamY, z, VoxelMaterialKind.Wood);
        }

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < depth - 1; z++)
            {
                SetCell(x, roofY, z, VoxelMaterialKind.Brick);

                bool isRim = x == 1 || x == width - 2 || z == 1 || z == depth - 2;

                if (isRim && roofY + 1 < height)
                {
                    SetCell(x, roofY + 1, z, VoxelMaterialKind.Roof);
                }
            }
        }

        for (int x = 4; x <= width - 5; x++)
        {
            for (int y = 1; y <= beamY - 1; y++)
            {
                SetCell(x, y, frontZ, y == 2 ? VoxelMaterialKind.Glass : VoxelMaterialKind.Wood);
                SetCell(x, y, backZ, y == 2 ? VoxelMaterialKind.Glass : VoxelMaterialKind.Wood);
            }
        }
    }

    private void SetCell(int x, int y, int z, VoxelMaterialKind material)
    {
        if (!IsInside(x, y, z))
        {
            return;
        }

        cells[x, y, z] = new VoxelCell
        {
            active = true,
            hp = GetMaxHp(material),
            material = material
        };
    }

    private void DetachUnsupportedGroups(Vector3 explosionPoint, float impulse)
    {
        bool[,,] visited = new bool[width, height, depth];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (visited[x, y, z] || !IsActive(x, y, z))
                    {
                        continue;
                    }

                    bool anchored = FloodFillComponent(new Vector3Int(x, y, z), visited, floodBuffer);

                    if (anchored || floodBuffer.Count < minDetachedVoxels)
                    {
                        continue;
                    }

                    List<Vector3Int> detachedCells = new List<Vector3Int>(floodBuffer);

                    for (int i = 0; i < detachedCells.Count; i++)
                    {
                        Vector3Int position = detachedCells[i];
                        cells[position.x, position.y, position.z].active = false;
                    }

                    if (Application.isPlaying)
                    {
                        CreateDetachedChunk(detachedCells, explosionPoint, impulse);
                    }
                }
            }
        }
    }

    private bool FloodFillComponent(Vector3Int start, bool[,,] visited, List<Vector3Int> component)
    {
        component.Clear();
        floodQueue.Clear();
        floodQueue.Enqueue(start);
        visited[start.x, start.y, start.z] = true;

        bool anchored = false;

        while (floodQueue.Count > 0)
        {
            Vector3Int current = floodQueue.Dequeue();
            component.Add(current);
            anchored |= current.y == 0;

            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                Vector3Int next = current + NeighborOffsets[i];

                if (!IsInside(next.x, next.y, next.z) || visited[next.x, next.y, next.z] || !IsActive(next.x, next.y, next.z))
                {
                    continue;
                }

                visited[next.x, next.y, next.z] = true;
                floodQueue.Enqueue(next);
            }
        }

        return anchored;
    }

    private void CreateDetachedChunk(List<Vector3Int> chunkCells, Vector3 explosionPoint, float impulse)
    {
        if (chunkCells.Count == 0)
        {
            return;
        }

        PruneDetachedChunks();

        while (detachedChunks.Count >= maxActiveDetachedChunks)
        {
            GameObject oldest = detachedChunks[0];
            detachedChunks.RemoveAt(0);

            if (oldest != null)
            {
                Destroy(oldest);
            }
        }

        Bounds gridBounds = GetGridBounds(chunkCells);
        Vector3 chunkLocalCenter = GridPointToLocal(gridBounds.center);
        Mesh chunkMesh = BuildMesh(chunkCells, chunkLocalCenter, false);

        GameObject chunkObject = new GameObject("VoxelDetachedChunk_" + explosionSequence++);
        chunkObject.transform.SetPositionAndRotation(transform.TransformPoint(chunkLocalCenter), transform.rotation);
        chunkObject.transform.localScale = transform.lossyScale;

        MeshFilter chunkFilter = chunkObject.AddComponent<MeshFilter>();
        chunkFilter.sharedMesh = chunkMesh;

        MeshRenderer chunkRenderer = chunkObject.AddComponent<MeshRenderer>();
        chunkRenderer.sharedMaterials = runtimeMaterials;

        AddChunkColliders(chunkObject, chunkCells, chunkLocalCenter);

        Rigidbody body = chunkObject.AddComponent<Rigidbody>();
        body.mass = Mathf.Max(0.1f, chunkCells.Count * detachedChunkMassPerVoxel);
        body.drag = detachedChunkDrag;
        body.angularDrag = detachedChunkAngularDrag;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.AddExplosionForce(impulse, explosionPoint, Mathf.Max(cellSize, cellSize * 6f), cellSize * 0.5f, ForceMode.Impulse);
        body.AddTorque(Random.onUnitSphere * impulse * 0.2f, ForceMode.Impulse);

        detachedChunks.Add(chunkObject);

        if (detachedChunkLifetime > 0f)
        {
            Destroy(chunkObject, detachedChunkLifetime);
        }
    }

    private void AddChunkColliders(GameObject chunkObject, List<Vector3Int> chunkCells, Vector3 chunkLocalCenter)
    {
        if (chunkCells.Count > maxCompoundColliders)
        {
            Bounds bounds = GetGridBounds(chunkCells);
            BoxCollider collider = chunkObject.AddComponent<BoxCollider>();
            collider.center = GridPointToLocal(bounds.center) - chunkLocalCenter;
            collider.size = new Vector3(bounds.size.x, bounds.size.y, bounds.size.z) * cellSize;
            return;
        }

        for (int i = 0; i < chunkCells.Count; i++)
        {
            BoxCollider collider = chunkObject.AddComponent<BoxCollider>();
            collider.center = GetCellCenterLocal(chunkCells[i], chunkLocalCenter);
            collider.size = Vector3.one * cellSize * 0.96f;
        }
    }

    private void RebuildMainMesh()
    {
        if (cells == null)
        {
            return;
        }

        List<Vector3Int> activeCells = new List<Vector3Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (IsActive(x, y, z))
                    {
                        activeCells.Add(new Vector3Int(x, y, z));
                    }
                }
            }
        }

        ActiveVoxelCount = activeCells.Count;
        ClearGeneratedMesh();

        generatedMesh = BuildMesh(activeCells, Vector3.zero, true);
        meshFilter.sharedMesh = generatedMesh;
        meshRenderer.sharedMaterials = runtimeMaterials;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = generatedMesh;
    }

    private Mesh BuildMesh(List<Vector3Int> sourceCells, Vector3 localOffset, bool useGlobalCellsForNeighbors)
    {
        Mesh mesh = new Mesh
        {
            name = "VoxelSurfaceMesh"
        };

        Dictionary<Vector3Int, int> vertexLookup = new Dictionary<Vector3Int, int>();
        HashSet<Vector3Int> sourceLookup = useGlobalCellsForNeighbors ? null : new HashSet<Vector3Int>(sourceCells);
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int>[] submeshTriangles = new List<int>[runtimeMaterials.Length];

        for (int i = 0; i < submeshTriangles.Length; i++)
        {
            submeshTriangles[i] = new List<int>();
        }

        for (int i = 0; i < sourceCells.Count; i++)
        {
            Vector3Int cellPosition = sourceCells[i];
            VoxelCell cell = cells[cellPosition.x, cellPosition.y, cellPosition.z];
            int materialIndex = Mathf.Clamp((int)cell.material, 0, runtimeMaterials.Length - 1);

            for (int faceIndex = 0; faceIndex < Faces.Length; faceIndex++)
            {
                Face face = Faces[faceIndex];
                Vector3Int neighbor = cellPosition + face.normal;

                if (HasNeighbor(neighbor, useGlobalCellsForNeighbors, sourceLookup))
                {
                    continue;
                }

                AddFace(mesh, cellPosition, face, materialIndex, localOffset, vertexLookup, vertices, uvs, submeshTriangles);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.subMeshCount = submeshTriangles.Length;

        for (int i = 0; i < submeshTriangles.Length; i++)
        {
            mesh.SetTriangles(submeshTriangles[i], i);
        }

        mesh.RecalculateNormals();
        SoftenNormals(mesh);
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void AddFace(
        Mesh mesh,
        Vector3Int cellPosition,
        Face face,
        int materialIndex,
        Vector3 localOffset,
        Dictionary<Vector3Int, int> vertexLookup,
        List<Vector3> vertices,
        List<Vector2> uvs,
        List<int>[] submeshTriangles)
    {
        int[] indices = new int[4];
        Vector3[] positions = new Vector3[4];

        for (int i = 0; i < face.corners.Length; i++)
        {
            Vector3Int gridVertex = cellPosition + face.corners[i];

            if (!vertexLookup.TryGetValue(gridVertex, out int vertexIndex))
            {
                vertexIndex = vertices.Count;
                Vector3 position = GridPointToLocal(gridVertex) - localOffset;
                vertexLookup.Add(gridVertex, vertexIndex);
                vertices.Add(position);
                uvs.Add(new Vector2(position.x, position.z) * 0.35f);
            }

            indices[i] = vertexIndex;
            positions[i] = vertices[vertexIndex];
        }

        Vector3 expectedNormal = face.normal;
        Vector3 calculatedNormal = Vector3.Cross(positions[1] - positions[0], positions[2] - positions[0]);
        List<int> triangles = submeshTriangles[materialIndex];

        if (Vector3.Dot(calculatedNormal, expectedNormal) >= 0f)
        {
            triangles.Add(indices[0]);
            triangles.Add(indices[1]);
            triangles.Add(indices[2]);
            triangles.Add(indices[0]);
            triangles.Add(indices[2]);
            triangles.Add(indices[3]);
        }
        else
        {
            triangles.Add(indices[0]);
            triangles.Add(indices[2]);
            triangles.Add(indices[1]);
            triangles.Add(indices[0]);
            triangles.Add(indices[3]);
            triangles.Add(indices[2]);
        }
    }

    private void SoftenNormals(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Dictionary<Vector3, Vector3> normalByPosition = new Dictionary<Vector3, Vector3>();

        for (int i = 0; i < vertices.Length; i++)
        {
            if (!normalByPosition.ContainsKey(vertices[i]))
            {
                normalByPosition.Add(vertices[i], Vector3.zero);
            }

            normalByPosition[vertices[i]] += normals[i];
        }

        for (int i = 0; i < vertices.Length; i++)
        {
            normals[i] = normalByPosition[vertices[i]].normalized;
        }

        mesh.normals = normals;
    }

    private bool HasNeighbor(Vector3Int neighbor, bool useGlobalCellsForNeighbors, HashSet<Vector3Int> sourceLookup)
    {
        if (useGlobalCellsForNeighbors)
        {
            return IsInside(neighbor.x, neighbor.y, neighbor.z) && IsActive(neighbor.x, neighbor.y, neighbor.z);
        }

        return sourceLookup != null && sourceLookup.Contains(neighbor);
    }

    private void SpawnRemovedVoxelDebris(List<Vector3Int> removedCells, Vector3 explosionPoint, float impulse)
    {
        if (!Application.isPlaying || maxDebrisPerBlast <= 0)
        {
            return;
        }

        SpawnEffect(dustEffectPrefab, explosionPoint, 1f);

        int spawnCount = Mathf.Min(maxDebrisPerBlast, removedCells.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3Int cellPosition = removedCells[(i * removedCells.Count) / spawnCount];
            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.name = "VoxelDustChip";
            debris.transform.position = transform.TransformPoint(GetCellCenterLocal(cellPosition, Vector3.zero));
            debris.transform.rotation = Random.rotation;
            debris.transform.localScale = Vector3.one * cellSize * debrisScale;

            Renderer renderer = debris.GetComponent<Renderer>();

            if (renderer != null && runtimeMaterials != null && runtimeMaterials.Length > 0)
            {
                int materialIndex = Mathf.Clamp((int)cells[cellPosition.x, cellPosition.y, cellPosition.z].material, 0, runtimeMaterials.Length - 1);
                renderer.sharedMaterial = runtimeMaterials[materialIndex];
            }

            Rigidbody body = debris.AddComponent<Rigidbody>();
            body.mass = 0.04f;
            body.AddExplosionForce(impulse * 0.65f, explosionPoint, cellSize * 5f, cellSize, ForceMode.Impulse);

            if (debrisLifetime > 0f)
            {
                Destroy(debris, debrisLifetime);
            }
        }
    }

    private void SpawnEffect(GameObject prefab, Vector3 position, float scale)
    {
        if (!Application.isPlaying || prefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(prefab, position, Quaternion.identity);
        effect.transform.localScale *= Mathf.Max(0.2f, scale);
        Destroy(effect, 5f);
    }

    private Bounds GetGridBounds(List<Vector3Int> positions)
    {
        Vector3 min = positions[0];
        Vector3 max = positions[0] + Vector3.one;

        for (int i = 1; i < positions.Count; i++)
        {
            Vector3 cellMin = positions[i];
            Vector3 cellMax = cellMin + Vector3.one;
            min = Vector3.Min(min, cellMin);
            max = Vector3.Max(max, cellMax);
        }

        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        return bounds;
    }

    private Vector3 GridPointToLocal(Vector3 gridPoint)
    {
        return new Vector3(
            (gridPoint.x - width * 0.5f) * cellSize,
            gridPoint.y * cellSize,
            (gridPoint.z - depth * 0.5f) * cellSize);
    }

    private Vector3 GetCellCenterLocal(Vector3Int cellPosition, Vector3 localOffset)
    {
        return GridPointToLocal(new Vector3(cellPosition.x + 0.5f, cellPosition.y + 0.5f, cellPosition.z + 0.5f)) - localOffset;
    }

    private bool IsActive(int x, int y, int z)
    {
        VoxelCell cell = cells[x, y, z];
        return cell != null && cell.active;
    }

    private bool IsInside(int x, int y, int z)
    {
        return x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < depth;
    }

    private float GetMaxHp(VoxelMaterialKind material)
    {
        switch (material)
        {
            case VoxelMaterialKind.Concrete:
                return 130f;
            case VoxelMaterialKind.Brick:
                return 85f;
            case VoxelMaterialKind.Wood:
                return 55f;
            case VoxelMaterialKind.Roof:
                return 95f;
            case VoxelMaterialKind.Glass:
                return 24f;
            default:
                return 80f;
        }
    }

    private float GetDamageResponse(VoxelMaterialKind material)
    {
        switch (material)
        {
            case VoxelMaterialKind.Concrete:
                return 0.85f;
            case VoxelMaterialKind.Brick:
                return 1f;
            case VoxelMaterialKind.Wood:
                return 1.25f;
            case VoxelMaterialKind.Roof:
                return 0.95f;
            case VoxelMaterialKind.Glass:
                return 2.4f;
            default:
                return 1f;
        }
    }

    private void PruneDetachedChunks()
    {
        for (int i = detachedChunks.Count - 1; i >= 0; i--)
        {
            if (detachedChunks[i] == null)
            {
                detachedChunks.RemoveAt(i);
            }
        }
    }

    private void ClearGeneratedMesh()
    {
        if (meshFilter != null)
        {
            meshFilter.sharedMesh = null;
        }

        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
        }

        if (generatedMesh == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedMesh);
        }
        else
        {
            DestroyImmediate(generatedMesh);
        }

        generatedMesh = null;
    }
}
