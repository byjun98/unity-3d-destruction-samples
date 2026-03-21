using UnityEngine;
using UnityEngine.UI;

namespace Slicing
{
    public class SliceCanvasUI : MonoBehaviour
    {
        public SliceController controller;
        Text _stats;

        public static SliceCanvasUI Build(SliceController controller)
        {
            var canvasGo = new GameObject("SliceCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var ui = canvasGo.AddComponent<SliceCanvasUI>();
            ui.controller = controller;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            MakeText(canvasGo.transform, "Hint",
                "[1] Slash mode    [2] Punch mode    [R] Reset\n" +
                "LMB drag = slash through scene    LMB / RMB on target (Punch) = cut at hit point",
                new Vector2(0f, 1f), new Vector2(40, -40), TextAnchor.UpperLeft, 30, font);

            ui._stats = MakeText(canvasGo.transform, "Stats",
                "Slices: 0\nMode: Slash",
                new Vector2(1f, 1f), new Vector2(-40, -40), TextAnchor.UpperRight, 36, font);

            return ui;
        }

        static Text MakeText(Transform parent, string name, string text, Vector2 anchor, Vector2 offset, TextAnchor align, int size, Font font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = new Vector2(1100, 240);

            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.alignment = align;
            t.fontSize = size;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.78f);
            shadow.effectDistance = new Vector2(2, -2);

            return t;
        }

        void Update()
        {
            if (controller == null || _stats == null) return;
            _stats.text = $"Slices: {controller.SliceCount}\nMode: {controller.Mode}";
        }
    }
}
