using TMPro;
using UnityEngine;

namespace Luan.LuckyWheel
{
    [ExecuteAlways]
    public sealed class LuckyWheelPurchaseUI : MonoBehaviour
    {
        public const string PrefsFirstTenDiscountUsedKey = "LUCKY_WHEEL_FIRST_TEN_DISCOUNT_USED";

        [Header("Diamond prices")]
        [SerializeField, Min(0)] private int priceForOne = 50;
        [SerializeField, Min(0)] private int defaultPriceForTen = 500;
        [SerializeField, Min(0)] private int discountedPriceForTen = 400;

        [Header("Visible promotion")]
        [SerializeField, Range(0, 100)] private int discountPercent = 20;

        [Header("Free spin display")]
        [SerializeField] private string freeText = "MIỄN PHÍ";

        [Header("Test settings")]
        [Tooltip("Khi bật: Mỗi lần vào Play Mode sẽ tự động xóa bộ nhớ khuyến mãi để test lại từ đầu.")]
        [SerializeField] private bool resetOnPlayMode = true;

        [Header("UI References")]
        [SerializeField] private TMP_Text priceOneLabel;
        [SerializeField] private TMP_Text priceTenLabel;
        [SerializeField] private TMP_Text discountLabel;
        [SerializeField] private GameObject discountBadge;
        [SerializeField] private GameObject diamondOneIcon;
        [SerializeField] private GameObject diamondTenIcon;
        [SerializeField] private RectTransform priceOnePanel;
        [SerializeField] private RectTransform priceTenPanel;
        [SerializeField] private LuckyWheelEditablePanels panels;

        private static readonly Vector2 DefaultPriceOnePos = new Vector2(1470.5111f, -969.7256f);
        private static readonly Vector2 DefaultPriceOneSize = new Vector2(140.3678f, 54.0793f);
        private static readonly Vector2 DefaultPriceTenPos = new Vector2(1739.3259f, -960.4749f);
        private static readonly Vector2 DefaultPriceTenSize = new Vector2(114.0743f, 52.3244f);

        private bool hasCachedDefaults;
        private Vector2 cachedPriceOnePos = DefaultPriceOnePos;
        private Vector2 cachedPriceOneSize = DefaultPriceOneSize;
        private Vector2 cachedPriceTenPos = DefaultPriceTenPos;
        private Vector2 cachedPriceTenSize = DefaultPriceTenSize;

        public int PriceForOne => priceForOne;
        public int DefaultPriceForTen => defaultPriceForTen;
        public int DiscountedPriceForTen => discountedPriceForTen;
        public int CurrentPriceForTen => IsFirstTenDiscountUsed ? defaultPriceForTen : discountedPriceForTen;
        public int DiscountPercent => discountPercent;

        public bool IsOneFree => Application.isPlaying && panels != null && panels.HasFreeSpinForOne;
        public bool IsTenFree => Application.isPlaying && panels != null && panels.HasFreeSpinForTen;

        public bool IsFirstTenDiscountUsed
        {
            get
            {
                if (Application.isPlaying)
                {
                    return PlayerPrefs.GetInt(PrefsFirstTenDiscountUsedKey, 0) == 1;
                }
                return false;
            }
        }

        public void Configure(TMP_Text one, TMP_Text ten, TMP_Text discount, GameObject badge = null)
        {
            priceOneLabel = one;
            priceTenLabel = ten;
            discountLabel = discount;
            if (badge != null) discountBadge = badge;
            Refresh();
        }

        private void Awake()
        {
            if (Application.isPlaying && resetOnPlayMode)
            {
                PlayerPrefs.DeleteKey(PrefsFirstTenDiscountUsedKey);
                PlayerPrefs.Save();
            }
            CacheDefaults();
            Refresh();
        }

        private void OnEnable()
        {
            try { CacheDefaults(); Refresh(); } catch { }
        }

        private void OnValidate()
        {
            try { CacheDefaults(); Refresh(); } catch { }
        }

        private void CacheDefaults()
        {
            if (hasCachedDefaults) return;
            ResolveReferences();

            if (priceOneLabel != null && priceOneLabel.text != freeText)
            {
                cachedPriceOnePos = priceOneLabel.rectTransform.anchoredPosition;
                cachedPriceOneSize = priceOneLabel.rectTransform.sizeDelta;
            }

            if (priceTenLabel != null && priceTenLabel.text != freeText)
            {
                cachedPriceTenPos = priceTenLabel.rectTransform.anchoredPosition;
                cachedPriceTenSize = priceTenLabel.rectTransform.sizeDelta;
            }

            if (priceOneLabel != null && priceTenLabel != null)
            {
                hasCachedDefaults = true;
            }
        }

        public void ConsumeFirstTenDiscount()
        {
            if (Application.isPlaying)
            {
                PlayerPrefs.SetInt(PrefsFirstTenDiscountUsedKey, 1);
                PlayerPrefs.Save();
            }
            Refresh();
        }

        [ContextMenu("Reset First Ten Discount")]
        public void ResetFirstTenDiscount()
        {
            PlayerPrefs.DeleteKey(PrefsFirstTenDiscountUsedKey);
            PlayerPrefs.Save();
            Refresh();
        }

        public void ResolveReferences()
        {
            try
            {
                if (priceOneLabel == null) priceOneLabel = transform.Find("PriceOneLabel")?.GetComponent<TMP_Text>();
                if (priceTenLabel == null) priceTenLabel = transform.Find("PriceTenLabel")?.GetComponent<TMP_Text>();
                if (discountLabel == null) discountLabel = transform.Find("DiscountLabel")?.GetComponent<TMP_Text>();
                if (discountBadge == null) discountBadge = transform.Find("DiscountBadge")?.gameObject;
                if (diamondOneIcon == null) diamondOneIcon = transform.Find("DiamondOneIcon")?.gameObject;
                if (diamondTenIcon == null) diamondTenIcon = transform.Find("DiamondTenIcon")?.gameObject;
                if (priceOnePanel == null) priceOnePanel = transform.Find("PriceOnePanel")?.GetComponent<RectTransform>();
                if (priceTenPanel == null) priceTenPanel = transform.Find("PriceTenPanel")?.GetComponent<RectTransform>();
                if (panels == null) panels = FindFirstObjectByType<LuckyWheelEditablePanels>();
            }
            catch (MissingReferenceException)
            {
                priceOneLabel = transform.Find("PriceOneLabel")?.GetComponent<TMP_Text>();
                priceTenLabel = transform.Find("PriceTenLabel")?.GetComponent<TMP_Text>();
                discountLabel = transform.Find("DiscountLabel")?.GetComponent<TMP_Text>();
                discountBadge = transform.Find("DiscountBadge")?.gameObject;
                diamondOneIcon = transform.Find("DiamondOneIcon")?.gameObject;
                diamondTenIcon = transform.Find("DiamondTenIcon")?.gameObject;
                priceOnePanel = transform.Find("PriceOnePanel")?.GetComponent<RectTransform>();
                priceTenPanel = transform.Find("PriceTenPanel")?.GetComponent<RectTransform>();
                panels = FindFirstObjectByType<LuckyWheelEditablePanels>();
            }
        }

        public void Refresh()
        {
            ResolveReferences();
            CacheDefaults();

            bool isOneFree = IsOneFree;
            if (priceOneLabel != null)
            {
                if (isOneFree)
                {
                    priceOneLabel.text = freeText;
                    var centerX = priceOnePanel != null ? priceOnePanel.anchoredPosition.x : 1439f;
                    priceOneLabel.rectTransform.anchoredPosition = new Vector2(centerX, cachedPriceOnePos.y);
                    priceOneLabel.rectTransform.sizeDelta = new Vector2(Mathf.Max(cachedPriceOneSize.x, 260f), cachedPriceOneSize.y);
                }
                else
                {
                    priceOneLabel.text = priceForOne.ToString("N0");
                    priceOneLabel.rectTransform.anchoredPosition = cachedPriceOnePos;
                    priceOneLabel.rectTransform.sizeDelta = cachedPriceOneSize;
                }
            }

            try
            {
                if (diamondOneIcon != null)
                {
                    diamondOneIcon.SetActive(!isOneFree);
                }
            }
            catch (MissingReferenceException)
            {
                diamondOneIcon = transform.Find("DiamondOneIcon")?.gameObject;
                if (diamondOneIcon != null) diamondOneIcon.SetActive(!isOneFree);
            }

            bool isTenFree = IsTenFree;
            bool discountUsed = IsFirstTenDiscountUsed;
            int tenPrice = discountUsed ? defaultPriceForTen : discountedPriceForTen;

            if (priceTenLabel != null)
            {
                if (isTenFree)
                {
                    priceTenLabel.text = freeText;
                    var centerX = priceTenPanel != null ? priceTenPanel.anchoredPosition.x : 1729.4065f;
                    priceTenLabel.rectTransform.anchoredPosition = new Vector2(centerX, cachedPriceTenPos.y);
                    priceTenLabel.rectTransform.sizeDelta = new Vector2(Mathf.Max(cachedPriceTenSize.x, 240f), cachedPriceTenSize.y);
                }
                else
                {
                    priceTenLabel.text = tenPrice.ToString("N0");
                    priceTenLabel.rectTransform.anchoredPosition = cachedPriceTenPos;
                    priceTenLabel.rectTransform.sizeDelta = cachedPriceTenSize;
                }
            }

            try
            {
                if (diamondTenIcon != null)
                {
                    diamondTenIcon.SetActive(!isTenFree);
                }
            }
            catch (MissingReferenceException)
            {
                diamondTenIcon = transform.Find("DiamondTenIcon")?.gameObject;
                if (diamondTenIcon != null) diamondTenIcon.SetActive(!isTenFree);
            }

            bool showDiscount = !isTenFree && !discountUsed;

            try
            {
                if (discountBadge != null)
                {
                    discountBadge.SetActive(showDiscount);
                }
            }
            catch (MissingReferenceException)
            {
                discountBadge = transform.Find("DiscountBadge")?.gameObject;
                if (discountBadge != null)
                {
                    discountBadge.SetActive(showDiscount);
                }
            }

            try
            {
                if (discountLabel != null)
                {
                    discountLabel.gameObject.SetActive(showDiscount);
                    if (showDiscount)
                    {
                        discountLabel.text = $"Tiết kiệm\n<size=125%>{discountPercent}%</size>";
                    }
                }
            }
            catch (MissingReferenceException)
            {
                discountLabel = transform.Find("DiscountLabel")?.GetComponent<TMP_Text>();
                if (discountLabel != null)
                {
                    discountLabel.gameObject.SetActive(showDiscount);
                    if (showDiscount)
                    {
                        discountLabel.text = $"Tiết kiệm\n<size=125%>{discountPercent}%</size>";
                    }
                }
            }
        }
    }
}
