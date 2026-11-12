using System;
using CampusNavigation.Pathfinding;
using UnityEngine;

namespace CampusNavigation.Navigation
{
    /// <summary>
    /// Contract between the navigation engine (Student 3) and the AR guidance / UI (Student 4).
    /// Based on proposal section 6: Start(route), UpdatePose(pose), CurrentInstruction,
    /// CurrentWaypoint, NavigationState.
    ///
    /// All positions are campus-local (meters). Converting to AR space is done by the
    /// renderer through the localization module.
    ///
    /// MockNavigationService implements this until the real engine is ready.
    /// </summary>
    public interface INavigationService
    {
        NavigationState State { get; }

        /// <summary>The active route, or null when Idle.</summary>
        PathResult CurrentRoute { get; }

        /// <summary>Index into CurrentRoute.Waypoints of the waypoint the user is walking to.</summary>
        int CurrentWaypointIndex { get; }

        /// <summary>Campus position of the waypoint the user is walking to.</summary>
        Vector3 CurrentWaypoint { get; }

        NavigationInstruction CurrentInstruction { get; }

        /// <summary>Walking distance left in meters.</summary>
        float RemainingDistance { get; }

        /// <summary>0 at the start of the route, 1 on arrival.</summary>
        float Progress { get; }

        /// <summary>
        /// Starts following a route from the pathfinding module. Goes to Navigating if
        /// localization is reliable, otherwise to Scanning.
        /// (Named StartNavigation instead of Start to avoid clashing with MonoBehaviour.Start.)
        /// </summary>
        void StartNavigation(PathResult route);

        /// <summary>Feeds the user's current campus pose (e.g. from CoordinateMapper).</summary>
        void UpdatePose(Pose campusPose);

        /// <summary>
        /// Tells the engine whether localization can be trusted
        /// (e.g. from LocalizationTrackingMonitor.ReliabilityChanged).
        /// </summary>
        void SetLocalizationReliable(bool reliable);

        /// <summary>Cancels navigation and returns to Idle.</summary>
        void StopNavigation();

        /// <summary>Starts the current route again from its first waypoint.</summary>
        void RestartNavigation();

        event Action<NavigationState> StateChanged;

        /// <summary>Raised when a new route starts or the route is cleared (null).</summary>
        event Action<PathResult> RouteChanged;

        /// <summary>Raised with the new CurrentWaypointIndex when the user passes a waypoint.</summary>
        event Action<int> WaypointChanged;

        event Action<NavigationInstruction> InstructionChanged;
    }
}
