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

        [Header("Battle Settings")]
        [Range(0f, 1f)] public float opponentNoteRatio = 0.5f;
        [Range(0f, 1f)] public float opponentAccuracy = 0.85f;
        [Tooltip("Chia bài theo khu vực: 0 = random, 1 = nửa đầu player/nửa sau opponent, 2 = xen kẽ đoạn")]
        public int battleSplitMode = 1;
        [Tooltip("Độ dài mỗi đoạn khi split mode = 2 (số phách)")]
        public float sectionLengthBeats = 8f;

        public List<NoteData> notes = new List<NoteData>();

        public float SecondsPerBeat => 60f / bpm;

        public float BeatToTime(float beat) => firstNoteOffset + beat * SecondsPerBeat;

        public void AddNoteAtBeat(float beat, int lane, NoteType type = NoteType.Tap, float holdBeats = 0f, NoteOwner owner = NoteOwner.Player)
        {
            notes.Add(new NoteData(beat, lane, type, holdBeats, owner));
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

                NoteOwner owner = rng.NextDouble() < opponentNoteRatio ? NoteOwner.Opponent : NoteOwner.Player;

                if (rng.NextDouble() < 0.2)
                {
                    float holdBeats = rng.NextDouble() < 0.5 ? 1f : 2f;
                    AddNoteAtBeat(beat, lane, NoteType.Hold, holdBeats, owner);
                    beat += holdBeats + 0.5f;
                }
                else
                {
                    AddNoteAtBeat(beat, lane, NoteType.Tap, 0f, owner);
                    beat += rng.NextDouble() < 0.3 ? 0.5f : 1f;
                }
            }
        }

        [ContextMenu("Generate Battle Chart")]
        public void GenerateBattleChart()
        {
            notes.Clear();
            var rng = new System.Random(1);
            int prev = -1;
            float beat = 0f;
            float totalBeats = 50f; // ước lượng

            for (int i = 0; i < 50; i++)
            {
                int lane;
                do { lane = rng.Next(0, laneCount); } while (lane == prev);
                prev = lane;

                NoteOwner owner = GetOwnerForBeat(beat, totalBeats, rng);

                if (rng.NextDouble() < 0.2)
                {
                    float holdBeats = rng.NextDouble() < 0.5 ? 1f : 2f;
                    AddNoteAtBeat(beat, lane, NoteType.Hold, holdBeats, owner);
                    beat += holdBeats + 0.5f;
                }
                else
                {
                    AddNoteAtBeat(beat, lane, NoteType.Tap, 0f, owner);
                    beat += rng.NextDouble() < 0.3 ? 0.5f : 1f;
                }
            }
        }

        NoteOwner GetOwnerForBeat(float beat, float totalBeats, System.Random rng)
        {
            switch (battleSplitMode)
            {
                case 0: // Random
                    return rng.NextDouble() < opponentNoteRatio ? NoteOwner.Opponent : NoteOwner.Player;

                case 1: // Half & Half
                    return beat < totalBeats * 0.5f ? NoteOwner.Player : NoteOwner.Opponent;

                case 2: // Alternating sections
                    int sectionIndex = Mathf.FloorToInt(beat / sectionLengthBeats);
                    return (sectionIndex % 2 == 0) ? NoteOwner.Player : NoteOwner.Opponent;

                default:
                    return NoteOwner.Player;
            }
        }

        [ContextMenu("Convert to Battle (Half Split)")]
        public void ConvertToBattleHalfSplit()
        {
            if (notes.Count == 0) return;

            float lastBeat = notes[notes.Count - 1].beat;
            float midBeat = lastBeat * 0.5f;

            foreach (var note in notes)
            {
                note.owner = note.beat < midBeat ? NoteOwner.Player : NoteOwner.Opponent;
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [ContextMenu("Convert to Battle (Alternating Sections)")]
        public void ConvertToBattleAlternating()
        {
            if (notes.Count == 0) return;

            foreach (var note in notes)
            {
                int sectionIndex = Mathf.FloorToInt(note.beat / sectionLengthBeats);
                note.owner = (sectionIndex % 2 == 0) ? NoteOwner.Player : NoteOwner.Opponent;
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        [ContextMenu("Balance Note Count")]
        public void BalanceNoteCount()
        {
            int playerCount = 0, opponentCount = 0;
            foreach (var note in notes)
            {
                if (note.owner == NoteOwner.Player) playerCount++;
                else opponentCount++;
            }

            int diff = Mathf.Abs(playerCount - opponentCount);
            if (diff < 2) return;

            // Swap some notes to balance
            var sortedNotes = new List<NoteData>(notes);
            sortedNotes.Sort((a, b) => a.beat.CompareTo(b.beat));

            int toSwap = diff / 2;
            int swapped = 0;
            NoteOwner majority = playerCount > opponentCount ? NoteOwner.Player : NoteOwner.Opponent;

            for (int i = 0; i < sortedNotes.Count && swapped < toSwap; i++)
            {
                if (sortedNotes[i].owner == majority)
                {
                    sortedNotes[i].owner = majority == NoteOwner.Player ? NoteOwner.Opponent : NoteOwner.Player;
                    swapped++;
                }
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}