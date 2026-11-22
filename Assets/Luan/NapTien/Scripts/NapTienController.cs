using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Luan.NapTien
{
    public enum TopUpCategory
    {
        Gems = 0,
        Coins = 1,
        Special = 2
    }

    [Serializable]
    public class TopUpPackData
    {
        public string packId;
        public string title;
        public int mainAmount;
        public int bonusAmount;
        public int priceVnd;
        public string badgeText;
        public Sprite iconSprite;
        public CurrencyType currencyType;
        [Tooltip("Dùng riêng cho gói đặc biệt có vé")]
        public int bonusTickets;
    }

    public class NapTienController : MonoBehaviour
    {
        [Header("--- Header UI ---")]
        [SerializeField] private Button backButton;
        [SerializeField] private string returnSceneName = "Thuan_Shop";
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text gemText;

        [Header("--- Category Tabs ---")]
        [SerializeField] private Button tabGemsButton;
        [SerializeField] private Button tabCoinsButton;
        [SerializeField] private Button tabSpecialButton;

        [SerializeField] private Image tabGemsBg;
        [SerializeField] private Image tabCoinsBg;
        [SerializeField] private Image tabSpecialBg;

        [SerializeField] private TMP_Text tabGemsText;
        [SerializeField] private TMP_Text tabCoinsText;
        [SerializeField] private TMP_Text tabSpecialText;

        [Header("--- Pack Grid Containers ---")]
        [SerializeField] private GameObject gemsContainer;
        [SerializeField] private GameObject coinsContainer;
        [SerializeField] private GameObject specialContainer;

        [Header("--- Card Views ---")]
        [SerializeField] private NapTienCardView[] gemCardViews;
        [SerializeField] private NapTienCardView[] coinCardViews;
        [SerializeField] private NapTienCardView[] specialCardViews;

        [Header("--- Payment Modal ---")]
        [SerializeField] private GameObject paymentModal;
        [SerializeField] private RectTransform paymentModalDialog;
        [SerializeField] private Image modalPackIcon;
        [SerializeField] private TMP_Text modalPackTitle;
        [SerializeField] private TMP_Text modalPackValue;
        [SerializeField] private TMP_Text modalPackPrice;

        [Header("Payment Channel Buttons")]
        [SerializeField] private Button btnVietQR;
        [SerializeField] private Button btnZaloPay;
        [SerializeField] private Button btnMoMo;
        [SerializeField] private Image imgVietQRSelect;
        [SerializeField] private Image imgZaloPaySelect;
        [SerializeField] private Image imgMoMoSelect;

        [Header("Payment Details & QR")]
        [SerializeField] private Image qrImage;
        [SerializeField] private TMP_Text bankNameText;
        [SerializeField] private TMP_Text accountNumText;
        [SerializeField] private TMP_Text accountNameText;
        [SerializeField] private TMP_Text transferContentText;
        [SerializeField] private TMP_Text amountToPayText;
        [SerializeField] private TMP_Text timerCountdownText;

        [SerializeField] private Button copyAccountNumBtn;
        [SerializeField] private Button copyContentBtn;
        [SerializeField] private Button confirmPaymentBtn;
        [SerializeField] private Button closePaymentModalBtn;

        [Header("--- Success Notification / Toast ---")]
        [SerializeField] private GameObject successToast;
        [SerializeField] private RectTransform successToastDialog;
        [SerializeField] private TMP_Text successMessageText;
        [SerializeField] private Button successOkButton;

        [Header("--- Toast Copy ---")]
        [SerializeField] private GameObject copyToast;
        [SerializeField] private TMP_Text copyToastText;

        // Trạng thái runtime
        private TopUpCategory _currentCategory = TopUpCategory.Gems;
        private TopUpPackData _selectedPack;
        private string _currentPaymentChannel = "VietQR";
        private Coroutine _timerRoutine;
        private Coroutine _toastRoutine;
        private Coroutine _copyToastRoutine;

        private void Awake()
        {
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);

            if (tabGemsButton != null) tabGemsButton.onClick.AddListener(() => SwitchCategory(TopUpCategory.Gems));
            if (tabCoinsButton != null) tabCoinsButton.onClick.AddListener(() => SwitchCategory(TopUpCategory.Coins));
            if (tabSpecialButton != null) tabSpecialButton.onClick.AddListener(() => SwitchCategory(TopUpCategory.Special));

            if (btnVietQR != null) btnVietQR.onClick.AddListener(() => SelectPaymentChannel("VietQR"));
            if (btnZaloPay != null) btnZaloPay.onClick.AddListener(() => SelectPaymentChannel("ZaloPay"));
            if (btnMoMo != null) btnMoMo.onClick.AddListener(() => SelectPaymentChannel("MoMo"));

            if (copyAccountNumBtn != null) copyAccountNumBtn.onClick.AddListener(OnCopyAccountNumClicked);
            if (copyContentBtn != null) copyContentBtn.onClick.AddListener(OnCopyContentClicked);
            if (confirmPaymentBtn != null) confirmPaymentBtn.onClick.AddListener(OnConfirmPaymentClicked);
            if (closePaymentModalBtn != null) closePaymentModalBtn.onClick.AddListener(ClosePaymentModal);
            if (successOkButton != null) successOkButton.onClick.AddListener(CloseSuccessToast);

            if (paymentModal != null) paymentModal.SetActive(false);
            if (successToast != null) successToast.SetActive(false);
            if (copyToast != null) copyToast.SetActive(false);
        }

        private void OnEnable()
        {
            Wallet.Changed += RefreshWallet;
            RefreshWallet();
        }

        private void OnDisable()
        {
            Wallet.Changed -= RefreshWallet;
            StopTimer();
        }

        private void Start()
        {
            RefreshWallet();
            SwitchCategory(TopUpCategory.Gems);
            PlaySceneBgm();
        }

        private void PlaySceneBgm()
        {
            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlayBGM();
            }
            else if (MusicManager.Instance != null)
            {
                var bgm = Resources.Load<AudioClip>("Audio/menu");
                if (bgm != null) MusicManager.Instance.PlayMusic(bgm);
            }
        }

        public void RefreshWallet()
        {
            if (coinText != null) coinText.text = Wallet.Coins.ToString("N0");
            if (gemText != null) gemText.text = Wallet.Gems.ToString("N0");
        }

        public void SwitchCategory(TopUpCategory category)
        {
            _currentCategory = category;

            if (gemsContainer != null) gemsContainer.SetActive(category == TopUpCategory.Gems);
            if (coinsContainer != null) coinsContainer.SetActive(category == TopUpCategory.Coins);
            if (specialContainer != null) specialContainer.SetActive(category == TopUpCategory.Special);

            UpdateTabButtonVisuals();

            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlaySelectSound();
            }
        }

        private void UpdateTabButtonVisuals()
        {
            Color activeColor = new Color(0f, 1f, 0.9f, 1f);     // Neon Cyan
            Color normalColor = new Color(0.6f, 0.65f, 0.8f, 1f); // Dim Cyan/Grey
            Color activeBg = new Color(0.12f, 0.35f, 0.6f, 0.9f);
            Color normalBg = new Color(0.08f, 0.08f, 0.18f, 0.75f);

            if (tabGemsText != null) tabGemsText.color = _currentCategory == TopUpCategory.Gems ? activeColor : normalColor;
            if (tabCoinsText != null) tabCoinsText.color = _currentCategory == TopUpCategory.Coins ? activeColor : normalColor;
            if (tabSpecialText != null) tabSpecialText.color = _currentCategory == TopUpCategory.Special ? activeColor : normalColor;

            if (tabGemsBg != null) tabGemsBg.color = _currentCategory == TopUpCategory.Gems ? activeBg : normalBg;
            if (tabCoinsBg != null) tabCoinsBg.color = _currentCategory == TopUpCategory.Coins ? activeBg : normalBg;
            if (tabSpecialBg != null) tabSpecialBg.color = _currentCategory == TopUpCategory.Special ? activeBg : normalBg;
        }

        public void OnPackSelected(TopUpPackData pack)
        {
            if (pack == null) return;
            _selectedPack = pack;

            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlayConfirmSound();
            }

            OpenPaymentModal();
        }

        private void OpenPaymentModal()
        {
            if (paymentModal == null || _selectedPack == null) return;

            paymentModal.SetActive(true);

            if (modalPackIcon != null && _selectedPack.iconSprite != null)
            {
                modalPackIcon.sprite = _selectedPack.iconSprite;
            }
            if (modalPackTitle != null) modalPackTitle.text = _selectedPack.title;
            if (modalPackPrice != null) modalPackPrice.text = $"{_selectedPack.priceVnd:N0} VNĐ";

            if (modalPackValue != null)
            {
                string unit = _selectedPack.currencyType == CurrencyType.Coin ? "Vàng" : "Kim Cương";
                if (_selectedPack.bonusAmount > 0)
                {
                    modalPackValue.text = $"+{_selectedPack.mainAmount:N0} {unit} (+{_selectedPack.bonusAmount:N0} Thưởng)";
                }
                else
                {
                    modalPackValue.text = $"+{_selectedPack.mainAmount:N0} {unit}";
                }
            }

            SelectPaymentChannel(_currentPaymentChannel);

            // Hiệu ứng Pop-up Ease Out Back
            if (paymentModalDialog != null)
            {
                StartCoroutine(PopDialogRoutine(paymentModalDialog));
            }

            StartTimer(300); // 5 phút đếm ngược
        }

        public void ClosePaymentModal()
        {
            StopTimer();
            if (paymentModal != null) paymentModal.SetActive(false);
            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlayCloseSound();
            }
        }

        public void SelectPaymentChannel(string channel)
        {
            _currentPaymentChannel = channel;

            if (imgVietQRSelect != null) imgVietQRSelect.gameObject.SetActive(channel == "VietQR");
            if (imgZaloPaySelect != null) imgZaloPaySelect.gameObject.SetActive(channel == "ZaloPay");
            if (imgMoMoSelect != null) imgMoMoSelect.gameObject.SetActive(channel == "MoMo");

            int randomOrderCode = UnityEngine.Random.Range(10000, 99999);
            string orderCodeStr = $"NAP{randomOrderCode}";

            if (amountToPayText != null && _selectedPack != null)
            {
                amountToPayText.text = $"{_selectedPack.priceVnd:N0} đ";
            }

            if (channel == "VietQR")
            {
                if (bankNameText != null) bankNameText.text = "MB Bank (Quân Đội)";
                if (accountNumText != null) accountNumText.text = "0988776655";
                if (accountNameText != null) accountNameText.text = "RHYTHM GAME STUDIO";
                if (transferContentText != null) transferContentText.text = orderCodeStr;
            }
            else if (channel == "ZaloPay")
            {
                if (bankNameText != null) bankNameText.text = "Ví Điện Tử ZaloPay";
                if (accountNumText != null) accountNumText.text = "0988776655";
                if (accountNameText != null) accountNameText.text = "RHYTHM GAME STUDIO";
                if (transferContentText != null) transferContentText.text = $"ZLP {orderCodeStr}";
            }
            else // MoMo
            {
                if (bankNameText != null) bankNameText.text = "Ví Điện Tử MoMo";
                if (accountNumText != null) accountNumText.text = "0988776655";
                if (accountNameText != null) accountNameText.text = "RHYTHM GAME STUDIO";
                if (transferContentText != null) transferContentText.text = $"MOM {orderCodeStr}";
            }

            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlaySelectSound();
            }
        }

        private void OnCopyAccountNumClicked()
        {
            if (accountNumText != null)
            {
                GUIUtility.systemCopyBuffer = accountNumText.text;
                ShowCopyToast("Đã sao chép Số Tài Khoản!");
            }
        }

        private void OnCopyContentClicked()
        {
            if (transferContentText != null)
            {
                GUIUtility.systemCopyBuffer = transferContentText.text;
                ShowCopyToast("Đã sao chép Nội Dung Chuyển Khoản!");
            }
        }

        private void ShowCopyToast(string msg)
        {
            if (copyToast == null) return;
            if (_copyToastRoutine != null) StopCoroutine(_copyToastRoutine);
            _copyToastRoutine = StartCoroutine(CopyToastRoutine(msg));
        }

        private IEnumerator CopyToastRoutine(string msg)
        {
            if (copyToastText != null) copyToastText.text = msg;
            copyToast.SetActive(true);
            yield return new WaitForSecondsRealtime(1.8f);
            copyToast.SetActive(false);
            _copyToastRoutine = null;
        }

        private void OnConfirmPaymentClicked()
        {
            if (_selectedPack == null) return;

            int totalAmount = _selectedPack.mainAmount + _selectedPack.bonusAmount;

            // Cộng tiền vào Wallet
            Wallet.Add(_selectedPack.currencyType, totalAmount);

            if (_selectedPack.bonusTickets > 0)
            {
                Wallet.AddTickets(_selectedPack.bonusTickets);
            }

            RefreshWallet();

            // Phát âm thanh mua sắm thành công
            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlayShopPurchase();
            }

            string unit = _selectedPack.currencyType == CurrencyType.Coin ? "Vàng" : "Kim Cương";
            string msg = $"NẠP THÀNH CÔNG!\nBạn nhận được <color=#00FFE6>+{totalAmount:N0} {unit}</color>";
            if (_selectedPack.bonusTickets > 0)
            {
                msg += $" và <color=#FFD700>+{_selectedPack.bonusTickets} Vé Quay</color>";
            }

            ClosePaymentModal();
            ShowSuccessToast(msg);
        }

        private void ShowSuccessToast(string msg)
        {
            if (successToast == null) return;
            if (successMessageText != null) successMessageText.text = msg;

            successToast.SetActive(true);
            if (successToastDialog != null)
            {
                StartCoroutine(PopDialogRoutine(successToastDialog));
            }

            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(AutoCloseSuccessToastRoutine());
        }

        public void CloseSuccessToast()
        {
            if (_toastRoutine != null) { StopCoroutine(_toastRoutine); _toastRoutine = null; }
            if (successToast != null) successToast.SetActive(false);
            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlaySelectSound();
            }
        }

        private IEnumerator AutoCloseSuccessToastRoutine()
        {
            yield return new WaitForSecondsRealtime(3.5f);
            if (successToast != null) successToast.SetActive(false);
            _toastRoutine = null;
        }

        private void StartTimer(int totalSeconds)
        {
            StopTimer();
            _timerRoutine = StartCoroutine(TimerRoutine(totalSeconds));
        }

        private void StopTimer()
        {
            if (_timerRoutine != null)
            {
                StopCoroutine(_timerRoutine);
                _timerRoutine = null;
            }
        }

        private IEnumerator TimerRoutine(int remaining)
        {
            while (remaining > 0)
            {
                int min = remaining / 60;
                int sec = remaining % 60;
                if (timerCountdownText != null)
                {
                    timerCountdownText.text = $"Hiệu lực: <color=#00FFE6>{min:D2}:{sec:D2}</color>";
                }
                yield return new WaitForSecondsRealtime(1f);
                remaining--;
            }
            if (timerCountdownText != null) timerCountdownText.text = "<color=#FF4444>Hết hạn</color>";
        }

        private IEnumerator PopDialogRoutine(RectTransform target)
        {
            target.localScale = Vector3.one * 0.7f;
            float elapsed = 0f;
            float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out back
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float eased = 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);
                target.localScale = Vector3.one * Mathf.LerpUnclamped(0.7f, 1f, eased);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        public void OnBackClicked()
        {
            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlayCloseSound();
            }

            if (!string.IsNullOrEmpty(returnSceneName))
            {
                SceneManager.LoadScene(returnSceneName);
            }
            else
            {
                SceneManager.LoadScene("Thuan_Shop");
            }
        }
    }
}
