using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CampusNavigation.ARGuidance.Tests
{
    public class ARPathRendererTests
    {
        private static readonly Vector3[] LRoute =
        {
            Vector3.zero, new Vector3(4, 0, 0), new Vector3(4, 0, 4)
        };

        private GameObject root;
        private ARPathRenderer pathRenderer;

        private class FakeAlignment : ICampusAlignment
        {
            public bool IsAligned { get; set; } = true;
            public Vector3 Offset { get; set; }
            public event Action AlignmentChanged;
            public Vector3 CampusToWorld(Vector3 campusPosition) => campusPosition + Offset;
            public void Raise() => AlignmentChanged?.Invoke();
        }

        private static RouteMarker MakeTemplate(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            return go.AddComponent<RouteMarker>();
        }

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Test Root");
            pathRenderer = new GameObject("Renderer").AddComponent<ARPathRenderer>();
            pathRenderer.transform.SetParent(root.transform);

            pathRenderer.Configure(
                MakeTemplate("Arrow", root.transform),
                MakeTemplate("Waypoint", root.transform),
                MakeTemplate("Destination", root.transform),
                2f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
        }

        private RouteMarker[] SpawnedMarkers() =>
            pathRenderer.GetComponentsInChildren<RouteMarker>(true);

        [Test]
        public void DisplayRoute_SpawnsArrowsAndIndicators()
        {
            pathRenderer.DisplayRoute(LRoute, null);

            Assert.AreEqual(4, pathRenderer.ArrowCount);
            Assert.AreEqual(3, pathRenderer.WaypointMarkerCount);
            Assert.AreEqual(7, SpawnedMarkers().Length);
        }

        [Test]
        public void DisplayRoute_UsesAlignmentAndFloorOffset()
        {
            var alignment = new FakeAlignment { Offset = new Vector3(100, 0, 0) };

            pathRenderer.DisplayRoute(LRoute, alignment);

            RouteMarker destination = SpawnedMarkers().Single(m => m.name == "Destination");
            Assert.AreEqual(104f, destination.transform.position.x, 0.001f);
            Assert.AreEqual(4f, destination.transform.position.z, 0.001f);
            Assert.Greater(destination.transform.position.y, 0f);
        }

        [Test]
        public void ArrowsPointAlongTheirSegment()
        {
            pathRenderer.DisplayRoute(LRoute, null);

            RouteMarker[] arrows = SpawnedMarkers().Where(m => m.name.StartsWith("Arrow")).ToArray();

            Assert.Less(Vector3.Angle(arrows[0].transform.forward, Vector3.right), 0.1f);
            Assert.Less(Vector3.Angle(arrows[3].transform.forward, Vector3.forward), 0.1f);
        }

        [Test]
        public void HighlightNextWaypoint_HidesPassedAndMarksNext()
        {
            pathRenderer.DisplayRoute(LRoute, null);

            pathRenderer.HighlightNextWaypoint(2);

            RouteMarker[] markers = SpawnedMarkers();
            RouteMarker firstArrow = markers.First(m => m.name.StartsWith("Arrow 0"));
            RouteMarker lastArrow = markers.First(m => m.name.StartsWith("Arrow 3"));
            RouteMarker destination = markers.Single(m => m.name == "Destination");

            Assert.IsFalse(firstArrow.gameObject.activeSelf, "Arrows on walked segments are hidden");
            Assert.AreEqual(RouteVisualState.Next, lastArrow.State);
            Assert.AreEqual(RouteVisualState.Next, destination.State);
        }

        [Test]
        public void HighlightPastLastWaypoint_HidesArrows_KeepsDestination()
        {
            pathRenderer.DisplayRoute(LRoute, null);

            pathRenderer.HighlightNextWaypoint(LRoute.Length);

            RouteMarker[] markers = SpawnedMarkers();
            Assert.IsTrue(markers.Where(m => m.name.StartsWith("Arrow")).All(m => !m.gameObject.activeSelf));
            Assert.AreEqual(RouteVisualState.Passed, markers.Single(m => m.name == "Destination").State);
        }

        [Test]
        public void SetDimmed_AppliesToAllMarkers()
        {
            pathRenderer.DisplayRoute(LRoute, null);

            pathRenderer.SetDimmed(true);

            Assert.IsTrue(SpawnedMarkers().All(m => m.Dimmed));
        }

        [Test]
        public void NotAligned_DrawsNothingUntilAlignmentChanges()
        {
            var alignment = new FakeAlignment { IsAligned = false };

            pathRenderer.DisplayRoute(LRoute, alignment);
            Assert.AreEqual(0, SpawnedMarkers().Length);

            alignment.IsAligned = true;
            alignment.Raise();
            Assert.AreEqual(7, SpawnedMarkers().Length);
        }

        [Test]
        public void AlignmentChanged_MovesTheRoute()
        {
            var alignment = new FakeAlignment();
            pathRenderer.DisplayRoute(LRoute, alignment);

            alignment.Offset = new Vector3(0, 0, 10);
            alignment.Raise();

            RouteMarker destination = SpawnedMarkers().Single(m => m.name == "Destination");
            Assert.AreEqual(14f, destination.transform.position.z, 0.001f);
            Assert.AreEqual(7, SpawnedMarkers().Length, "Old markers are removed on rebuild");
        }

        [Test]
        public void ClearRoute_RemovesEverything_AndStopsListening()
        {
            var alignment = new FakeAlignment();
            pathRenderer.DisplayRoute(LRoute, alignment);

            pathRenderer.ClearRoute();
            alignment.Raise();

            Assert.IsFalse(pathRenderer.HasRoute);
            Assert.AreEqual(0, SpawnedMarkers().Length);
        }
    }
}
