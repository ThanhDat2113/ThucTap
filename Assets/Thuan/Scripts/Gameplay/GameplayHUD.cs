using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public enum HudJudgement { Perfect = 0, Good = 1, Bad = 2, Miss = 3 }

// UI gameplay (chi hien thi). Gameplay that chi can goi cac ham public ben duoi:
//   SetSongInfo, SetProgress, SetScore, SetCombo, ShowJudgement,
//   SetPlayerHealth, SetOpponentHealth, SetLanePressed, PlayLaneHit, ShowResult
// va nghe cac su kien PauseChanged / RestartRequested / QuitRequested.
public class GameplayHUD : MonoBehaviour
{
    [Header("Score / Combo")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private RectTransform comboRoot;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private int comboShowFrom = 5;

    [Header("Judgement")]
    [SerializeField] private TMP_Text judgementText;
    [SerializeField] private RectTransform[] laneJudgeAnchors = new RectTransform[4];

    [Header("HP doi dau")]
    [SerializeField] private HudHealthBar playerHp;
    [SerializeField] private HudHealthBar opponentHp;
    [SerializeField] private Image playerPortrait;
    [SerializeField] private Image opponentPortrait;
    [SerializeField] private RectTransform crown;

    [Header("Nhan vat (portrait lay tu man Chon nhan vat)")]
    [SerializeField] private List<CharacterData> roster = new List<CharacterData>();
    [SerializeField] private CharacterData opponent;

    [Header("Lanes")]
    [SerializeField] private HudReceptor[] receptors = new HudReceptor[4];

    [Header("Thanh bai hat")]
    [SerializeField] private TMP_Text songTitleText;
    [SerializeField] private TMP_Text songArtistText;
    [SerializeField] private Image progressFill;
    [SerializeField] private RectTransform progressKnob;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text difficultyText;
    [SerializeField] private RectTransform[] eqBars;

    [Header("Pause / Ket qua")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject resultPanel;

    private GameplayResultScreen resultScreen;
    private CharacterData selectedCharacter;

    public event Action<bool> PauseChanged;
    public event Action RestartRequested;
    public event Action QuitRequested;

    public bool IsPaused { get; private set; }
    public HudReceptor[] Receptors { get { return receptors; } }

    private int scoreTarget;
    private float scoreShown;
    private float comboPunch, crownPunch;
    private Coroutine judgeRoutine;
    private bool playing;

    private static readonly string[] JudgeWords = { "PERFECT!!", "GOOD!", "BAD", "MISS" };
    private static readonly Color[] JudgeTop = { new Color(0.85f, 1f, 1f), new Color(0.55f, 0.85f, 1f), new Color(1f, 0.8f, 0.4f), new Color(0.8f, 0.8f, 0.85f) };
    private static readonly Color[] JudgeBottom = { new Color(0.15f, 0.85f, 1f), new Color(0.2f, 0.45f, 1f), new Color(1f, 0.45f, 0.15f), new Color(0.55f, 0.25f, 0.4f) };

    private void Awake()
    {
        if (resultPanel != null) resultScreen = resultPanel.GetComponent<GameplayResultScreen>();
        if (resultScreen != null)
        {
            resultScreen.RetryRequested += Restart;
            resultScreen.ContinueRequested += Continue;
            resultScreen.MenuRequested += Quit;
        }
        for (int i = 0; i < receptors.Length; i++) if (receptors[i] != null) receptors[i].Lane = i;
        if (pauseButton != null) pauseButton.onClick.AddListener(() => SetPaused(true));
        if (resumeButton != null) resumeButton.onClick.AddListener(() => SetPaused(false));
        if (restartButton != null) restartButton.onClick.AddListener(Restart);
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
    }

    private void Start()
    {
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (judgementText != null) judgementText.gameObject.SetActive(false);
        SetScore(0, true);
        SetCombo(0);
        SetPlayerHealth(1f, true);
        SetOpponentHealth(1f, true);
        ApplyPortraits();
    }

    // ---------------- Song ----------------
    public void SetSongInfo(string title, string artist, string difficulty)
    {
        if (songTitleText != null) songTitleText.text = title;
        if (songArtistText != null) songArtistText.text = artist;
        if (difficultyText != null) difficultyText.text = difficulty.ToUpperInvariant();
    }

    public void SetProgress(float current, float total)
    {
        playing = !IsPaused && current > 0f && current < total;
        float k = total > 0f ? Mathf.Clamp01(current / total) : 0f;
        if (progressFill != null) progressFill.fillAmount = k;
        if (progressKnob != null)
        {
            var parent = (RectTransform)progressKnob.parent;
            progressKnob.anchoredPosition = new Vector2(parent.rect.width * k, progressKnob.anchoredPosition.y);
        }
        if (timeText != null) timeText.text = Clock(current) + " / " + Clock(total);
    }

    private static string Clock(float s)
    {
        s = Mathf.Max(0f, s);
        return ((int)s / 60) + ":" + ((int)s % 60).ToString("00");
    }

    // ---------------- Score / Combo / Judgement ----------------
    public void SetScore(int score, bool instant = false)
    {
        scoreTarget = score;
        if (instant) { scoreShown = score; if (scoreText != null) scoreText.text = score.ToString("N0"); }
    }

    public void SetCombo(int combo)
    {
        bool show = combo >= comboShowFrom;
        if (comboRoot != null) comboRoot.gameObject.SetActive(show);
        if (show && comboText != null)
        {
            if (comboText.text != combo.ToString()) comboPunch = 1f;
            comboText.text = combo.ToString();
        }
    }

    public void ShowJudgement(HudJudgement j, int lane = -1)
    {
        if (judgementText == null) return;
        if (judgeRoutine != null) StopCoroutine(judgeRoutine);
        judgeRoutine = StartCoroutine(JudgeRoutine(j, lane));
    }

    private IEnumerator JudgeRoutine(HudJudgement j, int lane)
    {
        var rt = judgementText.rectTransform;
        int i = (int)j;
        judgementText.text = JudgeWords[i];
        judgementText.colorGradient = new VertexGradient(JudgeTop[i], JudgeTop[i], JudgeBottom[i], JudgeBottom[i]);
        judgementText.fontSize = j == HudJudgement.Perfect ? 96 : 76;
        Vector2 basePos = new Vector2(0f, 0f);
        if (lane >= 0 && lane < laneJudgeAnchors.Length && laneJudgeAnchors[lane] != null) basePos = laneJudgeAnchors[lane].anchoredPosition;
        rt.anchoredPosition = basePos;
        rt.localEulerAngles = new Vector3(0, 0, UnityEngine.Random.Range(4f, 10f));
        judgementText.gameObject.SetActive(true);

        float t = 0f;
        while (t < 0.55f)
        {
            t += Time.unscaledDeltaTime;
            float pop = t < 0.08f ? Mathf.Lerp(0.5f, 1.2f, t / 0.08f) : (t < 0.16f ? Mathf.Lerp(1.2f, 1f, (t - 0.08f) / 0.08f) : 1f);
            rt.localScale = new Vector3(pop, pop, 1f);
            rt.anchoredPosition = basePos + new Vector2(0f, 30f * t);
            judgementText.alpha = t > 0.35f ? 1f - (t - 0.35f) / 0.2f : 1f;
            yield return null;
        }
        judgementText.gameObject.SetActive(false);
        judgeRoutine = null;
    }

    // ---------------- HP ----------------
    public void SetPlayerHealth(float normalized, bool instant = false) { if (playerHp != null) playerHp.SetValue(normalized, instant); }
    public void SetOpponentHealth(float normalized, bool instant = false) { if (opponentHp != null) opponentHp.SetValue(normalized, instant); }

    public void SetPortraits(Sprite player, Sprite opp)
    {
        if (playerPortrait != null && player != null) playerPortrait.sprite = player;
        if (opponentPortrait != null && opp != null) opponentPortrait.sprite = opp;
    }

    private void ApplyPortraits()
    {
        CharacterData me = roster.Find(c => c != null && c.Id == CharacterSelection.SelectedId);
        if (me == null && roster.Count > 0) me = roster[0];
        selectedCharacter = me;
        SetPortraits(AvatarOf(me), AvatarOf(opponent));
    }

    private static Sprite AvatarOf(CharacterData c)
    {
        if (c == null) return null;
        return c.avatar != null ? c.avatar : c.cardSprite;
    }

    // ---------------- Lanes ----------------
    public void SetLanePressed(int lane, bool pressed)
    {
        if (lane >= 0 && lane < receptors.Length && receptors[lane] != null) receptors[lane].SetPressed(pressed);
    }

    public void PlayLaneHit(int lane)
    {
        if (lane >= 0 && lane < receptors.Length && receptors[lane] != null) receptors[lane].PlayHit();
        crownPunch = 0.5f;
    }

    // ---------------- Pause / Result ----------------
    public void SetPaused(bool paused)
    {
        if (resultPanel != null && resultPanel.activeSelf) return;
        IsPaused = paused;
        if (pauseMenu != null) pauseMenu.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;
        if (PauseChanged != null) PauseChanged(paused);
    }

    // Goi khi het bai / het mau. Neu qua bai thi luu ky luc vao SongStats -> man chon nhac se hien Best Score, Combo, Accuracy.
    // accuracy: 0-100 (%), truyen -1 neu khong tinh.
    public void ShowResult(bool cleared, int score, int maxCombo, float accuracy = -1f,
        int perfect = 0, int good = 0, int bad = 0, int miss = 0)
    {
        SongData song = GameSession.SelectedSong;
        bool newBest = cleared && song != null && score > 0 && score > SongStats.GetBestScore(song);
        if (cleared && song != null) SongStats.SaveResult(song, score, maxCombo, accuracy);
        StorySession.Report(cleared, score, accuracy);
        if (resultPanel == null) return;
        if (pauseMenu != null) pauseMenu.SetActive(false);
        resultPanel.SetActive(true);
        if (resultScreen != null)
        {
            Sprite performer = selectedCharacter != null
                ? (selectedCharacter.fullArt != null ? selectedCharacter.fullArt : selectedCharacter.posterSprite)
                : null;
            bool hasNext = StorySession.Active && cleared && StorySession.HasNext;
            resultScreen.Show(cleared, song, performer, score, maxCombo, accuracy,
                perfect, good, bad, miss, newBest, hasNext);
        }
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        if (GameSession.SelectedSong != null) SongStats.IncrementPlayCount(GameSession.SelectedSong);
        if (RestartRequested != null) RestartRequested();
    }

    // Story Mode: qua bai -> sang bai tiep theo trong tuan; con lai ve man chon nhac
    private void Continue()
    {
        Time.timeScale = 1f;
        if (StorySession.Active && StorySession.LastCleared && StorySession.HasNext)
        {
            StorySession.Advance();
            if (GameSession.SelectedSong != null) SongStats.IncrementPlayCount(GameSession.SelectedSong);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }
        Quit();
    }

    private void Quit()
    {
        Time.timeScale = 1f;
        StorySession.End();
        if (QuitRequested != null) QuitRequested();
    }

    private void OnDestroy() { Time.timeScale = 1f; }

    // ---------------- Anim ----------------
    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        if (scoreText != null && Mathf.Abs(scoreShown - scoreTarget) > 0.5f)
        {
            scoreShown = Mathf.Lerp(scoreShown, scoreTarget, 1f - Mathf.Exp(-12f * dt));
            if (Mathf.Abs(scoreShown - scoreTarget) < 1f) scoreShown = scoreTarget;
            scoreText.text = Mathf.RoundToInt(scoreShown).ToString("N0");
        }
        if (comboRoot != null && comboRoot.gameObject.activeSelf)
        {
            comboPunch = Mathf.MoveTowards(comboPunch, 0f, dt * 6f);
            float s = 1f + 0.18f * comboPunch * comboPunch;
            comboRoot.localScale = new Vector3(s, s, 1f);
        }
        if (crown != null)
        {
            crownPunch = Mathf.MoveTowards(crownPunch, 0f, dt * 4f);
            float s = 1f + 0.12f * crownPunch + 0.03f * Mathf.Sin(Time.unscaledTime * 3f);
            crown.localScale = new Vector3(s, s, 1f);
        }
        if (eqBars != null)
        {
            for (int i = 0; i < eqBars.Length; i++)
            {
                if (eqBars[i] == null) continue;
                float h = playing ? 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * (5f + i * 1.7f) + i)) : 0.3f;
                eqBars[i].localScale = new Vector3(1f, Mathf.Lerp(eqBars[i].localScale.y, h, 1f - Mathf.Exp(-14f * dt)), 1f);
            }
        }
    }
}
