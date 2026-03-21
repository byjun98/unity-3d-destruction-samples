using System;
using UnityEngine;

namespace Slicing
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class SliceTarget : MonoBehaviour
    {
        [Header("Cut Look")]
        public Color capColor = new Color(1.0f, 0.25f, 0.18f);
        public Color capEmission = new Color(2.4f, 0.45f, 0.10f);

        [Header("Physics")]
        public bool addRigidbodyOnSlice = true;
        public float separationImpulse = 2.6f;
        public float separationTorque = 1.2f;

        [Header("FX (optional)")]
        public GameObject impactFxPrefab;
        public float halfLifeSeconds = 8f;

        public event Action<SliceResult, GameObject, GameObject> Sliced;

        Material _capMaterial;

        public bool TrySlice(SlicePlane worldPlane)
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();
            if (mf == null || mf.sharedMesh == null || mr == null) return false;

            if (!mf.sharedMesh.isReadable)
            {
                Debug.LogWarning($"[Slicing] Mesh '{mf.sharedMesh.name}' on '{name}' is not Read/Write enabled. Skipping.", this);
                return false;
            }

            if (!IsPlaneCrossingBounds(worldPlane, mr.bounds)) return false;

            var capMat = GetOrCreateCapMaterial();
            var result = MeshSlicer.Slice(mf, mr, worldPlane, capMat);
            if (result == null || !result.DidSlice) return false;

            var upperGo = SpawnHalf(result.UpperMesh, result.UpperMaterials, "_Upper", worldPlane.Normal);
            var lowerGo = SpawnHalf(result.LowerMesh, result.LowerMaterials, "_Lower", -worldPlane.Normal);

            if (impactFxPrefab != null)
            {
                var fx = Instantiate(impactFxPrefab, transform.position, Quaternion.LookRotation(worldPlane.Normal));
                fx.SetActive(true);
                Destroy(fx, 4f);
            }

            Sliced?.Invoke(result, upperGo, lowerGo);
            Destroy(gameObject);
            return true;
        }

        GameObject SpawnHalf(Mesh mesh, Material[] mats, string suffix, Vector3 pushDir)
        {
            var go = new GameObject(name + suffix);
            // Pre-separate halves so the two coplanar cap surfaces don't z-fight.
            // 0.003m is still below visible-gap threshold at typical camera distances.
            const float SpawnSeparation = 0.003f;
            Vector3 sepOffset = pushDir.normalized * SpawnSeparation;
            go.transform.SetPositionAndRotation(transform.position + sepOffset, transform.rotation);
            go.transform.localScale = transform.localScale;
            go.layer = gameObject.layer;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = mats;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;

            // Use BoxCollider on halves: convex MeshCollider has a 256-poly cook limit and silently fails
            // on most sliced meshes, leaving them un-collidable. A tight bounds box is reliable.
            var box = go.AddComponent<BoxCollider>();
            box.size = mesh.bounds.size;
            box.center = mesh.bounds.center;

            if (addRigidbodyOnSlice)
            {
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = Mathf.Max(0.5f, mesh.bounds.size.magnitude * 0.6f);
                rb.AddForce(pushDir.normalized * separationImpulse, ForceMode.VelocityChange);
                rb.AddTorque(UnityEngine.Random.insideUnitSphere * separationTorque, ForceMode.VelocityChange);
            }

            // Carry forward parent's velocity if it had a rigidbody.
            var parentRb = GetComponent<Rigidbody>();
            if (parentRb != null && go.TryGetComponent(out Rigidbody childRb))
            {
                childRb.velocity += parentRb.velocity;
                childRb.angularVelocity += parentRb.angularVelocity;
            }

            // Tag halves with a fresh SliceTarget so they can be re-sliced.
            var st = go.AddComponent<SliceTarget>();
            st.capColor = capColor;
            st.capEmission = capEmission;
            st.addRigidbodyOnSlice = addRigidbodyOnSlice;
            st.separationImpulse = separationImpulse;
            st.separationTorque = separationTorque;
            st.impactFxPrefab = impactFxPrefab;
            st.halfLifeSeconds = halfLifeSeconds;
            st._capMaterial = _capMaterial; // share the cap material to keep batching tight.

            if (halfLifeSeconds > 0f)
                Destroy(go, halfLifeSeconds);

            return go;
        }

        Material GetOrCreateCapMaterial()
        {
            if (_capMaterial != null) return _capMaterial;
            // Custom double-sided unlit shader avoids fan-winding stripe artifacts on cut caps.
            var sh = Shader.Find("Slicing/CapUnlit");
            if (sh != null)
            {
                _capMaterial = new Material(sh) { name = "SliceCap_RT" };
                _capMaterial.SetColor("_BaseColor", capColor);
                _capMaterial.SetColor("_EmissionColor", capEmission);
                return _capMaterial;
            }
            // Fallback: lit shader (still fine after spawn-separation removes z-fight)
            _capMaterial = new Material(SliceShaders.Lit()) { name = "SliceCap_RT" };
            SliceShaders.SetTint(_capMaterial, capColor);
            SliceShaders.SetEmission(_capMaterial, capEmission);
            SliceShaders.SetMetallicSmoothness(_capMaterial, 0.1f, 0.6f);
            return _capMaterial;
        }

        static bool IsPlaneCrossingBounds(SlicePlane plane, Bounds b)
        {
            // Project bounds extents onto plane normal; compare to signed distance of center.
            float r = Mathf.Abs(b.extents.x * plane.Normal.x)
                    + Mathf.Abs(b.extents.y * plane.Normal.y)
                    + Mathf.Abs(b.extents.z * plane.Normal.z);
            float d = plane.SignedDistance(b.center);
            return Mathf.Abs(d) <= r;
        }
    }
}
