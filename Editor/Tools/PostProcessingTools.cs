// qFoldIT Toolbelt for Unity — PostProcessingTools.cs
// Category: PostProcessing
// Uses URP's Volume framework (UnityEngine.Rendering.Volume +
// VolumeProfile), which ships with com.unity.render-pipelines.universal.
// If your project uses the Built-in Render Pipeline instead, these tools
// will still create the Volume component but visual effects will only
// apply if URP is active.

using System.IO;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class PostProcessingTools
    {
        // ── postfx_create_global_volume ────────────────────────────────
        public class CreateGlobalVolumeParams
        {
            public string Name { get; set; } = "GlobalVolume";
            [McpDescription("Output VolumeProfile asset path", Default = "Assets/PostProcessing/GlobalProfile.asset")]
            public string ProfilePath { get; set; } = "Assets/PostProcessing/GlobalProfile.asset";
        }

        [McpTool("postfx_create_global_volume", "Creates a global post-processing Volume GameObject with a new VolumeProfile asset.")]
        public static object CreateGlobalVolume(CreateGlobalVolumeParams p)
        {
            var dir = Path.GetDirectoryName(p.ProfilePath);
            if (!AssetDatabase.IsValidFolder(dir))
                CreateFolderRecursive(dir);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, p.ProfilePath);

            var go = new GameObject(p.Name, typeof(Volume));
            var volume = go.GetComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = profile;

            Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Create Global Volume");
            return new { success = true, name = p.Name, profile_path = p.ProfilePath };
        }

        private static void CreateFolderRecursive(string assetPath)
        {
            var parts = assetPath.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // ── postfx_set_bloom ────────────────────────────────────────────
        public class SetBloomParams
        {
            [McpDescription("VolumeProfile asset path", Required = true)]
            public string ProfilePath { get; set; }
            public bool Enabled { get; set; } = true;
            public float Intensity { get; set; } = 1f;
            public float Threshold { get; set; } = 1f;
        }

        [McpTool("postfx_set_bloom", "Adds/configures a Bloom override on a VolumeProfile (URP).")]
        public static object SetBloom(SetBloomParams p)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(p.ProfilePath);
            if (profile == null) return new { success = false, error = $"No VolumeProfile at '{p.ProfilePath}'." };

            if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
            bloom.active = p.Enabled;
            bloom.intensity.Override(p.Intensity);
            bloom.threshold.Override(p.Threshold);

            EditorUtility.SetDirty(profile);
            return new { success = true, profile = p.ProfilePath, intensity = p.Intensity };
        }

        // ── postfx_set_vignette ─────────────────────────────────────────
        public class SetVignetteParams
        {
            [McpDescription("VolumeProfile asset path", Required = true)]
            public string ProfilePath { get; set; }
            public bool Enabled { get; set; } = true;
            [McpDescription("Vignette color as hex", Default = "000000")]
            public string ColorHex { get; set; } = "000000";
            [McpDescription("Intensity, 0-1", Default = 0.4f)]
            public float Intensity { get; set; } = 0.4f;
            [McpDescription("Smoothness, 0-1", Default = 0.2f)]
            public float Smoothness { get; set; } = 0.2f;
        }

        [McpTool("postfx_set_vignette", "Adds/configures a Vignette override on a VolumeProfile (URP).")]
        public static object SetVignette(SetVignetteParams p)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(p.ProfilePath);
            if (profile == null) return new { success = false, error = $"No VolumeProfile at '{p.ProfilePath}'." };

            if (!profile.TryGet(out Vignette vignette)) vignette = profile.Add<Vignette>(true);
            vignette.active = p.Enabled;
            if (ColorUtility.TryParseHtmlString("#" + p.ColorHex.TrimStart('#'), out var c)) vignette.color.Override(c);
            vignette.intensity.Override(p.Intensity);
            vignette.smoothness.Override(p.Smoothness);

            EditorUtility.SetDirty(profile);
            return new { success = true, profile = p.ProfilePath };
        }

        // ── postfx_set_color_adjustments ───────────────────────────────
        public class SetColorAdjustmentsParams
        {
            [McpDescription("VolumeProfile asset path", Required = true)]
            public string ProfilePath { get; set; }
            public float PostExposure { get; set; } = 0f;
            [McpDescription("Contrast, -100..100", Default = 0f)]
            public float Contrast { get; set; } = 0f;
            [McpDescription("Saturation, -100..100", Default = 0f)]
            public float Saturation { get; set; } = 0f;
        }

        [McpTool("postfx_set_color_adjustments", "Adds/configures a Color Adjustments override on a VolumeProfile (URP): exposure, contrast, saturation.")]
        public static object SetColorAdjustments(SetColorAdjustmentsParams p)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(p.ProfilePath);
            if (profile == null) return new { success = false, error = $"No VolumeProfile at '{p.ProfilePath}'." };

            if (!profile.TryGet(out ColorAdjustments ca)) ca = profile.Add<ColorAdjustments>(true);
            ca.active = true;
            ca.postExposure.Override(p.PostExposure);
            ca.contrast.Override(p.Contrast);
            ca.saturation.Override(p.Saturation);

            EditorUtility.SetDirty(profile);
            return new { success = true, profile = p.ProfilePath };
        }

        // ── postfx_set_depth_of_field ───────────────────────────────────
        public class SetDepthOfFieldParams
        {
            [McpDescription("VolumeProfile asset path", Required = true)]
            public string ProfilePath { get; set; }
            public bool Enabled { get; set; } = true;
            [McpDescription("Focus distance in world units", Default = 10f)]
            public float FocusDistance { get; set; } = 10f;
            [McpDescription("Aperture, f-stop", Default = 5.6f)]
            public float Aperture { get; set; } = 5.6f;
        }

        [McpTool("postfx_set_depth_of_field", "Adds/configures a Depth of Field override on a VolumeProfile (URP, Bokeh mode).")]
        public static object SetDepthOfField(SetDepthOfFieldParams p)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(p.ProfilePath);
            if (profile == null) return new { success = false, error = $"No VolumeProfile at '{p.ProfilePath}'." };

            if (!profile.TryGet(out DepthOfField dof)) dof = profile.Add<DepthOfField>(true);
            dof.active = p.Enabled;
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(p.FocusDistance);
            dof.aperture.Override(p.Aperture);

            EditorUtility.SetDirty(profile);
            return new { success = true, profile = p.ProfilePath };
        }
    }
}
