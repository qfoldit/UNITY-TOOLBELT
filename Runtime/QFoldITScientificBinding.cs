// qFoldIT Toolbelt for Unity — QFoldITScientificBinding.cs
//
// Concrete substance for UAG bindings[]: {id, source, target}. Without
// this, "applying a binding" would mean accepting-and-discarding a
// scientific-state:// URI — a silent fabrication of capability the
// specification explicitly forbids (SPECIFICATION.md §3: "An adapter
// MUST NOT silently fabricate unsupported functionality").
//
// This component makes the binding a real, inspectable, queryable fact
// about the GameObject: which scientific-state URI it's bound to, and
// (optionally) the last value read from it. It does NOT itself poll or
// subscribe to a live MCP backend — that would require a running network
// client inside the Unity Editor process talking to a specific qFoldIT
// science MCP server, which is out of scope for a generic engine adapter.
// What it DOES do honestly: hold the binding as real component data any
// other tool (world_state_export, a future scientific.state.query
// canonical action) can read back, and expose SetValue() for whatever
// process (a companion MCP client, a manual Editor script, a later
// polling tool) does own that connection to update it.
//
// This is the "adapter" capability level, explicitly, not "native" — see
// qfoldit.adapter.json's notes field for this repo's honest self-rating.

using UnityEngine;

namespace QFoldIT.Toolbelt.Runtime
{
    [DisallowMultipleComponent]
    public class QFoldITScientificBinding : MonoBehaviour
    {
        [Tooltip("UAG bindings[].id")]
        public string BindingId;

        [Tooltip("UAG bindings[].source — a scientific-state:// URI, e.g. scientific-state://protein_design_mcp/protein_folding_construction")]
        public string SourceUri;

        [Tooltip("UAG bindings[].target — should equal this GameObject's UAG node id")]
        public string TargetNodeId;

        [Tooltip("Last known value read from SourceUri, if anything has called SetValue(). Empty until then — this component does not fabricate a value.")]
        public string LastKnownValue = "";

        [Tooltip("UTC ISO-8601 timestamp of the last SetValue() call, empty if never set.")]
        public string LastUpdatedUtc = "";

        /// <summary>
        /// Called by whatever process actually owns the live connection to
        /// SourceUri (not by this component itself). Kept intentionally
        /// simple/string-typed since the scientific value's shape varies
        /// per domain (a scalar energy value, a JSON blob, etc.) and this
        /// component's job is just to hold and expose it, not interpret it.
        /// </summary>
        public void SetValue(string value)
        {
            LastKnownValue = value;
            LastUpdatedUtc = System.DateTime.UtcNow.ToString("o");
        }
    }
}
