using UnityEngine;

[DisallowMultipleComponent]
public sealed class StressCollapseHud : MonoBehaviour
{
    [SerializeField] private StressCollapseBuilding building;
    [SerializeField] private Texture2D reticleTexture;
    [SerializeField] private bool showReticle = true;
    [SerializeField, Range(4f, 64f)] private float reticleSize = 14f;

    [SerializeField] private bool showWorldLabels = true;
    [SerializeField] private Camera labelCamera;
    [SerializeField, Range(0f, 1f)] private float labelDistanceFade = 0.4f;

    private GUIStyle labelStyle;
    private GUIStyle titleStyle;
    private GUIStyle smallStyle;
    private GUIStyle controlsStyle;
    private GUIStyle worldLabelStyle;
    private Texture2D whiteTex;
    private Texture2D panelTex;

    private void Awake()
    {
        if (building == null)
        {
            building = FindObjectOfType<StressCollapseBuilding>();
        }
    }

    private void OnDestroy()
    {
        if (whiteTex != null)
        {
            Destroy(whiteTex);
        }

        if (panelTex != null)
        {
            Destroy(panelTex);
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        if (building == null)
        {
            return;
        }

        DrawStressPanel();
        DrawControlsBar();

        if (showWorldLabels)
        {
            DrawWorldLabels();
        }

        if (showReticle)
        {
            DrawReticle();
        }
    }

    private void DrawWorldLabels()
    {
        Camera cam = labelCamera != null ? labelCamera : Camera.main;

        if (cam == null)
        {
            return;
        }

        EnsureLabelStyles();

        for (int i = 0; i < building.LayerCount; i++)
        {
            StressCollapseBuilding.StressLayer layer = building.GetLayer(i);

            if (layer == null || layer.layerRoot == null || layer.collapsed)
            {
                continue;
            }

            if (layer.pillars != null && layer.pillars.Length > 0)
            {
                for (int p = 0; p < layer.pillars.Length; p++)
                {
                    StressCollapseBuilding.StressPillarRef pillar = layer.pillars[p];

                    if (pillar == null || pillar.failed || pillar.pillarRoot == null)
                    {
                        continue;
                    }

                    DrawPillarLabel(cam, pillar);
                }
            }
            else
            {
                DrawLayerLabel(cam, layer);
            }
        }
    }

    private void DrawPillarLabel(Camera cam, StressCollapseBuilding.StressPillarRef pillar)
    {
        Renderer[] rends = pillar.cachedRenderers;
        Vector3 worldPos;

        if (rends != null && rends.Length > 0)
        {
            Bounds b = rends[0].bounds;

            for (int i = 1; i < rends.Length; i++)
            {
                if (rends[i] != null)
                {
                    b.Encapsulate(rends[i].bounds);
                }
            }

            worldPos = new Vector3(b.center.x, b.max.y + 0.45f, b.center.z);
        }
        else
        {
            worldPos = pillar.pillarRoot.position + Vector3.up * 1.2f;
        }

        Vector3 screen = cam.WorldToScreenPoint(worldPos);

        if (screen.z <= 0f)
        {
            return;
        }

        float ratio = pillar.pillarStrength > 0.001f ? Mathf.Clamp01(pillar.currentLoad / pillar.pillarStrength) : 0f;
        Color color = ColorForRatio(ratio);
        string text = string.Format("{0:0.0}/{1:0.0}", pillar.currentLoad, pillar.pillarStrength);

        DrawWorldText(screen, text, color, 11);
    }

    private void DrawLayerLabel(Camera cam, StressCollapseBuilding.StressLayer layer)
    {
        Renderer[] rends = layer.layerRoot.GetComponentsInChildren<Renderer>(true);

        if (rends.Length == 0)
        {
            return;
        }

        Bounds b = rends[0].bounds;

        for (int i = 1; i < rends.Length; i++)
        {
            if (rends[i] != null)
            {
                b.Encapsulate(rends[i].bounds);
            }
        }

        Vector3 worldPos = new Vector3(b.center.x, b.max.y + 0.6f, b.center.z);
        Vector3 screen = cam.WorldToScreenPoint(worldPos);

        if (screen.z <= 0f)
        {
            return;
        }

        float ratio = layer.strength > 0.001f ? Mathf.Clamp01(layer.currentLoad / layer.strength) : 0f;
        Color color = ColorForRatio(ratio);
        string text = string.Format("{0}: {1:0.0}/{2:0.0}", layer.layerName, layer.currentLoad, layer.strength);

        DrawWorldText(screen, text, color, 13);
    }

    private void DrawWorldText(Vector3 screenPos, string text, Color color, int fontSize)
    {
        Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
        GUIContent content = new GUIContent(text);

        worldLabelStyle.fontSize = fontSize;
        Vector2 size = worldLabelStyle.CalcSize(content);

        Rect bgRect = new Rect(guiPos.x - size.x * 0.5f - 4f, guiPos.y - size.y * 0.5f - 2f, size.x + 8f, size.y + 4f);
        DrawSolidRect(bgRect, new Color(0f, 0f, 0f, 0.6f));

        Color prev = GUI.color;
        GUI.color = new Color(color.r, color.g, color.b, 1f);
        GUI.Label(new Rect(guiPos.x - size.x * 0.5f, guiPos.y - size.y * 0.5f, size.x, size.y), content, worldLabelStyle);
        GUI.color = prev;
    }

    private Color ColorForRatio(float ratio)
    {
        if (ratio >= 1f)
        {
            return new Color(1f, 0.32f, 0.18f, 1f);
        }

        if (ratio >= 0.75f)
        {
            return new Color(1f, 0.55f, 0.22f, 1f);
        }

        if (ratio >= 0.5f)
        {
            return new Color(1f, 0.86f, 0.32f, 1f);
        }

        return new Color(0.7f, 0.95f, 0.6f, 1f);
    }

    private void EnsureLabelStyles()
    {
        if (worldLabelStyle != null)
        {
            return;
        }

        if (labelStyle == null)
        {
            EnsureStyles();
        }

        worldLabelStyle = new GUIStyle(labelStyle);
        worldLabelStyle.alignment = TextAnchor.MiddleCenter;
        worldLabelStyle.fontStyle = FontStyle.Bold;
        worldLabelStyle.fontSize = 12;
    }

    private void DrawStressPanel()
    {
        const float panelWidth = 380f;
        const float headerHeight = 76f;
        const float rowHeight = 50f;

        int layerCount = building.LayerCount;
        float panelHeight = headerHeight + 16f + Mathf.Max(1, layerCount) * rowHeight;

        Rect panelRect = new Rect(16f, 16f, panelWidth, panelHeight);
        DrawSolidRect(panelRect, new Color(0f, 0f, 0f, 0.66f));
        DrawSolidRect(new Rect(panelRect.x, panelRect.y, panelRect.width, 3f), new Color(1f, 0.66f, 0.18f, 1f));

        GUILayout.BeginArea(new Rect(panelRect.x + 14f, panelRect.y + 10f, panelRect.width - 28f, panelRect.height - 20f));
        GUILayout.Label("STRUCTURAL STRESS MONITOR", titleStyle);
        GUILayout.Label("Each floor's load (sum of weight above) vs strength.", smallStyle);
        GUILayout.Space(4f);

        for (int i = layerCount - 1; i >= 0; i--)
        {
            StressCollapseBuilding.StressLayer layer = building.GetLayer(i);

            if (layer == null)
            {
                continue;
            }

            DrawLayerRow(layer);
        }

        GUILayout.EndArea();
    }

    private void DrawLayerRow(StressCollapseBuilding.StressLayer layer)
    {
        float ratio = layer.strength > 0.001f ? Mathf.Clamp01(layer.currentLoad / layer.strength) : 0f;
        Color barColor;
        string status;

        if (layer.collapsed)
        {
            barColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            status = "DOWN";
        }
        else if (ratio >= 1f)
        {
            barColor = new Color(1f, 0.32f, 0.18f, 1f);
            status = "FAIL";
        }
        else if (ratio >= 0.75f)
        {
            barColor = new Color(1f, 0.55f, 0.22f, 1f);
            status = "CRIT";
        }
        else if (ratio >= 0.5f)
        {
            barColor = new Color(1f, 0.78f, 0.28f, 1f);
            status = "WARN";
        }
        else
        {
            barColor = new Color(0.46f, 0.86f, 0.42f, 1f);
            status = "OK";
        }

        Rect row = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.label, GUILayout.Height(46f));

        float labelWidth = 140f;
        float statusWidth = 46f;
        float gap = 8f;
        float barX = row.x + labelWidth + gap;
        float barWidth = row.width - labelWidth - statusWidth - gap * 2f;

        Rect labelRect = new Rect(row.x, row.y + 6f, labelWidth, 18f);
        Rect numRect = new Rect(row.x, row.y + 24f, labelWidth, 18f);
        Rect barRect = new Rect(barX, row.y + 8f, barWidth, 14f);
        Rect pillarsRect = new Rect(barX, row.y + 24f, barWidth, 18f);
        Rect statusRect = new Rect(row.x + row.width - statusWidth, row.y + 10f, statusWidth, 18f);

        GUI.Label(labelRect, layer.layerName, labelStyle);
        GUI.Label(numRect, string.Format("{0:0}/{1:0}  w{2:0}", layer.currentLoad, layer.strength, layer.weight), smallStyle);

        DrawSolidRect(barRect, new Color(0.16f, 0.16f, 0.18f, 1f));
        DrawSolidRect(new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(ratio), barRect.height), barColor);

        Color prev = GUI.color;
        GUI.color = barColor;
        GUI.Label(statusRect, status, smallStyle);
        GUI.color = prev;

        if (layer.pillars != null && layer.pillars.Length > 0)
        {
            int pillarCount = layer.pillars.Length;
            int alive = 0;

            for (int i = 0; i < pillarCount; i++)
            {
                if (layer.pillars[i] != null && !layer.pillars[i].failed)
                {
                    alive++;
                }
            }

            float perW = barWidth / pillarCount;

            for (int i = 0; i < pillarCount; i++)
            {
                Rect cell = new Rect(barX + perW * i + 1f, row.y + 26f, perW - 2f, 6f);
                bool ok = layer.pillars[i] != null && !layer.pillars[i].failed;
                Color c = ok
                    ? (alive < Mathf.Max(1, layer.minPillarsForSupport) ? new Color(1f, 0.55f, 0.22f, 1f) : new Color(0.55f, 0.85f, 0.55f, 1f))
                    : new Color(0.32f, 0.32f, 0.32f, 1f);
                DrawSolidRect(cell, c);
            }

            Rect pillarLabel = new Rect(barX, row.y + 32f, barWidth, 14f);
            Color prevC = GUI.color;
            GUI.color = alive < Mathf.Max(1, layer.minPillarsForSupport) ? new Color(1f, 0.32f, 0.18f, 1f) : new Color(0.78f, 0.82f, 0.86f, 1f);
            GUI.Label(pillarLabel, string.Format("pillars {0}/{1}", alive, pillarCount), smallStyle);
            GUI.color = prevC;
        }
    }

    private void DrawControlsBar()
    {
        const float height = 36f;
        Rect rect = new Rect(0f, Screen.height - height, Screen.width, height);
        DrawSolidRect(rect, new Color(0f, 0f, 0f, 0.66f));
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(1f, 0.66f, 0.18f, 1f));
        GUI.Label(new Rect(20f, rect.y + 9f, rect.width - 40f, height),
            "LMB / Space  fire stress damage    |    RMB drag  orbit    |    Wheel  zoom    |    Q / E  rotate    |    R  reset",
            controlsStyle);
    }

    private void DrawReticle()
    {
        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        if (reticleTexture != null)
        {
            GUI.DrawTexture(new Rect(cx - reticleSize * 0.5f, cy - reticleSize * 0.5f, reticleSize, reticleSize), reticleTexture);
            return;
        }

        Color outline = new Color(0f, 0f, 0f, 0.65f);
        Color core = new Color(1f, 0.86f, 0.35f, 0.95f);

        float arm = reticleSize;
        DrawSolidRect(new Rect(cx - arm, cy - 1f, arm * 2f, 2f), outline);
        DrawSolidRect(new Rect(cx - 1f, cy - arm, 2f, arm * 2f), outline);
        DrawSolidRect(new Rect(cx - arm + 1f, cy, arm * 2f - 2f, 1f), core);
        DrawSolidRect(new Rect(cx, cy - arm + 1f, 1f, arm * 2f - 2f), core);
        DrawSolidRect(new Rect(cx - 2f, cy - 2f, 4f, 4f), core);
    }

    private void DrawSolidRect(Rect rect, Color color)
    {
        if (whiteTex == null)
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
            whiteTex.hideFlags = HideFlags.HideAndDontSave;
        }

        Color prev = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTex);
        GUI.color = prev;
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 16;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(1f, 0.78f, 0.32f, 1f);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = Color.white;

        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.fontSize = 11;
        smallStyle.normal.textColor = new Color(0.86f, 0.86f, 0.9f, 1f);

        controlsStyle = new GUIStyle(GUI.skin.label);
        controlsStyle.fontSize = 12;
        controlsStyle.alignment = TextAnchor.MiddleCenter;
        controlsStyle.normal.textColor = new Color(1f, 0.92f, 0.74f, 1f);
    }
}
