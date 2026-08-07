// qFoldIT Toolbelt for Unity — UAGBridgeTools.cs
// Category: UAGBridge
//
// This is the piece that actually connects UNITY-TOOLBELT to the rest of
// the qFoldIT stack (SOS -> SKG -> SEM -> UAG -> UWI -> MCP). Everything
// else in this repo is a library of composite Unity actions; this file is
// the adapter that turns a Universal Assembly Graph into calls against
// that library — mirroring UEFN-TOOLBELT's unreal-world-builder skill:
// validate first, call existing tools (never re-implement primitives
// here), and report gaps explicitly instead of papering over them.
//
// Two tools:
//   uag_validate — pure validation + gap report, no engine mutation.
//   uag_apply    — runs uag_validate internally; if valid, realizes the
//                  graph by calling the same tool methods other MCP
//                  callers use (SceneTools.SpawnPrimitive, etc.).

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
        // ── uag_validate ────────────────────────────────────────────────
        public class UagValidateParams
        {
            [McpDescription("Full UAG v0.1 document as a JSON string (see qfoldit/UEFN-TOOLBELT's uag_schema.md)", Required = true)]
            public string UagJson { get; set; }
        }

        [McpTool("uag_validate", "Validates a UAG v0.1 graph against this engine's adapter: dangling id references, parent_child cycles, and which node/constraint/interaction types this adapter can and cannot realize. Makes no changes to the scene.")]
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
                errors = result.Errors,
                unmapped_node_types = result.UnmappedNodeTypes,
                unmapped_constraint_types = result.UnmappedConstraintTypes,
                unmapped_interactions = result.UnmappedInteractions.Select(i => new { i.Id, i.Trigger, i.TargetNode, i.Action }),
                node_count = graph.Nodes.Count,
                connection_count = graph.Connections.Count,
                constraint_count = graph.Constraints.Count,
                interaction_count = graph.Interactions.Count
            };
        }

        // ── uag_apply ───────────────────────────────────────────────────
        public class UagApplyParams
        {
            [McpDescription("Full UAG v0.1 document as a JSON string", Required = true)]
            public string UagJson { get; set; }
            [McpDescription("If true, generates a wired MonoBehaviour stub referencing every node with an unmapped interaction/logic constraint, instead of leaving them unrealized with no follow-up artifact", Default = true)]
            public bool GenerateInteractionStub { get; set; } = true;
            [McpDescription("Output path for the generated interaction stub script, relative to Assets/", Default = "Scripts/Generated/UagInteractionHandlers.cs")]
            public string StubOutputPath { get; set; } = "Scripts/Generated/UagInteractionHandlers.cs";
        }

        [McpTool("uag_apply", "Realizes a validated UAG v0.1 graph in the active scene by calling this toolbelt's own tools (spawn_primitive, light_create, parent_object, physics_add_collider, etc.) — the Universal World Interface adapter for Unity. Aborts with no scene changes if validation fails.")]
        public static object UagApply(UagApplyParams p)
        {
            UagGraph graph;
            try { graph = UagGraph.Parse(p.UagJson); }
            catch (System.Exception ex) { return new { success = false, error = $"Could not parse UAG JSON: {ex.Message}" }; }

            var validation = UagValidator.Validate(graph);
            if (!validation.IsValid)
                return new { success = false, error = "Validation failed — no changes made.", validation_errors = validation.Errors };

            var idMap = new Dictionary<string, string>();          // uag node id -> engine object name
            var nodeFailures = new List<object>();
            var unrealizedNodeIds = new HashSet<string>();          // nodes that exist in the graph but weren't created

            // ── Pass 1: create every node (flat, at its raw transform) ──
            foreach (var node in graph.Nodes)
            {
                try
                {
                    if (!UagValidator.MappedNodeTypes.Contains(node.Type))
                    {
                        unrealizedNodeIds.Add(node.Id);
                        continue; // reported via unmapped_node_types below, not silently invented
                    }

                    CreateNode(node);
                    idMap[node.Id] = node.Id; // every creation call below sets Name = node.Id directly
                    ApplyTransform(node);
                }
                catch (System.Exception ex)
                {
                    unrealizedNodeIds.Add(node.Id);
                    nodeFailures.Add(new { node_id = node.Id, type = node.Type, error = ex.Message });
                }
            }

            // ── Pass 2: parent_id hierarchy ──
            int reparented = 0;
            foreach (var node in graph.Nodes)
            {
                if (string.IsNullOrEmpty(node.ParentId) || unrealizedNodeIds.Contains(node.Id) || unrealizedNodeIds.Contains(node.ParentId))
                    continue;
                SceneTools.ParentObject(new SceneTools.ParentObjectParams { Child = node.Id, Parent = node.ParentId, WorldPositionStays = true });
                reparented++;
            }

            // ── Pass 3: connections ──
            int connectionsApplied = 0;
            var unmappedConnectionTypes = new HashSet<string>();
            foreach (var conn in graph.Connections)
            {
                if (unrealizedNodeIds.Contains(conn.FromNode) || unrealizedNodeIds.Contains(conn.ToNode)) continue;

                switch (conn.Type)
                {
                    case "parent_child":
                        SceneTools.ParentObject(new SceneTools.ParentObjectParams { Child = conn.FromNode, Parent = conn.ToNode, WorldPositionStays = true });
                        connectionsApplied++;
                        break;
                    case "joint_fixed":
                    case "joint_hinge":
                    case "joint_slider":
                        // physics_add_joint's JointType enum has no direct "Slider"
                        // case; Configurable is the closest approximation (it can be
                        // set up as a slider by locking the right axes, but this
                        // call alone does not configure those axis limits).
                        var jointType = conn.Type == "joint_fixed" ? "Fixed" : conn.Type == "joint_hinge" ? "Hinge" : "Configurable";
                        PhysicsTools.AddJoint(new PhysicsTools.AddJointParams { Name = conn.FromNode, JointType = jointType, ConnectedBody = conn.ToNode });
                        connectionsApplied++;
                        break;
                    default:
                        unmappedConnectionTypes.Add(conn.Type); // e.g. data_link — no Unity primitive
                        break;
                }
            }

            // ── Pass 4: constraints ──
            int constraintsApplied = 0;
            var interactionConstraintNodeIds = new HashSet<string>();
            foreach (var constraint in graph.Constraints)
            {
                var validTargets = constraint.TargetNodes.Where(t => !unrealizedNodeIds.Contains(t)).ToList();
                if (constraint.Type == "physics_collision")
                {
                    foreach (var target in validTargets)
                    {
                        string shape = (string)constraint.Properties["shape"] ?? "Box";
                        PhysicsTools.AddCollider(new PhysicsTools.AddColliderParams { Name = target, Shape = shape, IsTrigger = false });
                        PhysicsTools.AddRigidbody(new PhysicsTools.AddRigidbodyParams { Name = target });
                        constraintsApplied++;
                    }
                }
                else
                {
                    // interaction_grabbable / animation_trigger / logic_rule — no
                    // direct Unity primitive; collect for the codegen stub below.
                    foreach (var t in validTargets) interactionConstraintNodeIds.Add(t);
                }
            }
            foreach (var interaction in graph.Interactions)
                if (!unrealizedNodeIds.Contains(interaction.TargetNode))
                    interactionConstraintNodeIds.Add(interaction.TargetNode);

            // ── Optional: generate a wired stub for everything this adapter
            // couldn't realize live, so the gap produces a usable artifact
            // instead of just a text report. ──
            string stubPath = null;
            if (p.GenerateInteractionStub && interactionConstraintNodeIds.Count > 0)
            {
                var className = "UagInteractionHandlers";
                var codegenResult = CodeGenTools.CodegenMonoBehaviour(new CodeGenTools.CodegenMonoBehaviourParams
                {
                    ClassName = className,
                    ObjectNames = string.Join(",", interactionConstraintNodeIds),
                    OutputPath = p.StubOutputPath,
                    Namespace = "QFoldIT.Generated"
                });
                stubPath = $"Assets/{p.StubOutputPath}";
            }

            return new
            {
                success = true,
                nodes_created = idMap.Count,
                node_failures = nodeFailures,
                unmapped_node_types = validation.UnmappedNodeTypes,
                nodes_reparented = reparented,
                connections_applied = connectionsApplied,
                unmapped_connection_types = unmappedConnectionTypes,
                constraints_applied = constraintsApplied,
                unmapped_constraint_types = validation.UnmappedConstraintTypes,
                unmapped_interactions = validation.UnmappedInteractions.Select(i => new { i.Id, i.Trigger, i.TargetNode, i.Action }),
                interaction_stub_path = stubPath,
                id_map = idMap
            };
        }

        // ── Node type -> existing-tool dispatch ────────────────────────
        private static void CreateNode(UagNode node)
        {
            var pos = node.Transform?.Position ?? new float[] { 0, 0, 0 };
            float x = pos.Length > 0 ? pos[0] : 0, y = pos.Length > 1 ? pos[1] : 0, z = pos.Length > 2 ? pos[2] : 0;

            switch (node.Type)
            {
                case "mesh":
                    var meshRef = (string)node.Properties["mesh_ref"];
                    if (!string.IsNullOrEmpty(meshRef) && meshRef.StartsWith("Assets/"))
                        AssetTools.InstantiatePrefab(new AssetTools.InstantiatePrefabParams { PrefabPath = meshRef, X = x, Y = y, Z = z, Name = node.Id });
                    else
                        SceneTools.SpawnPrimitive(new SceneTools.SpawnPrimitiveParams { Type = (string)node.Properties["primitive"] ?? "Cube", Name = node.Id, X = x, Y = y, Z = z });
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
                    AudioTools.AddSource(new AudioTools.AddSourceParams
                    {
                        Name = CreateEmptyAnchor(node.Id, x, y, z),
                        ClipPath = (string)node.Properties["clip_ref"] ?? "",
                        Loop = (bool?)node.Properties["loop"] ?? false
                    });
                    break;

                case "particle_emitter":
                    ParticleTools.ApplyPreset(new ParticleTools.ApplyPresetParams { Name = node.Id, Preset = (string)node.Properties["preset"] ?? "Sparkle", X = x, Y = y, Z = z });
                    break;

                case "ui_panel":
                    // UI is Unity's 2D screen-space Widget/Canvas system — the UAG
                    // node's world x/y are reused as anchored screen pixels, which
                    // is a reasonable default but not a true 3D placement; a UAG
                    // producer targeting in-world UI should say so via properties
                    // and a future revision can branch on that.
                    UITools.CreatePanel(new UITools.CreatePanelParams { Name = node.Id, X = x, Y = y });
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
            var rot = node.Transform?.RotationEulerDeg ?? new float[] { 0, 0, 0 };
            var scl = node.Transform?.Scale ?? new float[] { 1, 1, 1 };
            SceneTools.TransformObject(new SceneTools.TransformObjectParams
            {
                Name = node.Id,
                RotX = rot.Length > 0 ? rot[0] : 0, RotY = rot.Length > 1 ? rot[1] : 0, RotZ = rot.Length > 2 ? rot[2] : 0,
                Scale = scl.Length > 0 ? scl[0] : 1f // uniform-scale tools only; non-uniform UAG scale is approximated by its X component
            });
        }

        private static string CreateEmptyAnchor(string name, float x, float y, float z)
        {
            SceneTools.SpawnGroupNode(new SceneTools.SpawnGroupNodeParams { Name = name, X = x, Y = y, Z = z });
            return name;
        }
    }
}
