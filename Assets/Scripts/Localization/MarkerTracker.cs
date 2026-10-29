
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
        if (!markers.TryGetValue(image.trackableId, out var marker))
        {
            marker = Instantiate(markerPrefab);
            markers.Add(image.trackableId, marker);
        }

        bool isTracking =
            image.trackingState == TrackingState.Tracking;
        
        if (image.referenceImage.name == "HallwayCheckpoint")
        {
            Debug.Log(
                $"CHECKPOINT DEBUG | " +
                $"Tracking: {image.trackingState} | " +
                $"Aligned: {coordinateMapper.IsAligned} | " +
                $"Processed: {checkpointProcessed}"
            );
        }
        
        Debug.Log(
            $"TRACKED IMAGE: {image.referenceImage.name}"
        );

        marker.SetActive(isTracking);

        if (!isTracking)
            return;

        // Update the cube's position and rotation.
        marker.transform.SetPositionAndRotation(
            image.transform.position,
            image.transform.rotation
        );

        // Look up the detected marker's campus coordinates.
        if (!markerMap.TryGetMarker(
            image.referenceImage.name,
            out CampusMarker campusMarker))
        {
            Debug.LogWarning(
                $"No campus mapping for: {image.referenceImage.name}"
            );
            return;
        }

        // Establish alignment once after recognizing
        // a known marker with reliable tracking.
        if (!coordinateMapper.IsAligned)
        {
          
            Quaternion campusRotation = Quaternion.Euler(campusMarker.campusRotation);

            // Temporary rotation handling for our home test
            // with horizontal markers.
            Quaternion horizontalRotation = Quaternion.Euler(0f, image.transform.eulerAngles.y, 0f);

            if (!coordinateMapper.IsAligned)
            {
                // Only EntranceMarker can initialize localization.
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
            else if (
                campusMarker.markerName == "HallwayCheckpoint" &&
                !checkpointProcessed)
            {
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
}
