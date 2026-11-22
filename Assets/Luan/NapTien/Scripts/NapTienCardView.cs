using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Luan.NapTien
{
    public class NapTienCardView : MonoBehaviour
    {
        [Header("Card Elements")]
        [SerializeField] private Image bgImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text bonusText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text priceText;

        [Header("Badge")]
        [SerializeField] private GameObject badgeObj;
        [SerializeField] private TMP_Text badgeText;

        [Header("Card Button Overlay")]
        [SerializeField] private Button cardFullButton;

        private TopUpPackData _data;
        private Action<TopUpPackData> _onSelected;

        public TopUpPackData Data => _data;

        public void Bind(TopUpPackData data, Action<TopUpPackData> onSelected)
        {
            _data = data;
            _onSelected = onSelected;

            if (_data == null) return;

            if (titleText != null) titleText.text = _data.title;
            if (iconImage != null && _data.iconSprite != null) iconImage.sprite = _data.iconSprite;

            string unit = _data.currencyType == CurrencyType.Coin ? "Vàng" : "Kim Cương";
            if (amountText != null)
            {
                amountText.text = $"{_data.mainAmount:N0} {unit}";
            }

            if (bonusText != null)
            {
                if (_data.bonusAmount > 0)
                {
                    bonusText.text = $"+{_data.bonusAmount:N0} Thưởng";
                    bonusText.gameObject.SetActive(true);
                }
                else
                {
                    bonusText.gameObject.SetActive(false);
                }
            }

            if (priceText != null)
            {
                priceText.text = $"{_data.priceVnd:N0} đ";
            }

            if (badgeObj != null)
            {
                bool hasBadge = !string.IsNullOrEmpty(_data.badgeText);
                badgeObj.SetActive(hasBadge);
                if (hasBadge && badgeText != null)
                {
                    badgeText.text = _data.badgeText;
                }
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(OnClick);
            }

            if (cardFullButton != null)
            {
                cardFullButton.onClick.RemoveAllListeners();
                cardFullButton.onClick.AddListener(OnClick);
            }
        }

        private void OnClick()
        {
            _onSelected?.Invoke(_data);
        }
    }
}
