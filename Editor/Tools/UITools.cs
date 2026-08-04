// qFoldIT Toolbelt for Unity — UITools.cs
// Category: UI
// Builds uGUI (Canvas / RectTransform) hierarchies — the built-in Unity UI
// system, available with no extra package dependency.

using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class UITools
    {
        // ── ui_create_canvas ───────────────────────────────────────────
        public class CreateCanvasParams
        {
            [McpDescription("Canvas name", Default = "Canvas")]
            public string Name { get; set; } = "Canvas";
            [McpDescription("Render mode", EnumType = typeof(RenderMode), Default = "ScreenSpaceOverlay")]
            public string RenderMode { get; set; } = "ScreenSpaceOverlay";
        }

        [McpTool("ui_create_canvas", "Creates a Canvas GameObject with CanvasScaler and GraphicRaycaster, plus an EventSystem if none exists.")]
        public static object CreateCanvas(CreateCanvasParams p)
        {
            var go = new GameObject(p.Name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = (RenderMode)System.Enum.Parse(typeof(RenderMode), p.RenderMode, true);
            Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Create Canvas");

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(es, "qFoldIT: Create EventSystem");
            }

            return new { success = true, name = go.name, render_mode = p.RenderMode };
        }

        private static RectTransform FindOrCreateCanvas(string canvasName)
        {
            var canvasGo = GameObject.Find(canvasName);
            if (canvasGo == null || canvasGo.GetComponent<Canvas>() == null)
            {
                var created = (object)CreateCanvas(new CreateCanvasParams { Name = string.IsNullOrEmpty(canvasName) ? "Canvas" : canvasName });
                canvasGo = GameObject.Find(string.IsNullOrEmpty(canvasName) ? "Canvas" : canvasName);
            }
            return canvasGo.GetComponent<RectTransform>();
        }

        // ── ui_create_button ────────────────────────────────────────────
        public class CreateButtonParams
        {
            [McpDescription("Parent Canvas name; created automatically if missing", Default = "Canvas")]
            public string Canvas { get; set; } = "Canvas";
            public string Label { get; set; } = "Button";
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 0f;
            public float Width { get; set; } = 160f;
            public float Height { get; set; } = 40f;
        }

        [McpTool("ui_create_button", "Creates a Button with a Text label under the given Canvas at an anchored position.")]
        public static object CreateButton(CreateButtonParams p)
        {
            var parent = FindOrCreateCanvas(p.Canvas);
            var go = new GameObject(p.Label + "_Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(p.X, p.Y);
            rt.sizeDelta = new Vector2(p.Width, p.Height);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.GetComponent<Text>();
            text.text = p.Label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one; textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;

            Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Create Button");
            return new { success = true, name = go.name };
        }

        // ── ui_create_text ──────────────────────────────────────────────
        public class CreateTextParams
        {
            public string Canvas { get; set; } = "Canvas";
            public string Text { get; set; } = "Text";
            public int FontSize { get; set; } = 24;
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 0f;
        }

        [McpTool("ui_create_text", "Creates a Text label under the given Canvas at an anchored position.")]
        public static object CreateText(CreateTextParams p)
        {
            var parent = FindOrCreateCanvas(p.Canvas);
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(p.X, p.Y);
            rt.sizeDelta = new Vector2(300, p.FontSize * 1.5f);

            var text = go.GetComponent<Text>();
            text.text = p.Text;
            text.fontSize = p.FontSize;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Create Text");
            return new { success = true, name = go.name, text = p.Text };
        }

        // ── ui_create_panel ─────────────────────────────────────────────
        public class CreatePanelParams
        {
            public string Canvas { get; set; } = "Canvas";
            [McpDescription("Panel background color as hex with alpha, e.g. 000000C0", Default = "FFFFFFC0")]
            public string ColorHex { get; set; } = "FFFFFFC0";
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 0f;
            public float Width { get; set; } = 400f;
            public float Height { get; set; } = 300f;
        }

        [McpTool("ui_create_panel", "Creates a background panel Image under the given Canvas.")]
        public static object CreatePanel(CreatePanelParams p)
        {
            var parent = FindOrCreateCanvas(p.Canvas);
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(p.X, p.Y);
            rt.sizeDelta = new Vector2(p.Width, p.Height);

            if (ColorUtility.TryParseHtmlString("#" + p.ColorHex.TrimStart('#'), out var c))
                go.GetComponent<Image>().color = c;

            Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Create Panel");
            return new { success = true, name = go.name };
        }

        // ── ui_create_slider ────────────────────────────────────────────
        public class CreateSliderParams
        {
            public string Canvas { get; set; } = "Canvas";
            public float Min { get; set; } = 0f;
            public float Max { get; set; } = 1f;
            public float Value { get; set; } = 0.5f;
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 0f;
        }

        [McpTool("ui_create_slider", "Creates a Slider control under the given Canvas.")]
        public static object CreateSlider(CreateSliderParams p)
        {
            var parent = FindOrCreateCanvas(p.Canvas);
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(p.X, p.Y);
            rt.sizeDelta = new Vector2(200, 20);

            var slider = go.GetComponent<Slider>();
            slider.minValue = p.Min; slider.maxValue = p.Max; slider.value = p.Value;

            Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Create Slider");
            return new { success = true, name = go.name };
        }

        // ── ui_create_image ─────────────────────────────────────────────
        public class CreateImageParams
        {
            public string Canvas { get; set; } = "Canvas";
            [McpDescription("Sprite asset path, e.g. Assets/UI/icon.png", Default = "")]
            public string SpritePath { get; set; } = "";
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 0f;
            public float Width { get; set; } = 100f;
            public float Height { get; set; } = 100f;
        }

        [McpTool("ui_create_image", "Creates an Image element under the given Canvas, optionally loading a sprite from the project.")]
        public static object CreateImage(CreateImageParams p)
        {
            var parent = FindOrCreateCanvas(p.Canvas);
            var go = new GameObject("Image", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(p.X, p.Y);
            rt.sizeDelta = new Vector2(p.Width, p.Height);

            if (!string.IsNullOrEmpty(p.SpritePath))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(p.SpritePath);
                if (sprite != null) go.GetComponent<Image>().sprite = sprite;
            }

            Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Create Image");
            return new { success = true, name = go.name };
        }

        // ── ui_set_anchor_preset ────────────────────────────────────────
        public enum AnchorPreset { TopLeft, TopCenter, TopRight, MiddleLeft, MiddleCenter, MiddleRight, BottomLeft, BottomCenter, BottomRight, StretchAll }

        public class SetAnchorPresetParams
        {
            [McpDescription("Target UI GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("Anchor preset", Required = true, EnumType = typeof(AnchorPreset))]
            public string Preset { get; set; }
        }

        [McpTool("ui_set_anchor_preset", "Sets a RectTransform's anchor min/max to a common layout preset (corners, edges, center, or stretch).")]
        public static object SetAnchorPreset(SetAnchorPresetParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return new { success = false, error = $"'{p.Name}' has no RectTransform." };

            var preset = (AnchorPreset)System.Enum.Parse(typeof(AnchorPreset), p.Preset, true);
            (Vector2 min, Vector2 max) = preset switch
            {
                AnchorPreset.TopLeft => (new Vector2(0, 1), new Vector2(0, 1)),
                AnchorPreset.TopCenter => (new Vector2(0.5f, 1), new Vector2(0.5f, 1)),
                AnchorPreset.TopRight => (new Vector2(1, 1), new Vector2(1, 1)),
                AnchorPreset.MiddleLeft => (new Vector2(0, 0.5f), new Vector2(0, 0.5f)),
                AnchorPreset.MiddleCenter => (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)),
                AnchorPreset.MiddleRight => (new Vector2(1, 0.5f), new Vector2(1, 0.5f)),
                AnchorPreset.BottomLeft => (new Vector2(0, 0), new Vector2(0, 0)),
                AnchorPreset.BottomCenter => (new Vector2(0.5f, 0), new Vector2(0.5f, 0)),
                AnchorPreset.BottomRight => (new Vector2(1, 0), new Vector2(1, 0)),
                AnchorPreset.StretchAll => (Vector2.zero, Vector2.one),
                _ => (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f))
            };

            Undo.RecordObject(rt, "qFoldIT: Set Anchor Preset");
            rt.anchorMin = min; rt.anchorMax = max;
            return new { success = true, name = p.Name, preset = preset.ToString() };
        }
    }
}
