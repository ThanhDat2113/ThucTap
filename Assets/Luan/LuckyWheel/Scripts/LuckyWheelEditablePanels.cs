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

        public int CoinBalance => coinBalance;
        public int DiamondBalance => diamondBalance;
        public int TicketCount => ticketCount;
        public bool HasDailyFreeSpin => hasDailyFreeSpin;
        public int TotalAvailableSpins => (hasDailyFreeSpin ? 1 : 0) + ticketCount;
        public int SpinCount => TotalAvailableSpins;
        public bool HasFreeSpinForOne => TotalAvailableSpins >= 1;
        public bool HasFreeSpinForTen => TotalAvailableSpins >= 10;
        public int BonusProgress => bonusProgress;
        public int BonusTarget => bonusTarget;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (resetOnPlayMode)
                {
                    PlayerPrefs.DeleteKey(PrefsDiamondKey);
                    PlayerPrefs.DeleteKey(PrefsCoinKey);
                    PlayerPrefs.DeleteKey(PrefsTicketsKey);
                    PlayerPrefs.DeleteKey(PrefsDailyFreeDateKey);
                    PlayerPrefs.DeleteKey("LUCKY_WHEEL_SPIN_COUNT");
                    PlayerPrefs.DeleteKey(PrefsBonusProgressKey);
                    PlayerPrefs.Save();

                    diamondBalance = 1000;
                    coinBalance = 5000;
                    ticketCount = 0;
                    hasDailyFreeSpin = true;
                    bonusProgress = 0;
                }
                else
                {
                    if (PlayerPrefs.HasKey(PrefsDiamondKey))
                    {
                        diamondBalance = PlayerPrefs.GetInt(PrefsDiamondKey);
                    }
                    else
                    {
                        if (diamondBalance <= 0) diamondBalance = 1000;
                        PlayerPrefs.SetInt(PrefsDiamondKey, diamondBalance);
                        PlayerPrefs.Save();
                    }

                    if (PlayerPrefs.HasKey(PrefsCoinKey))
                    {
                        coinBalance = PlayerPrefs.GetInt(PrefsCoinKey);
                    }
                    else
                    {
                        if (coinBalance <= 0) coinBalance = 5000;
                        PlayerPrefs.SetInt(PrefsCoinKey, coinBalance);
                        PlayerPrefs.Save();
                    }

                    if (PlayerPrefs.HasKey(PrefsTicketsKey))
                    {
                        ticketCount = PlayerPrefs.GetInt(PrefsTicketsKey);
                    }
                    else if (PlayerPrefs.HasKey("LUCKY_WHEEL_SPIN_COUNT"))
                    {
                        ticketCount = PlayerPrefs.GetInt("LUCKY_WHEEL_SPIN_COUNT");
                    }

                    string lastClaimDate = PlayerPrefs.GetString(PrefsDailyFreeDateKey, string.Empty);
                    string today = DateTime.Today.ToString("yyyy-MM-dd");
                    hasDailyFreeSpin = (lastClaimDate != today);

                    if (PlayerPrefs.HasKey(PrefsBonusProgressKey))
                    {
                        bonusProgress = PlayerPrefs.GetInt(PrefsBonusProgressKey);
                    }
                }
            }
            Refresh();
            NotifyPurchaseUIRefresh();
        }

        public bool HasEnoughDiamonds(int amount)
        {
            return diamondBalance >= amount;
        }

        public bool TrySpendDiamonds(int amount)
        {
            if (diamondBalance < amount) return false;
            diamondBalance -= amount;
            if (Application.isPlaying)
            {
                PlayerPrefs.SetInt(PrefsDiamondKey, diamondBalance);
                PlayerPrefs.Save();
            }
            Refresh();
            NotifyPurchaseUIRefresh();
            return true;
        }

        public void AddDiamonds(int amount)
        {
            if (amount <= 0) return;
            diamondBalance += amount;
            if (Application.isPlaying)
            {
                PlayerPrefs.SetInt(PrefsDiamondKey, diamondBalance);
                PlayerPrefs.Save();
            }
            Refresh();
            NotifyPurchaseUIRefresh();
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            coinBalance += amount;
            if (Application.isPlaying)
            {
                PlayerPrefs.SetInt(PrefsCoinKey, coinBalance);
                PlayerPrefs.Save();
            }
            Refresh();
        }

        public void AddTickets(int amount)
        {
            if (amount <= 0) return;
            ticketCount += amount;
            if (Application.isPlaying)
            {
                PlayerPrefs.SetInt(PrefsTicketsKey, ticketCount);
                PlayerPrefs.Save();
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
                ticketCount = Mathf.Max(0, ticketCount - remaining);
                if (Application.isPlaying)
                {
                    PlayerPrefs.SetInt(PrefsTicketsKey, ticketCount);
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

        public void AddBonusProgress(int amount)
        {
            if (amount <= 0) return;
            bonusProgress = Mathf.Min(bonusTarget, bonusProgress + amount);
            if (Application.isPlaying)
            {
                PlayerPrefs.SetInt(PrefsBonusProgressKey, bonusProgress);
                PlayerPrefs.Save();
            }
            Refresh();
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
                PlayerPrefs.SetInt(PrefsDiamondKey, diamondBalance);
                PlayerPrefs.SetInt(PrefsCoinKey, coinBalance);
                PlayerPrefs.Save();
            }
            Refresh();
        }

        public void SetSpinCount(int count)
        {
            ticketCount = Mathf.Max(0, count);
            if (Application.isPlaying)
            {
                PlayerPrefs.SetInt(PrefsTicketsKey, ticketCount);
                PlayerPrefs.Save();
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

        private void OnEnable() => Refresh();
        private void OnValidate() => Refresh();

        public void Refresh()
        {
            SetText("TopBar/Currencies/EditableContent/CoinValue", coinBalance.ToString("N0", Vietnamese));
            SetText("TopBar/Currencies/EditableContent/DiamondValue", diamondBalance.ToString("N0", Vietnamese));
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
            SetText("Footer/BonusProgress/EditableContent/BonusDescription",
                $"Quay thêm <color=#FFE600>{remaining}</color> lần để nhận quà đặc biệt!");
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
