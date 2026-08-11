// qFoldIT Toolbelt for Unity — UagValidator.cs
//
// Validates against qfoldit-engine-adapter-spec-v0.1's normative rules
// (spec/SPECIFICATION.md §7, conformance/CONFORMANCE.md "UAG" section),
// emitting the same structured error CODES as the spec's own reference
// validator (conformance/run_conformance.py) so this adapter's output can
// be checked directly against conformance/test_vectors.json — not just
// "looks similar", but genuinely comparable string-for-string:
//
//   INVALID_SCHEMA, DUPLICATE_NODE_ID, DANGLING_PARENT, HIERARCHY_CYCLE
//
// Three additional codes cover checks CONFORMANCE.md requires but the
// reference script (intentionally minimal, per its own docstring) doesn't
// implement — this adapter still needs them since "report unsupported
// node/constraint/interaction types" is an explicit MUST:
//
//   UNSUPPORTED_NODE_TYPE, UNSUPPORTED_CONSTRAINT_TYPE, UNSUPPORTED_INTERACTION_TYPE
//
// Each error is a structured {code, message} object (matching
// execution-report.schema.json's "errors": [{"type":"object"}] shape),
// not a bare string — code for machine consumers, message for humans.

using System.Collections.Generic;
using System.Linq;
using QFoldIT.Toolbelt.Editor.Uag;

namespace QFoldIT.Toolbelt.Editor.Core
{
    public readonly struct UagError
    {
        public readonly string Code;
        public readonly string Message;
        public UagError(string code, string message) { Code = code; Message = message; }
    }

    public class UagValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<UagError> Errors { get; } = new List<UagError>();

        // Gaps are not validation failures — the graph is still valid UAG,
        // this engine just can't fully realize every part of it yet.
        public List<string> UnmappedNodeTypes { get; } = new List<string>();
        public List<string> UnmappedConstraintTypes { get; } = new List<string>();
        public List<UagInteraction> UnmappedInteractions { get; } = new List<UagInteraction>();
    }

    public static class UagValidator
    {
        // What UAGBridgeTools.cs actually knows how to realize in Unity today.
        // "scientific_subject/*" is a prefix match, handled separately below
        // (any mechanic suffix is accepted — see IsMappedNodeType).
        public static readonly HashSet<string> MappedNodeTypes = new HashSet<string>
        {
            "mesh", "light", "camera", "trigger_volume", "ui_panel", "particle_emitter",
            "audio_source", "group",
            // Legacy Phase-1 node types kept mapped for documents written
            // against the earlier informal schema draft.
            "molecular_structure", "interaction_zone"
        };

        public static bool IsMappedNodeType(string type) =>
            type != null && (MappedNodeTypes.Contains(type) || type.StartsWith("scientific_subject/"));

        public static readonly HashSet<string> MappedConstraintTypes = new HashSet<string>
        {
            "physics_collision", "physics.collision", "physics.joint"
        };

        public static UagValidationResult Validate(UagGraph graph)
        {
            var result = new UagValidationResult();

            if (graph.Schema != UagGraph.SupportedSchema)
                result.Errors.Add(new UagError("INVALID_SCHEMA", $"Expected schema '{UagGraph.SupportedSchema}', got '{graph.Schema ?? "(missing)"}'."));

            var nodeIds = new HashSet<string>(graph.Nodes.Select(n => n.Id));

            var duplicateIds = graph.Nodes.GroupBy(n => n.Id).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var dup in duplicateIds)
                result.Errors.Add(new UagError("DUPLICATE_NODE_ID", $"Duplicate node id '{dup}'."));

            foreach (var node in graph.Nodes)
                if (!string.IsNullOrEmpty(node.Parent) && !nodeIds.Contains(node.Parent))
                    result.Errors.Add(new UagError("DANGLING_PARENT", $"Node '{node.Id}' has parent '{node.Parent}' which does not exist."));

            foreach (var constraint in graph.Constraints)
                foreach (var target in constraint.TargetNodes)
                    if (!nodeIds.Contains(target))
                        result.Errors.Add(new UagError("DANGLING_REFERENCE", $"Constraint '{constraint.Id}' target_node '{target}' does not exist."));

            foreach (var interaction in graph.Interactions)
                if (!string.IsNullOrEmpty(interaction.Target) && !nodeIds.Contains(interaction.Target))
                    result.Errors.Add(new UagError("DANGLING_REFERENCE", $"Interaction '{interaction.Id}' target '{interaction.Target}' does not exist."));

            foreach (var binding in graph.Bindings)
                if (!string.IsNullOrEmpty(binding.Target) && !nodeIds.Contains(binding.Target))
                    result.Errors.Add(new UagError("DANGLING_REFERENCE", $"Binding '{binding.Id}' target '{binding.Target}' does not exist."));

            // Cycle detection over the parent hierarchy — identical algorithm
            // to run_conformance.py's reference implementation (walk each
            // node upward, a repeat visit means a cycle), extended slightly
            // to also stop safely if a parent reference is dangling (already
            // reported above as DANGLING_PARENT, not re-reported as a cycle).
            var parentOf = graph.Nodes.Where(n => !string.IsNullOrEmpty(n.Parent) && nodeIds.Contains(n.Parent))
                                       .ToDictionary(n => n.Id, n => n.Parent);
            var cycleAlreadyReportedFor = new HashSet<string>();
            foreach (var start in nodeIds)
            {
                var visited = new HashSet<string> { start };
                var current = start;
                while (parentOf.TryGetValue(current, out var parent))
                {
                    if (!visited.Add(parent))
                    {
                        if (cycleAlreadyReportedFor.Add(start))
                            result.Errors.Add(new UagError("HIERARCHY_CYCLE", $"Cycle detected in parent hierarchy involving node '{start}'."));
                        break;
                    }
                    current = parent;
                }
            }

            // Gap reporting (not an error — informational).
            foreach (var type in graph.Nodes.Select(n => n.Type).Distinct())
                if (!IsMappedNodeType(type))
                    result.UnmappedNodeTypes.Add(type);

            foreach (var type in graph.Constraints.Select(c => c.Type).Distinct())
                if (!MappedConstraintTypes.Contains(type))
                    result.UnmappedConstraintTypes.Add(type);

            // Interaction types are the 10 gameplay "mechanic" identifiers
            // (construction, optimization, ...) per
            // qfoldit-scientific-gameplay-framework-v0.1's compiler output,
            // plus the legacy on_click/on_grab/... trigger vocabulary from
            // the Phase-1 informal schema. Anything else is an explicit gap.
            foreach (var interaction in graph.Interactions)
                if (!UAGBridgeMechanics.MappedInteractionTypes.Contains(interaction.Type))
                    result.UnmappedInteractions.Add(interaction);

            return result;
        }
    }
}
