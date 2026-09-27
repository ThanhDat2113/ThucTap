using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    [CreateAssetMenu(fileName = "NewChart", menuName = "RhythmGame/Chart Data")]
    public class ChartData : ScriptableObject
    {
        public string songName = "Untitled";
        [Header("Audio")]
        public AudioClip audioClip;
        public float musicStartDelay = 0f;
        public float bpm = 120f;
        public int laneCount = 4;
        public float firstNoteOffset = 0.5f;

        public List<NoteData> notes = new List<NoteData>();

        public float SecondsPerBeat => 60f / bpm;

        public float BeatToTime(float beat) => firstNoteOffset + beat * SecondsPerBeat;

        public void AddNoteAtBeat(float beat, int lane, NoteType type = NoteType.Tap, float holdBeats = 0f)
        {
            notes.Add(new NoteData(beat, lane, type, holdBeats));
        }

        [ContextMenu("Generate Test Chart")]
        public void GenerateTestChart()
        {
            notes.Clear();
            var rng = new System.Random(1);
            int prev = -1;
            float beat = 0f;

            for (int i = 0; i < 50; i++)
            {
                int lane;
                do { lane = rng.Next(0, laneCount); } while (lane == prev);
                prev = lane;

                AddNoteAtBeat(beat, lane);
                beat += rng.NextDouble() < 0.3 ? 0.5f : 1f;
            }
        }
    }
}