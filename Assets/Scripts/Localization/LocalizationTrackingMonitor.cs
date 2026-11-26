using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class LocalizationTrackingMonitor : MonoBehaviour
{
    [SerializeField] private CoordinateMapper coordinateMapper;

    public bool IsARTracking =>
        ARSession.state == ARSessionState.SessionTracking;

    public bool RequiresRelocalization { get; private set; }

    public bool IsLocalizationReliable =>
        coordinateMapper != null &&
        coordinateMapper.IsAligned &&
        IsARTracking &&
        hasConfirmedMarker &&
        !RequiresRelocalization;

    public event Action<bool> ReliabilityChanged;

    private bool hasConfirmedMarker;
    private bool lastReliability;

    private void Update()
    {
        // Only invalidate an established localization.
        if (hasConfirmedMarker && !IsARTracking)
        {
            if (!RequiresRelocalization)
            {
                RequiresRelocalization = true;

                Debug.LogWarning(
                    "LOCALIZATION | AR tracking lost. " +
                    "Marker relocalization required."
                );
            }
        }

        NotifyIfChanged();
    }

    public void ConfirmMarkerAlignment()
    {
        if (!IsARTracking || coordinateMapper == null)
            return;

        if (!coordinateMapper.IsAligned)
            return;

        hasConfirmedMarker = true;
        RequiresRelocalization = false;

        Debug.Log(
            "LOCALIZATION | Marker alignment confirmed."
        );

        NotifyIfChanged();
    }

    private void NotifyIfChanged()
    {
        bool reliable = IsLocalizationReliable;

        if (reliable == lastReliability)
            return;

        lastReliability = reliable;
        ReliabilityChanged?.Invoke(reliable);

        Debug.Log(
            reliable
                ? "LOCALIZATION | Position reliable."
                : "LOCALIZATION | Position unreliable."
        );
    }
}