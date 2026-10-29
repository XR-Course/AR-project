using UnityEngine;

public class CoordinateMapper : MonoBehaviour
{
    [SerializeField]
    private Transform arCamera;

    private Matrix4x4 campusFromAR =
        Matrix4x4.identity;

    public bool IsAligned { get; private set; }

    public Vector3 CurrentCampusPosition
    {
        get
        {
            if (!IsAligned || arCamera == null)
                return Vector3.zero;

            return ARToCampus(arCamera.position);
        }
    }

    public Quaternion CurrentCampusRotation
    {
        get
        {
            if (!IsAligned || arCamera == null)
                return Quaternion.identity;

            return campusFromAR.rotation *
                   arCamera.rotation;
        }
    }

    public void Align(
        Vector3 markerARPosition,
        Quaternion markerARRotation,
        Vector3 markerCampusPosition,
        Quaternion markerCampusRotation)
    {
        Matrix4x4 markerInAR = Matrix4x4.TRS(
            markerARPosition,
            markerARRotation,
            Vector3.one
        );

        Matrix4x4 markerInCampus = Matrix4x4.TRS(
            markerCampusPosition,
            markerCampusRotation,
            Vector3.one
        );

        campusFromAR =
            markerInCampus * markerInAR.inverse;

        IsAligned = true;

        Debug.Log("Campus alignment established.");
    }

    public Vector3 ARToCampus(Vector3 arPosition)
    {
        return campusFromAR.MultiplyPoint3x4(arPosition);
    }

    public Vector3 CampusToAR(Vector3 campusPosition)
    {
        return campusFromAR.inverse.MultiplyPoint3x4(
            campusPosition
        );
    }

    public void ResetAlignment()
    {
        IsAligned = false;
        campusFromAR = Matrix4x4.identity;
    }
}