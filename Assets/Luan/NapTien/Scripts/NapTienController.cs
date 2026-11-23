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
        [SerializeField] private Button coinBarButton;
        [SerializeField] private Button gemBarButton;

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

        [SerializeField] private Image tabGemsIcon;
        [SerializeField] private Image tabCoinsIcon;
        [SerializeField] private Image tabSpecialIcon;

        [Header("--- Tab Sprites ---")]
        [SerializeField] private Sprite tabNormalSprite;
        [SerializeField] private Sprite tabActiveCyanSprite;
        [SerializeField] private Sprite tabActiveGoldSprite;
        [SerializeField] private Sprite tabActivePinkSprite;

        [Header("--- Pack Grid Containers ---")]
        [SerializeField] private GameObject gemsContainer;
        [SerializeField] private GameObject coinsContainer;
        [SerializeField] private GameObject specialContainer;

        [Header("--- Card Views ---")]
        [SerializeField] private NapTienCardView[] gemCardViews;
        [SerializeField] private NapTienCardView[] coinCardViews;
        [SerializeField] private NapTienCardView[] specialCardViews;

        [Header("--- Pack Data (Serialized) ---")]
        [SerializeField] private TopUpPackData[] gemPacks;
        [SerializeField] private TopUpPackData[] coinPacks;
        [SerializeField] private TopUpPackData[] specialPacks;

        [Header("--- Payment Modal ---")]
        [SerializeField] private GameObject paymentModal;
        [SerializeField] private RectTransform paymentModalDialog;
        [SerializeField] private Image modalPackIcon;
        [SerializeField] private TMP_Text modalPackTitle;
        [SerializeField] private TMP_Text modalPackValue;
        [SerializeField] private TMP_Text modalPackBonus;
        [SerializeField] private TMP_Text modalPackPrice;

        [Header("Payment Channel Buttons")]
        [SerializeField] private Button btnVietQR;
        [SerializeField] private Button btnZaloPay;
        [SerializeField] private Button btnMoMo;
        [SerializeField] private Image bgVietQR;
        [SerializeField] private Image bgZaloPay;
        [SerializeField] private Image bgMoMo;
        [SerializeField] private Image imgVietQRSelect;
        [SerializeField] private Image imgZaloPaySelect;
        [SerializeField] private Image imgMoMoSelect;

        [Header("Payment Channel Sprites")]
        [SerializeField] private Sprite tabChannelNormal;
        [SerializeField] private Sprite tabChannelActiveCyan;
        [SerializeField] private Sprite tabChannelActiveBlue;
        [SerializeField] private Sprite tabChannelActivePink;

        [Header("--- Payment Details & QR ---")]
        [SerializeField] private PaymentGateway paymentGateway;
        [SerializeField] private RawImage qrRawImage;
        [SerializeField] private Image qrImage;
        [SerializeField] private Sprite qrVietQRSprite;
        [SerializeField] private Sprite qrZaloPaySprite;
        [SerializeField] private Sprite qrMoMoSprite;
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
        [SerializeField] private TMP_Text successTitleText;
        [SerializeField] private Image successIcon;
        [SerializeField] private Image successAura;
        [SerializeField] private TMP_Text successPackNameText;
        [SerializeField] private TMP_Text successAmountText;
        [SerializeField] private TMP_Text successBonusText;
        [SerializeField] private TMP_Text successMessageText;
        [SerializeField] private Button successOkButton;
        [SerializeField] private Button successOverlayButton;
        [SerializeField] private Sprite auraCyan;
        [SerializeField] private Sprite auraGold;

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

            if (coinBarButton != null) coinBarButton.onClick.AddListener(() => SwitchCategory(TopUpCategory.Coins));
            if (gemBarButton != null) gemBarButton.onClick.AddListener(() => SwitchCategory(TopUpCategory.Gems));

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
            if (successOverlayButton != null) successOverlayButton.onClick.AddListener(CloseSuccessToast);

            if (paymentModal != null) paymentModal.SetActive(false);
            if (successToast != null) successToast.SetActive(false);
            if (copyToast != null) copyToast.SetActive(false);

            if (paymentGateway == null) paymentGateway = GetComponent<PaymentGateway>() ?? gameObject.AddComponent<PaymentGateway>();
            paymentGateway.OnPaymentSuccess += OnOnlinePaymentSuccess;

            BindAllCards();
        }

        private void OnDestroy()
        {
            if (paymentGateway != null) paymentGateway.OnPaymentSuccess -= OnOnlinePaymentSuccess;
        }

        private void BindAllCards()
        {
            InitCards(gemCardViews, gemPacks);
            InitCards(coinCardViews, coinPacks);
            InitCards(specialCardViews, specialPacks);
        }

        private void InitCards(NapTienCardView[] views, TopUpPackData[] packs)
        {
            if (views == null) return;
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] == null) continue;
                TopUpPackData pack = (packs != null && i < packs.Length) ? packs[i] : views[i].Data;
                if (pack != null)
                {
                    views[i].Bind(pack, OnPackSelected);
                }
            }
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

            // Kích hoạt hiệu ứng xuất hiện cascade giật cấp mượt mà
            NapTienCardView[] activeCards = category switch
            {
                TopUpCategory.Gems => gemCardViews,
                TopUpCategory.Coins => coinCardViews,
                _ => specialCardViews
            };

            if (activeCards != null)
            {
                for (int i = 0; i < activeCards.Length; i++)
                {
                    if (activeCards[i] != null)
                    {
                        var anim = activeCards[i].GetComponent<UICardAnimation>();
                        if (anim != null) anim.PlayCascadeAppear(i * 0.045f);
                    }
                }
            }

            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlaySelectSound();
            }
        }

        private void UpdateTabButtonVisuals()
        {
            Color activeCyanTxt = new Color(0f, 1f, 0.95f, 1f);     // Neon Cyan
            Color activeGoldTxt = new Color(1f, 0.88f, 0.2f, 1f);    // Neon Gold
            Color activePinkTxt = new Color(1f, 0.35f, 0.75f, 1f);   // Neon Pink
            Color normalTxt = new Color(0.62f, 0.68f, 0.82f, 0.9f);  // Sleek Dim Grey/Blue

            bool isGems = _currentCategory == TopUpCategory.Gems;
            bool isCoins = _currentCategory == TopUpCategory.Coins;
            bool isSpecial = _currentCategory == TopUpCategory.Special;

            // Sprites
            if (tabGemsBg != null)
            {
                tabGemsBg.sprite = isGems ? (tabActiveCyanSprite != null ? tabActiveCyanSprite : tabGemsBg.sprite) : (tabNormalSprite != null ? tabNormalSprite : tabGemsBg.sprite);
                tabGemsBg.color = Color.white;
            }
            if (tabCoinsBg != null)
            {
                tabCoinsBg.sprite = isCoins ? (tabActiveGoldSprite != null ? tabActiveGoldSprite : tabCoinsBg.sprite) : (tabNormalSprite != null ? tabNormalSprite : tabCoinsBg.sprite);
                tabCoinsBg.color = Color.white;
            }
            if (tabSpecialBg != null)
            {
                tabSpecialBg.sprite = isSpecial ? (tabActivePinkSprite != null ? tabActivePinkSprite : tabSpecialBg.sprite) : (tabNormalSprite != null ? tabNormalSprite : tabSpecialBg.sprite);
                tabSpecialBg.color = Color.white;
            }

            // Text colors
            if (tabGemsText != null) tabGemsText.color = isGems ? activeCyanTxt : normalTxt;
            if (tabCoinsText != null) tabCoinsText.color = isCoins ? activeGoldTxt : normalTxt;
            if (tabSpecialText != null) tabSpecialText.color = isSpecial ? activePinkTxt : normalTxt;

            // Icon opacity & colors
            if (tabGemsIcon != null) tabGemsIcon.color = isGems ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            if (tabCoinsIcon != null) tabCoinsIcon.color = isCoins ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            if (tabSpecialIcon != null) tabSpecialIcon.color = isSpecial ? Color.white : new Color(1f, 1f, 1f, 0.45f);
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
            if (modalPackPrice != null) modalPackPrice.text = $"{_selectedPack.priceVnd:N0} đ";

            string unit = _selectedPack.currencyType == CurrencyType.Coin ? "Vàng" : "Kim Cương";
            if (modalPackBonus != null)
            {
                if (modalPackValue != null) modalPackValue.text = $"+{_selectedPack.mainAmount:N0} {unit}";
                if (_selectedPack.bonusAmount > 0)
                {
                    modalPackBonus.text = $"+{_selectedPack.bonusAmount:N0} Thưởng";
                    modalPackBonus.gameObject.SetActive(true);
                }
                else
                {
                    modalPackBonus.gameObject.SetActive(false);
                }
            }
            else if (modalPackValue != null)
            {
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
            if (paymentGateway != null) paymentGateway.StopPolling();
            if (paymentModal != null) paymentModal.SetActive(false);
            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlayCloseSound();
            }
        }

        public void SelectPaymentChannel(string channel)
        {
            _currentPaymentChannel = channel;

            // Tab Backgrounds & Highlights
            if (bgVietQR != null) bgVietQR.sprite = channel == "VietQR" ? tabChannelActiveCyan : tabChannelNormal;
            if (bgZaloPay != null) bgZaloPay.sprite = channel == "ZaloPay" ? tabChannelActiveBlue : tabChannelNormal;
            if (bgMoMo != null) bgMoMo.sprite = channel == "MoMo" ? tabChannelActivePink : tabChannelNormal;

            if (imgVietQRSelect != null) imgVietQRSelect.gameObject.SetActive(channel == "VietQR");
            if (imgZaloPaySelect != null) imgZaloPaySelect.gameObject.SetActive(channel == "ZaloPay");
            if (imgMoMoSelect != null) imgMoMoSelect.gameObject.SetActive(channel == "MoMo");

            if (amountToPayText != null && _selectedPack != null)
            {
                amountToPayText.text = $"{_selectedPack.priceVnd:N0} đ";
            }

            // Gọi PaymentGateway (Tích hợp PayOS & ZaloPay của thầy)
            if (paymentGateway != null && _selectedPack != null)
            {
                paymentGateway.CreatePayment(channel, _selectedPack.priceVnd, _selectedPack.title, (order) =>
                {
                    if (order == null) return;
                    if (bankNameText != null) bankNameText.text = order.bankName;
                    if (accountNumText != null) accountNumText.text = order.accountNum;
                    if (accountNameText != null) accountNameText.text = order.accountName;
                    if (transferContentText != null) transferContentText.text = order.transferContent;
                    if (amountToPayText != null) amountToPayText.text = $"{order.amount:N0} đ";

                    // Gán Texture QR Code tạo động từ ZXing
                    if (qrRawImage != null && order.qrTexture != null)
                    {
                        qrRawImage.texture = order.qrTexture;
                    }
                    else if (qrImage != null && order.qrTexture != null)
                    {
                        qrImage.sprite = Sprite.Create(order.qrTexture, new Rect(0, 0, order.qrTexture.width, order.qrTexture.height), new Vector2(0.5f, 0.5f));
                    }
                });
            }

            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlaySelectSound();
            }
        }

        private void OnOnlinePaymentSuccess(PaymentOrderInfo order)
        {
            DeliverPackRewards(_selectedPack, "ONLINE");
        }

        private void DeliverPackRewards(TopUpPackData pack, string source)
        {
            if (pack == null) return;

            int totalAmount = pack.mainAmount + pack.bonusAmount;

            // Cộng tiền vào Wallet
            Wallet.Add(pack.currencyType, totalAmount);

            if (pack.bonusTickets > 0)
            {
                Wallet.AddTickets(pack.bonusTickets);
            }

            RefreshWallet();

            // Phát âm thanh mua sắm thành công
            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlayShopPurchase();
            }

            string unit = pack.currencyType == CurrencyType.Coin ? "Vàng" : "Kim Cương";
            string msg = source == "ONLINE"
                ? $"THANH TOÁN THÀNH CÔNG!\nBạn nhận được <color=#00FFE6>+{totalAmount:N0} {unit}</color>"
                : $"NẠP THÀNH CÔNG!\nBạn nhận được <color=#00FFE6>+{totalAmount:N0} {unit}</color>";

            if (pack.bonusTickets > 0)
            {
                msg += $" và <color=#FFD700>+{pack.bonusTickets} Vé Quay</color>";
            }

            ClosePaymentModal();
            ShowSuccessModal(pack, source);
        }

        private void OnConfirmPaymentClicked()
        {
            DeliverPackRewards(_selectedPack, "TEST");
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



        public void ShowSuccessModal(TopUpPackData pack, string source = "")
        {
            if (successToast == null) return;

            if (successTitleText != null)
            {
                successTitleText.text = "GIAO DỊCH THÀNH CÔNG!";
            }

            if (successIcon != null)
            {
                if (pack != null && pack.iconSprite != null)
                {
                    successIcon.sprite = pack.iconSprite;
                    successIcon.enabled = true;
                }
            }

            if (successAura != null)
            {
                if (pack != null && pack.currencyType == CurrencyType.Coin)
                {
                    if (auraGold != null) successAura.sprite = auraGold;
                    successAura.color = new Color(1f, 0.85f, 0.2f, 0.35f);
                }
                else
                {
                    if (auraCyan != null) successAura.sprite = auraCyan;
                    successAura.color = new Color(0f, 0.85f, 1f, 0.35f);
                }
            }

            if (successPackNameText != null)
            {
                successPackNameText.text = pack != null && !string.IsNullOrEmpty(pack.title) ? pack.title.ToUpper() : "GÓI NẠP TIỀN";
            }

            if (successAmountText != null)
            {
                if (pack != null)
                {
                    int total = pack.mainAmount + pack.bonusAmount;
                    string unit = pack.currencyType == CurrencyType.Coin ? "VÀNG" : "KIM CƯƠNG";
                    string hexColor = pack.currencyType == CurrencyType.Coin ? "#FFD700" : "#00FFE6";
                    successAmountText.text = $"<color={hexColor}>+{total:N0} {unit}</color>";
                }
                else
                {
                    successAmountText.text = string.Empty;
                }
            }

            if (successBonusText != null)
            {
                if (pack != null && pack.bonusTickets > 0)
                {
                    successBonusText.gameObject.SetActive(true);
                    successBonusText.text = $"<color=#FFD700>+{pack.bonusTickets} VÉ QUAY MAY MẮN</color>";
                }
                else
                {
                    successBonusText.gameObject.SetActive(false);
                }
            }

            if (successMessageText != null)
            {
                successMessageText.text = "✓ Đã cộng trực tiếp vào ví của bạn";
            }

            successToast.SetActive(true);
            if (successToastDialog != null)
            {
                StartCoroutine(PopDialogRoutine(successToastDialog));
            }

            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(AutoCloseSuccessToastRoutine());
        }

        public void ShowSuccessToast(string msg)
        {
            if (_selectedPack != null)
            {
                ShowSuccessModal(_selectedPack);
            }
            else
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
            yield return new WaitForSecondsRealtime(6f);
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
                    timerCountdownText.text = $"<color=#FFD700>Hiệu lực:</color> <color=#00FFE6>{min:D2}:{sec:D2}</color>";
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
