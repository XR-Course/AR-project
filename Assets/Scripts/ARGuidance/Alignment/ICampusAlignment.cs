using System;
using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// The "alignment" argument of IRouteRenderer.DisplayRoute: converts campus-local
    /// positions (meters) to Unity world space (AR session space on a device).
    ///
    /// On a device this is backed by the localization module's CoordinateMapper
    /// (see CampusNavigation.Integration.CoordinateMapperAlignment). In the editor
    /// demo, IdentityAlignment or TransformAlignment is used instead.
    /// </summary>
    public interface ICampusAlignment
    {
        /// <summary>False until a marker has been scanned. Nothing is drawn while false.</summary>
        bool IsAligned { get; }

        Vector3 CampusToWorld(Vector3 campusPosition);

        /// <summary>Raised when the mapping changes (e.g. after a marker rescan).</summary>
        event Action AlignmentChanged;
    }
}
