
using System.Collections.Generic;
using UnityEngine;

public class CoordinateGridVisualizer : MonoBehaviour
{
    [SerializeField]
    private CoordinateMapper coordinateMapper;

    [SerializeField]
    private float gridSize = 1f;

    [SerializeField]
    private float cellSize = 0.25f;

    [SerializeField]
    private float lineWidth = 0.008f;

    [SerializeField]
    private Color gridColor = Color.cyan;

    private readonly List<GameObject> gridLines = new();

    private Material lineMaterial;
    private bool gridCreated;

    private void Update()
    {
        if (coordinateMapper == null)
            return;

        if (coordinateMapper.IsAligned && !gridCreated)
        {
            CreateGrid();
            gridCreated = true;
        }
        else if (!coordinateMapper.IsAligned && gridCreated)
        {
            ClearGrid();
            gridCreated = false;
        }
    }

    private void CreateGrid()
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError("No compatible grid shader found.");
            return;
        }

        lineMaterial = new Material(shader);

        if (lineMaterial.HasProperty("_BaseColor"))
            lineMaterial.SetColor("_BaseColor", gridColor);
        else
            lineMaterial.color = gridColor;

        float half = gridSize / 2f;
        float offset = 0.015f;

        int cells = Mathf.Max(
            1,
            Mathf.RoundToInt(gridSize / cellSize)
        );

        for (int i = 0; i <= cells; i++)
        {
            float position =
                -half + i * gridSize / cells;

            // Lines parallel to the X axis
            CreateLine(
                new Vector3(-half, offset, position),
                new Vector3(half, offset, position)
            );

// Lines parallel to the Z axis
            CreateLine(
                new Vector3(position, offset, -half),
                new Vector3(position, offset, half)
            );
        }

        Debug.Log("Coordinate grid created.");
    }

    private void CreateLine(Vector3 campusStart, Vector3 campusEnd)
    {
        GameObject lineObject =
            new GameObject("GridLine");

        LineRenderer line =
            lineObject.AddComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.material = lineMaterial;
        line.startColor = gridColor;
        line.endColor = gridColor;

        line.SetPosition(
            0,
            coordinateMapper.CampusToAR(campusStart)
        );

        line.SetPosition(
            1,
            coordinateMapper.CampusToAR(campusEnd)
        );

        gridLines.Add(lineObject);
    }

    private void ClearGrid()
    {
        foreach (GameObject line in gridLines)
        {
            if (line != null)
                Destroy(line);
        }

        gridLines.Clear();

        if (lineMaterial != null)
            Destroy(lineMaterial);
    }

    private void OnDestroy()
    {
        ClearGrid();
    }
}
