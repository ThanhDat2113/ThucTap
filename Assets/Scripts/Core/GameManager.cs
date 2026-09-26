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

        [Header("Countdown")]
        public float countdownSeconds = 3f;

        public GameState State { get; private set; }
        public float SongTime { get; private set; }
        public float CountdownRemaining { get; private set; }

        public System.Action<GameState> OnStateChanged;

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
            spawner.Begin(chart);
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

            SongTime = chart.notes.Count > 0 ? chart.notes[0].time - spawner.TravelTime : 0f;
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
        }
    }
}
