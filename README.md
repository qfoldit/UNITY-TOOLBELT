# qFoldIT Toolbelt — Unity

**25 composite editor-automation tools for Unity, exposed to AI agents through Unity's own official MCP bridge.**

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

## Tool categories (25 tools total)

| Category     | Tools | What it covers |
|--------------|:-----:|-----------------|
| Scene        | 7 | spawn, transform, clone, delete, parent, list, find |
| Materials    | 5 | 12 presets, bulk swap, team-color split, list presets |
| Procedural   | 2 | 8-pattern placement, symmetrical arena generator |
| Stamps       | 3 | save/place/list reusable object groups |
| Project      | 1 | folder scaffold + boilerplate GameManager |
| WorldState   | 1 | full scene graph → JSON for AI context |
| CodeGen      | 1 | MonoBehaviour wired to real scene objects |
| Assets       | 3 | list / instantiate / find project assets |
| BuildConsole | 3 | menu items, player builds, console log count |

## Roadmap to parity

This is a **foundation release** — 25 real tools, not 355. The categories
above are structured to grow the same way UEFN Toolbelt's did: community
plugin files under `Editor/Tools/`, each adding a handful of `[McpTool]`
methods, tracked in `registry.json`. See `ROADMAP.md`-equivalent notes in
[ARCHITECTURE.md](ARCHITECTURE.md#extending-the-toolbelt).

## License

AGPL-3.0, with an additional visible-attribution requirement — see [LICENSE](LICENSE). Any tool built on this codebase must credit qFoldIT and link back to this repository (see LICENSE for the exact wording); network/hosted use requires publishing your modified source.
