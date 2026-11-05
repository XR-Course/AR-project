using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CampusNavigation.Pathfinding.Tests
{
    public class PathfindingServiceTests
    {
        private PathfindingService service;

        [SetUp]
        public void SetUp()
        {
            service = new PathfindingService(MockCampusMap.Create());
        }

        [Test]
        public void FindPath_EntranceToXrLab_ReturnsShortestRoute()
        {
            PathResult result = service.FindPath(MockCampusMap.Entrance, MockCampusMap.XrLab);

            Assert.AreEqual(PathStatus.Success, result.Status);
            CollectionAssert.AreEqual(
                new[] { MockCampusMap.Entrance, MockCampusMap.JunctionA, MockCampusMap.XrLab },
                result.NodeIds.ToArray());
            Assert.AreEqual(18f, result.TotalDistance, 0.001f);
        }

        [Test]
        public void FindPath_EntranceToClassroom_PassesBothJunctions()
        {
            PathResult result = service.FindPath(MockCampusMap.Entrance, MockCampusMap.Classroom2);

            Assert.IsTrue(result.Success);
            CollectionAssert.AreEqual(
                new[]
                {
                    MockCampusMap.Entrance, MockCampusMap.JunctionA,
                    MockCampusMap.JunctionB, MockCampusMap.Classroom2
                },
                result.NodeIds.ToArray());
            Assert.AreEqual(30f, result.TotalDistance, 0.001f);
        }

        [Test]
        public void FindPath_WaypointsMatchNodePositions()
        {
            PathResult result = service.FindPath(MockCampusMap.Entrance, MockCampusMap.Reception);

            CollectionAssert.AreEqual(
                new[]
                {
                    new Vector3(0, 0, 0), new Vector3(10, 0, 0),
                    new Vector3(20, 0, 0), new Vector3(20, 0, -5)
                },
                result.Waypoints.ToArray());
        }

        [Test]
        public void FindPath_IsSymmetric()
        {
            PathResult there = service.FindPath(MockCampusMap.Entrance, MockCampusMap.Reception);
            PathResult back = service.FindPath(MockCampusMap.Reception, MockCampusMap.Entrance);

            CollectionAssert.AreEqual(there.NodeIds.Reverse().ToArray(), back.NodeIds.ToArray());
            Assert.AreEqual(there.TotalDistance, back.TotalDistance, 0.001f);
        }

        [Test]
        public void FindPath_StartEqualsDestination_ReturnsSingleNode()
        {
            PathResult result = service.FindPath(MockCampusMap.XrLab, MockCampusMap.XrLab);

            Assert.IsTrue(result.Success);
            CollectionAssert.AreEqual(new[] { MockCampusMap.XrLab }, result.NodeIds.ToArray());
            Assert.AreEqual(0f, result.TotalDistance);
        }

        [Test]
        public void FindPath_DisconnectedNode_ReturnsUnreachable()
        {
            PathResult result = service.FindPath(MockCampusMap.Entrance, MockCampusMap.Storage);

            Assert.AreEqual(PathStatus.Unreachable, result.Status);
            Assert.IsEmpty(result.Waypoints);
        }

        [TestCase("does-not-exist")]
        [TestCase("")]
        [TestCase(null)]
        public void FindPath_UnknownStart_ReturnsStartNotFound(string startId)
        {
            PathResult result = service.FindPath(startId, MockCampusMap.XrLab);

            Assert.AreEqual(PathStatus.StartNotFound, result.Status);
        }

        [TestCase("does-not-exist")]
        [TestCase(null)]
        public void FindPath_UnknownDestination_ReturnsDestinationNotFound(string destinationId)
        {
            PathResult result = service.FindPath(MockCampusMap.Entrance, destinationId);

            Assert.AreEqual(PathStatus.DestinationNotFound, result.Status);
        }

        [Test]
        public void FindPath_PrefersShorterEdgeOverFewerHops()
        {
            var graph = new CampusGraph();
            graph.AddNode(new CampusNode("a", Vector3.zero));
            graph.AddNode(new CampusNode("b", Vector3.zero));
            graph.AddNode(new CampusNode("c", Vector3.zero));
            graph.AddEdge("a", "c", 10f);
            graph.AddEdge("a", "b", 2f);
            graph.AddEdge("b", "c", 3f);

            PathResult result = new PathfindingService(graph).FindPath("a", "c");

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, result.NodeIds.ToArray());
            Assert.AreEqual(5f, result.TotalDistance, 0.001f);
        }

        [Test]
        public void GetDestinations_ReturnsOnlyDestinationNodes()
        {
            string[] ids = service.GetDestinations().Select(n => n.id).OrderBy(id => id).ToArray();

            CollectionAssert.AreEqual(
                new[] { MockCampusMap.Classroom2, MockCampusMap.Reception, MockCampusMap.Storage, MockCampusMap.XrLab },
                ids);
        }
    }
}
