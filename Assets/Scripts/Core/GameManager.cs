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

        Coroutine playMusicRoutine;
        bool clearNotesPending;

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
            // Äang pause mÃ  Restart (phÃ­m R hoáº·c nÃºt trong menu pause) -> bá» pause trÆ°á»›c
            if (PauseManager.Instance != null) PauseManager.Instance.ForceUnpause();
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
                case GameState.Dialogue: break;
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
            if (opponentAI != null) opponentAI.SetSongTime(SongTime);

            spawner.Tick(SongTime);
            judgement.CheckMisses();

            // CheckMisses cÃ³ thá»ƒ lÃ m cáº¡n mÃ¡u -> chuyá»ƒn sang Failed giá»¯a chá»«ng,
            // lÃºc nÃ y Ä‘Ã£ káº¿t thÃºc lÆ°á»£t chÆ¡i nÃªn khÃ´ng Ä‘Æ°á»£c ghi Ä‘Ã¨ báº±ng Finished.
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
            if (s == GameState.Playing) { playMusicRoutine = StartCoroutine(PlayMusicDelayed()); if (opponentAI != null) opponentAI.SetActive(true); }
            if (s != GameState.Playing && opponentAI != null) opponentAI.SetActive(false);
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
