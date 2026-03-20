#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BeamSoftBodyVehicleDemoSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/BeamSoftBodyVehicleDemo.unity";
    private const string DemoFolder = "Assets/SoftBodyVehicleDemo";
    private const string MaterialFolder = "Assets/SoftBodyVehicleDemo/Materials";

    private static readonly float[] BodyX = { -1.18f, 1.18f };
    private static readonly float[] BodyY = { 0.36f, 1.02f, 1.55f };
    private static readonly float[] BodyZ = { -2.55f, -1.35f, 0.05f, 1.35f, 2.45f };

    private const string MinorImpactPrefabPath = "Assets/UnityTechnologies/ParticlePack/EffectExamples/Misc Effects/Prefabs/SparksEffect.prefab";
    private const string MediumImpactPrefabPath = "Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/DustExplosion.prefab";
    private const string HeavyImpactPrefabPath = "Assets/UnityTechnologies/ParticlePack/EffectExamples/Fire & Explosion Effects/Prefabs/SmallExplosion.prefab";
    private const string SmokePrefabPath = "Assets/UnityTechnologies/ParticlePack/EffectExamples/Smoke & Steam Effects/Prefabs/SmokeEffect.prefab";
    private const string SparkPrefabPath = "Assets/UnityTechnologies/ParticlePack/EffectExamples/Misc Effects/Prefabs/ElectricalSparks.prefab";

    [MenuItem("Tools/Demos/Build Beam Soft Body Vehicle Demo")]
    public static void BuildScene()
    {
        EnsureFolders();
        EditorSceneManager.SaveOpenScenes();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BeamSoftBodyVehicleDemo";

        Material groundMaterial = CreateLitMaterial(MaterialFolder + "/SoftBody_Ground.mat", new Color(0.18f, 0.2f, 0.19f, 1f), 0f, 0.55f);
        Material laneMaterial = CreateLitMaterial(MaterialFolder + "/SoftBody_Lane.mat", new Color(0.78f, 0.78f, 0.78f, 1f), 0f, 0.4f);
        Material vehicleAssetMaterial = CreateLitMaterial(MaterialFolder + "/SoftBody_VehicleAsset.mat", new Color(0.66f, 0.42f, 0.24f, 1f), 0.1f, 0.55f);
        Material vfxParticleMaterial = CreateUrpParticleAdditiveMaterial(MaterialFolder + "/SoftBody_VfxParticle.mat", Color.white);
        Material barrierMaterial = CreateLitMaterial(MaterialFolder + "/SoftBody_Barrier.mat", new Color(0.78f, 0.1f, 0.08f, 1f), 0f, 0.38f);
        Material concreteMaterial = CreateLitMaterial(MaterialFolder + "/SoftBody_Concrete.mat", new Color(0.62f, 0.6f, 0.58f, 1f), 0f, 0.35f);
        Material yellowStripeMaterial = CreateLitMaterial(MaterialFolder + "/SoftBody_Stripe.mat", new Color(1f, 0.78f, 0.1f, 1f), 0.05f, 0.5f);
        Material woodMaterial = CreateLitMaterial(MaterialFolder + "/SoftBody_Wood.mat", new Color(0.55f, 0.32f, 0.18f, 1f), 0f, 0.3f);
        Material nodeMaterial = CreateLitMaterial(MaterialFolder + "/SoftBody_Node_Yellow.mat", new Color(0.93f, 1f, 0.18f, 1f), 0f, 0.3f);
        Material wheelNodeMaterial = CreateLitMaterial(MaterialFolder + "/SoftBody_Node_Cyan.mat", new Color(0.28f, 1f, 0.92f, 1f), 0f, 0.2f);
        Material beamMaterial = CreateUnlitMaterial(MaterialFolder + "/SoftBody_Beam_Green.mat", new Color(0.25f, 1f, 0.14f, 1f));
        Material damagedBeamMaterial = CreateUnlitMaterial(MaterialFolder + "/SoftBody_Beam_Damaged.mat", new Color(1f, 0.12f, 0.78f, 1f));

        GameObject minorImpactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MinorImpactPrefabPath);
        GameObject mediumImpactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MediumImpactPrefabPath);
        GameObject heavyImpactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeavyImpactPrefabPath);
        GameObject smokePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmokePrefabPath);
        GameObject sparkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SparkPrefabPath);

        CreateGroundAndArena(groundMaterial, laneMaterial, barrierMaterial, concreteMaterial, yellowStripeMaterial, woodMaterial);
        CreateLights();

        GameObject vehicleRoot = new GameObject("BeamSoftBodyVehicle_Car");
        vehicleRoot.transform.position = new Vector3(0f, 0.15f, -16f);
        vehicleRoot.transform.rotation = Quaternion.identity;

        BoxCollider chassisCollider = vehicleRoot.AddComponent<BoxCollider>();
        chassisCollider.center = new Vector3(0f, 0.95f, 0f);
        chassisCollider.size = new Vector3(2.6f, 1.7f, 5.4f);

        PhysicMaterial chassisPhysics = CreatePhysicsMaterial(MaterialFolder + "/SoftBody_ChassisPhysics.physicMaterial", 0.55f, 0.6f, 0.18f);
        chassisCollider.sharedMaterial = chassisPhysics;

        Rigidbody body = vehicleRoot.AddComponent<Rigidbody>();
        body.mass = 1450f;
        body.drag = 0.08f;
        body.angularDrag = 4.5f;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        BeamSoftBodyVehicle softBody = vehicleRoot.AddComponent<BeamSoftBodyVehicle>();
        BeamSoftBodyDriveController drive = vehicleRoot.AddComponent<BeamSoftBodyDriveController>();
        BeamSoftBodyCrashHandler crashHandler = vehicleRoot.AddComponent<BeamSoftBodyCrashHandler>();
        BeamSoftBodyMeshDeformer meshDeformer = vehicleRoot.AddComponent<BeamSoftBodyMeshDeformer>();

        VehicleAssetResult vehicleAsset = InstantiateVehicleAsset(vehicleRoot.transform, vehicleAssetMaterial);
        drive.AssignVisualWheels(vehicleAsset.wheels);

        List<BeamSoftBodyVehicle.SoftNode> nodes = new List<BeamSoftBodyVehicle.SoftNode>();
        Dictionary<string, int> nodeLookup = new Dictionary<string, int>();
        BuildBodyNodes(nodes, nodeLookup);

        List<BeamSoftBodyVehicle.SoftBeam> beams = new List<BeamSoftBodyVehicle.SoftBeam>();
        HashSet<string> beamKeys = new HashSet<string>();
        BuildBodyBeams(beams, beamKeys, nodeLookup);
        BuildWheelNodesAndBeams(nodes, nodeLookup, beams, beamKeys);

        BeamSoftBodyVehicle.SoftPanel[] panels = new BeamSoftBodyVehicle.SoftPanel[0];

        softBody.ConfigureForDemo(
            nodes.ToArray(),
            beams.ToArray(),
            panels,
            nodeMaterial,
            wheelNodeMaterial,
            beamMaterial,
            damagedBeamMaterial);
        softBody.RebuildVisuals();
        EditorUtility.SetDirty(softBody);

        AssignSerializedReference(crashHandler, "minorImpactVfx", minorImpactPrefab);
        AssignSerializedReference(crashHandler, "mediumImpactVfx", mediumImpactPrefab);
        AssignSerializedReference(crashHandler, "heavyImpactVfx", heavyImpactPrefab);
        AssignSerializedReference(crashHandler, "smokeVfx", smokePrefab);
        AssignSerializedReference(crashHandler, "sparkVfx", sparkPrefab);
        AssignSerializedReference(crashHandler, "vfxParticleMaterial", vfxParticleMaterial);

        meshDeformer.ConfigureForDemo(softBody, vehicleRoot.transform, vehicleAsset.bodyFilters);
        EditorUtility.SetDirty(meshDeformer);

        BeamSoftBodyFollowCamera followCam = CreateCameraAndHud(vehicleRoot.transform, softBody, drive, crashHandler);
        AssignSerializedReference(crashHandler, "shakeTarget", followCam);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built BeamSoftBodyVehicleDemo at " + ScenePath);
    }

    private static void BuildBodyNodes(List<BeamSoftBodyVehicle.SoftNode> nodes, Dictionary<string, int> lookup)
    {
        for (int x = 0; x < BodyX.Length; x++)
        {
            for (int y = 0; y < BodyY.Length; y++)
            {
                for (int z = 0; z < BodyZ.Length; z++)
                {
                    bool anchored = y == 0 && z >= 3;
                    string key = BodyKey(x, y, z);
                    AddNode(nodes, lookup, key, new Vector3(BodyX[x], BodyY[y], BodyZ[z]), anchored, false, y == 0 ? 1.8f : 1f);
                }
            }
        }

        for (int y = 0; y < BodyY.Length; y++)
        {
            for (int z = 0; z < BodyZ.Length; z++)
            {
                bool anchored = y == 0 && z >= 3;
                AddNode(nodes, lookup, CenterKey(y, z), new Vector3(0f, BodyY[y], BodyZ[z]), anchored, false, y == 0 ? 1.6f : 0.9f);
            }
        }
    }

    private static void BuildBodyBeams(
        List<BeamSoftBodyVehicle.SoftBeam> beams,
        HashSet<string> beamKeys,
        Dictionary<string, int> lookup)
    {
        for (int x = 0; x < BodyX.Length; x++)
        {
            for (int y = 0; y < BodyY.Length; y++)
            {
                for (int z = 0; z < BodyZ.Length; z++)
                {
                    if (z + 1 < BodyZ.Length)
                    {
                        AddBeam(beams, beamKeys, lookup, BodyKey(x, y, z), BodyKey(x, y, z + 1), 95f, 8f);
                    }

                    if (y + 1 < BodyY.Length)
                    {
                        AddBeam(beams, beamKeys, lookup, BodyKey(x, y, z), BodyKey(x, y + 1, z), 105f, 8f);
                    }

                    if (x + 1 < BodyX.Length)
                    {
                        AddBeam(beams, beamKeys, lookup, BodyKey(x, y, z), BodyKey(x + 1, y, z), 120f, 9f);
                    }
                }
            }
        }

        for (int x = 0; x < BodyX.Length; x++)
        {
            for (int y = 0; y < BodyY.Length - 1; y++)
            {
                for (int z = 0; z < BodyZ.Length - 1; z++)
                {
                    AddBeam(beams, beamKeys, lookup, BodyKey(x, y, z), BodyKey(x, y + 1, z + 1), 76f, 7f);
                    AddBeam(beams, beamKeys, lookup, BodyKey(x, y + 1, z), BodyKey(x, y, z + 1), 76f, 7f);
                }
            }
        }

        for (int y = 0; y < BodyY.Length; y++)
        {
            for (int z = 0; z < BodyZ.Length - 1; z++)
            {
                AddBeam(beams, beamKeys, lookup, BodyKey(0, y, z), BodyKey(1, y, z + 1), 78f, 7f);
                AddBeam(beams, beamKeys, lookup, BodyKey(1, y, z), BodyKey(0, y, z + 1), 78f, 7f);
            }
        }

        for (int y = 0; y < BodyY.Length; y++)
        {
            for (int z = 0; z < BodyZ.Length; z++)
            {
                if (z + 1 < BodyZ.Length)
                {
                    AddBeam(beams, beamKeys, lookup, CenterKey(y, z), CenterKey(y, z + 1), 105f, 8f);
                }

                if (y + 1 < BodyY.Length)
                {
                    AddBeam(beams, beamKeys, lookup, CenterKey(y, z), CenterKey(y + 1, z), 110f, 8f);
                }

                AddBeam(beams, beamKeys, lookup, CenterKey(y, z), BodyKey(0, y, z), 105f, 8f);
                AddBeam(beams, beamKeys, lookup, CenterKey(y, z), BodyKey(1, y, z), 105f, 8f);
            }
        }

        for (int y = 0; y < BodyY.Length - 1; y++)
        {
            for (int z = 0; z < BodyZ.Length - 1; z++)
            {
                AddBeam(beams, beamKeys, lookup, CenterKey(y, z), BodyKey(0, y + 1, z + 1), 72f, 6f);
                AddBeam(beams, beamKeys, lookup, CenterKey(y, z), BodyKey(1, y + 1, z + 1), 72f, 6f);
                AddBeam(beams, beamKeys, lookup, CenterKey(y + 1, z), BodyKey(0, y, z + 1), 72f, 6f);
                AddBeam(beams, beamKeys, lookup, CenterKey(y + 1, z), BodyKey(1, y, z + 1), 72f, 6f);
            }
        }
    }

    private static void BuildWheelNodesAndBeams(
        List<BeamSoftBodyVehicle.SoftNode> nodes,
        Dictionary<string, int> lookup,
        List<BeamSoftBodyVehicle.SoftBeam> beams,
        HashSet<string> beamKeys)
    {
        float[] sides = { -1.28f, 1.28f };
        float[] wheelZ = { -1.58f, 1.58f };

        for (int side = 0; side < sides.Length; side++)
        {
            for (int axle = 0; axle < wheelZ.Length; axle++)
            {
                string prefix = "Wheel_" + side + "_" + axle;
                Vector3 center = new Vector3(sides[side], 0.43f, wheelZ[axle]);
                AddNode(nodes, lookup, prefix + "_Center", center, axle == 1, true, 2.2f);

                int ringCount = 10;

                for (int i = 0; i < ringCount; i++)
                {
                    float angle = Mathf.PI * 2f * i / ringCount;
                    Vector3 offset = new Vector3(0f, Mathf.Sin(angle) * 0.36f, Mathf.Cos(angle) * 0.36f);
                    AddNode(nodes, lookup, prefix + "_Ring_" + i, center + offset, axle == 1 && i < 3, true, 0.6f);
                    AddBeam(beams, beamKeys, lookup, prefix + "_Center", prefix + "_Ring_" + i, 68f, 5f);
                    AddBeam(beams, beamKeys, lookup, prefix + "_Ring_" + i, prefix + "_Ring_" + ((i + 1) % ringCount), 58f, 4f);
                }

                string bodyBottom = BodyKey(side, 0, axle == 0 ? 1 : 3);
                string bodyMid = BodyKey(side, 1, axle == 0 ? 1 : 3);
                AddBeam(beams, beamKeys, lookup, prefix + "_Center", bodyBottom, 95f, 8f);
                AddBeam(beams, beamKeys, lookup, prefix + "_Center", bodyMid, 70f, 6f);
            }
        }
    }


    private struct VehicleAssetResult
    {
        public Transform[] wheels;
        public MeshFilter[] bodyFilters;
    }

    private static VehicleAssetResult InstantiateVehicleAsset(Transform parent, Material materialOverride)
    {
        GameObject prefab = LoadFirstPrefab(
            "Assets/MENA - Low Poly Cars/Prefabs (With Colliders)/Arizona.prefab",
            "Assets/MENA - Low Poly Cars/Prefabs (With Colliders)/Betsy.prefab",
            "Assets/MENA - Low Poly Cars/Prefabs (With Colliders)/Greta.prefab");

        if (prefab == null)
        {
            Debug.LogWarning("MENA vehicle prefab was not found. The deformable shell will still be created.");
            return default(VehicleAssetResult);
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

        if (instance == null)
        {
            return default(VehicleAssetResult);
        }

        instance.name = "MENA_LowPolyCar_Visual";
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            Object.DestroyImmediate(colliders[i]);
        }

        Rigidbody[] rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Object.DestroyImmediate(rigidbodies[i]);
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];

            if (r == null)
            {
                continue;
            }

            int slotCount = r.sharedMaterials != null ? r.sharedMaterials.Length : 0;

            if (slotCount <= 1)
            {
                r.sharedMaterial = materialOverride;
            }
            else
            {
                Material[] mats = new Material[slotCount];

                for (int j = 0; j < slotCount; j++)
                {
                    mats[j] = materialOverride;
                }

                r.sharedMaterials = mats;
            }
        }

        Transform[] wheels = FindWheelTransforms(instance.transform);
        MeshFilter[] bodyFilters = FindBodyMeshFilters(instance.transform, wheels);

        return new VehicleAssetResult
        {
            wheels = wheels,
            bodyFilters = bodyFilters
        };
    }

    private static MeshFilter[] FindBodyMeshFilters(Transform root, Transform[] wheels)
    {
        HashSet<Transform> wheelSet = new HashSet<Transform>();

        if (wheels != null)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                if (wheels[i] != null)
                {
                    wheelSet.Add(wheels[i]);
                }
            }
        }

        MeshFilter[] all = root.GetComponentsInChildren<MeshFilter>(true);
        List<MeshFilter> body = new List<MeshFilter>();

        for (int i = 0; i < all.Length; i++)
        {
            MeshFilter filter = all[i];

            if (filter == null || filter.sharedMesh == null)
            {
                continue;
            }

            if (IsUnderWheel(filter.transform, wheelSet))
            {
                continue;
            }

            body.Add(filter);
        }

        return body.ToArray();
    }

    private static bool IsUnderWheel(Transform candidate, HashSet<Transform> wheelSet)
    {
        Transform current = candidate;

        while (current != null)
        {
            if (wheelSet.Contains(current))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static Transform[] FindWheelTransforms(Transform root)
    {
        List<Transform> wheels = new List<Transform>();
        Transform[] all = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < all.Length; i++)
        {
            string name = all[i].name;

            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            string upper = name.ToUpperInvariant();

            if (upper == "FRW" || upper == "FLW" || upper == "RRW" || upper == "RLW"
                || upper.Contains("WHEEL") || upper.EndsWith("_WHEEL"))
            {
                wheels.Add(all[i]);
            }
        }

        return wheels.ToArray();
    }

    private static void CreateGroundAndArena(
        Material ground,
        Material lane,
        Material barrier,
        Material concrete,
        Material yellowStripe,
        Material wood)
    {
        GameObject groundObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        groundObject.name = "SoftBodyDemo_Ground";
        groundObject.transform.position = new Vector3(0f, -0.5f, 0f);
        groundObject.transform.localScale = new Vector3(60f, 1f, 50f);
        groundObject.GetComponent<Renderer>().sharedMaterial = ground;
        groundObject.isStatic = true;

        for (int i = 0; i < 14; i++)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "RoadStripe_" + i;
            stripe.transform.position = new Vector3(-13.5f + i * 2f, 0.02f, 0f);
            stripe.transform.localScale = new Vector3(1.4f, 0.04f, 0.2f);
            stripe.GetComponent<Renderer>().sharedMaterial = yellowStripe;
            stripe.isStatic = true;
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
        }

        CreateWall(new Vector3(0f, 1.2f, 24.5f), new Vector3(60f, 2.4f, 1f), concrete, "ArenaWall_North");
        CreateWall(new Vector3(0f, 1.2f, -24.5f), new Vector3(60f, 2.4f, 1f), concrete, "ArenaWall_South");
        CreateWall(new Vector3(29.5f, 1.2f, 0f), new Vector3(1f, 2.4f, 50f), concrete, "ArenaWall_East");
        CreateWall(new Vector3(-29.5f, 1.2f, 0f), new Vector3(1f, 2.4f, 50f), concrete, "ArenaWall_West");

        CreateBarrier(new Vector3(-7f, 0.55f, 6f), new Vector3(0.6f, 1.1f, 5.8f), barrier, "Barrier_LeftLane");
        CreateBarrier(new Vector3(7f, 0.55f, 6f), new Vector3(0.6f, 1.1f, 5.8f), barrier, "Barrier_RightLane");
        CreateBarrier(new Vector3(0f, 0.55f, 12.5f), new Vector3(7f, 1.1f, 0.6f), barrier, "Barrier_FrontGate");

        CreatePillar(new Vector3(-12f, 1.5f, -4f), 1.4f, 3f, concrete, "Pillar_BL");
        CreatePillar(new Vector3(12f, 1.5f, -4f), 1.4f, 3f, concrete, "Pillar_BR");
        CreatePillar(new Vector3(-15f, 1.5f, 8f), 1.4f, 3f, concrete, "Pillar_TL");
        CreatePillar(new Vector3(15f, 1.5f, 8f), 1.4f, 3f, concrete, "Pillar_TR");

        CreateRamp(new Vector3(0f, 0.001f, 18f), new Vector3(0f, 14f, 0f), wood, "JumpRamp");

        CreateCrate(new Vector3(-18f, 0.6f, 14f), wood, "Crate_LF");
        CreateCrate(new Vector3(-19f, 0.6f, 12.4f), wood, "Crate_LR");
        CreateCrate(new Vector3(-18.4f, 1.8f, 13.2f), wood, "Crate_LT");
        CreateCrate(new Vector3(18f, 0.6f, 14f), wood, "Crate_RF");
        CreateCrate(new Vector3(19f, 0.6f, 12.4f), wood, "Crate_RR");
        CreateCrate(new Vector3(18.4f, 1.8f, 13.2f), wood, "Crate_RT");

        for (int i = 0; i < 6; i++)
        {
            float x = -10f + i * 4f;
            CreateCone(new Vector3(x, 0f, -10f), barrier, "Cone_" + i);
        }

        Material laneStripe = lane;

        for (int i = 0; i < 8; i++)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "EdgeMarker_" + i;
            stripe.transform.position = new Vector3(-22f + i * 6f, 0.025f, -22f);
            stripe.transform.localScale = new Vector3(2f, 0.04f, 0.4f);
            stripe.GetComponent<Renderer>().sharedMaterial = laneStripe;
            stripe.isStatic = true;
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
        }
    }

    private static void CreateWall(Vector3 position, Vector3 scale, Material material, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = material;
        wall.isStatic = true;
    }

    private static void CreateBarrier(Vector3 position, Vector3 scale, Material material, string name)
    {
        GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrier.name = name;
        barrier.transform.position = position;
        barrier.transform.localScale = scale;
        barrier.GetComponent<Renderer>().sharedMaterial = material;
        barrier.isStatic = true;
    }

    private static void CreatePillar(Vector3 position, float radius, float height, Material material, string name)
    {
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = name;
        pillar.transform.position = position;
        pillar.transform.localScale = new Vector3(radius, height, radius);
        pillar.GetComponent<Renderer>().sharedMaterial = material;
        pillar.isStatic = true;
    }

    private static void CreateRamp(Vector3 position, Vector3 eulerAngles, Material material, string name)
    {
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = name;
        ramp.transform.position = position + new Vector3(0f, 0.6f, 0f);
        ramp.transform.localEulerAngles = eulerAngles;
        ramp.transform.localScale = new Vector3(8f, 0.4f, 5f);
        ramp.GetComponent<Renderer>().sharedMaterial = material;
        ramp.isStatic = true;
    }

    private static void CreateCrate(Vector3 position, Material material, string name)
    {
        GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crate.name = name;
        crate.transform.position = position;
        crate.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
        crate.GetComponent<Renderer>().sharedMaterial = material;

        Rigidbody crateBody = crate.AddComponent<Rigidbody>();
        crateBody.mass = 28f;
        crateBody.drag = 0.4f;
        crateBody.angularDrag = 1.2f;
    }

    private static void CreateCone(Vector3 basePosition, Material material, string name)
    {
        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.name = name;
        cone.transform.position = basePosition + new Vector3(0f, 0.4f, 0f);
        cone.transform.localScale = new Vector3(0.45f, 0.4f, 0.45f);
        cone.GetComponent<Renderer>().sharedMaterial = material;

        Rigidbody coneBody = cone.AddComponent<Rigidbody>();
        coneBody.mass = 1.4f;
        coneBody.drag = 0.6f;
        coneBody.angularDrag = 1.5f;
    }

    private static void CreateLights()
    {
        GameObject sun = new GameObject("Directional Light");
        Light sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 1.55f;
        sunLight.color = new Color(1f, 0.96f, 0.88f, 1f);
        sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        sunLight.shadows = LightShadows.Soft;

        GameObject fill = new GameObject("SoftBodyDemo_FillLight");
        Light fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Point;
        fillLight.intensity = 4f;
        fillLight.range = 16f;
        fillLight.color = new Color(0.5f, 0.7f, 1f, 1f);
        fill.transform.position = new Vector3(0f, 5f, 0f);
    }

    private static BeamSoftBodyFollowCamera CreateCameraAndHud(
        Transform vehicleTransform,
        BeamSoftBodyVehicle vehicle,
        BeamSoftBodyDriveController drive,
        BeamSoftBodyCrashHandler crashHandler)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        cameraObject.tag = "MainCamera";
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 250f;
        cameraObject.transform.position = vehicleTransform.position + new Vector3(0f, 4.2f, -9.2f);
        cameraObject.transform.LookAt(vehicleTransform.position + new Vector3(0f, 1.3f, 0.5f));

        BeamSoftBodyFollowCamera followCam = cameraObject.AddComponent<BeamSoftBodyFollowCamera>();
        AssignSerializedReference(followCam, "target", vehicleTransform);

        BeamSoftBodyHud hud = cameraObject.AddComponent<BeamSoftBodyHud>();
        AssignSerializedReference(hud, "vehicle", vehicle);
        AssignSerializedReference(hud, "drive", drive);
        AssignSerializedReference(hud, "crashHandler", crashHandler);

        return followCam;
    }

    private static void AssignSerializedReference(Object component, string propertyName, Object value)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(propertyName);

        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static int AddNode(
        List<BeamSoftBodyVehicle.SoftNode> nodes,
        Dictionary<string, int> lookup,
        string key,
        Vector3 position,
        bool anchored,
        bool wheelNode,
        float mass)
    {
        int index = nodes.Count;
        nodes.Add(new BeamSoftBodyVehicle.SoftNode
        {
            nodeName = key,
            restLocalPosition = position,
            anchored = anchored,
            wheelNode = wheelNode,
            mass = mass
        });
        lookup[key] = index;
        return index;
    }

    private static void AddBeam(
        List<BeamSoftBodyVehicle.SoftBeam> beams,
        HashSet<string> beamKeys,
        Dictionary<string, int> lookup,
        string aKey,
        string bKey,
        float stiffness,
        float damping)
    {
        int a;
        int b;

        if (!lookup.TryGetValue(aKey, out a) || !lookup.TryGetValue(bKey, out b) || a == b)
        {
            return;
        }

        int min = Mathf.Min(a, b);
        int max = Mathf.Max(a, b);
        string key = min + "_" + max;

        if (!beamKeys.Add(key))
        {
            return;
        }

        beams.Add(new BeamSoftBodyVehicle.SoftBeam
        {
            nodeA = a,
            nodeB = b,
            stiffness = stiffness,
            damping = damping,
            yieldStrain = 0.12f,
            plasticity = 2.2f,
            breakStrain = 0.85f
        });
    }

    private static string BodyKey(int x, int y, int z)
    {
        return "Body_" + x + "_" + y + "_" + z;
    }

    private static string CenterKey(int y, int z)
    {
        return "Center_" + y + "_" + z;
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

        SetCommonMaterialProperties(material, color, metallic, smoothness);
        return material;
    }

    private static Material CreateUnlitMaterial(string path, Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");

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

        SetCommonMaterialProperties(material, color, 0f, 0f);
        return material;
    }

    private static Material CreateUrpParticleAdditiveMaterial(string path, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
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

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 1f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", 0f);
        }

        material.renderQueue = 3000;
        material.SetOverrideTag("RenderType", "Transparent");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_BLENDMODE_ADD");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static PhysicMaterial CreatePhysicsMaterial(string path, float dynamicFriction, float staticFriction, float bounciness)
    {
        PhysicMaterial material = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(path);

        if (material == null)
        {
            material = new PhysicMaterial();
            AssetDatabase.CreateAsset(material, path);
        }

        material.dynamicFriction = dynamicFriction;
        material.staticFriction = staticFriction;
        material.bounciness = bounciness;
        material.frictionCombine = PhysicMaterialCombine.Average;
        material.bounceCombine = PhysicMaterialCombine.Average;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void SetCommonMaterialProperties(Material material, Color color, float metallic, float smoothness)
    {
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
            AssetDatabase.CreateFolder("Assets", "SoftBodyVehicleDemo");
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
