using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusNavigation.Pathfinding
{
    /// <summary>
    /// Splits a route into evenly spaced points, e.g. for placing AR arrows.
    /// </summary>
    public static class RouteSampler
    {
        /// <summary>
        /// Returns points along the route with at most maxSpacing meters between
        /// them. Every original waypoint (corner) is kept, so the shape is unchanged.
        /// </summary>
        public static List<Vector3> Resample(IReadOnlyList<Vector3> waypoints, float maxSpacing)
        {
            if (waypoints == null)
                throw new ArgumentNullException(nameof(waypoints));

            if (maxSpacing <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maxSpacing), "Spacing must be greater than 0.");

            var result = new List<Vector3>();

            if (waypoints.Count == 0)
                return result;

            result.Add(waypoints[0]);

            for (int i = 1; i < waypoints.Count; i++)
            {
                Vector3 from = waypoints[i - 1];
                Vector3 to = waypoints[i];
                float length = Vector3.Distance(from, to);

                // Duplicate point: nothing to add.
                if (length <= 0f)
                    continue;

                int steps = Mathf.CeilToInt(length / maxSpacing);

                for (int step = 1; step < steps; step++)
                    result.Add(Vector3.Lerp(from, to, (float)step / steps));

                result.Add(to);
            }

            return result;
        }
    }
}
