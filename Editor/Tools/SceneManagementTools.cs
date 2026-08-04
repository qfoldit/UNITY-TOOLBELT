// qFoldIT Toolbelt for Unity — SceneManagementTools.cs
// Category: SceneManagement

using System.IO;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class SceneManagementTools
    {
        // ── scene_create_additive ──────────────────────────────────────
        public class CreateAdditiveParams
        {
            [McpDescription("Output .unity scene path, e.g. Assets/Scenes/Level1_Streaming.unity", Required = true)]
            public string OutputPath { get; set; }
        }

        [McpTool("scene_create_additive", "Creates a new empty scene and saves it, without loading it into the current multi-scene setup.")]
        public static object CreateAdditive(CreateAdditiveParams p)
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(UnityEngine.Application.dataPath, "..", p.OutputPath)) ?? ".");
            bool ok = EditorSceneManager.SaveScene(newScene, p.OutputPath);
            EditorSceneManager.CloseScene(newScene, true);

            return new { success = ok, path = p.OutputPath };
        }

        // ── scene_load_additive ────────────────────────────────────────
        public class LoadAdditiveParams
        {
            [McpDescription("Path to an existing .unity scene", Required = true)]
            public string ScenePath { get; set; }
        }

        [McpTool("scene_load_additive", "Loads a scene additively alongside the currently open scene(s), in the Editor.")]
        public static object LoadAdditive(LoadAdditiveParams p)
        {
            var scene = EditorSceneManager.OpenScene(p.ScenePath, OpenSceneMode.Additive);
            return new { success = scene.IsValid(), scene = scene.name };
        }

        // ── scene_unload ────────────────────────────────────────────────
        public class UnloadParams
        {
            [McpDescription("Name of a currently open scene to unload", Required = true)]
            public string SceneName { get; set; }
        }

        [McpTool("scene_unload", "Unloads (closes) an additively loaded scene by name, without saving.")]
        public static object Unload(UnloadParams p)
        {
            var scene = SceneManager.GetSceneByName(p.SceneName);
            if (!scene.IsValid()) return new { success = false, error = $"Scene '{p.SceneName}' is not currently open." };

            bool ok = EditorSceneManager.CloseScene(scene, true);
            return new { success = ok, scene = p.SceneName };
        }

        // ── scene_set_active ────────────────────────────────────────────
        public class SetActiveParams
        {
            [McpDescription("Name of a currently open scene to mark active", Required = true)]
            public string SceneName { get; set; }
        }

        [McpTool("scene_set_active", "Sets which of the currently open scenes is the 'active' scene (new GameObjects are created there).")]
        public static object SetActive(SetActiveParams p)
        {
            var scene = SceneManager.GetSceneByName(p.SceneName);
            if (!scene.IsValid()) return new { success = false, error = $"Scene '{p.SceneName}' is not currently open." };

            bool ok = SceneManager.SetActiveScene(scene);
            return new { success = ok, active_scene = p.SceneName };
        }

        // ── scene_save ──────────────────────────────────────────────────
        public class SaveParams
        {
            [McpDescription("Name of the currently open scene to save; empty = save the active scene", Default = "")]
            public string SceneName { get; set; } = "";
        }

        [McpTool("scene_save", "Saves a currently open scene (or the active scene, if no name given) to disk.")]
        public static object Save(SaveParams p)
        {
            var scene = string.IsNullOrEmpty(p.SceneName) ? SceneManager.GetActiveScene() : SceneManager.GetSceneByName(p.SceneName);
            if (!scene.IsValid()) return new { success = false, error = $"Scene not found or not open." };

            bool ok = EditorSceneManager.SaveScene(scene);
            return new { success = ok, scene = scene.name, path = scene.path };
        }
    }
}
