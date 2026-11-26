using CampusNavigation.Pathfinding;
using UnityEngine;

namespace CampusNavigation.Navigation.Mock
{
    /// <summary>
    /// Scene component that owns a MockNavigationService and fakes a user walking
    /// along the route, so navigation can be demoed in the editor without an AR device.
    /// Use the context menu (or the public methods) to simulate tracking loss.
    ///
    /// Positions are campus-local. Optionally moves a Transform to show the fake user.
    /// </summary>
    public class MockNavigationSimulator : MonoBehaviour, INavigationServiceProvider
    {
        [Tooltip("Start as localized. If false, navigation waits in Scanning until SimulateRelocalized().")]
        [SerializeField]
        private bool startLocalized = true;

        [SerializeField]
        private float waypointReachedRadius = 0.75f;

        [Tooltip("Move the fake user along the route automatically.")]
        [SerializeField]
        private bool autoWalk = true;

        [SerializeField]
        private float walkSpeed = 1.4f;

        [Tooltip("Where the fake user stands before a route starts (campus coordinates).")]
        [SerializeField]
        private Vector3 startPosition = Vector3.zero;

        [Tooltip("Optional: moved to the fake user's pose every frame.")]
        [SerializeField]
        private Transform simulatedUser;

        private MockNavigationService service;
        private Pose userPose;

        public INavigationService NavigationService => Service;

        public MockNavigationService Service
        {
            get
            {
                EnsureService();
                return service;
            }
        }

        /// <summary>Current campus pose of the fake user, e.g. to route from.</summary>
        public Pose UserPose => userPose;

        public bool AutoWalk
        {
            get => autoWalk;
            set => autoWalk = value;
        }

        public float WalkSpeed
        {
            get => walkSpeed;
            set => walkSpeed = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            EnsureService();
        }

        private void OnDestroy()
        {
            if (service != null)
                service.RouteChanged -= OnRouteChanged;
        }

        private void EnsureService()
        {
            if (service != null)
                return;

            userPose = new Pose(startPosition, Quaternion.identity);
            service = new MockNavigationService(startLocalized, waypointReachedRadius);
            service.RouteChanged += OnRouteChanged;
        }

        private void Update()
        {
            if (autoWalk && service.State == NavigationState.Navigating)
                WalkTowards(service.CurrentWaypoint, walkSpeed * Time.deltaTime);

            ApplyToTransform();
        }

        /// <summary>Moves the fake user and reports the new pose to the service.</summary>
        public void SetUserPose(Pose campusPose)
        {
            userPose = campusPose;
            service.UpdatePose(userPose);
            ApplyToTransform();
        }

        private void WalkTowards(Vector3 target, float step)
        {
            Vector3 current = userPose.position;
            target.y = current.y;
            Vector3 next = Vector3.MoveTowards(current, target, step);
            Vector3 direction = next - current;

            Quaternion rotation = direction.sqrMagnitude > 0.000001f
                ? Quaternion.LookRotation(direction, Vector3.up)
                : userPose.rotation;

            SetUserPose(new Pose(next, rotation));
        }

        private void OnRouteChanged(PathResult route)
        {
            // Restarting replays the route, so put the fake user at its start.
            if (route != null && route.Waypoints.Count > 0)
                userPose = new Pose(route.Waypoints[0], userPose.rotation);
        }

        private void ApplyToTransform()
        {
            if (simulatedUser != null)
                simulatedUser.SetPositionAndRotation(userPose.position, userPose.rotation);
        }

        [ContextMenu("Simulate Tracking Lost")]
        public void SimulateTrackingLost() => Service.SetLocalizationReliable(false);

        [ContextMenu("Simulate Relocalized")]
        public void SimulateRelocalized() => Service.SetLocalizationReliable(true);
    }
}
