using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CampusNavigation.Pathfinding.Tests
{
    /// <summary>
    /// Checks the proof-of-concept map asset. Marker positions must match
    /// the MarkerMap in SampleScene, otherwise routes are drawn in the wrong place.
    /// </summary>
    public class PocCampusMapTests
    {
        private const string MapPath = "Assets/Pathfinding/Maps/PocCampusMap.asset";

        private CampusGraph graph;

        [SetUp]
        public void SetUp()
        {
            var data = AssetDatabase.LoadAssetAtPath<CampusMapData>(MapPath);
            Assert.IsNotNull(data, $"Map asset not found at {MapPath}");

            graph = data.BuildGraph();
        }

        [Test]
        public void Map_HasExpectedSize()
        {
            Assert.AreEqual(6, graph.NodeCount);
            Assert.AreEqual(5, graph.Edges.Count);
        }

        [Test]
        public void Map_HasThreeDestinations()
        {
            CollectionAssert.AreEquivalent(
                new[] { "xr-lab", "classroom", "reception" },
                graph.Destinations.Select(n => n.id).ToArray());
        }

        [TestCase("EntranceMarker", 0f, 0f, 0f)]
        [TestCase("HallwayCheckpoint", 2f, 0f, 0.7f)]
        public void MarkerNodes_MatchMarkerMapPositions(string markerName, float x, float y, float z)
        {
            Assert.IsTrue(graph.TryGetNodeByMarker(markerName, out CampusNode node), $"No node for {markerName}");
            Assert.Less(Vector3.Distance(new Vector3(x, y, z), node.campusPosition), 0.001f);
        }

        [TestCase("entrance")]
        [TestCase("hallway-checkpoint")]
        public void AllDestinations_AreReachable(string startId)
        {
            var service = new PathfindingService(graph);

            foreach (CampusNode destination in graph.Destinations)
            {
                PathResult result = service.FindPath(startId, destination.id);
                Assert.IsTrue(result.Success, $"{startId} -> {destination.id}: {result.Status}");
            }
        }

        [Test]
        public void EntranceToXrLab_PassesCheckpointAndJunction()
        {
            PathResult result = new PathfindingService(graph).FindPath("entrance", "xr-lab");

            CollectionAssert.AreEqual(
                new[] { "entrance", "hallway-checkpoint", "junction", "xr-lab" },
                result.NodeIds.ToArray());
            Assert.AreEqual(Mathf.Sqrt(4f + 0.49f) + 4f + 2.3f, result.TotalDistance, 0.001f);
        }
    }
}
