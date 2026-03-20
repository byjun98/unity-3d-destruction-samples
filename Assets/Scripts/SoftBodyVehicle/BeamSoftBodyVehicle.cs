using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BeamSoftBodyVehicle : MonoBehaviour
{
    [System.Serializable]
    public sealed class SoftNode
    {
        public string nodeName;
        public Vector3 restLocalPosition;
        public bool anchored;
        public bool wheelNode;
        [Min(0.01f)] public float mass = 1f;

        [System.NonSerialized] public Vector3 localPosition;
        [System.NonSerialized] public Vector3 velocity;
        [System.NonSerialized] public float damage;
        [System.NonSerialized] public Transform marker;
    }

    [System.Serializable]
    public sealed class SoftBeam
    {
        public int nodeA;
        public int nodeB;
        [Min(0f)] public float stiffness = 80f;
        [Min(0f)] public float damping = 7f;
        [Min(0f)] public float yieldStrain = 0.16f;
        [Min(0f)] public float plasticity = 1.8f;
        [Min(0f)] public float breakStrain = 0.75f;

        [System.NonSerialized] public float restLength;
        [System.NonSerialized] public float strain;
        [System.NonSerialized] public bool broken;
        [System.NonSerialized] public LineRenderer line;
    }

    [System.Serializable]
    public sealed class SoftPanel
    {
        public string panelName;
        public Transform panel;
        public int[] nodeIndices;
        [Tooltip("0=X, 1=Y, 2=Z")]
        [Range(0, 2)] public int compressionAxis = 2;
        [Range(0.05f, 1f)] public float minimumAxisScale = 0.35f;
        [Min(0f)] public float followStrength = 0.85f;

        [System.NonSerialized] public Vector3 originalLocalPosition;
        [System.NonSerialized] public Quaternion originalLocalRotation;
        [System.NonSerialized] public Vector3 originalLocalScale;
    }

    [Header("Soft Body")]
    [SerializeField] private SoftNode[] nodes;
    [SerializeField] private SoftBeam[] beams;
    [SerializeField] private SoftPanel[] panels;
    [SerializeField, Min(1)] private int solverIterations = 3;
    [SerializeField, Min(0f)] private float nodeDrag = 5f;
    [SerializeField, Min(0f)] private float anchorStiffness = 180f;
    [SerializeField, Min(0f)] private float anchorDamping = 14f;
    [SerializeField, Min(0.05f)] private float maxNodeDisplacement = 1.15f;

    [Header("Impact")]
    [SerializeField, Min(0f)] private float directDeformation = 0.045f;
    [SerializeField, Min(0f)] private float velocityScale = 0.7f;
    [SerializeField, Min(0f)] private float damageScale = 0.075f;

    [Header("Visuals")]
    [SerializeField] private Material bodyNodeMaterial;
    [SerializeField] private Material wheelNodeMaterial;
    [SerializeField] private Material beamMaterial;
    [SerializeField] private Material damagedBeamMaterial;
    [SerializeField, Min(0.015f)] private float bodyNodeRadius = 0.07f;
    [SerializeField, Min(0.015f)] private float wheelNodeRadius = 0.055f;
    [SerializeField, Min(0.002f)] private float beamWidth = 0.018f;

    private const string NodeGroupName = "BeamSoftBody_NodeVisuals";
    private const string BeamGroupName = "BeamSoftBody_BeamVisuals";

    public int NodeCount
    {
        get { return nodes != null ? nodes.Length : 0; }
    }

    public int BeamCount
    {
        get { return beams != null ? beams.Length : 0; }
    }

    public SoftNode[] GetSoftNodesReadOnly()
    {
        return nodes;
    }

    public float AverageDamage
    {
        get
        {
            if (nodes == null || nodes.Length == 0)
            {
                return 0f;
            }

            float total = 0f;

            for (int i = 0; i < nodes.Length; i++)
            {
                total += nodes[i].damage;
            }

            return total / nodes.Length;
        }
    }

    private void Awake()
    {
        InitializeRuntimeState();
        CachePanels();
    }

    private void Start()
    {
        RebuildVisuals();
        UpdateVisuals();
    }

    private void FixedUpdate()
    {
        if (nodes == null || beams == null)
        {
            return;
        }

        float substep = Time.fixedDeltaTime / Mathf.Max(1, solverIterations);

        for (int i = 0; i < solverIterations; i++)
        {
            SolveBeams(substep);
            SolveAnchors(substep);
            IntegrateNodes(substep);
        }

        UpdatePanels();
        UpdateVisuals();
    }

    public void ConfigureForDemo(
        SoftNode[] configuredNodes,
        SoftBeam[] configuredBeams,
        SoftPanel[] configuredPanels,
        Material configuredBodyNodeMaterial,
        Material configuredWheelNodeMaterial,
        Material configuredBeamMaterial,
        Material configuredDamagedBeamMaterial)
    {
        nodes = configuredNodes;
        beams = configuredBeams;
        panels = configuredPanels;
        bodyNodeMaterial = configuredBodyNodeMaterial;
        wheelNodeMaterial = configuredWheelNodeMaterial;
        beamMaterial = configuredBeamMaterial;
        damagedBeamMaterial = configuredDamagedBeamMaterial;
        InitializeRuntimeState();
        CachePanels();
    }

    public void ApplyImpact(Vector3 worldPoint, Vector3 worldDirection, float force, float radius)
    {
        if (nodes == null || nodes.Length == 0)
        {
            return;
        }

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        Vector3 localDirection = transform.InverseTransformDirection(worldDirection);

        if (localDirection.sqrMagnitude < 0.001f)
        {
            localDirection = Vector3.back;
        }

        localDirection.Normalize();
        float clampedRadius = Mathf.Max(0.05f, radius);
        float clampedForce = Mathf.Max(0f, force);

        for (int i = 0; i < nodes.Length; i++)
        {
            SoftNode node = nodes[i];
            float distance = Vector3.Distance(node.localPosition, localPoint);

            if (distance > clampedRadius)
            {
                continue;
            }

            float falloff = 1f - distance / clampedRadius;
            falloff *= falloff;
            float inverseMass = node.anchored ? 0.15f : 1f / Mathf.Max(0.01f, node.mass);
            Vector3 displacement = localDirection * clampedForce * directDeformation * falloff * inverseMass;

            node.localPosition += displacement;
            node.velocity += localDirection * clampedForce * velocityScale * falloff * inverseMass;
            node.damage = Mathf.Clamp01(node.damage + clampedForce * damageScale * falloff);
            ClampNodeDisplacement(node);
        }

        DamageNearbyBeams(localPoint, clampedRadius, clampedForce);
        UpdatePanels();
        UpdateVisuals();
    }

    [ContextMenu("Rebuild Visuals")]
    public void RebuildVisuals()
    {
        ClearVisualGroup(NodeGroupName);
        ClearVisualGroup(BeamGroupName);

        GameObject nodeGroup = new GameObject(NodeGroupName);
        nodeGroup.transform.SetParent(transform, false);

        GameObject beamGroup = new GameObject(BeamGroupName);
        beamGroup.transform.SetParent(transform, false);

        if (nodes != null)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                SoftNode node = nodes[i];
                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = node.nodeName;
                marker.transform.SetParent(nodeGroup.transform, false);
                marker.transform.localPosition = node.localPosition;
                float radius = node.wheelNode ? wheelNodeRadius : bodyNodeRadius;
                marker.transform.localScale = Vector3.one * radius * 2f;

                Collider collider = marker.GetComponent<Collider>();
                DestroyCompatible(collider);

                Renderer renderer = marker.GetComponent<Renderer>();
                renderer.sharedMaterial = node.wheelNode && wheelNodeMaterial != null ? wheelNodeMaterial : bodyNodeMaterial;
                node.marker = marker.transform;
            }
        }

        if (beams != null)
        {
            for (int i = 0; i < beams.Length; i++)
            {
                SoftBeam beam = beams[i];
                GameObject lineObject = new GameObject("Beam_" + beam.nodeA + "_" + beam.nodeB);
                lineObject.transform.SetParent(beamGroup.transform, false);

                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 2;
                line.widthMultiplier = beamWidth;
                line.numCapVertices = 2;
                line.sharedMaterial = beamMaterial;
                line.startColor = new Color(0.35f, 1f, 0.18f, 0.92f);
                line.endColor = new Color(0.35f, 1f, 0.18f, 0.92f);
                beam.line = line;
            }
        }

        UpdateVisuals();
    }

    public string GetDebugSummary()
    {
        int brokenCount = 0;
        float maxStrain = 0f;

        if (beams != null)
        {
            for (int i = 0; i < beams.Length; i++)
            {
                if (beams[i].broken)
                {
                    brokenCount++;
                }

                maxStrain = Mathf.Max(maxStrain, beams[i].strain);
            }
        }

        return string.Format(
            "nodes {0} / beams {1} / damage {2:0.00} / max strain {3:0.00} / failed beams {4}",
            NodeCount,
            BeamCount,
            AverageDamage,
            maxStrain,
            brokenCount);
    }

    private void InitializeRuntimeState()
    {
        if (nodes != null)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].localPosition = nodes[i].restLocalPosition;
                nodes[i].velocity = Vector3.zero;
                nodes[i].damage = 0f;
                nodes[i].marker = null;
            }
        }

        if (beams != null)
        {
            for (int i = 0; i < beams.Length; i++)
            {
                SoftBeam beam = beams[i];
                beam.broken = false;
                beam.strain = 0f;
                beam.line = null;

                if (IsValidNodeIndex(beam.nodeA) && IsValidNodeIndex(beam.nodeB))
                {
                    beam.restLength = Vector3.Distance(nodes[beam.nodeA].restLocalPosition, nodes[beam.nodeB].restLocalPosition);
                }
                else
                {
                    beam.restLength = 0f;
                }
            }
        }
    }

    private void CachePanels()
    {
        if (panels == null)
        {
            return;
        }

        for (int i = 0; i < panels.Length; i++)
        {
            SoftPanel panel = panels[i];

            if (panel == null || panel.panel == null)
            {
                continue;
            }

            panel.originalLocalPosition = panel.panel.localPosition;
            panel.originalLocalRotation = panel.panel.localRotation;
            panel.originalLocalScale = panel.panel.localScale;
        }
    }

    private void SolveBeams(float dt)
    {
        for (int i = 0; i < beams.Length; i++)
        {
            SoftBeam beam = beams[i];

            if (beam.broken || !IsValidNodeIndex(beam.nodeA) || !IsValidNodeIndex(beam.nodeB))
            {
                continue;
            }

            SoftNode a = nodes[beam.nodeA];
            SoftNode b = nodes[beam.nodeB];
            Vector3 delta = b.localPosition - a.localPosition;
            float currentLength = delta.magnitude;

            if (currentLength < 0.0001f || beam.restLength <= 0f)
            {
                continue;
            }

            Vector3 direction = delta / currentLength;
            float stretch = currentLength - beam.restLength;
            float relativeVelocity = Vector3.Dot(b.velocity - a.velocity, direction);
            float scalarForce = stretch * beam.stiffness + relativeVelocity * beam.damping;
            Vector3 force = direction * scalarForce;
            float inverseMassA = a.anchored ? 0f : 1f / Mathf.Max(0.01f, a.mass);
            float inverseMassB = b.anchored ? 0f : 1f / Mathf.Max(0.01f, b.mass);

            a.velocity += force * inverseMassA * dt;
            b.velocity -= force * inverseMassB * dt;

            beam.strain = Mathf.Abs(stretch) / beam.restLength;

            if (beam.strain > beam.yieldStrain)
            {
                float plasticStep = Mathf.Clamp01(beam.plasticity * dt * (beam.strain - beam.yieldStrain + 0.05f));
                beam.restLength = Mathf.Lerp(beam.restLength, currentLength, plasticStep);
                a.damage = Mathf.Clamp01(a.damage + beam.strain * 0.006f);
                b.damage = Mathf.Clamp01(b.damage + beam.strain * 0.006f);
            }

            if (beam.strain > beam.breakStrain)
            {
                beam.broken = true;
            }
        }
    }

    private void SolveAnchors(float dt)
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            SoftNode node = nodes[i];

            if (!node.anchored)
            {
                continue;
            }

            Vector3 toRest = node.restLocalPosition - node.localPosition;
            node.velocity += toRest * anchorStiffness * dt;
            node.velocity *= Mathf.Clamp01(1f - anchorDamping * dt);
        }
    }

    private void IntegrateNodes(float dt)
    {
        for (int i = 0; i < nodes.Length; i++)
        {
            SoftNode node = nodes[i];
            node.velocity *= Mathf.Clamp01(1f - nodeDrag * dt);
            node.localPosition += node.velocity * dt;
            ClampNodeDisplacement(node);
        }
    }

    private void DamageNearbyBeams(Vector3 localPoint, float radius, float force)
    {
        if (beams == null)
        {
            return;
        }

        for (int i = 0; i < beams.Length; i++)
        {
            SoftBeam beam = beams[i];

            if (beam.broken || !IsValidNodeIndex(beam.nodeA) || !IsValidNodeIndex(beam.nodeB))
            {
                continue;
            }

            Vector3 a = nodes[beam.nodeA].localPosition;
            Vector3 b = nodes[beam.nodeB].localPosition;
            float distance = DistancePointToSegment(localPoint, a, b);

            if (distance > radius)
            {
                continue;
            }

            float falloff = 1f - distance / radius;
            beam.restLength *= 1f + force * 0.002f * falloff;
        }
    }

    private void UpdatePanels()
    {
        if (panels == null || nodes == null)
        {
            return;
        }

        for (int i = 0; i < panels.Length; i++)
        {
            SoftPanel panel = panels[i];

            if (panel == null || panel.panel == null || panel.nodeIndices == null || panel.nodeIndices.Length == 0)
            {
                continue;
            }

            Vector3 averageDisplacement = Vector3.zero;
            float panelDamage = 0f;
            int count = 0;

            for (int j = 0; j < panel.nodeIndices.Length; j++)
            {
                int nodeIndex = panel.nodeIndices[j];

                if (!IsValidNodeIndex(nodeIndex))
                {
                    continue;
                }

                SoftNode node = nodes[nodeIndex];
                averageDisplacement += node.localPosition - node.restLocalPosition;
                panelDamage = Mathf.Max(panelDamage, node.damage);
                count++;
            }

            if (count == 0)
            {
                continue;
            }

            averageDisplacement /= count;
            panelDamage = Mathf.Clamp01(panelDamage);

            Vector3 scale = panel.originalLocalScale;
            scale[panel.compressionAxis] = Mathf.Lerp(
                panel.originalLocalScale[panel.compressionAxis],
                panel.originalLocalScale[panel.compressionAxis] * panel.minimumAxisScale,
                panelDamage);

            panel.panel.localPosition = panel.originalLocalPosition + averageDisplacement * panel.followStrength;
            panel.panel.localScale = scale;
            panel.panel.localRotation = panel.originalLocalRotation * Quaternion.Euler(
                averageDisplacement.z * 14f,
                averageDisplacement.x * 10f,
                (i % 2 == 0 ? 1f : -1f) * panelDamage * 8f);
        }
    }

    private void UpdateVisuals()
    {
        if (nodes != null)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                SoftNode node = nodes[i];

                if (node.marker == null)
                {
                    continue;
                }

                node.marker.localPosition = node.localPosition;
                float radius = node.wheelNode ? wheelNodeRadius : bodyNodeRadius;
                node.marker.localScale = Vector3.one * radius * Mathf.Lerp(2f, 2.8f, node.damage);
            }
        }

        if (beams == null)
        {
            return;
        }

        for (int i = 0; i < beams.Length; i++)
        {
            SoftBeam beam = beams[i];

            if (beam.line == null)
            {
                continue;
            }

            if (!IsValidNodeIndex(beam.nodeA) || !IsValidNodeIndex(beam.nodeB))
            {
                beam.line.enabled = false;
                continue;
            }

            beam.line.enabled = !beam.broken;
            beam.line.SetPosition(0, nodes[beam.nodeA].localPosition);
            beam.line.SetPosition(1, nodes[beam.nodeB].localPosition);

            if (damagedBeamMaterial != null && beam.strain > beam.yieldStrain)
            {
                beam.line.sharedMaterial = damagedBeamMaterial;
            }
            else
            {
                beam.line.sharedMaterial = beamMaterial;
            }
        }
    }

    private void ClampNodeDisplacement(SoftNode node)
    {
        Vector3 displacement = node.localPosition - node.restLocalPosition;

        if (displacement.magnitude <= maxNodeDisplacement)
        {
            return;
        }

        node.localPosition = node.restLocalPosition + displacement.normalized * maxNodeDisplacement;
        node.velocity *= 0.35f;
    }

    private bool IsValidNodeIndex(int index)
    {
        return nodes != null && index >= 0 && index < nodes.Length;
    }

    private void ClearVisualGroup(string groupName)
    {
        Transform existing = transform.Find(groupName);

        if (existing != null)
        {
            DestroyCompatible(existing.gameObject);
        }
    }

    private static void DestroyCompatible(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private static float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float denominator = ab.sqrMagnitude;

        if (denominator < 0.0001f)
        {
            return Vector3.Distance(point, a);
        }

        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / denominator);
        return Vector3.Distance(point, a + ab * t);
    }
}
