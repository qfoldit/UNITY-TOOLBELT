# Architecture — qFoldIT Toolbelt for Unity

## Why no external bridge process

UEFN has no native MCP support, so UEFN Toolbelt has to run two processes:
an external `mcp_server.py` that speaks MCP over stdio to Claude Code, and
an in-editor Python HTTP listener that it relays to. That's a workaround
for a missing feature, not a design goal.

Unity **does** have native MCP support (`com.unity.ai.assistant`, "Unity
MCP"): the Editor itself launches an MCP-compatible relay binary and
exposes a `McpToolRegistry` that discovers `[McpTool]`-attributed methods
via `TypeCache` at startup. So the qFoldIT layer for Unity is a single
in-process C# assembly — no bridge, no port, no polling loop.

```
AI Client (Claude Code, Cursor, ...)
    │  MCP protocol (stdio)
    ▼
Relay binary (~/.unity/relay/, started with --mcp)
    │  IPC (named pipe / Unix socket)
    ▼
Unity Editor — MCP Bridge
    │
    ▼
McpToolRegistry
    ├── Unity's own built-in tools (scene mgmt, asset ops, script edit, console)
    └── qFoldIT Toolbelt tools  ← this package, Editor/Tools/*.cs
```

## File layout

```
qfoldit-unity-toolbelt/
├── package.json                  Unity Package Manager manifest
├── Editor/
│   ├── QFoldIT.Toolbelt.Editor.asmdef   references Unity.AI.MCP.Editor
│   ├── Core/
│   │   └── ToolbeltRegistry.cs   category metadata + manifest export menu item
│   └── Tools/
│       ├── SceneTools.cs         spawn/transform/clone/delete/parent/list/find
│       ├── MaterialTools.cs      presets, bulk swap, team-color split
│       ├── ProceduralPlacementTools.cs   8-pattern placement + arena generator
│       ├── StampTools.cs         save/place/list reusable object groups
│       ├── ProjectSetupTools.cs  folder scaffold + GameManager boilerplate
│       ├── WorldStateExportTools.cs      scene graph → JSON
│       ├── CodeGenTools.cs       MonoBehaviour wired to real objects
│       ├── AssetTools.cs         list/instantiate/find project assets
│       └── ConsoleBuildTools.cs  menu items, player build, console log
├── docs/TOOL_REFERENCE.md
├── registry.json                 plugin manifest (mirrors UEFN Toolbelt's format)
└── Tests/Editor/                 edit-mode smoke tests
```

## Tool authoring convention

Every tool follows the same shape, matching Unity's documented
"static method with typed parameters" registration path:

```csharp
public class MyToolParams
{
    [McpDescription("What this does", Required = true)]
    public string SomeField { get; set; }
}

[McpTool("my_tool", "One-line description shown to the AI client.")]
public static object MyTool(MyToolParams p)
{
    // ... UnityEditor / UnityEngine calls ...
    return new { success = true, /* structured result */ };
}
```

Rules kept consistent across all tool files:

- **Always return a structured object** with at least a `success` boolean —
  never throw for expected failure cases (object not found, invalid enum,
  etc.); return `{ success = false, error = "..." }` instead so an agent
  can branch on it without a try/catch.
- **Always wrap scene mutations in `Undo.*`** so every AI-driven change is a
  normal, reversible Editor action (`Undo.RegisterCreatedObjectUndo`,
  `Undo.RecordObject`, `Undo.DestroyObjectImmediate`, `Undo.SetTransformParent`).
- **Find objects by name, not GUID**, for parity with how an agent reads
  `scene_list_objects` / `world_state_export` output back into subsequent
  calls.
- **One tool per capability, not per variant** — `procedural_place` takes a
  `pattern` enum instead of eight separate tools, matching UEFN Toolbelt's
  `Prop Patterns` design (`tool_count: 1` covering 8 patterns in its own
  `registry.json`).

## Extending the toolbelt

To add a new tool: create or extend a file under `Editor/Tools/`, add a
`[McpTool]` method following the convention above, and add an entry to
`registry.json` so the category table in `ToolbeltRegistry.cs` and the
generated manifest stay accurate. Unity discovers new tools automatically
via `TypeCache` — no manual registration call is needed.

Planned next categories (not yet implemented): terrain tools, animation
state-machine scaffolding, UI Toolkit layout generation, Cinemachine camera
rigs, addressables workflow, ProBuilder procedural geometry.
