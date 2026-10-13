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

        [Header("Spin animation")]
        [SerializeField, Min(0.25f)] private float spinDuration = 5.5f;
        [SerializeField, Min(1)] private int minimumTurns = 4;

        [Header("Prize configuration (clockwise, starting at the top)")]
        [SerializeField] private List<Prize> prizes = new List<Prize>();

        [Header("Runtime item layout")]
        [SerializeField] private float iconRadius = 275f;
        [SerializeField] private float labelRadius = 180f;
        [SerializeField] private Vector2 iconSize = new Vector2(100f, 100f);
        [SerializeField] private Vector2 labelSize = new Vector2(110f, 42f);
        [SerializeField] private TMP_FontAsset itemFont;
        [SerializeField] private Material itemFontMaterial;

        [Header("Item orientation")]
        [Tooltip("Nếu bật: Các icon và chữ luôn giữ hướng thẳng đứng khi mâm quay (chuyển động kiểu cabin đu quay). Nếu tắt: Các icon và chữ quay dính liền theo nan quạt.")]
        [SerializeField] private bool keepItemsUpright = true;

        [Header("Events")]
        public UnityEvent<string> onPrizeWon = new UnityEvent<string>();
        public UnityEvent onSpinSequenceFinished = new UnityEvent();

        public bool IsSpinning { get; private set; }
        public IReadOnlyList<Prize> Prizes => prizes;
        public bool KeepItemsUpright
        {
            get => keepItemsUpright;
            set
            {
                keepItemsUpright = value;
                if (wheel != null && !IsSpinning)
                    KeepItemViewsUpright(keepItemsUpright ? wheel.localEulerAngles.z : 0f);
            }
        }

        public void ApplyReadablePresentation()
        {
            spinDuration = 5.5f;
            minimumTurns = 4;
            iconRadius = 275f;
            labelRadius = 180f;
            iconSize = new Vector2(100f, 100f);
            labelSize = new Vector2(110f, 42f);
            keepItemsUpright = true;
        }

        private void Awake()
        {
            ResolveReferences();
            RebuildItemViews();
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
            if (!IsSpinning) StartCoroutine(SpinSequence(1));
        }

        public void SpinTenTimes()
        {
            if (!IsSpinning) StartCoroutine(SpinSequence(10));
        }

        private IEnumerator SpinSequence(int count)
        {
            if (wheel == null || prizes.Count == 0) yield break;
            IsSpinning = true;
            SetButtonsInteractable(false);

            for (var i = 0; i < count; i++)
            {
                var prizeIndex = PickWeightedPrize();
                yield return SpinToPrize(prizeIndex);
                onPrizeWon.Invoke(prizes[prizeIndex].id);
                if (i < count - 1) yield return new WaitForSecondsRealtime(0.25f);
            }

            SetButtonsInteractable(true);
            IsSpinning = false;
            onSpinSequenceFinished.Invoke();
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

        private void ResolveReferences()
        {
            if (wheel == null) wheel = transform.Find("Wheel") as RectTransform;
            if (spinCenterButton == null) spinCenterButton = transform.Find("SpinCenterButton")?.GetComponent<Button>();
            if (spinX1Button == null) spinX1Button = transform.root.Find("RightPanel/SpinX1Button")?.GetComponent<Button>();
            if (spinX10Button == null) spinX10Button = transform.root.Find("RightPanel/SpinX10Button")?.GetComponent<Button>();
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
            KeepItemViewsUpright(keepItemsUpright ? wheel.localEulerAngles.z : 0f);
        }

        private void OnValidate()
        {
            if (wheel != null && !IsSpinning)
            {
                KeepItemViewsUpright(keepItemsUpright ? wheel.localEulerAngles.z : 0f);
            }
        }

        private void KeepItemViewsUpright(float wheelAngle)
        {
            var container = wheel == null ? null : wheel.Find("RuntimeItems");
            if (container == null) return;
            var targetRot = Quaternion.Euler(0f, 0f, -wheelAngle);
            for (var i = 0; i < container.childCount; i++)
            {
                var slot = container.GetChild(i);
                for (var c = 0; c < slot.childCount; c++)
                {
                    slot.GetChild(c).localRotation = targetRot;
                }
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
            slot.anchoredPosition = Vector2.zero;
            slot.sizeDelta = Vector2.zero;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.SetParent(slot, false);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = direction * iconRadius;
            iconRect.sizeDelta = iconSize;
            var image = iconGo.GetComponent<Image>();
            image.sprite = prize.icon;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(slot, false);
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = direction * labelRadius;
            labelRect.sizeDelta = labelSize;
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
                text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.28f);
                text.fontMaterial.EnableKeyword("UNDERLAY_ON");
                text.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.85f));
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.35f);
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.35f);
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.1f);
                text.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.15f);
            }

            text.fontSize = 18f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.lineSpacing = -10f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14f;
            text.fontSizeMax = 20f;
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
