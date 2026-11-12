using UnityEngine;
using UnityEngine.UI;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// Keeps the portrait-designed UI usable in any orientation:
    /// scales by screen width in portrait and by height in landscape, and limits the
    /// UI to a centered column of at most maxWidth reference pixels, so in landscape
    /// or on tablets the AR view stays visible at the sides.
    /// Put on the content container under the SafeArea.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class AdaptiveLayout : MonoBehaviour
    {
        [SerializeField]
        private CanvasScaler canvasScaler;

        [SerializeField]
        private float maxWidth = 1080f;

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;

            if (canvasScaler == null)
                canvasScaler = GetComponentInParent<CanvasScaler>();
        }

        private void Update()
        {
            if (canvasScaler != null)
                canvasScaler.matchWidthOrHeight = Screen.width > Screen.height ? 1f : 0f;

            var parent = rectTransform.parent as RectTransform;

            if (parent == null)
                return;

            float inset = Mathf.Max(0f, (parent.rect.width - maxWidth) * 0.5f);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(inset, 0f);
            rectTransform.offsetMax = new Vector2(-inset, 0f);
        }
    }
}
