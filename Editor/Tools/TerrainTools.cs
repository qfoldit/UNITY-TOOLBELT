// qFoldIT Toolbelt for Unity — TerrainTools.cs
// Category: Terrain

using System.IO;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class TerrainTools
    {
        // ── terrain_create ──────────────────────────────────────────────
        public class TerrainCreateParams
        {
            public string Name { get; set; } = "Terrain";
            public float Width { get; set; } = 500f;
            public float Length { get; set; } = 500f;
            public float Height { get; set; } = 100f;
            [McpDescription("Heightmap resolution (power-of-two + 1, e.g. 513)", Default = 513)]
            public int Resolution { get; set; } = 513;
        }

        [McpTool("terrain_create", "Creates a new flat Terrain GameObject with the given world size and heightmap resolution.")]
        public static object TerrainCreate(TerrainCreateParams p)
        {
            var data = new TerrainData
            {
                heightmapResolution = p.Resolution,
                size = new Vector3(p.Width, p.Height, p.Length)
            };

            var dataDir = "Assets/Terrains";
            if (!AssetDatabase.IsValidFolder(dataDir)) AssetDatabase.CreateFolder("Assets", "Terrains");
            var dataPath = $"{dataDir}/{p.Name}_Data.asset";
            AssetDatabase.CreateAsset(data, dataPath);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = p.Name;
            Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Create Terrain");

            return new { success = true, name = p.Name, data_path = dataPath, size = new[] { p.Width, p.Height, p.Length } };
        }

        // ── terrain_sculpt_hill ─────────────────────────────────────────
        public class SculptHillParams
        {
            [McpDescription("Target Terrain GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("Normalized X position (0-1) within the terrain", Default = 0.5f)]
            public float NormX { get; set; } = 0.5f;
            [McpDescription("Normalized Z position (0-1) within the terrain", Default = 0.5f)]
            public float NormZ { get; set; } = 0.5f;
            [McpDescription("Normalized radius (0-1) of the hill/crater", Default = 0.1f)]
            public float NormRadius { get; set; } = 0.1f;
            [McpDescription("Height delta, -1..1 (positive = hill, negative = crater)", Default = 0.2f)]
            public float HeightDelta { get; set; } = 0.2f;
        }

        [McpTool("terrain_sculpt_hill", "Raises or lowers a smooth circular hill/crater into a Terrain's heightmap at a normalized position.")]
        public static object SculptHill(SculptHillParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Terrain '{p.Name}' not found." };
            var terrain = go.GetComponent<Terrain>();
            if (terrain == null) return new { success = false, error = $"'{p.Name}' has no Terrain component." };

            var data = terrain.terrainData;
            int res = data.heightmapResolution;
            var heights = data.GetHeights(0, 0, res, res);

            int cx = Mathf.RoundToInt(p.NormX * res);
            int cz = Mathf.RoundToInt(p.NormZ * res);
            int radiusPx = Mathf.Max(1, Mathf.RoundToInt(p.NormRadius * res));

            for (int z = Mathf.Max(0, cz - radiusPx); z < Mathf.Min(res, cz + radiusPx); z++)
            {
                for (int x = Mathf.Max(0, cx - radiusPx); x < Mathf.Min(res, cx + radiusPx); x++)
                {
                    float d = Vector2.Distance(new Vector2(x, z), new Vector2(cx, cz)) / radiusPx;
                    if (d > 1f) continue;
                    float falloff = Mathf.Cos(d * Mathf.PI * 0.5f); // smooth falloff to 0 at the edge
                    heights[z, x] = Mathf.Clamp01(heights[z, x] + p.HeightDelta * falloff);
                }
            }

            data.SetHeights(0, 0, heights);
            return new { success = true, name = p.Name };
        }

        // ── terrain_flatten ─────────────────────────────────────────────
        public class FlattenParams
        {
            [McpDescription("Target Terrain GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("Normalized height, 0-1", Default = 0f)]
            public float NormHeight { get; set; } = 0f;
        }

        [McpTool("terrain_flatten", "Sets every point of a Terrain's heightmap to a single normalized height.")]
        public static object Flatten(FlattenParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Terrain '{p.Name}' not found." };
            var terrain = go.GetComponent<Terrain>();
            if (terrain == null) return new { success = false, error = $"'{p.Name}' has no Terrain component." };

            var data = terrain.terrainData;
            int res = data.heightmapResolution;
            var heights = new float[res, res];
            for (int z = 0; z < res; z++)
                for (int x = 0; x < res; x++)
                    heights[z, x] = p.NormHeight;

            data.SetHeights(0, 0, heights);
            return new { success = true, name = p.Name, height = p.NormHeight };
        }

        // ── terrain_paint_texture ───────────────────────────────────────
        public class PaintTextureParams
        {
            [McpDescription("Target Terrain GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("Diffuse texture asset path to add as a terrain layer", Required = true)]
            public string TexturePath { get; set; }
            [McpDescription("World-unit tiling size for the layer", Default = 15f)]
            public float TileSize { get; set; } = 15f;
        }

        [McpTool("terrain_paint_texture", "Adds a TerrainLayer (from a diffuse texture) to a Terrain and fully paints it across the whole surface.")]
        public static object PaintTexture(PaintTextureParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Terrain '{p.Name}' not found." };
            var terrain = go.GetComponent<Terrain>();
            if (terrain == null) return new { success = false, error = $"'{p.Name}' has no Terrain component." };

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(p.TexturePath);
            if (tex == null) return new { success = false, error = $"No texture at '{p.TexturePath}'." };

            var layer = new TerrainLayer { diffuseTexture = tex, tileSize = new Vector2(p.TileSize, p.TileSize) };
            var layerDir = "Assets/Terrains/Layers";
            if (!AssetDatabase.IsValidFolder(layerDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Terrains")) AssetDatabase.CreateFolder("Assets", "Terrains");
                AssetDatabase.CreateFolder("Assets/Terrains", "Layers");
            }
            var layerPath = $"{layerDir}/{Path.GetFileNameWithoutExtension(p.TexturePath)}_Layer.asset";
            AssetDatabase.CreateAsset(layer, layerPath);

            var data = terrain.terrainData;
            var layers = new System.Collections.Generic.List<TerrainLayer>(data.terrainLayers) { layer };
            data.terrainLayers = layers.ToArray();

            int newIndex = layers.Count - 1;
            var alphamaps = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);
            for (int y = 0; y < data.alphamapHeight; y++)
                for (int x = 0; x < data.alphamapWidth; x++)
                    for (int l = 0; l < layers.Count; l++)
                        alphamaps[y, x, l] = l == newIndex ? 1f : 0f;
            data.SetAlphamaps(0, 0, alphamaps);

            return new { success = true, name = p.Name, layer_path = layerPath };
        }

        // ── terrain_add_trees ───────────────────────────────────────────
        public class AddTreesParams
        {
            [McpDescription("Target Terrain GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("Tree prefab asset path", Required = true)]
            public string PrefabPath { get; set; }
            [McpDescription("Number of trees to scatter randomly", Default = 200)]
            public int Count { get; set; } = 200;
        }

        [McpTool("terrain_add_trees", "Registers a tree prefab on a Terrain and scatters N instances at random positions across the surface.")]
        public static object AddTrees(AddTreesParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Terrain '{p.Name}' not found." };
            var terrain = go.GetComponent<Terrain>();
            if (terrain == null) return new { success = false, error = $"'{p.Name}' has no Terrain component." };

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(p.PrefabPath);
            if (prefab == null) return new { success = false, error = $"No prefab at '{p.PrefabPath}'." };

            var data = terrain.terrainData;
            var protoList = new System.Collections.Generic.List<TreePrototype>(data.treePrototypes)
            {
                new TreePrototype { prefab = prefab }
            };
            data.treePrototypes = protoList.ToArray();
            int protoIndex = protoList.Count - 1;

            var instances = new System.Collections.Generic.List<TreeInstance>(data.treeInstances);
            var rng = new System.Random();
            for (int i = 0; i < p.Count; i++)
            {
                instances.Add(new TreeInstance
                {
                    position = new Vector3((float)rng.NextDouble(), 0f, (float)rng.NextDouble()),
                    prototypeIndex = protoIndex,
                    widthScale = 1f,
                    heightScale = 1f,
                    color = Color.white,
                    lightmapColor = Color.white
                });
            }
            data.treeInstances = instances.ToArray();

            return new { success = true, name = p.Name, trees_added = p.Count };
        }
    }
}
