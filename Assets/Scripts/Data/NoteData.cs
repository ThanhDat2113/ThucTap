using RhythmGame.Core;
using UnityEngine;

namespace RhythmGame.Data
{
    [System.Serializable]
    public class NoteData
    {
        public float hitTime;
        public LaneID lane;
        public NoteType type = NoteType.Normal;
        public float holdDuration;

        public bool IsHold => type == NoteType.Hold;
        public float GetTailTime => hitTime + Mathf.Max(0f, holdDuration);
    }
}
