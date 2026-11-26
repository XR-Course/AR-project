using CampusNavigation.Navigation;
using CampusNavigation.Pathfinding;
using NUnit.Framework;
using UnityEngine;

namespace CampusNavigation.ARGuidance.Tests
{
    public class NavigationUITextTests
    {
        private class FixedPosition : ICampusPositionProvider
        {
            public bool HasPosition { get; set; }
            public Vector3 CampusPosition { get; set; }
        }

        [TestCase(NavigationState.Idle, NavigationScreen.DestinationSelection)]
        [TestCase(NavigationState.Scanning, NavigationScreen.Navigation)]
        [TestCase(NavigationState.Navigating, NavigationScreen.Navigation)]
        [TestCase(NavigationState.Relocalizing, NavigationScreen.Navigation)]
        [TestCase(NavigationState.Arrived, NavigationScreen.Arrived)]
        public void ScreenFor_MapsEveryState(NavigationState state, NavigationScreen expected)
        {
            Assert.AreEqual(expected, NavigationUIText.ScreenFor(state));
        }

        [Test]
        public void BannerFor_ShowsPromptsOnlyWhenPositionIsUncertain()
        {
            Assert.AreEqual(BannerSeverity.Info, NavigationUIText.BannerFor(NavigationState.Scanning).Severity);
            Assert.AreEqual(BannerSeverity.Warning, NavigationUIText.BannerFor(NavigationState.Relocalizing).Severity);
            Assert.IsFalse(NavigationUIText.BannerFor(NavigationState.Navigating).Visible);
            Assert.IsFalse(NavigationUIText.BannerFor(NavigationState.Idle).Visible);
            Assert.IsFalse(NavigationUIText.BannerFor(NavigationState.Arrived).Visible);
        }

        [TestCase(0.4f, "< 1 m")]
        [TestCase(12.4f, "12 m")]
        [TestCase(999f, "999 m")]
        [TestCase(1500f, "1.5 km")]
        public void FormatDistance_RoundsForReading(float meters, string expected)
        {
            Assert.AreEqual(expected, NavigationUIText.FormatDistance(meters));
        }

        [TestCase(10f, "1 min")]
        [TestCase(60f, "1 min")]
        [TestCase(150f, "3 min")]
        public void FormatWalkingTime_RoundsUpToMinutes(float meters, string expected)
        {
            Assert.AreEqual(expected, NavigationUIText.FormatWalkingTime(meters));
        }

        [Test]
        public void FormatProgress_ShowsDistanceAndPercent()
        {
            Assert.AreEqual("14 m left · 22%", NavigationUIText.FormatProgress(14f, 0.222f));
            Assert.AreEqual("< 1 m left · 100%", NavigationUIText.FormatProgress(0f, 1.3f));
        }

        [Test]
        public void DescribeDestination_CoversHereReachableAndUnreachable()
        {
            var pathfinding = new PathfindingService(MockCampusMap.Create());

            Assert.AreEqual("You are here", NavigationUIText.DescribeDestination(
                pathfinding.FindPathFromPosition(new Vector3(20, 0, -5), MockCampusMap.Reception)));
            Assert.AreEqual("18 m · 1 min", NavigationUIText.DescribeDestination(
                pathfinding.FindPath(MockCampusMap.Entrance, MockCampusMap.XrLab)));
            Assert.AreEqual("No route", NavigationUIText.DescribeDestination(
                pathfinding.FindPath(MockCampusMap.Entrance, MockCampusMap.Storage)));
        }

        [Test]
        public void RouteError_ExplainsUnreachableDestination()
        {
            Assert.AreEqual("No walkable route to Storage.",
                NavigationUIText.RouteError(PathStatus.Unreachable, "Storage"));
            Assert.IsEmpty(NavigationUIText.RouteError(PathStatus.Success, "XR Lab"));
        }

        [TestCase(TurnDirection.Straight, 0f)]
        [TestCase(TurnDirection.Left, 90f)]
        [TestCase(TurnDirection.Right, -90f)]
        [TestCase(TurnDirection.UTurn, 180f)]
        [TestCase(TurnDirection.Arrive, 0f)]
        public void IconRotation_TurnsArrowTowardsTheTurn(TurnDirection direction, float expected)
        {
            Assert.AreEqual(expected, NavigationUIText.IconRotation(direction));
        }

        [Test]
        public void Captions_HandleMissingNames()
        {
            Assert.AreEqual("To XR Lab", NavigationUIText.DestinationCaption("XR Lab"));
            Assert.IsEmpty(NavigationUIText.DestinationCaption(null));
            Assert.AreEqual("You have arrived", NavigationUIText.ArrivedMessage(""));
        }

        [Test]
        public void PlanRoute_WithoutPosition_StartsAtFallbackNode()
        {
            var pathfinding = new PathfindingService(MockCampusMap.Create());

            PathResult route = DestinationSelector.PlanRoute(
                pathfinding, new FixedPosition { HasPosition = false }, MockCampusMap.Entrance, MockCampusMap.XrLab);

            Assert.IsTrue(route.Success);
            Assert.AreEqual(MockCampusMap.Entrance, route.NodeIds[0]);
        }

        [Test]
        public void PlanRoute_WithPosition_StartsFromUser()
        {
            var pathfinding = new PathfindingService(MockCampusMap.Create());
            var position = new FixedPosition { HasPosition = true, CampusPosition = new Vector3(15, 0, 0) };

            PathResult route = DestinationSelector.PlanRoute(
                pathfinding, position, MockCampusMap.Entrance, MockCampusMap.Reception);

            Assert.IsTrue(route.Success);
            Assert.Less(route.TotalDistance, 15f, "Routed from (15,0,0), not from the entrance");
        }
    }
}
