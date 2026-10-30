namespace RhythmGame
{
    public enum NoteType { Tap, Hold }
    public enum NoteOwner { Player, Opponent }

    [System.Serializable]
    public class NoteData
    {
        public float beat;
        public int lane;
        public NoteType type = NoteType.Tap;
        public float holdBeats = 0f;
        public NoteOwner owner = NoteOwner.Player;

        public NoteData() { }

        public NoteData(float beat, int lane, NoteType type = NoteType.Tap, float holdBeats = 0f, NoteOwner owner = NoteOwner.Player)
        {
            this.beat = beat;
            this.lane = lane;
            this.type = type;
            this.holdBeats = holdBeats;
            this.owner = owner;
        }
    }
}