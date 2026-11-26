using System.Collections.Generic;
using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// Shows a route as floor arrows plus waypoint and destination indicators in
    /// world space. Rebuilds itself when the alignment changes (marker rescan), so
    /// arrows stay on the physical corridor.
    /// </summary>
    public class ARPathRenderer : MonoBehaviour, IRouteRenderer
    {
        [Header("Prefabs (root needs a RouteMarker)")]
        [SerializeField]
        private RouteMarker arrowPrefab;

        [SerializeField]
        private RouteMarker waypointPrefab;

        [SerializeField]
        private RouteMarker destinationPrefab;

        [Header("Layout")]
        [Tooltip("Max distance between arrows in meters.")]
        [Min(0.25f)]
        [SerializeField]
        private float arrowSpacing = 1.5f;

        [Tooltip("Lifts markers slightly above the floor to avoid z-fighting with AR planes.")]
        [SerializeField]
        private float floorOffset = 0.02f;

        [SerializeField]
        private bool showWaypointIndicators = true;

        private readonly List<Vector3> campusWaypoints = new();
        private readonly List<(RouteMarker marker, int target)> arrows = new();
        private readonly List<(RouteMarker marker, int index)> waypointMarkers = new();

        private ICampusAlignment alignment;
        private Transform container;
        private int nextWaypointIndex;
        private bool dimmed;

        public bool HasRoute => campusWaypoints.Count > 0;
        public int ArrowCount => arrows.Count;
        public int WaypointMarkerCount => waypointMarkers.Count;
        public int NextWaypointIndex => nextWaypointIndex;
        public bool IsDimmed => dimmed;

        public void Configure(RouteMarker arrow, RouteMarker waypoint, RouteMarker destination, float spacing)
        {
            arrowPrefab = arrow;
            waypointPrefab = waypoint;
            destinationPrefab = destination;
            arrowSpacing = Mathf.Max(0.25f, spacing);
        }

        public void DisplayRoute(IReadOnlyList<Vector3> waypoints, ICampusAlignment routeAlignment)
        {
            ClearRoute();

            if (waypoints == null || waypoints.Count == 0)
                return;

            campusWaypoints.AddRange(waypoints);
            alignment = routeAlignment ?? IdentityAlignment.Instance;
            alignment.AlignmentChanged += Rebuild;
            nextWaypointIndex = 0;

            Rebuild();
        }

        public void HighlightNextWaypoint(int index)
        {
            // Count (one past the last waypoint) means the route is completed.
            nextWaypointIndex = HasRoute ? Mathf.Clamp(index, 0, campusWaypoints.Count) : 0;
            ApplyStates();
        }

        public void ClearRoute()
        {
            if (alignment != null)
                alignment.AlignmentChanged -= Rebuild;

            alignment = null;
            campusWaypoints.Clear();
            nextWaypointIndex = 0;
            DestroyMarkers();
        }

        public void SetDimmed(bool value)
        {
            dimmed = value;
            ApplyStates();
        }

        private void OnDestroy()
        {
            if (alignment != null)
                alignment.AlignmentChanged -= Rebuild;
        }

        /// <summary>Recreates all markers from the stored route and current alignment.</summary>
        public void Rebuild()
        {
            DestroyMarkers();

            if (!HasRoute || !alignment.IsAligned)
                return;

            EnsureContainer();

            if (arrowPrefab != null)
            {
                foreach (RouteArrowPlacement placement in RouteArrowLayout.Build(campusWaypoints, arrowSpacing))
                {
                    Vector3 position = ToWorld(placement.Position);
                    Vector3 direction = ToWorld(placement.Position + placement.Direction) - position;
                    direction.y = 0f;

                    Quaternion rotation = direction.sqrMagnitude > 0.000001f
                        ? Quaternion.LookRotation(direction, Vector3.up)
                        : Quaternion.identity;

                    RouteMarker arrow = Instantiate(arrowPrefab, position, rotation, container);
                    arrow.name = $"Arrow {arrows.Count} -> WP {placement.TargetWaypointIndex}";
                    arrows.Add((arrow, placement.TargetWaypointIndex));
                }
            }

            int lastIndex = campusWaypoints.Count - 1;

            for (int i = 0; i <= lastIndex; i++)
            {
                bool isDestination = i == lastIndex;
                RouteMarker prefab = isDestination ? destinationPrefab : waypointPrefab;

                if (prefab == null || (!isDestination && !showWaypointIndicators))
                    continue;

                RouteMarker marker = Instantiate(prefab, ToWorld(campusWaypoints[i]), Quaternion.identity, container);
                marker.name = isDestination ? "Destination" : $"Waypoint {i}";
                waypointMarkers.Add((marker, i));
            }

            ApplyStates();
        }

        private void ApplyStates()
        {
            foreach ((RouteMarker marker, int target) in arrows)
                marker.SetState(RouteArrowLayout.StateFor(target, nextWaypointIndex), dimmed);

            foreach ((RouteMarker marker, int index) in waypointMarkers)
                marker.SetState(RouteArrowLayout.StateFor(index, nextWaypointIndex), dimmed);
        }

        private Vector3 ToWorld(Vector3 campusPosition) =>
            alignment.CampusToWorld(campusPosition) + Vector3.up * floorOffset;

        private void EnsureContainer()
        {
            if (container != null)
                return;

            container = new GameObject("Route").transform;
            container.SetParent(transform, false);
        }

        private void DestroyMarkers()
        {
            foreach ((RouteMarker marker, _) in arrows)
                DestroyMarker(marker);

            foreach ((RouteMarker marker, _) in waypointMarkers)
                DestroyMarker(marker);

            arrows.Clear();
            waypointMarkers.Clear();
        }

        private static void DestroyMarker(RouteMarker marker)
        {
            if (marker == null)
                return;

            if (Application.isPlaying)
                Destroy(marker.gameObject);
            else
                DestroyImmediate(marker.gameObject);
        }
    }
}
