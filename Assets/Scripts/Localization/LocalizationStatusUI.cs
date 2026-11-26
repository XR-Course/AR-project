using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocalizationStatusUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private LocalizationTrackingMonitor trackingMonitor;

    [SerializeField]
    private TMP_Text statusText;

    [SerializeField]
    private Image statusBackground;

    [Header("Colors")]
    [SerializeField]
    private Color reliableColor = new Color(0.1f, 0.55f, 0.3f, 0.9f);

    [SerializeField]
    private Color recoveryColor = new Color(0.85f, 0.55f, 0.1f, 0.9f);

    [SerializeField]
    private Color lostColor = new Color(0.75f, 0.15f, 0.15f, 0.9f);

    private void Update()
    {
        if (trackingMonitor == null ||
            statusText == null ||
            statusBackground == null)
        {
            return;
        }

        if (!trackingMonitor.IsARTracking)
        {
            SetStatus(
                "TRACKING LOST\nMove phone slowly",
                lostColor
            );
        }
        else if (trackingMonitor.RequiresRelocalization)
        {
            SetStatus(
                "RELOCALIZATION REQUIRED\nScan a known marker",
                recoveryColor
            );
        }
        else if (trackingMonitor.IsLocalizationReliable)
        {
            SetStatus(
                "LOCALIZATION RELIABLE\nAR tracking active",
                reliableColor
            );
        }
        else
        {
            SetStatus(
                "NOT LOCALIZED\nScan entrance marker",
                recoveryColor
            );
        }
    }

    private void SetStatus(string message, Color background)
    {
        if (statusText.text != message)
        {
            statusText.text = message;
        }

        if (statusBackground.color != background)
        {
            statusBackground.color = background;
        }
    }
}