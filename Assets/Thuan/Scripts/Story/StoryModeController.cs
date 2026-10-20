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

    [Header("Do kho: 0 Easy, 1 Normal, 2 Hard")]
    [SerializeField] private Button[] difficultyButtons = new Button[3];
    [SerializeField] private Sprite[] difficultyOn = new Sprite[3];
    [SerializeField] private Sprite[] difficultyOff = new Sprite[3];
    [SerializeField] private TMP_Text[] difficultyLabels = new TMP_Text[3];

    [Header("Start")]
    [SerializeField] private Button startButton;
    [SerializeField] private CanvasGroup startGroup;
    [SerializeField] private TMP_Text startHint;
    [SerializeField] private string gameplayScene = "Thuan_Gameplay";

    [Header("Audio")]
    [SerializeField] private AudioClip selectClip;
    [SerializeField] private AudioClip lockedClip;

    private StoryWeekCardUI selected;
    private Coroutine artRoutine;
    private Vector2 artBasePos;
    private const string LastWeekKey = "story_last_week";

    private void Awake()
    {
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            if (difficultyButtons[i] == null) continue;
            int idx = i;
            difficultyButtons[i].onClick.AddListener(() => { GameSession.SelectedDifficulty = (ChartDifficulty)idx; RefreshDetail(); });
        }
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
            bool prevCleared = i == 0 || StoryProgress.IsCleared(weeks[i - 1]);
            string msg = !w.Playable ? "COMING SOON" : "CLEAR WEEK " + weeks[i - 1 >= 0 ? i - 1 : 0].weekNumber + " TO UNLOCK";
            cards[i].Setup(w, prevCleared && w.Playable, msg, OnCardClicked);
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
        var diff = GameSession.SelectedDifficulty;
        detailWeek.text = "WEEK " + w.weekNumber;
        detailTitle.text = w.title;
        detailDescription.text = w.description;
        if (detailArt != null) { detailArt.sprite = w.detailArt; detailArt.color = selected.Locked ? new Color(0.45f, 0.45f, 0.55f) : Color.white; }

        int best = StoryProgress.GetBestScore(w, diff);
        float rate = StoryProgress.GetClearRate(w, diff);
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

        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            if (difficultyButtons[i] == null) continue;
            ((Image)difficultyButtons[i].targetGraphic).sprite = i == (int)diff ? difficultyOn[i] : difficultyOff[i];
            if (difficultyLabels[i] != null) difficultyLabels[i].alpha = i == (int)diff ? 1f : 0.5f;
        }

        bool canStart = !selected.Locked;
        startButton.interactable = canStart;
        if (startGroup != null) startGroup.alpha = canStart ? 1f : 0.45f;
        if (startHint != null) { startHint.gameObject.SetActive(!canStart); startHint.text = w.Playable ? "CLEAR THE PREVIOUS WEEK FIRST" : "COMING SOON"; }
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
        if (selected == null || selected.Locked) return;
        StorySession.Begin(selected.Week);
        if (GameSession.SelectedSong != null) SongStats.IncrementPlayCount(GameSession.SelectedSong);
        SceneManager.LoadScene(gameplayScene);
    }

    private static void Play(AudioClip clip) { if (clip != null && SFXManager.Instance != null) SFXManager.Instance.PlaySFX(clip); }

    public StoryWeekData[] Weeks { get { return weeks.ToArray(); } }
}
