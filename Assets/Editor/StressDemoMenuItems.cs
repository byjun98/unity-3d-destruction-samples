#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class StressDemoMenuItems
{
    [MenuItem("Tools/Stress Demo/Collapse From Bottom")]
    public static void CollapseFromBottom()
    {
        StressCollapseBuilding b = Object.FindObjectOfType<StressCollapseBuilding>();

        if (b == null)
        {
            Debug.LogWarning("No StressCollapseBuilding in scene.");
            return;
        }

        b.SendMessage("CollapseFromBottom", SendMessageOptions.DontRequireReceiver);
    }

    [MenuItem("Tools/Stress Demo/Fail All Bottom Pillars")]
    public static void FailAllBottomPillars()
    {
        StressCollapseBuilding b = Object.FindObjectOfType<StressCollapseBuilding>();

        if (b == null)
        {
            Debug.LogWarning("No StressCollapseBuilding in scene.");
            return;
        }

        b.SendMessage("FailAllBottomPillars", SendMessageOptions.DontRequireReceiver);
    }

    [MenuItem("Tools/Stress Demo/Damage Bottom Layer")]
    public static void DamageBottomLayer()
    {
        StressCollapseBuilding b = Object.FindObjectOfType<StressCollapseBuilding>();

        if (b == null)
        {
            Debug.LogWarning("No StressCollapseBuilding in scene.");
            return;
        }

        b.DamageLayer(0, 200f);
    }
}
#endif
