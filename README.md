# qFoldIT Toolbelt — Unity

**109 composite editor-automation tools for Unity, exposed to AI agents through Unity's own official MCP bridge.**

> Built by **qFoldIT** — foundation release, 2026

---

## What this is

Unity 2023+ ships its own MCP server built into the Editor
(`com.unity.ai.assistant`, "Unity MCP"). It already gives an AI agent a
generic, low-level surface: run C#, query the scene, call arbitrary editor
APIs. **qFoldIT Toolbelt does not replace that** — it registers a second
layer of higher-level, *composite* tools on top of it, the same way
[UEFN Toolbelt](https://github.com/undergroundrap/UEFN-TOOLBELT) built 355
named commands on top of UEFN's raw Python API instead of making an agent
write one-off scripts every time.

Instead of an agent writing:
```csharp
// 15 lines of GameObject.CreatePrimitive + loop math + material setup
// to place 12 crates in a circle with a neon material...
```
it calls:
```
procedural_place(pattern="circle", count=12, radius=5)
material_bulk_swap(name_contains="Crate", preset="neon")
```

## Architecture

```
Claude / any MCP client
    │  MCP protocol (stdio, via Unity's relay binary)
    ▼
Unity Editor  ──  McpToolRegistry (built into com.unity.ai.assistant)
    │
    ▼
qFoldIT Toolbelt tools  (Editor/Tools/*.cs, [McpTool] attributes)
    │
    ▼
UnityEditor / UnityEngine APIs
```

No external process, no HTTP relay, no polling — this package is pure C#
that Unity's own `TypeCache` scan discovers at Editor startup, exactly as
documented in
[Register custom MCP tools](https://docs.unity3d.com/Packages/com.unity.ai.assistant@2.0/manual/unity-mcp-tool-registration.html).

See [ARCHITECTURE.md](ARCHITECTURE.md) for the full design and
[docs/TOOL_REFERENCE.md](docs/TOOL_REFERENCE.md) for every tool's signature.

## Install

1. Ensure `com.unity.ai.assistant` (Unity MCP) is installed and enabled:
   **Edit → Project Settings → AI → Unity MCP**.
2. Add this package via Package Manager → "Install package from disk" and
   point it at this folder's `package.json`, or add it to
   `Packages/manifest.json`:
   ```json
   "com.qfoldit.toolbelt": "file:../qfoldit-unity-toolbelt"
   ```
3. Reopen the Editor. The tools appear automatically under **AI → Unity MCP
   → Tools** and are exposed to any connected MCP client (Claude Code,
   Cursor, etc.) — no separate server to start.
4. Optional: **qFoldIT → Toolbelt → Export Tool Manifest** writes
   `Saved/QFoldIT_Toolbelt/tool_manifest.json` for agents that prefer to
   load a static manifest instead of calling MCP's live `list tools`.

## Tool categories (109 tools total)

| Category | Tools | What it covers |
|----------|:-----:|-----------------|
| Animation | 5 | Create AnimatorControllers, states, transitions, parameters, and attach them to objects. |
| Assets | 3 | List, instantiate, and find project assets by type and name. |
| Audio | 5 | AudioSource setup, one-shot playback, mixer groups, listener management, reverb zones. |
| Camera | 5 | Create cameras, dependency-free follow behaviour, clipping planes, background, screenshots. |
| CodeGen | 1 | Generates a MonoBehaviour with real, bindable public fields for named scene objects. |
| Components | 5 | Reflection-based generic add/remove/get/set/list for any component type. |
| BuildConsole | 3 | Execute menu items, trigger player builds, read console log entry count. |
| Interaction | 2 | Real interaction realization: attaches a working, pre-compiled QFoldITInteractable component (click-wired UnityEvent) for any of the 10 gameplay mechanics or legacy triggers. |
| Lighting | 6 | Create lights, set skybox/ambient/fog, bake lightmaps, apply full lighting presets. |
| Materials | 4 | 12 material presets, bulk swap by name match, team-color split, preset listing. |
| Measurement | 3 | Distance between objects, per-object bounds, and full-scene bounds. |
| Navigation | 4 | Bake NavMesh, add agents/obstacles, set runtime pathing destinations. |
| Particles | 4 | 7 particle system presets (fire, smoke, explosion, sparkle, rain, snow, magic) plus fine control. |
| Physics | 6 | Rigidbody/collider setup, physics materials, joints, raycasts, global gravity. |
| PostProcessing | 5 | URP Volume/VolumeProfile setup: bloom, vignette, color adjustments, depth of field. |
| Assets | 4 | Create prefabs from scene objects, apply/revert overrides, unpack instances. |
| Procedural | 2 | 8 geometric placement patterns (grid, circle, arc, spiral, line, wave, helix, radial) plus a symmetrical arena generator. |
| Project | 1 | Standard folder scaffold plus a boilerplate GameManager singleton MonoBehaviour. |
| SceneManagement | 5 | Create, load, unload, activate, and save scenes in a multi-scene setup. |
| Scene | 8 | Spawn, transform, clone, delete, parent, list, and find GameObjects in the active scene. |
| ScientificVisualization | 2 | Real scientific-state visualization: mechanic-differentiated visible anchors with optional world-space labels, plus QFoldITScientificBinding components for live scientific-state URIs. |
| Stamps | 3 | Save a selection as a reusable stamp; place it anywhere with rotation; list saved stamps. |
| TagsLayers | 4 | Create and assign tags and layers, including recursive layer assignment. |
| Terrain | 5 | Create terrain, sculpt hills/craters, flatten, paint textures, scatter trees. |
| UAGBridge | 2 | Validates and realizes qFoldIT Universal Assembly Graphs by calling this toolbelt's own tools — the Universal World Interface adapter connecting Unity to the rest of the qFoldIT stack. |
| UI | 7 | Build uGUI Canvas hierarchies: buttons, text, panels, sliders, images, anchor presets. |
| Utility | 4 | Batch rename, Game view screenshots, Editor undo/redo. |
| WorldState | 1 | Exports the full scene graph (names, components, transforms, parents) to JSON for AI context. |

## Roadmap to parity

This release brings the toolbelt to **109 real tools** across 28
categories — still short of UEFN Toolbelt's 355, but well past the
25-tool foundation. More importantly, this revision adapts the whole UAG
Bridge to **qfoldit-engine-adapter-spec-v0.1**, the formal spec package
(not the earlier informal Phase-1 draft): `UagModel.cs` now matches the
normative `schemas/uag.schema.json` exactly (`schema`/`scene`/
`node.parent`/`bindings[]`), `uag_validate` emits `{code, message}` errors
matching the spec's own `conformance/test_vectors.json` byte-for-byte, and
`qfoldit.adapter.json` (this repo's root) is strictly valid against
`schemas/adapter-manifest.schema.json`.

**Real, verified milestone**: running the spec's own unmodified
`reference/compiler.py` (from `qfoldit-scientific-gameplay-framework-v0.1`)
against this repo's actual `qfoldit.adapter.json` now compiles all 5
currently-unlocked gameplay patterns with `status=success` and zero gaps —
up from 0/5 before this revision (see `cross-engine-compile-report.md` in
that package). This was earned by building real capability, not by
editing the manifest's status field by hand:

- **`interaction`** (blocked 4/5 patterns): `Runtime/QFoldITInteractable.cs`
  — a real, pre-compiled MonoBehaviour in a dedicated Runtime assembly
  (works in Play Mode and builds, not just the Editor) with a working
  `OnMouseDown → UnityEvent` wiring, for all 10 gameplay mechanics plus
  legacy triggers. `interaction_create` attaches it to any node.
- **`scientific.visualization`** (blocked 5/5 patterns):
  `ScientificVisualizationTools.cs` realizes every UAG
  `scientific_subject/<mechanic>` node as a real, visible,
  mechanic-differentiated object (shape + material preset keyed by
  mechanic), with `Runtime/QFoldITScientificBinding.cs` giving `bindings[]`
  genuine, queryable substance instead of accepting-and-discarding a
  `scientific-state://` URI.
- **`geometry.procedural`** (blocked 2/5 patterns): already real, working
  capability from an earlier revision (`procedural_place`'s 8 patterns +
  `arena_generate`) — the manifest's earlier "partial" rating undercounted
  it; no new code was needed, just an honest status correction.

See [docs/UAG_BRIDGE.md](docs/UAG_BRIDGE.md) for the full contract,
mapping table, and — importantly — what these capabilities honestly do
*not* cover (native per-mechanic gameplay logic, live Niagara-style
parameter-mapped feedback). `tests/conformance/` runs the spec's real
`test_vectors.json` against the actual, unmodified `UagValidator.cs`.

Structured to keep growing the same way: new files under `Editor/Tools/`,
each adding `[McpTool]` methods, tracked in `registry.json`.


## License

AGPL-3.0, with an additional visible-attribution requirement — see [LICENSE](LICENSE). Any tool built on this codebase must credit qFoldIT and link back to this repository (see LICENSE for the exact wording); network/hosted use requires publishing your modified source.
