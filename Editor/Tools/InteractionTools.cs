// qFoldIT Toolbelt for Unity — InteractionTools.cs
// Category: Interaction
//
// The concrete "adapter"-level realization behind the interaction
// capability in qfoldit.adapter.json. Ensures a target has both a real
// Collider (so it's physically clickable) and a real, pre-compiled
// QFoldITInteractable component (Runtime/QFoldITInteractable.cs) with its
// UnityEvent wired to fire on click out of the box — genuine working
// infrastructure, not a codegen stub the caller has to finish wiring
// before anything happens.

using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;
using QFoldIT.Toolbelt.Runtime;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class InteractionTools
    {
        // ── interaction_create ──────────────────────────────────────────
        public class CreateParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("UAG interactions[].type — one of the 10 gameplay mechanics (construction, optimization, ...) or a legacy trigger (on_click, selection, ...)", Required = true)]
            public string InteractionType { get; set; }
            [McpDescription("UAG node id this corresponds to, if different from Name", Default = "")]
            public string UagNodeId { get; set; } = "";
        }

        [McpTool("interaction_create", "Makes a GameObject interactable: ensures it has a Collider, then attaches a real QFoldITInteractable component whose OnInteract UnityEvent fires on click (OnMouseDown) out of the box — wire additional listeners onto OnInteract in the Inspector or via codegen_monobehaviour for mechanic-specific gameplay logic.")]
        public static object Create(CreateParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            if (go.GetComponent<Collider>() == null)
                Undo.AddComponent<BoxCollider>(go);

            var interactable = go.GetComponent<QFoldITInteractable>() ?? Undo.AddComponent<QFoldITInteractable>(go);
            interactable.InteractionType = p.InteractionType;
            interactable.UagNodeId = string.IsNullOrEmpty(p.UagNodeId) ? p.Name : p.UagNodeId;

            return new { success = true, name = p.Name, interaction_type = p.InteractionType };
        }

        // ── interaction_fire ────────────────────────────────────────────
        public class FireParams
        {
            [McpDescription("Target GameObject name (must already have a QFoldITInteractable)", Required = true)]
            public string Name { get; set; }
        }

        [McpTool("interaction_fire", "Manually invokes a GameObject's QFoldITInteractable.OnInteract event — useful in Play Mode for mechanics (rhythm, racing_tuning) whose trigger isn't a mouse click, or for testing wiring without clicking in the Game view.")]
        public static object Fire(FireParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var interactable = go.GetComponent<QFoldITInteractable>();
            if (interactable == null) return new { success = false, error = $"'{p.Name}' has no QFoldITInteractable — call interaction_create first." };

            if (!Application.isPlaying)
                return new { success = false, error = "interaction_fire requires Play Mode — UnityEvent listeners only run while playing." };

            interactable.Fire();
            return new { success = true, name = p.Name };
        }
    }
}
