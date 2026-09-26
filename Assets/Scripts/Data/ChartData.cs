using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Chart của 1 bài hát, lưu dưới dạng asset (.asset) trong project.
    /// Sau này bạn có thể viết thêm 1 importer đọc JSON/MIDI rồi
    /// tạo ra ChartData bằng code, hoặc chỉnh tay trực tiếp trong Inspector.
    /// Chuột phải trong Project window -> Create -> RhythmGame -> Chart Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewChart", menuName = "RhythmGame/Chart Data")]
    public class ChartData : ScriptableObject
    {
        public string songName = "Untitled";
        public AudioClip audioClip;         // để trống ở giai đoạn prototype, gắn sau khi có nhạc
        public float bpm = 120f;
        public int laneCount = 4;
        public float firstNoteOffset = 0.5f;

        public List<NoteData> notes = new List<NoteData>();

        /// <summary>Sinh chart ngẫu nhiên để test khi chưa có chart thật.</summary>
        [ContextMenu("Generate Test Chart")]
        public void GenerateTestChart()
        {
            notes.Clear();
            var rng = new System.Random(1);
            float beat = 60f / bpm;
            float t = firstNoteOffset;
            int prev = -1;

            for (int i = 0; i < 50; i++)
            {
                int lane;
                do { lane = rng.Next(0, laneCount); } while (lane == prev);
                prev = lane;

                notes.Add(new NoteData(t, lane));
                t += rng.NextDouble() < 0.3 ? beat * 0.5f : beat;
            }
        }
    }
}
