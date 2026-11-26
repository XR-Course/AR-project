using System;
using CampusNavigation.ARGuidance;
using CampusNavigation.Integration;
using CampusNavigation.Pathfinding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>Creates a separate device scene without modifying the localization scene.</summary>
public static class AndroidPocSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/AndroidPoc.unity";

    [MenuItem("Campus Navigation/Build Android PoC Scene")]
    public static void BuildMenu()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            Build();
    }

    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var mapper = UnityEngine.Object.FindFirstObjectByType<CoordinateMapper>();
        var monitor = UnityEngine.Object.FindFirstObjectByType<LocalizationTrackingMonitor>();
        if (mapper == null || monitor == null)
            throw new InvalidOperationException("The localization scene is missing its mapper or tracking monitor.");

        foreach (var component in UnityEngine.Object.FindObjectsByType<LocalizationDebugUI>(FindObjectsSortMode.None))
            component.enabled = false;
        foreach (var component in UnityEngine.Object.FindObjectsByType<CoordinateGridVisualizer>(FindObjectsSortMode.None))
            component.enabled = false;
        foreach (var component in UnityEngine.Object.FindObjectsByType<CampusWaypointVisualizer>(FindObjectsSortMode.None))
            component.enabled = false;
        foreach (var component in UnityEngine.Object.FindObjectsByType<LocalizationStatusUI>(FindObjectsSortMode.None))
            component.gameObject.SetActive(false);

        var services = new GameObject("PoC Navigation Services");
        var pathfinding = services.AddComponent<PathfindingManager>();
        SetReference(pathfinding, "mapData", Load<CampusMapData>("Assets/Pathfinding/Maps/PocCampusMap.asset"));
        var navigation = services.AddComponent<DeviceNavigationProvider>();
        SetReference(navigation, "coordinateMapper", mapper);
        SetReference(navigation, "trackingMonitor", monitor);
        var alignment = services.AddComponent<CoordinateMapperAlignment>();
        SetReference(alignment, "coordinateMapper", mapper);

        var route = new GameObject("PoC AR Route");
        var renderer = route.AddComponent<ARPathRenderer>();
        renderer.Configure(
            Load<GameObject>("Assets/ARGuidance/Prefabs/RouteArrow.prefab").GetComponent<RouteMarker>(),
            Load<GameObject>("Assets/ARGuidance/Prefabs/RouteWaypoint.prefab").GetComponent<RouteMarker>(),
            Load<GameObject>("Assets/ARGuidance/Prefabs/RouteDestination.prefab").GetComponent<RouteMarker>(), 1.5f);
        var binder = route.AddComponent<NavigationRouteBinder>();
        SetReference(binder, "navigationSource", navigation);
        SetReference(binder, "routeRendererSource", renderer);
        SetReference(binder, "alignmentSource", alignment);

        var canvas = (GameObject)PrefabUtility.InstantiatePrefab(
            Load<GameObject>("Assets/ARGuidance/Prefabs/NavigationCanvas.prefab"), scene);
        var ui = canvas.GetComponent<NavigationUI>();
        var selector = canvas.GetComponentInChildren<DestinationSelector>(true);
        SetReference(ui, "navigationSource", navigation);
        SetReference(ui, "pathfindingSource", pathfinding);
        SetReference(selector, "navigationSource", navigation);
        SetReference(selector, "pathfindingSource", pathfinding);
        SetReference(selector, "positionSource", alignment);

        var events = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        if (events == null)
            events = new GameObject("EventSystem").AddComponent<EventSystem>();
        var oldInput = events.GetComponent<StandaloneInputModule>();
        if (oldInput != null)
            UnityEngine.Object.DestroyImmediate(oldInput);
        if (events.GetComponent<InputSystemUIInputModule>() == null)
            events.gameObject.AddComponent<InputSystemUIInputModule>();

        if (!EditorSceneManager.SaveScene(scene, ScenePath))
            throw new InvalidOperationException("Could not save " + ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Debug.Log("Android PoC scene created: " + ScenePath);
    }

    private static T Load<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        return asset != null ? asset : throw new InvalidOperationException("Missing asset: " + path);
    }

    private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        if (target == null)
            throw new InvalidOperationException("Missing component for field " + field);
        var serialized = new SerializedObject(target);
        var property = serialized.FindProperty(field);
        if (property == null)
            throw new InvalidOperationException($"Missing {target.GetType().Name}.{field}");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
