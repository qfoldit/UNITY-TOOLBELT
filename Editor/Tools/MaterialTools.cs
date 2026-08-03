// qFoldIT Toolbelt for Unity — MaterialTools.cs
// Category: Materials
// Mirrors UEFN Toolbelt's "Material Master": a fixed set of presets plus
// bulk swap / team-color split, all driven off Unity's built-in URP/
// Standard shader so it works without a custom shader package.

using System.Collections.Generic;
using System.Linq;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class MaterialTools
    {
        public enum MaterialPreset
        {
            Chrome, Neon, Hologram, Lava, Ice, Glass, Emissive,
            Matte, Rubber, Gold, Toxic, Ghost
        }

        private static Material BuildPresetMaterial(MaterialPreset preset)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = $"qFoldIT_{preset}" };

            switch (preset)
            {
                case MaterialPreset.Chrome:
                    mat.color = Color.white; TrySetFloat(mat, "_Metallic", 1f); TrySetFloat(mat, "_Smoothness", 0.95f); break;
                case MaterialPreset.Neon:
                    mat.color = Color.cyan; TrySetEmission(mat, Color.cyan * 3f); break;
                case MaterialPreset.Hologram:
                    mat.color = new Color(0.3f, 0.9f, 1f, 0.35f); TrySetEmission(mat, new Color(0.2f, 0.7f, 1f) * 1.5f); SetTransparent(mat); break;
                case MaterialPreset.Lava:
                    mat.color = new Color(1f, 0.25f, 0f); TrySetEmission(mat, new Color(1f, 0.3f, 0f) * 2f); break;
                case MaterialPreset.Ice:
                    mat.color = new Color(0.7f, 0.9f, 1f, 0.6f); TrySetFloat(mat, "_Smoothness", 0.9f); SetTransparent(mat); break;
                case MaterialPreset.Glass:
                    mat.color = new Color(1f, 1f, 1f, 0.15f); TrySetFloat(mat, "_Smoothness", 1f); SetTransparent(mat); break;
                case MaterialPreset.Emissive:
                    mat.color = Color.white; TrySetEmission(mat, Color.white * 4f); break;
                case MaterialPreset.Matte:
                    mat.color = Color.gray; TrySetFloat(mat, "_Smoothness", 0.05f); break;
                case MaterialPreset.Rubber:
                    mat.color = new Color(0.1f, 0.1f, 0.1f); TrySetFloat(mat, "_Smoothness", 0.15f); break;
                case MaterialPreset.Gold:
                    mat.color = new Color(1f, 0.84f, 0.2f); TrySetFloat(mat, "_Metallic", 1f); TrySetFloat(mat, "_Smoothness", 0.8f); break;
                case MaterialPreset.Toxic:
                    mat.color = new Color(0.5f, 1f, 0f); TrySetEmission(mat, new Color(0.5f, 1f, 0f) * 1.5f); break;
                case MaterialPreset.Ghost:
                    mat.color = new Color(0.8f, 0.85f, 1f, 0.2f); SetTransparent(mat); break;
            }
            return mat;
        }

        private static void TrySetFloat(Material m, string prop, float v) { if (m.HasProperty(prop)) m.SetFloat(prop, v); }
        private static void TrySetEmission(Material m, Color c)
        {
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        private static void SetTransparent(Material m)
        {
            m.SetFloat("_Surface", 1); // URP transparent
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        // ── material_apply_preset ──────────────────────────────────────
        public class ApplyPresetParams
        {
            [McpDescription("Name of the target GameObject", Required = true)]
            public string Name { get; set; }

            [McpDescription("Preset to apply", Required = true, EnumType = typeof(MaterialPreset))]
            public string Preset { get; set; }
        }

        [McpTool("material_apply_preset", "Applies one of 12 built-in material presets (chrome, neon, hologram, lava, ice, glass, emissive, matte, rubber, gold, toxic, ghost) to a GameObject's renderer.")]
        public static object ApplyPreset(ApplyPresetParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };
            var renderer = go.GetComponent<Renderer>();
            if (renderer == null) return new { success = false, error = $"'{p.Name}' has no Renderer component." };

            var preset = (MaterialPreset)System.Enum.Parse(typeof(MaterialPreset), p.Preset, true);
            Undo.RecordObject(renderer, "qFoldIT: Apply Material Preset");
            renderer.sharedMaterial = BuildPresetMaterial(preset);

            return new { success = true, name = p.Name, preset = preset.ToString() };
        }

        // ── material_bulk_swap ─────────────────────────────────────────
        public class BulkSwapParams
        {
            [McpDescription("Substring match on GameObject name; all matches are updated", Required = true)]
            public string NameContains { get; set; }

            [McpDescription("Preset to apply to every match", Required = true, EnumType = typeof(MaterialPreset))]
            public string Preset { get; set; }
        }

        [McpTool("material_bulk_swap", "Applies a material preset to every GameObject in the scene whose name contains the given substring.")]
        public static object BulkSwap(BulkSwapParams p)
        {
            var preset = (MaterialPreset)System.Enum.Parse(typeof(MaterialPreset), p.Preset, true);
            var mat = BuildPresetMaterial(preset);
            var matched = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .Where(r => r.gameObject.name.ToLowerInvariant().Contains(p.NameContains.ToLowerInvariant()))
                .ToList();

            foreach (var r in matched)
            {
                Undo.RecordObject(r, "qFoldIT: Bulk Material Swap");
                r.sharedMaterial = mat;
            }

            return new { success = true, preset = preset.ToString(), objects_updated = matched.Count };
        }

        // ── material_team_color_split ──────────────────────────────────
        public class TeamColorSplitParams
        {
            [McpDescription("Substring match for team A objects", Required = true)]
            public string TeamAContains { get; set; }

            [McpDescription("Substring match for team B objects", Required = true)]
            public string TeamBContains { get; set; }

            [McpDescription("Team A hex color, e.g. FF3B30", Default = "FF3B30")]
            public string TeamAColor { get; set; } = "FF3B30";

            [McpDescription("Team B hex color, e.g. 0A84FF", Default = "0A84FF")]
            public string TeamBColor { get; set; } = "0A84FF";
        }

        [McpTool("material_team_color_split", "Colors two groups of objects (matched by name substring) in two distinct team colors — for arenas, capture points, spawn markers.")]
        public static object TeamColorSplit(TeamColorSplitParams p)
        {
            int UpdateGroup(string contains, string hex)
            {
                if (!ColorUtility.TryParseHtmlString("#" + hex.TrimStart('#'), out var color))
                    color = Color.white;
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var mat = new Material(shader) { color = color };

                var matched = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                    .Where(r => r.gameObject.name.ToLowerInvariant().Contains(contains.ToLowerInvariant()))
                    .ToList();
                foreach (var r in matched)
                {
                    Undo.RecordObject(r, "qFoldIT: Team Color Split");
                    r.sharedMaterial = mat;
                }
                return matched.Count;
            }

            int a = UpdateGroup(p.TeamAContains, p.TeamAColor);
            int b = UpdateGroup(p.TeamBContains, p.TeamBColor);
            return new { success = true, team_a_updated = a, team_b_updated = b };
        }

        // ── material_list_presets ──────────────────────────────────────
        public class ListPresetsParams
        {
            // No inputs required — Unity MCP still needs a params type for
            // the typed-parameter registration path.
        }

        [McpTool("material_list_presets", "Lists all available material preset names.")]
        public static object ListPresets(ListPresetsParams p)
        {
            var names = System.Enum.GetNames(typeof(MaterialPreset));
            return new { success = true, presets = names };
        }
    }
}
