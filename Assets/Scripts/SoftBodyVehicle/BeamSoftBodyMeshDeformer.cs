using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BeamSoftBodyMeshDeformer : MonoBehaviour
{
    [SerializeField] private BeamSoftBodyVehicle softBody;
    [SerializeField] private Transform vehicleRoot;
    [SerializeField] private MeshFilter[] targetMeshFilters;
    [SerializeField, Min(1)] private int influenceCount = 4;
    [SerializeField, Min(0.05f)] private float influenceRadius = 1.6f;
    [SerializeField, Min(0f)] private float displacementMultiplier = 1f;
    [SerializeField, Min(0f)] private float falloffPower = 2f;
    [SerializeField] private bool recalculateNormals = true;

    private struct VertexBinding
    {
        public int nodeAIndex;
        public int nodeBIndex;
        public int nodeCIndex;
        public int nodeDIndex;
        public float weightA;
        public float weightB;
        public float weightC;
        public float weightD;
    }

    private sealed class MeshState
    {
        public MeshFilter filter;
        public Mesh runtimeMesh;
        public Vector3[] originalVertices;
        public Vector3[] workingVertices;
        public VertexBinding[] bindings;
        public Matrix4x4 vehicleToMesh;
        public Matrix4x4 meshToVehicle;
    }

    private readonly List<MeshState> meshStates = new List<MeshState>();
    private bool initialized;

    public void ConfigureForDemo(BeamSoftBodyVehicle softBodyComponent, Transform root, MeshFilter[] filters)
    {
        softBody = softBodyComponent;
        vehicleRoot = root;
        targetMeshFilters = filters;
    }

    private void Start()
    {
        Initialize();
    }

    private void OnDisable()
    {
        for (int i = 0; i < meshStates.Count; i++)
        {
            MeshState s = meshStates[i];

            if (s == null || s.runtimeMesh == null || s.filter == null)
            {
                continue;
            }

            s.filter.sharedMesh = null;
        }

        meshStates.Clear();
        initialized = false;
    }

    private void LateUpdate()
    {
        if (!initialized || softBody == null)
        {
            return;
        }

        for (int s = 0; s < meshStates.Count; s++)
        {
            MeshState state = meshStates[s];

            if (state == null || state.runtimeMesh == null)
            {
                continue;
            }

            UpdateMeshVertices(state);
        }
    }

    private void Initialize()
    {
        if (softBody == null || vehicleRoot == null)
        {
            return;
        }

        if (targetMeshFilters == null || targetMeshFilters.Length == 0)
        {
            return;
        }

        BeamSoftBodyVehicle.SoftNode[] nodes = softBody.GetSoftNodesReadOnly();

        if (nodes == null || nodes.Length == 0)
        {
            return;
        }

        meshStates.Clear();

        for (int i = 0; i < targetMeshFilters.Length; i++)
        {
            MeshFilter filter = targetMeshFilters[i];

            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            MeshState state = BuildMeshState(filter, nodes);

            if (state != null)
            {
                meshStates.Add(state);
            }
        }

        initialized = meshStates.Count > 0;
    }

    private MeshState BuildMeshState(MeshFilter filter, BeamSoftBodyVehicle.SoftNode[] nodes)
    {
        Mesh source = filter.sharedMesh;
        Mesh runtime = Instantiate(source);
        runtime.name = source.name + "_BeamSoftDeform";
        runtime.MarkDynamic();
        filter.sharedMesh = runtime;

        Vector3[] originalVertices = source.vertices;
        VertexBinding[] bindings = new VertexBinding[originalVertices.Length];

        Matrix4x4 meshToVehicle = vehicleRoot.worldToLocalMatrix * filter.transform.localToWorldMatrix;
        Matrix4x4 vehicleToMesh = meshToVehicle.inverse;

        for (int v = 0; v < originalVertices.Length; v++)
        {
            Vector3 vertexInVehicleLocal = meshToVehicle.MultiplyPoint3x4(originalVertices[v]);
            bindings[v] = ComputeBinding(vertexInVehicleLocal, nodes);
        }

        return new MeshState
        {
            filter = filter,
            runtimeMesh = runtime,
            originalVertices = originalVertices,
            workingVertices = new Vector3[originalVertices.Length],
            bindings = bindings,
            vehicleToMesh = vehicleToMesh,
            meshToVehicle = meshToVehicle
        };
    }

    private VertexBinding ComputeBinding(Vector3 vertexInVehicleLocal, BeamSoftBodyVehicle.SoftNode[] nodes)
    {
        int bestA = 0;
        int bestB = 0;
        int bestC = 0;
        int bestD = 0;
        float bestDistA = float.MaxValue;
        float bestDistB = float.MaxValue;
        float bestDistC = float.MaxValue;
        float bestDistD = float.MaxValue;

        for (int n = 0; n < nodes.Length; n++)
        {
            if (nodes[n].wheelNode)
            {
                continue;
            }

            float distSqr = (nodes[n].restLocalPosition - vertexInVehicleLocal).sqrMagnitude;

            if (distSqr < bestDistA)
            {
                bestDistD = bestDistC;
                bestD = bestC;
                bestDistC = bestDistB;
                bestC = bestB;
                bestDistB = bestDistA;
                bestB = bestA;
                bestDistA = distSqr;
                bestA = n;
            }
            else if (distSqr < bestDistB)
            {
                bestDistD = bestDistC;
                bestD = bestC;
                bestDistC = bestDistB;
                bestC = bestB;
                bestDistB = distSqr;
                bestB = n;
            }
            else if (distSqr < bestDistC)
            {
                bestDistD = bestDistC;
                bestD = bestC;
                bestDistC = distSqr;
                bestC = n;
            }
            else if (distSqr < bestDistD)
            {
                bestDistD = distSqr;
                bestD = n;
            }
        }

        float invRadius = 1f / Mathf.Max(0.05f, influenceRadius);
        float weightA = ComputeWeight(Mathf.Sqrt(bestDistA) * invRadius);
        float weightB = ComputeWeight(Mathf.Sqrt(bestDistB) * invRadius);
        float weightC = ComputeWeight(Mathf.Sqrt(bestDistC) * invRadius);
        float weightD = ComputeWeight(Mathf.Sqrt(bestDistD) * invRadius);
        float totalWeight = weightA + weightB + weightC + weightD;

        if (totalWeight < 0.0001f)
        {
            weightA = 1f;
            weightB = 0f;
            weightC = 0f;
            weightD = 0f;
        }
        else
        {
            float invTotal = 1f / totalWeight;
            weightA *= invTotal;
            weightB *= invTotal;
            weightC *= invTotal;
            weightD *= invTotal;
        }

        return new VertexBinding
        {
            nodeAIndex = bestA,
            nodeBIndex = bestB,
            nodeCIndex = bestC,
            nodeDIndex = bestD,
            weightA = weightA,
            weightB = weightB,
            weightC = weightC,
            weightD = weightD
        };
    }

    private float ComputeWeight(float normalizedDistance)
    {
        float clamped = Mathf.Clamp01(1f - normalizedDistance);
        return Mathf.Pow(clamped, falloffPower);
    }

    private void UpdateMeshVertices(MeshState state)
    {
        BeamSoftBodyVehicle.SoftNode[] nodes = softBody.GetSoftNodesReadOnly();

        if (nodes == null || nodes.Length == 0)
        {
            return;
        }

        Vector3[] vertices = state.workingVertices;
        Vector3[] originals = state.originalVertices;
        VertexBinding[] bindings = state.bindings;

        for (int v = 0; v < vertices.Length; v++)
        {
            VertexBinding binding = bindings[v];

            Vector3 displacementA = nodes[binding.nodeAIndex].localPosition - nodes[binding.nodeAIndex].restLocalPosition;
            Vector3 displacementB = nodes[binding.nodeBIndex].localPosition - nodes[binding.nodeBIndex].restLocalPosition;
            Vector3 displacementC = nodes[binding.nodeCIndex].localPosition - nodes[binding.nodeCIndex].restLocalPosition;
            Vector3 displacementD = nodes[binding.nodeDIndex].localPosition - nodes[binding.nodeDIndex].restLocalPosition;

            Vector3 totalDisplacement =
                displacementA * binding.weightA +
                displacementB * binding.weightB +
                displacementC * binding.weightC +
                displacementD * binding.weightD;

            totalDisplacement *= displacementMultiplier;

            Vector3 vertexInVehicleLocal = state.meshToVehicle.MultiplyPoint3x4(originals[v]) + totalDisplacement;
            vertices[v] = state.vehicleToMesh.MultiplyPoint3x4(vertexInVehicleLocal);
        }

        state.runtimeMesh.vertices = vertices;

        if (recalculateNormals)
        {
            state.runtimeMesh.RecalculateNormals();
        }

        state.runtimeMesh.RecalculateBounds();
    }
}
