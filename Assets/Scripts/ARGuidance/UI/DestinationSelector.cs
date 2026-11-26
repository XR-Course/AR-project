using System;
using System.Collections.Generic;
using System.Linq;
using CampusNavigation.Navigation;
using CampusNavigation.Pathfinding;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// Destination selection screen: lists destinations from the pathfinding module
    /// with distance and time, previews the route of the selected one, and starts
    /// navigation. Refreshes every time the screen is shown.
    /// </summary>
    public class DestinationSelector : MonoBehaviour
    {
        [Header("Services (empty = find in scene)")]
        [Tooltip("IPathfindingService, e.g. PathfindingManager.")]
        [SerializeField]
        private MonoBehaviour pathfindingSource;

        [Tooltip("INavigationService or INavigationServiceProvider.")]
        [SerializeField]
        private MonoBehaviour navigationSource;

        [Tooltip("ICampusPositionProvider. Without a position, routes start at the fallback node.")]
        [SerializeField]
        private MonoBehaviour positionSource;

        [Tooltip("Start node used while the user's position is unknown (not localized yet).")]
        [SerializeField]
        private string fallbackStartNodeId = MockCampusMap.Entrance;

        [Header("UI")]
        [SerializeField]
        private RectTransform listContainer;

        [Tooltip("Inactive row that gets cloned for every destination.")]
        [SerializeField]
        private DestinationListItem itemTemplate;

        [SerializeField]
        private Button startButton;

        [SerializeField]
        private TMP_Text summaryText;

        private readonly List<DestinationListItem> items = new();

        private IPathfindingService pathfinding;
        private INavigationService navigation;
        private ICampusPositionProvider position;

        public CampusNode SelectedDestination { get; private set; }
        public PathResult PreviewRoute { get; private set; }
        public IReadOnlyList<DestinationListItem> Items => items;

        /// <summary>Raised after navigation was started for a destination.</summary>
        public event Action<CampusNode> NavigationStarted;

        private void Awake()
        {
            if (itemTemplate != null)
                itemTemplate.gameObject.SetActive(false);

            if (startButton != null)
                startButton.onClick.AddListener(StartSelected);
        }

        private void OnEnable()
        {
            ResolveServices();
            Refresh();
        }

        private void ResolveServices()
        {
            pathfinding ??= SceneServices.Resolve<IPathfindingService>(pathfindingSource);
            navigation ??= SceneServices.ResolveNavigation(navigationSource);
            position ??= SceneServices.Resolve<ICampusPositionProvider>(positionSource);
        }

        /// <summary>Rebuilds the list with fresh distances from the current position.</summary>
        public void Refresh()
        {
            ClearItems();
            SelectedDestination = null;
            PreviewRoute = null;

            if (pathfinding == null)
            {
                Debug.LogError("DestinationSelector: no IPathfindingService found.", this);
                UpdateSummary();
                return;
            }

            foreach (CampusNode node in pathfinding.GetDestinations().OrderBy(n => n.displayName))
            {
                PathResult route = PlanRoute(pathfinding, position, fallbackStartNodeId, node.id);
                string detail = NavigationUIText.DescribeDestination(route);

                DestinationListItem item = Instantiate(itemTemplate, listContainer);
                item.gameObject.SetActive(true);
                item.name = $"Destination {node.id}";
                item.Bind(node, detail, route.Success, OnItemClicked);
                items.Add(item);
            }

            UpdateSummary();
        }

        private void OnItemClicked(DestinationListItem item) => Select(item.Node.id);

        public void Select(string destinationId)
        {
            DestinationListItem selected = items.FirstOrDefault(i => i.Node.id == destinationId);

            SelectedDestination = selected != null ? selected.Node : null;
            PreviewRoute = SelectedDestination != null
                ? PlanRoute(pathfinding, position, fallbackStartNodeId, destinationId)
                : null;

            foreach (DestinationListItem item in items)
                item.SetSelected(item == selected);

            UpdateSummary();
        }

        /// <summary>Plans the route again (the user may have moved) and starts navigation.</summary>
        public void StartSelected()
        {
            if (SelectedDestination == null || navigation == null)
                return;

            PathResult route = PlanRoute(pathfinding, position, fallbackStartNodeId, SelectedDestination.id);
            PreviewRoute = route;

            if (!route.Success)
            {
                UpdateSummary();
                return;
            }

            CampusNode destination = SelectedDestination;
            navigation.StartNavigation(route);
            NavigationStarted?.Invoke(destination);
        }

        /// <summary>
        /// Routes from the user's position when known, otherwise from the fallback node
        /// (e.g. before the first marker scan; navigation then waits in Scanning).
        /// </summary>
        public static PathResult PlanRoute(
            IPathfindingService pathfinding,
            ICampusPositionProvider position,
            string fallbackStartNodeId,
            string destinationId)
        {
            if (position != null && position.HasPosition)
                return pathfinding.FindPathFromPosition(position.CampusPosition, destinationId);

            return pathfinding.FindPath(fallbackStartNodeId, destinationId);
        }

        private void UpdateSummary()
        {
            bool canStart = PreviewRoute != null && PreviewRoute.Success && navigation != null;

            if (startButton != null)
                startButton.interactable = canStart;

            if (summaryText == null)
                return;

            if (SelectedDestination == null)
                summaryText.text = items.Count > 0 ? "Choose where you want to go." : "No destinations available.";
            else if (PreviewRoute.Success)
                summaryText.text = $"{SelectedDestination.displayName}: {NavigationUIText.DescribeRoute(PreviewRoute)}";
            else
                summaryText.text = NavigationUIText.RouteError(PreviewRoute.Status, SelectedDestination.displayName);
        }

        private void ClearItems()
        {
            foreach (DestinationListItem item in items)
            {
                if (item == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(item.gameObject);
                else
                    DestroyImmediate(item.gameObject);
            }

            items.Clear();
        }
    }
}
