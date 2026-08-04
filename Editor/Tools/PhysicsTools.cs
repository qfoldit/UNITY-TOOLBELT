// qFoldIT Toolbelt for Unity — PhysicsTools.cs
// Category: Physics

using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class PhysicsTools
    {
        // ── physics_add_rigidbody ──────────────────────────────────────
        public class AddRigidbodyParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            public float Mass { get; set; } = 1f;
            public bool UseGravity { get; set; } = true;
            public bool IsKinematic { get; set; } = false;
            [McpDescription("Drag / linear damping", Default = 0f)]
            public float Drag { get; set; } = 0f;
        }

        [McpTool("physics_add_rigidbody", "Adds and configures a Rigidbody component on a GameObject.")]
        public static object AddRigidbody(AddRigidbodyParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var rb = go.GetComponent<Rigidbody>() ?? Undo.AddComponent<Rigidbody>(go);
            rb.mass = p.Mass;
            rb.useGravity = p.UseGravity;
            rb.isKinematic = p.IsKinematic;
            rb.linearDamping = p.Drag;

            return new { success = true, name = p.Name, mass = p.Mass, use_gravity = p.UseGravity };
        }

        // ── physics_add_collider ───────────────────────────────────────
        public enum ColliderShape { Box, Sphere, Capsule, Mesh }

        public class AddColliderParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("Collider shape", Required = true, EnumType = typeof(ColliderShape))]
            public string Shape { get; set; }
            public bool IsTrigger { get; set; } = false;
        }

        [McpTool("physics_add_collider", "Adds a Box/Sphere/Capsule/Mesh collider to a GameObject, optionally as a trigger.")]
        public static object AddCollider(AddColliderParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var shape = (ColliderShape)System.Enum.Parse(typeof(ColliderShape), p.Shape, true);
            Collider col = shape switch
            {
                ColliderShape.Box => Undo.AddComponent<BoxCollider>(go),
                ColliderShape.Sphere => Undo.AddComponent<SphereCollider>(go),
                ColliderShape.Capsule => Undo.AddComponent<CapsuleCollider>(go),
                ColliderShape.Mesh => Undo.AddComponent<MeshCollider>(go),
                _ => Undo.AddComponent<BoxCollider>(go)
            };
            col.isTrigger = p.IsTrigger;

            return new { success = true, name = p.Name, shape = shape.ToString(), is_trigger = p.IsTrigger };
        }

        // ── physics_set_physics_material ───────────────────────────────
        public class SetPhysicsMaterialParams
        {
            [McpDescription("Target GameObject name (must already have a Collider)", Required = true)]
            public string Name { get; set; }
            public float Friction { get; set; } = 0.6f;
            public float Bounciness { get; set; } = 0f;
        }

        [McpTool("physics_set_physics_material", "Creates and assigns a PhysicsMaterial (friction/bounciness) to a GameObject's collider.")]
        public static object SetPhysicsMaterial(SetPhysicsMaterialParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };
            var col = go.GetComponent<Collider>();
            if (col == null) return new { success = false, error = $"'{p.Name}' has no Collider." };

            var mat = new PhysicsMaterial($"qFoldIT_PhysMat_{p.Name}") { dynamicFriction = p.Friction, staticFriction = p.Friction, bounciness = p.Bounciness };
            Undo.RecordObject(col, "qFoldIT: Set Physics Material");
            col.sharedMaterial = mat;

            return new { success = true, name = p.Name, friction = p.Friction, bounciness = p.Bounciness };
        }

        // ── physics_add_joint ───────────────────────────────────────────
        public enum JointType { Fixed, Hinge, Spring, Configurable }

        public class AddJointParams
        {
            [McpDescription("Target GameObject name (gets the Rigidbody the joint is attached to)", Required = true)]
            public string Name { get; set; }
            [McpDescription("Joint type", Required = true, EnumType = typeof(JointType))]
            public string JointType { get; set; }
            [McpDescription("Name of the GameObject this joint connects to; empty = connect to world", Default = "")]
            public string ConnectedBody { get; set; } = "";
        }

        [McpTool("physics_add_joint", "Adds a Fixed/Hinge/Spring/Configurable joint connecting a GameObject to another rigidbody (or the world).")]
        public static object AddJoint(AddJointParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };
            if (go.GetComponent<Rigidbody>() == null) Undo.AddComponent<Rigidbody>(go);

            Rigidbody connected = null;
            if (!string.IsNullOrEmpty(p.ConnectedBody))
            {
                var connGo = GameObject.Find(p.ConnectedBody);
                if (connGo == null) return new { success = false, error = $"Connected body '{p.ConnectedBody}' not found." };
                connected = connGo.GetComponent<Rigidbody>() ?? Undo.AddComponent<Rigidbody>(connGo);
            }

            var type = (JointType)System.Enum.Parse(typeof(JointType), p.JointType, true);
            Joint joint = type switch
            {
                JointType.Fixed => Undo.AddComponent<FixedJoint>(go),
                JointType.Hinge => Undo.AddComponent<HingeJoint>(go),
                JointType.Spring => Undo.AddComponent<SpringJoint>(go),
                JointType.Configurable => Undo.AddComponent<ConfigurableJoint>(go),
                _ => Undo.AddComponent<FixedJoint>(go)
            };
            joint.connectedBody = connected;

            return new { success = true, name = p.Name, joint_type = type.ToString(), connected_to = p.ConnectedBody };
        }

        // ── physics_raycast_query ──────────────────────────────────────
        public class RaycastQueryParams
        {
            public float OriginX { get; set; } = 0f;
            public float OriginY { get; set; } = 0f;
            public float OriginZ { get; set; } = 0f;
            public float DirX { get; set; } = 0f;
            public float DirY { get; set; } = -1f;
            public float DirZ { get; set; } = 0f;
            public float MaxDistance { get; set; } = 100f;
        }

        [McpTool("physics_raycast_query", "Casts a ray in the Editor's physics scene and reports the first hit (name, point, distance), if any.")]
        public static object RaycastQuery(RaycastQueryParams p)
        {
            var origin = new Vector3(p.OriginX, p.OriginY, p.OriginZ);
            var dir = new Vector3(p.DirX, p.DirY, p.DirZ).normalized;

            if (Physics.Raycast(origin, dir, out var hit, p.MaxDistance))
            {
                return new
                {
                    success = true,
                    hit = true,
                    object_name = hit.collider.gameObject.name,
                    point = new[] { hit.point.x, hit.point.y, hit.point.z },
                    distance = hit.distance
                };
            }
            return new { success = true, hit = false };
        }

        // ── physics_set_gravity ────────────────────────────────────────
        public class SetGravityParams
        {
            public float X { get; set; } = 0f;
            public float Y { get; set; } = -9.81f;
            public float Z { get; set; } = 0f;
        }

        [McpTool("physics_set_gravity", "Sets the global Physics.gravity vector for the project.")]
        public static object SetGravity(SetGravityParams p)
        {
            Physics.gravity = new Vector3(p.X, p.Y, p.Z);
            return new { success = true, gravity = new[] { p.X, p.Y, p.Z } };
        }
    }
}
