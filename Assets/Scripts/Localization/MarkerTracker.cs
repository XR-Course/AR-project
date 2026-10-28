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

        Debug.Log(
            $"Marker: {image.referenceImage.name} | " +
            $"Tracking: {image.trackingState}"
        );

        // Keep the cube visible after its first detection
        // to help us debug tracking loss.
        marker.SetActive(
            image.trackingState == TrackingState.Tracking
        );

        // Only update its position when tracking is reliable.
        if (image.trackingState == TrackingState.Tracking)
        {
            marker.transform.SetPositionAndRotation(
                image.transform.position,
                image.transform.rotation
            );
        }
        
        if (markerMap.TryGetMarker(
                image.referenceImage.name,
                out CampusMarker campusMarker))
        {
            Debug.Log(
                $"Recognized: {campusMarker.markerName} | " +
                $"Campus position: {campusMarker.campusPosition}"
            );
        }
        else
        {
            Debug.LogWarning(
                $"No campus mapping for: {image.referenceImage.name}"
            );
        }
    }

}