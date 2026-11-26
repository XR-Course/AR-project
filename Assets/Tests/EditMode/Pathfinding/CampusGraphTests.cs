using System;
using NUnit.Framework;
using UnityEngine;

namespace CampusNavigation.Pathfinding.Tests
{
    public class CampusGraphTests
    {
        [Test]
        public void AddEdge_WithoutDistance_UsesStraightLineDistance()
        {
            var graph = new CampusGraph();
            graph.AddNode(new CampusNode("a", new Vector3(0, 0, 0)));
            graph.AddNode(new CampusNode("b", new Vector3(3, 0, 4)));

            graph.AddEdge("a", "b");

            Assert.AreEqual(5f, graph.GetNeighbors("a")[0].Distance, 0.001f);
        }

        [Test]
        public void AddEdge_IsTwoWay()
        {
            var graph = new CampusGraph();
            graph.AddNode(new CampusNode("a", Vector3.zero));
            graph.AddNode(new CampusNode("b", Vector3.one));

            graph.AddEdge("a", "b");

            Assert.AreEqual("b", graph.GetNeighbors("a")[0].NodeId);
            Assert.AreEqual("a", graph.GetNeighbors("b")[0].NodeId);
        }

        [Test]
        public void AddNode_DuplicateId_Throws()
        {
            var graph = new CampusGraph();
            graph.AddNode(new CampusNode("a", Vector3.zero));

            Assert.Throws<ArgumentException>(() => graph.AddNode(new CampusNode("a", Vector3.one)));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        public void AddNode_EmptyId_Throws(string id)
        {
            var graph = new CampusGraph();

            Assert.Throws<ArgumentException>(() => graph.AddNode(new CampusNode(id, Vector3.zero)));
        }

        [Test]
        public void AddEdge_UnknownNode_Throws()
        {
            var graph = new CampusGraph();
            graph.AddNode(new CampusNode("a", Vector3.zero));

            Assert.Throws<ArgumentException>(() => graph.AddEdge("a", "missing"));
        }

        [Test]
        public void AddEdge_SelfLoop_Throws()
        {
            var graph = new CampusGraph();
            graph.AddNode(new CampusNode("a", Vector3.zero));

            Assert.Throws<ArgumentException>(() => graph.AddEdge("a", "a"));
        }

        [Test]
        public void TryGetNodeByMarker_FindsMappedNode()
        {
            CampusGraph graph = MockCampusMap.Create();

            Assert.IsTrue(graph.TryGetNodeByMarker("HallwayCheckpoint", out CampusNode node));
            Assert.AreEqual(MockCampusMap.JunctionA, node.id);
        }

        [Test]
        public void Build_FromSerializedData_UsesDistanceOverride()
        {
            CampusGraph graph = CampusGraph.Build(
                new[] { new CampusNode("a", Vector3.zero), new CampusNode("b", new Vector3(1, 0, 0)) },
                new[] { new CampusEdge("a", "b", 7f) });

            Assert.AreEqual(2, graph.NodeCount);
            Assert.AreEqual(1, graph.Edges.Count);
            Assert.AreEqual(7f, graph.GetNeighbors("a")[0].Distance, 0.001f);
        }

        [Test]
        public void Build_EdgeToMissingNode_Throws()
        {
            Assert.Throws<ArgumentException>(() => CampusGraph.Build(
                new[] { new CampusNode("a", Vector3.zero) },
                new[] { new CampusEdge("a", "missing") }));
        }

        [Test]
        public void AddNode_Null_Throws()
        {
            Assert.Throws<ArgumentException>(() => new CampusGraph().AddNode(null));
        }

        [Test]
        public void TryFindNearestEdgePoint_ProjectsOntoCorridor()
        {
            CampusGraph graph = MockCampusMap.Create();

            Assert.IsTrue(graph.TryFindNearestEdgePoint(new Vector3(10.4f, 1.5f, 3f), out CampusGraph.EdgePoint hit));
            Assert.AreEqual(MockCampusMap.JunctionA, hit.Edge.From.id);
            Assert.AreEqual(MockCampusMap.XrLab, hit.Edge.To.id);
            Assert.Less(Vector3.Distance(new Vector3(10, 0, 3), hit.Point), 0.001f);
            Assert.AreEqual(3f, hit.DistanceToFrom, 0.001f);
            Assert.AreEqual(5f, hit.DistanceToTo, 0.001f);
            Assert.AreEqual(0.4f, hit.HorizontalOffset, 0.001f);
        }

        [Test]
        public void TryFindNearestEdgePoint_NoEdges_ReturnsFalse()
        {
            var graph = new CampusGraph();
            graph.AddNode(new CampusNode("a", Vector3.zero));

            Assert.IsFalse(graph.TryFindNearestEdgePoint(Vector3.zero, out _));
        }

        [Test]
        public void GetNeighbors_UnknownNode_ReturnsEmpty()
        {
            CampusGraph graph = MockCampusMap.Create();

            Assert.IsEmpty(graph.GetNeighbors("missing"));
        }
    }
}
