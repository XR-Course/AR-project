namespace CampusNavigation.Navigation
{
    /// <summary>
    /// States of the navigation state machine (proposal section 5, Student 3).
    /// </summary>
    public enum NavigationState
    {
        /// <summary>No route. Waiting for the user to pick a destination.</summary>
        Idle,

        /// <summary>A route is requested but the user is not localized yet (scan a marker).</summary>
        Scanning,

        /// <summary>Localized and following the route.</summary>
        Navigating,

        /// <summary>Tracking was lost during navigation; the user must rescan a marker.</summary>
        Relocalizing,

        /// <summary>The last waypoint was reached.</summary>
        Arrived
    }
}
