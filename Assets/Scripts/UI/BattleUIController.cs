using UnityEngine;
using TMPro;

namespace RhythmGame
{
    public class BattleUIController : MonoBehaviour
    {
        [Header("Refs")]
        public GameManager gameManager;
        public JudgementSystem judgement;

        [Header("Player UI")]
        public TMP_Text playerScoreText;
        public TMP_Text playerComboText;
        public TMP_Text playerGradeText;
        public HealthBar playerHealthBar;

        [Header("Opponent UI")]
        public TMP_Text opponentScoreText;
        public TMP_Text opponentComboText;
        public TMP_Text opponentGradeText;
        public HealthBar opponentHealthBar;

        [Header("Shared UI")]
        public TMP_Text countdownText;
        public TMP_Text messageText;

        [Header("Turn Results")]
        public TurnResultPanel turnResultPanel;

        [Header("Grade Thresholds")]
        [Range(0f, 1f)] public float thresholdS = 0.95f;
        [Range(0f, 1f)] public float thresholdA = 0.90f;
        [Range(0f, 1f)] public float thresholdB = 0.80f;
        [Range(0f, 1f)] public float thresholdC = 0.70f;
        [Range(0f, 1f)] public float thresholdD = 0.60f;
        [Range(0f, 1f)] public float thresholdE = 0.50f;

        int playerScore, playerCombo, playerMaxCombo;
        int opponentScore, opponentCombo, opponentMaxCombo;
        Grade playerGrade = Grade.F, opponentGrade = Grade.F;
        int playerMaxScore, opponentMaxScore;

        void Start()
        {
            gameManager.OnStateChanged += HandleStateChanged;
            judgement.OnJudged += HandleJudged;

            if (gameManager.chart != null)
            {
                CalculateMaxScores(gameManager.chart);
            }
        }

        void OnDestroy()
        {
            if (gameManager != null) gameManager.OnStateChanged -= HandleStateChanged;
            if (judgement != null) judgement.OnJudged -= HandleJudged;
        }

        void CalculateMaxScores(ChartData chart)
        {
            playerMaxScore = 0;
            opponentMaxScore = 0;
            foreach (var note in chart.notes)
            {
                int noteMaxScore = note.type == NoteType.Hold ? 400 : 300;
                if (note.owner == NoteOwner.Player)
                    playerMaxScore += noteMaxScore;
                else
                    opponentMaxScore += noteMaxScore;
            }
        }

        void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Finished:
                    messageText.text = "THẮNG! Nhấn R để chơi lại";
                    messageText.color = Color.green;
                    break;
                case GameState.Failed:
                    messageText.text = "THUA! Nhấn R để chơi lại";
                    messageText.color = Color.red;
                    break;
                default:
                    messageText.text = "";
                    messageText.color = Color.white;
                    break;
            }

            if (state == GameState.Playing)
            {
                playerScore = playerCombo = playerMaxCombo = 0;
                opponentScore = opponentCombo = opponentMaxCombo = 0;
                playerGrade = opponentGrade = Grade.F;
                UpdatePlayerUI();
                UpdateOpponentUI();
            }
        }

        void HandleJudged(Judgement j, int points, bool isHit, NoteOwner owner)
        {
            if (owner == NoteOwner.Player)
            {
                if (isHit && points > 0)
                {
                    playerScore += points;
                    playerCombo = isHit ? playerCombo + 1 : 0;
                    playerMaxCombo = Mathf.Max(playerMaxCombo, playerCombo);
                }
                else if (!isHit)
                {
                    playerCombo = 0;
                }
                UpdatePlayerUI();
            }
            else
            {
                if (isHit && points > 0)
                {
                    opponentScore += points;
                    opponentCombo = isHit ? opponentCombo + 1 : 0;
                    opponentMaxCombo = Mathf.Max(opponentMaxCombo, opponentCombo);
                }
                else if (!isHit)
                {
                    opponentCombo = 0;
                }
                UpdateOpponentUI();
            }
            
            // Record for final result panel (combined stats)
            turnResultPanel?.RecordJudgement(owner, j);
            int combo = owner == NoteOwner.Player ? playerCombo : opponentCombo;
            int maxCombo = owner == NoteOwner.Player ? playerMaxCombo : opponentMaxCombo;
            turnResultPanel?.RecordScore(points, combo, maxCombo);
        }

        void UpdatePlayerUI()
        {
            if (playerScoreText) playerScoreText.text = "P1: " + playerScore;
            if (playerComboText) playerComboText.text = "Combo: " + playerCombo + " (max " + playerMaxCombo + ")";
            UpdateGradeText(playerGradeText, playerScore, playerMaxScore);
        }

        void UpdateOpponentUI()
        {
            if (opponentScoreText) opponentScoreText.text = "P2: " + opponentScore;
            if (opponentComboText) opponentComboText.text = "Combo: " + opponentCombo + " (max " + opponentMaxCombo + ")";
            UpdateGradeText(opponentGradeText, opponentScore, opponentMaxScore);
        }

        void UpdateGradeText(TMP_Text gradeText, int score, int maxScore)
        {
            if (gradeText == null) return;
            float percentage = maxScore > 0 ? (float)score / maxScore : 0f;
            Grade grade = CalculateGrade(percentage);
            gradeText.text = grade.ToString();
            gradeText.color = GetGradeColor(grade);
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
            if (countdownText) countdownText.gameObject.SetActive(counting);
            if (counting && countdownText)
            {
                int num = Mathf.CeilToInt(gameManager.CountdownRemaining);
                countdownText.text = num.ToString();
            }
        }

        public void AddOpponentScore(int points, bool isHit)
        {
            opponentScore += points;
            opponentCombo = isHit ? opponentCombo + 1 : 0;
            opponentMaxCombo = Mathf.Max(opponentMaxCombo, opponentCombo);
            UpdateOpponentUI();
        }

        public void SetOpponentMiss()
        {
            opponentCombo = 0;
            UpdateOpponentUI();
        }
    }
}