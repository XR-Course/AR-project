using System;
using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    /// <summary>Campus space equals world space. Default when no alignment is given.</summary>
    public sealed class IdentityAlignment : ICampusAlignment
    {
        public static readonly IdentityAlignment Instance = new();

        public bool IsAligned => true;

        public Vector3 CampusToWorld(Vector3 campusPosition) => campusPosition;

        // Never changes, so the event is a no-op.
        public event Action AlignmentChanged
        {
            add { }
            remove { }
        }
    }
}
