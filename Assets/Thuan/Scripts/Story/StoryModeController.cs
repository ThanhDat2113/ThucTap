using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Tab STORY MODE trong man chon nhac: chon tuan -> choi 3 bai lien tiep
public class StoryModeController : MonoBehaviour
{
    [SerializeField] private List<StoryWeekData> weeks = new List<StoryWeekData>();
    [SerializeField] private StoryWeekCardUI[] cards;
    [SerializeField] private RectTransform pointer;

    [Header("Chi tiet tuan")]
    [SerializeField] private TMP_Text detailWeek;
    [SerializeField] private TMP_Text detailTitle;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private Image detailArt;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text clearRateText;
    [SerializeField] private Image nextCover;
    [SerializeField] private TMP_Text nextTitle;

    [Header("Start")]
    [SerializeField] private Button startButton;
    [SerializeField] private CanvasGroup startGroup;
    [SerializeField] private TMP_Text startHint;
    [SerializeField] private string gameplayScene = "GameplayScene";

    [Header("Audio")]
    [SerializeField] private AudioClip selectClip;
    [SerializeField] private AudioClip lockedClip;

    private StoryWeekCardUI selected;
    private Coroutine artRoutine;
    private Vector2 artBasePos;
    private const string LastWeekKey = "story_last_week";

    private void Awake()
    {
        if (startButton != null) startButton.onClick.AddListener(StartWeek);
        if (detailArt != null) artBasePos = detailArt.rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        if (cards == null || cards.Length == 0) return;
        Build();
    }

    private void Build()
    {
        StorySession.End();
        for (int i = 0; i < cards.Length; i++)
        {
            bool has = i < weeks.Count && weeks[i] != null;
            cards[i].gameObject.SetActive(has);
            if (!has) continue;
            var w = weeks[i];
            // Mo khoa theo tien do: qua tuan truoc thi mo tuan sau
            bool prevCleared = i == 0 || StoryProgress.IsCleared(weeks[i - 1]);
            string msg = i == 0 ? "" : "CLEAR WEEK " + weeks[i - 1].weekNumber;
            cards[i].Setup(w, prevCleared, msg, OnCardClicked);
        }
        int last = Mathf.Clamp(PlayerPrefs.GetInt(LastWeekKey, 0), 0, Mathf.Min(weeks.Count, cards.Length) - 1);
        Select(cards[last], false);
    }

    private void OnCardClicked(StoryWeekCardUI card)
    {
        if (card == selected) return;
        Play(card.Locked ? lockedClip : selectClip);
        Select(card, true);
    }

    private void Select(StoryWeekCardUI card, bool animate)
    {
        if (selected != null) selected.SetSelected(false);
        selected = card;
        selected.SetSelected(true);
        PlayerPrefs.SetInt(LastWeekKey, System.Array.IndexOf(cards, card));
        if (pointer != null)
        {
            var crt = (RectTransform)card.transform;
            pointer.anchoredPosition = new Vector2(pointer.anchoredPosition.x, crt.anchoredPosition.y);
        }
        RefreshDetail();
        if (detailArt != null)
        {
            if (artRoutine != null) StopCoroutine(artRoutine);
            if (animate) artRoutine = StartCoroutine(SlideArt());
        }
    }

    private void RefreshDetail()
    {
        if (selected == null) return;
        var w = selected.Week;
        detailWeek.text = "WEEK " + w.weekNumber;
        detailTitle.text = w.title;
        detailDescription.text = w.description;
        if (detailArt != null) { detailArt.sprite = w.detailArt; detailArt.color = selected.Locked ? new Color(0.45f, 0.45f, 0.55f) : Color.white; }

        int best = StoryProgress.GetBestScore(w);
        float rate = StoryProgress.GetClearRate(w);
        bestScoreText.text = best > 0 ? best.ToString("N0") : "--";
        rankText.text = best > 0 ? StoryProgress.Rank(rate) + " RANK" : "";
        clearRateText.text = rate >= 0f ? rate.ToString("0.0") + "%" : "--";

        var first = w.tracks != null && w.tracks.Length > 0 ? w.tracks[0] : null;
        if (nextTitle != null) nextTitle.text = first != null ? first.DisplayTitle : "";
        if (nextCover != null)
        {
            Sprite c = first != null && first.song != null ? first.song.coverArt : null;
            nextCover.sprite = c; nextCover.color = c != null ? (selected.Locked ? new Color(0.4f, 0.4f, 0.5f) : Color.white) : new Color(0.1f, 0.05f, 0.2f);
        }

        bool canStart = !selected.Locked && w.Playable;
        startButton.interactable = canStart;
        if (startGroup != null) startGroup.alpha = canStart ? 1f : 0.45f;
        if (startHint != null) { startHint.gameObject.SetActive(!canStart); startHint.text = selected.Locked ? "CLEAR THE PREVIOUS WEEK FIRST" : "SONGS COMING SOON"; }
    }

    private IEnumerator SlideArt()
    {
        var rt = detailArt.rectTransform;
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.25f), 3f);
            rt.anchoredPosition = artBasePos + new Vector2(80f * (1f - e), 0f);
            var c = detailArt.color; c.a = e; detailArt.color = c;
            yield return null;
        }
        rt.anchoredPosition = artBasePos;
        artRoutine = null;
    }

    private void StartWeek()
    {
        if (selected == null || selected.Locked || !selected.Week.Playable) return;
        StorySession.Begin(selected.Week);
        Luan.Dialogue.StoryDialogueManager.ResetSessionState();
        if (GameSession.SelectedSong != null) SongStats.IncrementPlayCount(GameSession.SelectedSong);
        SceneManager.LoadScene(gameplayScene);
    }

    private static void Play(AudioClip clip) { if (clip != null && SFXManager.Instance != null) SFXManager.Instance.PlaySFX(clip); }

    public StoryWeekData[] Weeks { get { return weeks.ToArray(); } }
}
