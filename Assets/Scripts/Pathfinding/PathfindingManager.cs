using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusNavigation.Pathfinding
{
    /// <summary>
    /// Scene entry point for pathfinding. Add it to a GameObject, assign a
    /// CampusMapData asset, and reference this component from other scripts.
    /// Without a map asset it falls back to MockCampusMap (if enabled).
    /// </summary>
    [DisallowMultipleComponent]
    public class PathfindingManager : MonoBehaviour, IPathfindingService
    {
        [SerializeField]
        private CampusMapData mapData;

        [Tooltip("Use the built-in mock corridor when no map asset is assigned.")]
        [SerializeField]
        private bool useMockMapWhenEmpty = true;

        private PathfindingService service;
        private CampusGraph graph;

        /// <summary>The loaded graph. Empty if the map data was invalid.</summary>
        public CampusGraph Graph
        {
            get
            {
                EnsureInitialized();
                return graph;
            }
        }

        /// <summary>False if the map data could not be loaded.</summary>
        public bool IsReady { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>Rebuilds the graph, e.g. after swapping the map asset.</summary>
        public void Reload()
        {
            service = null;
            EnsureInitialized();
        }

        public void SetMapData(CampusMapData data)
        {
            mapData = data;
            Reload();
        }

        public PathResult FindPath(string startNodeId, string destinationNodeId)
        {
            EnsureInitialized();
            return service.FindPath(startNodeId, destinationNodeId);
        }

        public PathResult FindPathFromPosition(Vector3 campusPosition, string destinationNodeId)
        {
            EnsureInitialized();
            return service.FindPathFromPosition(campusPosition, destinationNodeId);
        }

        public CampusNode FindNearestNode(Vector3 campusPosition)
        {
            EnsureInitialized();
            return service.FindNearestNode(campusPosition);
        }

        public IEnumerable<CampusNode> GetDestinations()
        {
            EnsureInitialized();
            return service.GetDestinations();
        }

        private void EnsureInitialized()
        {
            if (service != null)
                return;

            graph = LoadGraph(out bool loaded);
            IsReady = loaded;
            service = new PathfindingService(graph);
        }

        private CampusGraph LoadGraph(out bool loaded)
        {
            loaded = false;

            if (mapData == null)
            {
                if (!useMockMapWhenEmpty)
                {
                    Debug.LogError("PathfindingManager: no CampusMapData assigned.", this);
                    return new CampusGraph();
                }

                Debug.LogWarning("PathfindingManager: no CampusMapData assigned, using MockCampusMap.", this);
                loaded = true;
                return MockCampusMap.Create();
            }

            try
            {
                CampusGraph built = mapData.BuildGraph();
                loaded = true;
                return built;
            }
            catch (ArgumentException e)
            {
                Debug.LogError($"PathfindingManager: invalid map data in '{mapData.name}': {e.Message}", this);
                return new CampusGraph();
            }
        }
    }
}
