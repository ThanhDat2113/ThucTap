using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    public class OpponentAI : MonoBehaviour
    {
        struct PendingNote
        {
            public float startTime;
            public float tailTime;
            public bool isHold;
            public bool started;
            public bool hit;
            public Judgement headJudgement;
        }


        [Header("Refs")]
        public NoteSpawner spawner;
        public JudgementSystem judgementSystem;
        public CharacterController characterController;
        public BattleUIController battleUI;

        [Header("AI Settings")]
        [Range(0f, 1f)] public float accuracy = 0.85f;
        [Tooltip("Độ trễ phản ứng (giây). Âm = gõ sớm, dương = gõ trễ")]
        public float reactionTime = 0f;
        [Tooltip("Tỷ lệ Perfect khi gõ trúng (0-1)")]
        [Range(0f, 1f)] public float perfectRate = 0.6f;
        [Tooltip("Tỷ lệ Good khi gõ trúng (0-1)")]
        [Range(0f, 1f)] public float goodRate = 0.3f;
        [Tooltip("Tỷ lệ Bad khi gõ trúng (0-1)")]
        [Range(0f, 1f)] public float badRate = 0.1f;

        [Header("Visual Feedback")]
        public bool showTapEffect = true;

        float songTime;
        bool isActive;
        readonly List<PendingNote> pendingNotes = new List<PendingNote>();

        void Start()
        {
            if (judgementSystem != null)
            {
                judgementSystem.OnJudged += OnJudged;
            }
            if (spawner != null)
            {
                spawner.OnNoteSpawned += HandleNoteSpawned;
            }
        }

        void OnDestroy()
        {
            if (judgementSystem != null) judgementSystem.OnJudged -= OnJudged;
            if (spawner != null) spawner.OnNoteSpawned -= HandleNoteSpawned;
        }

        public void SetSongTime(float t) => songTime = t;
        public void SetActive(bool active) => isActive = active;

        void Update()
        {
            if (!isActive || spawner == null || judgementSystem == null) return;

            for (int i = pendingNotes.Count - 1; i >= 0; i--)
            {
                var note = pendingNotes[i];

                if (!note.started)
                {
                    float diff = songTime - note.startTime - reactionTime;

                    if (diff > judgementSystem.missWindow)
                    {
                        pendingNotes.RemoveAt(i);
                        MissAtHead();
                    }
                    else if (diff >= -judgementSystem.missWindow && diff <= judgementSystem.missWindow)
                    {
                        note.started = true;

                        if (Random.value > accuracy)
                        {
                            pendingNotes.RemoveAt(i);
                            MissAtHead();
                        }
                        else
                        {
                            Judgement judgement;
                            float absDiff = Mathf.Abs(diff);

                            if (absDiff <= judgementSystem.perfectWindow)
                            {
                                float r = Random.value;
                                if (r < perfectRate) judgement = Judgement.Perfect;
                                else if (r < perfectRate + goodRate) judgement = Judgement.Good;
                                else judgement = Judgement.Bad;
                            }
                            else if (absDiff <= judgementSystem.goodWindow)
                            {
                                float r = Random.value;
                                if (r < goodRate / Mathf.Max(0.0001f, goodRate + badRate)) judgement = Judgement.Good;
                                else judgement = Judgement.Bad;
                            }
                            else
                            {
                                judgement = Judgement.Bad;
                            }

                            int points = GetPointsForJudgement(judgement);
                            if (battleUI != null) battleUI.AddOpponentScore(points, true);
                            if (characterController != null) characterController.TriggerDance();
                            if (battleUI != null) battleUI.SetOpponentJudgement(judgement);

                            note.hit = true;
                            note.headJudgement = judgement;

                            if (note.isHold)
                            {
                                pendingNotes[i] = note;
                            }
                            else
                            {
                                pendingNotes.RemoveAt(i);
                            }
                        }
                    }
                }
                else if (note.isHold && note.hit)
                {
                    if (songTime >= note.tailTime)
                    {
                        float tailDiff = songTime - note.tailTime;

                        Judgement tailJudgement;
                        int tailPoints;
                        if (tailDiff <= judgementSystem.holdTailPerfectWindow) { tailJudgement = Judgement.Perfect; tailPoints = 300; }
                        else if (tailDiff <= judgementSystem.holdTailGoodWindow) { tailJudgement = Judgement.Good; tailPoints = 100; }
                        else if (tailDiff <= judgementSystem.holdTailBadWindow) { tailJudgement = Judgement.Bad; tailPoints = 50; }
                        else { tailJudgement = Judgement.Miss; tailPoints = 0; }

                        if (Random.value > accuracy)
                        {
                            tailJudgement = Judgement.Miss;
                            tailPoints = 0;
                        }

                        int holdCompletePoints = judgementSystem != null ? judgementSystem.holdCompletePoints : 100;

                        if (tailJudgement == Judgement.Miss)
                        {
                            if (battleUI != null) battleUI.SetOpponentMiss();
                            if (battleUI != null) battleUI.SetOpponentJudgement(Judgement.Miss);
                            if (characterController != null) characterController.SetIdle();
                        }
                        else
                        {
                            if (battleUI != null) battleUI.AddOpponentScore(holdCompletePoints + tailPoints, true);
                            if (characterController != null) characterController.TriggerDance();
                            if (battleUI != null) battleUI.SetOpponentJudgement(Judgement.HoldComplete);
                        }

                        pendingNotes.RemoveAt(i);
                    }
                }
                else
                {
                    if (songTime > note.startTime + judgementSystem.missWindow)
                    {
                        pendingNotes.RemoveAt(i);
                    }
                }
            }
        }

        void MissAtHead()
        {
            if (battleUI != null) battleUI.SetOpponentMiss();
            if (battleUI != null) battleUI.SetOpponentJudgement(Judgement.Miss);
            if (characterController != null) characterController.SetIdle();
        }

        void HandleNoteSpawned(NoteView note)
        {
            if (note == null) return;
            pendingNotes.Add(new PendingNote
            {
                startTime = note.TimeSeconds,
                tailTime = note.TailTimeSeconds,
                isHold = note.IsHold,
                started = false,
                hit = false,
                headJudgement = Judgement.Miss
            });
        }

        void OnJudged(Judgement j, int points, bool isHit, NoteOwner owner)
        {
        }

        int GetPointsForJudgement(Judgement j)
        {
            return j switch
            {
                Judgement.Perfect => 300,
                Judgement.Good => 100,
                Judgement.Bad => 50,
                Judgement.HoldComplete => judgementSystem != null ? judgementSystem.holdCompletePoints : 100,
                _ => 0
            };
        }
    }
}