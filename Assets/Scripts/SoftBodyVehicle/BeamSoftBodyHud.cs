using UnityEngine;

[DisallowMultipleComponent]
public sealed class BeamSoftBodyHud : MonoBehaviour
{
    [SerializeField] private BeamSoftBodyVehicle vehicle;
    [SerializeField] private BeamSoftBodyDriveController drive;
    [SerializeField] private BeamSoftBodyCrashHandler crashHandler;

    private GUIStyle labelStyle;
    private GUIStyle titleStyle;
    private GUIStyle valueStyle;
    private GUIStyle warnStyle;

    private void Awake()
    {
        if (vehicle == null)
        {
            vehicle = FindObjectOfType<BeamSoftBodyVehicle>();
        }

        if (drive == null)
        {
            drive = FindObjectOfType<BeamSoftBodyDriveController>();
        }

        if (crashHandler == null)
        {
            crashHandler = FindObjectOfType<BeamSoftBodyCrashHandler>();
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUILayout.BeginArea(new Rect(16f, 16f, 470f, 220f), GUI.skin.box);
        GUILayout.Label("BeamNG-style Soft Body Vehicle Demo", titleStyle);
        GUILayout.Label("WASD drives. Space handbrakes. R resets.", labelStyle);
        GUILayout.Label("Crash into the barriers — dent depth scales with impact speed.", labelStyle);
        GUILayout.Space(4f);

        if (drive != null)
        {
            float kmh = drive.Speed * 3.6f;
            string grounded = drive.IsGrounded ? "grounded" : "airborne";
            GUILayout.Label(string.Format("Speed: {0:0.0} m/s   ({1:0} km/h)   {2}", drive.Speed, kmh, grounded), valueStyle);
        }

        if (crashHandler != null)
        {
            float since = Time.time - crashHandler.LastImpactTime;

            if (since < 0.7f && crashHandler.LastImpactIntensity > 0.1f)
            {
                GUILayout.Label(string.Format("CRASH! {0:0.0} m/s impact", crashHandler.LastImpactIntensity), warnStyle);
            }
            else
            {
                GUILayout.Label(string.Format("Last impact {0:0.0} m/s   ({1:0.0}s ago)", crashHandler.LastImpactIntensity, since), labelStyle);
            }
        }

        if (vehicle != null)
        {
            GUILayout.Label(vehicle.GetDebugSummary(), labelStyle);
        }

        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        if (labelStyle != null)
        {
            return;
        }

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;
        labelStyle.normal.textColor = Color.white;

        titleStyle = new GUIStyle(labelStyle);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;

        valueStyle = new GUIStyle(labelStyle);
        valueStyle.fontSize = 16;
        valueStyle.fontStyle = FontStyle.Bold;
        valueStyle.normal.textColor = new Color(0.55f, 1f, 0.6f);

        warnStyle = new GUIStyle(labelStyle);
        warnStyle.fontSize = 18;
        warnStyle.fontStyle = FontStyle.Bold;
        warnStyle.normal.textColor = new Color(1f, 0.32f, 0.18f);
    }
}
