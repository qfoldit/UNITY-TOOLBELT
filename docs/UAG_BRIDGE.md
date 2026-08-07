# UAG Bridge — qFoldIT Toolbelt for Unity

The UAG Bridge is what makes UNITY-TOOLBELT part of the qFoldIT stack
(`SOS → SKG → SEM → UAG → UWI → MCP`) rather than a standalone automation
kit. It's two tools, `uag_validate` and `uag_apply`, implemented in
[`Editor/Tools/UAGBridgeTools.cs`](../Editor/Tools/UAGBridgeTools.cs),
built on the UAG v0.1 model in
[`Editor/Core/UagModel.cs`](../Editor/Core/UagModel.cs) and the validator
in [`Editor/Core/UagValidator.cs`](../Editor/Core/UagValidator.cs).

The schema itself is not redefined here — it's copied field-for-field from
the canonical source: `qfoldit/UEFN-TOOLBELT`'s
`.claude/skills/game-designer/references/uag_schema.md`. If that schema
changes, `UagModel.cs` needs to change with it.

## Design principle

Same as UEFN-TOOLBELT's `unreal-world-builder` skill: **the bridge never
re-implements a primitive**. Every node/connection/constraint it can
realize, it realizes by calling this toolbelt's own already-registered
tools (`SceneTools.SpawnPrimitive`, `LightingTools.LightCreate`, etc.) —
the same tools any other MCP caller uses. What it *cannot* map, it reports
explicitly (`unmapped_node_types`, `unmapped_connection_types`,
`unmapped_constraint_types`, `unmapped_interactions`) rather than skipping
silently.

## The two tools

### `uag_validate(uag_json)`

Pure validation, no scene changes. Checks, in order:

1. **Duplicate node ids.**
2. **Dangling references** — every `parent_id`, `from_node`/`to_node`,
   `target_nodes[]`, and interaction `target_node` must resolve to an
   existing node id.
3. **Cycles** in the `parent_child` hierarchy (via `node.parent_id`,
   walked upward per node — a self-referential node counts as a cycle).
4. **Gap report** — which node/constraint types in the graph this adapter
   has no mapping for, and every interaction (interactions never have a
   live 1:1 realization — see below).

Returns `is_valid`, `errors[]`, and the three gap lists — gaps do **not**
make a graph invalid; only errors 1–3 do.

### `uag_apply(uag_json, generate_interaction_stub=true, stub_output_path=...)`

Runs the same validation internally first; **aborts with zero scene
changes if invalid**. If valid, executes in four passes:

1. Create every node whose type is mapped (unmapped types are recorded in
   `unmapped_node_types` and never touched again).
2. Apply `node.parent_id` hierarchy via `parent_object`.
3. Apply `connections[]`: `parent_child` → `parent_object`;
   `joint_fixed`/`joint_hinge`/`joint_slider` → `physics_add_joint`;
   anything else (e.g. `data_link`) → `unmapped_connection_types`.
4. Apply `constraints[]`: `physics_collision` → `physics_add_collider` +
   `physics_add_rigidbody` on every target; anything else
   (`interaction_grabbable`, `animation_trigger`, `logic_rule`) is
   collected, not silently dropped.

Every node with an interaction or an unmapped constraint targeting it, and
every interaction's own target node, are collected into one set. If
`generate_interaction_stub` is true (default) and that set is non-empty,
`uag_apply` calls `codegen_monobehaviour` once to generate a
`UagInteractionHandlers` MonoBehaviour with a public field for every one of
those nodes — a real, wired artifact instead of a text report you have to
act on by hand.

## Node type → tool mapping

| UAG `type` | Unity tool(s) called | Notes |
|---|---|---|
| `mesh` | `asset_instantiate_prefab` if `properties.mesh_ref` starts with `Assets/`, else `spawn_primitive` | `properties.primitive` selects the primitive shape (default `Cube`) |
| `light` | `light_create` | `properties.light_type`, `color_hex`, `intensity` |
| `camera` | `camera_create_rig` (`SetAsMain=false`) | `properties.fov` |
| `audio_source` | `spawn_group_node` (anchor) + `audio_add_source` | `properties.clip_ref` |
| `particle_emitter` | `particles_apply_preset` | `properties.preset`, default `Sparkle` |
| `ui_panel` | `ui_create_panel` | world x/y reused as 2D anchored screen position — **not** true 3D placement, see gap below |
| `trigger_volume` | `spawn_primitive` (Cube) + `physics_add_collider(is_trigger=true)` | |
| `group` | `spawn_group_node` | empty GameObject, no renderer — added specifically to close this gap (see below) |
| `custom` | *(none)* | always unmapped — there is no generic handler for a type UAG itself doesn't define |

## Known gaps (reported, not hidden)

- **`ui_panel` placement**: Unity's UI is a 2D screen-space Canvas system.
  A UAG node's 3D `transform.position` is reused as 2D anchored pixels,
  which is a reasonable default but not true in-world 3D UI. A future
  revision could branch on a `properties.world_space: true` flag.
- **`joint_slider`**: mapped to `physics_add_joint`'s `Configurable` joint
  type (Unity has no direct "Slider" joint enum case) — functionally close
  but not configured with the right axis locks by this call alone.
- **Non-uniform scale**: `physics_add_joint`/`transform_object`'s `Scale`
  parameter is a single float; a UAG `scale: [2, 1, 0.5]` is approximated
  by its X component only.
- **`data_link` connections** and **`interaction_grabbable` /
  `animation_trigger` / `logic_rule` constraints / all interaction
  triggers**: no live 1:1 Unity primitive exists for "this node's state
  drives that node's behavior" — these always surface as gaps, turned
  into the `UagInteractionHandlers` codegen stub rather than fabricated
  automatic behavior. This mirrors UEFN-TOOLBELT's own stated gap (no tool
  spawns a persistent `CameraActor` or Verse UI panel) — some things
  genuinely need a human or a follow-up code-generation pass, not a
  pretend automation.

## Example

```json
{
  "uag_version": "0.1",
  "nodes": [
    { "id": "lab_root", "type": "group" },
    { "id": "table", "type": "mesh", "parent_id": "lab_root",
      "transform": { "position": [0, 0, 0], "scale": [3, 0.2, 1.5] },
      "properties": { "primitive": "cube" } },
    { "id": "key_light", "type": "light", "parent_id": "lab_root",
      "properties": { "light_type": "point", "color_hex": "FFF4E0", "intensity": 1.5 } }
  ],
  "connections": [],
  "constraints": [
    { "id": "k1", "type": "physics_collision", "target_nodes": ["table"] }
  ],
  "interactions": []
}
```

Calling `uag_apply` with the above creates `lab_root` (empty container),
`table` (a scaled cube, parented under `lab_root`, with a collider and
rigidbody from the `physics_collision` constraint), and `key_light` (a
point light, parented under `lab_root`) — three tool calls total, all
through the existing `spawn_group_node` / `spawn_primitive` /
`physics_add_collider` / `physics_add_rigidbody` / `light_create` /
`parent_object` tools, none of them new code paths invented for this graph.

## How this was verified

`UagModel.cs` and `UagValidator.cs` have zero `UnityEngine`/`UnityEditor`
dependency by design, so before being committed, the validator's logic
(dangling references, 1/2/3-node cycles, false-positive check on a long
valid chain, duplicate ids, gap-vs-error distinction — 10 scenarios, 24
assertions) and the real `UagModel.cs`'s JSON deserialization (against the
real `Newtonsoft.Json.dll`, not a mock) were both compiled and run
standalone with `mcs`/`mono`, outside Unity entirely. See
[`Tests/Editor/UagValidatorTests.cs`](../Tests/Editor/UagValidatorTests.cs)
for the in-repo NUnit version of the same cases.

The orchestration logic in `uag_apply` itself (which tool gets called for
which node/connection/constraint type, in what order, with what
parameters) was **not** similarly simulated for Unity, since it calls
`UnityEditor`-dependent methods directly and can't run outside the Editor
easily — unlike UNIGINE-TOOLBELT's version of this file, which dispatches
through a name-based registry and *was* fully simulated end-to-end (see
`qfoldit-unigine-toolbelt/tests/uag_bridge_simulation/`). Treat `uag_apply`
here as verified-by-code-review against each called tool's real signature,
not by execution.
