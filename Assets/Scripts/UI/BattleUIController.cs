using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace RhythmGame
{
    public class BattleUIController : MonoBehaviour
    {
        [Header("Refs")]
        public GameManager gameManager;
        public JudgementSystem judgement;
        public FlowSystem flowSystem;

        [Header("Player UI")]
        public TMP_Text playerScoreText;
        public TMP_Text playerComboText;
        public TMP_Text playerGradeText;
        public HealthBar playerHealthBar;

        [Header("Flow UI")]
        public Image flowBar;
        public TMP_Text flowText;
        public TMP_Text flowStatusText;

        [Header("Opponent UI")]
        public TMP_Text opponentScoreText;
        public TMP_Text opponentComboText;
        public TMP_Text opponentGradeText;
        public HealthBar opponentHealthBar;

        [Header("Opponent Judgement UI")]
        public TMP_Text opponentJudgementText;
        public JudgementPopup opponentJudgementPopup;

        [Header("Shared UI")]
        public TMP_Text countdownText;
        public TMP_Text messageText;

        [Header("Turn Results")]
        public TurnResultPanel turnResultPanel;

        [Header("Grade Thresholds")]
        [Range(0f, 1f)] public float thresholdS = 0.95f;
        [Range(0f, 1f)] public float thresholdA = 0.80f;
        [Range(0f, 1f)] public float thresholdB = 0.60f;
        [Range(0f, 1f)] public float thresholdC = 0.40f;
        [Range(0f, 1f)] public float thresholdD = 0.20f;
        [Range(0f, 1f)] public float thresholdE = 0f;

        public int playerScore, playerCombo, playerMaxCombo;
        public int opponentScore, opponentCombo, opponentMaxCombo;
        Grade playerGrade = Grade.F, opponentGrade = Grade.F;
        int playerMaxScore, opponentMaxScore;
        public int playerTotalNotes, playerHitNotes;
        public int playerPerfect, playerGood;
        public int playerBad, playerMiss;

        public int TotalNoteSlots
        {
            get
            {
                int total = 0;
                if (gameManager != null && gameManager.chart != null)
                {
                    foreach (var note in gameManager.chart.notes)
                    {
                        total += note.type == NoteType.Hold ? 2 : 1;
                    }
                }
                return total;
            }
        }

        public float PlayerHitRatio
        {
            get
            {
                int total = TotalNoteSlots;
                return total > 0 ? (float)playerHitNotes / total : 0f;
            }
        }

        public Grade PlayerGrade
        {
            get
            {
                int total = TotalNoteSlots;
                if (total <= 0) return Grade.E;
                float p = (float)(playerPerfect + playerGood) / total;
                if (p >= thresholdS) return Grade.S;
                if (p >= thresholdA) return Grade.A;
                if (p >= thresholdB) return Grade.B;
                if (p >= thresholdC) return Grade.C;
                if (p >= thresholdD) return Grade.D;
                return Grade.E;
            }
        }

        void Start()
        {
            gameManager.OnStateChanged += HandleStateChanged;
            judgement.OnJudged += HandleJudged;
            if (flowSystem != null)
            {
                flowSystem.OnFlowAmountChanged += HandleFlowAmountChanged;
                flowSystem.OnFlowStateChanged += HandleFlowStateChanged;
            }

            if (gameManager.chart != null)
            {
                CalculateMaxScores(gameManager.chart);
            }
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

        void CalculateMaxScores(ChartData chart)
        {
            playerMaxScore = 0;
            opponentMaxScore = 0;
            foreach (var note in chart.notes)
            {
                int noteMaxScore = note.type == NoteType.Hold ? 400 : 300;
                playerMaxScore += noteMaxScore;
                opponentMaxScore += noteMaxScore;
            }
        }

        void HandleStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Finished:
                    if (messageText != null) { messageText.text = "THẮNG! Nhấn R để chơi lại"; messageText.color = Color.green; }
                    break;
                case GameState.Failed:
                    if (messageText != null) { messageText.text = "THUA! Nhấn R để chơi lại"; messageText.color = Color.red; }
                    break;
                default:
                    if (messageText != null) { messageText.text = ""; messageText.color = Color.white; }
                    break;
            }

            if (state == GameState.Playing)
            {
                playerScore = playerCombo = playerMaxCombo = 0;
                playerTotalNotes = playerHitNotes = playerPerfect = playerGood = playerBad = playerMiss = 0;
                opponentScore = opponentCombo = opponentMaxCombo = 0;
                playerGrade = opponentGrade = Grade.F;
                UpdatePlayerUI();
                UpdateOpponentUI();
                UpdateFlowUI();
            }
        }

        void HandleJudged(Judgement j, int points, bool isHit, NoteOwner owner)
        {
            if (owner != NoteOwner.Player) return;

            if (isHit && points > 0)
            {
                float multiplier = flowSystem != null ? flowSystem.FlowScoreMultiplier : 1f;
                playerScore += Mathf.RoundToInt(points * multiplier);
                playerCombo = isHit ? playerCombo + 1 : 0;
                playerMaxCombo = Mathf.Max(playerMaxCombo, playerCombo);
            }
            else if (!isHit)
            {
                playerCombo = 0;
            }
            playerTotalNotes++;
            if (isHit) playerHitNotes++;
            if (j == Judgement.Perfect) playerPerfect++;
            else if (j == Judgement.Good) playerGood++;
            else if (j == Judgement.HoldComplete && isHit) playerGood++;
            else if (j == Judgement.Bad) playerBad++;
            else if (j == Judgement.Miss) playerMiss++;
            UpdatePlayerUI();
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

        void UpdatePlayerUI()
        {
            if (playerScoreText) playerScoreText.text = "P1: " + playerScore;
            if (playerComboText) playerComboText.text = "Combo: " + playerCombo + " (max " + playerMaxCombo + ")";

            Grade g = PlayerGrade;
            if (playerGradeText != null)
            {
                playerGradeText.text = g.ToString();
                playerGradeText.color = GetGradeColor(g);
            }
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
            return Grade.E;
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

        public void SetOpponentJudgement(Judgement j)
        {
            if (opponentJudgementPopup != null)
            {
                opponentJudgementPopup.Show(j);
                return;
            }

            if (opponentJudgementText == null) return;

            string text;
            Color color;
            switch (j)
            {
                case Judgement.Perfect:
                    text = "PERFECT"; color = Color.yellow; break;
                case Judgement.Good:
                    text = "GOOD"; color = new Color(1f, 0.6f, 0.9f); break;
                case Judgement.Bad:
                    text = "BAD"; color = new Color(0.6f, 1f, 0.8f); break;
                case Judgement.HoldComplete:
                    text = "HOLD!"; color = Color.cyan; break;
                default:
                    text = "MISS"; color = Color.gray; break;
            }

            opponentJudgementText.text = text;
            opponentJudgementText.color = color;
        }
    }
}