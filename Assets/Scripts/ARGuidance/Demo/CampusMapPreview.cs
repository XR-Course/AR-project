using System.Collections.Generic;
using CampusNavigation.Pathfinding;
using TMPro;
using UnityEngine;

namespace CampusNavigation.ARGuidance.Demo
{
    /// <summary>
    /// Editor demo only: stands in for the physical hallway. Draws the corridor graph
    /// from the PathfindingManager as floor strips with name labels, so the route can
    /// be seen on top of "real" corridors.
    /// </summary>
    public class CampusMapPreview : MonoBehaviour
    {
        [SerializeField]
        private PathfindingManager pathfinding;

        [SerializeField]
        private Material corridorMaterial;

        [SerializeField]
        private float corridorWidth = 2f;

        [SerializeField]
        private bool showLabels = true;

        [SerializeField]
        private float labelHeight = 2.4f;

        [SerializeField]
        private float labelSize = 5f;

        private readonly List<Transform> labels = new();

        private void Start()
        {
            if (pathfinding == null)
                pathfinding = FindFirstObjectByType<PathfindingManager>();

            if (pathfinding != null)
                Build(pathfinding.Graph);
        }

        private void Build(CampusGraph graph)
        {
            foreach (CampusGraph.Edge edge in graph.Edges)
            {
                Vector3 from = edge.From.campusPosition;
                Vector3 to = edge.To.campusPosition;
                Vector3 direction = to - from;
                direction.y = 0f;

                if (direction.sqrMagnitude < 0.0001f)
                    continue;

                Transform strip = Floor($"Corridor {edge.From.id} - {edge.To.id}");
                strip.position = (from + to) * 0.5f + Vector3.down * 0.01f;
                strip.rotation = Quaternion.LookRotation(direction, Vector3.up);
                strip.localScale = new Vector3(corridorWidth, 0.02f, direction.magnitude);
            }

            foreach (CampusNode node in graph.Nodes)
            {
                Transform pad = Floor($"Node {node.id}");
                pad.position = node.campusPosition + Vector3.down * 0.01f;
                pad.localScale = new Vector3(corridorWidth, 0.02f, corridorWidth);

                // Only nodes with a human-readable name get a label (rooms, entrance).
                if (showLabels && node.displayName != node.id)
                    Label(node);
            }
        }

        private Transform Floor(string objectName)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objectName;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);

            if (corridorMaterial != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = corridorMaterial;

            return go.transform;
        }

        private void Label(CampusNode node)
        {
            var go = new GameObject($"Label {node.id}");
            go.transform.SetParent(transform, false);
            go.transform.position = node.campusPosition + Vector3.up * labelHeight;

            var text = go.AddComponent<TextMeshPro>();
            text.text = node.displayName;
            text.fontSize = labelSize;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = Color.white;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(0, 0, 0, 200);
            text.rectTransform.sizeDelta = new Vector2(10f, 2f);

            labels.Add(go.transform);
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;

            if (cam == null)
                return;

            // Billboard: labels always face the camera.
            foreach (Transform label in labels)
                label.rotation = Quaternion.LookRotation(label.position - cam.transform.position, Vector3.up);
        }
    }
}
