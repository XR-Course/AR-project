using CampusNavigation.Navigation;
using CampusNavigation.Navigation.Mock;
using UnityEngine;

namespace CampusNavigation.ARGuidance.Demo
{
    /// <summary>
    /// Editor demo only: on-screen buttons that fake what the device would do —
    /// scanning a marker, losing tracking, alignment drift — plus walk speed and
    /// camera view. Drawn with IMGUI so it is clearly separate from the app UI.
    /// </summary>
    public class DemoControlPanel : MonoBehaviour
    {
        [SerializeField]
        private MockNavigationSimulator simulator;

        [SerializeField]
        private DemoCameraRig cameraRig;

        [SerializeField]
        private TransformAlignment alignment;

        [Tooltip("How far the AR alignment is pushed off when simulating drift.")]
        [SerializeField]
        private Vector3 driftOffset = new(0.9f, 0f, 0.7f);

        [SerializeField]
        private float driftYaw = 4f;

        [SerializeField]
        private bool expanded = true;

        private Vector3 alignedPosition;
        private Quaternion alignedRotation;
        private bool drifting;
        private GUIStyle titleStyle;

        private void Start()
        {
            if (alignment != null)
            {
                alignedPosition = alignment.transform.position;
                alignedRotation = alignment.transform.rotation;
            }
        }

        public void ToggleDrift()
        {
            if (alignment == null)
                return;

            drifting = !drifting;

            if (drifting)
                alignment.transform.SetPositionAndRotation(
                    alignedPosition + driftOffset,
                    alignedRotation * Quaternion.Euler(0f, driftYaw, 0f));
            else
                alignment.transform.SetPositionAndRotation(alignedPosition, alignedRotation);
        }

        private void OnGUI()
        {
            if (simulator == null)
                return;

            float scale = Mathf.Max(1f, Screen.height / 1100f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            titleStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };

            // Below the status banner, above the map in overview.
            float top = Screen.height / scale * 0.13f;

            if (!expanded)
            {
                if (GUI.Button(new Rect(12f, top, 90f, 30f), "Demo"))
                    expanded = true;

                return;
            }

            GUILayout.BeginArea(new Rect(12f, top, 250f, 285f), GUI.skin.box);
            GUILayout.Label("Demo controls (not part of the app)", titleStyle);

            INavigationService nav = simulator.NavigationService;
            GUILayout.Label($"State: {nav.State}");

            if (nav.CurrentRoute != null)
                GUILayout.Label($"Waypoint {nav.CurrentWaypointIndex} / {nav.CurrentRoute.Waypoints.Count - 1}");

            if (GUILayout.Button("Scan marker (localized)"))
                simulator.SimulateRelocalized();

            if (GUILayout.Button("Lose tracking"))
                simulator.SimulateTrackingLost();

            if (alignment != null && GUILayout.Button(drifting ? "Recalibrate (fix drift)" : "Simulate drift"))
                ToggleDrift();

            simulator.AutoWalk = GUILayout.Toggle(simulator.AutoWalk, " Auto walk");
            GUILayout.Label($"Walk speed: {simulator.WalkSpeed:0.0} m/s");
            simulator.WalkSpeed = GUILayout.HorizontalSlider(simulator.WalkSpeed, 0.5f, 6f);

            if (cameraRig != null && GUILayout.Button($"Camera: {cameraRig.Mode}"))
                cameraRig.NextMode();

            if (GUILayout.Button("Hide"))
                expanded = false;

            GUILayout.EndArea();
        }
    }
}
