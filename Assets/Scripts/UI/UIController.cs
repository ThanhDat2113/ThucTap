using UnityEngine;
using TMPro;

namespace RhythmGame
{
    public enum Grade { F, E, D, C, B, A, S }

    public class UIController : MonoBehaviour
    {
        [Header("Refs")]
        public GameManager gameManager;
        public JudgementSystem judgement;
        public HealthSystem healthSystem;

        [Header("UI Elements (kéo thả trong Inspector)")]
        public TMP_Text countdownText;
        public TMP_Text judgeText;
        public TMP_Text scoreText;
        public TMP_Text comboText;
        public TMP_Text messageText;
        public TMP_Text gradeText;

        int score, combo, maxCombo;
        float judgeTimer;

        void Start()
        {
            gameManager.OnStateChanged += HandleStateChanged;
            judgement.OnJudged += HandleJudged;
            if (gradeText != null) gradeText.gameObject.SetActive(true);
        }

        void OnDestroy()
        {
            if (gameManager != null) gameManager.OnStateChanged -= HandleStateChanged;
            if (judgement != null) judgement.OnJudged -= HandleJudged;
        }

        void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Finished:
                    messageText.text = "XONG! Nhan R de choi lai";
                    messageText.color = Color.white;
                    break;
                case GameState.Failed:
                    messageText.text = "HET MAU! Nhan R de choi lai";
                    messageText.color = new Color(1f, 0.25f, 0.25f);
                    break;
                default:
                    messageText.text = "";
                    break;
            }

            if (state == GameState.Playing)
            {
                score = combo = maxCombo = 0;
                UpdateScoreUI();
                UpdateGrade();
            }
        }

        void HandleJudged(Judgement j, int points, bool isHit)
        {
            score += points;
            combo = isHit ? combo + 1 : 0;
            maxCombo = Mathf.Max(maxCombo, combo);
            UpdateScoreUI();
            UpdateGrade();

            judgeText.text = j == Judgement.HoldComplete ? "HOLD!" : j.ToString().ToUpper();
            judgeText.color = j switch
            {
                Judgement.Perfect => Color.yellow,
                Judgement.Good => Color.green,
                Judgement.Bad => new Color(1f, 0.6f, 0.2f),
                Judgement.HoldComplete => Color.cyan,
                _ => Color.red
            };
            judgeTimer = 0.5f;
        }

        void UpdateGrade()
        {
            if (gradeText == null || gameManager.chart == null) return;

            int maxScore = CalculateMaxScore(gameManager.chart);
            float percentage = maxScore > 0 ? (float)score / maxScore : 0f;
            Grade grade = CalculateGrade(percentage);

            gradeText.text = grade.ToString();
            gradeText.color = GetGradeColor(grade);
        }

        void UpdateScoreUI()
        {
            scoreText.text = "Score: " + score;
            comboText.text = "Combo: " + combo + " (max " + maxCombo + ")";
        }

        int CalculateMaxScore(ChartData chart)
        {
            int maxScore = 0;
            foreach (var note in chart.notes)
            {
                if (note.type == NoteType.Hold)
                    maxScore += 400; // 300 (perfect hit) + 100 (hold complete)
                else
                    maxScore += 300; // tap note max
            }
            return maxScore;
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
                Grade.A => Color.red,
                Grade.B => new Color(1f, 0.5f, 0f),      // Orange
                Grade.C => new Color(0f, 0.8f, 0.2f),    // Green
                Grade.D => new Color(0f, 0.6f, 1f),      // Blue
                Grade.E => new Color(0.8f, 0.2f, 0.8f),  // Purple
                Grade.F => new Color(0.6f, 0.6f, 0.6f),  // Gray
                _ => Color.white
            };
        }

        void Update()
        {
            bool counting = gameManager.State == GameState.Countdown;
            countdownText.gameObject.SetActive(counting);
            if (counting)
                countdownText.text = Mathf.CeilToInt(gameManager.CountdownRemaining).ToString();

            if (judgeTimer > 0f)
            {
                judgeTimer -= Time.deltaTime;
                var c = judgeText.color;
                c.a = Mathf.Clamp01(judgeTimer / 0.3f);
                judgeText.color = c;
            }
        }
    }
}