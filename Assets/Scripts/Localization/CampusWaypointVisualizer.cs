
using System.Collections.Generic;
using UnityEngine;

public class CampusWaypointVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CoordinateMapper coordinateMapper;
    [SerializeField] private MarkerMap markerMap;
    [SerializeField] private GameObject waypointPrefab;

    private readonly Dictionary<string, GameObject> waypoints = new();

    private void OnEnable()
    {
        if (coordinateMapper != null)
        {
            coordinateMapper.AlignmentUpdated += UpdateWaypoints;
        }
    }

    private void OnDisable()
    {
        if (coordinateMapper != null)
        {
            coordinateMapper.AlignmentUpdated -= UpdateWaypoints;
        }
    }

    private void Update()
    {
        if (coordinateMapper == null ||
            markerMap == null ||
            waypointPrefab == null)
        {
            return;
        }

        if (!coordinateMapper.IsAligned)
        {
            ClearWaypoints();
            return;
        }

        CreateMissingWaypoints();
    }

    private void CreateMissingWaypoints()
    {
        foreach (CampusMarker marker in markerMap.GetAllMarkers())
        {
            if (waypoints.ContainsKey(marker.markerName))
                continue;

            GameObject waypoint = Instantiate(waypointPrefab);

            waypoint.name = $"Waypoint_{marker.markerName}";

            waypoint.transform.position =
                coordinateMapper.CampusToAR(marker.campusPosition);

            waypoints.Add(marker.markerName, waypoint);

            Debug.Log(
                $"Created waypoint for {marker.markerName} " +
                $"at campus position {marker.campusPosition}"
            );
        }
    }

    private void UpdateWaypoints()
    {
        if (coordinateMapper == null ||
            markerMap == null ||
            !coordinateMapper.IsAligned)
        {
            return;
        }

        foreach (CampusMarker marker in markerMap.GetAllMarkers())
        {
            if (waypoints.TryGetValue(
                marker.markerName,
                out GameObject waypoint))
            {
                waypoint.transform.position =
                    coordinateMapper.CampusToAR(marker.campusPosition);
            }
        }
    }

    private void ClearWaypoints()
    {
        foreach (GameObject waypoint in waypoints.Values)
        {
            if (waypoint != null)
                Destroy(waypoint);
        }

        waypoints.Clear();
    }

    private void OnDestroy()
    {
        ClearWaypoints();
    }
}
