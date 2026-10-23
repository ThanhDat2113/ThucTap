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
        public FlowSystem flowSystem;

        [Header("Timing window (giây)")]
        public float perfectWindow = 0.045f;
        public float goodWindow = 0.09f;
        public float missWindow = 0.14f;

        [Header("Hold note")]
        [Tooltip("Điểm thưởng khi giữ tới hết đuôi hold note.")]
        public int holdCompletePoints = 100;
        [Tooltip("Nhả phím sớm hơn đuôi tối đa bao nhiêu giây vẫn tính là giữ xong.")]
        public float holdReleaseTolerance = 0.12f;
        [Tooltip("Cửa sổ Perfect/Good/Bad cho việc nhả đuôi hold (giây).")]
        public float holdTailPerfectWindow = 0.045f;
        public float holdTailGoodWindow = 0.09f;
        public float holdTailBadWindow = 0.14f;

        [Header("SFX")]
        public AudioClip perfectSfx;
        public AudioClip goodSfx;
        public AudioClip badSfx;
        public AudioClip missSfx;
        public AudioClip holdCompleteSfx;
        public AudioSource audioSource;

        public System.Action<Judgement, int, bool, NoteOwner> OnJudged;
        public System.Action<int> OnLaneActivated;

        float songTime;
        int _combo;

        void Start()
        {
            input.OnLanePressed += HandlePress;
            input.OnLaneReleased += HandleRelease;
            if (PauseManager.Instance != null)
                PauseManager.Instance.onResume.AddListener(OnGameResumed);
        }

        void OnDestroy()
        {
            if (PauseManager.Instance != null)
                PauseManager.Instance.onResume.RemoveListener(OnGameResumed);
            if (input == null) return;
            input.OnLanePressed -= HandlePress;
            input.OnLaneReleased -= HandleRelease;
        }

        public void SetSongTime(float t) => songTime = t;

        public void CheckMisses()
        {
            if (PauseManager.IsPaused) return;
            var notes = spawner.ActiveNotes;
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                NoteView n = notes[i];

                if (n.Data.owner != NoteOwner.Player) continue;

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
                    OnJudged?.Invoke(Judgement.Miss, 0, false, NoteOwner.Player);
                    PlaySfx(Judgement.Miss);
                    healthSystem?.ApplyJudgement(Judgement.Miss);

                    _combo = 0;
                    flowSystem?.OnMiss();
                }
            }
        }

        void HandlePress(int lane)
        {
            if (PauseManager.IsPaused) return;
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

            note.PlayHitEffect(judgement, note.transform.position);

            if (note.IsHold) note.BeginHold();
            else spawner.ReleaseNote(note);

            PlaySfx(judgement);
            OnJudged?.Invoke(judgement, points, true, note.Data.owner);
            healthSystem?.ApplyJudgement(judgement);

            _combo++;
            flowSystem?.OnNoteJudged(true, _combo);
        }

        void HandleRelease(int lane)
        {
            if (PauseManager.IsPaused) return;
            NoteView holding = null;
            foreach (var n in spawner.ActiveNotes)
            {
                if (n.Holding && n.Data.lane == lane && n.Data.owner == NoteOwner.Player) { holding = n; break; }
            }
            if (holding == null) return;

            float tailTime = holding.TailTimeSeconds;
            float diff = songTime - tailTime;

            if (diff < -holdReleaseTolerance)
            {
                holding.BreakHold();
                OnJudged?.Invoke(Judgement.Miss, 0, false, NoteOwner.Player);
                PlaySfx(Judgement.Miss);
                healthSystem?.ApplyJudgement(Judgement.Miss);
                return;
            }

            Judgement tailJudgement;
            int tailPoints;
            if (diff <= holdTailPerfectWindow) { tailJudgement = Judgement.Perfect; tailPoints = 300; }
            else if (diff <= holdTailGoodWindow) { tailJudgement = Judgement.Good; tailPoints = 100; }
            else if (diff <= holdTailBadWindow) { tailJudgement = Judgement.Bad; tailPoints = 50; }
            else { tailJudgement = Judgement.Miss; tailPoints = 0; }

            holding.PlayHitEffect(tailJudgement, holding.transform.position);
            spawner.ReleaseNote(holding);

            PlaySfx(tailJudgement);
            OnJudged?.Invoke(tailJudgement, holdCompletePoints + tailPoints, tailJudgement != Judgement.Miss, holding.Data.owner);
            healthSystem?.ApplyJudgement(tailJudgement);

            _combo++;
            flowSystem?.OnNoteJudged(tailJudgement != Judgement.Miss, _combo);
        }

        /// <summary>
        /// Gọi khi hết pause. Nếu người chơi nhả phím trong lúc pause thì hold note bị đứt
        /// (nếu không, hold sẽ tự chạy tới hết đuôi và được tính hoàn thành miễn phí).
        /// </summary>
        public void OnGameResumed()
        {
            var notes = spawner.ActiveNotes;
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                NoteView n = notes[i];
                if (!n.Holding || input.IsHeld(n.Data.lane)) continue;

                n.BreakHold();
                OnJudged?.Invoke(Judgement.Miss, 0, false, NoteOwner.Player);
                PlaySfx(Judgement.Miss);
                healthSystem?.ApplyJudgement(Judgement.Miss);
            }
        }

        void CompleteHold(NoteView n)
        {
            n.PlayHitEffect(Judgement.HoldComplete, n.transform.position);
            spawner.ReleaseNote(n);
            PlaySfx(Judgement.HoldComplete);
            OnJudged?.Invoke(Judgement.HoldComplete, holdCompletePoints, true, n.Data.owner);
            healthSystem?.ApplyJudgement(Judgement.HoldComplete);

            _combo++;
            flowSystem?.OnNoteJudged(true, _combo);
        }

        public void JudgeOpponentNote(NoteView note, Judgement judgement)
        {
            if (note == null || note.Judged) return;

            note.MarkJudged();
            int points = GetPointsForJudgement(judgement);
            note.PlayHitEffect(judgement, note.transform.position);

            if (note.IsHold)
            {
                note.BeginHold();
            }
            else
            {
                spawner.ReleaseNote(note);
            }

            PlaySfx(judgement);
            OnJudged?.Invoke(judgement, points, true, note.Data.owner);
        }

        public void JudgeOpponentHoldRelease(NoteView note, Judgement tailJudgement)
        {
            if (note == null) return;

            note.PlayHitEffect(tailJudgement, note.transform.position);
            spawner.ReleaseNote(note);

            int tailPoints = GetPointsForJudgement(tailJudgement);
            PlaySfx(tailJudgement);
            OnJudged?.Invoke(tailJudgement, holdCompletePoints + tailPoints, tailJudgement != Judgement.Miss, note.Data.owner);
        }

        int GetPointsForJudgement(Judgement j)
        {
            return j switch
            {
                Judgement.Perfect => 300,
                Judgement.Good => 100,
                Judgement.Bad => 50,
                Judgement.HoldComplete => holdCompletePoints,
                _ => 0
            };
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

        public void ResetCombo()
        {
            _combo = 0;
            flowSystem?.Reset();
        }
    }
}