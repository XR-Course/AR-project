
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class MarkerTracker : MonoBehaviour
{
    [SerializeField]
    private ARTrackedImageManager imageManager;

    [SerializeField]
    private GameObject markerPrefab;

    [SerializeField]
    private MarkerMap markerMap;

    [SerializeField]
    private CoordinateMapper coordinateMapper;

    private bool checkpointProcessed = false;

    private readonly Dictionary<TrackableId, GameObject>
        markers = new();

    private void OnEnable()
    {
        imageManager.trackablesChanged.AddListener(OnImagesChanged);
    }

    private void OnDisable()
    {
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
            if (markers.Remove(removed.Key, out var marker))
                Destroy(marker);
        }
    }

    private void UpdateMarker(ARTrackedImage image)
    {
        // Create a visual marker for each detected image.
        if (!markers.TryGetValue(image.trackableId, out var marker))
        {
            marker = Instantiate(markerPrefab);
            markers.Add(image.trackableId, marker);
        }

        bool isTracking =
            image.trackingState == TrackingState.Tracking;

        marker.SetActive(isTracking);

        // Only use images with reliable tracking.
        if (!isTracking)
            return;

        // Update the visual marker's position and rotation.
        marker.transform.SetPositionAndRotation(
            image.transform.position,
            image.transform.rotation
        );

        // Find the known campus coordinates of this marker.
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

        // Previous rotation method for home testing.
        // Restored because the experimental projected-axis
        // method caused incorrect grid placement.
        Quaternion horizontalRotation =
            Quaternion.Euler(
                0f,
                image.transform.eulerAngles.y,
                0f
            );

        // INITIAL LOCALIZATION
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
        }

        // CHECKPOINT RELOCALIZATION
        else if (
            campusMarker.markerName == "HallwayCheckpoint" &&
            !checkpointProcessed)
        {
            // Expected checkpoint position based on
            // the existing entrance alignment.
            Vector3 expectedCheckpointAR =
                coordinateMapper.CampusToAR(
                    campusMarker.campusPosition
                );

            // Actual checkpoint position detected by ARCore.
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

            // Log marker rotation information.
            Debug.Log(
                $"CHECKPOINT ROTATION | " +
                $"Image: {image.transform.eulerAngles} | " +
                $"Horizontal: {horizontalRotation.eulerAngles} | " +
                $"Campus: {campusRotation.eulerAngles}"
            );

            // Calculate the expected AR-world forward
            // direction of this checkpoint.
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

            // Recalibrate using the hallway checkpoint.
            coordinateMapper.Recalibrate(
                image.transform.position,
                horizontalRotation,
                campusMarker.campusPosition,
                campusRotation,
                campusMarker.markerName
            );

            checkpointProcessed = true;

            Debug.Log("Checkpoint recalibration complete.");
        }
    }
}
