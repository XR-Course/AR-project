using System;
using System.Collections.Generic;
using CampusNavigation.Navigation.Mock;
using CampusNavigation.Pathfinding;
using NUnit.Framework;
using UnityEngine;

namespace CampusNavigation.Navigation.Tests
{
    public class MockNavigationServiceTests
    {
        private PathfindingService pathfinding;

        [SetUp]
        public void SetUp()
        {
            pathfinding = new PathfindingService(MockCampusMap.Create());
        }

        // entrance (0,0,0) -> junction-a (10,0,0) -> xr-lab (10,0,8): left turn at junction-a.
        private PathResult RouteToXrLab() => pathfinding.FindPath(MockCampusMap.Entrance, MockCampusMap.XrLab);

        private static Pose At(Vector3 position) => new(position, Quaternion.identity);

        private static void WalkRoute(MockNavigationService nav, float step = 0.5f)
        {
            Vector3 position = nav.CurrentRoute.Waypoints[0];

            for (int guard = 0; guard < 10000 && nav.State == NavigationState.Navigating; guard++)
            {
                position = Vector3.MoveTowards(position, nav.CurrentWaypoint, step);
                nav.UpdatePose(At(position));
            }
        }

        [Test]
        public void NewService_IsIdle()
        {
            var nav = new MockNavigationService();

            Assert.AreEqual(NavigationState.Idle, nav.State);
            Assert.IsNull(nav.CurrentRoute);
        }

        [Test]
        public void StartNavigation_Localized_GoesToNavigating()
        {
            var nav = new MockNavigationService(localizationReliable: true);
            var states = new List<NavigationState>();
            nav.StateChanged += states.Add;

            nav.StartNavigation(RouteToXrLab());

            Assert.AreEqual(NavigationState.Navigating, nav.State);
            CollectionAssert.AreEqual(new[] { NavigationState.Navigating }, states);
            Assert.AreEqual(0f, nav.Progress, 0.001f);
            Assert.AreEqual(18f, nav.RemainingDistance, 0.001f);
        }

        [Test]
        public void StartNavigation_NotLocalized_WaitsInScanning()
        {
            var nav = new MockNavigationService(localizationReliable: false);

            nav.StartNavigation(RouteToXrLab());
            Assert.AreEqual(NavigationState.Scanning, nav.State);

            nav.SetLocalizationReliable(true);
            Assert.AreEqual(NavigationState.Navigating, nav.State);
        }

        [Test]
        public void StartNavigation_FailedRoute_Throws()
        {
            var nav = new MockNavigationService();
            PathResult unreachable = pathfinding.FindPath(MockCampusMap.Entrance, MockCampusMap.Storage);

            Assert.Throws<ArgumentException>(() => nav.StartNavigation(unreachable));
            Assert.Throws<ArgumentNullException>(() => nav.StartNavigation(null));
        }

        [Test]
        public void TrackingLost_GoesToRelocalizing_AndIgnoresPoses()
        {
            var nav = new MockNavigationService();
            nav.StartNavigation(RouteToXrLab());

            nav.SetLocalizationReliable(false);
            Assert.AreEqual(NavigationState.Relocalizing, nav.State);

            // A pose next to the destination must not count while relocalizing.
            nav.UpdatePose(At(new Vector3(10, 0, 8)));
            Assert.AreEqual(NavigationState.Relocalizing, nav.State);
            Assert.AreEqual(0, nav.CurrentWaypointIndex);

            nav.SetLocalizationReliable(true);
            Assert.AreEqual(NavigationState.Navigating, nav.State);
        }

        [Test]
        public void UpdatePose_NearWaypoint_AdvancesIndex()
        {
            var nav = new MockNavigationService(waypointReachedRadius: 1f);
            nav.StartNavigation(RouteToXrLab());
            var reached = new List<int>();
            nav.WaypointChanged += reached.Add;

            nav.UpdatePose(At(new Vector3(0.5f, 0, 0)));
            Assert.AreEqual(1, nav.CurrentWaypointIndex);

            nav.UpdatePose(At(new Vector3(9.5f, 0, 0)));
            Assert.AreEqual(2, nav.CurrentWaypointIndex);

            CollectionAssert.AreEqual(new[] { 1, 2 }, reached);
        }

        [Test]
        public void Instruction_BeforeJunction_IsLeftTurn()
        {
            var nav = new MockNavigationService();
            nav.StartNavigation(RouteToXrLab());

            nav.UpdatePose(At(new Vector3(4f, 0, 0)));

            Assert.AreEqual(TurnDirection.Left, nav.CurrentInstruction.Direction);
            Assert.AreEqual(6f, nav.CurrentInstruction.Distance, 0.001f);
            Assert.AreEqual("Turn left in 6 m", nav.CurrentInstruction.Text);
        }

        [Test]
        public void Instruction_ToReception_IsRightTurn()
        {
            var nav = new MockNavigationService();
            nav.StartNavigation(pathfinding.FindPath(MockCampusMap.Entrance, MockCampusMap.Reception));

            nav.UpdatePose(At(new Vector3(15f, 0, 0)));

            Assert.AreEqual(TurnDirection.Right, nav.CurrentInstruction.Direction);
        }

        [Test]
        public void Instruction_StraightThroughJunctions_NamesNextRealTurn()
        {
            var nav = new MockNavigationService();
            nav.StartNavigation(pathfinding.FindPath(MockCampusMap.Entrance, MockCampusMap.Reception));

            // Straight through junction-a, right turn at junction-b (20,0,0).
            nav.UpdatePose(At(new Vector3(6f, 0, 0)));

            Assert.AreEqual(TurnDirection.Right, nav.CurrentInstruction.Direction);
            Assert.AreEqual("Turn right in 14 m", nav.CurrentInstruction.Text);
        }

        [Test]
        public void Instruction_StraightToDestination_SaysDestination()
        {
            var nav = new MockNavigationService();
            nav.StartNavigation(pathfinding.FindPath(MockCampusMap.Entrance, MockCampusMap.Classroom2));

            nav.UpdatePose(At(new Vector3(6f, 0, 0)));

            Assert.AreEqual(TurnDirection.Arrive, nav.CurrentInstruction.Direction);
            Assert.AreEqual("Destination in 24 m", nav.CurrentInstruction.Text);
        }

        [Test]
        public void WalkingWholeRoute_Arrives()
        {
            var nav = new MockNavigationService();
            nav.StartNavigation(RouteToXrLab());

            WalkRoute(nav);

            Assert.AreEqual(NavigationState.Arrived, nav.State);
            Assert.AreEqual(1f, nav.Progress, 0.001f);
            Assert.AreEqual(0f, nav.RemainingDistance, 0.001f);
            Assert.AreEqual(TurnDirection.Arrive, nav.CurrentInstruction.Direction);
        }

        [Test]
        public void Progress_Halfway_IsAboutHalf()
        {
            var nav = new MockNavigationService(waypointReachedRadius: 0.5f);
            nav.StartNavigation(RouteToXrLab());

            // 9 m walked of 18: 1 m to junction-a + 8 m to xr-lab remain.
            nav.UpdatePose(At(new Vector3(9f, 0, 0)));

            Assert.AreEqual(0.5f, nav.Progress, 0.01f);
        }

        [Test]
        public void Stop_ClearsRoute_AndReturnsToIdle()
        {
            var nav = new MockNavigationService();
            nav.StartNavigation(RouteToXrLab());
            PathResult lastRoute = RouteToXrLab();
            nav.RouteChanged += r => lastRoute = r;

            nav.StopNavigation();

            Assert.AreEqual(NavigationState.Idle, nav.State);
            Assert.IsNull(nav.CurrentRoute);
            Assert.IsNull(lastRoute);
        }

        [Test]
        public void Restart_AfterArrival_StartsAgainFromFirstWaypoint()
        {
            var nav = new MockNavigationService();
            nav.StartNavigation(RouteToXrLab());
            WalkRoute(nav);

            nav.RestartNavigation();

            Assert.AreEqual(NavigationState.Navigating, nav.State);
            Assert.AreEqual(0, nav.CurrentWaypointIndex);
            Assert.AreEqual(0f, nav.Progress, 0.001f);
        }

        [TestCase(0f, TurnDirection.Straight)]
        [TestCase(30f, TurnDirection.SlightRight)]
        [TestCase(-90f, TurnDirection.Left)]
        [TestCase(150f, TurnDirection.SharpRight)]
        [TestCase(-179f, TurnDirection.UTurn)]
        public void ClassifyTurn_ReturnsExpectedDirection(float angle, TurnDirection expected)
        {
            Assert.AreEqual(expected, MockNavigationService.ClassifyTurn(angle));
        }
    }
}
