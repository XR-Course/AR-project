using CampusNavigation.ARGuidance.Demo;
using CampusNavigation.Navigation.Mock;
using CampusNavigation.Pathfinding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;

namespace CampusNavigation.ARGuidance.Editor
{
    /// <summary>
    /// Builds Assets/Scenes/ARGuidanceDemo.unity: the AR guidance module running on
    /// mock navigation in a normal Unity scene, no AR device needed (proposal
    /// section 5, Student 4 deliverable). Also rebuilds the prefabs it uses.
    /// Menu: Campus Navigation > AR Guidance > Build Demo Scene.
    /// </summary>
    public static class DemoSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/ARGuidanceDemo.unity";
        private const string MaterialFolder = NavigationUIBuilder.Root + "/Materials/";

        [MenuItem("Campus Navigation/AR Guidance/Build Demo Scene")]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Build();
        }

        public static void Build()
        {
            RoutePrefabBuilder.Build();
            NavigationUIBuilder.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.6f);

            Material ground = RoutePrefabBuilder.SaveMaterial(MaterialFolder + "DemoGround.mat",
                RoutePrefabBuilder.CreateLit(new Color(0.22f, 0.24f, 0.27f)));
            Material corridor = RoutePrefabBuilder.SaveMaterial(MaterialFolder + "DemoCorridor.mat",
                RoutePrefabBuilder.CreateLit(new Color(0.72f, 0.72f, 0.7f)));
            Material avatar = RoutePrefabBuilder.SaveMaterial(MaterialFolder + "DemoUser.mat",
                RoutePrefabBuilder.CreateLit(new Color(0.95f, 0.35f, 0.6f)));

            // Light
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // "Physical" world: ground + corridors drawn from the campus graph.
            GameObject groundPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundPlane.name = "Ground";
            Object.DestroyImmediate(groundPlane.GetComponent<Collider>());
            groundPlane.transform.position = new Vector3(25f, -0.03f, 20f);
            groundPlane.transform.localScale = new Vector3(12f, 1f, 12f);
            groundPlane.GetComponent<MeshRenderer>().sharedMaterial = ground;

            // Services: pathfinding (Student 2) + mock navigation (stands in for Student 3).
            var services = new GameObject("Campus Services");
            var pathfinding = services.AddComponent<PathfindingManager>();
            var simulator = services.AddComponent<MockNavigationSimulator>();
            var positionProvider = services.AddComponent<SimulatorPositionProvider>();

            var mapPreview = new GameObject("Campus Map Preview").AddComponent<CampusMapPreview>();
            SetFields(mapPreview, ("pathfinding", pathfinding), ("corridorMaterial", corridor));

            // Simulated user
            Transform user = BuildUser(avatar);
            SetFields(simulator, ("simulatedUser", user));
            SetBool(simulator, "startLocalized", false);
            SetFields(positionProvider, ("simulator", simulator));

            // Alignment: campus origin. Moving it = drift / recalibration.
            var alignment = new GameObject("Campus Origin (alignment)").AddComponent<TransformAlignment>();

            // Route rendering
            var routeObject = new GameObject("AR Route");
            var pathRenderer = routeObject.AddComponent<ARPathRenderer>();
            SetFields(pathRenderer,
                ("arrowPrefab", Prefab<RouteMarker>(RoutePrefabBuilder.ArrowPrefabPath)),
                ("waypointPrefab", Prefab<RouteMarker>(RoutePrefabBuilder.WaypointPrefabPath)),
                ("destinationPrefab", Prefab<RouteMarker>(RoutePrefabBuilder.DestinationPrefabPath)));

            var binder = routeObject.AddComponent<NavigationRouteBinder>();
            SetFields(binder,
                ("navigationSource", simulator),
                ("routeRendererSource", pathRenderer),
                ("alignmentSource", alignment));

            // UI
            var canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NavigationUIBuilder.PrefabPath);
            var canvas = (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab, scene);
            SetFields(canvas.GetComponent<NavigationUI>(),
                ("navigationSource", simulator),
                ("pathfindingSource", pathfinding));
            SetFields(canvas.GetComponentInChildren<DestinationSelector>(true),
                ("navigationSource", simulator),
                ("pathfindingSource", pathfinding),
                ("positionSource", positionProvider));

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            // Camera + demo controls
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = cameraObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.5f, 0.58f, 0.66f);
            cam.fieldOfView = 60f;
            cam.farClipPlane = 300f;
            var rig = cameraObject.AddComponent<DemoCameraRig>();
            SetFields(rig, ("user", user));

            var controls = new GameObject("Demo Controls").AddComponent<DemoControlPanel>();
            SetFields(controls,
                ("simulator", simulator),
                ("cameraRig", rig),
                ("alignment", alignment));

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"AR Guidance demo scene written to {ScenePath}");
        }

        private static Transform BuildUser(Material material)
        {
            var root = new GameObject("Simulated User").transform;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.5f, 0.9f, 0.5f);
            body.GetComponent<MeshRenderer>().sharedMaterial = material;

            // Shows which way the user faces.
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Facing";
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(root, false);
            nose.transform.localPosition = new Vector3(0f, 1.45f, 0.3f);
            nose.transform.localScale = new Vector3(0.2f, 0.12f, 0.25f);
            nose.GetComponent<MeshRenderer>().sharedMaterial = material;

            return root;
        }

        private static T Prefab<T>(string path) where T : Component =>
            AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<T>();

        private static void SetFields(Object target, params (string field, Object value)[] fields)
        {
            var so = new SerializedObject(target);

            foreach ((string field, Object value) in fields)
            {
                SerializedProperty property = so.FindProperty(field);

                if (property == null)
                    Debug.LogError($"DemoSceneBuilder: field '{field}' not found on {target.GetType().Name}");
                else
                    property.objectReferenceValue = value;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
