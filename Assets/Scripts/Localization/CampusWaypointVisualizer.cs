using UnityEngine;

public class CampusWaypointVisualizer : MonoBehaviour
{
    [SerializeField]
    private CoordinateMapper coordinateMapper;

    [SerializeField]
    private GameObject waypointPrefab;

    [SerializeField]
    private Vector3 campusPosition = new Vector3(1f, 0f, 0f);

    private GameObject waypoint;

    private void OnEnable()
    {
        if (coordinateMapper != null)
            coordinateMapper.AlignmentUpdated += UpdateWaypoint;
    }

    private void OnDisable()
    {
        if (coordinateMapper != null)
            coordinateMapper.AlignmentUpdated -= UpdateWaypoint;
    }

    private void Update()
    {
        if (coordinateMapper == null ||
            waypointPrefab == null)
            return;

        if (!coordinateMapper.IsAligned)
        {
            if (waypoint != null)
            {
                Destroy(waypoint);
                waypoint = null;
            }

            return;
        }

        if (waypoint == null)
        {
            waypoint = Instantiate(waypointPrefab);
            UpdateWaypoint();
        }
    }

    private void UpdateWaypoint()
    {
        if (waypoint == null ||
            coordinateMapper == null ||
            !coordinateMapper.IsAligned)
            return;

        waypoint.transform.position =
            coordinateMapper.CampusToAR(campusPosition);
    }

    private void OnDestroy()
    {
        if (waypoint != null)
            Destroy(waypoint);
    }
}