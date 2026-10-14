using UnityEngine;

namespace RhythmGame
{
    public class OpponentAI : MonoBehaviour
    {
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

        void Start()
        {
            if (judgementSystem != null)
            {
                judgementSystem.OnJudged += OnJudged;
            }
        }

        void OnDestroy()
        {
            if (judgementSystem != null)
            {
                judgementSystem.OnJudged -= OnJudged;
            }
        }

        public void SetSongTime(float t) => songTime = t;
        public void SetActive(bool active) => isActive = active;

        void Update()
        {
            if (!isActive || spawner == null) return;

            var notes = spawner.ActiveNotes;
            foreach (var note in notes)
            {
                if (note.Data.owner != NoteOwner.Opponent) continue;
                if (note.Judged) continue;
                if (note.Holding) continue;

                float diff = songTime - note.TimeSeconds - reactionTime;

                if (diff >= -judgementSystem.missWindow && diff <= judgementSystem.missWindow)
                {
                    AttemptTap(note, diff);
                }
            }

            CheckHoldReleases();
        }

        void AttemptTap(NoteView note, float diff)
        {
            if (Random.value > accuracy)
            {
                if (battleUI != null) battleUI.SetOpponentMiss();
                return;
            }

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
                if (r < goodRate / (goodRate + badRate)) judgement = Judgement.Good;
                else judgement = Judgement.Bad;
            }
            else
            {
                judgement = Judgement.Bad;
            }

            int points = GetPointsForJudgement(judgement);
            judgementSystem.JudgeOpponentNote(note, judgement);

            if (battleUI != null) battleUI.AddOpponentScore(points, true);

            if (characterController != null)
            {
                characterController.TriggerDance();
            }
        }

        void CheckHoldReleases()
        {
            var notes = spawner.ActiveNotes;
            foreach (var note in notes)
            {
                if (note.Data.owner != NoteOwner.Opponent) continue;
                if (!note.Holding) continue;

                float diff = songTime - note.TailTimeSeconds - reactionTime;

                if (diff >= -judgementSystem.holdTailBadWindow)
                {
                    Judgement tailJudgement;
                    if (diff <= judgementSystem.holdTailPerfectWindow) tailJudgement = Judgement.Perfect;
                    else if (diff <= judgementSystem.holdTailGoodWindow) tailJudgement = Judgement.Good;
                    else if (diff <= judgementSystem.holdTailBadWindow) tailJudgement = Judgement.Bad;
                    else tailJudgement = Judgement.Miss;

                    if (Random.value > accuracy)
                    {
                        tailJudgement = Judgement.Miss;
                    }

                    int tailPoints = GetPointsForJudgement(tailJudgement);
                    judgementSystem.JudgeOpponentHoldRelease(note, tailJudgement);

                    if (battleUI != null) battleUI.AddOpponentScore(tailPoints, tailJudgement != Judgement.Miss);

                    if (characterController != null)
                    {
                        characterController.TriggerDance();
                    }
                }
            }
        }

        void OnJudged(Judgement j, int points, bool isHit, NoteOwner owner)
        {
            // Optional: react to player's judgement
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