using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace Luan.LuckyWheel.Editor
{
    public static class LuckyWheelSceneBuilder
    {
        private const string Root = "Assets/Luan/LuckyWheel/Sprites/";
        private const float Scale = 1920f / 1672f;

        [MenuItem("Tools/Luan/Build Lucky Wheel UI")]
        public static void Build()
        {
            ConfigureSprites();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.015f, 0.008f, 0.06f, 1f);
            camera.orthographic = true;
            cameraGo.transform.position = new Vector3(0, 0, -10);

            var canvasGo = new GameObject("LuckyWheelCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            AddImage(canvasGo.transform, "Background", "Background.png", new Vector2(836, 470.5f), new Vector2(1672, 941), false);

            var decoration = AddGroup(canvasGo.transform, "Decorations");
            AddImage(decoration, "Characters", "Characters.png", new Vector2(266, 493), new Vector2(532, 650), false);
            AddImage(decoration, "TitleLogo", "TitleLogo.png", new Vector2(748, 127), new Vector2(724, 250), false);

            var topBar = AddGroup(canvasGo.transform, "TopBar");
            AddButton(topBar, "BackButton", "BackButton.png", new Vector2(48, 51), new Vector2(74, 76));
            AddImage(topBar, "PageTitle", "PageTitle.png", new Vector2(174, 48), new Vector2(205, 67), false);
            AddImage(topBar, "Currencies", "Currencies.png", new Vector2(1397, 45), new Vector2(406, 70), false);
            AddButton(topBar, "SettingsButton", "SettingsButton.png", new Vector2(1620, 46), new Vector2(68, 70));

            var wheel = AddGroup(canvasGo.transform, "WheelArea");
            AddImage(wheel, "Wheel", "WheelInner12_NeonSeparate.png", new Vector2(828, 575), new Vector2(675, 675), false);
            var centerButton = AddButton(wheel, "SpinCenterButton", "SpinCenterButton.png", new Vector2(828, 575), new Vector2(170, 170));
            centerButton.image.sprite = null;
            centerButton.image.color = Color.clear;
            var controller = wheel.gameObject.AddComponent<Luan.LuckyWheel.LuckyWheelController>();
            var icons = new Sprite[12];
            var iconNames = new[] { "01_GoldSmall", "02_Diamond", "03_CharacterBlue", "04_Jacket", "05_PremiumChest", "06_Ticket1", "07_Energy", "08_RareCrystal", "09_GoldLarge", "10_CharacterPink", "11_Ticket2", "12_WeaponSkin" };
            for (var i = 0; i < iconNames.Length; i++)
                icons[i] = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "ModularItems/" + iconNames[i] + ".png");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var fontMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat");
            controller.SetDefaultPrizes(icons, font, fontMat);
            controller.ApplyReadablePresentation();
            controller.RebuildItemViews();
            InstallSeparateWheelArt(wheel, wheel.Find("Wheel") as RectTransform);
            AddImage(wheel, "WheelPointer", "WheelPointer_Modular.png", new Vector2(828, 258), new Vector2(92, 122), false);

            var right = AddGroup(canvasGo.transform, "RightPanel");
            AddImage(right, "FeaturedRewards", "FeaturedRewards.png", new Vector2(1417, 371), new Vector2(507, 361), false);
            AddImage(right, "SpinInfo", "SpinInfo.png", new Vector2(1415, 625), new Vector2(474, 156), false);
            AddButton(right, "SpinX1Button", "SpinButtonBlue_Empty.png", new Vector2(1270, 778), new Vector2(280, 111));
            AddButton(right, "SpinX10Button", "SpinButtonPink_Empty.png", new Vector2(1519, 778), new Vector2(300, 111));
            BuildPurchaseUI(right);

            var footer = AddGroup(canvasGo.transform, "Footer");
            AddImage(footer, "BonusProgress", "BonusProgress.png", new Vector2(288, 845), new Vector2(555, 126), false);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EditorSceneManager.SaveScene(scene, "Assets/Luan/Scenes/LuckyWheel.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("LuckyWheel Canvas scene created: Assets/Luan/Scenes/LuckyWheel.unity");
        }

        [MenuItem("Tools/Luan/Sửa lỗi lệch trục mâm quay (Fix Wheel Axis Centering)")]
        [MenuItem("Tools/Luan/Sửa lại UI item trong vòng quay (Fix Wheel Items UI)")]
        public static void FixWheelAxisCentering()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Dừng Play Mode trước khi sửa.");
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.path.Contains("LuckyWheel"))
            {
                EditorSceneManager.OpenScene("Assets/Luan/Scenes/LuckyWheel.unity");
            }

            ConfigureSprite(Root + "WheelInner12_NeonSeparate.png");
            ConfigureSprite(Root + "WheelRim_NeonSeparate.png");
            ConfigureSprite(Root + "WheelHub_Fixed.png");
            ConfigureSprite(Root + "WheelBase_Modular.png");

            var controller = Object.FindFirstObjectByType<Luan.LuckyWheel.LuckyWheelController>();
            if (controller == null)
            {
                Debug.LogError("Không tìm thấy LuckyWheelController trong scene!");
                return;
            }

            var wheel = controller.transform.Find("Wheel") as RectTransform;
            if (wheel != null)
            {
                wheel.localRotation = Quaternion.identity;
            }
            var fontMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat");
            var so = new SerializedObject(controller);
            var matProp = so.FindProperty("itemFontMaterial");
            if (matProp != null)
            {
                matProp.objectReferenceValue = fontMat;
                so.ApplyModifiedProperties();
            }

            InstallSeparateWheelArt(controller.transform, wheel);
            controller.ApplyReadablePresentation();
            controller.RebuildItemViews();

            var centerButton = controller.transform.Find("SpinCenterButton")?.GetComponent<Button>();
            if (centerButton != null && wheel != null)
            {
                var rect = centerButton.GetComponent<RectTransform>();
                rect.anchoredPosition = wheel.anchoredPosition;
                rect.sizeDelta = new Vector2(170, 170) * Scale;
            }

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
            Debug.Log("Đã căn chỉnh trục mâm quay hoàn hảo và lưu scene thành công!");
        }

        [MenuItem("Tools/Luan/Update Lucky Wheel Presentation")]
        public static void UpdatePresentation()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before updating the Lucky Wheel scene.");
                return;
            }

            var controller = Object.FindFirstObjectByType<Luan.LuckyWheel.LuckyWheelController>();
            if (controller == null)
            {
                Debug.LogError("Open the LuckyWheel scene before updating its presentation.");
                return;
            }

            Undo.RecordObject(controller, "Update Lucky Wheel Presentation");
            var wheelRect = controller.transform.Find("Wheel") as RectTransform;
            if (wheelRect != null)
            {
                wheelRect.localRotation = Quaternion.identity;
            }
            var fontMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Outline.mat");
            var so = new SerializedObject(controller);
            var matProp = so.FindProperty("itemFontMaterial");
            if (matProp != null)
            {
                matProp.objectReferenceValue = fontMat;
                so.ApplyModifiedProperties();
            }
            controller.ApplyReadablePresentation();
            controller.RebuildItemViews();

            var centerButton = controller.transform.Find("SpinCenterButton")?.GetComponent<Button>();
            if (centerButton != null && wheelRect != null)
            {
                Undo.RecordObject(centerButton.image, "Remove Center Button Lettering");
                Undo.RecordObject(centerButton.GetComponent<RectTransform>(), "Center Spin Hit Area");
                centerButton.image.sprite = null;
                centerButton.image.color = Color.clear;
                var rect = centerButton.GetComponent<RectTransform>();
                rect.anchoredPosition = wheelRect.anchoredPosition;
                rect.sizeDelta = new Vector2(170, 170) * Scale;
            }

            var pointer = controller.transform.Find("WheelPointer") as RectTransform;
            if (pointer != null)
            {
                Undo.RecordObject(pointer, "Enlarge Prize Pointer");
                pointer.sizeDelta = new Vector2(92, 122) * Scale;
                pointer.anchoredPosition = new Vector2(828 * Scale, -258 * Scale);
            }

            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
            Debug.Log("LuckyWheel presentation updated without resetting prize configuration.");
        }

        [MenuItem("Tools/Luan/Update Lucky Wheel Purchase UI")]
        public static void UpdatePurchaseUI()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before updating the Lucky Wheel UI.");
                return;
            }

            var canvas = Object.FindFirstObjectByType<Canvas>();
            var right = canvas == null ? null : canvas.transform.Find("RightPanel");
            if (right == null)
            {
                Debug.LogError("Open the LuckyWheel scene before updating its purchase buttons.");
                return;
            }

            var oldTen = right.Find("SpinX5Button");
            if (oldTen != null) oldTen.name = "SpinX10Button";
            ConfigureSprite(Root + "DiscountBadge_Neon.png");
            ConfigureSprite(Root + "TicketOne_Clean.png");
            ConfigureSprite(Root + "TicketTen_x10.png");
            ConfigureSprite(Root + "DiamondPricePlate_Brush.png");
            SetPurchaseButton(right, "SpinX1Button", "SpinButtonBlue_Empty.png", new Vector2(1270, 778), new Vector2(280, 111));
            SetPurchaseButton(right, "SpinX10Button", "SpinButtonPink_Empty.png", new Vector2(1519, 778), new Vector2(300, 111));
            BuildPurchaseUI(right);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
        }

        [MenuItem("Tools/Luan/Install Separate Lucky Wheel Art")]
        public static void InstallCurrentWheelArt()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before replacing the Lucky Wheel art.");
                return;
            }

            var controller = Object.FindFirstObjectByType<Luan.LuckyWheel.LuckyWheelController>();
            var wheel = controller == null ? null : controller.transform.Find("Wheel") as RectTransform;
            if (wheel == null)
            {
                Debug.LogError("Open the LuckyWheel scene before replacing its wheel art.");
                return;
            }

            ConfigureSprite(Root + "WheelInner12_NeonSeparate.png");
            ConfigureSprite(Root + "WheelRim_NeonSeparate.png");
            ConfigureSprite(Root + "WheelHub_Fixed.png");
            InstallSeparateWheelArt(controller.transform, wheel);
            controller.ApplyReadablePresentation();
            controller.RebuildItemViews();
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
            Debug.Log("LuckyWheel now uses separate transparent inner-disc and fixed-rim sprites.");
        }

        private static void InstallSeparateWheelArt(Transform wheelArea, RectTransform wheel)
        {
            if (wheel == null) return;
            var image = wheel.GetComponent<Image>();
            var innerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "WheelInner12_NeonSeparate.png");
            var rimSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "WheelRim_NeonSeparate.png");
            var hubSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "WheelHub_Fixed.png");
            if (image == null || innerSprite == null || rimSprite == null || hubSprite == null)
            {
                Debug.LogError("The separate LuckyWheel sprites are missing or are not imported as sprites.");
                return;
            }

            var oldSectors = wheel.Find("WheelSectors");
            if (oldSectors != null) Object.DestroyImmediate(oldSectors.gameObject);
            var oldRim = wheelArea.Find("WheelRim");
            if (oldRim != null) Object.DestroyImmediate(oldRim.gameObject);
            var oldHub = wheelArea.Find("WheelHub");
            if (oldHub != null) Object.DestroyImmediate(oldHub.gameObject);

            image.sprite = innerSprite;
            image.enabled = true;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;
            EditorUtility.SetDirty(image);

            var rim = new GameObject("WheelRim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rim.transform.SetParent(wheelArea, false);
            var rimRect = rim.GetComponent<RectTransform>();
            rimRect.anchorMin = wheel.anchorMin;
            rimRect.anchorMax = wheel.anchorMax;
            rimRect.pivot = wheel.pivot;
            rimRect.anchoredPosition = wheel.anchoredPosition;
            rimRect.sizeDelta = wheel.sizeDelta;
            rim.transform.SetSiblingIndex(wheel.GetSiblingIndex() + 1);
            var rimImage = rim.GetComponent<Image>();
            rimImage.sprite = rimSprite;
            rimImage.preserveAspect = true;
            rimImage.raycastTarget = false;

            var hub = new GameObject("WheelHub", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            hub.transform.SetParent(wheelArea, false);
            var hubRect = hub.GetComponent<RectTransform>();
            hubRect.anchorMin = wheel.anchorMin;
            hubRect.anchorMax = wheel.anchorMax;
            hubRect.pivot = wheel.pivot;
            hubRect.anchoredPosition = wheel.anchoredPosition;
            hubRect.sizeDelta = wheel.sizeDelta * 0.38f;
            hub.transform.SetSiblingIndex(rim.transform.GetSiblingIndex() + 1);
            var hubImage = hub.GetComponent<Image>();
            hubImage.sprite = hubSprite;
            hubImage.preserveAspect = true;
            hubImage.raycastTarget = false;
        }

        private static void SetPurchaseButton(Transform right, string name, string spriteName, Vector2 center, Vector2 size)
        {
            var button = right.Find(name)?.GetComponent<Button>();
            if (button == null) return;
            var image = button.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + spriteName);
            image.preserveAspect = true;
            image.color = Color.white;
            var rect = button.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(center.x * Scale, -center.y * Scale);
            rect.sizeDelta = size * Scale;
            EditorUtility.SetDirty(button);
        }

        private static void BuildPurchaseUI(Transform right)
        {
            var names = new[] { "TicketOneIcon", "TicketTenIcon", "SpinOneTitle", "SpinTenTitle", "PriceOnePanel", "PriceTenPanel", "DiamondOneIcon", "DiamondTenIcon", "PriceOneLabel", "PriceTenLabel", "DiscountBadge", "DiscountBadgeShadow", "DiscountBadgeBorder", "DiscountLabel" };
            foreach (var name in names)
            {
                var existing = right.Find(name);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
            }

            var numberFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset");
            var comicFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Thuan/UI/Fonts/Bangers SDF.asset");
            AddImage(right, "TicketOneIcon", "TicketOne_Clean.png", new Vector2(1187, 775), new Vector2(94, 94), false);
            AddImage(right, "TicketTenIcon", "TicketTen_x10.png", new Vector2(1420, 775), new Vector2(94, 94), false);
            var spinOneTitle = AddText(right, "SpinOneTitle", "QUAY x1", comicFont, new Vector2(1285, 774), new Vector2(145, 58), 37, Color.white);
            var spinTenTitle = AddText(right, "SpinTenTitle", "QUAY x10", comicFont, new Vector2(1530, 774), new Vector2(145, 58), 33, Color.white);
            spinOneTitle.enableAutoSizing = spinTenTitle.enableAutoSizing = false;
            spinOneTitle.overflowMode = spinTenTitle.overflowMode = TextOverflowModes.Truncate;

            AddImage(right, "PriceOnePanel", "DiamondPricePlate_Brush.png", new Vector2(1268, 854), new Vector2(225, 70), false);
            AddImage(right, "PriceTenPanel", "DiamondPricePlate_Brush.png", new Vector2(1515, 854), new Vector2(225, 70), false);
            AddImage(right, "DiamondOneIcon", "ModularItems/02_Diamond.png", new Vector2(1217, 858), new Vector2(70, 70), false);
            AddImage(right, "DiamondTenIcon", "ModularItems/02_Diamond.png", new Vector2(1455, 858), new Vector2(70, 70), false);
            var one = AddText(right, "PriceOneLabel", "50", numberFont, new Vector2(1290, 860), new Vector2(98, 42), 36, Color.white);
            var ten = AddText(right, "PriceTenLabel", "400", numberFont, new Vector2(1523, 860), new Vector2(92, 42), 36, Color.white);
            one.fontSizeMin = ten.fontSizeMin = 18 * Scale;
            one.overflowMode = ten.overflowMode = TextOverflowModes.Ellipsis;

            var badge = AddImage(right, "DiscountBadge", "DiscountBadge_Neon.png", new Vector2(1618, 844), new Vector2(95, 58), false);
            var discount = AddText(right, "DiscountLabel", "Tiết kiệm\n20%", numberFont, new Vector2(1618, 844), new Vector2(74, 42), 17, new Color(0.15f, 0.025f, 0.15f));
            discount.rectTransform.localRotation = badge.rectTransform.localRotation;

            var purchase = right.GetComponent<Luan.LuckyWheel.LuckyWheelPurchaseUI>();
            if (purchase == null) purchase = right.gameObject.AddComponent<Luan.LuckyWheel.LuckyWheelPurchaseUI>();
            purchase.Configure(one, ten, discount);
        }

        private static Image AddSolidPanel(Transform parent, string name, Vector2 center, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(center.x * Scale, -center.y * Scale);
            rect.sizeDelta = size * Scale;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI AddText(Transform parent, string name, string value, TMP_FontAsset font, Vector2 center, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(center.x * Scale, -center.y * Scale);
            rect.sizeDelta = size * Scale;
            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = value;
            label.font = font;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = fontSize * Scale;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableAutoSizing = true;
            label.fontSizeMin = fontSize * Scale * 0.75f;
            label.fontSizeMax = fontSize * Scale;
            label.raycastTarget = false;
            return label;
        }

        private static Transform AddGroup(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go.transform;
        }

        private static Image AddImage(Transform parent, string name, string file, Vector2 sourceCenter, Vector2 sourceSize, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(sourceCenter.x * Scale, -sourceCenter.y * Scale);
            rt.sizeDelta = sourceSize * Scale;
            var image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + file);
            image.preserveAspect = true;
            image.raycastTarget = raycast;
            return image;
        }

        private static Button AddButton(Transform parent, string name, string file, Vector2 center, Vector2 size)
        {
            var image = AddImage(parent, name, file, center, size, true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            button.colors = colors;
            return button;
        }

        private static void ConfigureSprites()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root.TrimEnd('/') }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                ConfigureSprite(path);
            }
        }

        private static void ConfigureSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }
    }
}
