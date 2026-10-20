using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Luan.LuckyWheel;

namespace Luan.MainMenu
{
    // Mobile shortcut drawer; its hierarchy is created once so existing menu panels stay untouched.
    public sealed class QuickMenu : MonoBehaviour
    {
        private static readonly string[] Labels = { "Shop", "LuckyWheel", "Chọn Nhân Vật", "Điểm danh hằng ngày" };
        private static readonly string[] Icons = { "Shop", "LuckyWheel", "Character", "Daily" };
        public enum Item { Shop, LuckyWheel, Character, Daily }

        [Header("Dấu chấm thông báo")]
        [SerializeField] private bool shopHasNotification;
        [SerializeField] private bool wheelHasNotification;
        [SerializeField] private bool characterHasNotification;
        [SerializeField] private bool dailyHasNotification;

        private readonly Button[] entries = new Button[4];
        private readonly Image[] badges = new Image[4];
        private Image toggleBadge;
        private readonly CanvasGroup[] groups = new CanvasGroup[4];
        private readonly RectTransform[] rects = new RectTransform[4];
        private RectTransform root;
        private RectTransform canvasRect;
        private Rect lastSafeArea;
        private Button toggle;
        private bool expanded;
        private Coroutine animation;
        private int lastNotificationMask = -1;
        private float nextNotificationCheckTime = 0f;

        private void Start()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            var existing = canvas.transform.Find("QuickMenu");
            if (existing != null) BindExisting(existing);
            else Build(canvas.transform);

            UpdateNotifications();
        }

        private void OnEnable()
        {
            UpdateNotifications();
        }

#if UNITY_EDITOR
        public void CreateInScene()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            var old = canvas.transform.Find("QuickMenu");
            if (old != null) DestroyImmediate(old.gameObject);
            Build(canvas.transform);
        }
#endif

        private int lastToggleFrame = -1;

        private void BindExisting(Transform existing)
        {
            root = existing as RectTransform;
            canvasRect = existing.parent as RectTransform;

            var cg = existing.GetComponent<CanvasGroup>();
            if (cg == null) cg = existing.gameObject.AddComponent<CanvasGroup>();

            var anim = existing.GetComponent<UIPanelAnimator>();
            if (anim == null) anim = existing.gameObject.AddComponent<UIPanelAnimator>();
            anim.ConfigureSlide(UIPanelAnimator.TransitionType.SlideHorizontal, UIPanelAnimator.SlideDirection.FromRight, 0.4f);

            for (int i = 0; i < 4; i++)
            {
                var row = FindRow(existing, i);
                if (row == null)
                {
                    Debug.LogWarning($"QuickMenu: Row {i} ({Labels[i]}) not found in {existing.name}");
                    continue;
                }
                rects[i] = row as RectTransform;
                groups[i] = row.GetComponent<CanvasGroup>();
                if (groups[i] == null) groups[i] = row.gameObject.AddComponent<CanvasGroup>();

                if (!expanded && rects[i] != null)
                {
                    rects[i].anchoredPosition = Vector2.zero;
                    rects[i].gameObject.SetActive(false);
                    groups[i].alpha = 0f;
                    groups[i].interactable = groups[i].blocksRaycasts = false;
                }

                // Ensure sprite is set to minimalist atlas sprite
                var rowImage = row.GetComponent<Image>();
                if (rowImage != null)
                {
                    var s = AtlasSprite(Icons[i]);
                    if (s != null) rowImage.sprite = s;
                }

                entries[i] = row.GetComponent<Button>();
                if (entries[i] != null)
                {
                    int index = i;
                    entries[i].onClick.RemoveAllListeners();
                    entries[i].onClick.AddListener(() => Select(index));
                }
            }

            var toggleTrans = existing.Find("ButtonMucLuc");
            if (toggleTrans == null)
            {
                for (int c = 0; c < existing.childCount; c++)
                {
                    var ch = existing.GetChild(c);
                    if (ch.name.IndexOf("mucluc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        ch.name.IndexOf("menu", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        toggleTrans = ch;
                        break;
                    }
                }
            }

            toggle = toggleTrans?.GetComponent<Button>();
            if (toggle != null)
            {
                var toggleImg = toggle.GetComponent<Image>();
                if (toggleImg != null)
                {
                    var s = AtlasSprite("Menu");
                    if (s != null) toggleImg.sprite = s;
                }

                toggle.onClick.RemoveListener(Toggle);
                toggle.onClick.AddListener(Toggle);
            }

            for (int i = 0; i < 4; i++)
            {
                if (rects[i] == null) continue;
                var dotObj = rects[i].Find("NotificationDot");
                if (dotObj != null)
                {
                    var dotRect = dotObj as RectTransform;
                    if (dotRect != null) dotRect.anchoredPosition = new Vector2(-19f, -19f);
                    badges[i] = dotObj.GetComponent<Image>();
                    if (badges[i] != null && badges[i].sprite == null)
                    {
                        badges[i].sprite = AtlasSprite("Dot");
                    }
                }
                else
                {
                    badges[i] = NewBadge(rects[i]);
                }
            }

            if (toggleTrans != null)
            {
                var toggleDotObj = toggleTrans.Find("NotificationDot");
                if (toggleDotObj != null)
                {
                    var toggleDotRect = toggleDotObj as RectTransform;
                    if (toggleDotRect != null) toggleDotRect.anchoredPosition = new Vector2(-19f, -19f);
                    toggleBadge = toggleDotObj.GetComponent<Image>();
                    if (toggleBadge != null && toggleBadge.sprite == null)
                    {
                        toggleBadge.sprite = AtlasSprite("Dot");
                    }
                }
                else
                {
                    toggleBadge = NewBadge(toggleTrans as RectTransform);
                }
            }

            RefreshNotifications();
            UpdateSafeArea();
        }

        private static Transform FindRow(Transform root, int index)
        {
            if (root == null) return null;

            if (index >= 0 && index < Labels.Length)
            {
                var t = root.Find(Labels[index]);
                if (t != null) return t;
            }

            if (index >= 0 && index < Icons.Length)
            {
                var t = root.Find(Icons[index]);
                if (t != null) return t;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                var ch = root.GetChild(i);
                if (ch.name.IndexOf("mucluc", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                string lower = ch.name.ToLowerInvariant();
                switch (index)
                {
                    case 0:
                        if (lower.Contains("shop") || lower.Contains("store")) return ch;
                        break;
                    case 1:
                        if (lower.Contains("wheel") || lower.Contains("lucky") || lower.Contains("quay")) return ch;
                        break;
                    case 2:
                        if (lower.Contains("nhân") || lower.Contains("nhan") || lower.Contains("character")) return ch;
                        break;
                    case 3:
                        if (lower.Contains("điểm") || lower.Contains("diem") || lower.Contains("daily") || lower.Contains("checkin")) return ch;
                        break;
                }
            }

            if (index < root.childCount)
            {
                var ch = root.GetChild(index);
                if (ch.name.IndexOf("mucluc", StringComparison.OrdinalIgnoreCase) < 0) return ch;
            }

            return null;
        }

        private void Build(Transform canvas)
        {
            canvasRect = canvas as RectTransform;
            root = NewRect("QuickMenu", canvas);
            root.anchorMin = root.anchorMax = new Vector2(1, 0);
            root.pivot = new Vector2(1, 0);

            var cg = root.gameObject.GetComponent<CanvasGroup>();
            if (cg == null) cg = root.gameObject.AddComponent<CanvasGroup>();

            var anim = root.gameObject.GetComponent<UIPanelAnimator>();
            if (anim == null) anim = root.gameObject.AddComponent<UIPanelAnimator>();
            anim.ConfigureSlide(UIPanelAnimator.TransitionType.SlideHorizontal, UIPanelAnimator.SlideDirection.FromRight, 0.4f);

            UpdateSafeArea();
            root.sizeDelta = new Vector2(550, 108);
            root.SetAsLastSibling();

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                var row = NewRect(Labels[i], root);
                row.anchorMin = row.anchorMax = new Vector2(1, 0);
                row.pivot = new Vector2(1, 0);
                row.sizeDelta = new Vector2(104, 104);
                row.anchoredPosition = Vector2.zero;
                var rowImage = row.gameObject.AddComponent<Image>();
                rowImage.sprite = AtlasSprite(Icons[i]);
                var button = row.gameObject.AddComponent<Button>();
                button.targetGraphic = rowImage;
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.highlightedColor = new Color(1f, 1f, 1f);
                colors.pressedColor = new Color(0.7f, 0.85f, 1f);
                button.colors = colors;
                button.onClick.AddListener(() => Select(index));
                groups[i] = row.gameObject.AddComponent<CanvasGroup>();
                groups[i].alpha = 1;
                groups[i].interactable = groups[i].blocksRaycasts = false;
                badges[i] = NewBadge(row);
                row.gameObject.SetActive(false);
                rects[i] = row;
                entries[i] = button;
            }

            var toggleRect = NewRect("ButtonMucLuc", root);
            toggleRect.anchorMin = toggleRect.anchorMax = new Vector2(1, 0);
            toggleRect.pivot = new Vector2(1, 0);
            toggleRect.anchoredPosition = Vector2.zero;
            toggleRect.sizeDelta = new Vector2(104, 104);
            var image = toggleRect.gameObject.AddComponent<Image>();
            image.sprite = AtlasSprite("Menu");
            toggle = toggleRect.gameObject.AddComponent<Button>();
            toggle.targetGraphic = image;
            toggle.onClick.AddListener(Toggle);
            toggleBadge = NewBadge(toggleRect);
            RefreshNotifications();
        }


        private static Sprite AtlasSprite(string name)
        {
            foreach (var sprite in Resources.LoadAll<Sprite>("QuickMenu/MucLuc"))
                if (sprite.name == name) return sprite;
            Debug.LogWarning("MucLuc sprite missing: " + name);
            return null;
        }

        private static Image NewBadge(RectTransform parent)
        {
            var badge = NewRect("NotificationDot", parent);
            badge.anchorMin = badge.anchorMax = new Vector2(1, 1);
            badge.pivot = new Vector2(.5f, .5f);
            badge.sizeDelta = new Vector2(34, 34);
            badge.anchoredPosition = new Vector2(-19f, -19f);
            var image = badge.gameObject.AddComponent<Image>();
            image.sprite = AtlasSprite("Dot");
            image.raycastTarget = false;
            return image;
        }

        public void SetNotification(Item item, bool visible)
        {
            bool changed = false;
            switch (item)
            {
                case Item.Shop:
                    changed = (shopHasNotification != visible);
                    shopHasNotification = visible;
                    break;
                case Item.LuckyWheel:
                    changed = (wheelHasNotification != visible);
                    wheelHasNotification = visible;
                    break;
                case Item.Character:
                    changed = (characterHasNotification != visible);
                    characterHasNotification = visible;
                    break;
                case Item.Daily:
                    changed = (dailyHasNotification != visible);
                    dailyHasNotification = visible;
                    break;
            }
            if (changed || lastNotificationMask == -1)
            {
                RefreshNotifications();
            }
        }

        private void RefreshNotifications()
        {
            bool[] values = { shopHasNotification, wheelHasNotification,
                characterHasNotification, dailyHasNotification };
            lastNotificationMask = NotificationMask();
            for (int i = 0; i < 4; i++)
            {
                if (badges[i] != null)
                {
                    badges[i].gameObject.SetActive(values[i]);
                    badges[i].transform.SetAsLastSibling();
                }
            }
            if (toggleBadge != null)
            {
                bool anyActive = shopHasNotification || wheelHasNotification ||
                    characterHasNotification || dailyHasNotification;
                toggleBadge.gameObject.SetActive(anyActive);
                toggleBadge.transform.SetAsLastSibling();
            }
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private void Update()
        {
            if (root != null && Screen.safeArea != lastSafeArea) UpdateSafeArea();

            if (Time.unscaledTime >= nextNotificationCheckTime)
            {
                nextNotificationCheckTime = Time.unscaledTime + 0.5f;
                UpdateNotifications();
            }

            if (root != null && NotificationMask() != lastNotificationMask) RefreshNotifications();
        }

        private void UpdateNotifications()
        {
            bool hasLuckyWheelFree = LuckyWheelEditablePanels.CheckHasFreeSpin();
            SetNotification(Item.LuckyWheel, hasLuckyWheelFree);
        }

        private int NotificationMask()
        {
            return (shopHasNotification ? 1 : 0) | (wheelHasNotification ? 2 : 0) |
                (characterHasNotification ? 4 : 0) | (dailyHasNotification ? 8 : 0);
        }

        private void UpdateSafeArea()
        {
            lastSafeArea = Screen.safeArea;
            if (canvasRect == null || root == null) return;
            float scaleX = canvasRect.rect.width / Screen.width;
            float scaleY = canvasRect.rect.height / Screen.height;
            root.anchoredPosition = new Vector2(-36 - (Screen.width - lastSafeArea.xMax) * scaleX,
                36 + lastSafeArea.yMin * scaleY);

            var anim = root.GetComponent<UIPanelAnimator>();
            if (anim != null)
            {
                anim.SetOriginalPosition(root.anchoredPosition);
            }
        }

        public void Collapse()
        {
            if (!expanded) return;
            expanded = false;
            if (animation != null) StopCoroutine(animation);
            for (int i = 0; i < 4; i++)
            {
                if (rects != null && rects[i] != null)
                {
                    rects[i].anchoredPosition = Vector2.zero;
                    rects[i].gameObject.SetActive(false);
                }
                if (groups != null && groups[i] != null)
                {
                    groups[i].interactable = groups[i].blocksRaycasts = false;
                    groups[i].alpha = 0f;
                }
            }
        }

        private void OnDisable()
        {
            Collapse();
        }

        public void Toggle()
        {
            // Debounce guard: prevents multiple invocations within the same frame
            // (e.g. if Button has both persistent scene listener and runtime AddListener)
            if (Time.frameCount == lastToggleFrame) return;
            lastToggleFrame = Time.frameCount;

            expanded = !expanded;
            if (animation != null) StopCoroutine(animation);
            animation = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            const float duration = .32f;
            const float stagger = .04f;
            float[] from = new float[4];
            for (int i = 0; i < 4; i++)
            {
                if (rects[i] == null) continue;
                from[i] = rects[i].anchoredPosition.x;
                if (expanded)
                {
                    rects[i].gameObject.SetActive(true);
                }
                if (groups[i] != null)
                {
                    groups[i].interactable = groups[i].blocksRaycasts = expanded;
                }
            }

            float elapsed = 0f;
            float total = duration + 3 * stagger;
            while (elapsed < total)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < 4; i++)
                {
                    if (rects[i] == null) continue;
                    float delay = (expanded ? i : (3 - i)) * stagger;
                    float t = Mathf.Clamp01((elapsed - delay) / duration);
                    // Smooth ease out cubic
                    t = 1f - Mathf.Pow(1f - t, 3);
                    float targetX = expanded ? -108f * (i + 1) : 0f;
                    rects[i].anchoredPosition = new Vector2(Mathf.Lerp(from[i], targetX, t), 0f);
                    if (groups[i] != null)
                    {
                        groups[i].alpha = expanded ? Mathf.Lerp(0.2f, 1f, t) : Mathf.Lerp(1f, 0f, t);
                    }
                }
                yield return null;
            }

            for (int i = 0; i < 4; i++)
            {
                if (rects[i] == null) continue;
                rects[i].anchoredPosition = new Vector2(expanded ? -108f * (i + 1) : 0f, 0f);
                if (!expanded)
                {
                    rects[i].gameObject.SetActive(false);
                }
                if (groups[i] != null)
                {
                    groups[i].interactable = groups[i].blocksRaycasts = expanded;
                    groups[i].alpha = expanded ? 1f : 0f;
                }
            }
            animation = null;
        }

        public void OpenShop() => Select(0);
        public void OpenLuckyWheel() => Select(1);
        public void OpenCharacter() => Select(2);
        public void OpenDaily() => Select(3);

        private static readonly string[] SceneNames = {
            "Thuan_Shop",
            "LuckyWheel",
            "Thuan_CharacterSelect",
            "Thuan_DailyCheckin"
        };

        private void Select(int index)
        {
            if (index >= 0 && index < SceneNames.Length)
            {
                string sceneName = SceneNames[index];
                if (Application.CanStreamedLevelBeLoaded(sceneName))
                {
                    Debug.Log($"QuickMenu: Đang chuyển sang scene '{sceneName}' ({Labels[index]}).");
                    SceneManager.LoadScene(sceneName);
                    return;
                }
                Debug.LogWarning($"QuickMenu: Scene '{sceneName}' ({Labels[index]}) chưa được thêm vào Build Settings.");
            }

            if (index >= 0 && index < entries.Length && entries[index] != null)
            {
                var img = entries[index].GetComponent<Image>();
                if (img != null) StartCoroutine(Flash(img));
            }
        }

        private IEnumerator Flash(Image image)
        {
            var original = image.color;
            image.color = new Color(.2f, .65f, .8f, 1);
            yield return new WaitForSecondsRealtime(.18f);
            if (image != null) image.color = original;
        }
    }
}
