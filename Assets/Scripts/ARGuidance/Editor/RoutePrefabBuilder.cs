using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CampusNavigation.ARGuidance.Editor
{
    /// <summary>
    /// Generates the floor-level route prefabs (arrow, waypoint, destination), the
    /// arrow mesh and the shared transparent marker material.
    /// Menu: Campus Navigation > AR Guidance > Build Route Prefabs.
    /// </summary>
    public static class RoutePrefabBuilder
    {
        public const string ArrowPrefabPath = NavigationUIBuilder.Root + "/Prefabs/RouteArrow.prefab";
        public const string WaypointPrefabPath = NavigationUIBuilder.Root + "/Prefabs/RouteWaypoint.prefab";
        public const string DestinationPrefabPath = NavigationUIBuilder.Root + "/Prefabs/RouteDestination.prefab";
        public const string ArrowMeshPath = NavigationUIBuilder.Root + "/Meshes/FloorArrow.asset";
        public const string MarkerMaterialPath = NavigationUIBuilder.Root + "/Materials/RouteMarker.mat";

        private static readonly Color DestinationColor = new(0.15f, 0.85f, 0.45f, 0.9f);
        private static readonly Color DestinationNextColor = new(0.3f, 1f, 0.55f, 1f);

        [MenuItem("Campus Navigation/AR Guidance/Build Route Prefabs")]
        public static void BuildMenu()
        {
            Build();
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPrefabPath));
        }

        public static void Build()
        {
            Mesh arrowMesh = SaveMesh(FloorArrowMesh.Create(), ArrowMeshPath);
            Material material = SaveMaterial(MarkerMaterialPath, CreateTransparentUnlit());

            BuildArrow(arrowMesh, material);
            BuildWaypoint(material);
            BuildDestination(material);

            AssetDatabase.SaveAssets();
            Debug.Log("AR Guidance route prefabs written to " + NavigationUIBuilder.Root + "/Prefabs");
        }

        private static void BuildArrow(Mesh mesh, Material material)
        {
            var root = new GameObject("RouteArrow");
            root.AddComponent<RouteMarker>();

            var visual = new GameObject("Mesh");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            ConfigureRenderer(visual.AddComponent<MeshRenderer>(), material);

            Save(root, ArrowPrefabPath);
        }

        private static void BuildWaypoint(Material material)
        {
            var root = new GameObject("RouteWaypoint");
            root.AddComponent<RouteMarker>();

            Primitive(PrimitiveType.Cylinder, "Disc", root.transform, material,
                new Vector3(0f, 0.01f, 0f), new Vector3(0.4f, 0.01f, 0.4f));

            Save(root, WaypointPrefabPath);
        }

        private static void BuildDestination(Material material)
        {
            var root = new GameObject("RouteDestination");
            var marker = root.AddComponent<RouteMarker>();

            var so = new SerializedObject(marker);
            so.FindProperty("upcomingColor").colorValue = DestinationColor;
            so.FindProperty("nextColor").colorValue = DestinationNextColor;
            so.FindProperty("passedColor").colorValue = DestinationNextColor; // "reached" stays clearly visible
            so.FindProperty("hideWhenPassed").boolValue = false;
            so.FindProperty("pulseAmount").floatValue = 0.08f;
            so.ApplyModifiedPropertiesWithoutUndo();

            Primitive(PrimitiveType.Cylinder, "Disc", root.transform, material,
                new Vector3(0f, 0.01f, 0f), new Vector3(0.8f, 0.01f, 0.8f));
            Primitive(PrimitiveType.Cylinder, "Pole", root.transform, material,
                new Vector3(0f, 0.75f, 0f), new Vector3(0.05f, 0.75f, 0.05f));
            Primitive(PrimitiveType.Sphere, "Head", root.transform, material,
                new Vector3(0f, 1.6f, 0f), new Vector3(0.3f, 0.3f, 0.3f));

            Save(root, DestinationPrefabPath);
        }

        // ----- Helpers -----

        private static void Primitive(PrimitiveType type, string name, Transform parent, Material material,
            Vector3 position, Vector3 scale)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            ConfigureRenderer(go.GetComponent<MeshRenderer>(), material);
        }

        private static void ConfigureRenderer(MeshRenderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void Save(GameObject root, string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        /// <summary>URP Unlit, alpha blended, double sided. Color comes from RouteMarker.</summary>
        public static Material CreateTransparentUnlit()
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        public static Material CreateLit(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.2f);
            return material;
        }

        /// <summary>Creates the asset, or overwrites it in place so references (GUIDs) survive.</summary>
        public static Material SaveMaterial(string path, Material material)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, path);
                return material;
            }

            EditorUtility.CopySerialized(material, existing);
            Object.DestroyImmediate(material);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            EditorUtility.CopySerialized(mesh, existing);
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }
    }
}
