// qFoldIT Toolbelt for Unity — ParticleTools.cs
// Category: Particles

using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace QFoldIT.Toolbelt.Editor.Tools
{
    public static class ParticleTools
    {
        public enum ParticlePreset { Fire, Smoke, Explosion, Sparkle, Rain, Snow, Magic }

        // ── particles_apply_preset ─────────────────────────────────────
        public class ApplyPresetParams
        {
            [McpDescription("Name for the new ParticleSystem GameObject", Default = "")]
            public string Name { get; set; } = "";
            [McpDescription("Particle preset", Required = true, EnumType = typeof(ParticlePreset))]
            public string Preset { get; set; }
            public float X { get; set; } = 0f;
            public float Y { get; set; } = 0f;
            public float Z { get; set; } = 0f;
        }

        [McpTool("particles_apply_preset", "Creates a ParticleSystem configured as one of 7 presets: fire, smoke, explosion, sparkle, rain, snow, magic.")]
        public static object ApplyPreset(ApplyPresetParams p)
        {
            var preset = (ParticlePreset)System.Enum.Parse(typeof(ParticlePreset), p.Preset, true);
            var name = string.IsNullOrEmpty(p.Name) ? $"{preset}Particles" : p.Name;
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.position = new Vector3(p.X, p.Y, p.Z);
            var ps = go.GetComponent<ParticleSystem>();
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            var col = ps.colorOverLifetime;

            switch (preset)
            {
                case ParticlePreset.Fire:
                    main.startColor = new Color(1f, 0.4f, 0.05f); main.startSpeed = 2f; main.startLifetime = 1f; main.startSize = 0.5f;
                    emission.rateOverTime = 60f; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 15f;
                    break;
                case ParticlePreset.Smoke:
                    main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.6f); main.startSpeed = 1f; main.startLifetime = 3f; main.startSize = 1.5f;
                    emission.rateOverTime = 20f; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 10f;
                    break;
                case ParticlePreset.Explosion:
                    main.startColor = new Color(1f, 0.6f, 0.1f); main.startSpeed = 8f; main.startLifetime = 0.6f; main.startSize = 0.8f;
                    main.loop = false;
                    var burst = new ParticleSystem.Burst(0f, 80);
                    emission.SetBursts(new[] { burst });
                    emission.rateOverTime = 0f;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    break;
                case ParticlePreset.Sparkle:
                    main.startColor = Color.white; main.startSpeed = 0.5f; main.startLifetime = 1.5f; main.startSize = 0.1f;
                    emission.rateOverTime = 40f; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 1f;
                    break;
                case ParticlePreset.Rain:
                    main.startColor = new Color(0.6f, 0.7f, 1f, 0.5f); main.startSpeed = 15f; main.startLifetime = 1f; main.startSize = 0.05f;
                    main.gravityModifier = 1f;
                    emission.rateOverTime = 200f; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(10, 1, 10);
                    break;
                case ParticlePreset.Snow:
                    main.startColor = Color.white; main.startSpeed = 0.5f; main.startLifetime = 5f; main.startSize = 0.1f;
                    main.gravityModifier = 0.05f;
                    emission.rateOverTime = 30f; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(10, 1, 10);
                    break;
                case ParticlePreset.Magic:
                    main.startColor = new Color(0.6f, 0.2f, 1f); main.startSpeed = 1f; main.startLifetime = 2f; main.startSize = 0.2f;
                    emission.rateOverTime = 30f; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.5f;
                    break;
            }

            Undo.RegisterCreatedObjectUndo(go, "qFoldIT: Apply Particle Preset");
            return new { success = true, name = go.name, preset = preset.ToString() };
        }

        // ── particles_set_emission_rate ────────────────────────────────
        public class SetEmissionRateParams
        {
            [McpDescription("Target GameObject name (must have a ParticleSystem)", Required = true)]
            public string Name { get; set; }
            public float RateOverTime { get; set; } = 10f;
        }

        [McpTool("particles_set_emission_rate", "Sets the emission rate-over-time on an existing ParticleSystem.")]
        public static object SetEmissionRate(SetEmissionRateParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) return new { success = false, error = $"'{p.Name}' has no ParticleSystem." };

            var emission = ps.emission;
            emission.rateOverTime = p.RateOverTime;
            return new { success = true, name = p.Name, rate = p.RateOverTime };
        }

        // ── particles_set_color_over_lifetime ──────────────────────────
        public class SetColorOverLifetimeParams
        {
            [McpDescription("Target GameObject name (must have a ParticleSystem)", Required = true)]
            public string Name { get; set; }
            [McpDescription("Start color as hex", Default = "FFFFFF")]
            public string StartColorHex { get; set; } = "FFFFFF";
            [McpDescription("End color as hex", Default = "FFFFFF00")]
            public string EndColorHex { get; set; } = "FFFFFF00";
        }

        [McpTool("particles_set_color_over_lifetime", "Sets a two-stop color-over-lifetime gradient on an existing ParticleSystem (e.g. fade to transparent).")]
        public static object SetColorOverLifetime(SetColorOverLifetimeParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) return new { success = false, error = $"'{p.Name}' has no ParticleSystem." };

            Color ParseOrWhite(string hex)
            {
                return ColorUtility.TryParseHtmlString("#" + hex.TrimStart('#'), out var c) ? c : Color.white;
            }

            var startC = ParseOrWhite(p.StartColorHex);
            var endC = ParseOrWhite(p.EndColorHex);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(startC, 0f), new GradientColorKey(endC, 1f) },
                new[] { new GradientAlphaKey(startC.a, 0f), new GradientAlphaKey(endC.a, 1f) });
            col.color = gradient;

            return new { success = true, name = p.Name };
        }

        // ── particles_burst ─────────────────────────────────────────────
        public class BurstParams
        {
            [McpDescription("Target GameObject name (must have a ParticleSystem)", Required = true)]
            public string Name { get; set; }
            public int Count { get; set; } = 30;
        }

        [McpTool("particles_burst", "Emits an immediate one-time burst of particles from an existing ParticleSystem (Editor Play Mode or Simulate).")]
        public static object Burst(BurstParams p)
        {
            var go = GameObject.Find(p.Name);
            if (go == null) return new { success = false, error = $"Object '{p.Name}' not found." };
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) return new { success = false, error = $"'{p.Name}' has no ParticleSystem." };

            ps.Emit(p.Count);
            return new { success = true, name = p.Name, emitted = p.Count };
        }
    }
}
