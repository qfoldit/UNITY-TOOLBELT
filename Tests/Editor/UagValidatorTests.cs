using System.Linq;
using NUnit.Framework;
using QFoldIT.Toolbelt.Editor.Core;
using QFoldIT.Toolbelt.Editor.Uag;

namespace QFoldIT.Toolbelt.Tests
{
    /// <summary>
    /// UagValidator has zero UnityEngine/UnityEditor dependencies by design,
    /// so these cases were additionally verified standalone with
    /// `mcs`/`mono` — including against the REAL conformance/test_vectors.json
    /// from qfoldit-engine-adapter-spec-v0.1 — before being committed here.
    /// See tests/conformance/ for that standalone harness and its own README.
    /// </summary>
    public class UagValidatorTests
    {
        private static UagGraph Graph(params UagNode[] nodes) => new UagGraph
        {
            Schema = UagGraph.SupportedSchema,
            Scene = new UagScene { Id = "test-scene" },
            Nodes = nodes.ToList()
        };

        private static UagNode Node(string id, string type, string parent = null) =>
            new UagNode { Id = id, Type = type, Parent = parent };

        [Test]
        public void ValidGraph_AllTypesMapped_IsValidWithNoGaps()
        {
            var g = Graph(Node("root", "group"), Node("cube1", "mesh", "root"), Node("sun", "light", "root"));
            var r = UagValidator.Validate(g);
            Assert.IsTrue(r.IsValid);
            Assert.IsEmpty(r.Errors);
            Assert.IsEmpty(r.UnmappedNodeTypes);
        }

        [Test]
        public void WrongSchema_IsInvalidWithCode()
        {
            var g = Graph();
            g.Schema = "qfoldit.uag/9.9";
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Code == "INVALID_SCHEMA"));
        }

        [Test]
        public void DanglingParent_IsInvalidWithCode()
        {
            var g = Graph(Node("a", "mesh", "ghost"));
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Code == "DANGLING_PARENT" && e.Message.Contains("ghost")));
        }

        [Test]
        public void TwoNodeCycle_IsDetectedWithCode()
        {
            var g = Graph(Node("a", "mesh", "b"), Node("b", "mesh", "a"));
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Code == "HIERARCHY_CYCLE"));
        }

        [Test]
        public void SelfReferentialParent_IsDetectedAsCycle()
        {
            var g = Graph(Node("a", "mesh", "a"));
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Code == "HIERARCHY_CYCLE"));
        }

        [Test]
        public void ThreeNodeCycle_IsDetected()
        {
            var g = Graph(Node("a", "mesh", "b"), Node("b", "mesh", "c"), Node("c", "mesh", "a"));
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Code == "HIERARCHY_CYCLE"));
        }

        [Test]
        public void LongNonCyclicChain_IsNotFlaggedAsCycle()
        {
            var g = Graph(Node("a", "mesh"), Node("b", "mesh", "a"), Node("c", "mesh", "b"), Node("d", "mesh", "c"), Node("e", "mesh", "d"));
            var r = UagValidator.Validate(g);
            Assert.IsTrue(r.IsValid, "A long valid parent chain must not be flagged as a cycle.");
        }

        [Test]
        public void UnknownNodeType_IsGapNotError()
        {
            var g = Graph(Node("a", "quantum_circuit"));
            var r = UagValidator.Validate(g);
            Assert.IsTrue(r.IsValid, "Unmapped types are gaps, not validation errors.");
            Assert.Contains("quantum_circuit", r.UnmappedNodeTypes);
        }

        [Test]
        public void ScientificSubjectAndMechanicInteraction_AreNotGaps()
        {
            var g = Graph(Node("subject", "scientific_subject/construction"));
            g.Interactions.Add(new UagInteraction { Id = "i1", Type = "construction", Target = "subject" });
            var r = UagValidator.Validate(g);
            Assert.IsTrue(r.IsValid);
            Assert.IsEmpty(r.UnmappedNodeTypes, "scientific_subject/<mechanic> must be recognized — this is exactly what reference/compiler.py emits.");
            Assert.IsEmpty(r.UnmappedInteractions, "The 10 gameplay mechanic names must be recognized as valid interaction types.");
        }

        [Test]
        public void DuplicateNodeIds_IsInvalidWithCode()
        {
            var g = Graph(Node("a", "mesh"), Node("a", "light"));
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Code == "DUPLICATE_NODE_ID"));
        }

        [Test]
        public void DanglingBindingTarget_IsInvalid()
        {
            var g = Graph(Node("a", "mesh"));
            g.Bindings.Add(new UagBinding { Id = "b1", Source = "scientific-state://x", Target = "ghost" });
            var r = UagValidator.Validate(g);
            Assert.IsFalse(r.IsValid);
            Assert.IsTrue(r.Errors.Any(e => e.Code == "DANGLING_REFERENCE" && e.Message.Contains("ghost")));
        }
    }
}
