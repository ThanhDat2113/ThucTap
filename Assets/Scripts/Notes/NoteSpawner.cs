using System;
using System.Collections.Generic;
using RhythmGame.Core;
using RhythmGame.Data;
using RhythmGame.Scoring;
using UnityEngine;

namespace RhythmGame.Notes
{
    [DisallowMultipleComponent]
    public class NoteSpawner : MonoBehaviour
    {
        public static NoteSpawner Instance { get; private set; }

        [Header("Configuration")]
        public GameConfig config;
        public TimingWindow timingWindow;

        [Header("References (auto-wired if left empty)")]
        public RhythmGame.Input.PlayerInputHandler inputHandler;

        public Transform noteContainer;
        public Note notePrefab;

        public event Action<Judgement, Vector3> HitFeedback;
        private SongData song;
        private NoteData[] allNotes;
        private readonly List<Note> activeNotes = new List<Note>();
        private readonly List<ActiveHold> activeHolds = new List<ActiveHold>();
        private int spawnIndex;
        private bool isRunning;

        private struct ActiveHold
        {
            public Note note;
            public LaneID lane;
            public float tailTime;
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (config == null)
                config = GameManager.Instance?.Config;
            if (timingWindow == null)
                timingWindow = TimingWindow.Default;
            if (inputHandler == null)
                inputHandler = GameManager.Instance?.InputHandler;
            if (inputHandler == null)
                inputHandler = FindAnyObjectByType<RhythmGame.Input.PlayerInputHandler>();
            if (noteContainer == null)
            {
                var go = new GameObject("Notes");
                noteContainer = go.transform;
            }

            if (inputHandler != null)
            {
                inputHandler.OnLanePressed += HandleLanePressed;
                inputHandler.OnLaneReleased += HandleLaneReleased;
            }
        }

        public void Begin(SongData songData)
        {
            song = songData;
            allNotes = songData != null && songData.notes != null ? songData.notes : new NoteData[0];
            Array.Sort(allNotes, (a, b) => a.hitTime.CompareTo(b.hitTime));
            activeNotes.Clear();
            activeHolds.Clear();
            spawnIndex = 0;
            isRunning = true;
        }

        public void Stop()
        {
            isRunning = false;
        }

        private void Update()
        {
            if (!isRunning)
                return;

            SpawnDueNotes();
            UpdateHolds();
        }

        private void SpawnDueNotes()
        {
            if (Conductor.Instance == null || config == null || song == null)
                return;

            float songPos = Conductor.Instance.CurrentSongPosition;
            float lead = config.spawnLeadTime;

            while (spawnIndex < allNotes.Length && allNotes[spawnIndex].hitTime <= songPos + lead)
            {
                SpawnNote(allNotes[spawnIndex]);
                spawnIndex++;
            }
        }

        private void SpawnNote(NoteData data)
        {
            Note instance;
            if (notePrefab != null)
            {
                instance = Instantiate(notePrefab, noteContainer);
            }
            else
            {
                var go = new GameObject($"Note_{data.lane}_{data.hitTime:F3}");
                instance = go.AddComponent<Note>();
                instance.transform.SetParent(noteContainer, true);
            }

            instance.Setup(data, config);
            instance.OnMissed += HandleNoteMissed;
            activeNotes.Add(instance);
        }

        private void HandleLanePressed(LaneID lane)
        {
            if (!isRunning || Conductor.Instance == null)
                return;

            float pressTime = Conductor.Instance.CurrentSongPosition;
            TryHitNote(lane, pressTime);
        }

        private void TryHitNote(LaneID lane, float pressTime)
        {
            if (config == null)
                return;

            var window = timingWindow ?? TimingWindow.Default;

            Note best = null;
            float bestOffset = 0f;
            float bestDist = float.PositiveInfinity;

            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                var n = activeNotes[i];
                if (n.Lane != lane || n.IsMissed)
                    continue;
                if (n.Type == NoteType.Hold && n.IsHoldHeadHit)
                    continue;

                float offset = pressTime - n.HitTime;
                float dist = Mathf.Abs(offset);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = n;
                    bestOffset = offset;
                }
            }

            if (best == null || bestDist > config.goodWindow)
                return;

            var judgement = window.Judge(bestOffset);

            if (best.Type == NoteType.Hold)
                StartHold(best);

            best.ApplyHeadJudgement(judgement, bestOffset);
            ScoreManager.Instance?.ApplyHit(judgement, bestOffset);
            SpawnHitEffect(best.Lane, judgement);

            if (best.Type == NoteType.Normal)
                activeNotes.Remove(best);
        }

        private void StartHold(Note note)
        {
            activeHolds.Add(new ActiveHold
            {
                note = note,
                lane = note.Lane,
                tailTime = note.HitTime + note.HoldDuration
            });
        }

        private void HandleLaneReleased(LaneID lane)
        {
            for (int i = activeHolds.Count - 1; i >= 0; i--)
            {
                if (activeHolds[i].lane != lane || activeHolds[i].note.IsTailCleared)
                    continue;

                var hold = activeHolds[i];
                hold.note.IsTailCleared = true;
                ScoreManager.Instance?.ApplyHit(Judgement.Miss, 0f);
                SpawnHitEffect(hold.note.Lane, Judgement.Miss);
                activeHolds.RemoveAt(i);
                activeNotes.Remove(hold.note);
                Destroy(hold.note.gameObject);
            }
        }

        private void UpdateHolds()
        {
            if (Conductor.Instance == null || config == null || inputHandler == null)
                return;

            float songPos = Conductor.Instance.CurrentSongPosition;

            for (int i = activeHolds.Count - 1; i >= 0; i--)
            {
                var hold = activeHolds[i];
                if (songPos < hold.tailTime)
                    continue;

                bool heldAtTail = inputHandler.IsLaneHeld(hold.lane);
                var judgement = heldAtTail ? Judgement.Perfect : Judgement.Miss;
                hold.note.ClearHoldTail();
                ScoreManager.Instance?.ApplyHit(judgement, songPos - hold.tailTime);
                SpawnHitEffect(hold.lane, judgement);

                activeHolds.RemoveAt(i);
                activeNotes.Remove(hold.note);
                Destroy(hold.note.gameObject);
            }
        }

        private void HandleNoteMissed(Note note)
        {
            activeNotes.Remove(note);
            RemoveHoldForNote(note);
            ScoreManager.Instance?.ApplyHit(Judgement.Miss, 0f);
            SpawnHitEffect(note.Lane, Judgement.Miss);
        }

        private void RemoveHoldForNote(Note note)
        {
            for (int i = activeHolds.Count - 1; i >= 0; i--)
            {
                if (activeHolds[i].note == note)
                    activeHolds.RemoveAt(i);
            }
        }

        private void SpawnHitEffect(LaneID lane, Judgement judgement)
        {
            if (config == null)
                return;
            Vector3 pos = new Vector3(config.GetLaneCenterX(lane), config.hitPositionY, 0f);
            HitFeedback?.Invoke(judgement, pos);
        }

        public void Cleanup()
        {
            foreach (var n in activeNotes)
                if (n) Destroy(n.gameObject);
            activeNotes.Clear();
            activeHolds.Clear();
            allNotes = null;
            spawnIndex = 0;
        }
    }
}
