using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Luan.LuckyWheel
{
    [ExecuteAlways]
    public sealed class LuckyWheelEditablePanels : MonoBehaviour
    {
        private const string PrefsDiamondKey = "LUCKY_WHEEL_DIAMONDS";
        private const string PrefsCoinKey = "LUCKY_WHEEL_COINS";
        private const string PrefsTicketsKey = "LUCKY_WHEEL_TICKETS";
        private const string PrefsDailyFreeDateKey = "LUCKY_WHEEL_DAILY_FREE_CLAIM_DATE";
        private const string PrefsBonusProgressKey = "LUCKY_WHEEL_BONUS_PROGRESS";

        [Serializable]
        public sealed class FeaturedReward
        {
            public Sprite icon;
            public string title;
            public string subtitle;
        }

        [Header("Top currency bar")]
        [SerializeField, Min(0)] private int coinBalance;
        [SerializeField, Min(0)] private int diamondBalance;
        [SerializeField] private Sprite coinIcon;
        [SerializeField] private Sprite diamondIcon;

        [Header("Featured rewards")]
        [SerializeField] private string featuredHeading = "PHẦN THƯỞNG NỔI BẬT";
        [SerializeField] private FeaturedReward[] featuredRewards = new FeaturedReward[3];

        [Header("Spin information")]
        [FormerlySerializedAs("spinCount")]
        [SerializeField, Min(0)] private int ticketCount;
        [SerializeField, Min(0)] private int freeSpinsPerDay = 1;
        [SerializeField] private Sprite spinTicketIcon;
        [SerializeField] private Sprite informationIcon;
        [SerializeField] private Sprite calendarIcon;

        [Header("Bonus progress")]
        [SerializeField, Min(0)] private int bonusProgress;
        [SerializeField, Min(1)] private int bonusTarget = 80;
        [SerializeField] private Sprite bonusGiftIcon;
        [SerializeField] private Sprite bonusChestIcon;

        [Header("Test settings")]
        [Tooltip("Khi bật: Mỗi lần vào Play Mode sẽ tự động đặt lại 1.000 kim cương, 5.000 vàng, 1 lượt quay free hàng ngày, 0 vé để dễ test.")]
        [SerializeField] private bool resetOnPlayMode = true;

        private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
        private bool hasDailyFreeSpin;

        public static LuckyWheelEditablePanels Instance { get; private set; }

        public int CoinBalance => Application.isPlaying ? Wallet.Coins : coinBalance;
        public int DiamondBalance => Application.isPlaying ? Wallet.Gems : diamondBalance;
        public int TicketCount => Application.isPlaying ? Wallet.Tickets : ticketCount;
        public bool HasDailyFreeSpin => hasDailyFreeSpin;
        public int TotalAvailableSpins => (hasDailyFreeSpin ? 1 : 0) + TicketCount;
        public int SpinCount => TotalAvailableSpins;
        public bool HasFreeSpinForOne => TotalAvailableSpins >= 1;
        public bool HasFreeSpinForTen => TotalAvailableSpins >= 10;
        public int BonusProgress => bonusProgress;
        public int BonusTarget => bonusTarget;

        /// <summary>
        /// Kiểm tra xem người chơi còn lượt quay miễn phí hay không (dùng cho MainMenu, QuickMenu, v.v.).
        /// </summary>
        public static bool CheckHasFreeSpin()
        {
            if (Instance != null)
            {
                return Instance.HasFreeSpinForOne;
            }

            // 1. Kiểm tra vé quay (TicketCount) từ Wallet
            int tickets = Wallet.Tickets;
            if (tickets <= 0 && PlayerPrefs.HasKey(PrefsTicketsKey))
            {
                tickets = PlayerPrefs.GetInt(PrefsTicketsKey, 0);
            }
            if (tickets > 0) return true;

            // 2. Kiểm tra lượt quay hàng ngày (mỗi ngày 1 lượt nếu chưa nhận hôm nay)
            string lastClaimDate = PlayerPrefs.GetString(PrefsDailyFreeDateKey, string.Empty);
            string today = DateTime.Today.ToString("yyyy-MM-dd");
            return lastClaimDate != today;
        }

        /// <summary>
        /// Định dạng số dư tiền tệ:
        /// - Dưới 10.000: hiển thị đầy đủ (ví dụ: 5.000, 1.100, 9.999).
        /// - Từ 10.000 trở lên: tính theo k (ví dụ: 10k, 10.5k, 100k, 999k).
        /// - Từ 1.000.000 trở lên: tính theo M (ví dụ: 1M, 1.5M, 10M).
        /// - Từ 1.000.000.000 trở lên: tính theo B (ví dụ: 1B, 1.5B).
        /// </summary>
        public static string FormatCurrency(long value)
        {
            if (value < 0) return "-" + FormatCurrency(-value);
            if (value < 10000)
            {
                return value.ToString("N0", Vietnamese);
            }

            if (value < 1_000_000)
            {
                double k = value / 1000.0;
                if (k >= 999.95) return "1M";
                string formatted = k >= 100
                    ? Math.Floor(k).ToString("0", CultureInfo.InvariantCulture)
                    : k.ToString("0.#", CultureInfo.InvariantCulture);
                return formatted + "k";
            }

            if (value < 1_000_000_000)
            {
                double m = value / 1_000_000.0;
                if (m >= 999.95) return "1B";
                string formatted = m >= 100
                    ? Math.Floor(m).ToString("0", CultureInfo.InvariantCulture)
                    : m.ToString("0.#", CultureInfo.InvariantCulture);
                return formatted + "M";
            }

            double b = value / 1_000_000_000.0;
            string formattedB = b >= 100
                ? Math.Floor(b).ToString("0", CultureInfo.InvariantCulture)
                : b.ToString("0.#", CultureInfo.InvariantCulture);
            return formattedB + "B";
        }

        public static string FormatCurrency(int value) => FormatCurrency((long)value);

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                Instance = this;

                // Đồng bộ 1 lần dữ liệu cũ nếu trước đó chỉ lưu trong LuckyWheel mà Wallet đang = 0
                if (PlayerPrefs.HasKey(PrefsCoinKey) && !PlayerPrefs.HasKey("WALLET_MIGRATED_FROM_LW"))
                {
                    int oldCoins = PlayerPrefs.GetInt(PrefsCoinKey, 0);
                    int oldDiamonds = PlayerPrefs.GetInt(PrefsDiamondKey, 0);
                    int oldTickets = PlayerPrefs.GetInt(PrefsTicketsKey, 0);

                    if (Wallet.Coins == 0 && oldCoins > 0) Wallet.SetCoins(oldCoins, false);
                    if (Wallet.Gems == 0 && oldDiamonds > 0) Wallet.SetGems(oldDiamonds, false);
                    if (Wallet.Tickets == 0 && oldTickets > 0) Wallet.SetTickets(oldTickets, false);

                    PlayerPrefs.SetInt("WALLET_MIGRATED_FROM_LW", 1);
                    PlayerPrefs.Save();
                }

                string lastClaimDate = PlayerPrefs.GetString(PrefsDailyFreeDateKey, string.Empty);
                string today = DateTime.Today.ToString("yyyy-MM-dd");
                hasDailyFreeSpin = (lastClaimDate != today);

                if (PlayerPrefs.HasKey(PrefsBonusProgressKey))
                {
                    bonusProgress = PlayerPrefs.GetInt(PrefsBonusProgressKey);
                }
            }
            Refresh();
            NotifyPurchaseUIRefresh();
        }

        private void Start()
        {
            SetupChestClickListener();
        }

        private void SetupChestClickListener()
        {
            var chestTransform = transform.Find("Footer/BonusProgress/EditableContent/ChestIcon");
            if (chestTransform != null)
            {
                var chestBtn = chestTransform.GetComponent<Button>();
                if (chestBtn == null)
                {
                    chestBtn = chestTransform.gameObject.AddComponent<Button>();
                }
                var img = chestTransform.GetComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = true;
                    chestBtn.targetGraphic = img;
                }
                chestBtn.onClick.RemoveListener(OnChestClicked);
                chestBtn.onClick.AddListener(OnChestClicked);
            }
        }

        public void OnChestClicked()
        {
            if (bonusProgress >= bonusTarget)
            {
                var wheelController = FindFirstObjectByType<LuckyWheelController>();
                if (wheelController != null && !wheelController.IsSpinning)
                {
                    wheelController.ClaimPityRewardDirectly();
                }
            }
            else
            {
                int remaining = bonusTarget - bonusProgress;
                Debug.Log($"[LuckyWheel] Bảo hiểm: Hãy quay thêm {remaining} lần để nhận Nhân vật huyền thoại!");
            }
        }

        public bool HasEnoughDiamonds(int amount)
        {
            return DiamondBalance >= amount;
        }

        public bool TrySpendDiamonds(int amount)
        {
            if (DiamondBalance < amount) return false;
            if (Application.isPlaying)
            {
                Wallet.Add(CurrencyType.Gem, -amount);
            }
            else
            {
                diamondBalance -= amount;
            }
            Refresh();
            NotifyPurchaseUIRefresh();
            return true;
        }

        public void AddDiamonds(int amount)
        {
            if (amount <= 0) return;
            if (Application.isPlaying)
            {
                Wallet.Add(CurrencyType.Gem, amount);
            }
            else
            {
                diamondBalance += amount;
            }
            Refresh();
            NotifyPurchaseUIRefresh();
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            if (Application.isPlaying)
            {
                Wallet.Add(CurrencyType.Coin, amount);
            }
            else
            {
                coinBalance += amount;
            }
            Refresh();
        }

        public void AddTickets(int amount)
        {
            if (amount <= 0) return;
            if (Application.isPlaying)
            {
                Wallet.AddTickets(amount);
            }
            else
            {
                ticketCount += amount;
            }
            Refresh();
            NotifyPurchaseUIRefresh();
        }

        public void AddSpinCount(int count) => AddTickets(count);

        public bool ConsumeFreeSpins(int count)
        {
            if (TotalAvailableSpins < count) return false;

            var remaining = count;
            if (hasDailyFreeSpin && remaining > 0)
            {
                hasDailyFreeSpin = false;
                remaining--;
                if (Application.isPlaying)
                {
                    PlayerPrefs.SetString(PrefsDailyFreeDateKey, DateTime.Today.ToString("yyyy-MM-dd"));
                }
            }

            if (remaining > 0)
            {
                if (Application.isPlaying)
                {
                    Wallet.AddTickets(-remaining);
                }
                else
                {
                    ticketCount = Mathf.Max(0, ticketCount - remaining);
                }
            }

            if (Application.isPlaying)
            {
                PlayerPrefs.Save();
            }

            Refresh();
            NotifyPurchaseUIRefresh();
            return true;
        }

        public void NotifyPurchaseUIRefresh()
        {
            var purchaseUI = FindFirstObjectByType<LuckyWheelPurchaseUI>();
            if (purchaseUI != null)
            {
                purchaseUI.Refresh();
            }
        }

        public void SetBonusProgress(int progress)
        {
            bonusProgress = Mathf.Clamp(progress, 0, bonusTarget);
            if (Application.isPlaying)
            {
                PlayerPrefs.SetInt(PrefsBonusProgressKey, bonusProgress);
                PlayerPrefs.Save();
            }
            Refresh();
        }

        public void ResetBonusProgress()
        {
            SetBonusProgress(0);
        }

        public void AddBonusProgress(int amount)
        {
            if (amount <= 0) return;
            SetBonusProgress(bonusProgress + amount);
        }

        [ContextMenu("Add 1000 Diamonds")]
        public void DebugAddDiamonds() => AddDiamonds(1000);

        [ContextMenu("Test: Add 1 Ticket")]
        public void DebugAddOneTicket() => AddTickets(1);

        [ContextMenu("Test: Add 10 Tickets (Test Free x10)")]
        public void DebugAddTenTickets() => AddTickets(10);

        [ContextMenu("Test: Reset to 0 Free Spins")]
        public void DebugSetZeroFreeSpins()
        {
            hasDailyFreeSpin = false;
            ticketCount = 0;
            if (Application.isPlaying)
            {
                PlayerPrefs.SetString(PrefsDailyFreeDateKey, DateTime.Today.ToString("yyyy-MM-dd"));
                PlayerPrefs.SetInt(PrefsTicketsKey, 0);
                PlayerPrefs.Save();
            }
            Refresh();
            NotifyPurchaseUIRefresh();
        }

        [ContextMenu("Test: Give Daily Free Spin (1 Free)")]
        public void DebugGiveDailyFreeSpin()
        {
            hasDailyFreeSpin = true;
            if (Application.isPlaying)
            {
                PlayerPrefs.DeleteKey(PrefsDailyFreeDateKey);
                PlayerPrefs.Save();
            }
            Refresh();
            NotifyPurchaseUIRefresh();
        }

        [ContextMenu("Reset Balance (1000 Diamonds, 5000 Coins)")]
        public void ResetBalanceToDefault()
        {
            diamondBalance = 1000;
            coinBalance = 5000;
            ticketCount = 0;
            hasDailyFreeSpin = true;
            bonusProgress = 0;
            if (Application.isPlaying)
            {
                PlayerPrefs.SetInt(PrefsDiamondKey, 1000);
                PlayerPrefs.SetInt(PrefsCoinKey, 5000);
                PlayerPrefs.SetInt(PrefsTicketsKey, 0);
                PlayerPrefs.DeleteKey(PrefsDailyFreeDateKey);
                PlayerPrefs.SetInt(PrefsBonusProgressKey, 0);
                PlayerPrefs.Save();
            }
            Refresh();
            NotifyPurchaseUIRefresh();
        }

        public void ResetVisibleValues()
        {
            coinBalance = 0;
            diamondBalance = 0;
            ticketCount = 0;
            hasDailyFreeSpin = false;
            freeSpinsPerDay = 0;
            bonusProgress = 0;
            bonusTarget = 80;
            if (Application.isPlaying)
            {
                PlayerPrefs.SetInt(PrefsDiamondKey, 0);
                PlayerPrefs.SetInt(PrefsCoinKey, 0);
                PlayerPrefs.SetInt(PrefsTicketsKey, 0);
                PlayerPrefs.DeleteKey(PrefsDailyFreeDateKey);
                PlayerPrefs.SetInt(PrefsBonusProgressKey, 0);
                PlayerPrefs.Save();
            }
            Refresh();
            NotifyPurchaseUIRefresh();
        }

        public void SetBalances(int coins, int diamonds)
        {
            coinBalance = Mathf.Max(0, coins);
            diamondBalance = Mathf.Max(0, diamonds);
            if (Application.isPlaying)
            {
                Wallet.SetCurrencies(coins, diamonds, Wallet.Tickets);
            }
            Refresh();
        }

        public void SetSpinCount(int count)
        {
            ticketCount = Mathf.Max(0, count);
            if (Application.isPlaying)
            {
                Wallet.SetTickets(ticketCount);
            }
            Refresh();
            NotifyPurchaseUIRefresh();
        }

        public void SetBonusProgress(int progress, int target)
        {
            bonusTarget = Mathf.Max(1, target);
            bonusProgress = Mathf.Clamp(progress, 0, bonusTarget);
            if (Application.isPlaying)
            {
                PlayerPrefs.SetInt(PrefsBonusProgressKey, bonusProgress);
                PlayerPrefs.Save();
            }
            Refresh();
        }

        public void SetFeaturedReward(int index, Sprite icon, string title, string subtitle)
        {
            if (featuredRewards == null || index < 0 || index >= featuredRewards.Length) return;
            featuredRewards[index] ??= new FeaturedReward();
            featuredRewards[index].icon = icon;
            featuredRewards[index].title = title;
            featuredRewards[index].subtitle = subtitle;
            Refresh();
        }

        public void SetDefaultSprites(Sprite coins, Sprite diamonds, Sprite ticket, Sprite gift, Sprite chest,
            Sprite character, Sprite jacket, Sprite rewardChest)
        {
            coinIcon = coins;
            diamondIcon = diamonds;
            spinTicketIcon = ticket;
            bonusGiftIcon = gift;
            bonusChestIcon = chest;
            featuredRewards = new[]
            {
                new FeaturedReward { icon = character, title = "Nhân vật", subtitle = "Ánh Sao" },
                new FeaturedReward { icon = jacket, title = "Skin", subtitle = "Bóng Đêm" },
                new FeaturedReward { icon = rewardChest, title = "Rương quà", subtitle = "Cao Cấp" }
            };
            Refresh();
        }

        public void SetDetailIcons(Sprite gift, Sprite calendar, Sprite information)
        {
            bonusGiftIcon = gift;
            calendarIcon = calendar;
            informationIcon = information;
            Refresh();
        }

        private void OnEnable()
        {
            Wallet.Changed += OnWalletCurrencyChanged;
            Refresh();
        }

        private void OnDisable()
        {
            Wallet.Changed -= OnWalletCurrencyChanged;
        }

        private void OnWalletCurrencyChanged()
        {
            Refresh();
            NotifyPurchaseUIRefresh();
        }

        private void OnValidate() => Refresh();

        public void Refresh()
        {
            SetText("TopBar/Currencies/EditableContent/CoinValue", FormatCurrency(CoinBalance));
            SetText("TopBar/Currencies/EditableContent/DiamondValue", FormatCurrency(DiamondBalance));
            SetSprite("TopBar/Currencies/EditableContent/CoinIcon", coinIcon);
            SetSprite("TopBar/Currencies/EditableContent/DiamondIcon", diamondIcon);

            SetText("RightPanel/FeaturedRewards/EditableContent/Heading", featuredHeading);
            if (featuredRewards != null)
            {
                for (var i = 0; i < Mathf.Min(3, featuredRewards.Length); i++)
                {
                    var reward = featuredRewards[i];
                    if (reward == null) continue;
                    var prefix = $"RightPanel/FeaturedRewards/EditableContent/Reward{i + 1}";
                    SetSprite(prefix + "/Icon", reward.icon);
                    SetText(prefix + "/Title", reward.title);
                    SetText(prefix + "/Subtitle", reward.subtitle);
                }
            }

            SetText("RightPanel/SpinInfo/EditableContent/SpinCount", TotalAvailableSpins.ToString(Vietnamese));
            SetText("RightPanel/SpinInfo/EditableContent/FreeSpinText", $"{freeSpinsPerDay} lượt miễn phí mỗi ngày");
            SetSprite("RightPanel/SpinInfo/EditableContent/TicketIcon", spinTicketIcon);
            SetSprite("RightPanel/SpinInfo/EditableContent/InfoIcon", informationIcon);
            SetSprite("RightPanel/SpinInfo/EditableContent/CalendarIcon", calendarIcon);

            var remaining = Mathf.Max(0, bonusTarget - bonusProgress);
            if (remaining <= 0)
            {
                SetText("Footer/BonusProgress/EditableContent/BonusDescription",
                    "<color=#00FFCC>Bảo hiểm kích hoạt! Lượt quay tiếp theo 100% nhận quà đặc biệt!</color>");
            }
            else
            {
                SetText("Footer/BonusProgress/EditableContent/BonusDescription",
                    $"Quay thêm <color=#FFE600>{remaining}</color> lần để nhận quà đặc biệt!");
            }
            SetText("Footer/BonusProgress/EditableContent/ProgressLabel", $"{bonusProgress}/{bonusTarget}");
            SetSprite("Footer/BonusProgress/EditableContent/GiftIcon", bonusGiftIcon);
            SetSprite("Footer/BonusProgress/EditableContent/ChestIcon", bonusChestIcon);
            var fill = transform.Find("Footer/BonusProgress/EditableContent/ProgressFill")?.GetComponent<Image>();
            if (fill != null)
            {
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.fillOrigin = 0;
                fill.fillAmount = Mathf.Clamp01((float)bonusProgress / Mathf.Max(1, bonusTarget));
            }
        }

        private void SetText(string path, string value)
        {
            var label = transform.Find(path)?.GetComponent<TMP_Text>();
            if (label != null) label.text = value;
        }

        private void SetSprite(string path, Sprite value)
        {
            var image = transform.Find(path)?.GetComponent<Image>();
            if (image != null) image.sprite = value;
        }
    }
}
