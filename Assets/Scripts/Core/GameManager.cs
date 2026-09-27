using UnityEngine;

namespace RhythmGame
{
    public enum GameState { Countdown, Playing, Finished }

    /// <summary>
    /// Điều phối state machine (Countdown -> Playing -> Finished) và giữ
    /// đồng hồ bài hát (SongTime). Mọi thứ khác (spawn, judgement, UI)
    /// đều đi theo SongTime này, không tự đếm thời gian riêng.
    ///
    /// LƯU Ý QUAN TRỌNG khi thêm nhạc thật: thay đoạn "SongTime += Time.deltaTime"
    /// bằng cách đọc AudioSettings.dspTime (trừ đi thời điểm AudioSource.Play()),
    /// vì Time.deltaTime cộng dồn sẽ trôi lệch dần so với audio thật.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Refs")]
        public NoteSpawner spawner;
        public JudgementSystem judgement;
        public InputHandler input;

        [Header("Chart")]
        public ChartData chart;
        public AudioSource musicSource;

        [Header("Countdown")]
        public float countdownSeconds = 3f;

        public GameState State { get; private set; }
        public float SongTime { get; private set; }
        public float CountdownRemaining { get; private set; }

        public System.Action<GameState> OnStateChanged;

        Coroutine playMusicRoutine;

        void Start()
        {
            input.OnRestartPressed += Restart;
            Restart();
        }

        void OnDestroy()
        {
            if (input != null) input.OnRestartPressed -= Restart;
        }

        public void Restart()
        {
            if (musicSource != null) musicSource.Stop();
            if (playMusicRoutine != null) StopCoroutine(playMusicRoutine);
            spawner.Begin(chart);
            SongTime = 0f;
            CountdownRemaining = countdownSeconds;
            SetState(GameState.Countdown);
        }

        void Update()
        {
            switch (State)
            {
                case GameState.Countdown: TickCountdown(); break;
                case GameState.Playing: TickPlaying(); break;
            }
        }

        void TickCountdown()
        {
            CountdownRemaining -= Time.deltaTime;
            if (CountdownRemaining > 0f) return;

            SongTime = chart.notes.Count > 0 ? chart.BeatToTime(chart.notes[0].beat) - spawner.TravelTime : 0f;
            SetState(GameState.Playing);
        }

        void TickPlaying()
        {
            SongTime += Time.deltaTime;
            judgement.SetSongTime(SongTime);

            spawner.Tick(SongTime);
            judgement.CheckMisses();

            if (spawner.Finished) SetState(GameState.Finished);
        }

        void SetState(GameState s)
        {
            State = s;
            OnStateChanged?.Invoke(s);
            if (s == GameState.Playing) playMusicRoutine = StartCoroutine(PlayMusicDelayed());
        }

        System.Collections.IEnumerator PlayMusicDelayed()
        {
            float delay = chart != null ? chart.musicStartDelay : 0f;
            if (delay > 0f) yield return new WaitForSeconds(delay);

            if (musicSource != null && chart != null && chart.audioClip != null)
            {
                musicSource.clip = chart.audioClip;
                musicSource.time = 0f;
                musicSource.Play();
            }
        }
    }
}
