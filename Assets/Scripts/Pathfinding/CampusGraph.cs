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

        public readonly struct Edge
        {
            public readonly CampusNode From;
            public readonly CampusNode To;
            public readonly float Distance;

            public Edge(CampusNode from, CampusNode to, float distance)
            {
                From = from;
                To = to;
                Distance = distance;
            }
        }

        /// <summary>
        /// The closest point on the corridor network to some position.
        /// DistanceToFrom / DistanceToTo are walking distances along the edge.
        /// </summary>
        public readonly struct EdgePoint
        {
            public readonly Edge Edge;
            public readonly Vector3 Point;
            public readonly float DistanceToFrom;
            public readonly float DistanceToTo;
            public readonly float HorizontalOffset;

            public EdgePoint(
                Edge edge,
                Vector3 point,
                float distanceToFrom,
                float distanceToTo,
                float horizontalOffset)
            {
                Edge = edge;
                Point = point;
                DistanceToFrom = distanceToFrom;
                DistanceToTo = distanceToTo;
                HorizontalOffset = horizontalOffset;
            }
        }

        private readonly Dictionary<string, CampusNode> nodes = new();
        private readonly Dictionary<string, List<Neighbor>> adjacency = new();
        private readonly List<Edge> edges = new();

        public IEnumerable<CampusNode> Nodes => nodes.Values;

        public IReadOnlyList<Edge> Edges => edges;

        public IEnumerable<CampusNode> Destinations =>
            nodes.Values.Where(n => n.isDestination);

        public int NodeCount => nodes.Count;

        /// <summary>
        /// Builds a graph from serialized data. Throws ArgumentException on invalid data.
        /// </summary>
        public static CampusGraph Build(
            IEnumerable<CampusNode> nodeData,
            IEnumerable<CampusEdge> edgeData)
        {
            var graph = new CampusGraph();

            foreach (CampusNode node in nodeData)
                graph.AddNode(node);

            foreach (CampusEdge edge in edgeData)
                graph.AddEdge(edge.fromId, edge.toId, edge.distanceOverride);

            return graph;
        }

        public void AddNode(CampusNode node)
        {
            if (node == null)
                throw new ArgumentException("Node cannot be null.", nameof(node));

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
            edges.Add(new Edge(from, to, length));
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

        /// <summary>
        /// Closest node measured on the floor plane (Y is ignored, so the
        /// camera height of the phone does not matter). Null if the graph is empty.
        /// </summary>
        public CampusNode FindNearestNode(Vector3 campusPosition)
        {
            CampusNode nearest = null;
            float best = float.PositiveInfinity;

            foreach (CampusNode node in nodes.Values)
            {
                float distance = HorizontalDistance(campusPosition, node.campusPosition);

                if (distance < best)
                {
                    best = distance;
                    nearest = node;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Projects a position onto the closest edge (floor plane, Y ignored).
        /// Returns false if the graph has no edges.
        /// </summary>
        public bool TryFindNearestEdgePoint(Vector3 campusPosition, out EdgePoint result)
        {
            result = default;
            float best = float.PositiveInfinity;
            bool found = false;

            foreach (Edge edge in edges)
            {
                Vector3 a = edge.From.campusPosition;
                Vector3 b = edge.To.campusPosition;

                float t = ProjectOnSegment(campusPosition, a, b);
                Vector3 point = Vector3.Lerp(a, b, t);
                float offset = HorizontalDistance(campusPosition, point);

                if (offset < best)
                {
                    best = offset;
                    found = true;
                    result = new EdgePoint(
                        edge,
                        point,
                        t * edge.Distance,
                        (1f - t) * edge.Distance,
                        offset);
                }
            }

            return found;
        }

        public static float HorizontalDistance(Vector3 a, Vector3 b) =>
            new Vector2(a.x - b.x, a.z - b.z).magnitude;

        /// <summary>
        /// Returns t in [0, 1] for the closest point on segment a-b (floor plane).
        /// </summary>
        private static float ProjectOnSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            var ab = new Vector2(b.x - a.x, b.z - a.z);
            var ap = new Vector2(p.x - a.x, p.z - a.z);
            float lengthSquared = ab.sqrMagnitude;

            if (lengthSquared <= 0f)
                return 0f;

            return Mathf.Clamp01(Vector2.Dot(ap, ab) / lengthSquared);
        }
    }
}
