using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusNavigation.Pathfinding
{
    public enum PathStatus
    {
        Success,
        StartNotFound,
        DestinationNotFound,
        Unreachable
    }

    /// <summary>
    /// Output of a route search. Waypoints are campus-local positions (meters),
    /// ordered from start to destination. Empty unless Status is Success.
    /// </summary>
    public class PathResult
    {
        public PathStatus Status { get; }

        /// <summary>Graph nodes on the route, start to destination.</summary>
        public IReadOnlyList<string> NodeIds { get; }

        /// <summary>
        /// Positions to walk through. When HasEntryPoint is true, Waypoints[0]
        /// is the closest point on the corridor (not a node), and Waypoints[i + 1]
        /// belongs to NodeIds[i]. Otherwise Waypoints[i] belongs to NodeIds[i].
        /// </summary>
        public IReadOnlyList<Vector3> Waypoints { get; }

        /// <summary>Walking distance in meters from the start to the destination.</summary>
        public float TotalDistance { get; }

        /// <summary>True when the route starts from a position between nodes.</summary>
        public bool HasEntryPoint { get; }

        public bool Success => Status == PathStatus.Success;

        private PathResult(
            PathStatus status,
            IReadOnlyList<string> nodeIds,
            IReadOnlyList<Vector3> waypoints,
            float totalDistance,
            bool hasEntryPoint)
        {
            Status = status;
            NodeIds = nodeIds;
            Waypoints = waypoints;
            TotalDistance = totalDistance;
            HasEntryPoint = hasEntryPoint;
        }

        public static PathResult Found(
            IReadOnlyList<string> nodeIds,
            IReadOnlyList<Vector3> waypoints,
            float totalDistance,
            bool hasEntryPoint = false) =>
            new(PathStatus.Success, nodeIds, waypoints, totalDistance, hasEntryPoint);

        public static PathResult Failed(PathStatus status) =>
            new(status, Array.Empty<string>(), Array.Empty<Vector3>(), 0f, false);
    }
}
