// qFoldIT Toolbelt for Unity — UagModel.cs
// Data model for qFoldIT's Universal Assembly Graph (UAG) v0.1.
// Field names and shape are copied 1:1 from the canonical schema at
// qfoldit/UEFN-TOOLBELT: .claude/skills/game-designer/references/uag_schema.md
// so a UAG document produced by game-designer (or by hand) deserializes
// here without any translation layer of its own.
//
// Deliberately NOT using Unity's JsonUtility: UAG's "properties" field is a
// free-form object whose shape depends on node type, and JsonUtility can't
// deserialize into Dictionary<string,object> / arbitrary nested objects.
// Newtonsoft.Json (com.unity.nuget.newtonsoft-json) handles this cleanly
// via JObject/JToken.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QFoldIT.Toolbelt.Editor.Uag
{
    public class UagTransform
    {
        [JsonProperty("position")] public float[] Position { get; set; } = { 0, 0, 0 };
        [JsonProperty("rotation_euler_deg")] public float[] RotationEulerDeg { get; set; } = { 0, 0, 0 };
        [JsonProperty("scale")] public float[] Scale { get; set; } = { 1, 1, 1 };
    }

    public class UagNode
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("type")] public string Type { get; set; } // mesh|light|camera|trigger_volume|ui_panel|particle_emitter|audio_source|group|custom
        [JsonProperty("transform")] public UagTransform Transform { get; set; } = new UagTransform();
        [JsonProperty("properties")] public JObject Properties { get; set; } = new JObject();
        [JsonProperty("parent_id")] public string ParentId { get; set; }
    }

    public class UagConnection
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("type")] public string Type { get; set; } // parent_child|joint_fixed|joint_hinge|joint_slider|data_link
        [JsonProperty("from_node")] public string FromNode { get; set; }
        [JsonProperty("to_node")] public string ToNode { get; set; }
        [JsonProperty("properties")] public JObject Properties { get; set; } = new JObject();
    }

    public class UagConstraint
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("type")] public string Type { get; set; } // physics_collision|interaction_grabbable|animation_trigger|logic_rule
        [JsonProperty("target_nodes")] public List<string> TargetNodes { get; set; } = new List<string>();
        [JsonProperty("properties")] public JObject Properties { get; set; } = new JObject();
    }

    public class UagInteraction
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("trigger")] public string Trigger { get; set; } // on_grab|on_proximity|on_gaze|on_click|on_timer
        [JsonProperty("target_node")] public string TargetNode { get; set; }
        [JsonProperty("action")] public string Action { get; set; } // engine-agnostic free text
    }

    public class UagMetadata
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("source_context")] public string SourceContext { get; set; }
    }

    public class UagGraph
    {
        [JsonProperty("uag_version")] public string UagVersion { get; set; }
        [JsonProperty("metadata")] public UagMetadata Metadata { get; set; } = new UagMetadata();
        [JsonProperty("nodes")] public List<UagNode> Nodes { get; set; } = new List<UagNode>();
        [JsonProperty("connections")] public List<UagConnection> Connections { get; set; } = new List<UagConnection>();
        [JsonProperty("constraints")] public List<UagConstraint> Constraints { get; set; } = new List<UagConstraint>();
        [JsonProperty("interactions")] public List<UagInteraction> Interactions { get; set; } = new List<UagInteraction>();

        public static UagGraph Parse(string json) =>
            JsonConvert.DeserializeObject<UagGraph>(json) ?? new UagGraph();
    }
}
