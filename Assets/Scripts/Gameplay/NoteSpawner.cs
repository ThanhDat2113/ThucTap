using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    public class NoteSpawner : MonoBehaviour
    {
        [Header("Refs")]
        public NotePool pool;
        public Transform[] laneAnchors;
        public Color[] laneColors;

        [Header("Layout (kéo Transform để chỉnh trực quan trong Scene view)")]
        public Transform hitLineReference;
        public float hitLineY = -3.3f;
        public Transform spawnLineReference;
        public float spawnY = 5.5f;
        public float scrollSpeed = 7f;

        float HitLineY => hitLineReference != null ? hitLineReference.position.y : hitLineY;
        float SpawnY => spawnLineReference != null ? spawnLineReference.position.y : spawnY;

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
            while (nextIndex < chart.notes.Count &&
                   chart.BeatToTime(chart.notes[nextIndex].beat) - songTime <= TravelTime)
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
            float timeSeconds = chart.BeatToTime(data.beat);
            n.Setup(data, timeSeconds, laneColors[data.lane % laneColors.Length]);
            n.transform.position = new Vector3(laneAnchors[data.lane].position.x, SpawnY, 0f);
            active.Add(n);
        }

        public NoteView FindClosestUnjudged(int lane, float songTime)
        {
            NoteView best = null;
            float bestDiff = float.MaxValue;
            foreach (var n in active)
            {
                if (n.Judged || n.Data.lane != lane) continue;
                float diff = Mathf.Abs(songTime - n.TimeSeconds);
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