using System;
using UnityEngine;

namespace CampusNavigation.Pathfinding
{
    /// <summary>
    /// A walkable, two-way connection between two nodes.
    /// </summary>
    [Serializable]
    public class CampusEdge
    {
        public string fromId;
        public string toId;

        [Tooltip("Walking distance in meters. 0 = use straight-line distance between the nodes.")]
        [Min(0f)]
        public float distanceOverride;

        public CampusEdge()
        {
        }

        public CampusEdge(string fromId, string toId, float distanceOverride = 0f)
        {
            this.fromId = fromId;
            this.toId = toId;
            this.distanceOverride = distanceOverride;
        }
    }
}
