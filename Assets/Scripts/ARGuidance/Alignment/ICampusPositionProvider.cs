using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// The user's current campus position, used to plan a route from where the user
    /// stands. On a device: CoordinateMapperAlignment. In the editor demo:
    /// SimulatorPositionProvider.
    /// </summary>
    public interface ICampusPositionProvider
    {
        /// <summary>False while the user is not localized.</summary>
        bool HasPosition { get; }

        Vector3 CampusPosition { get; }
    }
}
