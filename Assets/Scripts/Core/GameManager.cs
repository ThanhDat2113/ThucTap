using UnityEngine;

namespace RhythmGame
{
    public enum GameState { Dialogue, Countdown, Playing, Finished, Failed }

    /// <summary>
    /// Äiá»u phá»‘i state machine (Countdown -> Playing -> Finished/Failed) vÃ  giá»¯
    /// Ä‘á»“ng há»“ bÃ i hÃ¡t (SongTime). Má»i thá»© khÃ¡c (spawn, judgement, UI)
    /// Ä‘á»u Ä‘i theo SongTime nÃ y, khÃ´ng tá»± Ä‘áº¿m thá»i gian riÃªng.
    ///
    /// LÆ¯U Ã QUAN TRá»ŒNG khi thÃªm nháº¡c tháº­t: thay Ä‘oáº¡n "SongTime += Time.deltaTime"
    /// báº±ng cÃ¡ch Ä‘á»c AudioSettings.dspTime (trá»« Ä‘i thá»i Ä‘iá»ƒm AudioSource.Play()),
    /// vÃ¬ Time.deltaTime cá»™ng dá»“n sáº½ trÃ´i lá»‡ch dáº§n so vá»›i audio tháº­t.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        void Awake() { Instance = this; }

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
        double dspMusicStartTime;
        bool musicScheduled;

        void Start()
        {
            input.OnRestartPressed += Restart;
            if (healthSystem != null) healthSystem.OnHealthDepleted.AddListener(OnHealthDepleted);
            if (PauseManager.Instance != null) PauseManager.Instance.RestartHandler = Restart;
            if (GameSession.SelectedSong != null)
            {
                if (GameSession.SelectedSong.chart != null) chart = GameSession.SelectedSong.chart;
                if (musicSource != null && GameSession.SelectedSong.clip != null) musicSource.clip = GameSession.SelectedSong.clip;
            }

            if (Luan.Dialogue.StoryDialogueManager.HasPendingDialogue())
            {
                SetState(GameState.Dialogue);
                CountdownRemaining = countdownSeconds;
                return;
            }
            Restart();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (PauseManager.Instance != null) PauseManager.Instance.RestartHandler = null;
            if (input != null) input.OnRestartPressed -= Restart;
            if (healthSystem != null) healthSystem.OnHealthDepleted.RemoveListener(OnHealthDepleted);
        }

        public void Restart()
        {
            // Đang pause mà Restart (phím R hoặc nút trong menu pause) -> bỏ pause trước
            if (PauseManager.Instance != null) PauseManager.Instance.ForceUnpause();
            if (musicSource != null) musicSource.Stop();

            // Tắt nhạc nền scene trước (Song List / Menu) nếu SceneBackgroundMusic tự phát khi load scene
            if (MusicManager.Instance != null) MusicManager.Instance.StopMusic();

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
                case GameState.Dialogue: break;
                case GameState.Countdown: TickCountdown(); break;
                case GameState.Playing: TickPlaying(); break;
            }
        }

        void TickCountdown()
        {
            // Giới hạn dt để tránh frame đầu sau load scene làm countdown nhảy qua các con số.
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
            float rawSongTime = (float)(dspNow - dspMusicStartTime);

            // Trước khi nhạc phát (dspNow < dspMusicStartTime) thì rawSongTime âm -> chờ, không advance.
            if (rawSongTime < 0f) return;

            SongTime = rawSongTime;

            // effectiveTime = thời gian hiệu lực cho note/judgement:
            // - trừ noteStartDelay (note xuất hiện sau khi nhạc đã phát một lát)
            // - cộng latencyCompensation (bù độ trễ khi thu, giúp note khớp lúc record)
            float noteDelay = chart != null ? chart.noteStartDelay : 0f;
            float latency = chart != null ? chart.latencyCompensation : 0f;
            float effectiveTime = Mathf.Max(0f, SongTime - noteDelay + latency);

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

            // Háº¿t mÃ¡u -> káº¿t thÃºc lÆ°á»£t chÆ¡i: dá»«ng nháº¡c vÃ  dá»«ng háº³n gameplay.
            if (musicSource != null) musicSource.Stop();

            // HoÃ£n xoÃ¡ note tá»›i frame sau: OnHealthDepleted Ä‘Æ°á»£c gá»i tá»« giá»¯a vÃ²ng
            // láº·p CheckMisses, xoÃ¡ list lÃºc Ä‘Ã³ sáº½ lÃ m vá»¡ vÃ²ng láº·p Ä‘ang duyá»‡t.
            clearNotesPending = true;

            SetState(GameState.Failed);
        }

        void SetState(GameState s)
        {
            State = s;
            // Chá»‰ cho pause khi Ä‘ang Ä‘áº¿m ngÆ°á»£c hoáº·c Ä‘ang chÆ¡i (khÃ´ng pause á»Ÿ mÃ n Finished/Failed)
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

            // Bắt đầu nhạc sau một khoảng nghỉ (dùng musicStartDelay của chart, mặc định 0.5s)
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
