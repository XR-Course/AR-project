using System.Collections.Generic;
using UnityEngine;

public class MarkerMap : MonoBehaviour
{
    [SerializeField]
    private List<CampusMarker> markers = new();

    private Dictionary<string, CampusMarker> markerLookup;

    private void Awake()
    {
        markerLookup = new Dictionary<string, CampusMarker>();

        foreach (var marker in markers)
        {
            if (string.IsNullOrWhiteSpace(marker.markerName))
            {
                Debug.LogWarning("Marker has no name.");
                continue;
            }

            if (!markerLookup.TryAdd(marker.markerName, marker))
            {
                Debug.LogWarning(
                    $"Duplicate marker: {marker.markerName}"
                );
            }
        }
    }
    
    public IEnumerable<CampusMarker> GetAllMarkers()
    {
        return markers;
    }

    public bool TryGetMarker(
        string markerName,
        out CampusMarker marker)
    {
        return markerLookup.TryGetValue(markerName, out marker);
    }
    
    
}