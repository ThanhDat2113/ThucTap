using UnityEngine;

namespace RhythmGame
{
    public enum Judgement { Perfect, Good, Bad, Miss }

    public class JudgementSystem : MonoBehaviour
    {
        [Header("Refs")]
        public NoteSpawner spawner;
        public InputHandler input;

        [Header("Timing window (giây)")]
        public float perfectWindow = 0.045f;
        public float goodWindow = 0.09f;
        public float missWindow = 0.14f;

        [Header("SFX")]
        public AudioClip perfectSfx;
        public AudioClip goodSfx;
        public AudioClip badSfx;
        public AudioClip missSfx;
        public AudioSource audioSource;

        public System.Action<Judgement, int, bool> OnJudged;
        public System.Action<int> OnLaneActivated;

        float songTime;

        void Start()
        {
            input.OnLanePressed += HandlePress;
        }

        void OnDestroy()
        {
            if (input != null) input.OnLanePressed -= HandlePress;
        }

        public void SetSongTime(float t) => songTime = t;

        public void CheckMisses()
        {
            foreach (var n in spawner.ActiveNotes)
            {
                if (n.Judged) continue;
                if (songTime - n.TimeSeconds > missWindow)
                {
                    n.MarkJudged();
                    n.PlayMissVisual();
                    OnJudged?.Invoke(Judgement.Miss, 0, false);
                    PlaySfx(Judgement.Miss);
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
            spawner.ReleaseNote(note);

            Judgement judgement;
            int points;
            if (diff <= perfectWindow) { judgement = Judgement.Perfect; points = 300; }
            else if (diff <= goodWindow) { judgement = Judgement.Good; points = 100; }
            else { judgement = Judgement.Bad; points = 50; }

            PlaySfx(judgement);
            OnJudged?.Invoke(judgement, points, true);
        }

        void PlaySfx(Judgement j)
        {
            if (audioSource == null) return;
            AudioClip clip = j switch
            {
                Judgement.Perfect => perfectSfx,
                Judgement.Good => goodSfx,
                Judgement.Bad => badSfx,
                _ => missSfx
            };
            if (clip != null) audioSource.PlayOneShot(clip);
        }
    }
}