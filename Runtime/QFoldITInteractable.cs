// qFoldIT Toolbelt for Unity — QFoldITInteractable.cs
//
// This is the concrete, working piece of the "interaction" capability
// declared in qfoldit.adapter.json. It is a real, pre-compiled component
// (not something generated per-call at Editor time, which Unity can't
// attach to a live GameObject without a domain reload anyway) that:
//
//   - Records which UAG interaction type it realizes (one of the 10
//     gameplay mechanics, or a legacy trigger like "on_click"/"selection").
//   - Fires a real, Inspector-wireable UnityEvent when the object is
//     clicked (OnMouseDown, which Unity calls automatically for any
//     GameObject with a Collider — no Input System setup required to get
//     a first working interaction).
//   - Exposes OnInteract as public so hand-written or generated gameplay
//     code (see CodeGenTools.CodegenMonoBehaviour) can subscribe to it
//     instead of re-deriving click detection from scratch.
//
// What this honestly does NOT do: implement full gameplay logic for any
// of the 10 mechanics (rhythm timing windows, racing physics tuning,
// survival wave spawning, etc.) — those remain genuinely game-specific.
// This component is the "adapter"-level capability level from
// spec/SPECIFICATION.md §5: a real, working piece of infrastructure that
// composed/generated gameplay code builds on, not a full native
// realization of every mechanic.

using UnityEngine;
using UnityEngine.Events;

namespace QFoldIT.Toolbelt.Runtime
{
    [DisallowMultipleComponent]
    public class QFoldITInteractable : MonoBehaviour
    {
        [Tooltip("The UAG interactions[].type this realizes — one of the 10 gameplay mechanics (construction, optimization, ...) or a legacy trigger (on_click, selection, ...).")]
        public string InteractionType;

        [Tooltip("Optional: the UAG node id this interactable corresponds to, for round-tripping back into world_state_export/scene_list_objects output.")]
        public string UagNodeId;

        [Tooltip("Fired when the player interacts with this object. Wired to OnMouseDown by default (works with any Collider, no Input System setup needed); replace or extend for mechanics that need continuous/analog input instead of a discrete click.")]
        public UnityEvent OnInteract;

        private void Reset()
        {
            // Interaction requires *something* to click on. If the caller
            // (UAGBridgeTools.RealizeInteraction) didn't already add one,
            // add a default so this component isn't silently inert.
            if (GetComponent<Collider>() == null)
                gameObject.AddComponent<BoxCollider>();
        }

        private void OnMouseDown()
        {
            OnInteract?.Invoke();
        }

        /// <summary>
        /// Called by generated/hand-written gameplay code instead of
        /// relying on the physics raycast path — useful for mechanics
        /// (rhythm, racing_tuning) where the trigger isn't a mouse click.
        /// </summary>
        public void Fire()
        {
            OnInteract?.Invoke();
        }
    }
}
