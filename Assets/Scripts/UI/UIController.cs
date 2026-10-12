using UnityEngine;
using TMPro;

namespace RhythmGame
{
    public enum Grade { F, E, D, C, B, A, S }

    /// <summary>
    /// Chỉ đọc dữ liệu từ GameManager/JudgementSystem qua event rồi cập nhật
    /// TextMeshPro. Không tự tính điểm, không tự biết luật chấm điểm.
    ///
    /// GHI CHÚ: phần hiển thị chữ "PERFECT/GOOD/BAD/MISS" đã chuyển sang cho
    /// JudgementPopup (gắn trên object JudgeText) phụ trách toàn bộ animation.
    /// UIController ở đây lo Score, Combo, Grade, Countdown, Message — không
    /// đụng vào judgeText để tránh 2 script cùng ghi đè lên 1 Text.
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Header("Refs")]
        public GameManager gameManager;
        public JudgementSystem judgement;

        [Header("UI Elements (kéo thả trong Inspector)")]
        public TMP_Text countdownText;
        public TMP_Text scoreText;
        public TMP_Text comboText;
        public TMP_Text messageText;
        [Tooltip("Chữ grade (S/A/B/C/D/E/F). Để trống thì phần này tự tắt, " +
                 "không gây lỗi. Nên đặt ở góc trên bên phải, cỡ chữ ~90.")]
        public TMP_Text gradeText;

        [Header("Ngưỡng grade (theo tỉ lệ điểm / tổng điểm tối đa)")]
        [Range(0f, 1f)] public float thresholdS = 0.95f;
        [Range(0f, 1f)] public float thresholdA = 0.90f;
        [Range(0f, 1f)] public float thresholdB = 0.80f;
        [Range(0f, 1f)] public float thresholdC = 0.70f;
        [Range(0f, 1f)] public float thresholdD = 0.60f;
        [Range(0f, 1f)] public float thresholdE = 0.50f;

        [Header("Grade Effects")]
        [Tooltip("Grade tăng lên sẽ nảy nhẹ một lần.")]
        public bool gradePopOnChange = true;
        public float gradePopScale = 1.25f;
        public float gradePopDuration = 0.18f;

        [Header("Countdown Effects")]
        [Tooltip("Nảy khi số đếm thay đổi.")]
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
            if (gradeText != null) gradeBaseScale = gradeText.transform.localScale;
            if (countdownText != null) countdownBaseScale = countdownText.transform.localScale;
            UpdateGrade();
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
                    messageText.fontStyle = FontStyles.Normal;
                    messageText.enableVertexGradient = false;
                    messageText.color = Color.white;
                    break;

                // Máu về 0 -> GameManager đã dừng nhạc và chuyển sang Failed.
                case GameState.Failed:
                    messageText.text = "GAME OVER";
                    messageText.fontStyle = FontStyles.Bold;
                    messageText.enableVertexGradient = true;
                    messageText.colorGradient = new VertexGradient(
                        new Color(0.95f, 0.25f, 0.25f),   // trên cùng
                        new Color(0.95f, 0.25f, 0.25f),   // trên dưới
                        new Color(1.00f, 0.60f, 0.60f),   // dưới trái
                        new Color(1.00f, 0.60f, 0.60f));  // dưới dưới
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
            }
        }

        void HandleJudged(Judgement j, int points, bool isHit)
        {
            score += points;
            combo = isHit ? combo + 1 : 0;
            maxCombo = Mathf.Max(maxCombo, combo);
            UpdateScoreUI();
            UpdateGrade();

            // Chữ PERFECT/GOOD/BAD/MISS giờ do JudgementPopup lo (xem VfxBinder).
        }

        void UpdateScoreUI()
        {
            scoreText.text = "Score: " + score;
            comboText.text = "Combo: " + combo + " (max " + maxCombo + ")";
        }

        /// <summary>
        /// Tính lại grade từ điểm hiện tại. Chart có thể null nên mọi thứ đều
        /// null-safe; thiếu gradeText thì hàm chỉ thoát sớm chứ không lỗi.
        /// </summary>
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

        /// <summary>Tap note ăn 300 điểm; hold note ăn 300 (bấm đầu) + 100 (giữ xong).</summary>
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

                // Setup gradient on first frame of countdown
                if (!countdownText.enableVertexGradient)
                {
                    countdownText.fontStyle = FontStyles.Bold;
                    countdownText.enableVertexGradient = true;
                    countdownText.colorGradient = new VertexGradient(
                        new Color(0.2f, 0.8f, 1f),   // xanh cyan trên
                        new Color(0.2f, 0.8f, 1f),
                        new Color(0.0f, 0.5f, 1f),   // xanh đậm dưới
                        new Color(0.0f, 0.5f, 1f));
                }

                // Pop animation when number changes
                if (countdownPopOnChange && countdownText != null && num != lastCountdownNumber)
                {
                    lastCountdownNumber = num;
                    countdownPopTimer = 0f;
                }
            }
            else
            {
                // Reset when not counting
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
