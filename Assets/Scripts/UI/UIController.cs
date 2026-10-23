using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace RhythmGame
{
    public enum Grade { F, E, D, C, B, A, S }

    public class UIController : MonoBehaviour
    {
        [Header("Refs")]
        public GameManager gameManager;
        public JudgementSystem judgement;
        public FlowSystem flowSystem;

        [Header("UI Elements")]
        public TMP_Text countdownText;
        public TMP_Text scoreText;
        public TMP_Text comboText;
        public TMP_Text messageText;
        [Tooltip("Chữ grade (S/A/B/C/D/E/F). Để trống thì phần này tự tắt.")]
        public TMP_Text gradeText;

        [Header("Flow UI")]
        public Image flowBar;
        public TMP_Text flowText;
        public TMP_Text flowStatusText;

        [Header("Grade Thresholds")]
        [Range(0f, 1f)] public float thresholdS = 0.95f;
        [Range(0f, 1f)] public float thresholdA = 0.90f;
        [Range(0f, 1f)] public float thresholdB = 0.80f;
        [Range(0f, 1f)] public float thresholdC = 0.70f;
        [Range(0f, 1f)] public float thresholdD = 0.60f;
        [Range(0f, 1f)] public float thresholdE = 0.50f;

        [Header("Grade Effects")]
        public bool gradePopOnChange = true;
        public float gradePopScale = 1.25f;
        public float gradePopDuration = 0.18f;

        [Header("Countdown Effects")]
        public bool countdownPopOnChange = true;
        public float countdownPopScale = 1.5f;
        public float countdownPopDuration = 0.15f;

        int score, combo, maxCombo;
        Grade currentGrade = Grade.F;
        float gradePopTimer = 999f;
        Vector3 gradeBaseScale = Vector3.one;

        int lastCountdownNumber = -1;
        float countdownPopTimer = 999f;
        Vector3 countdownBaseScale = Vector3.one;

        void Start()
        {
            gameManager.OnStateChanged += HandleStateChanged;
            judgement.OnJudged += HandleJudged;
            if (flowSystem != null)
            {
                flowSystem.OnFlowAmountChanged += HandleFlowAmountChanged;
                flowSystem.OnFlowStateChanged += HandleFlowStateChanged;
            }
            if (gradeText != null) gradeBaseScale = gradeText.transform.localScale;
            if (countdownText != null) countdownBaseScale = countdownText.transform.localScale;
            UpdateGrade();
            UpdateFlowUI();
        }

        void OnDestroy()
        {
            if (gameManager != null) gameManager.OnStateChanged -= HandleStateChanged;
            if (judgement != null) judgement.OnJudged -= HandleJudged;
            if (flowSystem != null)
            {
                flowSystem.OnFlowAmountChanged -= HandleFlowAmountChanged;
                flowSystem.OnFlowStateChanged -= HandleFlowStateChanged;
            }
        }

        void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Finished:
                    messageText.text = "XONG! Nhấn R để chơi lại";
                    messageText.fontStyle = FontStyles.Normal;
                    messageText.enableVertexGradient = false;
                    messageText.color = Color.white;
                    break;

                case GameState.Failed:
                    messageText.text = "GAME OVER";
                    messageText.fontStyle = FontStyles.Bold;
                    messageText.enableVertexGradient = true;
                    messageText.colorGradient = new VertexGradient(
                        new Color(0.95f, 0.25f, 0.25f),
                        new Color(0.95f, 0.25f, 0.25f),
                        new Color(1.00f, 0.60f, 0.60f),
                        new Color(1.00f, 0.60f, 0.60f));
                    break;

                default:
                    messageText.text = "";
                    messageText.fontStyle = FontStyles.Normal;
                    messageText.enableVertexGradient = false;
                    messageText.color = Color.white;
                    break;
            }

            if (state == GameState.Playing)
            {
                score = combo = maxCombo = 0;
                UpdateScoreUI();
                UpdateGrade();
                UpdateFlowUI();
            }
        }

        void HandleJudged(Judgement j, int points, bool isHit, NoteOwner owner)
        {
            // Chỉ xử lý note của player cho single-player UI
            if (owner != NoteOwner.Player) return;

            float multiplier = flowSystem != null ? flowSystem.FlowScoreMultiplier : 1f;
            score += Mathf.RoundToInt(points * multiplier);
            combo = isHit ? combo + 1 : 0;
            maxCombo = Mathf.Max(maxCombo, combo);
            UpdateScoreUI();
            UpdateGrade();
        }

        void UpdateScoreUI()
        {
            scoreText.text = "Score: " + score;
            comboText.text = "Combo: " + combo + " (max " + maxCombo + ")";
        }

        void HandleFlowAmountChanged(float normalized)
        {
            UpdateFlowUI();
        }

        void HandleFlowStateChanged(bool isActive)
        {
            UpdateFlowUI();
            if (flowStatusText != null)
            {
                flowStatusText.text = isActive ? "IN THE FLOW!" : "";
                flowStatusText.color = isActive ? Color.magenta : Color.white;
            }
        }

        void UpdateFlowUI()
        {
            if (flowBar != null)
                flowBar.fillAmount = flowSystem != null ? flowSystem.NormalizedFlow : 0f;
            if (flowText != null && flowSystem != null)
                flowText.text = $"FLOW: {flowSystem.FlowAmount:F0}/{flowSystem.maxFlow:F0}";
        }

        void UpdateGrade()
        {
            if (gradeText == null) return;

            ChartData chart = gameManager != null ? gameManager.chart : null;
            int maxScore = chart != null ? CalculateMaxScore(chart) : 0;
            float percentage = maxScore > 0 ? (float)score / maxScore : 0f;
            Grade grade = CalculateGrade(percentage);

            if (grade != currentGrade)
            {
                currentGrade = grade;
                gradePopTimer = 0f;
            }

            gradeText.text = grade.ToString();
            gradeText.color = GetGradeColor(grade);
        }

        int CalculateMaxScore(ChartData chart)
        {
            int maxScore = 0;
            foreach (var note in chart.notes)
            {
                if (note.type == NoteType.Hold) maxScore += 400;
                else maxScore += 300;
            }
            return maxScore;
        }

        Grade CalculateGrade(float percentage)
        {
            if (percentage >= thresholdS) return Grade.S;
            if (percentage >= thresholdA) return Grade.A;
            if (percentage >= thresholdB) return Grade.B;
            if (percentage >= thresholdC) return Grade.C;
            if (percentage >= thresholdD) return Grade.D;
            if (percentage >= thresholdE) return Grade.E;
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

        void Update()
        {
            bool counting = gameManager.State == GameState.Countdown;
            countdownText.gameObject.SetActive(counting);
            if (counting)
            {
                int num = Mathf.CeilToInt(gameManager.CountdownRemaining);
                countdownText.text = num.ToString();

                if (!countdownText.enableVertexGradient)
                {
                    countdownText.fontStyle = FontStyles.Bold;
                    countdownText.enableVertexGradient = true;
                    countdownText.colorGradient = new VertexGradient(
                        new Color(0.2f, 0.8f, 1f),
                        new Color(0.2f, 0.8f, 1f),
                        new Color(0.0f, 0.5f, 1f),
                        new Color(0.0f, 0.5f, 1f));
                }

                if (countdownPopOnChange && countdownText != null && num != lastCountdownNumber)
                {
                    lastCountdownNumber = num;
                    countdownPopTimer = 0f;
                }
            }
            else
            {
                lastCountdownNumber = -1;
                countdownPopTimer = 999f;
                if (countdownText != null)
                {
                    countdownText.enableVertexGradient = false;
                    countdownText.fontStyle = FontStyles.Normal;
                    countdownText.transform.localScale = countdownBaseScale;
                }
            }

            UpdateCountdownPop();
            UpdateGradePop();
        }

        void UpdateCountdownPop()
        {
            if (countdownText == null || !countdownPopOnChange) return;

            if (countdownPopTimer > countdownPopDuration)
            {
                countdownText.transform.localScale = countdownBaseScale;
                return;
            }

            countdownPopTimer += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(countdownPopTimer / countdownPopDuration);
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            countdownText.transform.localScale = countdownBaseScale * Mathf.Lerp(countdownPopScale, 1f, eased);
        }

        void UpdateGradePop()
        {
            if (gradeText == null || !gradePopOnChange) return;

            if (gradePopTimer > gradePopDuration)
            {
                gradeText.transform.localScale = gradeBaseScale;
                return;
            }

            gradePopTimer += Time.deltaTime;
            float k = Mathf.Clamp01(gradePopTimer / gradePopDuration);
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            gradeText.transform.localScale = gradeBaseScale * Mathf.Lerp(gradePopScale, 1f, eased);
        }
    }
}