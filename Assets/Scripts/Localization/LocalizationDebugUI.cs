using UnityEngine;

public class LocalizationDebugUI : MonoBehaviour
{
    [SerializeField]
    private CoordinateMapper coordinateMapper;

    private GUIStyle style;

    private void OnGUI()
    {
        if (coordinateMapper == null)
            return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box);
            style.fontSize = 32;
            style.alignment = TextAnchor.MiddleLeft;
            style.normal.textColor = Color.white;
        }

        string message = "Waiting for marker...";

        if (coordinateMapper.IsAligned)
        {
            Vector3 pos =
                coordinateMapper.CurrentCampusPosition;

            message =
                $"LOCALIZED\n" +
                $"X: {pos.x:F2} m\n" +
                $"Y: {pos.y:F2} m\n" +
                $"Z: {pos.z:F2} m";
        }

        GUI.Box(
            new Rect(20, 80, 320, 190),
            message,
            style
        );
    }
}