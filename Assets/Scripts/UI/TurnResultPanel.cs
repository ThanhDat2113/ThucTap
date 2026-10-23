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

        [Header("Panel")]
        public GameObject panelRoot;
        public CanvasGroup panelCanvasGroup;

        [Header("Content")]
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

        // Overall stats (entire song)
        int totalScore;
        int totalCombo;
        int totalMaxCombo;
        int totalPerfect;
        int totalGood;
        int totalBad;
        int totalMiss;
        int totalNotes;

        void Start()
        {
            gameManager.OnStateChanged += OnStateChanged;
            
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            
            if (panelRoot != null) panelRoot.SetActive(false);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
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
        }

        void OnStateChanged(GameState state)
        {
            if (state == GameState.Playing)
            {
                // Reset stats at start
                ResetStats();
            }
            else if (state == GameState.Finished || state == GameState.Failed)
            {
                // Game over - show final results
                ShowFinalResult(state);
            }
        }

        public void RecordJudgement(NoteOwner owner, Judgement judgement)
        {
            // Record for both player and opponent combined
            totalNotes++;
            switch (judgement)
            {
                case Judgement.Perfect: totalPerfect++; break;
                case Judgement.Good: totalGood++; break;
                case Judgement.Bad: totalBad++; break;
                case Judgement.Miss: totalMiss++; break;
            }
        }

        public void RecordScore(int points, int combo, int maxCombo)
        {
            totalScore += points;
            totalCombo = combo;
            totalMaxCombo = maxCombo;
        }

        void ResetStats()
        {
            totalScore = 0;
            totalCombo = 0;
            totalMaxCombo = 0;
            totalPerfect = totalGood = totalBad = totalMiss = 0;
            totalNotes = 0;
        }

        void ShowFinalResult(GameState endState)
        {
            if (panelRoot == null) return;

            // Update content
            if (songNameText != null && gameManager != null && gameManager.chart != null)
            {
                songNameText.text = gameManager.chart.songName;
            }

            if (scoreText != null) scoreText.text = totalScore.ToString();
            if (totalNotesText != null) totalNotesText.text = totalNotes.ToString();
            if (comboText != null) comboText.text = totalCombo.ToString();
            if (maxComboText != null) maxComboText.text = totalMaxCombo.ToString();

            // Calculate accuracy
            float accuracy = totalNotes > 0 ? (float)(totalPerfect + totalGood + totalBad) / totalNotes * 100f : 0f;
            if (accuracyText != null) accuracyText.text = string.Format("{0:F1}%", accuracy);

            // Grade
            Grade grade = CalculateGrade(accuracy / 100f);
            if (gradeText != null)
            {
                gradeText.text = grade.ToString();
                gradeText.color = GetGradeColor(grade);
            }

            // Judgement breakdown
            if (perfectText != null) perfectText.text = totalPerfect.ToString();
            if (goodText != null) goodText.text = totalGood.ToString();
            if (badText != null) badText.text = totalBad.ToString();
            if (missText != null) missText.text = totalMiss.ToString();

            // Save achievement (best score, combo, accuracy, play count)
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

            if (songToSave != null)
            {
                SongStats.SaveResult(songToSave, totalScore, totalMaxCombo, accuracy / 100f);
                Debug.Log($"[TurnResultPanel] Đã lưu thành tích: Song={songToSave.songName}, Score={totalScore}, MaxCombo={totalMaxCombo}, Accuracy={accuracy:F1}%");
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

        System.Collections.IEnumerator FadeAndRetry()
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

        System.Collections.IEnumerator FadeAndGoToSongList()
        {
            yield return FadePanel(1f, 0f, fadeDuration);
            HidePanelImmediate();
            SceneManager.LoadScene(songListSceneName);
        }

        System.Collections.IEnumerator FadeAndHide()
        {
            yield return FadePanel(1f, 0f, fadeDuration);
            HidePanelImmediate();
        }

        System.Collections.IEnumerator FadePanel(float from, float to, float duration)
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

        Grade CalculateGrade(float percentage)
        {
            if (percentage >= 0.95f) return Grade.S;
            if (percentage >= 0.90f) return Grade.A;
            if (percentage >= 0.80f) return Grade.B;
            if (percentage >= 0.70f) return Grade.C;
            if (percentage >= 0.60f) return Grade.D;
            if (percentage >= 0.50f) return Grade.E;
            return Grade.F;
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