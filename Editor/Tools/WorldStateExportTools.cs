// qFoldIT Toolbelt for Unity — WorldStateExportTools.cs
// Category: WorldState
// Dumps the full scene graph to JSON so an AI agent has ground truth about
// what's actually in the level before it generates code that references it.
// Mirrors UEFN Toolbelt's world_state_export.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class WorldStateExportTools
    {
        [Serializable]
        private class ExportedObject
        {
            public string name;
            public string[] componentTypes;
            public float[] position;
            public float[] eulerAngles;
            public float[] scale;
            public bool active;
            public string parent;
        }

        [Serializable]
        private class ExportedScene
        {
            public string sceneName;
            public string exportedAtUtc;
            public int objectCount;
            public List<ExportedObject> objects = new List<ExportedObject>();
        }

        public class WorldStateExportParams
        {
            [McpDescription("Output file path relative to the project root", Default = "docs/world_state.json")]
            public string OutputPath { get; set; } = "docs/world_state.json";
        }

        [McpTool("world_state_export", "Exports every GameObject in the active scene (name, components, transform, parent) to a JSON file an AI agent can read for full level context.")]
        public static object WorldStateExport(WorldStateExportParams p)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var export = new ExportedScene
            {
                sceneName = scene.name,
                exportedAtUtc = DateTime.UtcNow.ToString("o")
            };

            var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in all)
            {
                export.objects.Add(new ExportedObject
                {
                    name = go.name,
                    componentTypes = go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).ToArray(),
                    position = new[] { go.transform.position.x, go.transform.position.y, go.transform.position.z },
                    eulerAngles = new[] { go.transform.eulerAngles.x, go.transform.eulerAngles.y, go.transform.eulerAngles.z },
                    scale = new[] { go.transform.localScale.x, go.transform.localScale.y, go.transform.localScale.z },
                    active = go.activeSelf,
                    parent = go.transform.parent != null ? go.transform.parent.name : ""
                });
            }
            export.objectCount = export.objects.Count;

            var fullPath = Path.Combine(Application.dataPath, "..", p.OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
            File.WriteAllText(fullPath, JsonUtility.ToJson(export, true));

            return new { success = true, scene = scene.name, object_count = export.objectCount, path = p.OutputPath };
        }
    }
}
