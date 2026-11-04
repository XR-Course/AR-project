using System.Collections.Generic;
using UnityEngine;

namespace CampusNavigation.Pathfinding
{
    /// <summary>
    /// Editable map asset. Create via Assets > Create > Campus Navigation > Campus Map.
    /// All positions are campus-local coordinates in meters.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CampusMapData",
        menuName = "Campus Navigation/Campus Map")]
    public class CampusMapData : ScriptableObject
    {
        [SerializeField]
        private List<CampusNode> nodes = new();

        [SerializeField]
        private List<CampusEdge> edges = new();

        public IReadOnlyList<CampusNode> NodeList => nodes;
        public IReadOnlyList<CampusEdge> EdgeList => edges;

        /// <summary>
        /// Builds a runtime graph. Throws if the data is invalid
        /// (duplicate ids, edges pointing to missing nodes, ...).
        /// </summary>
        public CampusGraph BuildGraph()
        {
            var graph = new CampusGraph();

            foreach (CampusNode node in nodes)
                graph.AddNode(node);

            foreach (CampusEdge edge in edges)
                graph.AddEdge(edge.fromId, edge.toId, edge.distanceOverride);

            return graph;
        }
    }
}
