# Tool Reference — qFoldIT Toolbelt for Unity

Auto-summarized from `registry.json` (2026-08-02). 25 tools across 9 categories.

## Scene — Scene Tools

Spawn, transform, clone, delete, parent, list, and find GameObjects in the active scene.

Source: `Editor/Tools/SceneTools.cs`

- `spawn_primitive`
- `transform_object`
- `clone_object`
- `delete_object`
- `parent_object`
- `scene_list_objects`
- `scene_find_by_name`

## Materials — Material Tools

12 material presets, bulk swap by name match, team-color split, preset listing.

Source: `Editor/Tools/MaterialTools.cs`

- `material_apply_preset`
- `material_bulk_swap`
- `material_team_color_split`
- `material_list_presets`

## Procedural — Procedural Placement & Arena

8 geometric placement patterns (grid, circle, arc, spiral, line, wave, helix, radial) plus a symmetrical arena generator.

Source: `Editor/Tools/ProceduralPlacementTools.cs`

- `procedural_place`
- `arena_generate`

## Stamps — Stamp Tools

Save a selection as a reusable stamp; place it anywhere with rotation; list saved stamps.

Source: `Editor/Tools/StampTools.cs`

- `stamp_save`
- `stamp_place`
- `stamp_list`

## Project — Project Setup

Standard folder scaffold plus a boilerplate GameManager singleton MonoBehaviour.

Source: `Editor/Tools/ProjectSetupTools.cs`

- `project_setup`

## WorldState — World State Export

Exports the full scene graph (names, components, transforms, parents) to JSON for AI context.

Source: `Editor/Tools/WorldStateExportTools.cs`

- `world_state_export`

## CodeGen — CodeGen Tools

Generates a MonoBehaviour with real, bindable public fields for named scene objects.

Source: `Editor/Tools/CodeGenTools.cs`

- `codegen_monobehaviour`

## Assets — Asset Tools

List, instantiate, and find project assets by type and name.

Source: `Editor/Tools/AssetTools.cs`

- `asset_list`
- `asset_instantiate_prefab`
- `asset_find_by_type`

## BuildConsole — Console & Build Tools

Execute menu items, trigger player builds, read console log entry count.

Source: `Editor/Tools/ConsoleBuildTools.cs`

- `console_execute_menu_item`
- `build_player`
- `console_get_log`
