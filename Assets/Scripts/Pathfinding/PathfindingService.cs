using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusNavigation.Pathfinding
{
    /// <summary>
    /// Dijkstra shortest path over a CampusGraph.
    /// The graph is small (one corridor), so a simple O(V^2) search is enough.
    ///
    /// The search runs from the destination outwards. Because edges are
    /// two-way, "previous" then points one step closer to the destination,
    /// so a route can be read forwards from any start node.
    /// </summary>
    public class PathfindingService : IPathfindingService
    {
        // Start points closer than this to a node are snapped to the node.
        private const float SnapDistance = 0.01f;

        private readonly CampusGraph graph;

        public PathfindingService(CampusGraph graph)
        {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public IEnumerable<CampusNode> GetDestinations() => graph.Destinations;

        public CampusNode FindNearestNode(Vector3 campusPosition) =>
            graph.FindNearestNode(campusPosition);

        public PathResult FindPath(string startNodeId, string destinationNodeId)
        {
            if (!graph.ContainsNode(startNodeId))
                return PathResult.Failed(PathStatus.StartNotFound);

            if (!graph.ContainsNode(destinationNodeId))
                return PathResult.Failed(PathStatus.DestinationNotFound);

            SearchResult search = SearchFrom(destinationNodeId);

            if (float.IsPositiveInfinity(search.Distances[startNodeId]))
                return PathResult.Failed(PathStatus.Unreachable);

            return BuildResult(startNodeId, search, 0f, null);
        }

        public PathResult FindPathFromPosition(Vector3 campusPosition, string destinationNodeId)
        {
            if (graph.NodeCount == 0)
                return PathResult.Failed(PathStatus.StartNotFound);

            if (!graph.ContainsNode(destinationNodeId))
                return PathResult.Failed(PathStatus.DestinationNotFound);

            SearchResult search = SearchFrom(destinationNodeId);

            if (!graph.TryFindNearestEdgePoint(campusPosition, out CampusGraph.EdgePoint hit))
            {
                // No corridors at all: the only possible route is standing at the destination.
                CampusNode nearest = graph.FindNearestNode(campusPosition);

                if (float.IsPositiveInfinity(search.Distances[nearest.id]))
                    return PathResult.Failed(PathStatus.Unreachable);

                float offset = CampusGraph.HorizontalDistance(campusPosition, nearest.campusPosition);
                return BuildResult(nearest.id, search, offset, null);
            }

            // Standing somewhere on an edge: walk to whichever end gives the shorter total.
            float viaFrom = hit.DistanceToFrom + search.Distances[hit.Edge.From.id];
            float viaTo = hit.DistanceToTo + search.Distances[hit.Edge.To.id];

            if (float.IsPositiveInfinity(viaFrom) && float.IsPositiveInfinity(viaTo))
                return PathResult.Failed(PathStatus.Unreachable);

            bool useFrom = viaFrom <= viaTo;
            CampusNode firstNode = useFrom ? hit.Edge.From : hit.Edge.To;
            float alongEdge = useFrom ? hit.DistanceToFrom : hit.DistanceToTo;

            Vector3? entryPoint = alongEdge > SnapDistance ? hit.Point : (Vector3?)null;

            return BuildResult(firstNode.id, search, hit.HorizontalOffset + alongEdge, entryPoint);
        }

        private readonly struct SearchResult
        {
            public readonly Dictionary<string, float> Distances;
            public readonly Dictionary<string, string> NextTowardsSource;

            public SearchResult(
                Dictionary<string, float> distances,
                Dictionary<string, string> nextTowardsSource)
            {
                Distances = distances;
                NextTowardsSource = nextTowardsSource;
            }
        }

        private SearchResult SearchFrom(string sourceId)
        {
            var distances = new Dictionary<string, float>();
            var previous = new Dictionary<string, string>();
            var unvisited = new HashSet<string>();

            foreach (CampusNode node in graph.Nodes)
            {
                distances[node.id] = float.PositiveInfinity;
                unvisited.Add(node.id);
            }

            distances[sourceId] = 0f;

            while (unvisited.Count > 0)
            {
                string current = ClosestUnvisited(unvisited, distances);

                // Remaining nodes can't be reached from the source.
                if (current == null)
                    break;

                unvisited.Remove(current);

                foreach (CampusGraph.Neighbor neighbor in graph.GetNeighbors(current))
                {
                    if (!unvisited.Contains(neighbor.NodeId))
                        continue;

                    float candidate = distances[current] + neighbor.Distance;

                    if (candidate < distances[neighbor.NodeId])
                    {
                        distances[neighbor.NodeId] = candidate;
                        previous[neighbor.NodeId] = current;
                    }
                }
            }

            return new SearchResult(distances, previous);
        }

        private static string ClosestUnvisited(
            HashSet<string> unvisited,
            Dictionary<string, float> distances)
        {
            string closest = null;
            float best = float.PositiveInfinity;

            foreach (string id in unvisited)
            {
                if (distances[id] < best)
                {
                    best = distances[id];
                    closest = id;
                }
            }

            return closest;
        }

        /// <summary>
        /// Follows the search from the first node to the destination.
        /// entryPoint (if any) is added as the first waypoint, before the first node.
        /// </summary>
        private PathResult BuildResult(
            string firstNodeId,
            SearchResult search,
            float extraDistance,
            Vector3? entryPoint)
        {
            var nodeIds = new List<string>();

            for (string id = firstNodeId; id != null; search.NextTowardsSource.TryGetValue(id, out id))
                nodeIds.Add(id);

            var waypoints = new List<Vector3>(nodeIds.Count + 1);

            if (entryPoint.HasValue)
                waypoints.Add(entryPoint.Value);

            foreach (string id in nodeIds)
            {
                graph.TryGetNode(id, out CampusNode node);
                waypoints.Add(node.campusPosition);
            }

            float total = extraDistance + search.Distances[firstNodeId];

            return PathResult.Found(nodeIds, waypoints, total, entryPoint.HasValue);
        }
    }
}
