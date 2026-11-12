using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CampusNavigation.Pathfinding.Tests
{
    public class PathFromPositionTests
    {
        private const float Tolerance = 0.001f;

        private PathfindingService service;

        [SetUp]
        public void SetUp()
        {
            service = new PathfindingService(MockCampusMap.Create());
        }

        private static void AssertWaypoints(Vector3[] expected, PathResult result)
        {
            Assert.AreEqual(expected.Length, result.Waypoints.Count, "Waypoint count");

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Less(
                    Vector3.Distance(expected[i], result.Waypoints[i]),
                    Tolerance,
                    $"Waypoint {i}: expected {expected[i]}, got {result.Waypoints[i]}");
            }
        }

        [Test]
        public void FromPosition_BetweenNodes_WalksForwardToNearestUsefulNode()
        {
            // Standing 4 m into the first corridor (camera height 1.5 m), slightly off-center.
            PathResult result = service.FindPathFromPosition(new Vector3(4, 1.5f, 0.3f), MockCampusMap.XrLab);

            Assert.IsTrue(result.Success);
            Assert.IsTrue(result.HasEntryPoint);
            CollectionAssert.AreEqual(
                new[] { MockCampusMap.JunctionA, MockCampusMap.XrLab },
                result.NodeIds.ToArray());
            AssertWaypoints(
                new[] { new Vector3(4, 0, 0), new Vector3(10, 0, 0), new Vector3(10, 0, 8) },
                result);
            Assert.AreEqual(0.3f + 6f + 8f, result.TotalDistance, Tolerance);
        }

        [Test]
        public void FromPosition_PastTurn_WalksBackToTurn()
        {
            // Walked 5 m past junction-a, but the XR Lab is back at junction-a.
            PathResult result = service.FindPathFromPosition(new Vector3(15, 0, 0.5f), MockCampusMap.XrLab);

            CollectionAssert.AreEqual(
                new[] { MockCampusMap.JunctionA, MockCampusMap.XrLab },
                result.NodeIds.ToArray());
            AssertWaypoints(
                new[] { new Vector3(15, 0, 0), new Vector3(10, 0, 0), new Vector3(10, 0, 8) },
                result);
            Assert.AreEqual(0.5f + 5f + 8f, result.TotalDistance, Tolerance);
        }

        [Test]
        public void FromPosition_NearJunctionB_ContinuesForward()
        {
            PathResult result = service.FindPathFromPosition(new Vector3(18, 0, 0), MockCampusMap.Classroom2);

            CollectionAssert.AreEqual(
                new[] { MockCampusMap.JunctionB, MockCampusMap.Classroom2 },
                result.NodeIds.ToArray());
            Assert.AreEqual(12f, result.TotalDistance, Tolerance);
        }

        [Test]
        public void FromPosition_ExactlyOnNode_HasNoEntryPoint()
        {
            PathResult result = service.FindPathFromPosition(new Vector3(10, 1.6f, 0), MockCampusMap.Reception);

            Assert.IsTrue(result.Success);
            Assert.IsFalse(result.HasEntryPoint);
            CollectionAssert.AreEqual(
                new[] { MockCampusMap.JunctionA, MockCampusMap.JunctionB, MockCampusMap.Reception },
                result.NodeIds.ToArray());
            Assert.AreEqual(result.NodeIds.Count, result.Waypoints.Count);
            Assert.AreEqual(15f, result.TotalDistance, Tolerance);
        }

        [Test]
        public void FromPosition_UsesEdgeDistanceOverride()
        {
            var graph = new CampusGraph();
            graph.AddNode(new CampusNode("a", new Vector3(0, 0, 0)));
            graph.AddNode(new CampusNode("b", new Vector3(10, 0, 0)));
            graph.AddEdge("a", "b", 20f);

            PathResult result = new PathfindingService(graph).FindPathFromPosition(new Vector3(5, 0, 0), "b");

            Assert.AreEqual(10f, result.TotalDistance, Tolerance);
        }

        [Test]
        public void FromPosition_UnknownDestination_ReturnsDestinationNotFound()
        {
            PathResult result = service.FindPathFromPosition(Vector3.zero, "does-not-exist");

            Assert.AreEqual(PathStatus.DestinationNotFound, result.Status);
        }

        [Test]
        public void FromPosition_DisconnectedDestination_ReturnsUnreachable()
        {
            PathResult result = service.FindPathFromPosition(new Vector3(4, 0, 0), MockCampusMap.Storage);

            Assert.AreEqual(PathStatus.Unreachable, result.Status);
            Assert.IsEmpty(result.Waypoints);
        }

        [Test]
        public void FromPosition_EmptyGraph_ReturnsStartNotFound()
        {
            PathResult result = new PathfindingService(new CampusGraph())
                .FindPathFromPosition(Vector3.zero, "anything");

            Assert.AreEqual(PathStatus.StartNotFound, result.Status);
        }

        [Test]
        public void FromPosition_GraphWithoutEdges_UsesNearestNode()
        {
            var graph = new CampusGraph();
            graph.AddNode(new CampusNode("a", new Vector3(0, 0, 0)));
            graph.AddNode(new CampusNode("b", new Vector3(5, 0, 0)));
            var noEdges = new PathfindingService(graph);

            PathResult toA = noEdges.FindPathFromPosition(new Vector3(0.2f, 0, 0), "a");
            PathResult toB = noEdges.FindPathFromPosition(new Vector3(0.2f, 0, 0), "b");

            Assert.IsTrue(toA.Success);
            Assert.AreEqual(0.2f, toA.TotalDistance, Tolerance);
            Assert.AreEqual(PathStatus.Unreachable, toB.Status);
        }

        [Test]
        public void FindNearestNode_ReturnsClosestNode()
        {
            CampusNode node = service.FindNearestNode(new Vector3(9, 1.7f, 1));

            Assert.AreEqual(MockCampusMap.JunctionA, node.id);
        }

        [Test]
        public void FindNearestNode_IgnoresHeight()
        {
            var graph = new CampusGraph();
            graph.AddNode(new CampusNode("a", new Vector3(0, 0, 0)));
            graph.AddNode(new CampusNode("b", new Vector3(3, 5, 0)));

            // In 3D "a" is closer, but on the floor plane "b" is.
            CampusNode node = graph.FindNearestNode(new Vector3(2, 0, 0));

            Assert.AreEqual("b", node.id);
        }

        [Test]
        public void FindNearestNode_EmptyGraph_ReturnsNull()
        {
            Assert.IsNull(new CampusGraph().FindNearestNode(Vector3.zero));
        }
    }
}
