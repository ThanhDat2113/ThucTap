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

        [Header("Test settings")]
        [Tooltip("Khi bật: Mỗi lần vào Play Mode sẽ tự động xóa bộ nhớ khuyến mãi để test lại từ đầu.")]
        [SerializeField] private bool resetOnPlayMode = true;

        [Header("UI References")]
        [SerializeField] private TMP_Text priceOneLabel;
        [SerializeField] private TMP_Text priceTenLabel;
        [SerializeField] private TMP_Text discountLabel;
        [SerializeField] private GameObject discountBadge;

        public int PriceForOne => priceForOne;
        public int DefaultPriceForTen => defaultPriceForTen;
        public int DiscountedPriceForTen => discountedPriceForTen;
        public int CurrentPriceForTen => IsFirstTenDiscountUsed ? defaultPriceForTen : discountedPriceForTen;
        public int DiscountPercent => discountPercent;

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
            Refresh();
        }
        private void OnEnable()
        {
            try { Refresh(); } catch { }
        }

        private void OnValidate()
        {
            try { Refresh(); } catch { }
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
            }
            catch (MissingReferenceException)
            {
                priceOneLabel = transform.Find("PriceOneLabel")?.GetComponent<TMP_Text>();
                priceTenLabel = transform.Find("PriceTenLabel")?.GetComponent<TMP_Text>();
                discountLabel = transform.Find("DiscountLabel")?.GetComponent<TMP_Text>();
                discountBadge = transform.Find("DiscountBadge")?.gameObject;
            }
        }

        public void Refresh()
        {
            ResolveReferences();

            if (priceOneLabel != null)
            {
                priceOneLabel.text = priceForOne.ToString("N0");
            }

            bool discountUsed = IsFirstTenDiscountUsed;
            int tenPrice = discountUsed ? defaultPriceForTen : discountedPriceForTen;

            if (priceTenLabel != null)
            {
                priceTenLabel.text = tenPrice.ToString("N0");
            }

            try
            {
                if (discountBadge != null)
                {
                    discountBadge.SetActive(!discountUsed);
                }
            }
            catch (MissingReferenceException)
            {
                discountBadge = transform.Find("DiscountBadge")?.gameObject;
                if (discountBadge != null)
                {
                    discountBadge.SetActive(!discountUsed);
                }
            }

            try
            {
                if (discountLabel != null)
                {
                    discountLabel.gameObject.SetActive(!discountUsed);
                    if (!discountUsed)
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
                    discountLabel.gameObject.SetActive(!discountUsed);
                    if (!discountUsed)
                    {
                        discountLabel.text = $"Tiết kiệm\n<size=125%>{discountPercent}%</size>";
                    }
                }
            }
        }
    }
}
