// qFoldIT Toolbelt for Unity — LightingTools.cs
// Category: Lighting

using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class LightingTools
    {
        // ── light_create ───────────────────────────────────────────────
        public class LightCreateParams
        {
            [McpDescription("Light type", Required = true, EnumType = typeof(LightType))]
            public string Type { get; set; }
            public string Name { get; set; } = "";
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 3f;
            public float Z { get; set; } = 0f;
            [McpDescription("Light color as hex, e.g. FFFFFF", Default = "FFFFFF")]
            public string ColorHex { get; set; } = "FFFFFF";
            public float Intensity { get; set; } = 1f;
            [McpDescription("Range in meters (point/spot only)", Default = 10f)]
            public float Range { get; set; } = 10f;
        }

        [McpTool("light_create", "Creates a directional, point, spot, or area light at a world position with a given color and intensity.")]
        public static object LightCreate(LightCreateParams p)
        {
            var type = (LightType)System.Enum.Parse(typeof(LightType), p.Type, true);
            var go = new GameObject(string.IsNullOrEmpty(p.Name) ? $"{type}Light" : p.Name);
            go.transform.position = new Vector3(p.X, p.Y, p.Z);
            var light = go.AddComponent<Light>();
            light.type = type;
            light.intensity = p.Intensity;
            light.range = p.Range;
            if (ColorUtility.TryParseHtmlString("#" + p.ColorHex.TrimStart('#'), out var c)) light.color = c;

            Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Create Light");
            return new { success = true, name = go.name, type = type.ToString() };
        }

        // ── light_set_skybox ───────────────────────────────────────────
        public class SetSkyboxParams
        {
            [McpDescription("Material asset path for the skybox, e.g. Assets/Skyboxes/Sunset.mat", Required = true)]
            public string MaterialPath { get; set; }
        }

        [McpTool("light_set_skybox", "Sets the scene's skybox material and triggers an environment lighting update.")]
        public static object SetSkybox(SetSkyboxParams p)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(p.MaterialPath);
            if (mat == null) return new { success = false, error = $"No material found at '{p.MaterialPath}'." };
            RenderSettings.skybox = mat;
            DynamicGI.UpdateEnvironment();
            return new { success = true, material_path = p.MaterialPath };
        }

        // ── light_set_ambient ──────────────────────────────────────────
        public class SetAmbientParams
        {
            [McpDescription("Ambient color as hex", Default = "B4B4B4")]
            public string ColorHex { get; set; } = "B4B4B4";
            public float Intensity { get; set; } = 1f;
        }

        [McpTool("light_set_ambient", "Sets flat ambient lighting color and intensity for the scene.")]
        public static object SetAmbient(SetAmbientParams p)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            if (ColorUtility.TryParseHtmlString("#" + p.ColorHex.TrimStart('#'), out var c))
                RenderSettings.ambientLight = c;
            RenderSettings.ambientIntensity = p.Intensity;
            return new { success = true, color = p.ColorHex, intensity = p.Intensity };
        }

        // ── light_set_fog ──────────────────────────────────────────────
        public class SetFogParams
        {
            public bool Enabled { get; set; } = true;
            [McpDescription("Fog color as hex", Default = "C8C8C8")]
            public string ColorHex { get; set; } = "C8C8C8";
            [McpDescription("Fog mode", EnumType = typeof(FogMode), Default = "ExponentialSquared")]
            public string Mode { get; set; } = "ExponentialSquared";
            public float Density { get; set; } = 0.01f;
        }

        [McpTool("light_set_fog", "Enables/configures scene fog: color, mode (Linear/Exponential/ExponentialSquared), and density.")]
        public static object SetFog(SetFogParams p)
        {
            RenderSettings.fog = p.Enabled;
            if (ColorUtility.TryParseHtmlString("#" + p.ColorHex.TrimStart('#'), out var c)) RenderSettings.fogColor = c;
            RenderSettings.fogMode = (FogMode)System.Enum.Parse(typeof(FogMode), p.Mode, true);
            RenderSettings.fogDensity = p.Density;
            return new { success = true, enabled = p.Enabled, mode = p.Mode, density = p.Density };
        }

        // ── light_bake_lightmaps ───────────────────────────────────────
        public class BakeLightmapsParams
        {
            [McpDescription("If true, returns immediately after starting an async bake instead of blocking", Default = false)]
            public bool Async { get; set; } = false;
        }

        [McpTool("light_bake_lightmaps", "Triggers a lightmap bake for the active scene using current Lighting Settings.")]
        public static object BakeLightmaps(BakeLightmapsParams p)
        {
            if (p.Async)
            {
                bool started = Lightmapping.BakeAsync();
                return new { success = started, mode = "async" };
            }
            bool ok = Lightmapping.Bake();
            return new { success = ok, mode = "blocking" };
        }

        // ── light_apply_preset ─────────────────────────────────────────
        public enum LightingPreset { Daylight, Sunset, Night, Studio, Moody, Overcast }

        public class ApplyPresetParams
        {
            [McpDescription("Lighting preset", Required = true, EnumType = typeof(LightingPreset))]
            public string Preset { get; set; }
        }

        [McpTool("light_apply_preset", "Applies a full lighting preset (sun color/angle, ambient, fog) in one call: daylight, sunset, night, studio, moody, overcast.")]
        public static object ApplyPreset(ApplyPresetParams p)
        {
            var preset = (LightingPreset)System.Enum.Parse(typeof(LightingPreset), p.Preset, true);
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun == null || sun.type != LightType.Directional)
            {
                var go = new GameObject("Directional Light (qFoldIT)");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Apply Lighting Preset");
            }

            switch (preset)
            {
                case LightingPreset.Daylight:
                    sun.color = Color.white; sun.intensity = 1.2f; sun.transform.eulerAngles = new Vector3(50, -30, 0);
                    RenderSettings.ambientLight = new Color(0.6f, 0.65f, 0.7f); RenderSettings.fog = false;
                    break;
                case LightingPreset.Sunset:
                    sun.color = new Color(1f, 0.55f, 0.3f); sun.intensity = 1f; sun.transform.eulerAngles = new Vector3(10, -60, 0);
                    RenderSettings.ambientLight = new Color(0.5f, 0.35f, 0.4f); RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.9f, 0.5f, 0.4f); RenderSettings.fogDensity = 0.008f;
                    break;
                case LightingPreset.Night:
                    sun.color = new Color(0.4f, 0.45f, 0.6f); sun.intensity = 0.15f; sun.transform.eulerAngles = new Vector3(-30, 20, 0);
                    RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.1f); RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.05f); RenderSettings.fogDensity = 0.02f;
                    break;
                case LightingPreset.Studio:
                    sun.color = Color.white; sun.intensity = 1.5f; sun.transform.eulerAngles = new Vector3(40, 0, 0);
                    RenderSettings.ambientLight = new Color(0.8f, 0.8f, 0.8f); RenderSettings.fog = false;
                    break;
                case LightingPreset.Moody:
                    sun.color = new Color(0.5f, 0.5f, 0.6f); sun.intensity = 0.5f; sun.transform.eulerAngles = new Vector3(70, -20, 0);
                    RenderSettings.ambientLight = new Color(0.15f, 0.15f, 0.2f); RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.2f, 0.2f, 0.25f); RenderSettings.fogDensity = 0.03f;
                    break;
                case LightingPreset.Overcast:
                    sun.color = new Color(0.8f, 0.8f, 0.85f); sun.intensity = 0.7f; sun.transform.eulerAngles = new Vector3(60, 0, 0);
                    RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f); RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.7f, 0.7f, 0.75f); RenderSettings.fogDensity = 0.015f;
                    break;
            }

            DynamicGI.UpdateEnvironment();
            return new { success = true, preset = preset.ToString() };
        }
    }
}
