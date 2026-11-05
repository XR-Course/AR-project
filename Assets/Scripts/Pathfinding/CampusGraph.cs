using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CampusNavigation.Pathfinding
{
    /// <summary>
    /// Runtime corridor graph. Plain C#, no scene or AR dependencies.
    /// Edges are undirected (you can walk both ways).
    /// </summary>
    public class CampusGraph
    {
        public readonly struct Neighbor
        {
            public readonly string NodeId;
            public readonly float Distance;

            public Neighbor(string nodeId, float distance)
            {
                NodeId = nodeId;
                Distance = distance;
            }
        }

        private readonly Dictionary<string, CampusNode> nodes = new();
        private readonly Dictionary<string, List<Neighbor>> adjacency = new();

        public IEnumerable<CampusNode> Nodes => nodes.Values;

        public IEnumerable<CampusNode> Destinations =>
            nodes.Values.Where(n => n.isDestination);

        public int NodeCount => nodes.Count;

        public void AddNode(CampusNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (string.IsNullOrWhiteSpace(node.id))
                throw new ArgumentException("Node id cannot be empty.", nameof(node));

            if (nodes.ContainsKey(node.id))
                throw new ArgumentException($"Duplicate node id: {node.id}", nameof(node));

            nodes.Add(node.id, node);
            adjacency.Add(node.id, new List<Neighbor>());
        }

        /// <summary>
        /// Connects two existing nodes. If distance is 0 or less, the
        /// straight-line distance between the node positions is used.
        /// </summary>
        public void AddEdge(string fromId, string toId, float distance = 0f)
        {
            if (!nodes.TryGetValue(fromId ?? string.Empty, out CampusNode from))
                throw new ArgumentException($"Unknown node id: {fromId}", nameof(fromId));

            if (!nodes.TryGetValue(toId ?? string.Empty, out CampusNode to))
                throw new ArgumentException($"Unknown node id: {toId}", nameof(toId));

            if (fromId == toId)
                throw new ArgumentException($"Edge cannot connect a node to itself: {fromId}");

            float length = distance > 0f
                ? distance
                : Vector3.Distance(from.campusPosition, to.campusPosition);

            adjacency[fromId].Add(new Neighbor(toId, length));
            adjacency[toId].Add(new Neighbor(fromId, length));
        }

        public bool ContainsNode(string id) =>
            id != null && nodes.ContainsKey(id);

        public bool TryGetNode(string id, out CampusNode node)
        {
            node = null;
            return id != null && nodes.TryGetValue(id, out node);
        }

        public bool TryGetNodeByMarker(string markerName, out CampusNode node)
        {
            node = nodes.Values.FirstOrDefault(n => n.markerName == markerName);
            return node != null;
        }

        public IReadOnlyList<Neighbor> GetNeighbors(string id)
        {
            if (id != null && adjacency.TryGetValue(id, out List<Neighbor> neighbors))
                return neighbors;

            return Array.Empty<Neighbor>();
        }
    }
}
