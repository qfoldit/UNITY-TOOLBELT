using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QFoldIT.Toolbelt.Editor.Core;
using QFoldIT.Toolbelt.Editor.Uag;

namespace QFoldIT.Toolbelt.Tests
{
    /// <summary>
    /// UagValidator has zero UnityEngine/UnityEditor dependencies by design
    /// (see the comment at the top of UagValidator.cs), so these cases were
    /// additionally verified standalone with `mcs`/`mono` outside the Editor
    /// before being committed here — this file is the permanent, in-repo
    /// version of that same verification.
    /// </summary>
    public class UagValidatorTests
    {
        private static UagNode Node(string id, string type, string parentId = null) =>
            new UagNode { Id = id, Type = type, ParentId = parentId };

        [Test]
        public void ValidGraph_AllTypesMapped_IsValidWithNoGaps()
        {
            var g = new UagGraph
            {
                Nodes = new List<UagNode> { Node("root", "group"), Node("cube1", "mesh", "root"), Node("sun", "light", "root") },
                Connections = new List<UagConnection> { new UagConnection { Id = "c1", Type = "parent_child", FromNode = "cube1", ToNode = "root" } },
                Constraints = new List<UagConstraint> { new UagConstraint { Id = "k1", Type = "physics_collision", TargetNodes = new List<string> { "cube1" } } },
            };
            var r = UagValidator.Validate(g);
            Assert.IsTrue(r.IsValid);
            Assert.IsEmpty(r.Errors);
            Assert.IsEmpty(r.UnmappedNodeTypes);
            Assert.IsEmpty(r.UnmappedConstraintTypes);
        }

        [Test]
        public void DanglingParentId_IsInvalid()
        {
            var g = new UagGraph { Nodes = new List<UagNode> { Node("a", "mesh", "ghost") } };
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("ghost")));
        }

        [Test]
        public void DanglingConnectionReference_IsInvalid()
        {
            var g = new UagGraph
            {
                Nodes = new List<UagNode> { Node("a", "mesh") },
                Connections = new List<UagConnection> { new UagConnection { Id = "c1", Type = "parent_child", FromNode = "a", ToNode = "missing" } }
            };
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("missing")));
        }

        [Test]
        public void TwoNodeCycle_IsDetected()
        {
            var g = new UagGraph { Nodes = new List<UagNode> { Node("a", "mesh", "b"), Node("b", "mesh", "a") } };
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("Cycle")));
        }

        [Test]
        public void SelfReferentialParent_IsDetectedAsCycle()
        {
            var g = new UagGraph { Nodes = new List<UagNode> { Node("a", "mesh", "a") } };
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("Cycle")));
        }

        [Test]
        public void ThreeNodeCycle_IsDetected()
        {
            var g = new UagGraph { Nodes = new List<UagNode> { Node("a", "mesh", "b"), Node("b", "mesh", "c"), Node("c", "mesh", "a") } };
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("Cycle")));
        }

        [Test]
        public void LongNonCyclicChain_IsNotFlaggedAsCycle()
        {
            var g = new UagGraph
            {
                Nodes = new List<UagNode> { Node("a", "mesh"), Node("b", "mesh", "a"), Node("c", "mesh", "b"), Node("d", "mesh", "c"), Node("e", "mesh", "d") }
            };
            var r = UagValidator.Validate(g);
            Assert.IsTrue(r.IsValid, "A long valid parent chain must not be flagged as a cycle.");
        }

        [Test]
        public void UnmappedTypes_AreGapsNotErrors()
        {
            var g = new UagGraph
            {
                Nodes = new List<UagNode> { Node("a", "custom") },
                Constraints = new List<UagConstraint> { new UagConstraint { Id = "k1", Type = "logic_rule", TargetNodes = new List<string> { "a" } } },
                Interactions = new List<UagInteraction> { new UagInteraction { Id = "i1", Trigger = "on_click", TargetNode = "a", Action = "toggle_light" } }
            };
            var r = UagValidator.Validate(g);
            Assert.IsTrue(r.IsValid, "Unmapped types are gaps, not validation errors.");
            Assert.Contains("custom", r.UnmappedNodeTypes);
            Assert.Contains("logic_rule", r.UnmappedConstraintTypes);
            Assert.AreEqual(1, r.UnmappedInteractions.Count);
        }

        [Test]
        public void DuplicateNodeIds_IsInvalid()
        {
            var g = new UagGraph { Nodes = new List<UagNode> { Node("a", "mesh"), Node("a", "light") } };
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("Duplicate")));
        }

        [Test]
        public void DanglingConstraintAndInteractionTargets_AreInvalid()
        {
            var g = new UagGraph
            {
                Nodes = new List<UagNode> { Node("a", "mesh") },
                Constraints = new List<UagConstraint> { new UagConstraint { Id = "k1", Type = "physics_collision", TargetNodes = new List<string> { "a", "ghost" } } },
                Interactions = new List<UagInteraction> { new UagInteraction { Id = "i1", Trigger = "on_grab", TargetNode = "ghost2", Action = "x" } }
            };
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("ghost") && e.Contains("Constraint")));
            Assert.IsTrue(r.Errors.Any(e => e.Contains("ghost2")));
        }
    }
}
