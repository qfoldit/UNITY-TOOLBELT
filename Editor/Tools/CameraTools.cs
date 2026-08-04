// qFoldIT Toolbelt for Unity — CameraTools.cs
// Category: Camera

using System.IO;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class CameraTools
    {
        // ── camera_create_rig ───────────────────────────────────────────
        public class CreateRigParams
        {
            public string Name { get; set; } = "Camera";
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 2f;
            public float Z { get; set; } = -10f;
            public float Fov { get; set; } = 60f;
            [McpDescription("Make this the active MainCamera (tag = MainCamera)", Default = true)]
            public bool SetAsMain { get; set; } = true;
        }

        [McpTool("camera_create_rig", "Creates a Camera GameObject at a world position with a given field of view.")]
        public static object CreateRig(CreateRigParams p)
        {
            var go = new GameObject(p.Name, typeof(Camera));
            go.transform.position = new Vector3(p.X, p.Y, p.Z);
            var cam = go.GetComponent<Camera>();
            cam.fieldOfView = p.Fov;
            if (p.SetAsMain) go.tag = "MainCamera";

            Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Create Camera");
            return new { success = true, name = go.name, fov = p.Fov };
        }

        // ── camera_set_follow_target ────────────────────────────────────
        public class SetFollowTargetParams
        {
            [McpDescription("Camera GameObject name", Required = true)]
            public string CameraName { get; set; }
            [McpDescription("Target GameObject name to follow", Required = true)]
            public string TargetName { get; set; }
            [McpDescription("Fixed local offset from target", Default = "0,3,-8")]
            public float OffsetX { get; set; } = 0f;
            public float OffsetY { get; set; } = 3f;
            public float OffsetZ { get; set; } = -8f;
        }

        [McpTool("camera_set_follow_target", "Attaches a simple qFoldITCameraFollow component so the camera trails a target with a fixed offset (no Cinemachine dependency).")]
        public static object SetFollowTarget(SetFollowTargetParams p)
        {
            var camGo = GameObject.Find(p.CameraName);
            if (camGo == null) return new { success = false, error = $"Camera '{p.CameraName}' not found." };
            var targetGo = GameObject.Find(p.TargetName);
            if (targetGo == null) return new { success = false, error = $"Target '{p.TargetName}' not found." };

            var follow = camGo.GetComponent<QFoldITCameraFollow>() ?? Undo.AddComponent<QFoldITCameraFollow>(camGo);
            follow.target = targetGo.transform;
            follow.offset = new Vector3(p.OffsetX, p.OffsetY, p.OffsetZ);

            return new { success = true, camera = p.CameraName, target = p.TargetName };
        }

        // ── camera_set_clipping ─────────────────────────────────────────
        public class SetClippingParams
        {
            [McpDescription("Camera GameObject name", Required = true)]
            public string CameraName { get; set; }
            public float Near { get; set; } = 0.3f;
            public float Far { get; set; } = 1000f;
        }

        [McpTool("camera_set_clipping", "Sets a Camera's near/far clipping planes.")]
        public static object SetClipping(SetClippingParams p)
        {
            var go = GameObject.Find(p.CameraName);
            if (go == null) return new { success = false, error = $"Camera '{p.CameraName}' not found." };
            var cam = go.GetComponent<Camera>();
            if (cam == null) return new { success = false, error = $"'{p.CameraName}' has no Camera component." };

            cam.nearClipPlane = p.Near;
            cam.farClipPlane = p.Far;
            return new { success = true, near = p.Near, far = p.Far };
        }

        // ── camera_set_background ───────────────────────────────────────
        public class SetBackgroundParams
        {
            [McpDescription("Camera GameObject name", Required = true)]
            public string CameraName { get; set; }
            [McpDescription("Clear flags", EnumType = typeof(CameraClearFlags), Default = "Skybox")]
            public string ClearFlags { get; set; } = "Skybox";
            [McpDescription("Solid background color as hex, used when ClearFlags = SolidColor", Default = "202020")]
            public string ColorHex { get; set; } = "202020";
        }

        [McpTool("camera_set_background", "Sets a Camera's clear flags (Skybox/SolidColor/Depth/Nothing) and, for SolidColor, the background color.")]
        public static object SetBackground(SetBackgroundParams p)
        {
            var go = GameObject.Find(p.CameraName);
            if (go == null) return new { success = false, error = $"Camera '{p.CameraName}' not found." };
            var cam = go.GetComponent<Camera>();
            if (cam == null) return new { success = false, error = $"'{p.CameraName}' has no Camera component." };

            cam.clearFlags = (CameraClearFlags)System.Enum.Parse(typeof(CameraClearFlags), p.ClearFlags, true);
            if (ColorUtility.TryParseHtmlString("#" + p.ColorHex.TrimStart('#'), out var c)) cam.backgroundColor = c;

            return new { success = true, clear_flags = p.ClearFlags };
        }

        // ── camera_screenshot ────────────────────────────────────────────
        public class ScreenshotParams
        {
            [McpDescription("Camera GameObject name", Required = true)]
            public string CameraName { get; set; }
            [McpDescription("Output PNG path relative to the project root", Required = true)]
            public string OutputPath { get; set; }
            public int Width { get; set; } = 1920;
            public int Height { get; set; } = 1080;
        }

        [McpTool("camera_screenshot", "Renders a Camera's view to a PNG file at a given resolution, independent of the Game view size.")]
        public static object Screenshot(ScreenshotParams p)
        {
            var go = GameObject.Find(p.CameraName);
            if (go == null) return new { success = false, error = $"Camera '{p.CameraName}' not found." };
            var cam = go.GetComponent<Camera>();
            if (cam == null) return new { success = false, error = $"'{p.CameraName}' has no Camera component." };

            var rt = new RenderTexture(p.Width, p.Height, 24);
            cam.targetTexture = rt;
            var tex = new Texture2D(p.Width, p.Height, TextureFormat.RGB24, false);
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, p.Width, p.Height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            var fullPath = Path.Combine(Application.dataPath, "..", p.OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            return new { success = true, path = p.OutputPath, width = p.Width, height = p.Height };
        }
    }

    /// <summary>
    /// Minimal runtime follow behaviour used by camera_set_follow_target.
    /// Intentionally not Cinemachine — keeps the toolbelt dependency-free.
    /// </summary>
    public class QFoldITCameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 3, -8);
        [Range(0.01f, 1f)] public float smoothing = 0.15f;

        private void LateUpdate()
        {
            if (target == null) return;
            var desired = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desired, smoothing);
            transform.LookAt(target);
        }
    }
}
