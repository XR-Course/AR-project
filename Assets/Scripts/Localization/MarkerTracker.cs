
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

    [Header("Recalibration")]
    [SerializeField] private float recalibrationCooldown = 5f;

    private readonly Dictionary<TrackableId, GameObject> markers = new();

    // Checkpoints already recalibrated during their current
    // continuous Tracking period.
    private readonly HashSet<TrackableId> calibratedThisTrackingPeriod = new();

    // Last successful recalibration time for each checkpoint.
    private readonly Dictionary<string, float> lastRecalibrationTimes = new();

    private void OnEnable()
    {
        if (imageManager != null)
        {
            imageManager.trackablesChanged.AddListener(OnImagesChanged);
        }
    }

    private void OnDisable()
    {
        if (imageManager != null)
        {
            imageManager.trackablesChanged.RemoveListener(OnImagesChanged);
        }
    }

    private void OnImagesChanged(
        ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var image in args.added)
        {
            UpdateMarker(image);
        }

        foreach (var image in args.updated)
        {
            UpdateMarker(image);
        }

        foreach (var removed in args.removed)
        {
            calibratedThisTrackingPeriod.Remove(removed.Key);

            if (markers.Remove(removed.Key, out var marker))
            {
                Destroy(marker);
            }
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
            image.trackingState == TrackingState.Tracking;

        if (marker != null)
        {
            marker.SetActive(isTracking);
        }

        // Allow a future recalibration after tracking is lost.
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
        {
            return;
        }

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

        // Preserve the rotation calculation from our working version.
        Quaternion horizontalRotation = Quaternion.Euler(
            0f,
            image.transform.eulerAngles.y,
            0f
        );

        // Initial localization.
        if (!coordinateMapper.IsAligned)
        {
            if (campusMarker.markerName == "EntranceMarker")
            {
                coordinateMapper.Recalibrate(
                    image.transform.position,
                    horizontalRotation,
                    campusMarker.campusPosition,
                    campusRotation,
                    campusMarker.markerName
                );

                Debug.Log("Initial localization complete.");
            }

            return;
        }

        // Checkpoint-based recalibration.
        if (campusMarker.markerName == "HallwayCheckpoint")
        {
            TryRecalibrateCheckpoint(
                image,
                campusMarker,
                campusRotation,
                horizontalRotation
            );
        }
    }

    private void TryRecalibrateCheckpoint(
        ARTrackedImage image,
        CampusMarker campusMarker,
        Quaternion campusRotation,
        Quaternion horizontalRotation)
    {
        TrackableId id = image.trackableId;
        string checkpointName = campusMarker.markerName;

        // Only one successful recalibration per continuous
        // Tracking period.
        if (calibratedThisTrackingPeriod.Contains(id))
        {
            return;
        }

        // Prevent rapid recalibration.
        if (lastRecalibrationTimes.TryGetValue(
                checkpointName,
                out float lastTime))
        {
            float elapsed = Time.time - lastTime;

            if (elapsed < recalibrationCooldown)
            {
                return;
            }
        }

        Vector3 expectedCheckpointAR =
            coordinateMapper.CampusToAR(campusMarker.campusPosition);

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

        coordinateMapper.Recalibrate(
            image.transform.position,
            horizontalRotation,
            campusMarker.campusPosition,
            campusRotation,
            checkpointName
        );

        calibratedThisTrackingPeriod.Add(id);
        lastRecalibrationTimes[checkpointName] = Time.time;

        Debug.Log(
            $"Checkpoint recalibration complete: {checkpointName}"
        );
    }
}
