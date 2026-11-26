
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MarkerTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ARTrackedImageManager imageManager;
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private MarkerMap markerMap;
    [SerializeField] private CoordinateMapper coordinateMapper;
    [SerializeField] private LocalizationTrackingMonitor trackingMonitor;

    [Header("Recalibration")]
    [SerializeField] private float recalibrationCooldown = 5f;

    private readonly Dictionary<TrackableId, GameObject> markers = new();

    private readonly HashSet<TrackableId>
        calibratedThisTrackingPeriod = new();

    private readonly Dictionary<string, float>
        lastRecalibrationTimes = new();

    private void OnEnable()
    {
        if (imageManager != null)
            imageManager.trackablesChanged.AddListener(OnImagesChanged);
    }

    private void OnDisable()
    {
        if (imageManager != null)
            imageManager.trackablesChanged.RemoveListener(OnImagesChanged);
    }

    private void OnImagesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var image in args.added)
            UpdateMarker(image);

        foreach (var image in args.updated)
            UpdateMarker(image);

        foreach (var removed in args.removed)
        {
            calibratedThisTrackingPeriod.Remove(removed.Key);

            if (markers.Remove(removed.Key, out var marker))
                Destroy(marker);
        }
    }
    
    private void UpdateMarker(ARTrackedImage image)
    {
        if (!markers.TryGetValue(image.trackableId, out var marker))
        {
            if (markerPrefab != null)
            {
                marker = Instantiate(markerPrefab);
                markers.Add(image.trackableId, marker);
            }
        }

        bool isTracking =
            image.trackingState == TrackingState.Tracking &&
            ARSession.state == ARSessionState.SessionTracking;

        if (marker != null)
            marker.SetActive(isTracking);

        if (!isTracking)
        {
            calibratedThisTrackingPeriod.Remove(image.trackableId);
            return;
        }

        if (marker != null)
        {
            marker.transform.SetPositionAndRotation(
                image.transform.position,
                image.transform.rotation
            );
        }

        if (markerMap == null || coordinateMapper == null)
            return;

        if (!markerMap.TryGetMarker(
                image.referenceImage.name,
                out CampusMarker campusMarker))
        {
            Debug.LogWarning(
                $"No campus mapping for: {image.referenceImage.name}"
            );

            return;
        }

        Quaternion campusRotation =
            Quaternion.Euler(campusMarker.campusRotation);

        // Preserve the rotation calculation used
        // in our previously working implementation.
        Quaternion horizontalRotation = Quaternion.Euler(
            0f,
            image.transform.eulerAngles.y,
            0f
        );
        
        bool requiresRecovery =
            trackingMonitor != null &&
            trackingMonitor.RequiresRelocalization;

        // Initial localization or recovery using entrance.
        if (!coordinateMapper.IsAligned || requiresRecovery)
        {
            if (campusMarker.markerName == "EntranceMarker")
            {
                AlignUsingMarker(
                    image,
                    campusMarker,
                    horizontalRotation,
                    campusRotation
                );

                Debug.Log(
                    requiresRecovery
                        ? "Entrance relocalization complete."
                        : "Initial localization complete."
                );

                return;
            }

            // Initial alignment must still start at entrance.
            if (!coordinateMapper.IsAligned)
                return;
        }

        // Any registered marker other than the entrance is a checkpoint.
        // Initial localization still requires EntranceMarker.
        if (campusMarker.markerName != "EntranceMarker")
        {
            TryRecalibrateCheckpoint(
                image,
                campusMarker,
                campusRotation,
                horizontalRotation,
                requiresRecovery
            );
        }
    }

    private void AlignUsingMarker(
        ARTrackedImage image,
        CampusMarker campusMarker,
        Quaternion horizontalRotation,
        Quaternion campusRotation)
    {
        coordinateMapper.Recalibrate(
            image.transform.position,
            horizontalRotation,
            campusMarker.campusPosition,
            campusRotation,
            campusMarker.markerName
        );

        if (trackingMonitor != null)
            trackingMonitor.ConfirmMarkerAlignment();
    }

    private void TryRecalibrateCheckpoint(
        ARTrackedImage image,
        CampusMarker campusMarker,
        Quaternion campusRotation,
        Quaternion horizontalRotation,
        bool requiresRecovery)
    {
        TrackableId id = image.trackableId;
        string checkpointName = campusMarker.markerName;

        // Normal recalibration requires a new tracking period.
        // Recovery after AR tracking loss bypasses this limit.
        if (!requiresRecovery &&
            calibratedThisTrackingPeriod.Contains(id))
        {
            return;
        }
        
                // Recovery is allowed immediately.
        if (!requiresRecovery &&
            lastRecalibrationTimes.TryGetValue(
                checkpointName,
                out float lastTime))
        {
            if (Time.time - lastTime < recalibrationCooldown)
                return;
        }

        Vector3 expectedCheckpointAR =
            coordinateMapper.CampusToAR(
                campusMarker.campusPosition
            );

        Vector3 detectedCheckpointAR =
            image.transform.position;

        Vector3 difference =
            detectedCheckpointAR - expectedCheckpointAR;

        Debug.Log(
            $"CHECKPOINT ERROR | " +
            $"Expected: {expectedCheckpointAR} | " +
            $"Detected: {detectedCheckpointAR} | " +
            $"Difference: {difference} | " +
            $"Distance: {difference.magnitude:F2}m"
        );

        Debug.Log(
            $"CHECKPOINT ROTATION | " +
            $"Image: {image.transform.eulerAngles} | " +
            $"Horizontal: {horizontalRotation.eulerAngles} | " +
            $"Campus: {campusRotation.eulerAngles}"
        );

        Vector3 expectedForward =
            coordinateMapper.CampusToAR(
                campusMarker.campusPosition +
                campusRotation * Vector3.forward
            ) -
            coordinateMapper.CampusToAR(
                campusMarker.campusPosition
            );

        expectedForward.y = 0f;

        if (expectedForward.sqrMagnitude > 0.0001f)
        {
            expectedForward.Normalize();

            float expectedHeading =
                Mathf.Atan2(
                    expectedForward.x,
                    expectedForward.z
                ) * Mathf.Rad2Deg;

            float detectedHeading =
                horizontalRotation.eulerAngles.y;

            float headingDifference =
                Mathf.DeltaAngle(
                    expectedHeading,
                    detectedHeading
                );

            Debug.Log(
                $"HEADING DEBUG | " +
                $"Expected AR heading: {expectedHeading:F1} | " +
                $"Detected AR heading: {detectedHeading:F1} | " +
                $"Difference: {headingDifference:F1} degrees"
            );
        }

        AlignUsingMarker(
            image,
            campusMarker,
            horizontalRotation,
            campusRotation
        );

        calibratedThisTrackingPeriod.Add(id);
        lastRecalibrationTimes[checkpointName] = Time.time;

        Debug.Log(
            requiresRecovery
                ? $"Tracking recovery complete: {checkpointName}"
                : $"Checkpoint recalibration complete: {checkpointName}"
        );
    }
}