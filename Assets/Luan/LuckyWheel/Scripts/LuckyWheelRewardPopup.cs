using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Luan.LuckyWheel
{
    public sealed class LuckyWheelRewardPopup : MonoBehaviour
    {
        [SerializeField] private bool showTenResults;
        [SerializeField] private LuckyWheelController wheel;
        [SerializeField] private Image prizeIcon;
        [SerializeField] private TMP_Text prizeName;
        [SerializeField] private TMP_Text prizeAmount;
        [SerializeField] private Button claimButton;
        [SerializeField] private Image[] tenIcons;
        [SerializeField] private TMP_Text[] tenLabels;

        private readonly List<LuckyWheelController.Prize> lastClaimedPrizes = new List<LuckyWheelController.Prize>();
        private readonly List<LuckyWheelController.Prize> pendingLegacyPrizes = new List<LuckyWheelController.Prize>();

        public bool ShowTenResults => showTenResults;

        private void Awake()
        {
            HideImmediate();
            if (wheel == null) wheel = FindFirstObjectByType<LuckyWheelController>();
            if (wheel != null)
            {
                wheel.onSinglePrizeWon.AddListener(ShowSinglePrize);
                wheel.onTenPrizesWon.AddListener(ShowTenPrizes);
            }
            if (claimButton != null) claimButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            if (wheel != null)
            {
                wheel.onSinglePrizeWon.RemoveListener(ShowSinglePrize);
                wheel.onTenPrizesWon.RemoveListener(ShowTenPrizes);
            }
            if (claimButton != null) claimButton.onClick.RemoveListener(Hide);
        }

        public void ShowSinglePrize(LuckyWheelController.Prize prize)
        {
            if (showTenResults || prize == null) return;
            lastClaimedPrizes.Clear();
            lastClaimedPrizes.Add(prize);

            if (prizeIcon != null)
            {
                prizeIcon.sprite = prize.icon;
                prizeIcon.enabled = prize.icon != null;
            }

            if (prizeName != null)
            {
                var title = prize.displayName;
                if (!string.IsNullOrEmpty(title) && title.Contains("\n"))
                {
                    title = title.Split('\n')[0].Trim();
                }
                prizeName.text = title;
            }

            if (prizeAmount != null)
            {
                prizeAmount.text = "x" + prize.amount.ToString("N0");
            }

            OpenPopup();
        }

        public void ShowTenPrizes(List<LuckyWheelController.Prize> prizes)
        {
            if (!showTenResults || prizes == null) return;
            lastClaimedPrizes.Clear();
            lastClaimedPrizes.AddRange(prizes);

            if (tenIcons != null)
            {
                for (var i = 0; i < tenIcons.Length; i++)
                {
                    var hasPrize = i < prizes.Count;
                    if (tenIcons[i] != null)
                    {
                        tenIcons[i].sprite = hasPrize ? prizes[i].icon : null;
                        tenIcons[i].enabled = hasPrize && prizes[i].icon != null;
                    }
                    if (tenLabels != null && i < tenLabels.Length && tenLabels[i] != null)
                    {
                        tenLabels[i].text = hasPrize ? "x" + prizes[i].amount.ToString("N0") : "";
                    }
                }
            }

            OpenPopup();
        }

        public void ShowPrize(string prizeId)
        {
            if (wheel == null) return;
            foreach (var prize in wheel.Prizes)
            {
                if (prize.id != prizeId) continue;
                if (wheel.CurrentSpinCount == 10)
                {
                    if (showTenResults) pendingLegacyPrizes.Add(prize);
                    return;
                }
                if (showTenResults) return;
                ShowSinglePrize(prize);
                return;
            }
        }

        private void ShowTenResultsLegacy()
        {
            if (!showTenResults || wheel == null || wheel.CurrentSpinCount != 10) return;
            if (pendingLegacyPrizes.Count > 0)
            {
                ShowTenPrizes(new List<LuckyWheelController.Prize>(pendingLegacyPrizes));
                pendingLegacyPrizes.Clear();
            }
        }

        public void OpenPopup()
        {
            gameObject.SetActive(true);
            var group = GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 1f;
                group.interactable = true;
                group.blocksRaycasts = true;
            }
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            ClaimPendingPrizes();
            HideImmediate();
        }

        public void HideImmediate()
        {
            var group = GetComponent<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        private void ClaimPendingPrizes()
        {
            if (lastClaimedPrizes.Count == 0) return;
            var panels = FindFirstObjectByType<LuckyWheelEditablePanels>();
            if (panels != null)
            {
                foreach (var p in lastClaimedPrizes)
                {
                    if (p == null) continue;
                    if (p.id.Contains("diamond"))
                    {
                        panels.AddDiamonds(p.amount);
                    }
                    else if (p.id.Contains("gold"))
                    {
                        panels.AddCoins(p.amount);
                    }
                    else if (p.id.Contains("ticket"))
                    {
                        panels.AddTickets(p.amount);
                    }
                }
            }
            lastClaimedPrizes.Clear();
        }
    }
}
