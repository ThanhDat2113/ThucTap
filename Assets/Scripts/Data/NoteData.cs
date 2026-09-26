namespace RhythmGame
{
    public enum NoteType { Tap, Hold }

    /// <summary>
    /// Dữ liệu thuần (không phải MonoBehaviour) mô tả 1 note trong chart.
    /// Tách khỏi mọi thứ liên quan tới hiển thị/Unity object để sau này
    /// dễ dàng đọc/ghi từ JSON, MIDI, hoặc chỉnh sửa bằng editor riêng.
    /// </summary>
    [System.Serializable]
    public class NoteData
    {
        public float time;              // thời điểm phải bấm, tính bằng giây từ đầu bài
        public int lane;                // 0..laneCount-1
        public NoteType type = NoteType.Tap;
        public float holdDuration = 0f; // dùng cho note giữ (hold), bỏ qua nếu là Tap

        public NoteData() { }

        public NoteData(float time, int lane, NoteType type = NoteType.Tap, float holdDuration = 0f)
        {
            this.time = time;
            this.lane = lane;
            this.type = type;
            this.holdDuration = holdDuration;
        }
    }
}
