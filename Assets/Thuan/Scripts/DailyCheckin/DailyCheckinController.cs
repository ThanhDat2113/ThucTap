using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum DailyRewardType { Coins = 0, Gems = 1, Tickets = 2, Item = 3 }

[Serializable]
public class DailyRewardDay
{
    public string rewardName = "Reward";
    [TextArea(2, 3)] public string description;
    public DailyRewardType type = DailyRewardType.Coins;
    public int amount = 100;
    [Tooltip("Chi dung khi type = Item")]
    public ShopItemData item;
    public Sprite icon;

    [Header("Sprite the theo trang thai")]
    public Sprite cardClaimed;
    public Sprite cardToday;
    public Sprite cardUpcoming;
}

public class DailyCheckinController : MonoBehaviour
{
    [Header("Data (7 ngay)")]
    [SerializeField] private DailyRewardDay[] days = new DailyRewardDay[DailyCheckin.CycleLength];

    [Header("Cards")]
    [SerializeField] private Image[] dayCards = new Image[DailyCheckin.CycleLength];
    [SerializeField] private float todayPulse = 0.035f;

    [Header("Header")]
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private string streakColor = "#FF2FA8";
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text gemText;

    [Header("Today's reward")]
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TMP_Text rewardName;
    [SerializeField] private TMP_Text rewardDescription;
    [SerializeField] private Button claimButton;
    [SerializeField] private Image claimImage;
    [SerializeField] private Sprite claimSprite;
    [SerializeField] private Sprite claimDisabledSprite;
    [SerializeField] private TMP_Text nextRewardText;
    [SerializeField] private TMP_Text toastText;

    [Header("Audio")]
    [SerializeField] private AudioClip claimClip;

    private int todayIndex;
    private bool canClaim;
    private float nextTick;
    private Coroutine toastRoutine;

    private void Awake()
    {
        if (claimButton != null) claimButton.onClick.AddListener(ClaimToday);
    }

    private void OnEnable()
    {
        Wallet.Changed += RefreshWallet;
        DailyCheckin.Changed += Refresh;
    }

    private void OnDisable()
    {
        Wallet.Changed -= RefreshWallet;
        DailyCheckin.Changed -= Refresh;
    }

    private void Start()
    {
        if (toastText != null) toastText.gameObject.SetActive(false);
        RefreshWallet();
        Refresh();
    }

    public void Refresh()
    {
        canClaim = DailyCheckin.CanClaimToday;
        todayIndex = DailyCheckin.TodayIndex;
        int claimed = DailyCheckin.ClaimedInCycle;

        for (int i = 0; i < dayCards.Length && i < days.Length; i++)
        {
            if (dayCards[i] == null || days[i] == null) continue;
            Sprite s;
            if (i < claimed) s = days[i].cardClaimed;
            else if (i == todayIndex && canClaim) s = days[i].cardToday;
            else s = days[i].cardUpcoming;
            dayCards[i].sprite = s;
            dayCards[i].rectTransform.localScale = Vector3.one;
        }

        int streak = DailyCheckin.Streak;
        if (streakText != null)
            streakText.text = string.Format("Login Streak: <color={0}>{1} {2}</color>", streakColor, streak, streak == 1 ? "Day" : "Days");

        ShowReward(days[Mathf.Clamp(todayIndex, 0, days.Length - 1)]);

        if (claimButton != null) claimButton.interactable = canClaim;
        if (claimImage != null) claimImage.sprite = canClaim ? claimSprite : claimDisabledSprite;
        if (nextRewardText != null) nextRewardText.gameObject.SetActive(!canClaim);
        UpdateCountdown();
    }

    private void ShowReward(DailyRewardDay d)
    {
        if (d == null) return;
        if (rewardIcon != null) { rewardIcon.sprite = d.icon; rewardIcon.preserveAspect = true; }
        if (rewardName != null) rewardName.text = d.rewardName;
        if (rewardDescription != null) rewardDescription.text = d.description;
    }

    private void RefreshWallet()
    {
        if (coinText != null) coinText.text = Wallet.Coins.ToString("N0");
        if (gemText != null) gemText.text = Wallet.Gems.ToString("N0");
    }

    private void Update()
    {
        // The "hom nay" nhip nhe theo nhac
        if (canClaim && todayIndex >= 0 && todayIndex < dayCards.Length && dayCards[todayIndex] != null)
        {
            float s = 1f + todayPulse * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f));
            dayCards[todayIndex].rectTransform.localScale = new Vector3(s, s, 1f);
        }

        if (!canClaim && Time.unscaledTime >= nextTick)
        {
            nextTick = Time.unscaledTime + 1f;
            if (DailyCheckin.CanClaimToday) Refresh(); // qua nua dem khi dang mo man hinh
            else UpdateCountdown();
        }
    }

    private void UpdateCountdown()
    {
        if (nextRewardText == null || canClaim) return;
        TimeSpan t = DailyCheckin.TimeUntilNextDay;
        nextRewardText.text = string.Format("NEXT REWARD IN {0:00}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds);
    }

    public void ClaimToday()
    {
        int index = DailyCheckin.Claim(); // goi Changed -> Refresh
        if (index < 0) return;

        string msg = Grant(days[index]);
        if (claimClip != null && SFXManager.Instance != null) SFXManager.Instance.PlaySFX(claimClip);
        ShowToast(msg);
        if (index < dayCards.Length && dayCards[index] != null) StartCoroutine(Punch(dayCards[index].rectTransform, 0.18f));
        if (rewardIcon != null) StartCoroutine(Punch(rewardIcon.rectTransform, 0.25f));
    }

    private string Grant(DailyRewardDay d)
    {
        switch (d.type)
        {
            case DailyRewardType.Coins:
                Wallet.Add(CurrencyType.Coin, d.amount);
                return "+" + d.amount.ToString("N0") + " COINS!";
            case DailyRewardType.Gems:
                Wallet.Add(CurrencyType.Gem, d.amount);
                return "+" + d.amount.ToString("N0") + " GEMS!";
            case DailyRewardType.Tickets:
                Wallet.AddTickets(d.amount);
                return "+" + d.amount + " TICKETS!";
            default:
                if (d.item == null) return d.rewardName.ToUpper() + "!";
                if (Wallet.IsOwned(d.item.Id))
                {
                    // Da co roi -> doi thanh xu
                    Wallet.Add(CurrencyType.Coin, d.item.price);
                    return "ALREADY OWNED  +" + d.item.price.ToString("N0") + " COINS";
                }
                Wallet.GrantItem(d.item.Id);
                return d.rewardName.ToUpper() + " UNLOCKED!";
        }
    }

    private void ShowToast(string msg)
    {
        if (toastText == null) return;
        if (toastRoutine != null) StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(msg));
    }

    private IEnumerator ToastRoutine(string msg)
    {
        var rt = toastText.rectTransform;
        Vector2 basePos = rt.anchoredPosition;
        toastText.text = msg;
        toastText.gameObject.SetActive(true);

        float t = 0f;
        while (t < 1.8f)
        {
            t += Time.unscaledDeltaTime;
            float pop = t < 0.15f ? Mathf.Lerp(0.5f, 1.15f, t / 0.15f) : (t < 0.3f ? Mathf.Lerp(1.15f, 1f, (t - 0.15f) / 0.15f) : 1f);
            rt.localScale = new Vector3(pop, pop, 1f);
            rt.anchoredPosition = basePos + new Vector2(0f, 40f * Mathf.Clamp01(t / 1.8f));
            toastText.alpha = t > 1.3f ? 1f - (t - 1.3f) / 0.5f : 1f;
            yield return null;
        }

        toastText.gameObject.SetActive(false);
        rt.anchoredPosition = basePos;
        toastText.alpha = 1f;
        toastRoutine = null;
    }

    private IEnumerator Punch(RectTransform rt, float amount)
    {
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float s = 1f + amount * Mathf.Sin(Mathf.Clamp01(t / 0.35f) * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }
}
