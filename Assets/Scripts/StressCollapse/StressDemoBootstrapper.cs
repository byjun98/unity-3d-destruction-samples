using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public sealed class StressDemoBootstrapper : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInstall()
    {
        StressCollapseBuilding[] buildings = FindObjectsOfType<StressCollapseBuilding>();

        if (buildings == null || buildings.Length == 0)
        {
            return;
        }

        StressDemoBootstrapper existing = FindObjectOfType<StressDemoBootstrapper>();

        if (existing != null)
        {
            return;
        }

        GameObject autoGo = new GameObject("StressDemo_AutoBootstrap");
        autoGo.AddComponent<StressDemoBootstrapper>();
    }

    [Header("References (auto-resolved if empty)")]
    [SerializeField] private StressCollapseBuilding building;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform orbitTarget;

    [Header("Auto-Wire")]
    [SerializeField] private bool autoPopulatePillars = true;
    [SerializeField] private string[] pillarNameContains = new[] { "LoadBearingColumn", "Pillar", "Column" };

    [SerializeField] private bool addStressVisualizer = true;
    [SerializeField] private bool addEffectsBinder = true;
    [SerializeField] private bool addCameraShake = true;
    [SerializeField] private bool addOrbitCamera = true;
    [SerializeField] private bool addHud = true;

    [Header("Camera Default Pose")]
    [SerializeField, Min(2f)] private float defaultCameraDistance = 22f;
    [SerializeField] private float defaultYaw = 32f;
    [SerializeField, Range(-89f, 89f)] private float defaultPitch = 18f;

    [Header("Atmosphere")]
    [SerializeField] private bool enhanceLighting = true;
    [SerializeField] private bool enableFog = true;
    [SerializeField] private Color fogColor = new Color(0.42f, 0.45f, 0.5f, 1f);
    [SerializeField, Min(0f)] private float fogStart = 30f;
    [SerializeField, Min(1f)] private float fogEnd = 140f;

    private void Awake()
    {
        RunBootstrap();
    }

    private bool bootstrapped;

    public void RunBootstrap()
    {
        if (bootstrapped)
        {
            return;
        }

        bootstrapped = true;

        ResolveReferences();

        if (building == null)
        {
            return;
        }

        if (autoPopulatePillars)
        {
            AutoPopulatePillars();
            building.RefreshConfiguration();
        }

        if (addStressVisualizer && building.GetComponent<StressVisualizer>() == null)
        {
            building.gameObject.AddComponent<StressVisualizer>().Configure(building);
        }

        if (addEffectsBinder && building.GetComponent<StressEffectsBinder>() == null)
        {
            building.gameObject.AddComponent<StressEffectsBinder>().Bind(building);
        }

        if (addHud && FindObjectOfType<StressCollapseHud>() == null)
        {
            GameObject hudGo = new GameObject("StressCollapseHud_Auto");
            StressCollapseHud hud = hudGo.AddComponent<StressCollapseHud>();
            hud.transform.SetParent(transform, false);
        }

        SetupCamera();
        ApplyAtmosphere();
        FixSceneMaterials();
    }

    private void FixSceneMaterials()
    {
        ParticleSystem[] particles = FindObjectsOfType<ParticleSystem>(true);

        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] != null)
            {
                StressUrpMaterialFixer.Fix(particles[i].gameObject);
            }
        }
    }

    private void ResolveReferences()
    {
        if (building == null)
        {
            building = FindObjectOfType<StressCollapseBuilding>();
        }

        if (targetCamera == null)
        {
            StressCollapseShooter shooter = FindObjectOfType<StressCollapseShooter>();

            if (shooter != null)
            {
                targetCamera = shooter.GetComponent<Camera>();
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                Camera[] cams = FindObjectsOfType<Camera>();

                if (cams.Length > 0)
                {
                    targetCamera = cams[0];
                }
            }
        }

        if (orbitTarget == null && building != null)
        {
            GameObject pivotGo = new GameObject("StressOrbit_Pivot");
            pivotGo.transform.SetParent(building.transform, false);

            Bounds buildingBounds = ComputeWorldBounds(building.transform);
            pivotGo.transform.position = buildingBounds.center;
            orbitTarget = pivotGo.transform;
        }
    }

    private void AutoPopulatePillars()
    {
        for (int i = 0; i < building.LayerCount; i++)
        {
            StressCollapseBuilding.StressLayer layer = building.GetLayer(i);

            if (layer == null || layer.layerRoot == null)
            {
                continue;
            }

            if (layer.pillars != null && layer.pillars.Length > 0)
            {
                bool anyValid = false;

                for (int p = 0; p < layer.pillars.Length; p++)
                {
                    if (layer.pillars[p] != null && layer.pillars[p].pillarRoot != null)
                    {
                        anyValid = true;
                        break;
                    }
                }

                if (anyValid)
                {
                    continue;
                }
            }

            List<StressCollapseBuilding.StressPillarRef> found = new List<StressCollapseBuilding.StressPillarRef>();
            Transform[] descendants = layer.layerRoot.GetComponentsInChildren<Transform>(true);

            for (int t = 0; t < descendants.Length; t++)
            {
                Transform candidate = descendants[t];

                if (candidate == null || candidate == layer.layerRoot.transform)
                {
                    continue;
                }

                string name = candidate.name;

                if (!MatchesPillarName(name))
                {
                    continue;
                }

                StressCollapseBuilding.StressPillarRef pillarRef = new StressCollapseBuilding.StressPillarRef
                {
                    pillarName = name,
                    pillarRoot = candidate,
                    share = 1f
                };
                found.Add(pillarRef);
            }

            if (found.Count > 0)
            {
                layer.pillars = found.ToArray();

                if (layer.minPillarsForSupport <= 0 || layer.minPillarsForSupport > found.Count)
                {
                    layer.minPillarsForSupport = Mathf.Max(1, Mathf.CeilToInt(found.Count * 0.5f));
                }
            }
        }
    }

    private bool MatchesPillarName(string name)
    {
        if (string.IsNullOrEmpty(name) || pillarNameContains == null)
        {
            return false;
        }

        for (int i = 0; i < pillarNameContains.Length; i++)
        {
            string token = pillarNameContains[i];

            if (!string.IsNullOrEmpty(token) && name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void SetupCamera()
    {
        if (targetCamera == null)
        {
            return;
        }

        if (addCameraShake && targetCamera.GetComponent<StressCameraShake>() == null)
        {
            StressCameraShake shake = targetCamera.gameObject.AddComponent<StressCameraShake>();
            shake.Bind(building);
        }

        if (addOrbitCamera && orbitTarget != null && targetCamera.GetComponent<StressOrbitCamera>() == null)
        {
            StressOrbitCamera orbit = targetCamera.gameObject.AddComponent<StressOrbitCamera>();
            orbit.SetPivot(orbitTarget, defaultCameraDistance);

            var so = new SerializedFieldSetter(orbit);
            so.SetFloat("yaw", defaultYaw);
            so.SetFloat("pitch", defaultPitch);
        }
    }

    private void ApplyAtmosphere()
    {
        if (!enhanceLighting)
        {
            return;
        }

        if (enableFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
        }

        Light sun = RenderSettings.sun;

        if (sun == null)
        {
            Light[] lights = FindObjectsOfType<Light>();

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    sun = lights[i];
                    break;
                }
            }
        }

        if (sun != null)
        {
            sun.intensity = Mathf.Max(sun.intensity, 1.05f);
            sun.shadows = sun.shadows == LightShadows.None ? LightShadows.Soft : sun.shadows;

            if (sun.color.maxColorComponent < 0.7f)
            {
                sun.color = new Color(1f, 0.95f, 0.86f, 1f);
            }
        }

        Color ambientUp = new Color(0.32f, 0.36f, 0.42f, 1f);
        Color ambientMid = new Color(0.24f, 0.25f, 0.28f, 1f);
        Color ambientDown = new Color(0.13f, 0.13f, 0.14f, 1f);

        if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Trilight ||
            RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat)
        {
            RenderSettings.ambientSkyColor = ambientUp;
            RenderSettings.ambientEquatorColor = ambientMid;
            RenderSettings.ambientGroundColor = ambientDown;
        }
    }

    private static Bounds ComputeWorldBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return new Bounds(root.position, Vector3.one * 2f);
        }

        Bounds b = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }

        return b;
    }

    private sealed class SerializedFieldSetter
    {
        private readonly object target;
        private readonly System.Type type;

        public SerializedFieldSetter(object target)
        {
            this.target = target;
            this.type = target.GetType();
        }

        public void SetFloat(string fieldName, float value)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance);

            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(target, value);
            }
        }
    }
}
