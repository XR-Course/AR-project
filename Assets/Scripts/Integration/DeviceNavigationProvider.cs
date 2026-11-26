using CampusNavigation.Navigation;
using CampusNavigation.Navigation.Mock;
using UnityEngine;

namespace CampusNavigation.Integration
{
    /// <summary>Feeds the existing PoC navigation service with the real AR pose.</summary>
    public class DeviceNavigationProvider : MonoBehaviour, INavigationServiceProvider
    {
        [SerializeField] private CoordinateMapper coordinateMapper;
        [SerializeField] private LocalizationTrackingMonitor trackingMonitor;
        [SerializeField] private float waypointReachedRadius = 0.75f;

        private MockNavigationService service;

        public INavigationService NavigationService =>
            service ??= new MockNavigationService(false, waypointReachedRadius);

        private void Update()
        {
            INavigationService navigation = NavigationService;
            bool reliable = coordinateMapper != null && trackingMonitor != null &&
                            trackingMonitor.IsLocalizationReliable;
            navigation.SetLocalizationReliable(reliable);

            if (reliable)
                navigation.UpdatePose(new Pose(
                    coordinateMapper.CurrentCampusPosition,
                    coordinateMapper.CurrentCampusRotation));
        }
    }
}
