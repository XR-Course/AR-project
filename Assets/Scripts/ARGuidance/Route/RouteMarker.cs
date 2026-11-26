using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// Put on the root of an arrow / waypoint / destination prefab. Colors all child
    /// renderers by route state and pulses the "Next" marker. Uses a
    /// MaterialPropertyBlock, so prefabs can share one material.
    /// </summary>
    [DisallowMultipleComponent]
    public class RouteMarker : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField]
        private Color upcomingColor = new(0.1f, 0.7f, 1f, 0.85f);

        [SerializeField]
        private Color nextColor = new(1f, 0.8f, 0.1f, 1f);

        [SerializeField]
        private Color passedColor = new(1f, 1f, 1f, 0.2f);

        [Tooltip("Hide markers the user has already walked past.")]
        [SerializeField]
        private bool hideWhenPassed = true;

        [Tooltip("Alpha multiplier while tracking is uncertain.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float dimmedAlpha = 0.25f;

        [Tooltip("Extra scale of the pulse animation on the Next marker (0 = no pulse).")]
        [SerializeField]
        private float pulseAmount = 0.15f;

        [SerializeField]
        private float pulseSpeed = 4f;

        private Renderer[] renderers;
        private MaterialPropertyBlock block;
        private Vector3 baseScale;
        private bool initialized;

        public RouteVisualState State { get; private set; } = RouteVisualState.Upcoming;
        public bool Dimmed { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;
            renderers = GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();
            baseScale = transform.localScale;
        }

        public void SetState(RouteVisualState state, bool dimmed)
        {
            EnsureInitialized();

            State = state;
            Dimmed = dimmed;

            bool visible = !(hideWhenPassed && state == RouteVisualState.Passed);
            gameObject.SetActive(visible);

            if (!visible)
                return;

            Color color = state switch
            {
                RouteVisualState.Next => nextColor,
                RouteVisualState.Passed => passedColor,
                _ => upcomingColor
            };

            if (dimmed)
                color.a *= dimmedAlpha;

            ApplyColor(color);

            if (!IsPulsing)
                transform.localScale = baseScale;
        }

        private bool IsPulsing => State == RouteVisualState.Next && !Dimmed && pulseAmount > 0f;

        private void Update()
        {
            if (!IsPulsing)
                return;

            float pulse = 1f + pulseAmount * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
            transform.localScale = baseScale * pulse;
        }

        private void ApplyColor(Color color)
        {
            foreach (Renderer target in renderers)
            {
                target.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                block.SetColor(ColorId, color);
                target.SetPropertyBlock(block);
            }
        }
    }
}
