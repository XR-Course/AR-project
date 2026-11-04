using System.Collections.Generic;

namespace CampusNavigation.Pathfinding
{
    /// <summary>
    /// Contract used by the navigation engine. Returns campus-local waypoints;
    /// converting them to AR space is the localization module's job.
    /// </summary>
    public interface IPathfindingService
    {
        PathResult FindPath(string startNodeId, string destinationNodeId);

        IEnumerable<CampusNode> GetDestinations();
    }
}
