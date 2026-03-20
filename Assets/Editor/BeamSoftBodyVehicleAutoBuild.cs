#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BeamSoftBodyVehicleAutoBuild
{
    private const string MarkerKey = "BeamSoftBodyVehicleAutoBuild_v6";

    static BeamSoftBodyVehicleAutoBuild()
    {
        EditorApplication.delayCall += MaybeBuildOnce;
    }

    private static void MaybeBuildOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (EditorPrefs.GetBool(MarkerKey, false))
        {
            return;
        }

        EditorPrefs.SetBool(MarkerKey, true);

        try
        {
            BeamSoftBodyVehicleDemoSceneBuilder.BuildScene();
            Debug.Log("[BeamSoftBodyVehicleAutoBuild] One-shot rebuild complete. Open the BeamSoftBodyVehicleDemo scene and press Play.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[BeamSoftBodyVehicleAutoBuild] Failed: " + e);
        }
    }

    [MenuItem("Tools/Demos/Reset Beam Soft Body Auto-Build Marker")]
    private static void ResetMarker()
    {
        EditorPrefs.DeleteKey(MarkerKey);
        Debug.Log("[BeamSoftBodyVehicleAutoBuild] Marker reset; next domain reload will rebuild the scene.");
    }
}
#endif
