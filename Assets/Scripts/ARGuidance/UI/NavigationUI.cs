using System.Linq;
using CampusNavigation.Navigation;
using CampusNavigation.Pathfinding;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusNavigation.ARGuidance
{
    /// <summary>
    /// Screen-space UI driven by the navigation state: destination selection (Idle),
    /// navigation HUD with turn instruction and progress, arrival screen, tracking /
    /// recalibration banner and help overlay.
    /// </summary>
    public class NavigationUI : MonoBehaviour
    {
        [Header("Services (empty = find in scene)")]
        [SerializeField]
        private MonoBehaviour navigationSource;

        [Tooltip("IPathfindingService, used to show destination names.")]
        [SerializeField]
        private MonoBehaviour pathfindingSource;

        [Header("Screens")]
        [SerializeField]
        private GameObject destinationScreen;

        [SerializeField]
        private GameObject navigationScreen;

        [SerializeField]
        private GameObject arrivedScreen;

        [SerializeField]
        private GameObject helpScreen;

        [SerializeField]
        private bool showHelpOnStart;

        [Header("Status banner")]
        [SerializeField]
        private GameObject banner;

        [SerializeField]
        private Image bannerBackground;

        [SerializeField]
        private TMP_Text bannerTitle;

        [SerializeField]
        private TMP_Text bannerMessage;

        [SerializeField]
        private Color infoColor = new(0.1f, 0.35f, 0.75f, 0.92f);

        [SerializeField]
        private Color warningColor = new(0.85f, 0.45f, 0.05f, 0.95f);

        [Header("Navigation HUD")]
        [SerializeField]
        private TMP_Text destinationText;

        [SerializeField]
        private TMP_Text instructionText;

        [SerializeField]
        private RectTransform turnIcon;

        [SerializeField]
        private Image turnIconImage;

        [SerializeField]
        private Sprite turnArrowSprite;

        [SerializeField]
        private Sprite arriveSprite;

        [SerializeField]
        private TMP_Text progressText;

        [Tooltip("Stretched horizontally from 0 to 1 by the route progress.")]
        [SerializeField]
        private RectTransform progressFill;

        [Header("Buttons")]
        [SerializeField]
        private Button stopButton;

        [SerializeField]
        private Button restartButton;

        [SerializeField]
        private Button helpButton;

        [SerializeField]
        private Button closeHelpButton;

        [Header("Arrived screen")]
        [SerializeField]
        private TMP_Text arrivedText;

        [SerializeField]
        private Button newDestinationButton;

        [SerializeField]
        private Button arrivedRestartButton;

        private INavigationService navigation;
        private IPathfindingService pathfinding;
        private string destinationName = string.Empty;

        public NavigationScreen CurrentScreen { get; private set; }
        public bool IsHelpOpen => helpScreen != null && helpScreen.activeSelf;

        private void Awake()
        {
            AddListener(stopButton, StopNavigation);
            AddListener(restartButton, RestartNavigation);
            AddListener(arrivedRestartButton, RestartNavigation);
            AddListener(newDestinationButton, StopNavigation);
            AddListener(helpButton, ToggleHelp);
            AddListener(closeHelpButton, CloseHelp);
        }

        private void Start()
        {
            SetHelpVisible(showHelpOnStart);
        }

        private void OnEnable()
        {
            navigation ??= SceneServices.ResolveNavigation(navigationSource);
            pathfinding ??= SceneServices.Resolve<IPathfindingService>(pathfindingSource);

            if (navigation == null)
            {
                Debug.LogError("NavigationUI: no navigation service found.", this);
                enabled = false;
                return;
            }

            navigation.StateChanged += OnStateChanged;
            navigation.RouteChanged += OnRouteChanged;

            OnRouteChanged(navigation.CurrentRoute);
            OnStateChanged(navigation.State);
        }

        private void OnDisable()
        {
            if (navigation == null)
                return;

            navigation.StateChanged -= OnStateChanged;
            navigation.RouteChanged -= OnRouteChanged;
        }

        // Instruction and progress change with every pose update, so they are polled.
        private void Update()
        {
            if (CurrentScreen == NavigationScreen.Navigation)
                RefreshHud();
        }

        public void StopNavigation() => navigation?.StopNavigation();

        public void RestartNavigation() => navigation?.RestartNavigation();

        public void ToggleHelp() => SetHelpVisible(!IsHelpOpen);

        public void CloseHelp() => SetHelpVisible(false);

        private void SetHelpVisible(bool visible)
        {
            if (helpScreen != null)
                helpScreen.SetActive(visible);
        }

        private void OnRouteChanged(PathResult route)
        {
            destinationName = LookUpDestinationName(route);

            if (destinationText != null)
                destinationText.text = NavigationUIText.DestinationCaption(destinationName);
        }

        private void OnStateChanged(NavigationState state)
        {
            CurrentScreen = NavigationUIText.ScreenFor(state);

            SetActive(destinationScreen, CurrentScreen == NavigationScreen.DestinationSelection);
            SetActive(navigationScreen, CurrentScreen == NavigationScreen.Navigation);
            SetActive(arrivedScreen, CurrentScreen == NavigationScreen.Arrived);

            ShowBanner(NavigationUIText.BannerFor(state));

            if (arrivedText != null)
                arrivedText.text = NavigationUIText.ArrivedMessage(destinationName);

            if (CurrentScreen == NavigationScreen.Navigation)
                RefreshHud();
        }

        private void RefreshHud()
        {
            NavigationInstruction instruction = navigation.CurrentInstruction;

            if (instructionText != null)
                instructionText.text = instruction.Text;

            if (turnIcon != null)
                turnIcon.localEulerAngles = new Vector3(0f, 0f, NavigationUIText.IconRotation(instruction.Direction));

            if (turnIconImage != null)
            {
                Sprite sprite = instruction.Direction == TurnDirection.Arrive && arriveSprite != null
                    ? arriveSprite
                    : turnArrowSprite;

                if (turnIconImage.sprite != sprite)
                    turnIconImage.sprite = sprite;
            }

            if (progressText != null)
                progressText.text = NavigationUIText.FormatProgress(navigation.RemainingDistance, navigation.Progress);

            if (progressFill != null)
                progressFill.anchorMax = new Vector2(Mathf.Clamp01(navigation.Progress), progressFill.anchorMax.y);
        }

        private void ShowBanner(StatusBanner status)
        {
            SetActive(banner, status.Visible);

            if (!status.Visible)
                return;

            if (bannerTitle != null)
                bannerTitle.text = status.Title;

            if (bannerMessage != null)
                bannerMessage.text = status.Message;

            if (bannerBackground != null)
                bannerBackground.color = status.Severity == BannerSeverity.Warning ? warningColor : infoColor;
        }

        private string LookUpDestinationName(PathResult route)
        {
            if (route == null || route.NodeIds.Count == 0)
                return string.Empty;

            string id = route.NodeIds[route.NodeIds.Count - 1];
            CampusNode node = pathfinding?.GetDestinations().FirstOrDefault(n => n.id == id);
            return node != null ? node.displayName : id;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
                target.SetActive(active);
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }
    }
}
