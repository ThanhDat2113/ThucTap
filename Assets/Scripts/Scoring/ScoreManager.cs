using RhythmGame.Core;
using UnityEngine;

namespace RhythmGame.Scoring
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int MissCount { get; private set; }

        public float Accuracy { get; private set; } = 1f;

        private int totalHits;
        private int scoreStep = 0;
        private const int MaxScoreWeight = 10000;

        public delegate void HitEvent(Judgement judgement, float offset);
        public event HitEvent OnJudged;

        public delegate void ComboBrokenEvent();
        public event ComboBrokenEvent OnComboBroken;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void ApplyHit(Judgement judgement, float offset)
        {
            totalHits++;

            switch (judgement)
            {
                case Judgement.Perfect:
                    PerfectCount++;
                    scoreStep = 2;
                    AddScore(350);
                    AddAccuracy(1f);
                    break;
                case Judgement.Good:
                    GoodCount++;
                    scoreStep = 1;
                    AddScore(200);
                    AddAccuracy(0.7f);
                    break;
                case Judgement.Miss:
                    MissCount++;
                    scoreStep = 0;
                    AddScore(0);
                    AddAccuracy(0f);
                    break;
            }

            if (judgement == Judgement.Miss)
            {
                Combo = 0;
                OnComboBroken?.Invoke();
            }
            else
            {
                Combo++;
                if (Combo > MaxCombo)
                    MaxCombo = Combo;
            }

            OnJudged?.Invoke(judgement, offset);
        }

        private void AddScore(int baseAmount)
        {
            int computed = baseAmount + (scoreStep * 25) * 4;
            int capped = Mathf.Min(computed, MaxScoreWeight);
            Score += capped;
        }

        private void AddAccuracy(float weight)
        {
            Accuracy = ((Accuracy * (totalHits - 1)) + weight) / totalHits;
            Accuracy = Mathf.Clamp01(Accuracy);
        }

        public void ResetScore()
        {
            Score = 0;
            Combo = 0;
            MaxCombo = 0;
            PerfectCount = 0;
            GoodCount = 0;
            MissCount = 0;
            Accuracy = 1f;
            totalHits = 0;
            scoreStep = 0;
        }
    }
}
