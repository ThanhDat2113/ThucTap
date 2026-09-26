using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Biết về chart và đồng hồ bài hát, chịu trách nhiệm spawn note đúng lúc,
    /// cập nhật vị trí mỗi frame, và dọn note đã bay qua màn hình.
    /// Không biết gì về input hay judgement (JudgementSystem sẽ hỏi nó
    /// "note gần nhất trong lane X là note nào").
    /// </summary>
    public class NoteSpawner : MonoBehaviour
    {
        [Header("Refs")]
        public NotePool pool;
        public Transform[] laneAnchors;
        public Color[] laneColors;

        [Header("Layout (kéo Transform để chỉnh trực quan trong Scene view)")]
        [Tooltip("Kéo Transform của receptor vào đây để lấy vị trí Y trực quan. Để trống thì dùng hitLineY bên dưới.")]
        public Transform hitLineReference;
        public float hitLineY = -3.3f;
        [Tooltip("Kéo 1 Transform đặt ở mép trên màn hình vào đây. Để trống thì dùng spawnY bên dưới.")]
        public Transform spawnLineReference;
        public float spawnY = 5.5f;
        public float scrollSpeed = 7f;

        float HitLineY => hitLineReference != null ? hitLineReference.position.y : hitLineY;
        float SpawnY => spawnLineReference != null ? spawnLineReference.position.y : spawnY;

        /// <summary>Note cần bao lâu để rơi từ điểm spawn xuống vạch bấm.</summary>
        public float TravelTime => (SpawnY - HitLineY) / scrollSpeed;

        ChartData chart;
        int nextIndex;
        readonly List<NoteView> active = new List<NoteView>();
        public IReadOnlyList<NoteView> ActiveNotes => active;

        public bool Finished => chart != null && nextIndex >= chart.notes.Count && active.Count == 0;

        public void Begin(ChartData chartData)
        {
            foreach (var n in active) pool.Release(n);
            active.Clear();

            chart = chartData;
            nextIndex = 0;
        }

        public void Tick(float songTime)
        {
            while (nextIndex < chart.notes.Count && chart.notes[nextIndex].time - songTime <= TravelTime)
                Spawn(chart.notes[nextIndex++]);

            float killY = HitLineY - (SpawnY - HitLineY) - 2f;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                NoteView n = active[i];
                float laneX = laneAnchors[n.Data.lane].position.x;
                n.UpdatePosition(songTime, laneX, HitLineY, scrollSpeed);

                if (n.transform.position.y < killY)
                {
                    active.RemoveAt(i);
                    pool.Release(n);
                }
            }
        }

        void Spawn(NoteData data)
        {
            NoteView n = pool.Get();
            n.Setup(data, laneColors[data.lane % laneColors.Length]);
            n.transform.position = new Vector3(laneAnchors[data.lane].position.x, SpawnY, 0f);
            active.Add(n);
        }

        /// <summary>Tìm note chưa xử lý, gần thời điểm hiện tại nhất, trong 1 lane.</summary>
        public NoteView FindClosestUnjudged(int lane, float songTime)
        {
            NoteView best = null;
            float bestDiff = float.MaxValue;
            foreach (var n in active)
            {
                if (n.Judged || n.Data.lane != lane) continue;
                float diff = Mathf.Abs(songTime - n.Data.time);
                if (diff < bestDiff) { bestDiff = diff; best = n; }
            }
            return best;
        }

        public void ReleaseNote(NoteView n)
        {
            active.Remove(n);
            pool.Release(n);
        }
    }
}
