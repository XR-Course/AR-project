using System;
using UnityEngine;

namespace CampusNavigation.Pathfinding
{
    /// <summary>
    /// A point in the corridor graph, in campus-local coordinates (meters).
    /// </summary>
    [Serializable]
    public class CampusNode
    {
        [Tooltip("Unique id used by pathfinding, e.g. \"entrance\" or \"xr-lab\".")]
        public string id;

        [Tooltip("Name shown to the user, e.g. \"XR Lab\".")]
        public string displayName;

        [Tooltip("Position in campus coordinates (meters). Y = floor height.")]
        public Vector3 campusPosition;

        [Tooltip("True if the user can pick this node as a destination.")]
        public bool isDestination;

        [Tooltip("Optional: name of the image marker placed at this node.")]
        public string markerName;

        public CampusNode()
        {
        }

        public CampusNode(
            string id,
            Vector3 campusPosition,
            string displayName = null,
            bool isDestination = false,
            string markerName = null)
        {
            this.id = id;
            this.campusPosition = campusPosition;
            this.displayName = displayName ?? id;
            this.isDestination = isDestination;
            this.markerName = markerName;
        }
    }
}
