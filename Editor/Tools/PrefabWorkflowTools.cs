// qFoldIT Toolbelt for Unity — PrefabWorkflowTools.cs
// Category: Assets (prefab-specific workflow, kept separate from AssetTools.cs)

using System.IO;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class PrefabWorkflowTools
    {
        // ── prefab_create_from_selection ───────────────────────────────
        public class CreateFromNameParams
        {
            [McpDescription("Name of the GameObject in the scene to turn into a prefab", Required = true)]
            public string Name { get; set; }
            [McpDescription("Output .prefab path, e.g. Assets/Prefabs/Crate.prefab", Required = true)]
            public string OutputPath { get; set; }
        }

        [McpTool("prefab_create_from_object", "Saves a scene GameObject (found by name) as a new prefab asset, connecting the scene instance to it.")]
        public static object CreateFromObject(CreateFromNameParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Application.dataPath, "..", p.OutputPath)) ?? ".");
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(go, p.OutputPath, InteractionMode.UserAction);

            return new { success = prefab != null, name = p.Name, prefab_path = p.OutputPath };
        }

        // ── prefab_apply_overrides ─────────────────────────────────────
        public class ApplyOverridesParams
        {
            [McpDescription("Name of a prefab instance in the scene", Required = true)]
            public string Name { get; set; }
        }

        [McpTool("prefab_apply_overrides", "Applies a prefab instance's overrides back to its source prefab asset.")]
        public static object ApplyOverrides(ApplyOverridesParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };
            if (!PrefabUtility.IsPartOfPrefabInstance(go)) return new { success = false, error = $"'{p.Name}' is not a prefab instance." };

            PrefabUtility.ApplyPrefabInstance(go, InteractionMode.UserAction);
            return new { success = true, name = p.Name };
        }

        // ── prefab_revert_overrides ─────────────────────────────────────
        public class RevertOverridesParams
        {
            [McpDescription("Name of a prefab instance in the scene", Required = true)]
            public string Name { get; set; }
        }

        [McpTool("prefab_revert_overrides", "Reverts a prefab instance's overrides back to match its source prefab asset.")]
        public static object RevertOverrides(RevertOverridesParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };
            if (!PrefabUtility.IsPartOfPrefabInstance(go)) return new { success = false, error = $"'{p.Name}' is not a prefab instance." };

            PrefabUtility.RevertPrefabInstance(go, InteractionMode.UserAction);
            return new { success = true, name = p.Name };
        }

        // ── prefab_unpack ────────────────────────────────────────────────
        public class UnpackParams
        {
            [McpDescription("Name of a prefab instance in the scene", Required = true)]
            public string Name { get; set; }
            [McpDescription("Unpack the entire nested prefab hierarchy, not just the outermost root", Default = false)]
            public bool Completely { get; set; } = false;
        }

        [McpTool("prefab_unpack", "Unpacks a prefab instance in the scene, breaking its connection to the source prefab asset.")]
        public static object Unpack(UnpackParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };
            if (!PrefabUtility.IsPartOfPrefabInstance(go)) return new { success = false, error = $"'{p.Name}' is not a prefab instance." };

            var mode = p.Completely ? PrefabUnpackMode.Completely : PrefabUnpackMode.OutermostRoot;
            PrefabUtility.UnpackPrefabInstance(go, mode, InteractionMode.UserAction);
            return new { success = true, name = p.Name, mode = mode.ToString() };
        }
    }
}
