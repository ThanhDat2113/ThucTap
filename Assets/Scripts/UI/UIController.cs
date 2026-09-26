using UnityEngine;
using TMPro;

namespace RhythmGame
{
    /// <summary>
    /// Chỉ đọc dữ liệu từ GameManager/JudgementSystem qua event rồi cập nhật
    /// TextMeshPro. Không tự tính điểm, không tự biết luật chấm điểm.
    /// Dùng kiểu TMP_Text (lớp cơ sở) để field này nhận được cả
    /// TextMeshProUGUI (trong Canvas) lẫn TextMeshPro (3D, nếu sau này cần).
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Header("Refs")]
        public GameManager gameManager;
        public JudgementSystem judgement;

        [Header("UI Elements (kéo thả trong Inspector)")]
        public TMP_Text countdownText;
        public TMP_Text judgeText;
        public TMP_Text scoreText;
        public TMP_Text comboText;
        public TMP_Text messageText;

        int score, combo, maxCombo;
        float judgeTimer;

        void Start()
        {
            gameManager.OnStateChanged += HandleStateChanged;
            judgement.OnJudged += HandleJudged;
        }

        void OnDestroy()
        {
            if (gameManager != null) gameManager.OnStateChanged -= HandleStateChanged;
            if (judgement != null) judgement.OnJudged -= HandleJudged;
        }

        void HandleStateChanged(GameState state)
        {
            messageText.text = state == GameState.Finished ? "XONG! Nhan R de choi lai" : "";
            if (state == GameState.Playing)
            {
                score = combo = maxCombo = 0;
                UpdateScoreUI();
            }
        }

        void HandleJudged(Judgement j, int points, bool isHit)
        {
            score += points;
            combo = isHit ? combo + 1 : 0;
            maxCombo = Mathf.Max(maxCombo, combo);
            UpdateScoreUI();

            judgeText.text = j.ToString().ToUpper();
            judgeText.color = j switch
            {
                Judgement.Perfect => Color.yellow,
                Judgement.Good => Color.green,
                Judgement.Bad => new Color(1f, 0.6f, 0.2f),
                _ => Color.red
            };
            judgeTimer = 0.5f;
        }

        void UpdateScoreUI()
        {
            scoreText.text = "Score: " + score;
            comboText.text = "Combo: " + combo + " (max " + maxCombo + ")";
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
