using RhythmGame.Core;
using UnityEngine;

namespace RhythmGame.Scoring
{
    [System.Serializable]
    public class TimingWindow
    {
        public float perfectSeconds = 0.1f;
        public float goodSeconds = 0.2f;

        public Judgement Judge(float offset)
        {
            float abs = Mathf.Abs(offset);
            if (abs <= perfectSeconds)
                return Judgement.Perfect;
            if (abs <= goodSeconds)
                return Judgement.Good;
            return Judgement.Miss;
        }

        public static readonly TimingWindow Default = new TimingWindow
        {
            perfectSeconds = 0.1f,
            goodSeconds = 0.2f
        };
    }
}
