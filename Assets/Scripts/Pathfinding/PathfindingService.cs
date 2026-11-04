using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusNavigation.Pathfinding
{
    /// <summary>
    /// Dijkstra shortest path over a CampusGraph.
    /// The graph is small (one corridor), so a simple O(V^2) search is enough.
    /// </summary>
    public class PathfindingService : IPathfindingService
    {
        private readonly CampusGraph graph;

        public PathfindingService(CampusGraph graph)
        {
            this.graph = graph ?? throw new ArgumentNullException(nameof(graph));
        }

        public IEnumerable<CampusNode> GetDestinations() => graph.Destinations;

        public PathResult FindPath(string startNodeId, string destinationNodeId)
        {
            if (!graph.ContainsNode(startNodeId))
                return PathResult.Failed(PathStatus.StartNotFound);

            if (!graph.ContainsNode(destinationNodeId))
                return PathResult.Failed(PathStatus.DestinationNotFound);

            var distances = new Dictionary<string, float>();
            var previous = new Dictionary<string, string>();
            var unvisited = new HashSet<string>();

            foreach (CampusNode node in graph.Nodes)
            {
                distances[node.id] = float.PositiveInfinity;
                unvisited.Add(node.id);
            }

            distances[startNodeId] = 0f;

            while (unvisited.Count > 0)
            {
                string current = ClosestUnvisited(unvisited, distances);

                // Remaining nodes can't be reached from the start.
                if (current == null)
                    break;

                if (current == destinationNodeId)
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

            if (float.IsPositiveInfinity(distances[destinationNodeId]))
                return PathResult.Failed(PathStatus.Unreachable);

            return BuildResult(destinationNodeId, previous, distances[destinationNodeId]);
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

        private PathResult BuildResult(
            string destinationNodeId,
            Dictionary<string, string> previous,
            float totalDistance)
        {
            var nodeIds = new List<string>();

            for (string id = destinationNodeId; id != null; previous.TryGetValue(id, out id))
                nodeIds.Add(id);

            nodeIds.Reverse();

            var waypoints = new List<Vector3>(nodeIds.Count);

            foreach (string id in nodeIds)
            {
                graph.TryGetNode(id, out CampusNode node);
                waypoints.Add(node.campusPosition);
            }

            return PathResult.Found(nodeIds, waypoints, totalDistance);
        }
    }
}
