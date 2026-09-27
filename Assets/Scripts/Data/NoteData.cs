namespace RhythmGame
{
    public enum NoteType { Tap, Hold }

    [System.Serializable]
    public class NoteData
    {
        public float beat;
        public int lane;
        public NoteType type = NoteType.Tap;
        public float holdBeats = 0f;

        public NoteData() { }

        public NoteData(float beat, int lane, NoteType type = NoteType.Tap, float holdBeats = 0f)
        {
            this.beat = beat;
            this.lane = lane;
            this.type = type;
            this.holdBeats = holdBeats;
        }
    }
}