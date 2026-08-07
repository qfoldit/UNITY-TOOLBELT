// qFoldIT Toolbelt for Unity — UagValidator.cs
//
// Implements exactly the three checks the UAG v0.1 schema requires an
// engine adapter to perform BEFORE calling any MCP tool:
//   1. Every parent_id / from_node / to_node / target_node / interaction
//      target_node reference resolves to an existing node id.
//   2. The parent_child hierarchy (via node.parent_id, reinforced by any
//      parent_child connections) contains no cycles.
//   3. Every node type, constraint type, and interaction is checked against
//      what this engine adapter can actually realize — reported explicitly,
//      never silently dropped.
//
// This class has zero UnityEngine/UnityEditor dependencies so it can be
// unit-tested in isolation (see Tests/Editor/UagValidatorTests.cs) without
// needing a live Editor.

using System.Collections.Generic;
using System.Linq;
using QFoldIT.Toolbelt.Editor.Uag;

namespace QFoldIT.Toolbelt.Editor.Core
{
    public class UagValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new List<string>();

        // Gaps are not validation failures — the graph is still valid UAG,
        // this engine just can't realize every part of it. Reported so the
        // caller can decide what to do (skip, codegen a stub, ask a human).
        public List<string> UnmappedNodeTypes { get; } = new List<string>();
        public List<string> UnmappedConstraintTypes { get; } = new List<string>();
        public List<UagInteraction> UnmappedInteractions { get; } = new List<UagInteraction>();
    }

    public static class UagValidator
    {
        // What UAGBridgeTools.cs actually knows how to realize in Unity today.
        public static readonly HashSet<string> MappedNodeTypes = new HashSet<string>
        {
            "mesh", "light", "camera", "trigger_volume", "ui_panel", "particle_emitter", "audio_source", "group"
            // "custom" is intentionally absent — there is no generic mapping for it.
        };

        public static readonly HashSet<string> MappedConstraintTypes = new HashSet<string>
        {
            "physics_collision"
            // interaction_grabbable / animation_trigger / logic_rule have no
            // direct Unity primitive — they become codegen stubs instead
            // (see UAGBridgeTools.GenerateInteractionStub), not a live realization.
        };

        public static UagValidationResult Validate(UagGraph graph)
        {
            var result = new UagValidationResult();
            var nodeIds = new HashSet<string>(graph.Nodes.Select(n => n.Id));

            // 1. Duplicate node ids
            var duplicateIds = graph.Nodes.GroupBy(n => n.Id).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var dup in duplicateIds)
                result.Errors.Add($"Duplicate node id '{dup}'.");

            // 2. Dangling references
            foreach (var node in graph.Nodes)
            {
                if (!string.IsNullOrEmpty(node.ParentId) && !nodeIds.Contains(node.ParentId))
                    result.Errors.Add($"Node '{node.Id}' has parent_id '{node.ParentId}' which does not exist.");
            }
            foreach (var conn in graph.Connections)
            {
                if (!nodeIds.Contains(conn.FromNode))
                    result.Errors.Add($"Connection '{conn.Id}' from_node '{conn.FromNode}' does not exist.");
                if (!nodeIds.Contains(conn.ToNode))
                    result.Errors.Add($"Connection '{conn.Id}' to_node '{conn.ToNode}' does not exist.");
            }
            foreach (var constraint in graph.Constraints)
            {
                foreach (var target in constraint.TargetNodes)
                    if (!nodeIds.Contains(target))
                        result.Errors.Add($"Constraint '{constraint.Id}' target_node '{target}' does not exist.");
            }
            foreach (var interaction in graph.Interactions)
            {
                if (!nodeIds.Contains(interaction.TargetNode))
                    result.Errors.Add($"Interaction '{interaction.Id}' target_node '{interaction.TargetNode}' does not exist.");
            }

            // 3. Cycle detection over the parent_id hierarchy (walk each node
            // upward; a repeat visit means a cycle). parent_child connections
            // are treated as reinforcing the same hierarchy, not a second
            // independent one, per the schema's single-hierarchy model.
            var parentOf = graph.Nodes.Where(n => !string.IsNullOrEmpty(n.ParentId) && nodeIds.Contains(n.ParentId))
                                       .ToDictionary(n => n.Id, n => n.ParentId);
            foreach (var start in nodeIds)
            {
                var visited = new HashSet<string> { start };
                var current = start;
                while (parentOf.TryGetValue(current, out var parent))
                {
                    if (!visited.Add(parent))
                    {
                        result.Errors.Add($"Cycle detected in parent_child hierarchy involving node '{start}'.");
                        break;
                    }
                    current = parent;
                }
            }

            // 4. Gap reporting (not an error — informational)
            foreach (var type in graph.Nodes.Select(n => n.Type).Distinct())
                if (!MappedNodeTypes.Contains(type))
                    result.UnmappedNodeTypes.Add(type);

            foreach (var type in graph.Constraints.Select(c => c.Type).Distinct())
                if (!MappedConstraintTypes.Contains(type))
                    result.UnmappedConstraintTypes.Add(type);

            // No interaction trigger has a live 1:1 engine realization today —
            // they always surface as gaps (turned into a codegen stub by
            // uag_apply, never silently executed as fabricated behaviour).
            result.UnmappedInteractions.AddRange(graph.Interactions);

            return result;
        }
    }
}
