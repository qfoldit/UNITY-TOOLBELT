// qFoldIT Toolbelt for Unity — AssetTools.cs
// Category: Assets

using System.Linq;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class AssetTools
    {
        // ── asset_list ──────────────────────────────────────────────────
        public class AssetListParams
        {
            [McpDescription("Unity asset type filter, e.g. 'Prefab', 'Material', 'Texture2D'", Default = "Prefab")]
            public string TypeFilter { get; set; } = "Prefab";

            [McpDescription("Optional folder to restrict the search to, e.g. Assets/Props", Default = "Assets")]
            public string Folder { get; set; } = "Assets";

            [McpDescription("Max results to return", Default = 100)]
            public int MaxResults { get; set; } = 100;
        }

        [McpTool("asset_list", "Lists project assets of a given type (Prefab, Material, Texture2D, AudioClip, Scene, etc.) under a folder.")]
        public static object AssetList(AssetListParams p)
        {
            var guids = AssetDatabase.FindAssets($"t:{p.TypeFilter}", new[] { p.Folder });
            var paths = guids.Select(AssetDatabase.GUIDToAssetPath).Distinct().Take(Mathf.Max(1, p.MaxResults)).ToArray();
            return new { success = true, type = p.TypeFilter, folder = p.Folder, count = paths.Length, assets = paths };
        }

        // ── asset_instantiate_prefab ───────────────────────────────────
        public class InstantiatePrefabParams
        {
            [McpDescription("Prefab asset path, e.g. Assets/Props/Crate.prefab", Required = true)]
            public string PrefabPath { get; set; }

            [McpDescription("World position X")]
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 0f;
            public float Z { get; set; } = 0f;

            [McpDescription("Optional override name for the instance", Default = "")]
            public string Name { get; set; } = "";
        }

        [McpTool("asset_instantiate_prefab", "Instantiates a prefab from the project at a world position, preserving the prefab connection.")]
        public static object InstantiatePrefab(InstantiatePrefabParams p)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p.PrefabPath);
            if (prefab == null) return new { success = false, error = $"No prefab found at '{p.PrefabPath}'." };

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3(p.X, p.Y, p.Z);
            if (!string.IsNullOrEmpty(p.Name)) instance.name = p.Name;

            Undo.RegisterCreatedObjectUndo(instance, "qFoldIT: Instantiate Prefab");
            return new { success = true, name = instance.name, prefab_path = p.PrefabPath };
        }

        // ── asset_find_by_type ─────────────────────────────────────────
        public class FindByTypeParams
        {
            [McpDescription("Unity asset type, e.g. 'AudioClip'", Required = true)]
            public string TypeFilter { get; set; }

            [McpDescription("Substring to match against the asset file name", Required = true)]
            public string NameContains { get; set; }
        }

        [McpTool("asset_find_by_type", "Finds project assets of a given type whose file name contains a substring.")]
        public static object FindByType(FindByTypeParams p)
        {
            var guids = AssetDatabase.FindAssets($"t:{p.TypeFilter}");
            var matches = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => System.IO.Path.GetFileNameWithoutExtension(path)
                    .ToLowerInvariant().Contains(p.NameContains.ToLowerInvariant()))
                .Distinct()
                .ToArray();
            return new { success = true, matches };
        }
    }
}
