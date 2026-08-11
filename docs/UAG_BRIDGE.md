# UAG Bridge — qFoldIT Toolbelt for Unity

The UAG Bridge is what connects UNITY-TOOLBELT to the rest of the qFoldIT
stack (`SOS → SKG → SEM → UAG → UWI → MCP`). As of this revision it
conforms to the **formal, normative** artifacts in
`qfoldit-engine-adapter-spec-v0.1`, not the earlier informal markdown
schema draft this bridge was originally built against:

- `Editor/Core/UagModel.cs` — matches `schemas/uag.schema.json` exactly.
- `Editor/Core/UagValidator.cs` — emits `{code, message}` errors matching
  `conformance/test_vectors.json` (verified against the actual,
  unmodified vector file — see `tests/conformance/`).
- `qfoldit.adapter.json` (repo root) — strictly valid against
  `schemas/adapter-manifest.schema.json`.
- `Editor/Tools/UAGBridgeTools.cs` — `uag_validate`/`uag_apply` return
  shape matches `schemas/execution-report.schema.json`
  (`status`/`created`/`updated`/`skipped`/`gaps`/`warnings`/`errors`/
  `provenance`).

## Schema, in brief

```json
{
  "schema": "qfoldit.uag/0.1",
  "scene": { "id": "...", "name": "...", "metadata": {} },
  "nodes": [
    { "id": "a", "type": "mesh", "parent": null,
      "transform": { "position": [0,0,0], "rotation_euler_deg": [0,0,0], "scale": [1,1,1] },
      "properties": {} }
  ],
  "constraints": [],
  "interactions": [{ "id": "i1", "type": "construction", "target": "a" }],
  "bindings": [{ "id": "b1", "source": "scientific-state://...", "target": "a" }]
}
```

Key differences from the earlier Phase-1 draft this bridge used to
target: `node.parent` (not `parent_id`); no `connections[]` array at all
(hierarchy is `node.parent` only); `interactions[].target` is singular
(not `target_node`); a new `bindings[]` array. `constraints[]`/
`interactions[]`/`bindings[]` internal shape is **not** mandated by the
formal JSON Schema (it only requires arrays of objects) — this file
documents and implements one specific, working interpretation, informed
by the one concrete producer that exists today:
`qfoldit-scientific-gameplay-framework-v0.1`'s `reference/compiler.py`.

## The two tools

### `uag_validate(uag_json)`

Checks, in order: `schema` value (`INVALID_SCHEMA`), duplicate node ids
(`DUPLICATE_NODE_ID`), dangling `parent`/constraint `target_nodes`/
interaction `target`/binding `target` references (`DANGLING_PARENT` /
`DANGLING_REFERENCE`), `parent` hierarchy cycles (`HIERARCHY_CYCLE`), then
reports gaps (unmapped node/constraint/interaction types) —
informational, not errors. No scene changes.

### `uag_apply(uag_json)`

Validates first; aborts with **zero** scene changes if invalid. If valid,
five passes: create nodes → apply `parent` hierarchy → apply
`constraints[]` → apply `interactions[]` (real realization, see below) →
apply `bindings[]` (real realization, see below). Returns a structured
execution report:

```json
{
  "status": "success | partial | failed",
  "engine": "unity", "adapter": "qfoldit-unity-toolbelt", "adapter_version": "0.2.0",
  "created": ["..."], "updated": ["..."], "skipped": ["..."],
  "gaps": [{"element": "node|constraint|interaction|binding", "id": "...", "type": "...", "reason": "..."}],
  "warnings": [{"code": "...", "message": "..."}],
  "errors": [{"code": "...", "message": "..."}],
  "provenance": { "schema": "qfoldit.uag/0.1", "scene_id": "...", "compiler": "..." }
}
```

## Node type → tool mapping

| UAG `type` | Unity tool(s) called | Notes |
|---|---|---|
| `mesh` | `asset_instantiate_prefab` if `properties.mesh_ref` set, else `spawn_primitive` | `properties.primitive` selects the shape |
| `light` | `light_create` | |
| `camera` | `camera_create_rig` | |
| `audio_source` | `spawn_group_node` + `audio_add_source` | |
| `particle_emitter` | `particles_apply_preset` | |
| `ui_panel` | `ui_create_panel`, or a `WorldSpace` canvas if `properties.world_space: true` | |
| `trigger_volume` | `spawn_primitive` + `physics_add_collider(is_trigger=true)` | |
| `group` | `spawn_group_node` | |
| `molecular_structure` | `scientific_visualization_create` | legacy type from the spec's own hand-authored example |
| `interaction_zone` | `spawn_primitive`(Ghost material, trigger) + `interaction_create` | legacy type; `properties.interaction` selects the interaction type |
| `scientific_subject/<mechanic>` | `scientific_visualization_create` | **the exact shape `reference/compiler.py` emits** — see below |
| `custom` | *(none)* | always unmapped |

## Real capability: `interaction` and `scientific.visualization`

These two capabilities blocked 4/5 and 5/5 of the currently-unlocked
gameplay patterns per `qfoldit-scientific-gameplay-framework-v0.1`'s own
`ROADMAP.md`. Rather than leaving every `interactions[]`/
`scientific_subject/*` element as an unmapped gap, this bridge now
realizes them for real:

- **`interaction_create`** (`Editor/Tools/InteractionTools.cs`) ensures
  the target has a `Collider`, then attaches
  `Runtime/QFoldITInteractable.cs` — a real, pre-compiled MonoBehaviour
  (in a dedicated **Runtime** assembly, not Editor-only, so it works in
  Play Mode and builds) whose `OnMouseDown` fires a public `UnityEvent`.
  Covers all 10 gameplay mechanics (`construction`, `optimization`,
  `pattern_matching`, `rhythm`, `survival_defense`, `racing_tuning`,
  `spatial_puzzle`, `portal_exploration`, `investigation_annotation`,
  `competitive_microtasks`) plus legacy triggers (`on_click`,
  `selection`, ...).
- **`scientific_visualization_create`**
  (`Editor/Tools/ScientificVisualizationTools.cs`) realizes a
  `scientific_subject/<mechanic>` node as a real, visible,
  mechanic-differentiated primitive (shape + material preset keyed by
  mechanic — e.g. `construction` → matte cube, `portal_exploration` →
  holographic sphere), with an optional floating world-space text label.
- **`scientific_binding_create`** attaches
  `Runtime/QFoldITScientificBinding.cs` — a real component holding the
  bound `scientific-state://` URI as genuine, queryable data (readable by
  `component_get_field`, `world_state_export`, or any future
  `scientific.state.query` action), instead of silently
  accepting-and-discarding the binding.

**Honest scope** — what this does NOT claim: full native gameplay logic
per mechanic (rhythm timing windows, survival wave spawning, racing
physics tuning — these remain genuinely game-specific and are out of
scope for a generic engine adapter), and live
`niagara_parameter_mapping`-style continuous visual feedback from a
running scientific process (no current example pattern even uses that
optional schema field). `uag_apply` emits an explicit `warning` for every
realized gameplay-mechanic interaction saying so, rather than letting
`status: "success"` imply more than was actually delivered.

## Known gaps (reported, not hidden)

- **`joint_slider`**: mapped to `physics_add_joint`'s `Configurable` type
  (Unity has no direct "Slider" joint enum case).
- **`data_link` and `logic_rule`-flavoured constraints**: no live 1:1
  Unity primitive for "this node's state drives that node's behavior" —
  reported as gaps.
- **`ui_panel` + `ApplyTransform` redundancy**: a `world_space: true`
  panel gets its transform written twice (once by the canvas creation,
  once by the bridge's uniform post-creation transform pass) — landing at
  approximately the same coordinates either way, self-correcting rather
  than harmful, but worth knowing about in a trace.

## Verified

- `Editor/Core/UagModel.cs`/`UagValidator.cs`/`UAGBridgeMechanics.cs` have
  zero `UnityEngine`/`UnityEditor` dependency by design — compiled and run
  standalone with `mcs`/`mono`, including against the **real, unmodified**
  `conformance/test_vectors.json` from `qfoldit-engine-adapter-spec-v0.1`
  and the **real, unmodified output** of running
  `qfoldit-scientific-gameplay-framework-v0.1`'s `reference/compiler.py`.
  See `tests/conformance/README.md`.
- `Tests/Editor/UagValidatorTests.cs` — the in-repo NUnit version of the
  same cases, also executed via a reflection-based runner outside Unity
  (11/11 passing) before being committed.
- **The compiled result**: running the spec's own unmodified
  `reference/compiler.py` against this repo's actual `qfoldit.adapter.json`
  compiles all 5 currently-unlocked gameplay patterns with
  `status=success` and zero gaps.
- `UAGBridgeTools.cs`'s orchestration logic itself (which tool gets called
  for which node/interaction/binding, in what order) was **not**
  similarly executed, since it depends on `UnityEditor` directly and can't
  run outside the Editor — verified by code review against each called
  tool's real signature, matching this repo's established practice (see
  `qfoldit-unigine-toolbelt`'s version of this bridge for the
  fully-simulated alternative, made possible there by its
  name-based-dispatch architecture).
