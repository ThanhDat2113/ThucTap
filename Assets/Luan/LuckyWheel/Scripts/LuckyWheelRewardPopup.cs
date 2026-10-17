using System;
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

        [Header("Frames & Highlighting")]
        [SerializeField] private Sprite normalFrameSprite;
        [SerializeField] private Sprite legendaryFrameSprite;
        [SerializeField] private string rareItemId = "rare_item";

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

        public static string GetCleanPrizeName(LuckyWheelController.Prize prize)
        {
            if (prize == null) return string.Empty;
            if (prize.id == "rare_item") return "Ánh Sao";
            var title = prize.displayName ?? string.Empty;
            if (title.Contains("\n"))
            {
                var lines = title.Split('\n');
                if (lines.Length > 1 && (lines[1].TrimStart().StartsWith("x", StringComparison.OrdinalIgnoreCase) || (lines[1].TrimStart().Length > 0 && char.IsDigit(lines[1].TrimStart()[0]))))
                {
                    return lines[0].Trim();
                }
                else
                {
                    return string.Join(" ", lines).Trim();
                }
            }
            return title;
        }

        private TMP_Text GetOrCreateNameLabel(Transform slotTransform, TMP_FontAsset font)
        {
            var child = slotTransform.Find("NameLabel");
            if (child != null)
            {
                var txt = child.GetComponent<TMP_Text>();
                if (txt != null) return txt;
            }

            var go = new GameObject("NameLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(slotTransform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -22f);
            rt.sizeDelta = new Vector2(120f, 20f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = 12f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 8f;
            tmp.fontSizeMax = 12f;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        private GameObject GetOrCreateRarityBadge(Transform slotTransform, TMP_FontAsset font)
        {
            var child = slotTransform.Find("RarityBadge");
            if (child != null) return child.gameObject;

            var go = new GameObject("RarityBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(slotTransform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-2f, -2f);
            rt.sizeDelta = new Vector2(48f, 18f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.92f, 0.12f, 0.35f, 0.95f);
            img.raycastTarget = false;

            var labelGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;
            labelRt.pivot = new Vector2(0.5f, 0.5f);

            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = "SSR";
            tmp.fontSize = 11f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.95f, 0.2f, 1f);
            tmp.raycastTarget = false;

            return go;
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
                bool isRare = prize.id == rareItemId || prize.id == "rare_item";
                if (isRare)
                {
                    prizeName.text = "<color=#FFE600>Nhân vật Ánh Sao</color>";
                }
                else
                {
                    prizeName.text = GetCleanPrizeName(prize);
                }
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
                var font = tenLabels != null && tenLabels.Length > 0 && tenLabels[0] != null ? tenLabels[0].font : null;

                for (var i = 0; i < tenIcons.Length; i++)
                {
                    var hasPrize = i < prizes.Count;
                    var prize = hasPrize ? prizes[i] : null;
                    bool isRare = prize != null && (prize.id == rareItemId || prize.id == "rare_item");

                    if (tenIcons[i] != null)
                    {
                        tenIcons[i].sprite = hasPrize ? prize.icon : null;
                        tenIcons[i].enabled = hasPrize && prize.icon != null;

                        var iconRt = tenIcons[i].rectTransform;
                        if (iconRt != null)
                        {
                            iconRt.anchoredPosition = new Vector2(0f, 18f);
                            iconRt.sizeDelta = new Vector2(62f, 62f);
                        }

                        var slotTransform = tenIcons[i].transform.parent;
                        if (slotTransform != null)
                        {
                            var frameImg = slotTransform.GetComponent<Image>();
                            if (frameImg != null)
                            {
                                if (normalFrameSprite == null && frameImg.sprite != null && frameImg.sprite != legendaryFrameSprite)
                                {
                                    normalFrameSprite = frameImg.sprite;
                                }

                                if (isRare)
                                {
                                    if (legendaryFrameSprite != null) frameImg.sprite = legendaryFrameSprite;
                                    frameImg.color = new Color(1f, 0.9f, 0.4f, 1f);
                                }
                                else
                                {
                                    if (normalFrameSprite != null) frameImg.sprite = normalFrameSprite;
                                    frameImg.color = Color.white;
                                }
                            }

                            var badge = GetOrCreateRarityBadge(slotTransform, font);
                            if (badge != null)
                            {
                                badge.SetActive(isRare);
                            }

                            var nameLabel = GetOrCreateNameLabel(slotTransform, font);
                            if (nameLabel != null)
                            {
                                if (hasPrize)
                                {
                                    nameLabel.gameObject.SetActive(true);
                                    if (isRare)
                                    {
                                        nameLabel.text = "<color=#FFE600><b>Ánh Sao</b></color>";
                                    }
                                    else
                                    {
                                        nameLabel.text = GetCleanPrizeName(prize);
                                    }
                                }
                                else
                                {
                                    nameLabel.gameObject.SetActive(false);
                                }
                            }
                        }
                    }

                    if (tenLabels != null && i < tenLabels.Length && tenLabels[i] != null)
                    {
                        var labelRt = tenLabels[i].rectTransform;
                        if (labelRt != null)
                        {
                            labelRt.anchoredPosition = new Vector2(0f, -42f);
                            labelRt.sizeDelta = new Vector2(110f, 18f);
                        }
                        tenLabels[i].enableAutoSizing = true;
                        tenLabels[i].fontSizeMin = 8f;
                        tenLabels[i].fontSizeMax = 12f;

                        if (hasPrize)
                        {
                            string prefix = isRare ? "<color=#FFE600>" : "<color=#00FFCC>";
                            tenLabels[i].text = $"{prefix}x{prize.amount.ToString("N0")}</color>";
                        }
                        else
                        {
                            tenLabels[i].text = string.Empty;
                        }
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

            if (wheel != null)
            {
                wheel.PlayWinSound();
            }
            else if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlayWheelWin();
            }
        }

        public void Hide()
        {
            if (wheel != null)
            {
                wheel.PlayConfirmClickSound();
            }
            else if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlayConfirmSound();
            }
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
                    else if (p.id == "rare_item" || p.id.Contains("character"))
                    {
                        PlayerPrefs.SetInt("UNLOCKED_LEGENDARY_CHARACTER", 1);
                        PlayerPrefs.Save();
                        Debug.Log($"[LuckyWheel] Đã mở khóa phần thưởng đặc biệt: {p.displayName} ({p.id})");
                    }
                }
            }
            lastClaimedPrizes.Clear();
        }
    }
}
