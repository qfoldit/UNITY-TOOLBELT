// qFoldIT Toolbelt for Unity — MeasurementTools.cs
// Category: Measurement

using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class MeasurementTools
    {
        // ── measure_distance ────────────────────────────────────────────
        public class MeasureDistanceParams
        {
            [McpDescription("First GameObject name", Required = true)]
            public string ObjectA { get; set; }
            [McpDescription("Second GameObject name", Required = true)]
            public string ObjectB { get; set; }
        }

        [McpTool("measure_distance", "Reports the world-space distance between two named GameObjects.")]
        public static object MeasureDistance(MeasureDistanceParams p)
        {
            var a = GameObject.Find(p.ObjectA);
            var b = GameObject.Find(p.ObjectB);
            if (a == null) return new { success = false, error = $"Object '{p.ObjectA}' not found." };
            if (b == null) return new { success = false, error = $"Object '{p.ObjectB}' not found." };

            float dist = Vector3.Distance(a.transform.position, b.transform.position);
            return new { success = true, distance = dist };
        }

        // ── measure_bounds ──────────────────────────────────────────────
        public class MeasureBoundsParams
        {
            [McpDescription("GameObject name (uses combined Renderer bounds of it and its children)", Required = true)]
            public string Name { get; set; }
        }

        [McpTool("measure_bounds", "Reports the combined world-space bounding box (center + size) of a GameObject and its children's renderers.")]
        public static object MeasureBounds(MeasureBoundsParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new { success = false, error = $"'{p.Name}' has no renderers to measure." };

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            return new
            {
                success = true,
                center = new[] { bounds.center.x, bounds.center.y, bounds.center.z },
                size = new[] { bounds.size.x, bounds.size.y, bounds.size.z }
            };
        }

        // ── measure_scene_bounds ────────────────────────────────────────
        public class MeasureSceneBoundsParams { }

        [McpTool("measure_scene_bounds", "Reports the combined world-space bounding box of every renderer in the active scene.")]
        public static object MeasureSceneBounds(MeasureSceneBoundsParams p)
        {
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            if (renderers.Length == 0) return new { success = false, error = "No renderers in the active scene." };

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            return new
            {
                success = true,
                renderer_count = renderers.Length,
                center = new[] { bounds.center.x, bounds.center.y, bounds.center.z },
                size = new[] { bounds.size.x, bounds.size.y, bounds.size.z }
            };
        }
    }
}
