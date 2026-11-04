using UnityEngine;

namespace RhythmGame
{
    public enum GameState { Countdown, Playing, Finished, Failed }

    /// <summary>
    /// Điều phối state machine (Countdown -> Playing -> Finished/Failed) và giữ
    /// đồng hồ bài hát (SongTime). Mọi thứ khác (spawn, judgement, UI)
    /// đều đi theo SongTime này, không tự đếm thời gian riêng.
    ///
    /// SongTime được tính từ AudioSettings.dspTime (đồng hồ âm thanh thực)
    /// thay vì dồn Time.deltaTime, giúp note spawn khớp chính xác với nhạc
    /// cả lần đầu lẫn khi replay, không bị trôi lệch.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Refs")]
        public NoteSpawner spawner;
        public JudgementSystem judgement;
        public InputHandler input;
        public HealthSystem healthSystem;
        public OpponentAI opponentAI;

        [Header("Chart")]
        public ChartData chart;
        public AudioSource musicSource;

        [Header("Countdown")]
        public float countdownSeconds = 3f;

        public GameState State { get; private set; }
        public float SongTime { get; private set; }
        public float CountdownRemaining { get; private set; }

        public System.Action<GameState> OnStateChanged;

        bool clearNotesPending;
        double dspMusicStartTime; // AudioSettings.dspTime khi nhạc được lên lịch phát
        bool musicScheduled;

        void Start()
        {
            input.OnRestartPressed += Restart;
            if (healthSystem != null) healthSystem.OnHealthDepleted.AddListener(OnHealthDepleted);
            if (PauseManager.Instance != null) PauseManager.Instance.RestartHandler = Restart;
            if (GameSession.SelectedSong != null && GameSession.SelectedSong.chart != null)
            {
                // Luôn ưu tiên chart của bài người dùng chọn ở Song List
                chart = GameSession.SelectedSong.chart;
                if (musicSource != null && GameSession.SelectedSong.clip != null) musicSource.clip = GameSession.SelectedSong.clip;
            }
            else if (chart == null)
            {
                Debug.LogWarning("[GameManager] Chưa có ChartData nào. Chọn bài ở Song List hoặc gán chart trong Inspector.");
            }
            Restart();
        }

        void OnDestroy()
        {
            if (PauseManager.Instance != null) PauseManager.Instance.RestartHandler = null;
            if (input != null) input.OnRestartPressed -= Restart;
            if (healthSystem != null) healthSystem.OnHealthDepleted.RemoveListener(OnHealthDepleted);
        }

        public void Restart()
        {
            // Đang pause mà Restart (phím R hoặc nút trong menu pause) -> bỏ pause trước
            if (PauseManager.Instance != null) PauseManager.Instance.ForceUnpause();
            if (musicSource != null) musicSource.Stop();
            spawner.Begin(chart);
            SongTime = 0f;
            CountdownRemaining = countdownSeconds;
            clearNotesPending = false;
            musicScheduled = false;
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
            // Giới hạn delta time để tránh trường hợp frame đầu sau khi load scene
            // có unscaledDeltaTime lớn -> countdown bị nhảy từ 3 thẳng xuống 0.
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.5f);
            CountdownRemaining -= dt;
            if (CountdownRemaining > 0f) return;

            SetState(GameState.Playing);
        }

        void TickPlaying()
        {
            if (!musicScheduled) return;

            // SongTime tính từ đồng hồ âm thanh thực (AudioSettings.dspTime),
            // không dồn Time.deltaTime -> không trôi lệch so với nhạc.
            double dspNow = AudioSettings.dspTime;
            SongTime = (float)(dspNow - dspMusicStartTime);

            // effectiveTime = thời gian hiệu lực cho note/judgement, được lùi theo noteStartDelay của chart
            float noteDelay = chart != null ? chart.noteStartDelay : 0f;
            float effectiveTime = Mathf.Max(0f, SongTime - noteDelay);

            judgement.SetSongTime(effectiveTime);
            if (opponentAI != null) opponentAI.SetSongTime(effectiveTime);

            spawner.Tick(effectiveTime);
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
            // Chỉ cho pause khi đang đếm ngược hoặc đang chơi (không pause ở màn Finished/Failed)
            if (PauseManager.Instance != null)
                PauseManager.Instance.canPause = s == GameState.Countdown || s == GameState.Playing;
            OnStateChanged?.Invoke(s);
            if (s == GameState.Playing)
            {
                ScheduleMusic();
                if (opponentAI != null) opponentAI.SetActive(true);
            }
            if (s != GameState.Playing && opponentAI != null) opponentAI.SetActive(false);
        }

        /// <summary>
        /// Lên lịch phát nhạc tại một thời điểm DSP chắc chắn (PlayScheduled),
        /// tránh hitch/khựng màn hình do WaitForSeconds và Play() không chính xác mẫu.
        /// </summary>
        void ScheduleMusic()
        {
            if (musicSource == null || chart == null || chart.audioClip == null) return;

            // Bắt đầu nhạc sau một khoảng nghỉ. Đảm bảo có ít nhất 0.5s để
            // countdown hiển thị rõ trước khi nhạc phát (tránh phát ngay lập tức).
            float musicDelay = Mathf.Max(0.5f, chart != null ? chart.musicStartDelay : 0.5f);
            double startDSP = AudioSettings.dspTime + musicDelay;
            dspMusicStartTime = startDSP;
            musicSource.clip = chart.audioClip;
            musicSource.time = 0f;
            musicSource.PlayScheduled(startDSP);
            musicScheduled = true;
        }
    }
}