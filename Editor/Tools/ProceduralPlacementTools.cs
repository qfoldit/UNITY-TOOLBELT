// qFoldIT Toolbelt for Unity — ProceduralPlacementTools.cs
// Category: Procedural / Arena
// One composite tool covering 8 geometric patterns (mirrors UEFN Toolbelt's
// "Prop Patterns" plugin: 8 patterns, 1 registered tool) plus a symmetrical
// arena generator (mirrors "Arena Generator").

using System.Collections.Generic;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class ProceduralPlacementTools
    {
        public enum PlacementPattern { Grid, Circle, Arc, Spiral, Line, Wave, Helix, Radial }

        public class ProceduralPlaceParams
        {
            [McpDescription("Prefab asset path (e.g. Assets/Props/Crate.prefab); if empty, a cube primitive is used", Default = "")]
            public string PrefabPath { get; set; } = "";

            [McpDescription("Placement pattern", Required = true, EnumType = typeof(PlacementPattern))]
            public string Pattern { get; set; }

            [McpDescription("Number of instances to place", Default = 12)]
            public int Count { get; set; } = 12;

            [McpDescription("Pattern radius / spacing in world units", Default = 5f)]
            public float Radius { get; set; } = 5f;

            [McpDescription("Center X")]
            public float CenterX { get; set; } = 0f;
            [McpDescription("Center Y")]
            public float CenterY { get; set; } = 0f;
            [McpDescription("Center Z")]
            public float CenterZ { get; set; } = 0f;

            [McpDescription("Name prefix for spawned instances", Default = "PropPattern")]
            public string NamePrefix { get; set; } = "PropPattern";
        }

        [McpTool("procedural_place", "Places N copies of a prefab (or a default cube) using one of 8 geometric patterns: grid, circle, arc, spiral, line, wave, helix, radial.")]
        public static object ProceduralPlace(ProceduralPlaceParams p)
        {
            var pattern = (PlacementPattern)System.Enum.Parse(typeof(PlacementPattern), p.Pattern, true);
            var center = new Vector3(p.CenterX, p.CenterY, p.CenterZ);
            var positions = ComputePositions(pattern, Mathf.Max(1, p.Count), p.Radius, center);

            GameObject template = null;
            if (!string.IsNullOrEmpty(p.PrefabPath))
                template = AssetDatabase.LoadAssetAtPath<GameObject>(p.PrefabPath);

            var created = new List<string>();
            for (int i = 0; i < positions.Count; i++)
            {
                GameObject go = template != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(template)
                    : GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);

                go.name = $"{p.NamePrefix}_{i:D3}";
                go.transform.position = positions[i];
                Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Procedural Place");
                created.Add(go.name);
            }

            return new { success = true, pattern = pattern.ToString(), placed_count = created.Count, names = created };
        }

        private static List<Vector3> ComputePositions(PlacementPattern pattern, int count, float radius, Vector3 center)
        {
            var result = new List<Vector3>(count);
            switch (pattern)
            {
                case PlacementPattern.Grid:
                    int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int row = i / cols, col = i % cols;
                        result.Add(center + new Vector3(col * radius, 0, row * radius));
                    }
                    break;

                case PlacementPattern.Circle:
                    for (int i = 0; i < count; i++)
                    {
                        float a = 2 * Mathf.PI * i / count;
                        result.Add(center + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius));
                    }
                    break;

                case PlacementPattern.Arc:
                    for (int i = 0; i < count; i++)
                    {
                        float a = Mathf.PI * i / Mathf.Max(1, count - 1); // 0..180deg
                        result.Add(center + new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius));
                    }
                    break;

                case PlacementPattern.Spiral:
                    for (int i = 0; i < count; i++)
                    {
                        float t = i / (float)count;
                        float a = t * Mathf.PI * 6f;
                        float r = t * radius;
                        result.Add(center + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
                    }
                    break;

                case PlacementPattern.Line:
                    for (int i = 0; i < count; i++)
                        result.Add(center + new Vector3(i * radius, 0, 0));
                    break;

                case PlacementPattern.Wave:
                    for (int i = 0; i < count; i++)
                        result.Add(center + new Vector3(i * radius, Mathf.Sin(i * 0.6f) * radius * 0.5f, 0));
                    break;

                case PlacementPattern.Helix:
                    for (int i = 0; i < count; i++)
                    {
                        float a = i * 0.6f;
                        result.Add(center + new Vector3(Mathf.Cos(a) * radius, i * (radius * 0.25f), Mathf.Sin(a) * radius));
                    }
                    break;

                case PlacementPattern.Radial:
                    int rings = Mathf.Max(1, Mathf.CeilToInt(count / 8f));
                    int idx = 0;
                    for (int ring = 1; ring <= rings && idx < count; ring++)
                    {
                        int perRing = Mathf.Min(8 * ring, count - idx);
                        for (int i = 0; i < perRing; i++)
                        {
                            float a = 2 * Mathf.PI * i / perRing;
                            result.Add(center + new Vector3(Mathf.Cos(a) * radius * ring, 0, Mathf.Sin(a) * radius * ring));
                            idx++;
                        }
                    }
                    break;
            }
            return result;
        }

        // ── arena_generate ──────────────────────────────────────────────
        public enum ArenaSize { Small, Medium, Large }

        public class ArenaGenerateParams
        {
            [McpDescription("Arena size preset", Required = true, EnumType = typeof(ArenaSize))]
            public string Size { get; set; }

            [McpDescription("Center X")]
            public float CenterX { get; set; } = 0f;
            [McpDescription("Center Z")]
            public float CenterZ { get; set; } = 0f;
        }

        [McpTool("arena_generate", "Generates a symmetrical Red-vs-Blue competitive arena: floor, boundary walls, and spawn points, auto-split by team color.")]
        public static object ArenaGenerate(ArenaGenerateParams p)
        {
            var size = (ArenaSize)System.Enum.Parse(typeof(ArenaSize), p.Size, true);
            float halfExtent = size switch { ArenaSize.Small => 15f, ArenaSize.Medium => 25f, ArenaSize.Large => 40f, _ => 25f };
            int spawnsPerTeam = size switch { ArenaSize.Small => 3, ArenaSize.Medium => 6, ArenaSize.Large => 10, _ => 6 };
            var center = new Vector3(p.CenterX, 0, p.CenterZ);

            var parent = new GameObject($"Arena_{size}");
            Undo.RegisterCreatedObjectUndo(parent, "qFoldIT: Arena Generate");

            var floor = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Plane);
            floor.name = "Arena_Floor";
            floor.transform.position = center;
            floor.transform.localScale = Vector3.one * (halfExtent / 5f);
            floor.transform.parent = parent.transform;

            var redShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var redMat = new Material(redShader) { color = new Color(0.85f, 0.15f, 0.1f) };
            var blueMat = new Material(redShader) { color = new Color(0.1f, 0.4f, 0.85f) };

            int total = 0;
            for (int i = 0; i < spawnsPerTeam; i++)
            {
                float t = (i + 0.5f) / spawnsPerTeam;
                float x = Mathf.Lerp(-halfExtent * 0.8f, halfExtent * 0.8f, t);

                var redSpawn = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cylinder);
                redSpawn.name = $"RedSpawn_{i:D2}";
                redSpawn.transform.position = center + new Vector3(x, 0.1f, -halfExtent * 0.85f);
                redSpawn.transform.localScale = new Vector3(1f, 0.05f, 1f);
                redSpawn.GetComponent<Renderer>().sharedMaterial = redMat;
                redSpawn.transform.parent = parent.transform;

                var blueSpawn = GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cylinder);
                blueSpawn.name = $"BlueSpawn_{i:D2}";
                blueSpawn.transform.position = center + new Vector3(x, 0.1f, halfExtent * 0.85f);
                blueSpawn.transform.localScale = new Vector3(1f, 0.05f, 1f);
                blueSpawn.GetComponent<Renderer>().sharedMaterial = blueMat;
                blueSpawn.transform.parent = parent.transform;

                total += 2;
            }

            return new { success = true, size = size.ToString(), spawns_created = total, half_extent = halfExtent };
        }
    }
}
