using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RhythmGame
{
    /// <summary>
    /// Màn hình kết quả trò chơi — thay thế TurnResultPanel.
    /// Logic (ShowResult / StorySession / PauseManager / SaveAchievements) giữ nguyên.
    /// Animation: dùng hệ thống hiệu ứng đầy đủ (Sequence, Effects, Ambient, Confetti, Shines, Bokeh).
    /// </summary>
    public class GameplayResultScreen : MonoBehaviour
    {
        public event Action RetryRequested;
        public event Action ContinueRequested;
        public event Action MenuRequested;

        [Header("Config")]
        [SerializeField] private TMP_FontAsset displayFont;
        [Tooltip("Tên scene song list — nút MENU sẽ load scene này nếu không có ai subscribe MenuRequested")]
        [SerializeField] private string songListSceneName = "Thuan_SongList";

        [Header("Âm thanh (tuỳ chọn — dùng cho animation)")]
        [SerializeField] private AudioClip slamClip;
        [SerializeField] private AudioClip tickClip;
        [SerializeField] private AudioClip rankClip;

        // ----- UI refs (dựng trong Awake) -----
        private RectTransform content;
        private Sprite stageBackground, clearLogoSprite, failedLogoSprite;
        private Image background, character, resultLogo, coverArt;
        private Image songPanel, statsPanel, rankPanel, scoreBar;
        private TMP_Text songText, artistText, difficultyText;
        private TMP_Text perfectText, goodText, badText, missText;
        private TMP_Text comboText, scoreText, rankText, newBestText;
        private GameObject nextButton;
        private RectTransform retryButton, menuButton;

        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

        // ----- Animation state -----
        private RectTransform rankFlash;
        private RectTransform confettiLayer;
        private Image flashImage;
        private readonly List<RectTransform> buttons = new List<RectTransform>();
        private readonly Dictionary<RectTransform, Vector2> basePos = new Dictionary<RectTransform, Vector2>();
        private readonly List<RectTransform> confetti = new List<RectTransform>();
        private bool skip;
        private bool idle;
        private bool lastCleared;
        private float confettiUntil;

        // Effects
        private Image raysBehindCharacter, raysBehindRank, vignette;
        private RectTransform fxLayer;
        private readonly List<Image> shines = new List<Image>();
        private readonly List<Sparkle> sparkles = new List<Sparkle>();
        private float nextTwinkle, nextShine;
        private class Sparkle { public RectTransform rt; public Image img; public Vector2 vel; public float life, age, size, spin; }

        private static Sprite rayTex, starTex, ringTex, glowTex, bandTex, vignetteTex, beamTex;

        // Idle effects
        private RectTransform backLayer;
        private readonly List<Image> spotlights = new List<Image>();
        private readonly List<RectTransform> eqBars = new List<RectTransform>();
        private readonly List<Bokeh> bokeh = new List<Bokeh>();
        private readonly List<Image> orbiters = new List<Image>();
        private Image statsGlow, scoreGlint;
        private float idleStart, idleBlend, nextHype, nextGlint;
        private class Bokeh { public RectTransform rt; public Image img; public float speed, sway, phase, baseA; }

        // ----- State -----
        private CanvasGroup canvasGroup;
        private bool isShown;
        private bool subscribed;

        private static readonly Color Yellow = Hex("FFD21C");
        private static readonly Color Cyan   = Hex("22D9FF");
        private static readonly Color Red    = Hex("FF365D");
        private const float ScaleX = 1920f / 1672f;
        private const float ScaleY = 1080f / 941f;

        // ==================================================================
        //  Lifecycle
        // ==================================================================

        private void Awake()
        {
            if (displayFont == null) displayFont = TMP_Settings.defaultFontAsset;

            var selfRT = GetComponent<RectTransform>();
            if (selfRT == null) selfRT = gameObject.AddComponent<RectTransform>();
            selfRT.anchorMin = Vector2.zero;
            selfRT.anchorMax = Vector2.one;
            selfRT.pivot = new Vector2(0.5f, 0.5f);
            selfRT.offsetMin = Vector2.zero;
            selfRT.offsetMax = Vector2.zero;
            selfRT.localScale = Vector3.one;
            selfRT.localPosition = Vector3.zero;
            selfRT.localRotation = Quaternion.identity;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (GetComponentInParent<Canvas>() == null)
                Debug.LogWarning("[GameplayResultScreen] Không có Canvas cha — UI sẽ hiển thị sai.");

            stageBackground = Resources.Load<Sprite>("Result/result_stage_background");
            clearLogoSprite = Resources.Load<Sprite>("Result/stage_clear");
            failedLogoSprite = Resources.Load<Sprite>("Result/stage_failed");
            foreach (Sprite s in Resources.LoadAll<Sprite>("Result/result_assets"))
                if (!sprites.ContainsKey(s.name)) sprites[s.name] = s;

            foreach (Transform child in transform) child.gameObject.SetActive(false);
            Build();

            StartCoroutine(SubscribeWhenReady());

            ForceHide();
            Debug.Log("[GameplayResultScreen] Awake xong — đã ẩn.");
        }

        private void OnEnable()
        {
            if (!subscribed && GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= OnStateChanged;
                GameManager.Instance.OnStateChanged += OnStateChanged;
                subscribed = true;

                var cur = GameManager.Instance.State;
                if (cur == GameState.Finished || cur == GameState.Failed)
                    OnStateChanged(cur);
            }
        }

        private void OnDestroy()
        {
            if (subscribed && GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= OnStateChanged;
        }

        private IEnumerator SubscribeWhenReady()
        {
            float waited = 0f;
            while (GameManager.Instance == null && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (GameManager.Instance == null)
            {
                Debug.LogError("[GameplayResultScreen] Không tìm thấy GameManager — không subscribe được.");
                yield break;
            }

            GameManager.Instance.OnStateChanged += OnStateChanged;
            subscribed = true;
            Debug.Log("[GameplayResultScreen] Đã subscribe GameManager.OnStateChanged.");

            var cur = GameManager.Instance.State;
            if (cur == GameState.Finished || cur == GameState.Failed)
                OnStateChanged(cur);
            else
                ForceHide();
        }

        // ==================================================================
        //  GameManager events
        // ==================================================================

        private void OnStateChanged(GameState state)
        {
            Debug.Log($"[GameplayResultScreen] State -> {state} (isShown={isShown})");

            if (state == GameState.Finished)
            {
                if (StorySession.Active)
                {
                    var dlg = Luan.Dialogue.StoryDialogueManager.Instance;
                    if (dlg != null && dlg.HasPendingPostBattleDialogue(true))
                    {
                        Debug.Log("[GameplayResultScreen] Đợi post-battle victory dialogue xong mới hiện kết quả.");
                        return;
                    }
                }
                ShowResult(true);
            }
            else if (state == GameState.Failed)
            {
                if (StorySession.Active)
                {
                    var dlg = Luan.Dialogue.StoryDialogueManager.Instance;
                    if (dlg != null && dlg.HasPendingPostBattleDialogue(false))
                    {
                        Debug.Log("[GameplayResultScreen] Đợi post-battle defeat dialogue xong mới hiện kết quả.");
                        return;
                    }
                }
                ShowResult(false);
            }
            else
            {
                ForceHide();
            }
        }

        // ==================================================================
        //  Đọc dữ liệu + hiển thị (logic giữ nguyên File 2)
        // ==================================================================

        public void ShowResult(bool cleared)
        {
            var battleUI = FindObjectOfType<BattleUIController>();
            if (battleUI == null)
                Debug.LogError("[GameplayResultScreen] Không có BattleUIController — kết quả sẽ là 0.");

            int   score      = battleUI != null ? battleUI.playerScore    : 0;
            int   maxCombo   = battleUI != null ? battleUI.playerMaxCombo : 0;
            int   perfect    = battleUI != null ? battleUI.playerPerfect  : 0;
            int   good       = battleUI != null ? battleUI.playerGood     : 0;
            int   bad        = battleUI != null ? battleUI.playerBad      : 0;
            int   miss       = battleUI != null ? battleUI.playerMiss     : 0;
            float accuracy   = battleUI != null ? battleUI.PlayerHitRatio * 100f : 0f;
            Grade grade      = battleUI != null ? battleUI.PlayerGrade    : Grade.F;

            SongData song = GameSession.SelectedSong;
            if (song == null && MusicManager.Instance != null)
                song = MusicManager.Instance.CurrentSong;

            bool newBest = SaveAchievements(song, cleared, score, maxCombo, accuracy);

            Show(cleared, song, score, maxCombo, accuracy, grade,
                 perfect, good, bad, miss, newBest);
        }

        private bool SaveAchievements(SongData song, bool cleared, int score, int maxCombo, float accuracyPercent)
        {
            // Bao cao ket qua ve Story Mode (thang 1 tran -> unlock Chapter tiep theo ngay)
            if (StorySession.Active && StorySession.Week != null)
            {
                StorySession.Report(cleared, score, accuracyPercent);
            }
            else if (cleared && Luan.Dialogue.StoryDialogueManager.Instance != null)
            {
                int chapterNum = Luan.Dialogue.StoryDialogueManager.Instance.GetCurrentChapterNumber();
                if (chapterNum > 0)
                    StoryProgress.SaveChapterByNumber(chapterNum, score, accuracyPercent);
            }

            if (song == null)
            {
                Debug.LogWarning("[GameplayResultScreen] Không có SongData — bỏ qua lưu thành tích.");
                return false;
            }

            string keyScore = "best_score_" + song.name;
            int prevBest = PlayerPrefs.GetInt(keyScore, 0);
            bool newBest = cleared && score > prevBest;

            if (!cleared) return false;

            try { SongStats.SaveResult(song, score, maxCombo, accuracyPercent / 100f); }
            catch (Exception e) { Debug.LogWarning("[GameplayResultScreen] SongStats.SaveResult lỗi: " + e.Message); }

            if (newBest) PlayerPrefs.SetInt(keyScore, score);
            PlayerPrefs.Save();
            return newBest;
        }

        public void Show(bool cleared, SongData song, int score, int maxCombo, float accuracy,
                         Grade grade, int perfect, int good, int bad, int miss, bool newBest)
        {
            Debug.Log("[GameplayResultScreen] Show() — hiển thị kết quả.");

            if (PauseManager.Instance != null) PauseManager.Instance.HideForResult();

            if (StorySession.Active) StorySession.Report(cleared, score, accuracy);

            isShown = true;
            Time.timeScale = 0f;

            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            // Logo + song info
            resultLogo.sprite = cleared ? clearLogoSprite : failedLogoSprite;
            songText.text     = song != null ? song.songName : "RHYTHM BATTLE";
            artistText.text   = song != null && !string.IsNullOrWhiteSpace(song.artist)
                                ? song.artist : "UNKNOWN ARTIST";
            difficultyText.text = song != null ? song.difficulty + "/5" : "?/5";
            coverArt.sprite   = song != null ? song.coverArt : null;
            coverArt.enabled  = coverArt.sprite != null;

            // Stats
            perfectText.text = perfect.ToString("N0");
            goodText.text    = good.ToString("N0");
            badText.text     = bad.ToString("N0");
            missText.text    = miss.ToString("N0");
            comboText.text   = maxCombo.ToString("N0");
            scoreText.text   = score.ToString("N0");

            // Grade
            rankText.text  = grade.ToString();
            rankText.color = GradeColor(grade);

            if (newBestText != null) newBestText.gameObject.SetActive(newBest);

            // Button layout (Next luôn ẩn — giữ nguyên File 2)
            const bool hasNext = false;
            if (nextButton != null) nextButton.SetActive(hasNext);
            if (retryButton != null)
                retryButton.anchoredPosition = new Vector2(
                    (hasNext ? 670f : 815f) * ScaleX - 960f, retryButton.anchoredPosition.y);
            if (menuButton != null)
                menuButton.anchoredPosition = new Vector2(
                    (hasNext ? 1325f : 1165f) * ScaleX - 960f, menuButton.anchoredPosition.y);

            // ★ Bắt đầu animation đầy đủ
            lastCleared = cleared;
            StopAllCoroutines();
            StartCoroutine(Sequence(cleared, newBest, new[] { perfect, good, bad, miss, maxCombo }, score));
        }

        private void ForceHide()
        {
            isShown = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        // ==================================================================
        //  Button handlers (giữ nguyên File 2)
        // ==================================================================

        private void OnRetryClicked()
        {
            Time.timeScale = 1f;
            if (PauseManager.Instance != null) PauseManager.Instance.ShowAfterResult();
            if (RetryRequested != null) { RetryRequested.Invoke(); return; }
            if (GameManager.Instance != null) GameManager.Instance.Restart();
        }

        private void OnContinueClicked()
        {
            Time.timeScale = 1f;
            if (PauseManager.Instance != null) PauseManager.Instance.ShowAfterResult();
            if (ContinueRequested != null) ContinueRequested.Invoke();
            ForceHide();
        }

        private void OnMenuClicked()
        {
            Time.timeScale = 1f;
            if (PauseManager.Instance != null) PauseManager.Instance.ShowAfterResult();
            StorySession.End();
            if (MenuRequested != null) { MenuRequested.Invoke(); return; }
            if (!string.IsNullOrEmpty(songListSceneName))
                SceneManager.LoadScene(songListSceneName);
            else
                Debug.LogWarning("[GameplayResultScreen] Chưa đặt songListSceneName cho nút MENU.");
        }

        // ==================================================================
        //  Build UI (giữ nguyên logic File 2, thêm BuildEffects + basePos)
        // ==================================================================

        private void Build()
        {
            background = CreateImage("StageBackground", transform, stageBackground);
            Stretch(background.rectTransform);
            background.color = stageBackground != null
                ? new Color(.67f, .67f, .72f, 1f)
                : new Color(0.018f, 0.02f, 0.045f, 1f);
            background.preserveAspect = false;

            content = CreateNode("ResultContent", transform);
            Stretch(content);

            resultLogo = CreateImage("ResultLogo", content, clearLogoSprite);
            SetRect(resultLogo.rectTransform,
                new Vector2(720f * ScaleX, 231f * ScaleY), new Vector2(-546.6f, 407.6f));
            resultLogo.raycastTarget = false;
            resultLogo.rectTransform.sizeDelta *= 0.96f;
            resultLogo.rectTransform.anchoredPosition += new Vector2(50f, -6f);
            resultLogo.preserveAspect = false;

            character = CreateImage("Character", content, Resources.Load<Sprite>("Result/result_character"));
            SetRect(character.rectTransform,
                new Vector2(720f * ScaleX, 660f * ScaleY), new Vector2(-546.6f, 5.3f));
            character.raycastTarget = false;
            character.rectTransform.sizeDelta *= 1.28f;
            character.rectTransform.anchoredPosition += new Vector2(20f, -185f);

            songPanel  = PlaceSprite("SongPanel",  "song_panel",  new Rect(720, 710, 850, 231));
            statsPanel = PlaceSprite("StatsPanel", "stats_panel", new Rect(705, 315, 570, 400));
            rankPanel  = PlaceSprite("RankPanel",  "rank_panel",  new Rect(1270, 285, 402, 440));
            scoreBar   = PlaceSprite("ScoreBar",   "score_bar",   new Rect(700, 175, 630, 145));

            RectTransform songContent = CreateNode("SongContent", songPanel.transform);
            SetRect(songContent, new Vector2(880f, 170f), new Vector2(4f, -4f));
            songContent.localEulerAngles = new Vector3(0f, 0f, 2.5f);

            Image coverFrame = CreateImage("SongCoverFrame", songContent, null);
            SetRect(coverFrame.rectTransform, new Vector2(146f, 146f), new Vector2(-358f, 0f));
            coverFrame.color = new Color(1f, .16f, .5f, 1f);
            coverFrame.raycastTarget = false;
            coverArt = CreateImage("SongCover", songContent, null);
            SetRect(coverArt.rectTransform, new Vector2(138f, 138f), new Vector2(-358f, 0f));
            coverArt.preserveAspect = false;
            coverArt.raycastTarget = false;

            songText = CreateText("SongName", songContent, "SONG TITLE", 40, Color.white, TextAlignmentOptions.Left);
            SetRect(songText.rectTransform, new Vector2(560f, 54f), new Vector2(5f, 38f));
            songText.fontStyle = FontStyles.Bold;
            songText.enableAutoSizing = true;
            songText.fontSizeMin = 22f;
            songText.fontSizeMax = 40f;

            artistText = CreateText("Artist", songContent, "ARTIST", 26,
                new Color(.7f, .7f, .76f), TextAlignmentOptions.Left);
            SetRect(artistText.rectTransform, new Vector2(560f, 36f), new Vector2(5f, -6f));
            artistText.enableAutoSizing = true;
            artistText.fontSizeMin = 16f;
            artistText.fontSizeMax = 26f;

            Image difficultyBadge = CreateImage("DifficultyBadge", songContent, null);
            SetRect(difficultyBadge.rectTransform, new Vector2(130f, 38f), new Vector2(-210f, -50f));
            difficultyBadge.color = new Color(1f, .08f, .4f, 1f);
            difficultyBadge.raycastTarget = false;
            difficultyText = CreateText("Difficulty", songContent, "HARD", 24, Color.black,
                TextAlignmentOptions.Center);
            SetRect(difficultyText.rectTransform, new Vector2(126f, 36f), new Vector2(-210f, -50f));
            difficultyText.fontStyle = FontStyles.Bold;

            // 5 rows (File 2 không có SHIT)
            perfectText = ValueText("PerfectValue", statsPanel.transform, 155f);
            goodText    = ValueText("GoodValue",    statsPanel.transform, 91f);
            badText     = ValueText("BadValue",     statsPanel.transform, 31f);
            missText    = ValueText("MissValue",    statsPanel.transform, -93f);
            comboText   = ValueText("ComboValue",   statsPanel.transform, -175f);

            rankText = CreateText("RankValue", rankPanel.transform, "S", 205, Yellow,
                TextAlignmentOptions.Center);
            SetRect(rankText.rectTransform, new Vector2(300f, 265f), new Vector2(6f, -5f));
            rankText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            rankText.outlineWidth = 0.22f;
            rankText.outlineColor = new Color32(25, 0, 20, 255);

            newBestText = CreateText("NewRecord", rankPanel.transform, "NEW RECORD!", 27,
                Color.white, TextAlignmentOptions.Center);
            SetRect(newBestText.rectTransform, new Vector2(260f, 50f), new Vector2(10f, -153f));
            newBestText.fontStyle = FontStyles.Bold | FontStyles.Italic;

            scoreText = CreateText("ScoreValue", scoreBar.transform, "0", 62, Yellow,
                TextAlignmentOptions.Right);
            SetRect(scoreText.rectTransform, new Vector2(420f, 92f), new Vector2(84f, 0f));
            scoreText.fontStyle = FontStyles.Bold;

            retryButton = (RectTransform)CreateButton("RetryButton", "retry_button",
                new Rect(500, 60, 340, 120), OnRetryClicked).transform;
            nextButton  = CreateButton("NextButton", "next_button",
                new Rect(810, 60, 355, 120), OnContinueClicked);
            menuButton  = (RectTransform)CreateButton("MenuButton", "menu_button",
                new Rect(1120, 60, 410, 120), OnMenuClicked).transform;

            buttons.Add(retryButton);
            if (nextButton != null) buttons.Add((RectTransform)nextButton.transform);
            buttons.Add(menuButton);

            // Rank flash
            Image glow = CreateImage("RankFlash", rankPanel.transform, null);
            SetRect(glow.rectTransform, new Vector2(330f, 330f), new Vector2(6f, -5f));
            glow.color = new Color(1f, 1f, 1f, 0f);
            glow.raycastTarget = false;
            glow.transform.SetSiblingIndex(0);
            rankFlash = glow.rectTransform;

            // Confetti layer + flash
            confettiLayer = CreateNode("Confetti", transform);
            Stretch(confettiLayer);
            flashImage = CreateImage("Flash", transform, null);
            Stretch(flashImage.rectTransform);
            flashImage.color = new Color(1f, 1f, 1f, 0f);
            flashImage.raycastTarget = false;

            BuildEffects();

            // Lưu vị trí gốc cho animation
            foreach (var rt in new[] { character.rectTransform, resultLogo.rectTransform,
                songPanel.rectTransform, statsPanel.rectTransform, rankPanel.rectTransform,
                scoreBar.rectTransform, retryButton, menuButton })
                basePos[rt] = rt.anchoredPosition;
            if (nextButton != null)
                basePos[(RectTransform)nextButton.transform] = ((RectTransform)nextButton.transform).anchoredPosition;
        }

        private TMP_Text ValueText(string name, Transform parent, float y)
        {
            TMP_Text value = CreateText(name, parent, "0", 34, Color.white, TextAlignmentOptions.Right);
            SetRect(value.rectTransform, new Vector2(150f, 52f), new Vector2(166f, y));
            value.fontStyle = FontStyles.Bold;
            return value;
        }

        private Image PlaceSprite(string objectName, string spriteName, Rect sourceRect)
        {
            sprites.TryGetValue(spriteName, out Sprite sprite);
            Image image = CreateImage(objectName, content, sprite);
            float width  = sourceRect.width  * ScaleX;
            float height = sourceRect.height * ScaleY;
            float x = (sourceRect.x + sourceRect.width  * .5f) * ScaleX - 960f;
            float y = (sourceRect.y + sourceRect.height * .5f) * ScaleY - 540f;
            SetRect(image.rectTransform, new Vector2(width, height), new Vector2(x, y));
            image.raycastTarget = false;
            return image;
        }

        private GameObject CreateButton(string objectName, string spriteName, Rect sourceRect, Action action)
        {
            Image image = PlaceSprite(objectName, spriteName, sourceRect);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, .82f);
            colors.pressedColor     = new Color(.72f, .72f, .72f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => action?.Invoke());
            return image.gameObject;
        }

        private TMP_Text CreateText(string name, Transform parent, string value, float size,
            Color color, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.text = value;
            text.font = displayFont;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            return image;
        }

        private static RectTransform CreateNode(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        // ==================================================================
        //  ANIMATION (Sequence + Update + toàn bộ coroutine từ File 1)
        // ==================================================================

        private IEnumerator Sequence(bool cleared, bool newBest, int[] stats, int score)
        {
            skip = false;
            idle = false;
            var values = new[] { perfectText, goodText, badText, missText, comboText };
            foreach (var v in values) v.text = "0";
            scoreText.text = "0";
            content.localScale = Vector3.one;

            foreach (var kv in basePos)
                if (!buttons.Contains(kv.Key)) kv.Key.anchoredPosition = kv.Value;

            SetA(background, 0f);
            Fade(character, 0f); Fade(resultLogo, 0f); Fade(songPanel, 0f);
            Fade(statsPanel, 0f); Fade(rankPanel, 0f); Fade(scoreBar, 0f);
            rankText.alpha = 0f;
            newBestText.alpha = 0f;
            foreach (var b in buttons) { b.localScale = Vector3.zero; SetButtonsInteractable(false); }
            ClearConfetti();
            ResetEffects();

            // 1. Background + speed lines
            StartCoroutine(SpeedLines(cleared));
            yield return Tween(0.25f, k => SetA(background, k));

            // 2. Character slide-in + logo slam
            StartCoroutine(SlideIn(character.rectTransform, new Vector2(-520f, 0f), 0.45f));
            StartCoroutine(AfterImages(character, new Vector2(-520f, 0f), 0.45f));
            StartCoroutine(FadeImage(raysBehindCharacter, cleared ? 0.35f : 0.12f, 0.6f));
            yield return Wait(0.12f);
            Play(slamClip);
            yield return Tween(0.28f, k =>
            {
                var rt = resultLogo.rectTransform;
                float e = EaseOut(k);
                rt.localScale = Vector3.one * Mathf.Lerp(2.4f, 1f, e);
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(-14f, 0f, e));
                Fade(resultLogo, Mathf.Clamp01(k * 3f));
            });
            StartCoroutine(Shake(content, 0.25f, cleared ? 10f : 18f));
            StartCoroutine(Flash(cleared ? new Color(1f, 1f, 1f, 0.55f) : new Color(1f, 0.1f, 0.25f, 0.45f), 0.3f));
            StartCoroutine(Shockwave(FxPos(resultLogo.rectTransform), cleared ? Hex("FF3FA4") : Red, 1.6f));
            Burst(FxPos(resultLogo.rectTransform), 18, cleared ? Yellow : Red, 420f);
            if (!cleared) StartCoroutine(FadeImage(vignette, 0.35f, 0.5f));

            // 3. Panels fly in
            StartCoroutine(SlideIn(songPanel.rectTransform, new Vector2(0f, 260f), 0.4f));
            yield return Wait(0.08f);
            StartCoroutine(SlideIn(statsPanel.rectTransform, new Vector2(0f, -320f), 0.4f));
            yield return Wait(0.08f);
            StartCoroutine(SlideIn(scoreBar.rectTransform, new Vector2(-380f, 0f), 0.4f));
            yield return Wait(0.08f);
            StartCoroutine(SlideIn(rankPanel.rectTransform, new Vector2(420f, 0f), 0.4f));
            yield return Wait(0.35f);

            // 4. Đếm số từng dòng
            for (int i = 0; i < values.Length; i++)
            {
                StartCoroutine(Punch(values[i].rectTransform, 1.35f, 0.2f));
                StartCoroutine(RowFlash(values[i].rectTransform.anchoredPosition.y));
                yield return CountUp(values[i], stats[i], 0.28f);
            }

            // 5. Đếm điểm
            StartCoroutine(ScoreSparks(0.8f));
            yield return CountUp(scoreText, score, 0.8f);
            StartCoroutine(Punch(scoreText.rectTransform, 1.25f, 0.25f));
            StartCoroutine(Shockwave(FxPos(scoreText.rectTransform), Yellow, 0.9f));
            if (shines.Count > 1) StartCoroutine(Shine(shines[1]));

            // 6. Đóng dấu RANK
            yield return Wait(skip ? 0f : 0.15f);
            Play(rankClip);
            var rk = rankText.rectTransform;
            yield return Tween(0.3f, k =>
            {
                float e = EaseOut(k);
                rk.localScale = Vector3.one * Mathf.Lerp(3f, 1f, e);
                rk.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(25f, 0f, e));
                rankText.alpha = Mathf.Clamp01(k * 2.5f);
            }, true);
            StartCoroutine(Shake(rankPanel.rectTransform, 0.22f, 12f));
            StartCoroutine(RankBurst());
            StartCoroutine(Shockwave(FxPos(rankText.rectTransform), rankText.color, 1.3f));
            Burst(FxPos(rankText.rectTransform), 28, rankText.color, 520f);
            raysBehindRank.color = new Color(rankText.color.r, rankText.color.g, rankText.color.b, 0f);
            StartCoroutine(FadeImage(raysBehindRank, cleared ? 0.55f : 0.18f, 0.4f));
            if (cleared) SpawnConfetti(70);
            if (newBest)
            {
                yield return Tween(0.25f, k =>
                {
                    newBestText.alpha = k;
                    newBestText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, EaseOut(k));
                }, true);
            }

            // 7. Buttons pop
            for (int i = 0; i < buttons.Count; i++)
            {
                if (!buttons[i].gameObject.activeSelf) continue;
                StartCoroutine(PopButton(buttons[i]));
                yield return Wait(0.07f, true);
            }
            yield return Wait(0.25f, true);
            SetButtonsInteractable(true);

            // 8. Idle
            nextShine = Time.unscaledTime + 0.6f;
            idleStart = Time.unscaledTime;
            nextHype = idleStart + 1.5f;
            nextGlint = idleStart + 0.8f;
            foreach (var b in bokeh) ResetBokeh(b, true);
            idle = true;
        }

        private void Update()
        {
            if (!skip && AnyPressed()) skip = true;
            float t = Time.unscaledTime;
            if (idle)
            {
                var c = character.rectTransform;
                c.anchoredPosition = basePos[c] + new Vector2(0f, Mathf.Sin(t * 1.8f) * 8f);
                c.localScale = new Vector3(1f, 1f + Mathf.Sin(t * 1.8f + 1f) * 0.012f, 1f);
                float beat = Mathf.Pow(1f - Mathf.Repeat(t * 1.9f, 1f), 4f);
                resultLogo.rectTransform.localScale = Vector3.one * (1f + 0.035f * beat);
                resultLogo.rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 1.3f) * 1.5f);
                rankText.rectTransform.localScale = Vector3.one * (1f + 0.05f * beat);
                if (newBestText.gameObject.activeSelf) newBestText.alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 4f));
                for (int i = 0; i < buttons.Count; i++)
                    buttons[i].localScale = Vector3.one * (1f + Mathf.Sin(t * 2.4f + i * 0.8f) * 0.015f);
                IdleEffects(t, beat);
            }
            if (raysBehindCharacter != null)
            {
                raysBehindCharacter.rectTransform.localEulerAngles = new Vector3(0f, 0f, t * 8f);
                raysBehindRank.rectTransform.localEulerAngles = new Vector3(0f, 0f, -t * 14f);
            }
            UpdateSparkles();
            UpdateConfetti();
        }

        private IEnumerator CountUp(TMP_Text text, int target, float time)
        {
            if (target <= 0 || skip) { text.text = target.ToString("N0"); yield break; }
            float e = 0f;
            int last = -1;
            while (e < time && !skip)
            {
                e += Step();
                int v = Mathf.RoundToInt(target * EaseOut(e / time));
                if (v != last) { text.text = v.ToString("N0"); last = v; if (tickClip != null && Time.frameCount % 3 == 0) Play(tickClip); }
                yield return null;
            }
            text.text = target.ToString("N0");
        }

        private IEnumerator SlideIn(RectTransform rt, Vector2 offset, float time)
        {
            var end = basePos[rt];
            float e = 0f;
            while (e < time)
            {
                e += Step();
                float k = Mathf.Clamp01(e / time);
                rt.anchoredPosition = Vector2.LerpUnclamped(end + offset, end, EaseOutBack(k));
                Fade(rt, Mathf.Clamp01(k * 2.5f));
                yield return null;
            }
            rt.anchoredPosition = end;
            Fade(rt, 1f);
        }

        private IEnumerator PopButton(RectTransform rt)
        {
            float e = 0f;
            while (e < 0.3f)
            {
                e += Step(true);
                rt.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(e / 0.3f));
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        private IEnumerator Punch(RectTransform rt, float peak, float time)
        {
            float e = 0f;
            while (e < time)
            {
                e += Step(true);
                float k = Mathf.Clamp01(e / time);
                rt.localScale = Vector3.one * Mathf.Lerp(peak, 1f, EaseOut(k));
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        private IEnumerator Shake(RectTransform rt, float time, float power)
        {
            var start = rt.anchoredPosition;
            float e = 0f;
            while (e < time)
            {
                e += Step(true);
                rt.anchoredPosition = start + UnityEngine.Random.insideUnitCircle * power * (1f - e / time);
                yield return null;
            }
            rt.anchoredPosition = start;
        }

        private IEnumerator Flash(Color c, float time)
        {
            float e = 0f;
            while (e < time)
            {
                e += Step(true);
                flashImage.color = new Color(c.r, c.g, c.b, c.a * (1f - e / time));
                yield return null;
            }
            flashImage.color = new Color(c.r, c.g, c.b, 0f);
        }

        private IEnumerator RankBurst()
        {
            var img = rankFlash.GetComponent<Image>();
            Color col = rankText.color;
            float e = 0f;
            while (e < 0.45f)
            {
                e += Step(true);
                float k = e / 0.45f;
                rankFlash.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.6f, EaseOut(k));
                rankFlash.localEulerAngles = new Vector3(0f, 0f, 45f * k);
                img.color = new Color(col.r, col.g, col.b, 0.7f * (1f - k));
                yield return null;
            }
            img.color = new Color(col.r, col.g, col.b, 0f);
        }

        private IEnumerator Tween(float time, Action<float> step, bool ignoreSkip = false)
        {
            float e = 0f;
            while (e < time)
            {
                if (skip && !ignoreSkip) break;
                e += Step(ignoreSkip);
                step(Mathf.Clamp01(e / time));
                yield return null;
            }
            step(1f);
        }

        private IEnumerator Wait(float time, bool ignoreSkip = false)
        {
            float e = 0f;
            while (e < time && (ignoreSkip || !skip)) { e += Step(ignoreSkip); yield return null; }
        }

        private float Step(bool ignoreSkip = false)
        {
            float d = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            return skip && !ignoreSkip ? d * 6f : d;
        }

        // ================== Effects ==================

        private void BuildEffects()
        {
            MakeTextures();
            raysBehindCharacter = CreateImage("RaysCharacter", content, rayTex);
            SetRect(raysBehindCharacter.rectTransform, new Vector2(1500f, 1500f), character.rectTransform.anchoredPosition + new Vector2(0f, 120f));
            raysBehindCharacter.raycastTarget = false;
            raysBehindCharacter.color = new Color(1f, 0.3f, 0.75f, 0f);
            raysBehindCharacter.transform.SetSiblingIndex(0);

            raysBehindRank = CreateImage("RaysRank", rankPanel.transform, rayTex);
            SetRect(raysBehindRank.rectTransform, new Vector2(620f, 620f), rankText.rectTransform.anchoredPosition);
            raysBehindRank.raycastTarget = false;
            raysBehindRank.color = new Color(1f, 0.85f, 0.2f, 0f);
            raysBehindRank.transform.SetSiblingIndex(1);

            vignette = CreateImage("Vignette", transform, vignetteTex);
            Stretch(vignette.rectTransform);
            vignette.color = new Color(0.6f, 0f, 0.08f, 0f);
            vignette.raycastTarget = false;
            vignette.transform.SetSiblingIndex(content.GetSiblingIndex() + 1);

            fxLayer = CreateNode("FX", transform);
            Stretch(fxLayer);

            BuildAmbient();
            shines.Add(AddShine(resultLogo));
            shines.Add(AddShine(scoreBar));
            foreach (var b in buttons) shines.Add(AddShine(b.GetComponent<Image>()));
        }

        private Image AddShine(Image target)
        {
            var mask = target.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            Image band = CreateImage("Shine", target.transform, bandTex);
            var rt = band.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            float h = target.rectTransform.rect.height;
            rt.sizeDelta = new Vector2(Mathf.Max(70f, h * 0.45f), h * 2.2f);
            rt.localEulerAngles = new Vector3(0f, 0f, -22f);
            band.color = new Color(1f, 1f, 1f, 0f);
            band.raycastTarget = false;
            return band;
        }

        private void ResetEffects()
        {
            foreach (var sp in sparkles) if (sp.rt != null) Destroy(sp.rt.gameObject);
            sparkles.Clear();
            SetA(raysBehindCharacter, 0f);
            SetA(raysBehindRank, 0f);
            SetA(vignette, 0f);
            foreach (var sh in shines) SetA(sh, 0f);
            background.rectTransform.localScale = Vector3.one;
            resultLogo.color = Color.white;
            idleBlend = 0f;
            ApplyAmbientAlpha(0f);
        }

        private void BuildAmbient()
        {
            backLayer = CreateNode("Ambient", transform);
            Stretch(backLayer);
            backLayer.SetSiblingIndex(content.GetSiblingIndex());

            float[] xs = { -620f, 620f };
            Color[] cs = { new Color(1f, 0.3f, 0.8f), new Color(0.3f, 0.85f, 1f) };
            for (int i = 0; i < 2; i++)
            {
                Image beam = CreateImage("Spotlight", backLayer, beamTex);
                beam.raycastTarget = false;
                var rt = beam.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(520f, 1500f);
                rt.anchoredPosition = new Vector2(xs[i], 600f);
                beam.color = new Color(cs[i].r, cs[i].g, cs[i].b, 0f);
                spotlights.Add(beam);
            }

            for (int i = 0; i < 34; i++)
            {
                Image img = CreateImage("Bokeh", backLayer, glowTex);
                img.raycastTarget = false;
                var b = new Bokeh { rt = img.rectTransform, img = img };
                b.rt.anchorMin = b.rt.anchorMax = new Vector2(0.5f, 0.5f);
                ResetBokeh(b, true);
                bokeh.Add(b);
            }

            int n = 46;
            for (int i = 0; i < n; i++)
            {
                Image bar = CreateImage("EqBar", backLayer, null);
                bar.raycastTarget = false;
                var rt = bar.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                float w = 1920f / n;
                rt.anchoredPosition = new Vector2(-960f + w * (i + 0.5f), 0f);
                rt.sizeDelta = new Vector2(w * 0.6f, 10f);
                bar.color = Color.Lerp(new Color(1f, 0.25f, 0.75f, 0f), new Color(0.25f, 0.85f, 1f, 0f), i / (float)(n - 1));
                eqBars.Add(rt);
            }

            statsGlow = CreateImage("StatsGlow", content, glowTex);
            SetRect(statsGlow.rectTransform, statsPanel.rectTransform.sizeDelta * 1.7f, statsPanel.rectTransform.anchoredPosition);
            statsGlow.raycastTarget = false;
            statsGlow.color = new Color(1f, 0.3f, 0.8f, 0f);
            statsGlow.transform.SetSiblingIndex(statsPanel.transform.GetSiblingIndex());

            for (int i = 0; i < 4; i++)
            {
                Image o = CreateImage("Orbiter", rankPanel.transform, starTex);
                o.raycastTarget = false;
                SetRect(o.rectTransform, new Vector2(64f, 64f), rankText.rectTransform.anchoredPosition);
                o.color = new Color(1f, 1f, 1f, 0f);
                orbiters.Add(o);
            }

            scoreGlint = CreateImage("ScoreGlint", scoreBar.transform, starTex);
            scoreGlint.raycastTarget = false;
            SetRect(scoreGlint.rectTransform, new Vector2(80f, 80f), scoreText.rectTransform.anchoredPosition);
            scoreGlint.color = new Color(1f, 1f, 1f, 0f);
        }

        private void ResetBokeh(Bokeh b, bool anywhere)
        {
            float size = UnityEngine.Random.Range(14f, 70f);
            b.rt.sizeDelta = new Vector2(size, size);
            b.speed = UnityEngine.Random.Range(30f, 90f) * (lastCleared ? 1f : 1.6f);
            b.sway = UnityEngine.Random.Range(15f, 50f);
            b.phase = UnityEngine.Random.Range(0f, 10f);
            b.baseA = UnityEngine.Random.Range(0.3f, 0.7f) * (size < 30f ? 1.3f : 0.9f);
            b.rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-960f, 960f), anywhere ? UnityEngine.Random.Range(-560f, 560f) : -580f);
            Color[] clear = { new Color(1f, 0.35f, 0.8f), new Color(0.35f, 0.9f, 1f), new Color(0.75f, 0.5f, 1f), new Color(1f, 0.85f, 0.3f) };
            Color[] fail = { new Color(1f, 0.25f, 0.2f), new Color(1f, 0.5f, 0.15f), new Color(0.8f, 0.1f, 0.3f) };
            var pool = lastCleared ? clear : fail;
            var c = pool[UnityEngine.Random.Range(0, pool.Length)];
            b.img.color = new Color(c.r, c.g, c.b, 0f);
        }

        private void ApplyAmbientAlpha(float k)
        {
            foreach (var sp in spotlights) SetA(sp, 0f);
            foreach (var b in bokeh) SetA(b.img, 0f);
            foreach (var bar in eqBars) SetA(bar.GetComponent<Image>(), 0f);
            foreach (var o in orbiters) SetA(o, 0f);
            if (statsGlow != null) SetA(statsGlow, 0f);
            if (scoreGlint != null) SetA(scoreGlint, 0f);
        }

        private void AmbientEffects(float t, float beat)
        {
            float d = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            idleBlend = Mathf.Clamp01((t - idleStart) / 0.8f);
            float k = idleBlend;

            for (int i = 0; i < spotlights.Count; i++)
            {
                float dir = i == 0 ? 1f : -1f;
                spotlights[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, dir * (18f + 16f * Mathf.Sin(t * 0.7f + i * 1.7f)));
                SetA(spotlights[i], k * (lastCleared ? 0.22f : 0.12f) * (0.75f + 0.25f * beat));
            }

            foreach (var b in bokeh)
            {
                var p = b.rt.anchoredPosition;
                p.y += b.speed * d;
                p.x += Mathf.Sin(t * 0.8f + b.phase) * b.sway * d;
                b.rt.anchoredPosition = p;
                if (p.y > 580f) ResetBokeh(b, false);
                float tw = 0.65f + 0.35f * Mathf.Sin(t * 2f + b.phase * 3f);
                SetA(b.img, k * b.baseA * tw);
            }

            for (int i = 0; i < eqBars.Count; i++)
            {
                float n = Mathf.PerlinNoise(i * 0.33f, t * 1.7f);
                float target = Mathf.Lerp(10f, 150f, n * (0.6f + 0.4f * beat)) * (lastCleared ? 1f : 0.6f);
                var sz = eqBars[i].sizeDelta;
                sz.y = Mathf.Lerp(sz.y, target, 1f - Mathf.Exp(-12f * d));
                eqBars[i].sizeDelta = sz;
                SetA(eqBars[i].GetComponent<Image>(), k * 0.32f);
            }

            statsGlow.color = Color.Lerp(new Color(1f, 0.3f, 0.8f), new Color(0.3f, 0.85f, 1f), 0.5f + 0.5f * Mathf.Sin(t * 1.2f));
            SetA(statsGlow, k * (0.5f + 0.25f * beat));

            var center = rankText.rectTransform.anchoredPosition;
            for (int i = 0; i < orbiters.Count; i++)
            {
                float a = t * 1.6f + i * Mathf.PI * 0.5f;
                var o = orbiters[i].rectTransform;
                o.anchoredPosition = center + new Vector2(Mathf.Cos(a) * 150f, Mathf.Sin(a) * 105f);
                o.localScale = Vector3.one * (0.6f + 0.4f * Mathf.Abs(Mathf.Sin(t * 3f + i)));
                o.localEulerAngles = new Vector3(0f, 0f, t * 120f);
                float front = Mathf.Sin(a) < 0f ? 1f : 0.45f;
                orbiters[i].color = lastCleared ? Color.Lerp(Yellow, Color.white, 0.4f) : new Color(1f, 0.45f, 0.5f);
                SetA(orbiters[i], k * front);
            }

            if (t >= nextGlint) { nextGlint = t + 1.9f; StartCoroutine(Glint()); }

            content.localScale = Vector3.one * (1f + 0.006f * beat * k);
            if (t >= nextHype)
            {
                nextHype = t + 4f / 1.9f;
                StartCoroutine(Hype());
            }
        }

        private IEnumerator Glint()
        {
            var rt = scoreGlint.rectTransform;
            rt.anchoredPosition = scoreText.rectTransform.anchoredPosition + new Vector2(UnityEngine.Random.Range(-90f, 190f), UnityEngine.Random.Range(-12f, 18f));
            float e = 0f;
            while (e < 0.45f)
            {
                e += Step(true);
                float q = Mathf.Clamp01(e / 0.45f);
                rt.localScale = Vector3.one * Mathf.Sin(q * Mathf.PI) * 1.2f;
                rt.localEulerAngles = new Vector3(0f, 0f, q * 90f);
                SetA(scoreGlint, Mathf.Sin(q * Mathf.PI));
                yield return null;
            }
            SetA(scoreGlint, 0f);
        }

        private IEnumerator Hype()
        {
            var c = character.rectTransform;
            float e = 0f;
            while (e < 0.35f && idle)
            {
                e += Step(true);
                float q = Mathf.Clamp01(e / 0.35f);
                float jump = Mathf.Sin(q * Mathf.PI);
                c.anchoredPosition = basePos[c] + new Vector2(0f, Mathf.Sin(Time.unscaledTime * 1.8f) * 8f + jump * 22f);
                c.localScale = new Vector3(1f - 0.03f * jump, 1f + 0.04f * jump, 1f);
                yield return null;
            }
        }

        private void IdleEffects(float t, float beat)
        {
            AmbientEffects(t, beat);
            background.rectTransform.localScale = Vector3.one * (1.03f + 0.03f * Mathf.Sin(t * 0.25f));

            if (rankText.text == "S") rankText.color = Color.Lerp(Yellow, Color.HSVToRGB(Mathf.Repeat(t * 0.3f, 1f), 0.45f, 1f), 0.3f);

            if (t >= nextTwinkle)
            {
                nextTwinkle = t + (lastCleared ? 0.09f : 0.25f);
                var around = UnityEngine.Random.value < 0.6f ? rankText.rectTransform : resultLogo.rectTransform;
                var p = FxPos(around) + new Vector2(UnityEngine.Random.Range(-170f, 170f), UnityEngine.Random.Range(-130f, 130f));
                Spawn(p, Vector2.zero, lastCleared ? (UnityEngine.Random.value < 0.5f ? Yellow : Color.white) : new Color(1f, 0.4f, 0.5f), UnityEngine.Random.Range(36f, 70f), 0.8f);
            }

            if (t >= nextShine)
            {
                nextShine = t + 2.8f;
                StartCoroutine(ShineSequence());
            }

            if (!lastCleared)
            {
                vignette.color = new Color(0.6f, 0f, 0.08f, 0.32f + 0.1f * Mathf.Sin(t * 3f));
                var lrt = resultLogo.rectTransform;
                if (Mathf.Repeat(t, 1.7f) < 0.12f)
                {
                    lrt.anchoredPosition = basePos[lrt] + new Vector2(UnityEngine.Random.Range(-14f, 14f), UnityEngine.Random.Range(-4f, 4f));
                    resultLogo.color = Time.frameCount % 2 == 0 ? new Color(0.5f, 1f, 1f) : new Color(1f, 0.45f, 0.55f);
                }
                else
                {
                    lrt.anchoredPosition = basePos[lrt];
                    resultLogo.color = Color.white;
                }
            }
        }

        private IEnumerator ShineSequence()
        {
            for (int i = 0; i < shines.Count; i++)
            {
                if (!shines[i].transform.parent.gameObject.activeInHierarchy) continue;
                StartCoroutine(Shine(shines[i]));
                yield return Wait(0.12f, true);
            }
        }

        private IEnumerator Shine(Image band)
        {
            var rt = band.rectTransform;
            float w = ((RectTransform)rt.parent).rect.width;
            float e = 0f;
            while (e < 0.55f)
            {
                e += Step(true);
                float k = Mathf.Clamp01(e / 0.55f);
                rt.anchoredPosition = new Vector2(Mathf.Lerp(-w * 0.65f, w * 0.65f, k), 0f);
                SetA(band, Mathf.Sin(k * Mathf.PI) * 0.75f);
                yield return null;
            }
            SetA(band, 0f);
        }

        private IEnumerator SpeedLines(bool cleared)
        {
            int n = 12;
            var lines = new List<RectTransform>();
            for (int i = 0; i < n; i++)
            {
                Image img = CreateImage("Streak", fxLayer, bandTex);
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(UnityEngine.Random.Range(500f, 1100f), UnityEngine.Random.Range(5f, 16f));
                rt.anchoredPosition = new Vector2(-1500f - i * 160f, UnityEngine.Random.Range(-500f, 500f));
                img.color = i % 2 == 0 ? (cleared ? new Color(1f, 0.35f, 0.8f, 0.7f) : new Color(1f, 0.2f, 0.3f, 0.7f)) : new Color(0.3f, 0.9f, 1f, 0.6f);
                lines.Add(rt);
            }
            float e = 0f;
            while (e < 0.7f)
            {
                float d = Step(true);
                e += d;
                foreach (var rt in lines) rt.anchoredPosition += new Vector2(5200f * d, 0f);
                yield return null;
            }
            foreach (var rt in lines) Destroy(rt.gameObject);
        }

        private IEnumerator AfterImages(Image source, Vector2 offset, float time)
        {
            var ghosts = new List<Image>();
            for (int i = 0; i < 3; i++)
            {
                Image g = CreateImage("Ghost", content, source.sprite);
                g.raycastTarget = false;
                g.rectTransform.anchorMin = g.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                g.rectTransform.sizeDelta = source.rectTransform.sizeDelta;
                g.transform.SetSiblingIndex(source.transform.GetSiblingIndex());
                g.color = i % 2 == 0 ? new Color(1f, 0.3f, 0.8f, 0f) : new Color(0.3f, 0.9f, 1f, 0f);
                ghosts.Add(g);
            }
            var end = basePos[source.rectTransform];
            float e = 0f;
            while (e < time + 0.35f)
            {
                e += Step(true);
                for (int i = 0; i < ghosts.Count; i++)
                {
                    float k = Mathf.Clamp01((e - 0.05f * (i + 1)) / time);
                    ghosts[i].rectTransform.anchoredPosition = Vector2.LerpUnclamped(end + offset, end, EaseOutBack(k)) - new Vector2(30f * (i + 1) * (1f - k), 0f);
                    float a = e < time ? 0.35f : 0.35f * (1f - (e - time) / 0.35f);
                    SetA(ghosts[i], Mathf.Clamp01(a) * (1f - i * 0.25f));
                }
                yield return null;
            }
            foreach (var g in ghosts) Destroy(g.gameObject);
        }

        private IEnumerator RowFlash(float y)
        {
            Image bar = CreateImage("RowFlash", statsPanel.transform, bandTex);
            bar.raycastTarget = false;
            var rt = bar.rectTransform;
            SetRect(rt, new Vector2(statsPanel.rectTransform.rect.width * 0.5f, 50f), new Vector2(0f, y));
            float e = 0f;
            while (e < 0.35f)
            {
                e += Step(true);
                float k = Mathf.Clamp01(e / 0.35f);
                rt.anchoredPosition = new Vector2(Mathf.Lerp(-220f, 220f, k), y);
                bar.color = new Color(1f, 1f, 1f, Mathf.Sin(k * Mathf.PI) * 0.55f);
                yield return null;
            }
            Destroy(bar.gameObject);
        }

        private IEnumerator ScoreSparks(float time)
        {
            float e = 0f;
            while (e < time && !skip)
            {
                e += Step();
                if (Time.frameCount % 2 == 0)
                {
                    var p = FxPos(scoreText.rectTransform) + new Vector2(UnityEngine.Random.Range(-120f, 160f), UnityEngine.Random.Range(-20f, 20f));
                    Spawn(p, new Vector2(UnityEngine.Random.Range(-80f, 80f), UnityEngine.Random.Range(120f, 320f)), UnityEngine.Random.value < 0.5f ? Yellow : Color.white, UnityEngine.Random.Range(14f, 26f), 0.6f);
                }
                yield return null;
            }
        }

        private IEnumerator Shockwave(Vector2 pos, Color color, float maxScale)
        {
            Image ring = CreateImage("Shockwave", fxLayer, ringTex);
            ring.raycastTarget = false;
            var rt = ring.rectTransform;
            SetRect(rt, new Vector2(420f, 420f), pos);
            float e = 0f;
            while (e < 0.45f)
            {
                e += Step(true);
                float k = Mathf.Clamp01(e / 0.45f);
                rt.localScale = Vector3.one * Mathf.Lerp(0.2f, maxScale, EaseOut(k));
                ring.color = new Color(color.r, color.g, color.b, 0.9f * (1f - k));
                yield return null;
            }
            Destroy(ring.gameObject);
        }

        private IEnumerator FadeImage(Image img, float target, float time)
        {
            float from = img.color.a;
            float e = 0f;
            while (e < time)
            {
                e += Step(true);
                SetA(img, Mathf.Lerp(from, target, e / time));
                yield return null;
            }
            SetA(img, target);
        }

        private void Burst(Vector2 pos, int count, Color color, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)count * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
                var v = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed * UnityEngine.Random.Range(0.5f, 1f);
                Spawn(pos, v, i % 3 == 0 ? Color.white : color, UnityEngine.Random.Range(22f, 44f), UnityEngine.Random.Range(0.5f, 0.85f));
            }
        }

        private void Spawn(Vector2 pos, Vector2 vel, Color color, float size, float life)
        {
            Image img = CreateImage("Sparkle", fxLayer, starTex);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            SetRect(rt, new Vector2(size, size), pos);
            img.color = color;
            sparkles.Add(new Sparkle { rt = rt, img = img, vel = vel, life = life, size = size, spin = UnityEngine.Random.Range(-200f, 200f) });
        }

        private void UpdateSparkles()
        {
            float d = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            for (int i = sparkles.Count - 1; i >= 0; i--)
            {
                var sp = sparkles[i];
                sp.age += d;
                if (sp.age >= sp.life || sp.rt == null) { if (sp.rt != null) Destroy(sp.rt.gameObject); sparkles.RemoveAt(i); continue; }
                float k = sp.age / sp.life;
                sp.vel *= 1f - 2.5f * d;
                sp.rt.anchoredPosition += sp.vel * d;
                sp.rt.localEulerAngles += new Vector3(0f, 0f, sp.spin * d);
                sp.rt.localScale = Vector3.one * Mathf.Sin(Mathf.Min(1f, k * 1.3f) * Mathf.PI) * 1.1f;
                var c = sp.img.color; c.a = 1f - k * k; sp.img.color = c;
            }
        }

        private Vector2 FxPos(RectTransform rt)
        {
            return fxLayer.InverseTransformPoint(rt.position);
        }

        // ---- Procedural textures ----
        private static void MakeTextures()
        {
            if (rayTex != null) return;
            rayTex = Procedural(256, 256, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                float a = Mathf.Atan2(y, x);
                float wedge = SStep(0.55f, 0.95f, Mathf.Cos(a * 12f) * 0.5f + 0.5f);
                return wedge * Mathf.Pow(Mathf.Clamp01(1f - r), 1.4f) * SStep(0.02f, 0.15f, r);
            });
            starTex = Procedural(64, 64, (x, y) =>
            {
                float ax = Mathf.Abs(x), ay = Mathf.Abs(y);
                float cross = Mathf.Exp(-ax * 16f) * Mathf.Exp(-ay * 2.6f) + Mathf.Exp(-ay * 16f) * Mathf.Exp(-ax * 2.6f);
                float glow = Mathf.Exp(-(x * x + y * y) * 14f);
                return Mathf.Clamp01(cross + glow * 0.7f);
            });
            ringTex = Procedural(128, 128, (x, y) =>
            {
                float r = Mathf.Sqrt(x * x + y * y);
                float d = (r - 0.82f) / 0.07f;
                return Mathf.Exp(-d * d);
            });
            glowTex = Procedural(64, 64, (x, y) => { float r = Mathf.Sqrt(x * x + y * y); return Mathf.Pow(Mathf.Clamp01(1f - r), 2f); });
            bandTex = Procedural(64, 8, (x, y) => Mathf.Exp(-(x * x) / 0.12f));
            beamTex = Procedural(64, 256, (x, y) =>
            {
                float down = (1f - y) * 0.5f;
                float width = 0.12f + 0.88f * down;
                float edge = 1f - SStep(width * 0.55f, width, Mathf.Abs(x));
                return edge * Mathf.Pow(1f - down, 0.8f) * SStep(0f, 0.08f, down);
            });
            vignetteTex = Procedural(128, 128, (x, y) => { float r = Mathf.Sqrt(x * x + y * y); return SStep(0.8f, 1.4f, r); });
        }

        private static float SStep(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        private static Sprite Procedural(int w, int h, Func<float, float, float> alpha)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float x = (i + 0.5f) / w * 2f - 1f, y = (j + 0.5f) / h * 2f - 1f;
                    byte a = (byte)(Mathf.Clamp01(alpha(x, y)) * 255f);
                    px[j * w + i] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---- Confetti ----
        private void SpawnConfetti(int count)
        {
            Color[] cols = { Hex("FF3FA4"), Cyan, Yellow, Hex("B05CFF"), Color.white };
            for (int i = 0; i < count; i++)
            {
                Image img = CreateImage("Bit", confettiLayer, null);
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(UnityEngine.Random.Range(10f, 20f), UnityEngine.Random.Range(18f, 34f));
                rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-980f, 980f), UnityEngine.Random.Range(560f, 900f));
                rt.localEulerAngles = new Vector3(0f, 0f, UnityEngine.Random.Range(0f, 360f));
                img.color = cols[UnityEngine.Random.Range(0, cols.Length)];
                confetti.Add(rt);
            }
        }

        private void UpdateConfetti()
        {
            if (confetti.Count == 0) return;
            float t = Time.unscaledTime;
            float d = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            for (int i = confetti.Count - 1; i >= 0; i--)
            {
                var rt = confetti[i];
                float seed = i * 0.37f;
                rt.anchoredPosition += new Vector2(Mathf.Sin(t * 2f + seed) * 60f, -(220f + (i % 7) * 25f)) * d;
                rt.localEulerAngles += new Vector3(0f, 0f, (120f + (i % 5) * 40f) * d);
                rt.localScale = new Vector3(Mathf.Cos(t * 5f + seed), 1f, 1f);
                if (rt.anchoredPosition.y < -620f)
                {
                    if (lastCleared && (t < confettiUntil || confetti.Count <= 22)) rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-980f, 980f), 600f);
                    else { Destroy(rt.gameObject); confetti.RemoveAt(i); }
                }
            }
        }

        private void ClearConfetti()
        {
            foreach (var rt in confetti) if (rt != null) Destroy(rt.gameObject);
            confetti.Clear();
            confettiUntil = Time.unscaledTime + 6f;
        }

        private void SetButtonsInteractable(bool on)
        {
            foreach (var b in buttons)
            {
                var btn = b.GetComponent<Button>();
                if (btn != null) btn.interactable = on;
            }
        }

        // ---- Helpers ----
        private static void Fade(Component c, float a)
        {
            if (c == null) return;
            var cg = c.GetComponent<CanvasGroup>();
            if (cg == null) cg = c.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = a;
            cg.blocksRaycasts = a > 0.01f;
        }
        private static void SetA(Graphic g, float a) { if (g == null) return; var c = g.color; c.a = a; g.color = c; }

        private static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - Mathf.Pow(1f - x, 3f); }
        private static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        private static bool AnyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (UnityEngine.InputSystem.Touchscreen.current != null && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
#else
            return Input.anyKeyDown;
#endif
        }

        private static void Play(AudioClip clip)
        {
            if (clip != null && SFXManager.Instance != null) SFXManager.Instance.PlaySFX(clip);
        }

        private static Color GradeColor(Grade grade)
        {
            return grade switch
            {
                Grade.S => Yellow,
                Grade.A => Red,
                Grade.B => new Color(1f, 0.5f, 0f),
                Grade.C => new Color(0f, 0.8f, 0.2f),
                Grade.D => new Color(0f, 0.6f, 1f),
                Grade.E => new Color(0.8f, 0.2f, 0.8f),
                Grade.F => new Color(0.6f, 0.6f, 0.6f),
                _       => Color.white
            };
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }
    }
}
