using UnityEngine;

namespace CampusNavigation.ARGuidance.Demo
{
    public enum DemoCameraMode
    {
        /// <summary>Fixed view over the whole map.</summary>
        Overview,

        /// <summary>Behind and above the simulated user.</summary>
        Follow,

        /// <summary>Eye height, like holding the phone (closest to the AR view).</summary>
        FirstPerson
    }

    /// <summary>Editor demo only: camera views around the simulated user.</summary>
    public class DemoCameraRig : MonoBehaviour
    {
        [SerializeField]
        private Transform user;

        [SerializeField]
        private DemoCameraMode mode = DemoCameraMode.Overview;

        [SerializeField]
        private Vector3 overviewPosition = new(15f, 46f, -18f);

        [Tooltip("Aimed past the map center so the map sits between the demo panel and the bottom sheet.")]
        [SerializeField]
        private Vector3 overviewLookAt = new(15f, 0f, 6f);

        [SerializeField]
        private float smoothing = 5f;

        [SerializeField]
        private float eyeHeight = 1.6f;

        private bool userVisible = true;

        public DemoCameraMode Mode
        {
            get => mode;
            set => mode = value;
        }

        public void NextMode() => mode = (DemoCameraMode)(((int)mode + 1) % 3);

        private void Start()
        {
            GetTarget(out Vector3 position, out Quaternion rotation);
            transform.SetPositionAndRotation(position, rotation);
        }

        private void LateUpdate()
        {
            // The camera is inside the avatar in first person, so hide it there.
            SetUserVisible(mode != DemoCameraMode.FirstPerson);

            GetTarget(out Vector3 position, out Quaternion rotation);

            float t = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, position, t),
                Quaternion.Slerp(transform.rotation, rotation, t));
        }

        private void SetUserVisible(bool visible)
        {
            if (user == null || userVisible == visible)
                return;

            userVisible = visible;

            foreach (Renderer part in user.GetComponentsInChildren<Renderer>())
                part.enabled = visible;
        }

        private void GetTarget(out Vector3 position, out Quaternion rotation)
        {
            if (user == null || mode == DemoCameraMode.Overview)
            {
                position = overviewPosition;
                rotation = Quaternion.LookRotation(overviewLookAt - overviewPosition, Vector3.up);
                return;
            }

            Vector3 forward = user.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

            if (mode == DemoCameraMode.Follow)
            {
                // Looks at the user, so the user stays above the bottom sheet on a portrait screen.
                position = user.position - forward * 6f + Vector3.up * 7f;
                rotation = Quaternion.LookRotation(user.position + forward * 1.5f - position, Vector3.up);
            }
            else
            {
                position = user.position + Vector3.up * eyeHeight;
                rotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(30f, 0f, 0f);
            }
        }
    }
}
