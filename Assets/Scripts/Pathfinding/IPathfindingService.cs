using System.Collections.Generic;
using UnityEngine;

namespace CampusNavigation.Pathfinding
{
    /// <summary>
    /// Contract used by the navigation engine. Returns campus-local waypoints;
    /// converting them to AR space is the localization module's job.
    /// </summary>
    public interface IPathfindingService
    {
        /// <summary>Route between two known nodes, e.g. after scanning a marker.</summary>
        PathResult FindPath(string startNodeId, string destinationNodeId);

        /// <summary>
        /// Route from any position (e.g. the user's current campus position) for
        /// rerouting. Y is ignored, so the camera position can be passed directly.
        /// </summary>
        PathResult FindPathFromPosition(Vector3 campusPosition, string destinationNodeId);

        /// <summary>Closest node on the floor plane, or null if the map is empty.</summary>
        CampusNode FindNearestNode(Vector3 campusPosition);

        IEnumerable<CampusNode> GetDestinations();
    }
}
