using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// Shrinks this RectTransform to Screen.safeArea so UI stays clear of notches,
    /// camera cut-outs and the home indicator. Put all screens under it.
    /// Test in the editor with the Device Simulator.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect appliedArea;
        private Vector2Int appliedScreen;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            var screen = new Vector2Int(Screen.width, Screen.height);
            Rect area = Screen.safeArea;

            if (area == appliedArea && screen == appliedScreen)
                return;

            appliedArea = area;
            appliedScreen = screen;

            if (screen.x <= 0 || screen.y <= 0)
                return;

            rectTransform.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            rectTransform.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
