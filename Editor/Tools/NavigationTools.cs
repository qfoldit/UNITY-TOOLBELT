// qFoldIT Toolbelt for Unity — NavigationTools.cs
// Category: Navigation

using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEditor.AI;
using UnityEngine;
using UnityEngine.AI;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class NavigationTools
    {
        // ── nav_bake_navmesh ───────────────────────────────────────────
        public class BakeNavMeshParams { }

        [McpTool("nav_bake_navmesh", "Bakes the scene's NavMesh using the current Navigation window settings.")]
        public static object BakeNavMesh(BakeNavMeshParams p)
        {
            NavMeshBuilder.BuildNavMesh();
            return new { success = true };
        }

        // ── nav_add_agent ───────────────────────────────────────────────
        public class AddAgentParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            public float Speed { get; set; } = 3.5f;
            public float Radius { get; set; } = 0.5f;
            public float Height { get; set; } = 2f;
            public float AngularSpeed { get; set; } = 120f;
        }

        [McpTool("nav_add_agent", "Adds and configures a NavMeshAgent component on a GameObject.")]
        public static object AddAgent(AddAgentParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var agent = go.GetComponent<NavMeshAgent>() ?? Undo.AddComponent<NavMeshAgent>(go);
            agent.speed = p.Speed;
            agent.radius = p.Radius;
            agent.height = p.Height;
            agent.angularSpeed = p.AngularSpeed;

            return new { success = true, name = p.Name, speed = p.Speed };
        }

        // ── nav_add_obstacle ────────────────────────────────────────────
        public class AddObstacleParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("If true, this obstacle carves a hole in the baked NavMesh at runtime", Default = true)]
            public bool Carve { get; set; } = true;
        }

        [McpTool("nav_add_obstacle", "Adds a NavMeshObstacle component to a GameObject, optionally carving the baked NavMesh at runtime.")]
        public static object AddObstacle(AddObstacleParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var obstacle = go.GetComponent<NavMeshObstacle>() ?? Undo.AddComponent<NavMeshObstacle>(go);
            obstacle.carving = p.Carve;

            return new { success = true, name = p.Name, carve = p.Carve };
        }

        // ── nav_set_destination ─────────────────────────────────────────
        public class SetDestinationParams
        {
            [McpDescription("GameObject name with a NavMeshAgent", Required = true)]
            public string AgentName { get; set; }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

        [McpTool("nav_set_destination", "Sets a NavMeshAgent's destination (Play Mode only — pathing only evaluates while the game is running).")]
        public static object SetDestination(SetDestinationParams p)
        {
            var go = GameObject.Find(p.AgentName);
            if (go == null) return new { success = false, error = $"Object '{p.AgentName}' not found." };
            var agent = go.GetComponent<NavMeshAgent>();
            if (agent == null) return new { success = false, error = $"'{p.AgentName}' has no NavMeshAgent." };

            if (!Application.isPlaying)
                return new { success = false, error = "nav_set_destination requires Play Mode; NavMeshAgent pathing does not evaluate in Edit Mode." };

            bool ok = agent.SetDestination(new Vector3(p.X, p.Y, p.Z));
            return new { success = ok, agent = p.AgentName, destination = new[] { p.X, p.Y, p.Z } };
        }
    }
}
