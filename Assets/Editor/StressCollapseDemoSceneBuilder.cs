#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StressCollapseDemoSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/StressCollapseDemo.unity";
    private const string DemoFolder = "Assets/StressCollapseDemo";
    private const string MaterialFolder = "Assets/StressCollapseDemo/Materials";

    private struct LayerSpec
    {
        public string Name;
        public float Weight;
        public float Strength;
        public float Height;

        public LayerSpec(string name, float weight, float strength, float height)
        {
            Name = name;
            Weight = weight;
            Strength = strength;
            Height = height;
        }
    }

    [MenuItem("Tools/Demos/Build Stress Collapse Demo")]
    public static void BuildScene()
    {
        EnsureFolders();
        EditorSceneManager.SaveOpenScenes();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "StressCollapseDemo";

        Material concrete = CreateLitMaterial(MaterialFolder + "/Stress_Concrete.mat", new Color(0.62f, 0.63f, 0.6f, 1f), 0f, 0.45f);
        Material darkConcrete = CreateLitMaterial(MaterialFolder + "/Stress_DarkConcrete.mat", new Color(0.36f, 0.38f, 0.38f, 1f), 0f, 0.5f);
        Material metal = CreateLitMaterial(MaterialFolder + "/Stress_DarkMetal.mat", new Color(0.18f, 0.22f, 0.25f, 1f), 0.4f, 0.55f);
        Material hazard = CreateLitMaterial(MaterialFolder + "/Stress_HazardYellow.mat", new Color(1f, 0.72f, 0.08f, 1f), 0f, 0.35f);
        Material target = CreateLitMaterial(MaterialFolder + "/Stress_TargetRed.mat", new Color(0.9f, 0.18f, 0.12f, 1f), 0f, 0.4f);
        Material ground = CreateLitMaterial(MaterialFolder + "/Stress_Ground.mat", new Color(0.22f, 0.24f, 0.23f, 1f), 0f, 0.55f);

        GameObject hitEffect = LoadFirstPrefab(
            "Assets/UnityTechnologies/ParticlePack/EffectExamples/Weapon Effects/Prefabs/StoneImpacts.prefab",
            "Assets/DestructionDemo/Prefabs/Wood_HitEffect.prefab");
        GameObject breakEffect = LoadFirstPrefab(
            "Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/DustExplosion.prefab",
            "Assets/DestructionDemo/Prefabs/Wood_BreakDust.prefab");
        GameObject collapseEffect = LoadFirstPrefab(
            "Assets/FX_BigSmokeEffectsA/Prefabs/SFX_SmokeBigThickA.prefab",
            "Assets/UnityTechnologies/ParticlePack/EffectExamples/Smoke & Steam Effects/Prefabs/DustStorm.prefab",
            "Assets/UnityTechnologies/ParticlePack/EffectExamples/Smoke & Steam Effects/Prefabs/SmokeEffect.prefab");

        CreateGround(ground, hazard);
        CreateLights();

        GameObject buildingRoot = new GameObject("StressCollapse_Building");
        StressCollapseBuilding building = buildingRoot.AddComponent<StressCollapseBuilding>();

        LayerSpec[] specs = new LayerSpec[]
        {
            new LayerSpec("Level 0 Core", 32f, 118f, 0f),
            new LayerSpec("Level 1 Offices", 26f, 80f, 1.55f),
            new LayerSpec("Level 2 Offices", 22f, 58f, 3.1f),
            new LayerSpec("Roof Plant", 14f, 25f, 4.65f)
        };

        StressCollapseBuilding.StressLayer[] layers = new StressCollapseBuilding.StressLayer[specs.Length];

        for (int i = 0; i < specs.Length; i++)
        {
            GameObject layerRoot = new GameObject("StressLayer_" + i + "_" + specs[i].Name.Replace(" ", ""));
            layerRoot.transform.SetParent(buildingRoot.transform);
            layerRoot.transform.position = Vector3.zero;
            BuildLayer(building, layerRoot.transform, i, specs[i], concrete, darkConcrete, metal, hazard, target, hitEffect, breakEffect);

            layers[i] = new StressCollapseBuilding.StressLayer
            {
                layerName = specs[i].Name,
                layerRoot = layerRoot,
                weight = specs[i].Weight,
                strength = specs[i].Strength
            };
        }

        building.ConfigureForDemo(layers, collapseEffect);
        EditorUtility.SetDirty(building);

        CreateSetDressing(metal, hazard);
        CreateCameraAndHud(building, hitEffect);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built StressCollapseDemo at " + ScenePath);
    }

    private static void BuildLayer(
        StressCollapseBuilding building,
        Transform parent,
        int layerIndex,
        LayerSpec spec,
        Material concrete,
        Material darkConcrete,
        Material metal,
        Material hazard,
        Material target,
        GameObject hitEffect,
        GameObject breakEffect)
    {
        float y = spec.Height;
        float supportY = y + 0.72f;
        float deckY = y + 1.43f;
        float supportHealth = Mathf.Lerp(48f, 34f, layerIndex / 3f);
        float panelHealth = Mathf.Lerp(36f, 26f, layerIndex / 3f);

        CreateStressPiece(parent, "Floor_A_" + layerIndex, new Vector3(-1.05f, y + 0.08f, 0f), new Vector3(2.15f, 0.18f, 3.25f), darkConcrete, 5.5f, null, layerIndex, 0f, 0f, 0f, null, null);
        CreateStressPiece(parent, "Floor_B_" + layerIndex, new Vector3(1.05f, y + 0.08f, 0f), new Vector3(2.15f, 0.18f, 3.25f), darkConcrete, 5.5f, null, layerIndex, 0f, 0f, 0f, null, null);

        CreateStressPiece(parent, "Beam_Front_" + layerIndex, new Vector3(0f, deckY, -1.68f), new Vector3(5.1f, 0.28f, 0.22f), metal, 2.4f, null, layerIndex, 0f, 0f, 0f, null, null);
        CreateStressPiece(parent, "Beam_Back_" + layerIndex, new Vector3(0f, deckY, 1.68f), new Vector3(5.1f, 0.28f, 0.22f), metal, 2.4f, null, layerIndex, 0f, 0f, 0f, null, null);
        CreateStressPiece(parent, "Beam_Left_" + layerIndex, new Vector3(-2.45f, deckY, 0f), new Vector3(0.22f, 0.28f, 3.25f), metal, 2.4f, null, layerIndex, 0f, 0f, 0f, null, null);
        CreateStressPiece(parent, "Beam_Right_" + layerIndex, new Vector3(2.45f, deckY, 0f), new Vector3(0.22f, 0.28f, 3.25f), metal, 2.4f, null, layerIndex, 0f, 0f, 0f, null, null);

        Vector3[] supportPositions = new Vector3[]
        {
            new Vector3(-2.25f, supportY, -1.42f),
            new Vector3(2.25f, supportY, -1.42f),
            new Vector3(-2.25f, supportY, 1.42f),
            new Vector3(2.25f, supportY, 1.42f)
        };

        for (int i = 0; i < supportPositions.Length; i++)
        {
            Material supportMaterial = layerIndex == 0 ? target : concrete;
            CreateStressPiece(parent, "LoadBearingColumn_" + layerIndex + "_" + i, supportPositions[i], new Vector3(0.32f, 1.42f, 0.32f), supportMaterial, 2.1f, building, layerIndex, supportHealth, 0.35f, 12f, hitEffect, breakEffect);
        }

        CreateStressPiece(parent, "ShearWall_Left_" + layerIndex, new Vector3(-2.48f, supportY, 0f), new Vector3(0.2f, 1.05f, 1.3f), concrete, 1.9f, building, layerIndex, panelHealth, 0.18f, 7f, hitEffect, breakEffect);
        CreateStressPiece(parent, "ShearWall_Right_" + layerIndex, new Vector3(2.48f, supportY, 0f), new Vector3(0.2f, 1.05f, 1.3f), concrete, 1.9f, building, layerIndex, panelHealth, 0.18f, 7f, hitEffect, breakEffect);

        if (layerIndex < 3)
        {
            CreateStressPiece(parent, "WindowBand_Back_" + layerIndex, new Vector3(0f, supportY + 0.15f, 1.71f), new Vector3(2.7f, 0.58f, 0.12f), concrete, 1.2f, building, layerIndex, panelHealth * 0.7f, 0.12f, 4f, hitEffect, breakEffect);
        }

        if (layerIndex == 3)
        {
            CreateStressPiece(parent, "RoofMachineBlock_A", new Vector3(-0.9f, y + 0.45f, 0.55f), new Vector3(0.9f, 0.65f, 0.8f), metal, 2.2f, building, layerIndex, 28f, 0.2f, 5f, hitEffect, breakEffect);
            CreateStressPiece(parent, "RoofMachineBlock_B", new Vector3(0.8f, y + 0.35f, -0.45f), new Vector3(1.15f, 0.45f, 0.65f), hazard, 1.8f, building, layerIndex, 24f, 0.18f, 5f, hitEffect, breakEffect);
        }
    }

    private static GameObject CreateStressPiece(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Material material,
        float mass,
        StressCollapseBuilding building,
        int layerIndex,
        float health,
        float stressDamageScale,
        float breakStressDamage,
        GameObject hitEffect,
        GameObject breakEffect)
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = name;
        piece.transform.SetParent(parent);
        piece.transform.position = position;
        piece.transform.localScale = scale;

        Renderer renderer = piece.GetComponent<Renderer>();
        renderer.sharedMaterial = material;

        Rigidbody body = piece.AddComponent<Rigidbody>();
        body.mass = Mathf.Max(0.1f, mass);
        body.drag = 0.2f;
        body.angularDrag = 0.15f;
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (building != null && health > 0f)
        {
            StressDamageReceiver receiver = piece.AddComponent<StressDamageReceiver>();
            receiver.ConfigureForDemo(building, layerIndex, health, stressDamageScale, breakStressDamage, hitEffect, breakEffect);
            EditorUtility.SetDirty(receiver);
        }

        return piece;
    }

    private static void CreateGround(Material ground, Material hazard)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "StressDemo_Ground";
        floor.transform.position = new Vector3(0f, -0.08f, 0f);
        floor.transform.localScale = new Vector3(16f, 0.16f, 12f);
        floor.GetComponent<Renderer>().sharedMaterial = ground;
        floor.isStatic = true;

        for (int i = 0; i < 7; i++)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "HazardStripe_" + i;
            stripe.transform.position = new Vector3(-6f + i * 2f, 0.02f, -4.7f);
            stripe.transform.rotation = Quaternion.Euler(0f, 28f, 0f);
            stripe.transform.localScale = new Vector3(1.1f, 0.03f, 0.12f);
            stripe.GetComponent<Renderer>().sharedMaterial = hazard;
            stripe.isStatic = true;
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
        }
    }

    private static void CreateLights()
    {
        GameObject sun = new GameObject("Directional Light");
        Light sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 1.65f;
        sunLight.color = new Color(1f, 0.96f, 0.9f, 1f);
        sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

        GameObject fill = new GameObject("StressDemo_CoolFillLight");
        Light fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.intensity = 6f;
        fillLight.range = 9f;
        fillLight.color = new Color(0.45f, 0.65f, 1f, 1f);
        fill.transform.position = new Vector3(-4.5f, 3.2f, -3.5f);
    }

    private static void CreateSetDressing(Material metal, Material hazard)
    {
        CreatePrimitiveProp("StressDemo_BackWall", new Vector3(0f, 2.1f, 3.35f), new Vector3(8f, 4.2f, 0.22f), metal);
        CreatePrimitiveProp("StressDemo_LeftBarrier", new Vector3(-4.6f, 0.35f, -1.2f), new Vector3(0.2f, 0.7f, 4.7f), hazard);
        CreatePrimitiveProp("StressDemo_RightBarrier", new Vector3(4.6f, 0.35f, -1.2f), new Vector3(0.2f, 0.7f, 4.7f), hazard);

        GameObject lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Warning_Lamps_Set/Prefabs/warning_lamp_3.prefab");

        if (lampPrefab != null)
        {
            InstantiatePrefab(lampPrefab, "WarningLamp_Left", new Vector3(-3.8f, 0f, -3.7f), Quaternion.Euler(0f, 35f, 0f), Vector3.one * 0.9f);
            InstantiatePrefab(lampPrefab, "WarningLamp_Right", new Vector3(3.8f, 0f, -3.7f), Quaternion.Euler(0f, -35f, 0f), Vector3.one * 0.9f);
        }

        GameObject smokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FX_BigSmokeEffectsA/Prefabs/SFX_VaporGroundBigA.prefab");

        if (smokePrefab != null)
        {
            InstantiatePrefab(smokePrefab, "AmbientGroundVapor", new Vector3(0f, 0.08f, 2.5f), Quaternion.identity, Vector3.one * 0.7f);
        }
    }

    private static void CreatePrimitiveProp(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prop.name = name;
        prop.transform.position = position;
        prop.transform.localScale = scale;
        prop.GetComponent<Renderer>().sharedMaterial = material;
        prop.isStatic = true;
    }

    private static GameObject InstantiatePrefab(GameObject prefab, string name, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            return null;
        }

        instance.name = name;
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        instance.transform.localScale = scale;
        return instance;
    }

    private static void CreateCameraAndHud(StressCollapseBuilding building, GameObject hitEffect)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        cameraObject.tag = "MainCamera";
        camera.fieldOfView = 54f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 120f;
        cameraObject.transform.position = new Vector3(4.7f, 3.1f, -6.9f);
        cameraObject.transform.LookAt(new Vector3(0f, 2.45f, 0.25f));

        StressCollapseShooter shooter = cameraObject.AddComponent<StressCollapseShooter>();
        SerializedObject shooterObject = new SerializedObject(shooter);
        shooterObject.FindProperty("aimCamera").objectReferenceValue = camera;
        shooterObject.FindProperty("hitEffectPrefab").objectReferenceValue = hitEffect;
        shooterObject.ApplyModifiedPropertiesWithoutUndo();

        StressCollapseHud hud = cameraObject.AddComponent<StressCollapseHud>();
        SerializedObject hudObject = new SerializedObject(hud);
        hudObject.FindProperty("building").objectReferenceValue = building;
        hudObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material CreateLitMaterial(string path, Color color, float metallic, float smoothness)
    {
        bool usesScriptableRenderPipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
        Shader shader = usesScriptableRenderPipeline ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", smoothness);
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject LoadFirstPrefab(params string[] paths)
    {
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);

            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(DemoFolder))
        {
            AssetDatabase.CreateFolder("Assets", "StressCollapseDemo");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder(DemoFolder, "Materials");
        }
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == scenePath)
            {
                scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
