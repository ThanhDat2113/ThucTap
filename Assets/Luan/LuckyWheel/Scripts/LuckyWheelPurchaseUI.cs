using TMPro;
using UnityEngine;

namespace Luan.LuckyWheel
{
    [ExecuteAlways]
    public sealed class LuckyWheelPurchaseUI : MonoBehaviour
    {
        [Header("Diamond prices")]
        [SerializeField, Min(0)] private int priceForOne = 50;
        [SerializeField, Min(0)] private int priceForTen = 400;
        [Header("Visible promotion")]
        [SerializeField, Range(0, 100)] private int discountPercent = 20;

        [SerializeField] private TMP_Text priceOneLabel;
        [SerializeField] private TMP_Text priceTenLabel;
        [SerializeField] private TMP_Text discountLabel;

        public int PriceForOne => priceForOne;
        public int PriceForTen => priceForTen;
        public int DiscountPercent => discountPercent;

        public void Configure(TMP_Text one, TMP_Text ten, TMP_Text discount)
        {
            priceOneLabel = one;
            priceTenLabel = ten;
            discountLabel = discount;
            Refresh();
        }

        private void OnEnable() => Refresh();
        private void OnValidate() => Refresh();

        public void Refresh()
        {
            if (priceOneLabel != null) priceOneLabel.text = priceForOne.ToString("N0");
            if (priceTenLabel != null) priceTenLabel.text = priceForTen.ToString("N0");
            if (discountLabel != null) discountLabel.text = $"Tiết kiệm\n<size=125%>{discountPercent}%</size>";
        }
    }
}
