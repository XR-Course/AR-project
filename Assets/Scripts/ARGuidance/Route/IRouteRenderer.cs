using System.Collections.Generic;
using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// Draws a route in world space (proposal section 6).
    /// Waypoints are campus-local; the alignment converts them to world space.
    /// </summary>
    public interface IRouteRenderer
    {
        void DisplayRoute(IReadOnlyList<Vector3> campusWaypoints, ICampusAlignment alignment);

        /// <summary>
        /// Marks waypoint "index" as the one the user is walking to; earlier parts count as passed.
        /// index == waypoint count means the route is completed (only the destination stays).
        /// </summary>
        void HighlightNextWaypoint(int index);

        void ClearRoute();

        /// <summary>
        /// Fades the route while position is uncertain (scanning / relocalizing), so arrows
        /// that may be wrong are not trusted (proposal section 7, "tracking loss").
        /// Not in the original proposal interface.
        /// </summary>
        void SetDimmed(bool dimmed);
    }
}
