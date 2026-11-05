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
        public void GetNeighbors_UnknownNode_ReturnsEmpty()
        {
            CampusGraph graph = MockCampusMap.Create();

            Assert.IsEmpty(graph.GetNeighbors("missing"));
        }
    }
}
