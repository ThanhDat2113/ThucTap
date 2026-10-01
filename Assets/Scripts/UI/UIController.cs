using UnityEngine;
using TMPro;

namespace RhythmGame
{
    /// <summary>
    /// Chỉ đọc dữ liệu từ GameManager/JudgementSystem qua event rồi cập nhật
    /// TextMeshPro. Không tự tính điểm, không tự biết luật chấm điểm.
    ///
    /// GHI CHÚ: phần hiển thị chữ "PERFECT/GOOD/BAD/MISS" đã chuyển sang cho
    /// JudgementPopup (gắn trên object JudgeText) phụ trách toàn bộ animation.
    /// UIController ở đây chỉ còn lo Score, Combo, Countdown, Message — không
    /// đụng vào judgeText nữa để tránh 2 script cùng ghi đè lên 1 Text.
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

        int score, combo, maxCombo;

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

            // Chữ PERFECT/GOOD/BAD/MISS giờ do JudgementPopup lo (xem VfxBinder).
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
        }
    }
}