using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Luan.LuckyWheel
{
    public sealed class LuckyWheelController : MonoBehaviour
    {
        [Serializable]
        public class Prize
        {
            public string id;
            public string displayName;
            public Sprite icon;
            [Min(1)] public int amount = 1;
            [Min(0f)] public float weight = 1f;
        }

        [Header("Canvas references")]
        [SerializeField] private RectTransform wheel;
        [SerializeField] private Button spinCenterButton;
        [SerializeField] private Button spinX1Button;
        [SerializeField] private Button spinX10Button;
        [SerializeField] private LuckyWheelPurchaseUI purchaseUI;
        [SerializeField] private LuckyWheelEditablePanels panels;

        [Header("Spin animation")]
        [SerializeField, Min(0.25f)] private float spinDuration = 5.5f;
        [SerializeField, Min(1)] private int minimumTurns = 4;

        [Header("Prize configuration (clockwise, starting at the top)")]
        [SerializeField] private List<Prize> prizes = new List<Prize>();

        [Header("Runtime item layout")]
        [SerializeField] private float slotRadius = 245f;
        [SerializeField] private float iconOffsetY = 36f;
        [SerializeField] private float labelOffsetY = -36f;
        [SerializeField] private Vector2 iconSize = new Vector2(80f, 80f);
        [SerializeField] private Vector2 labelSize = new Vector2(82f, 32f);
        [SerializeField] private TMP_FontAsset itemFont;
        [SerializeField] private Material itemFontMaterial;

        [Header("Item orientation")]
        [Tooltip("Nếu bật: Các icon và chữ luôn giữ hướng thẳng đứng khi mâm quay (chuyển động kiểu cabin đu quay). Nếu tắt: Các icon và chữ quay dính liền theo nan quạt (căn giữa hoàn hảo, chuẩn phong cách game).")]
        [SerializeField] private bool keepItemsUpright = false;

        [Header("Events")]
        public UnityEvent<Prize> onSinglePrizeWon = new UnityEvent<Prize>();
        public UnityEvent<List<Prize>> onTenPrizesWon = new UnityEvent<List<Prize>>();
        public UnityEvent<string> onPrizeWon = new UnityEvent<string>();
        public UnityEvent onSpinSequenceFinished = new UnityEvent();

        public bool IsSpinning { get; private set; }
        public int CurrentSpinCount { get; private set; }
        public IReadOnlyList<Prize> Prizes => prizes;
        public bool KeepItemsUpright
        {
            get => keepItemsUpright;
            set
            {
                keepItemsUpright = value;
                if (wheel != null && !IsSpinning)
                    KeepItemViewsUpright(wheel.localEulerAngles.z);
            }
        }

        public void ApplyReadablePresentation()
        {
            spinDuration = 5.5f;
            minimumTurns = 4;
            slotRadius = 245f;
            iconOffsetY = 36f;
            labelOffsetY = -36f;
            iconSize = new Vector2(80f, 80f);
            labelSize = new Vector2(82f, 32f);
            keepItemsUpright = false;
        }

        private void Awake()
        {
            ResolveReferences();
            if (wheel != null) wheel.localRotation = Quaternion.identity;

            // Giữ nguyên thiết kế UI trong Scene của bạn, không tự ý xóa và tạo lại khi Play Mode!
            var runtimeItems = wheel != null ? wheel.Find("RuntimeItems") : null;
            if (runtimeItems == null || runtimeItems.childCount == 0)
            {
                RebuildItemViews();
            }

            spinCenterButton?.onClick.AddListener(SpinOnce);
            spinX1Button?.onClick.AddListener(SpinOnce);
            spinX10Button?.onClick.AddListener(SpinTenTimes);
        }

        private void OnDestroy()
        {
            spinCenterButton?.onClick.RemoveListener(SpinOnce);
            spinX1Button?.onClick.RemoveListener(SpinOnce);
            spinX10Button?.onClick.RemoveListener(SpinTenTimes);
        }

        public void SpinOnce()
        {
            if (IsSpinning) return;

            ResolveReferences();
            bool isFree = panels != null && panels.HasFreeSpinForOne;
            int cost = purchaseUI != null ? purchaseUI.PriceForOne : 50;

            if (!isFree)
            {
                if (panels != null && !panels.HasEnoughDiamonds(cost))
                {
                    Debug.LogWarning($"[LuckyWheel] Không đủ kim cương để quay x1! Cần {cost}, hiện có {panels.DiamondBalance}");
                    return;
                }

                if (panels != null)
                {
                    panels.TrySpendDiamonds(cost);
                }
            }
            else
            {
                if (panels != null)
                {
                    panels.ConsumeFreeSpins(1);
                }
            }

            if (purchaseUI != null)
            {
                purchaseUI.Refresh();
            }

            StartCoroutine(SpinOnceRoutine());
        }

        public void SpinTenTimes()
        {
            if (IsSpinning) return;

            ResolveReferences();
            bool isFree = panels != null && panels.HasFreeSpinForTen;
            int cost = purchaseUI != null ? purchaseUI.CurrentPriceForTen : 400;

            if (!isFree)
            {
                if (panels != null && !panels.HasEnoughDiamonds(cost))
                {
                    Debug.LogWarning($"[LuckyWheel] Không đủ kim cương để quay x10! Cần {cost}, hiện có {panels.DiamondBalance}");
                    return;
                }

                if (panels != null)
                {
                    panels.TrySpendDiamonds(cost);
                }

                if (purchaseUI != null && !purchaseUI.IsFirstTenDiscountUsed)
                {
                    purchaseUI.ConsumeFirstTenDiscount();
                }
            }
            else
            {
                if (panels != null)
                {
                    panels.ConsumeFreeSpins(10);
                }
            }

            if (purchaseUI != null)
            {
                purchaseUI.Refresh();
            }

            StartCoroutine(SpinTenTimesRoutine());
        }

        private IEnumerator SpinOnceRoutine()
        {
            if (wheel == null || prizes.Count == 0) yield break;
            IsSpinning = true;
            CurrentSpinCount = 1;
            SetButtonsInteractable(false);

            var prizeIndex = PickWeightedPrize();
            var prizeWon = prizes[prizeIndex];

            yield return SpinToPrize(prizeIndex);

            if (panels != null)
            {
                panels.AddBonusProgress(1);
            }

            onSinglePrizeWon.Invoke(prizeWon);
            onPrizeWon.Invoke(prizeWon.id);
            onSpinSequenceFinished.Invoke();

            SetButtonsInteractable(true);
            IsSpinning = false;
        }

        private IEnumerator SpinTenTimesRoutine()
        {
            if (wheel == null || prizes.Count == 0) yield break;
            IsSpinning = true;
            CurrentSpinCount = 10;
            SetButtonsInteractable(false);

            var wonPrizes = new List<Prize>(10);
            for (var i = 0; i < 10; i++)
            {
                var idx = PickWeightedPrize();
                wonPrizes.Add(prizes[idx]);
            }

            var displayPrizeIndex = prizes.IndexOf(wonPrizes[0]);
            if (displayPrizeIndex < 0) displayPrizeIndex = 0;

            yield return SpinToPrize(displayPrizeIndex);

            if (panels != null)
            {
                panels.AddBonusProgress(10);
            }

            onTenPrizesWon.Invoke(wonPrizes);
            onSpinSequenceFinished.Invoke();

            SetButtonsInteractable(true);
            IsSpinning = false;
        }

        private IEnumerator SpinToPrize(int prizeIndex)
        {
            var segmentAngle = 360f / prizes.Count;
            var current = wheel.localEulerAngles.z;
            var normalizedCurrent = Mathf.Repeat(current, 360f);
            var desired = Mathf.Repeat(prizeIndex * segmentAngle, 360f);
            var delta = Mathf.Repeat(desired - normalizedCurrent, 360f);
            var target = current + minimumTurns * 360f + delta;

            var elapsed = 0f;
            while (elapsed < spinDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / spinDuration);
                var eased = t * t * t * (t * (t * 6f - 15f) + 10f);
                var angle = Mathf.LerpUnclamped(current, target, eased);
                wheel.localRotation = Quaternion.Euler(0f, 0f, angle);
                if (keepItemsUpright) KeepItemViewsUpright(angle);
                yield return null;
            }
            wheel.localRotation = Quaternion.Euler(0f, 0f, target);
            if (keepItemsUpright) KeepItemViewsUpright(target);
        }

        private int PickWeightedPrize()
        {
            var total = 0f;
            foreach (var prize in prizes) total += Mathf.Max(0f, prize.weight);
            if (total <= 0f) return UnityEngine.Random.Range(0, prizes.Count);

            var roll = UnityEngine.Random.value * total;
            for (var i = 0; i < prizes.Count; i++)
            {
                roll -= Mathf.Max(0f, prizes[i].weight);
                if (roll <= 0f) return i;
            }
            return prizes.Count - 1;
        }

        private void SetButtonsInteractable(bool value)
        {
            if (spinCenterButton != null) spinCenterButton.interactable = value;
            if (spinX1Button != null) spinX1Button.interactable = value;
            if (spinX10Button != null) spinX10Button.interactable = value;
        }

        public void ResolveReferences()
        {
            if (wheel == null) wheel = transform.Find("Wheel") as RectTransform;
            if (spinCenterButton == null) spinCenterButton = transform.Find("SpinCenterButton")?.GetComponent<Button>();
            if (spinX1Button == null)
            {
                var btn = transform.root.Find("RightPanel/SpinX1Button");
                if (btn == null && transform.parent != null)
                    btn = transform.parent.Find("RightPanel/SpinX1Button");
                if (btn != null) spinX1Button = btn.GetComponent<Button>();
            }
            if (spinX10Button == null)
            {
                var btn = transform.root.Find("RightPanel/SpinX10Button");
                if (btn == null && transform.parent != null)
                    btn = transform.parent.Find("RightPanel/SpinX10Button");
                if (btn != null) spinX10Button = btn.GetComponent<Button>();
            }
            if (purchaseUI == null) purchaseUI = FindFirstObjectByType<LuckyWheelPurchaseUI>();
            if (panels == null) panels = FindFirstObjectByType<LuckyWheelEditablePanels>();
        }

        public void SetDefaultPrizes(Sprite[] icons, TMP_FontAsset font, Material fontMaterial = null)
        {
            itemFont = font;
            itemFontMaterial = fontMaterial;
            prizes = new List<Prize>
            {
                NewPrize("gold_1000", "Vàng\nx1.000", 1000, icons, 0),
                NewPrize("diamond_100", "Kim cương\nx100", 100, icons, 1),
                NewPrize("character_blue", "Nhân vật", 1, icons, 2),
                NewPrize("skin_jacket", "Skin", 1, icons, 3),
                NewPrize("premium_chest", "Rương quà", 1, icons, 4),
                NewPrize("ticket_1", "Vé quay\nx1", 1, icons, 5),
                NewPrize("energy_50", "Năng lượng\nx50", 50, icons, 6),
                NewPrize("rare_item", "Item hiếm", 1, icons, 7),
                NewPrize("gold_5000", "Vàng\nx5.000", 5000, icons, 8),
                NewPrize("character_pink", "Nhân vật", 1, icons, 9),
                NewPrize("ticket_2", "Vé quay\nx2", 2, icons, 10),
                NewPrize("weapon_skin", "Skin", 1, icons, 11)
            };
        }

        [ContextMenu("Rebuild Item Views")]
        public void RebuildItemViews()
        {
            if (wheel == null) ResolveReferences();
            if (wheel == null) return;

            var old = wheel.Find("RuntimeItems");
            if (old != null)
            {
                if (Application.isPlaying) Destroy(old.gameObject);
                else DestroyImmediate(old.gameObject);
            }

            var container = new GameObject("RuntimeItems", typeof(RectTransform)).GetComponent<RectTransform>();
            container.SetParent(wheel, false);
            container.anchorMin = container.anchorMax = container.pivot = new Vector2(0.5f, 0.5f);
            container.sizeDelta = Vector2.zero;

            for (var i = 0; i < prizes.Count; i++) CreateItemView(container, prizes[i], i);
            KeepItemViewsUpright(wheel.localEulerAngles.z);
        }

        private void OnValidate()
        {
            if (wheel != null && !IsSpinning && keepItemsUpright)
            {
                KeepItemViewsUpright(wheel.localEulerAngles.z);
            }
        }

        private void KeepItemViewsUpright(float wheelAngle)
        {
            if (!keepItemsUpright) return;
            var container = wheel == null ? null : wheel.Find("RuntimeItems");
            if (container == null) return;

            for (var i = 0; i < container.childCount; i++)
            {
                var slot = container.GetChild(i);
                slot.localRotation = Quaternion.Euler(0f, 0f, -wheelAngle);
            }
        }

        private void CreateItemView(RectTransform container, Prize prize, int index)
        {
            var angle = index * 360f / Mathf.Max(1, prizes.Count);
            var radians = angle * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));

            var slot = new GameObject($"Item_{index + 1:00}_{prize.id}", typeof(RectTransform)).GetComponent<RectTransform>();
            slot.SetParent(container, false);
            slot.anchorMin = slot.anchorMax = slot.pivot = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = direction * slotRadius;
            slot.localRotation = Quaternion.Euler(0f, 0f, -angle);
            slot.sizeDelta = Vector2.zero;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.SetParent(slot, false);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, iconOffsetY);
            iconRect.sizeDelta = iconSize;
            iconRect.localRotation = Quaternion.identity;
            var image = iconGo.GetComponent<Image>();
            image.sprite = prize.icon;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(slot, false);
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, labelOffsetY);
            labelRect.sizeDelta = labelSize;
            labelRect.localRotation = Quaternion.identity;
            var text = labelGo.GetComponent<TextMeshProUGUI>();
            text.text = prize.displayName;
            text.font = itemFont;

            var outlineMat = itemFontMaterial != null ? itemFontMaterial : Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Outline");
            if (outlineMat != null)
            {
                text.fontSharedMaterial = outlineMat;
            }

            if (text.fontMaterial != null)
            {
                text.fontMaterial.EnableKeyword("OUTLINE_ON");
                text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.04f, 0.01f, 0.08f, 1f));
                text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.32f);
                text.fontMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0f);
                text.fontMaterial.EnableKeyword("UNDERLAY_ON");
                text.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.85f));
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.35f);
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.35f);
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.1f);
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.15f);
            }

            text.fontSize = 15f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.lineSpacing = -10f;
            text.enableWordWrapping = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 16f;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = Vector4.zero;
            text.extraPadding = true;
            text.raycastTarget = false;
        }

        private static Prize NewPrize(string id, string displayName, int amount, Sprite[] icons, int iconIndex) => new Prize
        {
            id = id,
            displayName = displayName,
            amount = amount,
            icon = icons != null && iconIndex < icons.Length ? icons[iconIndex] : null,
            weight = 1f
        };
    }
}
