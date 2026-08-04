// qFoldIT Toolbelt for Unity — AnimationTools.cs
// Category: Animation

using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class AnimationTools
    {
        // ── anim_create_controller ─────────────────────────────────────
        public class CreateControllerParams
        {
            [McpDescription("Output asset path, e.g. Assets/Animations/PlayerController.controller", Required = true)]
            public string OutputPath { get; set; }
        }

        [McpTool("anim_create_controller", "Creates a new empty AnimatorController asset at the given path.")]
        public static object CreateController(CreateControllerParams p)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(p.OutputPath);
            return new { success = controller != null, path = p.OutputPath };
        }

        // ── anim_add_state ─────────────────────────────────────────────
        public class AddStateParams
        {
            [McpDescription("Path to an existing .controller asset", Required = true)]
            public string ControllerPath { get; set; }
            [McpDescription("Name for the new state", Required = true)]
            public string StateName { get; set; }
            [McpDescription("Optional AnimationClip asset path to assign as the state's motion", Default = "")]
            public string ClipPath { get; set; } = "";
        }

        [McpTool("anim_add_state", "Adds a new state to an AnimatorController's base layer, optionally wiring up an AnimationClip.")]
        public static object AddState(AddStateParams p)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(p.ControllerPath);
            if (controller == null) return new { success = false, error = $"No AnimatorController at '{p.ControllerPath}'." };

            var stateMachine = controller.layers[0].stateMachine;
            var state = stateMachine.AddState(p.StateName);

            if (!string.IsNullOrEmpty(p.ClipPath))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(p.ClipPath);
                if (clip != null) state.motion = clip;
            }

            EditorUtility.SetDirty(controller);
            return new { success = true, controller = p.ControllerPath, state = p.StateName };
        }

        // ── anim_add_transition ─────────────────────────────────────────
        public class AddTransitionParams
        {
            [McpDescription("Path to an existing .controller asset", Required = true)]
            public string ControllerPath { get; set; }
            [McpDescription("Source state name", Required = true)]
            public string FromState { get; set; }
            [McpDescription("Destination state name", Required = true)]
            public string ToState { get; set; }
            [McpDescription("Whether the transition has an exit time condition", Default = true)]
            public bool HasExitTime { get; set; } = true;
        }

        [McpTool("anim_add_transition", "Adds a transition between two existing states in an AnimatorController's base layer.")]
        public static object AddTransition(AddTransitionParams p)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(p.ControllerPath);
            if (controller == null) return new { success = false, error = $"No AnimatorController at '{p.ControllerPath}'." };

            var sm = controller.layers[0].stateMachine;
            AnimatorState from = System.Array.Find(sm.states, s => s.state.name == p.FromState).state;
            AnimatorState to = System.Array.Find(sm.states, s => s.state.name == p.ToState).state;
            if (from == null) return new { success = false, error = $"State '{p.FromState}' not found." };
            if (to == null) return new { success = false, error = $"State '{p.ToState}' not found." };

            var transition = from.AddTransition(to);
            transition.hasExitTime = p.HasExitTime;

            EditorUtility.SetDirty(controller);
            return new { success = true, from = p.FromState, to = p.ToState };
        }

        // ── anim_set_parameter ──────────────────────────────────────────
        public class SetParameterParams
        {
            [McpDescription("Path to an existing .controller asset", Required = true)]
            public string ControllerPath { get; set; }
            [McpDescription("Parameter name", Required = true)]
            public string ParamName { get; set; }
            [McpDescription("Parameter type", Required = true, EnumType = typeof(AnimatorControllerParameterType))]
            public string ParamType { get; set; }
        }

        [McpTool("anim_set_parameter", "Adds a Float/Int/Bool/Trigger parameter to an AnimatorController.")]
        public static object SetParameter(SetParameterParams p)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(p.ControllerPath);
            if (controller == null) return new { success = false, error = $"No AnimatorController at '{p.ControllerPath}'." };

            var type = (AnimatorControllerParameterType)System.Enum.Parse(typeof(AnimatorControllerParameterType), p.ParamType, true);
            controller.AddParameter(p.ParamName, type);
            EditorUtility.SetDirty(controller);

            return new { success = true, parameter = p.ParamName, type = type.ToString() };
        }

        // ── anim_attach_controller ──────────────────────────────────────
        public class AttachControllerParams
        {
            [McpDescription("Target GameObject name (an Animator component is added if missing)", Required = true)]
            public string Name { get; set; }
            [McpDescription("Path to the .controller asset to assign", Required = true)]
            public string ControllerPath { get; set; }
        }

        [McpTool("anim_attach_controller", "Adds an Animator component (if needed) to a GameObject and assigns an AnimatorController to it.")]
        public static object AttachController(AttachControllerParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(p.ControllerPath);
            if (controller == null) return new { success = false, error = $"No AnimatorController at '{p.ControllerPath}'." };

            var animator = go.GetComponent<Animator>() ?? Undo.AddComponent<Animator>(go);
            animator.runtimeAnimatorController = controller;

            return new { success = true, name = p.Name, controller = p.ControllerPath };
        }
    }
}
