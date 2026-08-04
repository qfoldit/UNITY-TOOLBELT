# qFoldIT Toolbelt — Unity

**102 composite editor-automation tools for Unity, exposed to AI agents through Unity's own official MCP bridge.**

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

## Tool categories (102 tools total)

| Category | Tools | What it covers |
|----------|:-----:|-----------------|
| Animation | 5 | Create AnimatorControllers, states, transitions, parameters, and attach them to objects. |
| Assets | 3 | List, instantiate, and find project assets by type and name. |
| Audio | 5 | AudioSource setup, one-shot playback, mixer groups, listener management, reverb zones. |
| Camera | 5 | Create cameras, dependency-free follow behaviour, clipping planes, background, screenshots. |
| CodeGen | 1 | Generates a MonoBehaviour with real, bindable public fields for named scene objects. |
| Components | 5 | Reflection-based generic add/remove/get/set/list for any component type. |
| BuildConsole | 3 | Execute menu items, trigger player builds, read console log entry count. |
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
| Scene | 7 | Spawn, transform, clone, delete, parent, list, and find GameObjects in the active scene. |
| Stamps | 3 | Save a selection as a reusable stamp; place it anywhere with rotation; list saved stamps. |
| TagsLayers | 4 | Create and assign tags and layers, including recursive layer assignment. |
| Terrain | 5 | Create terrain, sculpt hills/craters, flatten, paint textures, scatter trees. |
| UI | 7 | Build uGUI Canvas hierarchies: buttons, text, panels, sliders, images, anchor presets. |
| Utility | 4 | Batch rename, Game view screenshots, Editor undo/redo. |
| WorldState | 1 | Exports the full scene graph (names, components, transforms, parents) to JSON for AI context. |

## Roadmap to parity

This release brings the toolbelt to **102 real tools** across 24 categories
— still short of UEFN Toolbelt's 355, but a large step up from the initial
25-tool foundation release. Structured to keep growing the same way: new
files under `Editor/Tools/`, each adding `[McpTool]` methods, tracked in
`registry.json`. See [ARCHITECTURE.md](ARCHITECTURE.md#extending-the-toolbelt).

Categories still on the list: Cinemachine-native camera rigs (as an
optional dependency alongside the built-in follow behaviour), Addressables
workflow, ProBuilder procedural geometry, Timeline/cutscene tools,
Input System action asset generation, localization table scaffolding.

## License

AGPL-3.0, with an additional visible-attribution requirement — see [LICENSE](LICENSE). Any tool built on this codebase must credit qFoldIT and link back to this repository (see LICENSE for the exact wording); network/hosted use requires publishing your modified source.
