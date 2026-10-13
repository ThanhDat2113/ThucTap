using UnityEngine;

namespace RhythmGame
{
    public enum GameState { Countdown, Playing, Finished, Failed }

    /// <summary>
    /// Điều phối state machine (Countdown -> Playing -> Finished/Failed) và giữ
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
        public HealthSystem healthSystem;

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
        bool clearNotesPending;

        void Start()
        {
            input.OnRestartPressed += Restart;
            if (healthSystem != null) healthSystem.OnHealthDepleted.AddListener(OnHealthDepleted);
            Restart();
        }

        void OnDestroy()
        {
            if (input != null) input.OnRestartPressed -= Restart;
            if (healthSystem != null) healthSystem.OnHealthDepleted.RemoveListener(OnHealthDepleted);
        }

        public void Restart()
        {
            if (musicSource != null) musicSource.Stop();
            if (playMusicRoutine != null) StopCoroutine(playMusicRoutine);
            spawner.Begin(chart);
            SongTime = 0f;
            CountdownRemaining = countdownSeconds;
            clearNotesPending = false;
            if (healthSystem != null) healthSystem.ResetHealth();
            SetState(GameState.Countdown);
        }

        void Update()
        {
            if (clearNotesPending)
            {
                clearNotesPending = false;
                spawner.ClearActive();
            }

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

            // CheckMisses có thể làm cạn máu -> chuyển sang Failed giữa chừng,
            // lúc này đã kết thúc lượt chơi nên không được ghi đè bằng Finished.
            if (State == GameState.Playing && spawner.Finished) SetState(GameState.Finished);
        }

        void OnHealthDepleted()
        {
            if (State != GameState.Playing) return;

            // Hết máu -> kết thúc lượt chơi: dừng nhạc và dừng hẳn gameplay.
            if (musicSource != null) musicSource.Stop();

            // Hoãn xoá note tới frame sau: OnHealthDepleted được gọi từ giữa vòng
            // lặp CheckMisses, xoá list lúc đó sẽ làm vỡ vòng lặp đang duyệt.
            clearNotesPending = true;

            SetState(GameState.Failed);
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
