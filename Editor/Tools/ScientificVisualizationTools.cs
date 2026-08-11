// qFoldIT Toolbelt for Unity — ScientificVisualizationTools.cs
// Category: ScientificVisualization
//
// The concrete "adapter"-level realization behind the scientific.visualization
// capability in qfoldit.adapter.json. Maps a UAG "scientific_subject/<mechanic>"
// node — the exact shape qfoldit-scientific-gameplay-framework-v0.1's
// reference/compiler.py emits for every themed pattern — to a real, visible,
// mechanic-differentiated scene object, plus a real bindings[] realization.

using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;
using QFoldIT.Toolbelt.Runtime;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class ScientificVisualizationTools
    {
        // A small, honest visualization scheme: primitive shape + material
        // preset differ by mechanic, so at minimum different scientific
        // subjects are visually distinguishable at a glance. This is not
        // "Niagara-style" live parameter-mapped feedback (gameplay-pattern
        // schema's feedback.niagara_parameter_mapping) — that requires a
        // live data connection this generic adapter doesn't own — but it
        // is a real, working default rather than an unmapped gap.
        private static (string primitive, string materialPreset) SchemeFor(string mechanic) => mechanic switch
        {
            "construction" => ("Cube", "Matte"),
            "optimization" => ("Sphere", "Neon"),
            "pattern_matching" => ("Cylinder", "Chrome"),
            "rhythm" => ("Sphere", "Toxic"),
            "survival_defense" => ("Capsule", "Rubber"),
            "racing_tuning" => ("Cylinder", "Gold"),
            "spatial_puzzle" => ("Cube", "Ice"),
            "portal_exploration" => ("Sphere", "Hologram"),
            "investigation_annotation" => ("Capsule", "Glass"),
            "competitive_microtasks" => ("Cube", "Emissive"),
            _ => ("Sphere", "Matte"),
        };

        // Note: unlike UNIGINE-TOOLBELT, Unity tools self-register via Unity
        // MCP's [McpTool] TypeCache scan — no explicit Register() call needed.
        public class CreateParams
        {
            [McpDescription("GameObject name for the visualization anchor", Required = true)]
            public string Name { get; set; }
            [McpDescription("The mechanic suffix of a 'scientific_subject/<mechanic>' UAG node type, e.g. 'construction'", Default = "")]
            public string Mechanic { get; set; } = "";
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 0f;
            public float Z { get; set; } = 0f;
            [McpDescription("Text shown on a floating world-space label above the anchor; empty = no label", Default = "")]
            public string Label { get; set; } = "";
            [McpDescription("scientific-state:// URI to bind, if any — attaches a QFoldITScientificBinding component", Default = "")]
            public string SourceUri { get; set; } = "";
        }

        [McpTool("scientific_visualization_create", "Creates a real, mechanic-differentiated visualization anchor for a UAG 'scientific_subject/<mechanic>' node: a shaped, colored primitive, an optional floating world-space text label, and a QFoldITScientificBinding component if a source URI is given.")]
        public static object Create(CreateParams p)
        {
            var (primitive, preset) = SchemeFor(p.Mechanic);

            var spawnResult = SceneTools.SpawnPrimitive(new SceneTools.SpawnPrimitiveParams
            {
                Type = primitive, Name = p.Name, X = p.X, Y = p.Y, Z = p.Z
            });

            MaterialTools.ApplyPreset(new MaterialTools.ApplyPresetParams { Name = p.Name, Preset = preset });

            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Failed to create visualization anchor '{p.Name}'." };

            if (!string.IsNullOrEmpty(p.SourceUri))
            {
                var binding = Undo.AddComponent<QFoldITScientificBinding>(go);
                binding.BindingId = $"{p.Name}-binding";
                binding.SourceUri = p.SourceUri;
                binding.TargetNodeId = p.Name;
            }

            string labelName = null;
            if (!string.IsNullOrEmpty(p.Label))
            {
                var canvasName = $"{p.Name}_Label_Canvas";
                UITools.CreateCanvas(new UITools.CreateCanvasParams
                {
                    Name = canvasName, RenderMode = "WorldSpace",
                    X = p.X, Y = p.Y + 1.2f, Z = p.Z,
                    WorldSpaceScale = 0.01f
                });
                UITools.CreateText(new UITools.CreateTextParams { Canvas = canvasName, Text = p.Label, FontSize = 32, X = 0, Y = 0 });
                labelName = canvasName;
            }

            return new
            {
                success = true,
                name = p.Name,
                mechanic = p.Mechanic,
                primitive,
                material_preset = preset,
                bound = !string.IsNullOrEmpty(p.SourceUri),
                label = labelName
            };
        }

        // ── scientific_binding_create ───────────────────────────────────
        public class BindParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("UAG bindings[].id", Required = true)]
            public string BindingId { get; set; }
            [McpDescription("scientific-state:// URI", Required = true)]
            public string SourceUri { get; set; }
        }

        [McpTool("scientific_binding_create", "Attaches a QFoldITScientificBinding component to an existing GameObject, giving a UAG bindings[] entry real, queryable substance instead of accepting-and-discarding it.")]
        public static object Bind(BindParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var binding = go.GetComponent<QFoldITScientificBinding>() ?? Undo.AddComponent<QFoldITScientificBinding>(go);
            binding.BindingId = p.BindingId;
            binding.SourceUri = p.SourceUri;
            binding.TargetNodeId = p.Name;

            return new { success = true, name = p.Name, binding_id = p.BindingId, source = p.SourceUri };
        }
    }
}
