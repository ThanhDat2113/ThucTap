using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RhythmGame
{
    /// <summary>
    /// Màn hình kết quả trò chơi — thay thế TurnResultPanel.
    ///
    /// Nguyên tắc: KHÔNG tự tính toán gì. Mọi con số (score, combo, accuracy,
    /// grade, perfect/good/bad/miss, totalNotes) đọc TRỰC TIẾP từ
    /// BattleUIController — nguồn gameplay duy nhất. Script này chỉ:
    ///   - Đợi GameManager báo Finished/Failed rồi hiển thị.
    ///   - Chừa chỗ cho post-battle dialogue (StoryDialogueManager) chạy trước.
    ///   - Lưu thành tích 1 lần (SongStats.SaveResult + PlayerPrefs best).
    ///   - Xử lý 3 nút: Continue (ẩn), Retry (chơi lại), Menu (về song list).
    /// </summary>
    public class GameplayResultScreen : MonoBehaviour
    {
        // Sự kiện cho phép bên ngoài override hành vi nút (nếu muốn).
        public event Action RetryRequested;
        public event Action ContinueRequested;
        public event Action MenuRequested;

        [Header("Config")]
        [SerializeField] private TMP_FontAsset displayFont;
        [Tooltip("Tên scene song list — nút MENU sẽ load scene này nếu không có ai subscribe MenuRequested")]
        [SerializeField] private string songListSceneName = "Thuan_SongList";

        // ----- UI refs (dựng trong Awake) -----
        private RectTransform content;
        private Sprite stageBackground, clearLogoSprite, failedLogoSprite;
        private Image resultLogo, coverArt;
        private TMP_Text songText, artistText, difficultyText;
        private TMP_Text perfectText, goodText, badText, missText;
        private TMP_Text comboText, scoreText, rankText, newBestText;
        private GameObject nextButton;
        private RectTransform retryButton, menuButton;

        private readonly System.Collections.Generic.Dictionary<string, Sprite> sprites
            = new System.Collections.Generic.Dictionary<string, Sprite>();

        // ----- State -----
        private CanvasGroup canvasGroup;
        private bool isShown;
        private Coroutine popCoroutine;
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

            // Ép RectTransform phủ kín canvas cha
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

            // CanvasGroup làm lớp ẩn dự phòng
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (GetComponentInParent<Canvas>() == null)
                Debug.LogWarning("[GameplayResultScreen] Không có Canvas cha — UI sẽ hiển thị sai.");

            // Load sprites
            stageBackground = Resources.Load<Sprite>("Result/result_stage_background");
            clearLogoSprite = Resources.Load<Sprite>("Result/stage_clear");
            failedLogoSprite = Resources.Load<Sprite>("Result/stage_failed");
            foreach (Sprite s in Resources.LoadAll<Sprite>("Result/result_assets"))
                if (!sprites.ContainsKey(s.name)) sprites[s.name] = s;

            foreach (Transform child in transform) child.gameObject.SetActive(false);
            Build();

            // Subscribe an toàn — chờ GameManager sẵn sàng
            StartCoroutine(SubscribeWhenReady());

            ForceHide();
            Debug.Log("[GameplayResultScreen] Awake xong — đã ẩn.");
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

            // Race: state đã Finished/Failed trước khi kịp subscribe
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
                // ★ Story Mode + có post-battle dialogue (chiến thắng) -> đợi StoryDialogueManager
                //   gọi ShowPostBattleResults() -> ShowResult(true) sau khi dialogue xong.
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
                // ★ Story Mode + có post-battle dialogue (thất bại) -> đợi StoryDialogueManager
                //   gọi ShowPostBattleResults() -> ShowResult(false) sau khi dialogue xong.
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
        //  Đọc dữ liệu + hiển thị
        // ==================================================================

        public void ShowResult(bool cleared)
        {
            // ★ Đọc TRỰC TIẾP từ BattleUIController — không tự tính
            var battleUI = FindObjectOfType<BattleUIController>();
            if (battleUI == null)
            {
                Debug.LogError("[GameplayResultScreen] Không có BattleUIController — kết quả sẽ là 0.");
            }

            int   score      = battleUI != null ? battleUI.playerScore       : 0;
            int   maxCombo   = battleUI != null ? battleUI.playerMaxCombo    : 0;
            int   totalNotes = battleUI != null ? battleUI.TotalNoteSlots    : 0;
            int   perfect    = battleUI != null ? battleUI.playerPerfect     : 0;
            int   good       = battleUI != null ? battleUI.playerGood        : 0;
            int   bad        = battleUI != null ? battleUI.playerBad         : 0;
            int   miss       = battleUI != null ? battleUI.playerMiss        : 0;
            float accuracy   = battleUI != null ? battleUI.PlayerHitRatio * 100f : 0f;
            Grade grade      = battleUI != null ? battleUI.PlayerGrade       : Grade.F;

            // Song info
            SongData song = GameSession.SelectedSong;
            if (song == null && MusicManager.Instance != null)
                song = MusicManager.Instance.CurrentSong;

            // Lưu thành tích (giống TurnResultPanel) + kiểm tra kỷ lục mới
            bool newBest = SaveAchievements(song, cleared, score, maxCombo, accuracy);

            Show(cleared, song, score, maxCombo, accuracy, grade,
                 perfect, good, bad, miss, newBest);
        }

        /// <summary>
        /// Lưu thành tích — chép logic từ TurnResultPanel nhưng gộp 1 chỗ.
        /// Không ghi đè kỷ lục khi Failed.
        /// </summary>
        private bool SaveAchievements(SongData song, bool cleared, int score, int maxCombo, float accuracyPercent)
        {
            if (song == null)
            {
                Debug.LogWarning("[GameplayResultScreen] Không có SongData — bỏ qua lưu thành tích.");
                return false;
            }

            string keyScore = "best_score_" + song.name;
            int prevBest = PlayerPrefs.GetInt(keyScore, 0);
            bool newBest = cleared && score > prevBest;

            if (!cleared) return false;

            // Ghi vào SongStats (nguồn chính nếu project có dùng)
            try
            {
                SongStats.SaveResult(song, score, maxCombo, accuracyPercent / 100f);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameplayResultScreen] SongStats.SaveResult lỗi: " + e.Message);
            }

            // Ghi best score để so sánh lần sau (trong trường hợp SongStats không expose best)
            if (newBest) PlayerPrefs.SetInt(keyScore, score);
            PlayerPrefs.Save();

            return newBest;
        }

        public void Show(bool cleared, SongData song, int score, int maxCombo, float accuracy,
                         Grade grade, int perfect, int good, int bad, int miss, bool newBest)
        {
            Debug.Log("[GameplayResultScreen] Show() — hiển thị kết quả.");
            isShown = true;

            // Freeze gameplay (giống TurnResultPanel)
            Time.timeScale = 0f;

            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            // ----- Logo + song info -----
            resultLogo.sprite = cleared ? clearLogoSprite : failedLogoSprite;

            songText.text     = song != null ? song.songName : "RHYTHM BATTLE";
            artistText.text   = song != null && !string.IsNullOrWhiteSpace(song.artist)
                                ? song.artist : "UNKNOWN ARTIST";
            difficultyText.text = song != null ? song.difficulty + "/5" : "?/5";
            coverArt.sprite   = song != null ? song.coverArt : null;
            coverArt.enabled  = coverArt.sprite != null;

            // ----- Stats (đọc thô từ BattleUIController, không tính lại) -----
            perfectText.text = perfect.ToString("N0");
            goodText.text    = good.ToString("N0");
            badText.text     = bad.ToString("N0");
            missText.text    = miss.ToString("N0");
            comboText.text   = maxCombo.ToString("N0");
            scoreText.text   = score.ToString("N0");

            // ----- Grade + màu -----
            rankText.text  = grade.ToString();
            rankText.color = GradeColor(grade);

            if (newBestText != null) newBestText.gameObject.SetActive(newBest);

            // ----- Button layout (Next ẩn mặc định, không có next song) -----
            const bool hasNext = false;
            if (nextButton != null) nextButton.SetActive(hasNext);
            if (retryButton != null)
                retryButton.anchoredPosition = new Vector2(
                    (hasNext ? 670f : 815f) * ScaleX - 960f, retryButton.anchoredPosition.y);
            if (menuButton != null)
                menuButton.anchoredPosition = new Vector2(
                    (hasNext ? 1325f : 1165f) * ScaleX - 960f, menuButton.anchoredPosition.y);

            // ----- Pop animation -----
            content.localScale = Vector3.one * 0.92f;
            if (popCoroutine != null) StopCoroutine(popCoroutine);
            popCoroutine = StartCoroutine(Pop());
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
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }

        // ==================================================================
        //  Button handlers — hành vi mặc định giống TurnResultPanel
        // ==================================================================

        private void OnRetryClicked()
        {
            Time.timeScale = 1f;
            if (RetryRequested != null) { RetryRequested.Invoke(); return; }
            if (GameManager.Instance != null) GameManager.Instance.Restart();
        }

        private void OnContinueClicked()
        {
            Time.timeScale = 1f;
            if (ContinueRequested != null) ContinueRequested.Invoke();
            ForceHide();
        }

        private void OnMenuClicked()
        {
            Time.timeScale = 1f;
            if (MenuRequested != null) { MenuRequested.Invoke(); return; }
            if (!string.IsNullOrEmpty(songListSceneName))
                SceneManager.LoadScene(songListSceneName);
            else
                Debug.LogWarning("[GameplayResultScreen] Chưa đặt songListSceneName cho nút MENU.");
        }

        // ==================================================================
        //  Build UI (giữ nguyên từ phiên bản trước)
        // ==================================================================

        private void Build()
        {
            Image background = CreateImage("StageBackground", transform, stageBackground);
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

            Image character = CreateImage("Character", content, Resources.Load<Sprite>("Result/result_character"));
            SetRect(character.rectTransform,
                new Vector2(720f * ScaleX, 660f * ScaleY), new Vector2(-546.6f, 5.3f));
            character.raycastTarget = false;
            character.rectTransform.sizeDelta *= 1.28f;
            character.rectTransform.anchoredPosition += new Vector2(20f, -185f);

            Image songPanel  = PlaceSprite("SongPanel",  "song_panel",  new Rect(720, 710, 850, 231));
            Image statsPanel = PlaceSprite("StatsPanel", "stats_panel", new Rect(705, 315, 570, 400));
            Image rankPanel  = PlaceSprite("RankPanel",  "rank_panel",  new Rect(1270, 285, 402, 440));
            Image scoreBar   = PlaceSprite("ScoreBar",   "score_bar",   new Rect(700, 175, 630, 145));

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

        private IEnumerator Pop()
        {
            float elapsed = 0f;
            while (elapsed < .28f)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / .28f);
                content.localScale = Vector3.one * Mathf.Lerp(.92f, 1f, 1f - Mathf.Pow(1f - progress, 3f));
                yield return null;
            }
            content.localScale = Vector3.one;
            popCoroutine = null;
        }

        // ==================================================================
        //  Grade → màu (giống TurnResultPanel.GetGradeColor)
        // ==================================================================

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
