// qFoldIT Toolbelt for Unity — UtilityTools.cs
// Category: Utility

using System.IO;
using System.Linq;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class UtilityTools
    {
        // ── batch_rename ────────────────────────────────────────────────
        public class BatchRenameParams
        {
            [McpDescription("Substring match on current GameObject names", Required = true)]
            public string NameContains { get; set; }
            [McpDescription("New name prefix; objects become Prefix_000, Prefix_001, ...", Required = true)]
            public string NewPrefix { get; set; }
        }

        [McpTool("batch_rename", "Renames every GameObject in the scene whose name contains a substring to a common prefix with an incrementing index.")]
        public static object BatchRename(BatchRenameParams p)
        {
            var matched = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go.name.ToLowerInvariant().Contains(p.NameContains.ToLowerInvariant()))
                .ToList();

            for (int i = 0; i < matched.Count; i++)
            {
                Undo.RecordObject(matched[i], "qFoldIT: Batch Rename");
                matched[i].name = $"{p.NewPrefix}_{i:D3}";
            }

            return new { success = true, renamed_count = matched.Count };
        }

        // ── editor_screenshot ───────────────────────────────────────────
        public class EditorScreenshotParams
        {
            [McpDescription("Output PNG path relative to the project root", Required = true)]
            public string OutputPath { get; set; }
            [McpDescription("Super-size multiplier for resolution", Default = 1)]
            public int SuperSize { get; set; } = 1;
        }

        [McpTool("editor_screenshot", "Captures the Game view to a PNG file via ScreenCapture.CaptureScreenshot.")]
        public static object EditorScreenshot(EditorScreenshotParams p)
        {
            var fullPath = Path.Combine(Application.dataPath, "..", p.OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
            ScreenCapture.CaptureScreenshot(fullPath, p.SuperSize);

            return new
            {
                success = true,
                path = p.OutputPath,
                note = "Screenshot is captured at the end of the current frame — if calling this outside Play Mode, the Game view must be visible for content to be captured."
            };
        }

        // ── editor_undo ─────────────────────────────────────────────────
        public class EditorUndoParams { }

        [McpTool("editor_undo", "Performs one Editor Undo step.")]
        public static object EditorUndo(EditorUndoParams p)
        {
            Undo.PerformUndo();
            return new { success = true };
        }

        // ── editor_redo ─────────────────────────────────────────────────
        public class EditorRedoParams { }

        [McpTool("editor_redo", "Performs one Editor Redo step.")]
        public static object EditorRedo(EditorRedoParams p)
        {
            Undo.PerformRedo();
            return new { success = true };
        }
    }
}
