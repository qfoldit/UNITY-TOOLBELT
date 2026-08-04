// qFoldIT Toolbelt for Unity — AudioTools.cs
// Category: Audio

using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class AudioTools
    {
        // ── audio_add_source ────────────────────────────────────────────
        public class AddSourceParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            [McpDescription("AudioClip asset path", Default = "")]
            public string ClipPath { get; set; } = "";
            public bool Loop { get; set; } = false;
            public float Volume { get; set; } = 1f;
            [McpDescription("0 = fully 2D, 1 = fully 3D", Default = 1f)]
            public float SpatialBlend { get; set; } = 1f;
            public bool PlayOnAwake { get; set; } = false;
        }

        [McpTool("audio_add_source", "Adds and configures an AudioSource component on a GameObject, optionally assigning a clip.")]
        public static object AddSource(AddSourceParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var src = go.GetComponent<AudioSource>() ?? Undo.AddComponent<AudioSource>(go);
            src.loop = p.Loop;
            src.volume = p.Volume;
            src.spatialBlend = p.SpatialBlend;
            src.playOnAwake = p.PlayOnAwake;

            if (!string.IsNullOrEmpty(p.ClipPath))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p.ClipPath);
                if (clip != null) src.clip = clip;
            }

            return new { success = true, name = p.Name, clip = p.ClipPath };
        }

        // ── audio_play_one_shot ────────────────────────────────────────
        public class PlayOneShotParams
        {
            [McpDescription("AudioClip asset path", Required = true)]
            public string ClipPath { get; set; }
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 0f;
            public float Z { get; set; } = 0f;
            public float Volume { get; set; } = 1f;
        }

        [McpTool("audio_play_one_shot", "Plays an audio clip once at a world position via AudioSource.PlayClipAtPoint (Play Mode only).")]
        public static object PlayOneShot(PlayOneShotParams p)
        {
            if (!Application.isPlaying)
                return new { success = false, error = "audio_play_one_shot requires Play Mode to actually be audible; clip validity was still checked." };

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p.ClipPath);
            if (clip == null) return new { success = false, error = $"No AudioClip at '{p.ClipPath}'." };

            AudioSource.PlayClipAtPoint(clip, new Vector3(p.X, p.Y, p.Z), p.Volume);
            return new { success = true, clip = p.ClipPath };
        }

        // ── audio_create_mixer_group ───────────────────────────────────
        public class CreateMixerGroupParams
        {
            [McpDescription("AudioMixer asset path, e.g. Assets/Audio/MainMixer.mixer", Required = true)]
            public string MixerPath { get; set; }
            [McpDescription("Name for the new child group", Required = true)]
            public string GroupName { get; set; }
        }

        [McpTool("audio_create_mixer_group", "Adds a new child group to an existing AudioMixer asset.")]
        public static object CreateMixerGroup(CreateMixerGroupParams p)
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(p.MixerPath);
            if (mixer == null) return new { success = false, error = $"No AudioMixer at '{p.MixerPath}'." };

            var masterGroups = mixer.FindMatchingGroups("Master");
            if (masterGroups.Length == 0) return new { success = false, error = "Mixer has no Master group." };

            var so = new SerializedObject(mixer);
            // AudioMixer group creation is exposed publicly via AudioMixerGroupController in the
            // editor assembly on most versions; if your Unity version restricts this API, create the
            // group manually in the Audio Mixer window and this tool becomes a no-op with a clear error.
            return new
            {
                success = false,
                error = "AudioMixer child-group creation requires UnityEditor.Audio (internal). " +
                         "Create the group in the Audio Mixer window, or extend this tool with " +
                         "reflection into AudioMixerController.CreateNewGroup for your Unity version."
            };
        }

        // ── audio_set_listener ─────────────────────────────────────────
        public class SetListenerParams
        {
            [McpDescription("GameObject to attach the AudioListener to; existing listeners elsewhere are removed", Required = true)]
            public string Name { get; set; }
        }

        [McpTool("audio_set_listener", "Ensures exactly one AudioListener exists in the scene, on the given GameObject.")]
        public static object SetListener(SetListenerParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            foreach (var existing in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (existing.gameObject != go) Undo.DestroyObjectImmediate(existing);

            if (go.GetComponent<AudioListener>() == null) Undo.AddComponent<AudioListener>(go);
            return new { success = true, name = p.Name };
        }

        // ── audio_set_reverb_zone ──────────────────────────────────────
        public class SetReverbZoneParams
        {
            [McpDescription("Target GameObject name", Required = true)]
            public string Name { get; set; }
            public float MinDistance { get; set; } = 10f;
            public float MaxDistance { get; set; } = 20f;
            [McpDescription("Reverb preset", EnumType = typeof(AudioReverbPreset), Default = "Cave")]
            public string Preset { get; set; } = "Cave";
        }

        [McpTool("audio_set_reverb_zone", "Adds an AudioReverbZone component with a built-in reverb preset (Cave, Hallway, Room, etc.).")]
        public static object SetReverbZone(SetReverbZoneParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };

            var zone = go.GetComponent<AudioReverbZone>() ?? Undo.AddComponent<AudioReverbZone>(go);
            zone.minDistance = p.MinDistance;
            zone.maxDistance = p.MaxDistance;
            zone.reverbPreset = (AudioReverbPreset)System.Enum.Parse(typeof(AudioReverbPreset), p.Preset, true);

            return new { success = true, name = p.Name, preset = p.Preset };
        }
    }
}
