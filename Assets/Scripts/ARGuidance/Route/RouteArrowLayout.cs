using System;
using System.Collections.Generic;
using CampusNavigation.Pathfinding;
using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    public enum RouteVisualState
    {
        /// <summary>Further along the route.</summary>
        Upcoming,

        /// <summary>The part the user is walking now.</summary>
        Next,

        /// <summary>Already walked.</summary>
        Passed
    }

    /// <summary>One floor arrow, in campus-local coordinates.</summary>
    public readonly struct RouteArrowPlacement
    {
        public Vector3 Position { get; }

        /// <summary>Unit direction on the floor plane (y = 0).</summary>
        public Vector3 Direction { get; }

        /// <summary>Index of the waypoint this arrow leads to (end of its segment).</summary>
        public int TargetWaypointIndex { get; }

        public RouteArrowPlacement(Vector3 position, Vector3 direction, int targetWaypointIndex)
        {
            Position = position;
            Direction = direction;
            TargetWaypointIndex = targetWaypointIndex;
        }
    }

    /// <summary>
    /// Decides where arrows go. Pure math, no GameObjects, so it can be unit tested.
    /// </summary>
    public static class RouteArrowLayout
    {
        private const float MinSegmentLength = 0.01f;

        /// <summary>
        /// Places arrows along each segment, at most "spacing" meters apart. Arrows sit
        /// between the sample points (never on a corner or on the destination) so they
        /// don't overlap waypoint indicators, and every segment gets at least one arrow.
        /// </summary>
        public static List<RouteArrowPlacement> Build(IReadOnlyList<Vector3> waypoints, float spacing)
        {
            if (waypoints == null)
                throw new ArgumentNullException(nameof(waypoints));

            var placements = new List<RouteArrowPlacement>();

            for (int i = 1; i < waypoints.Count; i++)
            {
                Vector3 from = waypoints[i - 1];
                Vector3 to = waypoints[i];
                Vector3 direction = to - from;
                direction.y = 0f;

                if (direction.magnitude < MinSegmentLength)
                    continue;

                direction.Normalize();

                List<Vector3> samples = RouteSampler.Resample(new[] { from, to }, spacing);

                for (int s = 1; s < samples.Count; s++)
                {
                    Vector3 middle = Vector3.Lerp(samples[s - 1], samples[s], 0.5f);
                    placements.Add(new RouteArrowPlacement(middle, direction, i));
                }
            }

            return placements;
        }

        /// <summary>State of something that belongs to waypoint "index" while the user walks to "current".</summary>
        public static RouteVisualState StateFor(int index, int current)
        {
            if (index < current)
                return RouteVisualState.Passed;

            return index == current ? RouteVisualState.Next : RouteVisualState.Upcoming;
        }
    }
}
