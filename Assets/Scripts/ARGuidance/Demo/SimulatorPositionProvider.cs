using CampusNavigation.Navigation.Mock;
using UnityEngine;

namespace CampusNavigation.ARGuidance.Demo
{
    /// <summary>
    /// Editor demo only: reports the fake user of MockNavigationSimulator as the
    /// current campus position, so destinations are routed from where it stands.
    /// </summary>
    public class SimulatorPositionProvider : MonoBehaviour, ICampusPositionProvider
    {
        [SerializeField]
        private MockNavigationSimulator simulator;

        public bool HasPosition => Simulator != null;

        public Vector3 CampusPosition => Simulator != null ? Simulator.UserPose.position : Vector3.zero;

        // Looked up lazily: other components may ask before this one's Awake.
        private MockNavigationSimulator Simulator
        {
            get
            {
                if (simulator == null)
                    simulator = FindFirstObjectByType<MockNavigationSimulator>();

                return simulator;
            }
        }
    }
}
