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
        public IReadOnlyList<string> NodeIds { get; }
        public IReadOnlyList<Vector3> Waypoints { get; }
        public float TotalDistance { get; }

        public bool Success => Status == PathStatus.Success;

        private PathResult(
            PathStatus status,
            IReadOnlyList<string> nodeIds,
            IReadOnlyList<Vector3> waypoints,
            float totalDistance)
        {
            Status = status;
            NodeIds = nodeIds;
            Waypoints = waypoints;
            TotalDistance = totalDistance;
        }

        public static PathResult Found(
            IReadOnlyList<string> nodeIds,
            IReadOnlyList<Vector3> waypoints,
            float totalDistance) =>
            new(PathStatus.Success, nodeIds, waypoints, totalDistance);

        public static PathResult Failed(PathStatus status) =>
            new(status, Array.Empty<string>(), Array.Empty<Vector3>(), 0f);
    }
}
