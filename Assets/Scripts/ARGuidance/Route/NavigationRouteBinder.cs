using CampusNavigation.Navigation;
using CampusNavigation.Pathfinding;
using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// Connects the navigation engine to the route renderer: shows the active route,
    /// highlights the next waypoint and dims the route while scanning or relocalizing.
    /// Empty fields are filled from the scene.
    /// </summary>
    public class NavigationRouteBinder : MonoBehaviour
    {
        [Tooltip("INavigationService or INavigationServiceProvider (e.g. MockNavigationSimulator).")]
        [SerializeField]
        private MonoBehaviour navigationSource;

        [Tooltip("IRouteRenderer, e.g. ARPathRenderer.")]
        [SerializeField]
        private MonoBehaviour routeRendererSource;

        [Tooltip("ICampusAlignment. Empty = campus space is world space (editor demo).")]
        [SerializeField]
        private MonoBehaviour alignmentSource;

        private INavigationService navigation;
        private IRouteRenderer routeRenderer;
        private ICampusAlignment alignment;

        private void OnEnable()
        {
            navigation = SceneServices.ResolveNavigation(navigationSource);
            routeRenderer = SceneServices.Resolve<IRouteRenderer>(routeRendererSource);
            alignment = SceneServices.Resolve<ICampusAlignment>(alignmentSource) ?? IdentityAlignment.Instance;

            if (navigation == null || routeRenderer == null)
            {
                Debug.LogError("NavigationRouteBinder: navigation service or route renderer not found.", this);
                enabled = false;
                return;
            }

            navigation.RouteChanged += OnRouteChanged;
            navigation.WaypointChanged += OnWaypointChanged;
            navigation.StateChanged += OnStateChanged;

            // Catch up if navigation started before this component was enabled.
            OnRouteChanged(navigation.CurrentRoute);
            OnStateChanged(navigation.State);
        }

        private void OnDisable()
        {
            if (navigation == null)
                return;

            navigation.RouteChanged -= OnRouteChanged;
            navigation.WaypointChanged -= OnWaypointChanged;
            navigation.StateChanged -= OnStateChanged;
        }

        private void OnRouteChanged(PathResult route)
        {
            if (route == null)
            {
                routeRenderer.ClearRoute();
                return;
            }

            routeRenderer.DisplayRoute(route.Waypoints, alignment);
            routeRenderer.HighlightNextWaypoint(navigation.CurrentWaypointIndex);
            routeRenderer.SetDimmed(IsUncertain(navigation.State));
        }

        private void OnWaypointChanged(int index) => routeRenderer.HighlightNextWaypoint(index);

        private void OnStateChanged(NavigationState state)
        {
            routeRenderer.SetDimmed(IsUncertain(state));

            // Arrived: hide the arrows, keep only the destination marker.
            if (state == NavigationState.Arrived && navigation.CurrentRoute != null)
                routeRenderer.HighlightNextWaypoint(navigation.CurrentRoute.Waypoints.Count);
        }

        private static bool IsUncertain(NavigationState state) =>
            state == NavigationState.Scanning || state == NavigationState.Relocalizing;
    }
}
