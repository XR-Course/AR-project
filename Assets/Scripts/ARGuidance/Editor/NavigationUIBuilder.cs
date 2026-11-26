using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CampusNavigation.ARGuidance.Editor
{
    /// <summary>
    /// Generates the NavigationCanvas prefab (all screens of the AR guidance UI) and its
    /// icon sprites. Rerun after changing the layout code; hand edits to the prefab are
    /// overwritten. Menu: Campus Navigation > AR Guidance > Build UI Prefab.
    /// Layout targets a portrait phone (reference 1080 x 1920).
    /// </summary>
    public static class NavigationUIBuilder
    {
        public const string Root = "Assets/ARGuidance";
        public const string PrefabPath = Root + "/Prefabs/NavigationCanvas.prefab";
        public const string ArrowSpritePath = Root + "/Sprites/TurnArrow.png";
        public const string TargetSpritePath = Root + "/Sprites/DestinationTarget.png";

        private static readonly Color PanelColor = new(0.07f, 0.09f, 0.13f, 0.93f);
        private static readonly Color TextColor = Color.white;
        private static readonly Color MutedColor = new(1f, 1f, 1f, 0.65f);
        private static readonly Color AccentColor = new(0.1f, 0.7f, 1f, 1f);
        private static readonly Color PrimaryColor = new(0.12f, 0.6f, 0.35f, 1f);
        private static readonly Color DangerColor = new(0.78f, 0.22f, 0.2f, 1f);
        private static readonly Color SecondaryColor = new(1f, 1f, 1f, 0.16f);

        private const string HelpText =
            "1. Choose a destination and tap <b>Start navigation</b>.\n" +
            "2. Scan the nearest campus marker with your camera.\n" +
            "3. Follow the arrows on the floor. <color=#FFCC1A>Yellow</color> arrows lead to your next turn.\n" +
            "4. If the arrows drift or tracking is lost, scan any marker again to recalibrate.\n" +
            "5. You can stop or restart the route at any time.\n\n" +
            "<b>Safety:</b> " + NavigationUIText.SafetyReminder;

        private static Sprite roundedSprite;
        private static Sprite circleSprite;

        [MenuItem("Campus Navigation/AR Guidance/Build UI Prefab")]
        public static void BuildMenu()
        {
            GameObject prefab = Build();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        public static GameObject Build()
        {
            roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            Sprite arrowSprite = IconSpriteGenerator.CreateTurnArrow(ArrowSpritePath);
            Sprite targetSprite = IconSpriteGenerator.CreateDestinationTarget(TargetSpritePath);

            GameObject canvasObject = CreateCanvas();

            // Every screen lives inside the safe area (notches, home indicator).
            RectTransform safeArea = Rect("SafeArea", canvasObject.transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            // Centered column: phone-width UI even in landscape / on tablets.
            RectTransform column = Rect("Column", safeArea);
            Stretch(column);
            column.gameObject.AddComponent<AdaptiveLayout>();
            Transform root = column;

            BuildDestinationScreen(root, out GameObject destinationScreen);
            GameObject navigationScreen = BuildNavigationScreen(root, arrowSprite, out NavigationHud hud);
            GameObject arrivedScreen = BuildArrivedScreen(root, targetSprite, out TMP_Text arrivedText,
                out Button newDestinationButton, out Button arrivedRestartButton);
            GameObject banner = BuildBanner(root, out Image bannerBackground, out TMP_Text bannerTitle,
                out TMP_Text bannerMessage);
            Button helpButton = BuildHelpButton(root);
            GameObject helpScreen = BuildHelpScreen(root, out Button closeHelpButton);

            var ui = canvasObject.AddComponent<NavigationUI>();
            var so = new SerializedObject(ui);
            Set(so, "destinationScreen", destinationScreen);
            Set(so, "navigationScreen", navigationScreen);
            Set(so, "arrivedScreen", arrivedScreen);
            Set(so, "helpScreen", helpScreen);
            Set(so, "banner", banner);
            Set(so, "bannerBackground", bannerBackground);
            Set(so, "bannerTitle", bannerTitle);
            Set(so, "bannerMessage", bannerMessage);
            Set(so, "destinationText", hud.Destination);
            Set(so, "instructionText", hud.Instruction);
            Set(so, "turnIcon", hud.Icon.rectTransform);
            Set(so, "turnIconImage", hud.Icon);
            Set(so, "turnArrowSprite", arrowSprite);
            Set(so, "arriveSprite", targetSprite);
            Set(so, "progressText", hud.ProgressText);
            Set(so, "progressFill", hud.ProgressFill);
            Set(so, "stopButton", hud.Stop);
            Set(so, "restartButton", hud.Restart);
            Set(so, "helpButton", helpButton);
            Set(so, "closeHelpButton", closeHelpButton);
            Set(so, "arrivedText", arrivedText);
            Set(so, "newDestinationButton", newDestinationButton);
            Set(so, "arrivedRestartButton", arrivedRestartButton);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Default visibility: the UI script switches screens from the navigation state.
            navigationScreen.SetActive(false);
            arrivedScreen.SetActive(false);
            banner.SetActive(false);
            helpScreen.SetActive(false);

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath)!);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasObject, PrefabPath);
            Object.DestroyImmediate(canvasObject);

            Debug.Log($"AR Guidance UI prefab written to {PrefabPath}");
            return prefab;
        }

        // ----- Screens -----

        private static GameObject CreateCanvas()
        {
            var go = new GameObject("NavigationCanvas", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // AdaptiveLayout switches to height in landscape.

            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        private static DestinationSelector BuildDestinationScreen(Transform parent, out GameObject screen)
        {
            RectTransform sheet = BottomSheet("DestinationScreen", parent);
            screen = sheet.gameObject;

            Label("Title", sheet, "Where do you want to go?", 54, TextColor, FontStyles.Bold, TextAlignmentOptions.Left);

            // Scrollable list
            RectTransform list = Rect("List", sheet);
            AddImage(list, new Color(0f, 0f, 0f, 0.001f), null);
            Layout(list.gameObject, preferredHeight: 560f);

            RectTransform viewport = Rect("Viewport", list);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            VerticalLayout(content.gameObject, 0, 16f);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewport;
            scroll.content = content;

            DestinationListItem template = BuildListItem(content);

            TMP_Text summary = Label("Summary", sheet, "Choose where you want to go.", 36, MutedColor,
                FontStyles.Normal, TextAlignmentOptions.Left);

            Button start = MakeButton("StartButton", sheet, "Start navigation", PrimaryColor, 150f, 48f);

            var selector = screen.AddComponent<DestinationSelector>();
            var so = new SerializedObject(selector);
            Set(so, "listContainer", content);
            Set(so, "itemTemplate", template);
            Set(so, "startButton", start);
            Set(so, "summaryText", summary);
            so.ApplyModifiedPropertiesWithoutUndo();

            return selector;
        }

        private static DestinationListItem BuildListItem(Transform parent)
        {
            RectTransform item = Rect("ItemTemplate", parent);
            Image background = AddImage(item, new Color(1f, 1f, 1f, 0.08f), roundedSprite);
            var button = item.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            Layout(item.gameObject, preferredHeight: 130f);

            TMP_Text name = Label("Name", item, "Destination", 46, TextColor, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft, addLayout: false);
            RectTransform nameRect = name.rectTransform;
            Stretch(nameRect);
            nameRect.offsetMin = new Vector2(36f, 0f);
            nameRect.offsetMax = new Vector2(-340f, 0f);

            TMP_Text detail = Label("Detail", item, "0 m", 36, MutedColor, FontStyles.Normal,
                TextAlignmentOptions.MidlineRight, addLayout: false);
            RectTransform detailRect = detail.rectTransform;
            detailRect.anchorMin = new Vector2(1f, 0f);
            detailRect.anchorMax = new Vector2(1f, 1f);
            detailRect.pivot = new Vector2(1f, 0.5f);
            detailRect.anchoredPosition = new Vector2(-36f, 0f);
            detailRect.sizeDelta = new Vector2(320f, 0f);

            var listItem = item.gameObject.AddComponent<DestinationListItem>();
            var so = new SerializedObject(listItem);
            Set(so, "button", button);
            Set(so, "background", background);
            Set(so, "nameText", name);
            Set(so, "detailText", detail);
            so.ApplyModifiedPropertiesWithoutUndo();

            item.gameObject.SetActive(false);
            return listItem;
        }

        private struct NavigationHud
        {
            public TMP_Text Destination;
            public TMP_Text Instruction;
            public Image Icon;
            public TMP_Text ProgressText;
            public RectTransform ProgressFill;
            public Button Stop;
            public Button Restart;
        }

        private static GameObject BuildNavigationScreen(Transform parent, Sprite arrowSprite, out NavigationHud hud)
        {
            RectTransform sheet = BottomSheet("NavigationScreen", parent);
            hud = default;

            RectTransform row = Rect("InstructionRow", sheet);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 32f;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            Layout(row.gameObject, preferredHeight: 180f);

            // The icon sits in a fixed slot so rotating it doesn't disturb the layout.
            RectTransform iconSlot = Rect("TurnIconSlot", row);
            AddImage(iconSlot, AccentColor, circleSprite);
            Layout(iconSlot.gameObject, preferredWidth: 180f, preferredHeight: 180f);

            RectTransform icon = Rect("TurnIcon", iconSlot);
            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
            icon.sizeDelta = new Vector2(130f, 130f);
            hud.Icon = AddImage(icon, Color.white, arrowSprite);
            hud.Icon.preserveAspect = true;

            hud.Instruction = Label("InstructionText", row, "Go straight for 10 m", 60, TextColor, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft, addLayout: false);
            Layout(hud.Instruction.gameObject, flexibleWidth: 1f);

            hud.Destination = Label("DestinationText", sheet, "Destination", 38, MutedColor, FontStyles.Normal,
                TextAlignmentOptions.Left);

            RectTransform bar = Rect("ProgressBar", sheet);
            AddImage(bar, SecondaryColor, roundedSprite);
            Layout(bar.gameObject, preferredHeight: 22f);

            hud.ProgressFill = Rect("Fill", bar);
            hud.ProgressFill.anchorMin = Vector2.zero;
            hud.ProgressFill.anchorMax = new Vector2(0.3f, 1f);
            hud.ProgressFill.offsetMin = hud.ProgressFill.offsetMax = Vector2.zero;
            AddImage(hud.ProgressFill, AccentColor, roundedSprite);

            hud.ProgressText = Label("ProgressText", sheet, "12 m left · 33%", 36, TextColor, FontStyles.Normal,
                TextAlignmentOptions.Left);

            RectTransform buttons = ButtonRow("Buttons", sheet);
            hud.Restart = MakeButton("RestartButton", buttons, "Restart", SecondaryColor, 130f);
            hud.Stop = MakeButton("StopButton", buttons, "Stop", DangerColor, 130f);

            Label("SafetyText", sheet, NavigationUIText.SafetyReminder, 30, MutedColor, FontStyles.Italic,
                TextAlignmentOptions.Center);

            return sheet.gameObject;
        }

        private static GameObject BuildArrivedScreen(Transform parent, Sprite targetSprite, out TMP_Text message,
            out Button newDestination, out Button restart)
        {
            RectTransform sheet = BottomSheet("ArrivedScreen", parent);
            sheet.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;

            RectTransform icon = Rect("Icon", sheet);
            Image image = AddImage(icon, PrimaryColor, targetSprite);
            image.preserveAspect = true;
            Layout(icon.gameObject, preferredHeight: 160f);

            message = Label("ArrivedText", sheet, "You have arrived", 58, TextColor, FontStyles.Bold,
                TextAlignmentOptions.Center);

            RectTransform buttons = ButtonRow("Buttons", sheet);
            restart = MakeButton("RestartButton", buttons, "Restart route", SecondaryColor, 140f);
            newDestination = MakeButton("NewDestinationButton", buttons, "New destination", PrimaryColor, 140f);

            return sheet.gameObject;
        }

        private static GameObject BuildBanner(Transform parent, out Image background, out TMP_Text title,
            out TMP_Text message)
        {
            RectTransform banner = Rect("StatusBanner", parent);
            banner.anchorMin = new Vector2(0f, 1f);
            banner.anchorMax = new Vector2(1f, 1f);
            banner.pivot = new Vector2(0.5f, 1f);

            // 32 px left margin, 184 px right margin (room for the help button).
            banner.sizeDelta = new Vector2(-216f, 0f);
            banner.anchoredPosition = new Vector2(-76f, -40f);

            background = AddImage(banner, new Color(0.1f, 0.35f, 0.75f, 0.92f), roundedSprite);
            VerticalLayout(banner.gameObject, 32, 8f);
            banner.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            title = Label("Title", banner, "Scan a marker", 44, TextColor, FontStyles.Bold, TextAlignmentOptions.Left);
            message = Label("Message", banner, "Point your camera at the nearest campus marker.", 34, TextColor,
                FontStyles.Normal, TextAlignmentOptions.Left);

            return banner.gameObject;
        }

        private static Button BuildHelpButton(Transform parent)
        {
            RectTransform rect = Rect("HelpButton", parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-40f, -40f);
            rect.sizeDelta = new Vector2(120f, 120f);

            Image image = AddImage(rect, PanelColor, circleSprite);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = Label("Label", rect, "?", 64, TextColor, FontStyles.Bold, TextAlignmentOptions.Center,
                addLayout: false);
            Stretch(label.rectTransform);
            return button;
        }

        private static GameObject BuildHelpScreen(Transform parent, out Button close)
        {
            RectTransform overlay = Rect("HelpScreen", parent);
            Stretch(overlay);

            // The dim layer sits inside the safe-area column; extend it to cover the whole screen.
            overlay.offsetMin = new Vector2(-4000f, -400f);
            overlay.offsetMax = new Vector2(4000f, 400f);
            AddImage(overlay, new Color(0f, 0f, 0f, 0.7f), null);

            RectTransform panel = Rect("Panel", overlay);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(960f, 0f);
            AddImage(panel, PanelColor, roundedSprite);
            VerticalLayout(panel.gameObject, 48, 32f);
            panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Label("Title", panel, "How to use", 56, TextColor, FontStyles.Bold, TextAlignmentOptions.Left);
            Label("Body", panel, HelpText, 36, TextColor, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            close = MakeButton("CloseButton", panel, "Got it", PrimaryColor, 140f);

            return overlay.gameObject;
        }

        // ----- Building blocks -----

        private static RectTransform BottomSheet(string name, Transform parent)
        {
            RectTransform sheet = Rect(name, parent);
            sheet.anchorMin = new Vector2(0f, 0f);
            sheet.anchorMax = new Vector2(1f, 0f);
            sheet.pivot = new Vector2(0.5f, 0f);
            sheet.anchoredPosition = new Vector2(0f, 32f);
            sheet.sizeDelta = new Vector2(-64f, 0f);

            AddImage(sheet, PanelColor, roundedSprite);
            VerticalLayout(sheet.gameObject, 40, 24f);
            sheet.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return sheet;
        }

        private static RectTransform ButtonRow(string name, Transform parent)
        {
            RectTransform row = Rect(name, parent);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        private static Button MakeButton(string name, Transform parent, string text, Color color, float height,
            float fontSize = 44f)
        {
            RectTransform rect = Rect(name, parent);
            Image image = AddImage(rect, color, roundedSprite);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Layout(rect.gameObject, preferredHeight: height, flexibleWidth: 1f);

            TMP_Text label = Label("Label", rect, text, fontSize, TextColor, FontStyles.Bold,
                TextAlignmentOptions.Center, addLayout: false);
            Stretch(label.rectTransform);
            return button;
        }

        private static TMP_Text Label(string name, Transform parent, string text, float size, Color color,
            FontStyles style, TextAlignmentOptions alignment, bool addLayout = true)
        {
            RectTransform rect = Rect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = style;
            label.alignment = alignment;
            label.raycastTarget = false;

            if (addLayout)
                Layout(rect.gameObject);

            return label;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Image AddImage(RectTransform rect, Color color, Sprite sprite)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;

            if (sprite != null && sprite.border != Vector4.zero)
                image.type = Image.Type.Sliced;

            return image;
        }

        private static void VerticalLayout(GameObject go, int padding, float spacing)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void Layout(GameObject go, float preferredHeight = -1f, float preferredWidth = -1f,
            float flexibleWidth = -1f)
        {
            // Not "??": Unity's fake-null from GetComponent would slip through.
            if (!go.TryGetComponent(out LayoutElement element))
                element = go.AddComponent<LayoutElement>();

            element.preferredHeight = preferredHeight;
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;

            if (preferredHeight > 0f)
                element.minHeight = preferredHeight;
        }

        private static void Set(SerializedObject so, string field, Object value)
        {
            SerializedProperty property = so.FindProperty(field);

            if (property == null)
            {
                Debug.LogError($"NavigationUIBuilder: field '{field}' not found on {so.targetObject.GetType().Name}");
                return;
            }

            property.objectReferenceValue = value;
        }
    }
}
