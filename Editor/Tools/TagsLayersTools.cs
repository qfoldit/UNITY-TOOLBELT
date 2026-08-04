// qFoldIT Toolbelt for Unity — TagsLayersTools.cs
// Category: TagsLayers

using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class TagsLayersTools
    {
        // ── tag_create ──────────────────────────────────────────────────
        public class TagCreateParams
        {
            [McpDescription("New tag name", Required = true)]
            public string TagName { get; set; }
        }

        [McpTool("tag_create", "Adds a new tag to the project's Tag Manager if it doesn't already exist.")]
        public static object TagCreate(TagCreateParams p)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == p.TagName)
                    return new { success = true, tag = p.TagName, already_existed = true };

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = p.TagName;
            tagManager.ApplyModifiedProperties();

            return new { success = true, tag = p.TagName, already_existed = false };
        }

        // ── tag_assign ──────────────────────────────────────────────────
        public class TagAssignParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("Tag to assign (must already exist — use tag_create first if needed)", Required = true)]
            public string TagName { get; set; }
        }

        [McpTool("tag_assign", "Assigns an existing tag to a GameObject.")]
        public static object TagAssign(TagAssignParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            try
            {
                Undo.RecordObject(go, "qFoldIT: Assign Tag");
                go.tag = p.TagName;
            }
            catch (UnityException e)
            {
                return new { success = false, error = $"Tag '{p.TagName}' does not exist. Call tag_create first. ({e.Message})" };
            }

            return new { success = true, name = p.Name, tag = p.TagName };
        }

        // ── layer_create ────────────────────────────────────────────────
        public class LayerCreateParams
        {
            [McpDescription("New layer name", Required = true)]
            public string LayerName { get; set; }
        }

        [McpTool("layer_create", "Adds a new layer to the project's Tag Manager in the first free user layer slot (layers 8-31).")]
        public static object LayerCreate(LayerCreateParams p)
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProp = tagManager.FindProperty("layers");

            for (int i = 8; i < layersProp.arraySize; i++)
            {
                var element = layersProp.GetArrayElementAtIndex(i);
                if (element.stringValue == p.LayerName) return new { success = true, layer = p.LayerName, slot = i, already_existed = true };
            }

            for (int i = 8; i < layersProp.arraySize; i++)
            {
                var element = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(element.stringValue))
                {
                    element.stringValue = p.LayerName;
                    tagManager.ApplyModifiedProperties();
                    return new { success = true, layer = p.LayerName, slot = i, already_existed = false };
                }
            }

            return new { success = false, error = "No free user layer slots (8-31) available." };
        }

        // ── layer_assign ────────────────────────────────────────────────
        public class LayerAssignParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("Layer name (must already exist — use layer_create first if needed)", Required = true)]
            public string LayerName { get; set; }
            [McpDescription("Also apply to all children recursively", Default = false)]
            public bool IncludeChildren { get; set; } = false;
        }

        [McpTool("layer_assign", "Assigns an existing layer to a GameObject, optionally including all its children.")]
        public static object LayerAssign(LayerAssignParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            int layer = LayerMask.NameToLayer(p.LayerName);
            if (layer < 0) return new { success = false, error = $"Layer '{p.LayerName}' does not exist. Call layer_create first." };

            void Apply(GameObject target)
            {
                Undo.RecordObject(target, "qFoldIT: Assign Layer");
                target.layer = layer;
                if (p.IncludeChildren)
                    for (int i = 0; i < target.transform.childCount; i++)
                        Apply(target.transform.GetChild(i).gameObject);
            }
            Apply(go);

            return new { success = true, name = p.Name, layer = p.LayerName };
        }
    }
}
