// qFoldIT Toolbelt for Unity — UAGBridgeMechanics.cs
//
// Shared interaction-type vocabulary used by both UagValidator.cs (to
// decide what's a gap) and UAGBridgeTools.cs (to decide how to realize
// it). Two sources feed this list:
//
//   1. The 10 "mechanic" identifiers from
//      qfoldit-scientific-gameplay-framework-v0.1's gameplay-pattern
//      schema (gameplay.mechanic enum) — reference/compiler.py emits
//      interactions[].type equal to the mechanic name directly.
//   2. The legacy trigger-event vocabulary from the earlier informal UAG
//      schema draft (on_grab, on_click, etc.) plus "selection", seen in
//      qfoldit-engine-adapter-spec-v0.1's own hand-authored example
//      (examples/protein-folding.uag.json).
//
// Both vocabularies map to the same realization strategy in
// UAGBridgeTools.cs: ensure the target has a real Collider, attach a real
// (not generated-at-runtime) QFoldITInteractable component with
// InteractionType set, and wire OnMouseDown -> OnInteract so a click
// genuinely fires something, out of the box, with no further scripting
// required to get a first working interaction.

using System.Collections.Generic;

namespace QFoldIT.Toolbelt.Editor.Core
{
    public static class UAGBridgeMechanics
    {
        public static readonly HashSet<string> GameplayMechanics = new HashSet<string>
        {
            "construction", "optimization", "pattern_matching", "rhythm",
            "survival_defense", "racing_tuning", "spatial_puzzle",
            "portal_exploration", "investigation_annotation", "competitive_microtasks"
        };

        public static readonly HashSet<string> LegacyTriggers = new HashSet<string>
        {
            "on_grab", "on_proximity", "on_gaze", "on_click", "on_timer", "selection"
        };

        public static readonly HashSet<string> MappedInteractionTypes =
            new HashSet<string>(System.Linq.Enumerable.Concat(GameplayMechanics, LegacyTriggers));
    }
}
