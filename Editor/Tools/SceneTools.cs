// qFoldIT Toolbelt for Unity — SceneTools.cs
// Category: Scene
//
// Basic CRUD over GameObjects in the currently open scene, exposed as MCP
// tools via Unity's official attribute-based registration
// (see: Unity MCP > Register custom MCP tools).

using System.Collections.Generic;
using System.Linq;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class SceneTools
    {
        // ── spawn_primitive ────────────────────────────────────────────
        public class SpawnPrimitiveParams
        {
            [McpDescription("Primitive type to create", Required = true, EnumType = typeof(PrimitiveType))]
            public string Type { get; set; }

            [McpDescription("Object name")]
            public string Name { get; set; } = "";

            [McpDescription("World position X")]
            public float X { get; set; } = 0f;

            [McpDescription("World position Y")]
            public float Y { get; set; } = 0f;

            [McpDescription("World position Z")]
            public float Z { get; set; } = 0f;

            [McpDescription("Uniform scale")]
            public float Scale { get; set; } = 1f;
        }

        [McpTool("spawn_primitive", "Creates a primitive GameObject (cube, sphere, cylinder, capsule, plane, quad) at a world position.")]
        public static object SpawnPrimitive(SpawnPrimitiveParams p)
        {
            var type = (UnityEngine.PrimitiveType)System.Enum.Parse(typeof(UnityEngine.PrimitiveType), p.Type, true);
            var go = GameObject.CreatePrimitive(type);
            go.transform.position = new Vector3(p.X, p.Y, p.Z);
            go.transform.localScale = Vector3.one * p.Scale;
            if (!string.IsNullOrEmpty(p.Name)) go.name = p.Name;

            Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Spawn Primitive");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            return new { success = true, name = go.name, instance_id = go.GetInstanceID(), position = new[] { p.X, p.Y, p.Z } };
        }

        // ── transform_object ───────────────────────────────────────────
        public class TransformObjectParams
        {
            [McpDescription("Name of the target GameObject", Required = true)]
            public string Name { get; set; }

            [McpDescription("New world position [x,y,z]; omit fields to leave unchanged")]
            public float? X { get; set; }
            public float? Y { get; set; }
            public float? Z { get; set; }

            [McpDescription("New Euler rotation in degrees")]
            public float? RotX { get; set; }
            public float? RotY { get; set; }
            public float? RotZ { get; set; }

            [McpDescription("Uniform scale override")]
            public float? Scale { get; set; }
        }

        [McpTool("transform_object", "Sets position/rotation/scale on an existing GameObject found by name.")]
        public static object TransformObject(TransformObjectParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            Undo.RecordObject(go.transform, "qFoldIT: Transform Object");
            var pos = go.transform.position;
            var rot = go.transform.eulerAngles;

            if (p.X.HasValue) pos.x = p.X.Value;
            if (p.Y.HasValue) pos.y = p.Y.Value;
            if (p.Z.HasValue) pos.z = p.Z.Value;
            go.transform.position = pos;

            if (p.RotX.HasValue) rot.x = p.RotX.Value;
            if (p.RotY.HasValue) rot.y = p.RotY.Value;
            if (p.RotZ.HasValue) rot.z = p.RotZ.Value;
            go.transform.eulerAngles = rot;

            if (p.Scale.HasValue) go.transform.localScale = Vector3.one * p.Scale.Value;

            return new { success = true, name = go.name, position = new[] { go.transform.position.x, go.transform.position.y, go.transform.position.z } };
        }

        // ── clone_object ────────────────────────────────────────────────
        public class CloneObjectParams
        {
            [McpDescription("Name of the object to duplicate", Required = true)]
            public string Name { get; set; }

            [McpDescription("Number of copies to create", Default = 1)]
            public int Count { get; set; } = 1;

            [McpDescription("Position offset applied to each successive copy, X axis")]
            public float OffsetX { get; set; } = 1f;
            public float OffsetY { get; set; } = 0f;
            public float OffsetZ { get; set; } = 0f;
        }

        [McpTool("clone_object", "Duplicates a GameObject N times with an incremental offset per copy.")]
        public static object CloneObject(CloneObjectParams p)
        {
            var src = GameObject.Find(p.Name);
            if (src == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var created = new List<string>();
            for (int i = 1; i <= Mathf.Max(1, p.Count); i++)
            {
                var copy = Object.Instantiate(src);
                copy.name = $"{src.name}_{i}";
                copy.transform.position = src.transform.position + new Vector3(p.OffsetX * i, p.OffsetY * i, p.OffsetZ * i);
                Undo.RegisterCreatedObjectUndo(copy, "qFoldIT: Clone Object");
                created.Add(copy.name);
            }

            return new { success = true, created_count = created.Count, created_names = created };
        }

        // ── delete_object ───────────────────────────────────────────────
        public class DeleteObjectParams
        {
            [McpDescription("Name of the GameObject to delete", Required = true)]
            public string Name { get; set; }
        }

        [McpTool("delete_object", "Deletes a GameObject from the active scene by name.")]
        public static object DeleteObject(DeleteObjectParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };
            Undo.DestroyObjectImmediate(go);
            return new { success = true, deleted = p.Name };
        }

        // ── parent_object ───────────────────────────────────────────────
        public class ParentObjectParams
        {
            [McpDescription("Name of the child GameObject", Required = true)]
            public string Child { get; set; }

            [McpDescription("Name of the new parent GameObject; empty string un-parents")]
            public string Parent { get; set; } = "";

            [McpDescription("Keep the child's current world-space transform", Default = true)]
            public bool WorldPositionStays { get; set; } = true;
        }

        [McpTool("parent_object", "Reparents one GameObject under another (or un-parents it).")]
        public static object ParentObject(ParentObjectParams p)
        {
            var child = GameObject.Find(p.Child);
            if (child == null) return new { success = false, error = $"Child '{p.Child}' not found." };

            Transform parentTransform = null;
            if (!string.IsNullOrEmpty(p.Parent))
            {
                var parentGo = GameObject.Find(p.Parent);
                if (parentGo == null) return new { success = false, error = $"Parent '{p.Parent}' not found." };
                parentTransform = parentGo.transform;
            }

            Undo.SetTransformParent(child.transform, parentTransform, "qFoldIT: Parent Object");
            return new { success = true, child = p.Child, parent = p.Parent };
        }

        // ── scene_list_objects ─────────────────────────────────────────
        public class ListObjectsParams
        {
            [McpDescription("Only include root-level objects (skip children)", Default = false)]
            public bool RootOnly { get; set; } = false;
        }

        [McpTool("scene_list_objects", "Lists every GameObject in the active scene with name, position, and active state.")]
        public static object ListObjects(ListObjectsParams p)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var results = new List<object>();

            void Walk(GameObject go)
            {
                results.Add(new
                {
                    name = go.name,
                    active = go.activeSelf,
                    position = new[] { go.transform.position.x, go.transform.position.y, go.transform.position.z },
                    child_count = go.transform.childCount
                });
                if (!p.RootOnly)
                    for (int i = 0; i < go.transform.childCount; i++)
                        Walk(go.transform.GetChild(i).gameObject);
            }

            foreach (var r in roots) Walk(r);
            return new { success = true, scene = scene.name, object_count = results.Count, objects = results };
        }

        // ── scene_find_by_name ──────────────────────────────────────────
        public class FindByNameParams
        {
            [McpDescription("Substring to search for (case-insensitive)", Required = true)]
            public string Query { get; set; }
        }

        [McpTool("scene_find_by_name", "Finds all GameObjects whose name contains the given substring.")]
        public static object FindByName(FindByNameParams p)
        {
            var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go.name.ToLowerInvariant().Contains(p.Query.ToLowerInvariant()))
                .Select(go => go.name)
                .ToList();
            return new { success = true, query = p.Query, matches = all };
        }
    }
}
