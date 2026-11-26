using System;
using System.Collections.Generic;
using CampusNavigation.Pathfinding;
using UnityEngine;

namespace CampusNavigation.Navigation.Mock
{
    /// <summary>
    /// Stand-in for the real navigation engine (Student 3) so the AR guidance and UI
    /// can be built and tested without it. Implements the full state machine
    /// (Idle, Scanning, Navigating, Relocalizing, Arrived), waypoint progress and
    /// simple turn instructions. Plain C# so it can be unit tested.
    ///
    /// Replace with the real engine by swapping the component that provides
    /// INavigationService; nothing in the UI depends on this class.
    /// </summary>
    public class MockNavigationService : INavigationService
    {
        private const float MinSegmentLength = 0.01f;

        /// <summary>Max sideways distance (m) from a segment to still count as walking along it.</summary>
        private const float OnRouteTolerance = 2f;

        private bool localizationReliable;
        private bool hasPose;
        private Vector3 userPosition;
        private float routeLength;

        /// <summary>A waypoint counts as reached within this many meters (floor plane).</summary>
        public float WaypointReachedRadius { get; set; }

        public NavigationState State { get; private set; } = NavigationState.Idle;
        public PathResult CurrentRoute { get; private set; }
        public int CurrentWaypointIndex { get; private set; }
        public NavigationInstruction CurrentInstruction { get; private set; } = NavigationInstruction.None;
        public float RemainingDistance { get; private set; }
        public float Progress { get; private set; }

        public Vector3 CurrentWaypoint =>
            CurrentRoute != null ? CurrentRoute.Waypoints[CurrentWaypointIndex] : Vector3.zero;

        public event Action<NavigationState> StateChanged;
        public event Action<PathResult> RouteChanged;
        public event Action<int> WaypointChanged;
        public event Action<NavigationInstruction> InstructionChanged;

        public MockNavigationService(bool localizationReliable = true, float waypointReachedRadius = 1f)
        {
            this.localizationReliable = localizationReliable;
            WaypointReachedRadius = waypointReachedRadius;
        }

        public void StartNavigation(PathResult route)
        {
            if (route == null)
                throw new ArgumentNullException(nameof(route));

            if (!route.Success || route.Waypoints.Count == 0)
                throw new ArgumentException($"Cannot navigate a failed route ({route.Status}).", nameof(route));

            CurrentRoute = route;
            CurrentWaypointIndex = 0;
            hasPose = false;
            routeLength = PolylineLength(route.Waypoints, 0);

            RouteChanged?.Invoke(route);
            WaypointChanged?.Invoke(CurrentWaypointIndex);
            SetState(localizationReliable ? NavigationState.Navigating : NavigationState.Scanning);
            Refresh();
        }

        public void UpdatePose(Pose campusPose)
        {
            userPosition = campusPose.position;
            hasPose = true;

            // Poses are only trusted while navigating; while scanning or
            // relocalizing the position may be wrong.
            if (State != NavigationState.Navigating)
                return;

            IReadOnlyList<Vector3> waypoints = CurrentRoute.Waypoints;
            int lastIndex = waypoints.Count - 1;
            bool advanced = false;

            while (true)
            {
                bool reached = FlatDistance(userPosition, waypoints[CurrentWaypointIndex]) <= WaypointReachedRadius;

                if (reached && CurrentWaypointIndex == lastIndex)
                {
                    Arrive();
                    return;
                }

                if (!reached && !HasPassed(waypoints, CurrentWaypointIndex, userPosition))
                    break;

                CurrentWaypointIndex++;
                advanced = true;
            }

            if (advanced)
                WaypointChanged?.Invoke(CurrentWaypointIndex);

            Refresh();
        }

        public void SetLocalizationReliable(bool reliable)
        {
            localizationReliable = reliable;

            switch (State)
            {
                case NavigationState.Scanning when reliable:
                case NavigationState.Relocalizing when reliable:
                    SetState(NavigationState.Navigating);
                    break;

                case NavigationState.Navigating when !reliable:
                    SetState(NavigationState.Relocalizing);
                    break;
            }
        }

        public void StopNavigation()
        {
            bool hadRoute = CurrentRoute != null;

            CurrentRoute = null;
            CurrentWaypointIndex = 0;
            hasPose = false;
            routeLength = 0f;
            RemainingDistance = 0f;
            Progress = 0f;

            if (hadRoute)
                RouteChanged?.Invoke(null);

            SetInstruction(NavigationInstruction.None);
            SetState(NavigationState.Idle);
        }

        public void RestartNavigation()
        {
            if (CurrentRoute != null)
                StartNavigation(CurrentRoute);
        }

        private void Arrive()
        {
            RemainingDistance = 0f;
            Progress = 1f;
            SetInstruction(new NavigationInstruction(TurnDirection.Arrive, 0f, "You have arrived"));
            SetState(NavigationState.Arrived);
        }

        private void Refresh()
        {
            if (CurrentRoute == null || State == NavigationState.Arrived)
                return;

            IReadOnlyList<Vector3> waypoints = CurrentRoute.Waypoints;
            int index = CurrentWaypointIndex;
            Vector3 from = hasPose ? userPosition : waypoints[index];

            float distanceToWaypoint = FlatDistance(from, waypoints[index]);
            RemainingDistance = distanceToWaypoint + PolylineLength(waypoints, index);
            Progress = routeLength > 0f ? Mathf.Clamp01(1f - RemainingDistance / routeLength) : 1f;

            SetInstruction(BuildInstruction(waypoints, index, from, distanceToWaypoint));
        }

        private static NavigationInstruction BuildInstruction(
            IReadOnlyList<Vector3> waypoints, int index, Vector3 from, float distance)
        {
            int lastIndex = waypoints.Count - 1;

            if (index == lastIndex)
                return new NavigationInstruction(TurnDirection.Arrive, distance, $"Destination in {distance:0} m");

            Vector3 previous = index > 0 ? waypoints[index - 1] : from;
            Vector3 incoming = Flatten(waypoints[index] - previous);
            Vector3 outgoing = Flatten(waypoints[index + 1] - waypoints[index]);

            // Standing on the first waypoint: no incoming direction yet.
            if (incoming.magnitude < MinSegmentLength)
                return new NavigationInstruction(TurnDirection.Straight, outgoing.magnitude,
                    $"Go straight for {outgoing.magnitude:0} m");

            // Skip waypoints where the route just continues straight, so the
            // instruction names the next real turn ("Turn right in 14 m").
            int turnIndex = index;
            TurnDirection direction = TurnAt(waypoints, turnIndex, incoming);

            while (direction == TurnDirection.Straight && turnIndex < lastIndex)
            {
                distance += FlatDistance(waypoints[turnIndex], waypoints[turnIndex + 1]);
                turnIndex++;

                if (turnIndex == lastIndex)
                    return new NavigationInstruction(TurnDirection.Arrive, distance, $"Destination in {distance:0} m");

                direction = TurnAt(waypoints, turnIndex, Flatten(waypoints[turnIndex] - waypoints[turnIndex - 1]));
            }

            return new NavigationInstruction(direction, distance, $"{Describe(direction)} in {distance:0} m");
        }

        private static TurnDirection TurnAt(IReadOnlyList<Vector3> waypoints, int index, Vector3 incoming)
        {
            Vector3 outgoing = Flatten(waypoints[index + 1] - waypoints[index]);
            return ClassifyTurn(Vector3.SignedAngle(incoming, outgoing, Vector3.up));
        }

        /// <summary>Positive angles turn right (clockwise seen from above).</summary>
        public static TurnDirection ClassifyTurn(float signedAngle)
        {
            float angle = Mathf.Abs(signedAngle);
            bool right = signedAngle > 0f;

            if (angle < 20f) return TurnDirection.Straight;
            if (angle < 45f) return right ? TurnDirection.SlightRight : TurnDirection.SlightLeft;
            if (angle < 135f) return right ? TurnDirection.Right : TurnDirection.Left;
            if (angle < 165f) return right ? TurnDirection.SharpRight : TurnDirection.SharpLeft;
            return TurnDirection.UTurn;
        }

        private static string Describe(TurnDirection direction) => direction switch
        {
            TurnDirection.SlightLeft => "Bear left",
            TurnDirection.Left => "Turn left",
            TurnDirection.SharpLeft => "Turn sharp left",
            TurnDirection.SlightRight => "Bear right",
            TurnDirection.Right => "Turn right",
            TurnDirection.SharpRight => "Turn sharp right",
            TurnDirection.UTurn => "Turn around",
            TurnDirection.Arrive => "Arrive",
            _ => "Go straight"
        };

        private void SetState(NavigationState state)
        {
            if (State == state)
                return;

            State = state;
            StateChanged?.Invoke(state);
        }

        private void SetInstruction(NavigationInstruction instruction)
        {
            if (instruction.Direction == CurrentInstruction.Direction &&
                instruction.Text == CurrentInstruction.Text)
                return;

            CurrentInstruction = instruction;
            InstructionChanged?.Invoke(instruction);
        }

        /// <summary>
        /// True when the user is already past waypoint i, i.e. ahead of it along the
        /// next segment and close to that segment's line. Handles pose jumps that
        /// never come within WaypointReachedRadius.
        /// </summary>
        private static bool HasPassed(IReadOnlyList<Vector3> waypoints, int i, Vector3 user)
        {
            if (i >= waypoints.Count - 1)
                return false;

            Vector3 segment = Flatten(waypoints[i + 1] - waypoints[i]);
            if (segment.magnitude < MinSegmentLength)
                return true;

            Vector3 offset = Flatten(user - waypoints[i]);
            Vector3 direction = segment.normalized;
            float along = Vector3.Dot(offset, direction);
            float lateral = (offset - direction * along).magnitude;

            return along > 0f && lateral <= OnRouteTolerance;
        }

        private static float PolylineLength(IReadOnlyList<Vector3> points, int fromIndex)
        {
            float length = 0f;

            for (int i = fromIndex + 1; i < points.Count; i++)
                length += FlatDistance(points[i - 1], points[i]);

            return length;
        }

        private static Vector3 Flatten(Vector3 v) => new(v.x, 0f, v.z);

        private static float FlatDistance(Vector3 a, Vector3 b) => Flatten(a - b).magnitude;
    }
}
