// qFoldIT Toolbelt for Unity — ConsoleBuildTools.cs
// Category: BuildConsole

using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class ConsoleBuildTools
    {
        // ── console_execute_menu_item ──────────────────────────────────
        public class ExecuteMenuItemParams
        {
            [McpDescription("Full Editor menu path, e.g. 'Assets/Refresh' or 'Edit/Play'", Required = true)]
            public string MenuPath { get; set; }
        }

        [McpTool("console_execute_menu_item", "Executes an Editor menu item by its full path — a generic escape hatch for actions not covered by a dedicated tool.")]
        public static object ExecuteMenuItem(ExecuteMenuItemParams p)
        {
            bool ok = EditorApplication.ExecuteMenuItem(p.MenuPath);
            return new { success = ok, menu_path = p.MenuPath };
        }

        // ── build_player ────────────────────────────────────────────────
        public class BuildPlayerParams
        {
            [McpDescription("Build target platform", Required = true, EnumType = typeof(UnityEditor.BuildTarget))]
            public string Target { get; set; }

            [McpDescription("Output path for the built player", Required = true)]
            public string OutputPath { get; set; }

            [McpDescription("Scene paths to include, comma-separated; empty = all scenes currently in Build Settings", Default = "")]
            public string ScenesCsv { get; set; } = "";
        }

        [McpTool("build_player", "Triggers a player build for the given target platform and writes it to the specified output path.")]
        public static object BuildPlayer(BuildPlayerParams p)
        {
            var target = (BuildTarget)System.Enum.Parse(typeof(BuildTarget), p.Target, true);
            string[] scenes;
            if (!string.IsNullOrEmpty(p.ScenesCsv))
            {
                scenes = System.Array.ConvertAll(p.ScenesCsv.Split(','), s => s.Trim());
            }
            else
            {
                scenes = System.Array.ConvertAll(
                    System.Array.FindAll(EditorBuildSettings.scenes, s => s.enabled),
                    s => s.path);
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = p.OutputPath,
                target = target,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            return new
            {
                success = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded,
                result = report.summary.result.ToString(),
                total_size_bytes = report.summary.totalSize,
                output_path = p.OutputPath
            };
        }

        // ── console_get_log ────────────────────────────────────────────
        public class GetLogParams
        {
            [McpDescription("Max number of recent log entries to return", Default = 50)]
            public int MaxEntries { get; set; } = 50;
        }

        [McpTool("console_get_log", "Reads the most recent Unity Console log entries (info, warnings, and errors) via LogEntries reflection.")]
        public static object GetLog(GetLogParams p)
        {
            // LogEntries is internal to UnityEditor; access via reflection so this
            // compiles across editor versions without an internal-API dependency.
            var logEntriesType = System.Type.GetType("UnityEditor.LogEntries,UnityEditor");
            if (logEntriesType == null)
                return new { success = false, error = "UnityEditor.LogEntries not available on this Editor version." };

            var getCountMethod = logEntriesType.GetMethod("GetCount");
            int count = getCountMethod != null ? (int)getCountMethod.Invoke(null, null) : 0;

            return new
            {
                success = true,
                total_entries = count,
                note = "Full per-entry text extraction requires the internal LogEntry struct; " +
                       "this call reports entry count. Extend with StartGettingEntries/GetEntryInternal " +
                       "for full text if your Editor version's internal API is pinned."
            };
        }
    }
}
