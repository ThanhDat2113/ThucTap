using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Luan.LuckyWheel.Editor
{
    public static class LuckyWheelRewardPopupBuilder
    {
        [MenuItem("Tools/Luan/Preview Reward Popup X1")]
        public static void PreviewOne() => Preview(false);

        [MenuItem("Tools/Luan/Preview Reward Popup X10")]
        public static void PreviewTen() => Preview(true);

        [MenuItem("Tools/Luan/Hide Reward Popups")]
        public static void HideBoth()
        {
            const string scenePath = "Assets/Luan/Scenes/LuckyWheel.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath);
            var canvas = GameObject.Find("LuckyWheelCanvas")?.transform;
            var one = canvas?.Find("RewardPopup");
            var ten = canvas?.Find("RewardPopupX10");
            if (one != null) SetPreview(one, false);
            if (ten != null) SetPreview(ten, false);
            if (one != null)
            {
                EditorSceneManager.MarkSceneDirty(one.gameObject.scene);
                EditorSceneManager.SaveScene(one.gameObject.scene);
            }
        }

        private static void Preview(bool tenVisible)
        {
            const string scenePath = "Assets/Luan/Scenes/LuckyWheel.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath);
            var canvas = GameObject.Find("LuckyWheelCanvas")?.transform;
            var one = canvas?.Find("RewardPopup");
            var ten = canvas?.Find("RewardPopupX10");
            if (one == null || ten == null)
            {
                Debug.LogError("Chưa có đủ RewardPopup và RewardPopupX10 trong scene.");
                return;
            }
            SetPreview(one, !tenVisible);
            SetPreview(ten, tenVisible);
            EditorSceneManager.MarkSceneDirty(one.gameObject.scene);
            EditorSceneManager.SaveScene(one.gameObject.scene);
            Selection.activeGameObject = tenVisible ? ten.gameObject : one.gameObject;
        }

        private static void SetPreview(Transform popup, bool visible)
        {
            popup.gameObject.SetActive(true);
            var group = popup.GetComponent<CanvasGroup>();
            if (group == null) group = popup.gameObject.AddComponent<CanvasGroup>();
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            if (visible) popup.SetAsLastSibling();
        }

        [MenuItem("Tools/Luan/Add Reward Popup To Lucky Wheel")]
        public static void Build()
        {
            const string scenePath = "Assets/Luan/Scenes/LuckyWheel.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath);

            var canvas = GameObject.Find("LuckyWheelCanvas")?.transform;
            var wheel = Object.FindFirstObjectByType<LuckyWheelController>();
            if (canvas == null || wheel == null)
            {
                Debug.LogError("LuckyWheelCanvas hoặc LuckyWheelController không có trong scene.");
                return;
            }

            var existing = canvas.Find("RewardPopup");
            if (existing != null)
            {
                ApplyArt(existing);
                EnsureTenPopup(canvas, existing, wheel);
                var tenPopup = canvas.Find("RewardPopupX10");
                if (tenPopup != null)
                {
                    var tenCard = tenPopup.Find("Card") as RectTransform;
                    tenCard.sizeDelta = new Vector2(1020, 880);
                    SetPosition(tenCard, "ClaimButton", new Vector2(0, -225));
                    var tenTitle = tenCard.Find("Title").GetComponent<TextMeshProUGUI>();
                    tenTitle.fontSize = 38;
                    (tenTitle.transform as RectTransform).sizeDelta = new Vector2(760, 62);
                    for (var i = 1; i <= 10; i++)
                    {
                        var slot = tenCard.Find($"RewardSlot_{i:00}");
                        if (slot == null) continue;
                        var slotAmount = slot.Find("Amount") as RectTransform;
                        if (slotAmount == null) continue;
                        slotAmount.anchoredPosition = new Vector2(0, -29);
                        slotAmount.GetComponent<TextMeshProUGUI>().fontSize = 19;
                    }
                    tenPopup.gameObject.SetActive(true);
                    var group = tenPopup.GetComponent<CanvasGroup>();
                    if (group == null) group = tenPopup.gameObject.AddComponent<CanvasGroup>();
                    group.alpha = 0f;
                    group.blocksRaycasts = false;
                }
                existing.gameObject.SetActive(true);
                existing.SetAsLastSibling();
                EditorSceneManager.MarkSceneDirty(existing.gameObject.scene);
                EditorSceneManager.SaveScene(existing.gameObject.scene);
                Debug.Log("Đã thay sprite khung và nút của RewardPopup; giữ nguyên nội dung và vị trí chỉnh sửa.");
                return;
            }

            var root = Rect(canvas, "RewardPopup", Vector2.zero, Vector2.zero);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            var popup = root.gameObject.AddComponent<LuckyWheelRewardPopup>();
            ImageRect(root, "Dimmer", Vector2.zero, Vector2.zero, new Color(.015f, .008f, .06f, .83f), true).anchorMin = Vector2.zero;
            var dimmer = root.Find("Dimmer") as RectTransform;
            dimmer.anchorMax = Vector2.one;
            dimmer.offsetMin = dimmer.offsetMax = Vector2.zero;

            var shadow = ImageRect(root, "CardShadow", new Vector2(12, -18), new Vector2(680, 600), new Color(.02f, 0f, .11f, .85f));
            var outline = ImageRect(root, "CardOuterGlow", Vector2.zero, new Vector2(680, 600), new Color(.02f, .85f, 1f, .88f));
            var card = ImageRect(root, "Card", Vector2.zero, new Vector2(668, 588), new Color(.025f, .018f, .13f, 1f));
            ImageRect(card, "HeaderAccent", new Vector2(0, 259), new Vector2(632, 6), new Color(1f, .02f, .86f, 1f));
            ImageRect(card, "HeaderGlow", new Vector2(0, 246), new Vector2(632, 2), new Color(.1f, .88f, 1f, .85f));

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Thuan/UI/Fonts/Bangers SDF.asset");
            if (font == null) font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            Label(card, "Title", "PHẦN THƯỞNG CỦA BẠN", font, new Vector2(0, 200), new Vector2(610, 62), 43, Color.white);
            Label(card, "Subtitle", "CHÚC MỪNG BẠN ĐÃ QUAY TRÚNG!", font, new Vector2(0, 152), new Vector2(570, 38), 22, new Color(.36f, .9f, 1f));

            ImageRect(card, "PrizeFrameOuter", new Vector2(0, 8), new Vector2(238, 238), new Color(1f, .1f, .81f, 1f));
            var prizeFrame = ImageRect(card, "PrizeFrameInner", new Vector2(0, 8), new Vector2(228, 228), new Color(.025f, .04f, .2f, 1f));
            var iconRect = ImageRect(prizeFrame, "PrizeIcon", Vector2.zero, new Vector2(166, 166), Color.white);
            var icon = iconRect.GetComponent<Image>();
            icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luan/LuckyWheel/Sprites/ModularItems/02_Diamond.png");
            icon.preserveAspect = true;

            var name = Label(card, "PrizeName", "Kim cương", font, new Vector2(0, -146), new Vector2(565, 52), 35, Color.white);
            var amount = Label(card, "PrizeAmount", "x100", font, new Vector2(0, -190), new Vector2(260, 46), 31, new Color(1f, .87f, .12f));
            var buttonGlow = ImageRect(card, "ClaimGlow", new Vector2(0, -258), new Vector2(326, 68), new Color(.04f, .92f, 1f, 1f));
            var buttonRect = ImageRect(card, "ClaimButton", new Vector2(0, -258), new Vector2(314, 58), new Color(.86f, .02f, .68f, 1f), true);
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonRect.GetComponent<Image>();
            Label(buttonRect, "Label", "NHẬN THƯỞNG", font, Vector2.zero, new Vector2(300, 50), 31, Color.white);

            var serialized = new SerializedObject(popup);
            serialized.FindProperty("wheel").objectReferenceValue = wheel;
            serialized.FindProperty("prizeIcon").objectReferenceValue = icon;
            serialized.FindProperty("prizeName").objectReferenceValue = name;
            serialized.FindProperty("prizeAmount").objectReferenceValue = amount;
            serialized.FindProperty("claimButton").objectReferenceValue = button;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ApplyArt(root);
            EnsureTenPopup(canvas, root, wheel);

            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            EditorSceneManager.SaveScene(root.gameObject.scene);
            Selection.activeGameObject = root.gameObject;
            Debug.Log("Đã thêm RewardPopup. Popup đang hiện trong Scene để chỉnh thiết kế; Play Mode sẽ tự ẩn tới khi quay trúng.");
        }

        private static void ApplyArt(Transform root)
        {
            const string framePath = "Assets/Luan/LuckyWheel/Sprites/RewardPopupFrame_Neon.png";
            const string buttonPath = "Assets/Luan/LuckyWheel/Sprites/RewardClaimButton_Neon.png";
            const string itemPath = "Assets/Luan/LuckyWheel/Sprites/RewardItemFrame_Neon.png";
            var frameSprite = LoadSprite(framePath);
            var buttonSprite = LoadSprite(buttonPath);
            var itemSprite = LoadSprite(itemPath);
            var card = root.Find("Card");
            if (card != null && frameSprite != null)
            {
                var image = card.GetComponent<Image>();
                image.sprite = frameSprite;
                image.color = Color.white;
                (card as RectTransform).sizeDelta = new Vector2(780, 880);
                var oldOutline = root.Find("CardOuterGlow");
                var oldShadow = root.Find("CardShadow");
                var oldHeader = card.Find("HeaderAccent");
                var oldHeaderGlow = card.Find("HeaderGlow");
                if (oldOutline != null) oldOutline.gameObject.SetActive(false);
                if (oldShadow != null) oldShadow.gameObject.SetActive(false);
                if (oldHeader != null) oldHeader.gameObject.SetActive(false);
                if (oldHeaderGlow != null) oldHeaderGlow.gameObject.SetActive(false);
            }
            var button = card?.Find("ClaimButton");
            if (button != null && buttonSprite != null)
            {
                var image = button.GetComponent<Image>();
                image.sprite = buttonSprite;
                image.color = Color.white;
                (button as RectTransform).sizeDelta = new Vector2(365, 84);
                var oldGlow = card.Find("ClaimGlow");
                if (oldGlow != null) oldGlow.gameObject.SetActive(false);
            }
            var oldPrizeOutline = card?.Find("PrizeFrameOuter");
            var prizeFrame = card?.Find("PrizeFrameInner");
            if (oldPrizeOutline != null) oldPrizeOutline.gameObject.SetActive(false);
            if (prizeFrame != null && itemSprite != null)
            {
                prizeFrame.GetComponent<Image>().sprite = itemSprite;
                prizeFrame.GetComponent<Image>().color = Color.white;
                (prizeFrame as RectTransform).sizeDelta = new Vector2(220, 220);
                var icon = prizeFrame.Find("PrizeIcon") as RectTransform;
                if (icon != null) icon.sizeDelta = new Vector2(120, 120);
            }
            SetPosition(card, "Title", new Vector2(0, 203));
            SetPosition(card, "Subtitle", new Vector2(0, 160));
            var title = card?.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (title != null) title.fontSize = 38;
            SetPosition(card, "PrizeName", new Vector2(0, -110));
            SetPosition(card, "PrizeAmount", new Vector2(0, -150));
            SetPosition(card, "ClaimButton", new Vector2(0, -225));
        }

        private static void EnsureTenPopup(Transform canvas, Transform single, LuckyWheelController wheel)
        {
            var existing = canvas.Find("RewardPopupX10");
            if (existing != null) return;
            var ten = Object.Instantiate(single.gameObject, canvas);
            ten.name = "RewardPopupX10";
            ten.transform.SetSiblingIndex(single.GetSiblingIndex());
            var card = ten.transform.Find("Card");
            (card as RectTransform).sizeDelta = new Vector2(1020, 880);
            card.Find("Title").GetComponent<TextMeshProUGUI>().text = "PHẦN THƯỞNG QUAY X10";
            card.Find("Subtitle").GetComponent<TextMeshProUGUI>().text = "10 VẬT PHẨM BẠN NHẬN ĐƯỢC";
            card.Find("PrizeFrameInner").gameObject.SetActive(false);
            card.Find("PrizeName").gameObject.SetActive(false);
            card.Find("PrizeAmount").gameObject.SetActive(false);
            var frameSprite = LoadSprite("Assets/Luan/LuckyWheel/Sprites/RewardItemFrame_Neon.png");
            var font = card.Find("Title").GetComponent<TextMeshProUGUI>().font;
            var icons = new Image[10];
            var labels = new TMP_Text[10];
            for (var i = 0; i < 10; i++)
            {
                var x = (i % 5 - 2) * 145f;
                var y = i < 5 ? 64f : -94f;
                var slot = ImageRect(card, $"RewardSlot_{i + 1:00}", new Vector2(x, y), new Vector2(136, 136), Color.white);
                slot.GetComponent<Image>().sprite = frameSprite;
                var icon = ImageRect(slot, "Icon", new Vector2(0, 12), new Vector2(74, 74), Color.white);
                icons[i] = icon.GetComponent<Image>();
                icons[i].sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luan/LuckyWheel/Sprites/ModularItems/02_Diamond.png");
                icons[i].preserveAspect = true;
                labels[i] = Label(slot, "Amount", "x100", font, new Vector2(0, -29), new Vector2(105, 25), 19, new Color(1f, .87f, .12f));
            }
            var popup = ten.GetComponent<LuckyWheelRewardPopup>();
            var tenGroup = ten.AddComponent<CanvasGroup>();
            tenGroup.alpha = 0f;
            tenGroup.blocksRaycasts = false;
            var serialized = new SerializedObject(popup);
            serialized.FindProperty("wheel").objectReferenceValue = wheel;
            serialized.FindProperty("showTenResults").boolValue = true;
            var iconProperty = serialized.FindProperty("tenIcons");
            var labelProperty = serialized.FindProperty("tenLabels");
            iconProperty.arraySize = labelProperty.arraySize = 10;
            for (var i = 0; i < 10; i++)
            {
                iconProperty.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
                labelProperty.GetArrayElementAtIndex(i).objectReferenceValue = labels[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPosition(Transform parent, string name, Vector2 position)
        {
            var child = parent?.Find(name) as RectTransform;
            if (child != null) child.anchoredPosition = position;
        }

        private static Sprite LoadSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
            return rect;
        }

        private static RectTransform ImageRect(Transform parent, string name, Vector2 center, Vector2 size, Color color, bool raycast = false)
        {
            var rect = Rect(parent, name, center, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return rect;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string value, TMP_FontAsset font, Vector2 center, Vector2 size, float fontSize, Color color)
        {
            var rect = Rect(parent, name, center, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSharedMaterial = font.material;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }
    }
}
