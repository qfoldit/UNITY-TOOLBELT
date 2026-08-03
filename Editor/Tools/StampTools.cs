// qFoldIT Toolbelt for Unity — StampTools.cs
// Category: Stamps
// Save the current Selection as a reusable "stamp" (JSON describing prefab
// paths + local transforms relative to a pivot), then re-place it anywhere,
// at any rotation, later. Mirrors UEFN Toolbelt's stamp_save / stamp_place.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class StampTools
    {
        private static string StampDir =>
            Path.Combine(Application.dataPath, "..", "Saved", "QFoldIT_Toolbelt", "stamps");

        [Serializable]
        private class StampEntry
        {
            public string prefabPath;   // may be empty if the object has no prefab source
            public string primitiveFallback; // used when prefabPath is empty
            public float px, py, pz;    // position relative to pivot
            public float rx, ry, rz;    // euler rotation
            public float sx, sy, sz;    // scale
        }

        [Serializable]
        private class StampFile
        {
            public string name;
            public List<StampEntry> entries = new List<StampEntry>();
        }

        // ── stamp_save ──────────────────────────────────────────────────
        public class StampSaveParams
        {
            [McpDescription("Name to save this stamp under", Required = true)]
            public string Name { get; set; }
        }

        [McpTool("stamp_save", "Saves the currently selected GameObjects (in the Editor Selection) as a reusable stamp, positions stored relative to their combined pivot.")]
        public static object StampSave(StampSaveParams p)
        {
            var selection = Selection.gameObjects;
            if (selection.Length == 0)
                return new { success = false, error = "Nothing selected in the Editor." };

            Vector3 pivot = Vector3.zero;
            foreach (var go in selection) pivot += go.transform.position;
            pivot /= selection.Length;

            var stamp = new StampFile { name = p.Name };
            foreach (var go in selection)
            {
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
                var rel = go.transform.position - pivot;
                stamp.entries.Add(new StampEntry
                {
                    prefabPath = prefabPath ?? "",
                    primitiveFallback = string.IsNullOrEmpty(prefabPath) ? "Cube" : "",
                    px = rel.x, py = rel.y, pz = rel.z,
                    rx = go.transform.eulerAngles.x, ry = go.transform.eulerAngles.y, rz = go.transform.eulerAngles.z,
                    sx = go.transform.localScale.x, sy = go.transform.localScale.y, sz = go.transform.localScale.z
                });
            }

            Directory.CreateDirectory(StampDir);
            var path = Path.Combine(StampDir, $"{p.Name}.json");
            File.WriteAllText(path, JsonUtility.ToJson(stamp, true));

            return new { success = true, name = p.Name, objects_saved = stamp.entries.Count, path };
        }

        // ── stamp_place ─────────────────────────────────────────────────
        public class StampPlaceParams
        {
            [McpDescription("Name of a previously saved stamp", Required = true)]
            public string Name { get; set; }

            [McpDescription("World position to place the stamp's pivot at")]
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 0f;
            public float Z { get; set; } = 0f;

            [McpDescription("Yaw (Y-axis) rotation offset applied to the whole stamp, degrees", Default = 0f)]
            public float YawOffset { get; set; } = 0f;
        }

        [McpTool("stamp_place", "Places a previously saved stamp at a world position, with an optional yaw rotation applied to the whole group.")]
        public static object StampPlace(StampPlaceParams p)
        {
            var path = Path.Combine(StampDir, $"{p.Name}.json");
            if (!File.Exists(path)) return new { success = false, error = $"Stamp '{p.Name}' not found." };

            var stamp = JsonUtility.FromJson<StampFile>(File.ReadAllText(path));
            var origin = new Vector3(p.X, p.Y, p.Z);
            var yawRot = Quaternion.Euler(0, p.YawOffset, 0);
            var created = new List<string>();

            foreach (var e in stamp.entries)
            {
                GameObject go;
                if (!string.IsNullOrEmpty(e.prefabPath))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(e.prefabPath);
                    go = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
                }
                else
                {
                    go = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);
                }

                var localOffset = yawRot * new Vector3(e.px, e.py, e.pz);
                go.transform.position = origin + localOffset;
                go.transform.eulerAngles = new Vector3(e.rx, e.ry + p.YawOffset, e.rz);
                go.transform.localScale = new Vector3(e.sx, e.sy, e.sz);
                go.name = $"{stamp.name}_{created.Count:D2}";

                Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Stamp Place");
                created.Add(go.name);
            }

            return new { success = true, name = p.Name, placed_count = created.Count, names = created };
        }

        // ── stamp_list ──────────────────────────────────────────────────
        public class StampListParams { }

        [McpTool("stamp_list", "Lists every stamp saved so far.")]
        public static object StampList(StampListParams p)
        {
            if (!Directory.Exists(StampDir)) return new { success = true, stamps = Array.Empty<string>() };
            var stamps = Directory.GetFiles(StampDir, "*.json").Select(f => Path.GetFileNameWithoutExtension(f)).ToArray();
            return new { success = true, stamps };
        }
    }
}
