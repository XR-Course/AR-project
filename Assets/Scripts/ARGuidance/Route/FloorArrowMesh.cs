using UnityEngine;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// Builds a flat arrow lying on the floor (XZ plane), pointing +Z, facing up,
    /// centered on the origin. Used to create the arrow mesh asset for the prefab.
    /// </summary>
    public static class FloorArrowMesh
    {
        public static Mesh Create(
            float length = 0.6f,
            float headWidth = 0.45f,
            float shaftWidth = 0.18f,
            float headLength = 0.25f)
        {
            float back = -length * 0.5f;
            float tip = length * 0.5f;
            float neck = tip - headLength;
            float shaft = shaftWidth * 0.5f;
            float head = headWidth * 0.5f;

            var vertices = new[]
            {
                // Shaft
                new Vector3(-shaft, 0f, back),
                new Vector3(-shaft, 0f, neck),
                new Vector3(shaft, 0f, neck),
                new Vector3(shaft, 0f, back),

                // Head
                new Vector3(-head, 0f, neck),
                new Vector3(0f, 0f, tip),
                new Vector3(head, 0f, neck)
            };

            // Clockwise seen from above, so the faces point up (+Y).
            var triangles = new[]
            {
                0, 1, 2,
                0, 2, 3,
                4, 5, 6
            };

            var uvs = new Vector2[vertices.Length];
            var normals = new Vector3[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                uvs[i] = new Vector2(vertices[i].x / headWidth + 0.5f, vertices[i].z / length + 0.5f);
                normals[i] = Vector3.up;
            }

            var mesh = new Mesh
            {
                name = "FloorArrow",
                vertices = vertices,
                triangles = triangles,
                uv = uvs,
                normals = normals
            };

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
