using System;
using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// Editor/demo alignment: the campus origin is this GameObject's transform.
    /// Moving or rotating it in play mode behaves like a marker recalibration,
    /// so the route can be seen following a new alignment without a device.
    /// </summary>
    public class TransformAlignment : MonoBehaviour, ICampusAlignment
    {
        [SerializeField]
        private bool isAligned = true;

        public bool IsAligned => isAligned;

        public event Action AlignmentChanged;

        public Vector3 CampusToWorld(Vector3 campusPosition) => transform.TransformPoint(campusPosition);

        public void SetAligned(bool aligned)
        {
            if (isAligned == aligned)
                return;

            isAligned = aligned;
            AlignmentChanged?.Invoke();
        }

        private void Start()
        {
            transform.hasChanged = false;
        }

        private void Update()
        {
            if (!transform.hasChanged)
                return;

            transform.hasChanged = false;
            AlignmentChanged?.Invoke();
        }
    }
}
