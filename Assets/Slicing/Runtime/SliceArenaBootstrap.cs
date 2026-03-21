using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Slicing
{
    [DefaultExecutionOrder(-100)]
    public class SliceArenaBootstrap : MonoBehaviour
    {
        [Header("Build")]
        public bool buildOnAwake = true;

        [Header("Layout")]
        public int columns = 5;
        public int rows = 3;
        public float spacing = 1.9f;
        public float baseHeight = 1.2f;
        public float rowYStep = 1.4f;
        public float scaleMin = 0.65f;
        public float scaleMax = 1.25f;

        [Header("Look")]
        public Color groundColor = new Color(0.075f, 0.085f, 0.10f);
        public Color rimColor = new Color(0.07f, 0.65f, 1.0f);
        public Color[] palette = new[]
        {
            new Color(0.95f, 0.30f, 0.35f),
            new Color(0.20f, 0.85f, 0.55f),
            new Color(0.30f, 0.55f, 1.00f),
            new Color(1.00f, 0.78f, 0.20f),
            new Color(0.80f, 0.40f, 1.00f),
            new Color(1.00f, 0.50f, 0.20f),
            new Color(0.20f, 0.95f, 0.95f)
        };

        [Header("Physics & Lifetime")]
        public float halfLifeSeconds = 7f;
        public float separationImpulse = 2.4f;

        GameObject _root;
        GameObject _impactPrefab;
        SliceController _controller;

        void Awake()
        {
            if (buildOnAwake) Build();
        }

        public void Rebuild()
        {
            CleanupSliceables();
            SpawnTargets();
            if (_controller != null) _controller.ResetCount();
        }

        public void Build()
        {
            ApplyRenderSettings();
            EnsureCamera();
            EnsureLights();
            EnsureGround();
            EnsureRoot();
            EnsureImpactPrefab();
            EnsureController();
            EnsureUI();
            CleanupSliceables();
            SpawnTargets();
        }

        void ApplyRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.16f, 0.22f, 0.32f);
            RenderSettings.ambientEquatorColor = new Color(0.10f, 0.10f, 0.13f);
            RenderSettings.ambientGroundColor = new Color(0.04f, 0.04f, 0.06f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.08f);
            RenderSettings.fogDensity = 0.035f;
        }

        void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.transform.position = new Vector3(0f, 4.6f, -8.5f);
            cam.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            cam.fieldOfView = 55f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.07f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
        }

        void EnsureLights()
        {
            Light key = null;
            foreach (var l in FindObjectsOfType<Light>())
            {
                if (l.type == LightType.Directional) { key = l; break; }
            }
            if (key == null)
            {
                var go = new GameObject("KeyLight");
                key = go.AddComponent<Light>();
                key.type = LightType.Directional;
            }
            key.transform.rotation = Quaternion.Euler(45f, 35f, 0f);
            key.color = new Color(1f, 0.97f, 0.92f);
            key.intensity = 1.4f;
            key.shadows = LightShadows.Soft;

            var rimGo = GameObject.Find("RimLight");
            if (rimGo == null) rimGo = new GameObject("RimLight");
            var rim = rimGo.GetComponent<Light>();
            if (rim == null) rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = rimColor;
            rim.intensity = 0.9f;
            rim.shadows = LightShadows.None;
            rimGo.transform.rotation = Quaternion.Euler(15f, -150f, 0f);
        }

        void EnsureGround()
        {
            var existing = GameObject.Find("ArenaGround");
            if (existing != null) DestroyImmediate(existing);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "ArenaGround";
            ground.transform.localScale = new Vector3(28f, 0.6f, 22f);
            ground.transform.position = new Vector3(0f, -0.3f, 0f);
            var mr = ground.GetComponent<MeshRenderer>();
            mr.sharedMaterial = MakeUrpMaterial("ArenaGroundMat", groundColor, 0.0f, 0.4f, Color.black);

            // Subtle accent strips on the floor
            for (int i = -1; i <= 1; i += 2)
            {
                var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                strip.name = $"GroundAccent_{i}";
                strip.transform.localScale = new Vector3(20f, 0.02f, 0.05f);
                strip.transform.position = new Vector3(0f, 0.01f, i * 4.2f);
                var smr = strip.GetComponent<MeshRenderer>();
                smr.sharedMaterial = MakeUrpMaterial("ArenaStripMat", new Color(0.15f, 0.4f, 0.7f), 0f, 0.2f, rimColor * 0.8f);
                Destroy(strip.GetComponent<Collider>());
                strip.transform.SetParent(ground.transform, true);
            }
        }

        void EnsureRoot()
        {
            if (_root == null)
            {
                _root = GameObject.Find("ArenaRoot");
                if (_root == null) _root = new GameObject("ArenaRoot");
            }
        }

        void EnsureImpactPrefab()
        {
            if (_impactPrefab != null) return;
            _impactPrefab = SliceImpactFx.CreateRuntimeImpactPrefab();
            _impactPrefab.SetActive(false);
            _impactPrefab.transform.SetParent(transform, false);
            _impactPrefab.name = "SliceImpactTemplate";
        }

        void EnsureController()
        {
            if (_controller != null) return;
            var go = GameObject.Find("SliceController");
            if (go == null) go = new GameObject("SliceController");
            _controller = go.GetComponent<SliceController>();
            if (_controller == null) _controller = go.AddComponent<SliceController>();
            _controller.cam = Camera.main;
            _controller.impactPrefab = _impactPrefab;

            var juice = go.GetComponent<SliceJuice>();
            if (juice == null) juice = go.AddComponent<SliceJuice>();
            juice.controller = _controller;
            juice.cam = Camera.main;
        }

        void EnsureUI()
        {
            var existing = GameObject.Find("SliceCanvas");
            if (existing != null) DestroyImmediate(existing);
            SliceCanvasUI.Build(_controller);
        }

        void CleanupSliceables()
        {
            foreach (var t in FindObjectsOfType<SliceTarget>())
                if (t != null) DestroyImmediate(t.gameObject);
        }

        void SpawnTargets()
        {
            var prims = new[] { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Capsule, PrimitiveType.Cylinder };
            int variant = 0;
            int colorIdx = 0;
            float halfCols = (columns - 1) * 0.5f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    var prim = prims[variant++ % prims.Length];
                    var go = GameObject.CreatePrimitive(prim);
                    go.name = $"Target_{prim}_{r}{c}";
                    go.transform.SetParent(_root != null ? _root.transform : null, false);

                    float x = (c - halfCols) * spacing;
                    float z = (r - (rows - 1) * 0.5f) * spacing - 0.5f;
                    float y = baseHeight + r * rowYStep;
                    go.transform.position = new Vector3(x, y, z);
                    go.transform.rotation = Quaternion.Euler(Random.value * 30f, Random.value * 360f, Random.value * 30f);

                    float s = Random.Range(scaleMin, scaleMax);
                    if (prim == PrimitiveType.Capsule) s *= 0.85f;
                    if (prim == PrimitiveType.Cylinder) s *= 0.9f;
                    go.transform.localScale = Vector3.one * s;

                    var mr = go.GetComponent<MeshRenderer>();
                    var color = palette[colorIdx++ % palette.Length];
                    mr.sharedMaterial = MakeUrpMaterial($"TargetMat_{r}{c}", color, 0.05f, 0.55f, color * 0.15f);
                    mr.shadowCastingMode = ShadowCastingMode.On;

                    var st = go.AddComponent<SliceTarget>();
                    st.impactFxPrefab = _impactPrefab;
                    st.halfLifeSeconds = halfLifeSeconds;
                    st.separationImpulse = separationImpulse;
                    st.capColor = Color.Lerp(color, Color.white, 0.4f);
                    st.capEmission = color * 2.6f;

                    var spinner = go.AddComponent<TargetSpinner>();
                    spinner.axis = new Vector3(Random.Range(-0.3f, 0.3f), 1f, Random.Range(-0.3f, 0.3f));
                    spinner.speed = Random.Range(20f, 50f) * (Random.value < 0.5f ? -1f : 1f);
                    spinner.bobAmplitude = Random.Range(0.05f, 0.18f);
                    spinner.bobFrequency = Random.Range(0.3f, 0.8f);

                    // Make sure colliders are present and convex-friendly
                    var col = go.GetComponent<Collider>();
                    if (col == null) go.AddComponent<BoxCollider>();
                }
            }
        }

        static Material MakeUrpMaterial(string name, Color baseColor, float metallic, float smoothness, Color emission)
        {
            var mat = new Material(SliceShaders.Lit()) { name = name };
            SliceShaders.SetTint(mat, baseColor);
            SliceShaders.SetMetallicSmoothness(mat, metallic, smoothness);
            if (emission.maxColorComponent > 0.0001f) SliceShaders.SetEmission(mat, emission);
            return mat;
        }
    }
}
