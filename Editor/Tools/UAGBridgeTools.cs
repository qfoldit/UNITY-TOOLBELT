// qFoldIT Toolbelt for Unity — UAGBridgeTools.cs
// Category: UAGBridge
//
// Adapted to qfoldit-engine-adapter-spec-v0.1's formal contract:
//   - UagModel.cs now matches schemas/uag.schema.json (schema/scene/
//     node.parent/bindings), not the earlier informal Phase-1 shape.
//   - uag_validate emits {code, message} errors matching
//     conformance/test_vectors.json's error codes.
//   - uag_apply's return shape now matches
//     schemas/execution-report.schema.json: status (success/partial/
//     failed), engine, adapter, adapter_version, created/updated/skipped,
//     gaps/warnings/errors, provenance.
//   - "scientific_subject/<mechanic>" nodes and the 10 gameplay-mechanic
//     interaction types (construction, optimization, ...) — the exact
//     shape qfoldit-scientific-gameplay-framework-v0.1's
//     reference/compiler.py emits — are now REALLY realized (a visible,
//     mechanic-differentiated visualization anchor; a real, working
//     QFoldITInteractable component), not left as unmapped gaps.
//
// Design principle carried over unchanged from Phase 1: this file never
// re-implements a primitive — every node/constraint/interaction/binding
// it can realize, it realizes by calling this toolbelt's own
// already-registered tools.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using QFoldIT.Toolbelt.Editor.Core;
using QFoldIT.Toolbelt.Editor.Uag;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class UAGBridgeTools
    {
        public const string AdapterId = "qfoldit-unity-toolbelt";
        public const string AdapterVersion = "0.2.0";
        public const string EngineId = "unity";

        // ── uag_validate ────────────────────────────────────────────────
        public class UagValidateParams
        {
            [McpDescription("Full UAG document as a JSON string, conforming to qfoldit.uag/0.1 (schemas/uag.schema.json)", Required = true)]
            public string UagJson { get; set; }
        }

        [McpTool("uag_validate", "Validates a UAG document against this engine's adapter: schema id, duplicate/dangling references, hierarchy cycles, and which node/constraint/interaction types this adapter can and cannot realize. Makes no changes to the scene. Errors are {code, message} objects matching qfoldit-engine-adapter-spec-v0.1's conformance vectors.")]
        public static object UagValidateTool(UagValidateParams p)
        {
            UagGraph graph;
            try { graph = UagGraph.Parse(p.UagJson); }
            catch (System.Exception ex) { return new { success = false, error = $"Could not parse UAG JSON: {ex.Message}" }; }

            var result = UagValidator.Validate(graph);
            return new
            {
                success = true,
                is_valid = result.IsValid,
                errors = result.Errors.Select(e => new { code = e.Code, message = e.Message }),
                unmapped_node_types = result.UnmappedNodeTypes,
                unmapped_constraint_types = result.UnmappedConstraintTypes,
                unmapped_interactions = result.UnmappedInteractions.Select(i => new { i.Id, i.Type, i.Target }),
                node_count = graph.Nodes.Count,
                constraint_count = graph.Constraints.Count,
                interaction_count = graph.Interactions.Count,
                binding_count = graph.Bindings.Count
            };
        }

        // ── uag_apply ───────────────────────────────────────────────────
        public class UagApplyParams
        {
            [McpDescription("Full UAG document as a JSON string", Required = true)]
            public string UagJson { get; set; }
        }

        [McpTool("uag_apply", "Realizes a validated UAG document in the active scene by calling this toolbelt's own tools. Returns a structured execution report (status/created/updated/skipped/gaps/warnings/errors) matching schemas/execution-report.schema.json. Aborts with no scene changes if validation fails.")]
        public static object UagApply(UagApplyParams p)
        {
            UagGraph graph;
            try { graph = UagGraph.Parse(p.UagJson); }
            catch (System.Exception ex)
            {
                return Report("failed", errors: new object[] { new { code = "PARSE_ERROR", message = ex.Message } });
            }

            var validation = UagValidator.Validate(graph);
            if (!validation.IsValid)
            {
                return Report("failed",
                    errors: validation.Errors.Select(e => (object)new { code = e.Code, message = e.Message }),
                    provenance: Provenance(graph));
            }

            var created = new List<string>();
            var updated = new List<string>();
            var skipped = new List<string>();
            var gaps = new List<object>();
            var warnings = new List<object>();
            var errors = new List<object>();
            var unrealizedNodeIds = new HashSet<string>();

            // ── Pass 1: create every node ──
            foreach (var node in graph.Nodes)
            {
                if (!UagValidator.IsMappedNodeType(node.Type))
                {
                    unrealizedNodeIds.Add(node.Id);
                    skipped.Add(node.Id);
                    gaps.Add(new { element = "node", id = node.Id, type = node.Type, reason = "unmapped node type" });
                    continue;
                }

                try
                {
                    CreateNode(node);
                    created.Add(node.Id);
                    ApplyTransform(node);
                }
                catch (System.Exception ex)
                {
                    unrealizedNodeIds.Add(node.Id);
                    skipped.Add(node.Id);
                    errors.Add(new { code = "NODE_CREATE_FAILED", node_id = node.Id, type = node.Type, message = ex.Message });
                }
            }

            // ── Pass 2: parent hierarchy ──
            foreach (var node in graph.Nodes)
            {
                if (string.IsNullOrEmpty(node.Parent) || unrealizedNodeIds.Contains(node.Id) || unrealizedNodeIds.Contains(node.Parent))
                    continue;
                SceneTools.ParentObject(new SceneTools.ParentObjectParams { Child = node.Id, Parent = node.Parent, WorldPositionStays = true });
                if (!updated.Contains(node.Id)) updated.Add(node.Id);
            }

            // ── Pass 3: constraints ──
            foreach (var constraint in graph.Constraints)
            {
                var validTargets = constraint.TargetNodes.Where(t => !unrealizedNodeIds.Contains(t)).ToList();
                switch (constraint.Type)
                {
                    case "physics_collision":
                    case "physics.collision":
                        foreach (var target in validTargets)
                        {
                            string shape = (string)constraint.Properties["shape"] ?? "Box";
                            PhysicsTools.AddCollider(new PhysicsTools.AddColliderParams { Name = target, Shape = shape, IsTrigger = false });
                            PhysicsTools.AddRigidbody(new PhysicsTools.AddRigidbodyParams { Name = target });
                            if (!updated.Contains(target)) updated.Add(target);
                        }
                        break;
                    case "physics.joint":
                        if (validTargets.Count >= 1)
                        {
                            string jointType = (string)constraint.Properties["joint_type"] ?? "Fixed";
                            string connected = validTargets.Count >= 2 ? validTargets[1] : "";
                            PhysicsTools.AddJoint(new PhysicsTools.AddJointParams { Name = validTargets[0], JointType = jointType, ConnectedBody = connected });
                            if (!updated.Contains(validTargets[0])) updated.Add(validTargets[0]);
                        }
                        break;
                    default:
                        gaps.Add(new { element = "constraint", id = constraint.Id, type = constraint.Type, reason = "unmapped constraint type" });
                        break;
                }
            }

            // ── Pass 4: interactions — REAL realization via InteractionTools ──
            foreach (var interaction in graph.Interactions)
            {
                if (string.IsNullOrEmpty(interaction.Target) || unrealizedNodeIds.Contains(interaction.Target))
                {
                    gaps.Add(new { element = "interaction", id = interaction.Id, type = interaction.Type, reason = "target node was not realized" });
                    continue;
                }
                if (!UAGBridgeMechanics.MappedInteractionTypes.Contains(interaction.Type))
                {
                    gaps.Add(new { element = "interaction", id = interaction.Id, type = interaction.Type, reason = "unmapped interaction type" });
                    continue;
                }

                InteractionTools.Create(new InteractionTools.CreateParams
                {
                    Name = interaction.Target,
                    InteractionType = interaction.Type,
                    UagNodeId = interaction.Target
                });
                if (!updated.Contains(interaction.Target)) updated.Add(interaction.Target);

                if (UAGBridgeMechanics.GameplayMechanics.Contains(interaction.Type))
                {
                    warnings.Add(new
                    {
                        code = "INTERACTABLE_WIRED_NOT_GAMEPLAY_COMPLETE",
                        interaction_id = interaction.Id,
                        message = $"'{interaction.Target}' now has a real, clickable QFoldITInteractable(InteractionType={interaction.Type}), but full '{interaction.Type}' gameplay logic (scoring, progression, failure states) is not implemented by this generic adapter — subscribe to its OnInteract event."
                    });
                }
            }

            // ── Pass 5: bindings — REAL realization via ScientificVisualizationTools.Bind ──
            foreach (var binding in graph.Bindings)
            {
                if (string.IsNullOrEmpty(binding.Target) || unrealizedNodeIds.Contains(binding.Target))
                {
                    gaps.Add(new { element = "binding", id = binding.Id, reason = "target node was not realized" });
                    continue;
                }
                ScientificVisualizationTools.Bind(new ScientificVisualizationTools.BindParams
                {
                    Name = binding.Target,
                    BindingId = binding.Id,
                    SourceUri = binding.Source
                });
                if (!updated.Contains(binding.Target)) updated.Add(binding.Target);
            }

            string status = errors.Count > 0 && created.Count == 0 ? "failed"
                : (gaps.Count > 0 || warnings.Count > 0 || errors.Count > 0) ? "partial"
                : "success";

            return Report(status, created, updated, skipped, gaps, warnings, errors, Provenance(graph));
        }

        // ── Node type -> existing-tool dispatch ────────────────────────
        private static void CreateNode(UagNode node)
        {
            var pos = node.Position;
            float x = pos[0], y = pos[1], z = pos[2];

            if (node.Type.StartsWith("scientific_subject/"))
            {
                string mechanic = node.Type.Substring("scientific_subject/".Length);
                ScientificVisualizationTools.Create(new ScientificVisualizationTools.CreateParams
                {
                    Name = node.Id, Mechanic = mechanic, X = x, Y = y, Z = z,
                    Label = (string)node.Properties["label"] ?? "",
                    SourceUri = (string)node.Properties["source"] ?? ""
                });
                return;
            }

            switch (node.Type)
            {
                case "mesh":
                    var meshRef = (string)node.Properties["mesh_ref"];
                    if (!string.IsNullOrEmpty(meshRef) && meshRef.StartsWith("Assets/"))
                        AssetTools.InstantiatePrefab(new AssetTools.InstantiatePrefabParams { PrefabPath = meshRef, X = x, Y = y, Z = z, Name = node.Id });
                    else
                        SceneTools.SpawnPrimitive(new SceneTools.SpawnPrimitiveParams { Type = (string)node.Properties["primitive"] ?? "Cube", Name = node.Id, X = x, Y = y, Z = z });
                    break;

                case "molecular_structure":
                    // Legacy node type from the spec's own hand-authored example
                    // (examples/protein-folding.uag.json) — treated as a
                    // scientific subject with no specific mechanic scheme.
                    ScientificVisualizationTools.Create(new ScientificVisualizationTools.CreateParams
                    {
                        Name = node.Id, Mechanic = "", X = x, Y = y, Z = z,
                        SourceUri = (string)node.Properties["source"] ?? ""
                    });
                    break;

                case "interaction_zone":
                    SceneTools.SpawnPrimitive(new SceneTools.SpawnPrimitiveParams { Type = "Cube", Name = node.Id, X = x, Y = y, Z = z });
                    MaterialTools.ApplyPreset(new MaterialTools.ApplyPresetParams { Name = node.Id, Preset = "Ghost" });
                    PhysicsTools.AddCollider(new PhysicsTools.AddColliderParams { Name = node.Id, Shape = "Box", IsTrigger = true });
                    InteractionTools.Create(new InteractionTools.CreateParams
                    {
                        Name = node.Id,
                        InteractionType = (string)node.Properties["interaction"] ?? "selection",
                        UagNodeId = node.Id
                    });
                    break;

                case "light":
                    LightingTools.LightCreate(new LightingTools.LightCreateParams
                    {
                        Type = (string)node.Properties["light_type"] ?? "Point",
                        Name = node.Id, X = x, Y = y, Z = z,
                        ColorHex = (string)node.Properties["color_hex"] ?? "FFFFFF",
                        Intensity = (float?)node.Properties["intensity"] ?? 1f
                    });
                    break;

                case "camera":
                    CameraTools.CreateRig(new CameraTools.CreateRigParams { Name = node.Id, X = x, Y = y, Z = z, Fov = (float?)node.Properties["fov"] ?? 60f, SetAsMain = false });
                    break;

                case "audio_source":
                    SceneTools.SpawnGroupNode(new SceneTools.SpawnGroupNodeParams { Name = node.Id, X = x, Y = y, Z = z });
                    AudioTools.AddSource(new AudioTools.AddSourceParams
                    {
                        Name = node.Id,
                        ClipPath = (string)node.Properties["clip_ref"] ?? "",
                        Loop = (bool?)node.Properties["loop"] ?? false
                    });
                    break;

                case "particle_emitter":
                    ParticleTools.ApplyPreset(new ParticleTools.ApplyPresetParams { Name = node.Id, Preset = (string)node.Properties["preset"] ?? "Sparkle", X = x, Y = y, Z = z });
                    break;

                case "ui_panel":
                    bool worldSpace = (bool?)node.Properties["world_space"] ?? false;
                    if (worldSpace)
                    {
                        var canvasName = $"{node.Id}_Canvas";
                        UITools.CreateCanvas(new UITools.CreateCanvasParams { Name = canvasName, RenderMode = "WorldSpace", X = x, Y = y, Z = z });
                        UITools.CreatePanel(new UITools.CreatePanelParams { Name = node.Id, Canvas = canvasName, X = 0, Y = 0 });
                    }
                    else
                    {
                        UITools.CreatePanel(new UITools.CreatePanelParams { Name = node.Id, X = x, Y = y });
                    }
                    break;

                case "trigger_volume":
                    SceneTools.SpawnPrimitive(new SceneTools.SpawnPrimitiveParams { Type = "Cube", Name = node.Id, X = x, Y = y, Z = z });
                    PhysicsTools.AddCollider(new PhysicsTools.AddColliderParams { Name = node.Id, Shape = "Box", IsTrigger = true });
                    break;

                case "group":
                    SceneTools.SpawnGroupNode(new SceneTools.SpawnGroupNodeParams { Name = node.Id, X = x, Y = y, Z = z });
                    break;

                default:
                    throw new System.Exception($"No creation handler for node type '{node.Type}'.");
            }
        }

        private static void ApplyTransform(UagNode node)
        {
            var rot = node.RotationEulerDeg;
            var scl = node.Scale;
            SceneTools.TransformObject(new SceneTools.TransformObjectParams
            {
                Name = node.Id,
                RotX = rot[0], RotY = rot[1], RotZ = rot[2],
                ScaleX = scl[0], ScaleY = scl[1], ScaleZ = scl[2]
            });
        }

        private static object Provenance(UagGraph graph) => new
        {
            schema = graph.Schema,
            scene_id = graph.Scene?.Id,
            compiler = graph.Metadata != null && graph.Metadata["compiler"] != null ? (string)graph.Metadata["compiler"] : null
        };

        private static object Report(string status,
            IEnumerable<string> created = null, IEnumerable<string> updated = null, IEnumerable<string> skipped = null,
            IEnumerable<object> gaps = null, IEnumerable<object> warnings = null, IEnumerable<object> errors = null,
            object provenance = null) => new
        {
            success = status != "failed",
            status,
            engine = EngineId,
            adapter = AdapterId,
            adapter_version = AdapterVersion,
            created = created ?? System.Array.Empty<string>(),
            updated = updated ?? System.Array.Empty<string>(),
            skipped = skipped ?? System.Array.Empty<string>(),
            gaps = gaps ?? System.Array.Empty<object>(),
            warnings = warnings ?? System.Array.Empty<object>(),
            errors = errors ?? System.Array.Empty<object>(),
            provenance
        };
    }
}
