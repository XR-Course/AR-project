using NUnit.Framework;
using UnityEngine;

namespace CampusNavigation.Pathfinding.Tests
{
    public class PathfindingManagerTests
    {
        private GameObject gameObject;
        private PathfindingManager manager;

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("PathfindingManagerTest");
            manager = gameObject.AddComponent<PathfindingManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void WithoutMapData_FallsBackToMockMap()
        {
            PathResult result = manager.FindPath(MockCampusMap.Entrance, MockCampusMap.XrLab);

            Assert.IsTrue(manager.IsReady);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(18f, result.TotalDistance, 0.001f);
        }

        [Test]
        public void SetMapData_EmptyAsset_LoadsEmptyGraph()
        {
            var data = ScriptableObject.CreateInstance<CampusMapData>();

            manager.SetMapData(data);

            Assert.IsTrue(manager.IsReady);
            Assert.AreEqual(0, manager.Graph.NodeCount);
            Assert.AreEqual(
                PathStatus.StartNotFound,
                manager.FindPath(MockCampusMap.Entrance, MockCampusMap.XrLab).Status);

            Object.DestroyImmediate(data);
        }
    }
}
