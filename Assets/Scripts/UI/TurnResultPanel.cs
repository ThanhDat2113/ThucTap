using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

namespace RhythmGame
{
    public class TurnResultPanel : MonoBehaviour
    {
        [Header("Refs")]
        public GameManager gameManager;
        public BattleUIController battleUI;
        public UIController uiController;

        [Header("Result Screen mới (kiểu ảnh 2)")]
        [Tooltip("Gán object ResultPanel có GameplayResultScreen. Để trống = dùng UI cũ.")]
        public GameplayResultScreen resultScreen;

        [Header("Panel")]
        public GameObject panelRoot;
        public CanvasGroup panelCanvasGroup;

        [Header("Content (UI cũ)")]
        public TMP_Text songNameText;
        public TMP_Text scoreText;
        public TMP_Text totalNotesText;
        public TMP_Text comboText;
        public TMP_Text maxComboText;
        public TMP_Text accuracyText;
        public TMP_Text gradeText;
        public TMP_Text perfectText;
        public TMP_Text goodText;
        public TMP_Text badText;
        public TMP_Text missText;
        public Button continueButton;
        public Button retryButton;
        public Button confirmButton; // Trở về màn hình chọn nhạc

        [Header("Scene")]
        public string songListSceneName = "Thuan_SongList";

        [Header("Animation")]
        public float fadeDuration = 0.3f;

        void Start()
        {
            gameManager.OnStateChanged += OnStateChanged;

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);

            // UI mới: nối sự kiện 3 nút
            if (resultScreen != null)
            {
                resultScreen.RetryRequested += OnRetryClicked;
                resultScreen.ContinueRequested += OnContinueClicked;
                resultScreen.MenuRequested += OnConfirmClicked;
            }

            if (panelRoot != null) panelRoot.SetActive(false);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;

            if (uiController == null) uiController = FindObjectOfType<UIController>();
        }

        void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnStateChanged -= OnStateChanged;
            }
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);
            if (retryButton != null)
                retryButton.onClick.RemoveListener(OnRetryClicked);
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmClicked);

            if (resultScreen != null)
            {
                resultScreen.RetryRequested -= OnRetryClicked;
                resultScreen.ContinueRequested -= OnContinueClicked;
                resultScreen.MenuRequested -= OnConfirmClicked;
            }
        }

        void OnStateChanged(GameState state)
        {
            if (state == GameState.Playing)
            {
                // Gameplay counters now live on BattleUIController; nothing to reset here.
            }
            else if (state == GameState.Finished || state == GameState.Failed)
            {
                ShowFinalResult(state);
            }
        }

        void ShowFinalResult(GameState endState)
        {
            if (panelRoot == null) return;

            float hitRatio = battleUI != null ? battleUI.PlayerHitRatio : 0f;
            int total = battleUI != null ? battleUI.TotalNoteSlots : 0;
            int p = battleUI != null ? battleUI.playerPerfect : 0;
            int g = battleUI != null ? battleUI.playerGood : 0;
            int b = battleUI != null ? battleUI.playerBad : 0;
            int m = battleUI != null ? battleUI.playerMiss : 0;
            Grade grade = battleUI != null ? battleUI.PlayerGrade : Grade.F;
            Debug.Log($"[TurnResultPanel] Perfect={p}, Good={g}, TotalSlots={total}, HitRatio={hitRatio*100f:F1}%, Grade={grade}");

            // Lấy SongData (dùng cho cả lưu thành tích lẫn UI mới)
            SongData songToSave = GameSession.SelectedSong;
            if (songToSave == null && MusicManager.Instance != null)
            {
                songToSave = MusicManager.Instance.CurrentSong;
            }
            if (songToSave == null && gameManager != null && gameManager.chart != null)
            {
                // Fallback cho debug: tạo SongData tạm
                songToSave = ScriptableObject.CreateInstance<SongData>();
                songToSave.songName = gameManager.chart.songName;
                songToSave.name = gameManager.chart.songName;
            }

            if (resultScreen != null)
            {
                // ===== UI MỚI =====
                bool cleared = endState == GameState.Finished;
                resultScreen.Show(
                    cleared, songToSave, null,
                    battleUI != null ? battleUI.playerScore : 0,
                    battleUI != null ? battleUI.playerMaxCombo : 0,
                    hitRatio * 100f,
                    p, g, b, m,
                    false,   // newBest: chưa có dữ liệu, nối sau nếu cần
                    false);  // hasNext: chỉ true khi có bài kế tiếp trong Story Mode
            }
            else
            {
                // ===== UI CŨ =====
                if (songNameText != null && gameManager != null && gameManager.chart != null)
                    songNameText.text = gameManager.chart.songName;

                if (scoreText != null) scoreText.text = battleUI.playerScore.ToString();
                if (totalNotesText != null) totalNotesText.text = battleUI.TotalNoteSlots.ToString();
                if (comboText != null) comboText.text = battleUI.playerMaxCombo.ToString();
                if (maxComboText != null) maxComboText.text = battleUI.playerMaxCombo.ToString();
                if (accuracyText != null) accuracyText.text = string.Format("{0:F1}%", hitRatio * 100f);

                if (gradeText != null)
                {
                    gradeText.text = grade.ToString();
                    gradeText.color = GetGradeColor(grade);
                }

                if (perfectText != null) perfectText.text = p.ToString();
                if (goodText != null) goodText.text = g.ToString();
                if (badText != null) badText.text = b.ToString();
                if (missText != null) missText.text = m.ToString();
            }

            // Save achievement (best score, combo, accuracy, play count)
            if (songToSave != null)
            {
                SongStats.SaveResult(songToSave, battleUI.playerScore, battleUI.playerMaxCombo, hitRatio);
                Debug.Log($"[TurnResultPanel] Đã lưu thành tích: Song={songToSave.songName}, Score={battleUI.playerScore}, MaxCombo={battleUI.playerMaxCombo}, Accuracy={hitRatio * 100f:F1}%");
            }
            else
            {
                Debug.LogWarning("[TurnResultPanel] Không thể lưu thành tích: Không tìm thấy SongData");
            }

            // Show panel with fade in
            panelRoot.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadePanel(0f, 1f, fadeDuration));

            // Pause gameplay
            Time.timeScale = 0f;
        }

        void OnContinueClicked()
        {
            Time.timeScale = 1f;
            StartCoroutine(FadeAndHide());
        }

        void OnRetryClicked()
        {
            Time.timeScale = 1f;
            StartCoroutine(FadeAndRetry());
        }

        IEnumerator FadeAndRetry()
        {
            yield return FadePanel(1f, 0f, fadeDuration);
            HidePanelImmediate();
            if (gameManager != null) gameManager.Restart();
        }

        void OnConfirmClicked()
        {
            Time.timeScale = 1f;
            StartCoroutine(FadeAndGoToSongList());
        }

        IEnumerator FadeAndGoToSongList()
        {
            yield return FadePanel(1f, 0f, fadeDuration);
            HidePanelImmediate();
            SceneManager.LoadScene(songListSceneName);
        }

        IEnumerator FadeAndHide()
        {
            yield return FadePanel(1f, 0f, fadeDuration);
            HidePanelImmediate();
        }

        IEnumerator FadePanel(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(from, to, t);
                if (panelCanvasGroup != null) panelCanvasGroup.alpha = alpha;
                yield return null;
            }
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = to;
        }

        void HidePanelImmediate()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
        }

        Color GetGradeColor(Grade grade)
        {
            return grade switch
            {
                Grade.S => Color.yellow,
                Grade.A => new Color(1f, 0.2f, 0.2f),
                Grade.B => new Color(1f, 0.5f, 0f),
                Grade.C => new Color(0f, 0.8f, 0.2f),
                Grade.D => new Color(0f, 0.6f, 1f),
                Grade.E => new Color(0.8f, 0.2f, 0.8f),
                Grade.F => new Color(0.6f, 0.6f, 0.6f),
                _ => Color.white
            };
        }
    }
}