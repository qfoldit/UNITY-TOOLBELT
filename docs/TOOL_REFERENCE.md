# Tool Reference — qFoldIT Toolbelt for Unity

Auto-summarized from `registry.json` (2026-08-03). 102 tools across 25 categories.

## Animation — Animation Tools

Create AnimatorControllers, states, transitions, parameters, and attach them to objects.

Source: `Editor/Tools/AnimationTools.cs`

- `anim_create_controller`
- `anim_add_state`
- `anim_add_transition`
- `anim_set_parameter`
- `anim_attach_controller`

## Assets — Asset Tools

List, instantiate, and find project assets by type and name.

Source: `Editor/Tools/AssetTools.cs`

- `asset_list`
- `asset_instantiate_prefab`
- `asset_find_by_type`

## Audio — Audio Tools

AudioSource setup, one-shot playback, mixer groups, listener management, reverb zones.

Source: `Editor/Tools/AudioTools.cs`

- `audio_add_source`
- `audio_play_one_shot`
- `audio_create_mixer_group`
- `audio_set_listener`
- `audio_set_reverb_zone`

## Camera — Camera Tools

Create cameras, dependency-free follow behaviour, clipping planes, background, screenshots.

Source: `Editor/Tools/CameraTools.cs`

- `camera_create_rig`
- `camera_set_follow_target`
- `camera_set_clipping`
- `camera_set_background`
- `camera_screenshot`

## CodeGen — CodeGen Tools

Generates a MonoBehaviour with real, bindable public fields for named scene objects.

Source: `Editor/Tools/CodeGenTools.cs`

- `codegen_monobehaviour`

## Components — Component Tools

Reflection-based generic add/remove/get/set/list for any component type.

Source: `Editor/Tools/ComponentTools.cs`

- `component_add`
- `component_remove`
- `component_set_field`
- `component_list`
- `component_get_field`

## BuildConsole — Console & Build Tools

Execute menu items, trigger player builds, read console log entry count.

Source: `Editor/Tools/ConsoleBuildTools.cs`

- `console_execute_menu_item`
- `build_player`
- `console_get_log`

## Lighting — Lighting Tools

Create lights, set skybox/ambient/fog, bake lightmaps, apply full lighting presets.

Source: `Editor/Tools/LightingTools.cs`

- `light_create`
- `light_set_skybox`
- `light_set_ambient`
- `light_set_fog`
- `light_bake_lightmaps`
- `light_apply_preset`

## Materials — Material Tools

12 material presets, bulk swap by name match, team-color split, preset listing.

Source: `Editor/Tools/MaterialTools.cs`

- `material_apply_preset`
- `material_bulk_swap`
- `material_team_color_split`
- `material_list_presets`

## Measurement — Measurement Tools

Distance between objects, per-object bounds, and full-scene bounds.

Source: `Editor/Tools/MeasurementTools.cs`

- `measure_distance`
- `measure_bounds`
- `measure_scene_bounds`

## Navigation — Navigation Tools

Bake NavMesh, add agents/obstacles, set runtime pathing destinations.

Source: `Editor/Tools/NavigationTools.cs`

- `nav_bake_navmesh`
- `nav_add_agent`
- `nav_add_obstacle`
- `nav_set_destination`

## Particles — Particle Tools

7 particle system presets (fire, smoke, explosion, sparkle, rain, snow, magic) plus fine control.

Source: `Editor/Tools/ParticleTools.cs`

- `particles_apply_preset`
- `particles_set_emission_rate`
- `particles_set_color_over_lifetime`
- `particles_burst`

## Physics — Physics Tools

Rigidbody/collider setup, physics materials, joints, raycasts, global gravity.

Source: `Editor/Tools/PhysicsTools.cs`

- `physics_add_rigidbody`
- `physics_add_collider`
- `physics_set_physics_material`
- `physics_add_joint`
- `physics_raycast_query`
- `physics_set_gravity`

## PostProcessing — Post-Processing Tools

URP Volume/VolumeProfile setup: bloom, vignette, color adjustments, depth of field.

Source: `Editor/Tools/PostProcessingTools.cs`

- `postfx_create_global_volume`
- `postfx_set_bloom`
- `postfx_set_vignette`
- `postfx_set_color_adjustments`
- `postfx_set_depth_of_field`

## Assets — Prefab Workflow Tools

Create prefabs from scene objects, apply/revert overrides, unpack instances.

Source: `Editor/Tools/PrefabWorkflowTools.cs`

- `prefab_create_from_object`
- `prefab_apply_overrides`
- `prefab_revert_overrides`
- `prefab_unpack`

## Procedural — Procedural Placement & Arena

8 geometric placement patterns (grid, circle, arc, spiral, line, wave, helix, radial) plus a symmetrical arena generator.

Source: `Editor/Tools/ProceduralPlacementTools.cs`

- `procedural_place`
- `arena_generate`

## Project — Project Setup

Standard folder scaffold plus a boilerplate GameManager singleton MonoBehaviour.

Source: `Editor/Tools/ProjectSetupTools.cs`

- `project_setup`

## SceneManagement — Multi-Scene Management Tools

Create, load, unload, activate, and save scenes in a multi-scene setup.

Source: `Editor/Tools/SceneManagementTools.cs`

- `scene_create_additive`
- `scene_load_additive`
- `scene_unload`
- `scene_set_active`
- `scene_save`

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

## Stamps — Stamp Tools

Save a selection as a reusable stamp; place it anywhere with rotation; list saved stamps.

Source: `Editor/Tools/StampTools.cs`

- `stamp_save`
- `stamp_place`
- `stamp_list`

## TagsLayers — Tags & Layers Tools

Create and assign tags and layers, including recursive layer assignment.

Source: `Editor/Tools/TagsLayersTools.cs`

- `tag_create`
- `tag_assign`
- `layer_create`
- `layer_assign`

## Terrain — Terrain Tools

Create terrain, sculpt hills/craters, flatten, paint textures, scatter trees.

Source: `Editor/Tools/TerrainTools.cs`

- `terrain_create`
- `terrain_sculpt_hill`
- `terrain_flatten`
- `terrain_paint_texture`
- `terrain_add_trees`

## UI — UI Tools

Build uGUI Canvas hierarchies: buttons, text, panels, sliders, images, anchor presets.

Source: `Editor/Tools/UITools.cs`

- `ui_create_canvas`
- `ui_create_button`
- `ui_create_text`
- `ui_create_panel`
- `ui_create_slider`
- `ui_create_image`
- `ui_set_anchor_preset`

## Utility — Editor Utility Tools

Batch rename, Game view screenshots, Editor undo/redo.

Source: `Editor/Tools/UtilityTools.cs`

- `batch_rename`
- `editor_screenshot`
- `editor_undo`
- `editor_redo`

## WorldState — World State Export

Exports the full scene graph (names, components, transforms, parents) to JSON for AI context.

Source: `Editor/Tools/WorldStateExportTools.cs`

- `world_state_export`
