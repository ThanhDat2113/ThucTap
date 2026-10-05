using UnityEngine;

namespace RhythmGame
{
    public enum Judgement { Perfect, Good, Bad, Miss, HoldComplete }

    public class JudgementSystem : MonoBehaviour
    {
        [Header("Refs")]
        public NoteSpawner spawner;
        public InputHandler input;
        public HealthSystem healthSystem;

        [Header("Timing window (giây)")]
        public float perfectWindow = 0.045f;
        public float goodWindow = 0.09f;
        public float missWindow = 0.14f;

        [Header("Hold note")]
        [Tooltip("Điểm thưởng khi giữ tới hết đuôi hold note.")]
        public int holdCompletePoints = 100;
        [Tooltip("Nhả phím sớm hơn đuôi tối đa bao nhiêu giây vẫn tính là giữ xong.")]
        public float holdReleaseTolerance = 0.12f;

        [Header("SFX")]
        public AudioClip perfectSfx;
        public AudioClip goodSfx;
        public AudioClip badSfx;
        public AudioClip missSfx;
        public AudioClip holdCompleteSfx;
        public AudioSource audioSource;

        public System.Action<Judgement, int, bool> OnJudged;
        public System.Action<int> OnLaneActivated;

        float songTime;

        void Start()
        {
            input.OnLanePressed += HandlePress;
            input.OnLaneReleased += HandleRelease;
        }

        void OnDestroy()
        {
            if (input == null) return;
            input.OnLanePressed -= HandlePress;
            input.OnLaneReleased -= HandleRelease;
        }

        public void SetSongTime(float t) => songTime = t;

        public void CheckMisses()
        {
            var notes = spawner.ActiveNotes;
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                NoteView n = notes[i];

                if (n.Holding)
                {
                    if (songTime >= n.TailTimeSeconds) CompleteHold(n);
                    continue;
                }

                if (n.Judged) continue;
                if (songTime - n.TimeSeconds > missWindow)
                {
                    n.MarkJudged();
                    n.PlayMissVisual();
                    OnJudged?.Invoke(Judgement.Miss, 0, false);
                    PlaySfx(Judgement.Miss);
                    healthSystem?.ApplyJudgement(Judgement.Miss);
                }
            }
        }

        void HandlePress(int lane)
        {
            OnLaneActivated?.Invoke(lane);

            NoteView note = spawner.FindClosestUnjudged(lane, songTime);
            if (note == null) return;

            float diff = Mathf.Abs(songTime - note.TimeSeconds);
            if (diff > missWindow) return;

            note.MarkJudged();

            Judgement judgement;
            int points;
            if (diff <= perfectWindow) { judgement = Judgement.Perfect; points = 300; }
            else if (diff <= goodWindow) { judgement = Judgement.Good; points = 100; }
            else { judgement = Judgement.Bad; points = 50; }

            if (note.IsHold) note.BeginHold();
            else spawner.ReleaseNote(note);

            PlaySfx(judgement);
            OnJudged?.Invoke(judgement, points, true);
            healthSystem?.ApplyJudgement(judgement);
        }

        void HandleRelease(int lane)
        {
            NoteView holding = null;
            foreach (var n in spawner.ActiveNotes)
            {
                if (n.Holding && n.Data.lane == lane) { holding = n; break; }
            }
            if (holding == null) return;

            if (songTime >= holding.TailTimeSeconds - holdReleaseTolerance)
            {
                CompleteHold(holding);
            }
            else
            {
                holding.BreakHold();
                OnJudged?.Invoke(Judgement.Miss, 0, false);
                PlaySfx(Judgement.Miss);
                healthSystem?.ApplyJudgement(Judgement.Miss);
            }
        }

        void CompleteHold(NoteView n)
        {
            spawner.ReleaseNote(n);
            PlaySfx(Judgement.HoldComplete);
            OnJudged?.Invoke(Judgement.HoldComplete, holdCompletePoints, true);
            healthSystem?.ApplyJudgement(Judgement.HoldComplete);
        }

        void PlaySfx(Judgement j)
        {
            if (audioSource == null) return;
            AudioClip clip = j switch
            {
                Judgement.Perfect => perfectSfx,
                Judgement.Good => goodSfx,
                Judgement.Bad => badSfx,
                Judgement.HoldComplete => holdCompleteSfx,
                _ => missSfx
            };
            if (clip != null) audioSource.PlayOneShot(clip);
        }
    }
}