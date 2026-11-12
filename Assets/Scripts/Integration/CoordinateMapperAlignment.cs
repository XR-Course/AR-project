using System;
using CampusNavigation.ARGuidance;
using UnityEngine;

namespace CampusNavigation.Integration
{
    /// <summary>
    /// Adapter from the localization module's CoordinateMapper (Assembly-CSharp)
    /// to ARGuidance's ICampusAlignment and ICampusPositionProvider. Lives outside
    /// the ARGuidance assembly because asmdef assemblies cannot reference Assembly-CSharp.
    /// </summary>
    public class CoordinateMapperAlignment : MonoBehaviour, ICampusAlignment, ICampusPositionProvider
    {
        [SerializeField]
        private CoordinateMapper coordinateMapper;

        private bool lastAligned;

        public bool IsAligned => coordinateMapper != null && coordinateMapper.IsAligned;

        public bool HasPosition => IsAligned;

        public Vector3 CampusPosition => IsAligned ? coordinateMapper.CurrentCampusPosition : Vector3.zero;

        public event Action AlignmentChanged;

        public Vector3 CampusToWorld(Vector3 campusPosition) =>
            coordinateMapper != null ? coordinateMapper.CampusToAR(campusPosition) : campusPosition;

        private void OnEnable()
        {
            if (coordinateMapper == null)
                coordinateMapper = FindFirstObjectByType<CoordinateMapper>();

            if (coordinateMapper != null)
                coordinateMapper.AlignmentUpdated += RaiseChanged;

            lastAligned = IsAligned;
        }

        private void OnDisable()
        {
            if (coordinateMapper != null)
                coordinateMapper.AlignmentUpdated -= RaiseChanged;
        }

        private void Update()
        {
            // ResetAlignment() raises no event, so watch IsAligned as well.
            if (IsAligned == lastAligned)
                return;

            lastAligned = IsAligned;
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            lastAligned = IsAligned;
            AlignmentChanged?.Invoke();
        }
    }
}
