// qFoldIT Toolbelt for Unity — ComponentTools.cs
// Category: Components
// Reflection-based generic component add/remove/get/set — the escape hatch
// for any component type not covered by a dedicated tool (Scene, Physics,
// Audio, etc. tools wrap the common cases with proper typed parameters;
// this file handles everything else).

using System.Linq;
using System.Reflection;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class ComponentTools
    {
        private static System.Type ResolveComponentType(string typeName)
        {
            // Try the common UnityEngine namespace first, then a full scan —
            // covers both built-in components (e.g. "Rigidbody") and
            // project-defined MonoBehaviours (e.g. "PlayerController").
            var direct = System.Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (direct != null) return direct;

            return System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return System.Type.EmptyTypes; } })
                .FirstOrDefault(t => t.Name == typeName && typeof(Component).IsAssignableFrom(t));
        }

        // ── component_add ──────────────────────────────────────────────
        public class AddParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("Component type name, e.g. 'Rigidbody' or a project MonoBehaviour class name", Required = true)]
            public string ComponentType { get; set; }
        }

        [McpTool("component_add", "Adds a component to a GameObject by type name — works for built-in Unity components and project-defined MonoBehaviours alike.")]
        public static object Add(AddParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var type = ResolveComponentType(p.ComponentType);
            if (type == null) return new { success = false, error = $"Component type '{p.ComponentType}' not found." };

            var comp = Undo.AddComponent(go, type);
            return new { success = comp != null, name = p.Name, component_type = p.ComponentType };
        }

        // ── component_remove ────────────────────────────────────────────
        public class RemoveParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("Component type name to remove", Required = true)]
            public string ComponentType { get; set; }
        }

        [McpTool("component_remove", "Removes the first matching component of the given type name from a GameObject.")]
        public static object Remove(RemoveParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var type = ResolveComponentType(p.ComponentType);
            if (type == null) return new { success = false, error = $"Component type '{p.ComponentType}' not found." };

            var comp = go.GetComponent(type);
            if (comp == null) return new { success = false, error = $"'{p.Name}' has no component of type '{p.ComponentType}'." };

            Undo.DestroyObjectImmediate(comp);
            return new { success = true, name = p.Name, removed = p.ComponentType };
        }

        // ── component_set_field ────────────────────────────────────────
        public class SetFieldParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("Component type name", Required = true)]
            public string ComponentType { get; set; }
            [McpDescription("Public field or property name on the component", Required = true)]
            public string FieldName { get; set; }
            [McpDescription("New value, parsed as float/bool/string based on the field's declared type", Required = true)]
            public string Value { get; set; }
        }

        [McpTool("component_set_field", "Sets a public field or property on a component via reflection — supports float, int, bool, and string field types.")]
        public static object SetField(SetFieldParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var type = ResolveComponentType(p.ComponentType);
            if (type == null) return new { success = false, error = $"Component type '{p.ComponentType}' not found." };

            var comp = go.GetComponent(type);
            if (comp == null) return new { success = false, error = $"'{p.Name}' has no component of type '{p.ComponentType}'." };

            var field = type.GetField(p.FieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                Undo.RecordObject(comp, "qFoldIT: Set Component Field");
                field.SetValue(comp, ConvertValue(p.Value, field.FieldType));
                return new { success = true, name = p.Name, field = p.FieldName, value = p.Value };
            }

            var prop = type.GetProperty(p.FieldName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                Undo.RecordObject(comp, "qFoldIT: Set Component Field");
                prop.SetValue(comp, ConvertValue(p.Value, prop.PropertyType));
                return new { success = true, name = p.Name, field = p.FieldName, value = p.Value };
            }

            return new { success = false, error = $"No writable field/property '{p.FieldName}' on '{p.ComponentType}'." };
        }

        private static object ConvertValue(string raw, System.Type targetType)
        {
            if (targetType == typeof(float)) return float.Parse(raw);
            if (targetType == typeof(int)) return int.Parse(raw);
            if (targetType == typeof(bool)) return bool.Parse(raw);
            if (targetType == typeof(string)) return raw;
            return raw;
        }

        // ── component_list ──────────────────────────────────────────────
        public class ListParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
        }

        [McpTool("component_list", "Lists every component attached to a GameObject by type name.")]
        public static object List(ListParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var types = go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).ToArray();
            return new { success = true, name = p.Name, components = types };
        }

        // ── component_get_field ────────────────────────────────────────
        public class GetFieldParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("Component type name", Required = true)]
            public string ComponentType { get; set; }
            [McpDescription("Public field or property name", Required = true)]
            public string FieldName { get; set; }
        }

        [McpTool("component_get_field", "Reads a public field or property value from a component via reflection.")]
        public static object GetField(GetFieldParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var type = ResolveComponentType(p.ComponentType);
            if (type == null) return new { success = false, error = $"Component type '{p.ComponentType}' not found." };

            var comp = go.GetComponent(type);
            if (comp == null) return new { success = false, error = $"'{p.Name}' has no component of type '{p.ComponentType}'." };

            var field = type.GetField(p.FieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return new { success = true, value = field.GetValue(comp)?.ToString() };

            var prop = type.GetProperty(p.FieldName, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanRead) return new { success = true, value = prop.GetValue(comp)?.ToString() };

            return new { success = false, error = $"No readable field/property '{p.FieldName}' on '{p.ComponentType}'." };
        }
    }
}
