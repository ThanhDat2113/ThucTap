using UnityEngine;

namespace RhythmGame
{
    public enum Judgement { Perfect, Good, Bad, Miss }

    /// <summary>
    /// Nghe input, hỏi NoteSpawner note nào gần nhất trong lane vừa bấm,
    /// so lệch thời gian rồi phát ra kết quả qua event OnJudged.
    /// Không tự vẽ UI, không tự cộng điểm — UIController lo phần đó.
    /// </summary>
    public class JudgementSystem : MonoBehaviour
    {
        [Header("Refs")]
        public NoteSpawner spawner;
        public InputHandler input;

        [Header("Timing window (giây)")]
        public float perfectWindow = 0.045f;
        public float goodWindow = 0.09f;
        public float missWindow = 0.14f;

        public System.Action<Judgement, int, bool> OnJudged; // (loại, điểm, có phải cú hit)
        public System.Action<int> OnLaneActivated;           // báo cho UI/receptor biết lane vừa được bấm

        float songTime;

        void Start()
        {
            input.OnLanePressed += HandlePress;
        }

        void OnDestroy()
        {
            if (input != null) input.OnLanePressed -= HandlePress;
        }

        public void SetSongTime(float t) => songTime = t;

        /// <summary>Gọi mỗi frame từ GameManager để phát hiện note bị bấm trễ quá mốc.</summary>
        public void CheckMisses()
        {
            foreach (var n in spawner.ActiveNotes)
            {
                if (n.Judged) continue;
                if (songTime - n.Data.time > missWindow)
                {
                    n.MarkJudged();
                    n.PlayMissVisual();
                    OnJudged?.Invoke(Judgement.Miss, 0, false);
                }
            }
        }

        void HandlePress(int lane)
        {
            OnLaneActivated?.Invoke(lane);

            NoteView note = spawner.FindClosestUnjudged(lane, songTime);
            if (note == null) return;

            float diff = Mathf.Abs(songTime - note.Data.time);
            if (diff > missWindow) return; // bấm quá xa mốc: ghost tap, không phạt không thưởng

            note.MarkJudged();
            spawner.ReleaseNote(note);

            if (diff <= perfectWindow) OnJudged?.Invoke(Judgement.Perfect, 300, true);
            else if (diff <= goodWindow) OnJudged?.Invoke(Judgement.Good, 100, true);
            else OnJudged?.Invoke(Judgement.Bad, 50, true);
        }
    }
}
