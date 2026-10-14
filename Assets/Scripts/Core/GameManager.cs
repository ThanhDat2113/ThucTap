using System.Linq;
using UnityEngine;

namespace RhythmGame
{
    public enum GameState { Countdown, Playing, Finished, Failed }
    public enum GameMode { Single, Battle }

    public class GameManager : MonoBehaviour
    {
        [Header("Mode")]
        public GameMode gameMode = GameMode.Single;

        [Header("Refs")]
        public NoteSpawner spawner;
        public JudgementSystem judgement;
        public InputHandler input;
        public HealthSystem playerHealthSystem;
        public HealthSystem opponentHealthSystem;

        [Header("Battle")]
        public OpponentAI opponentAI;
        public CharacterController playerCharacter;
        public CharacterController opponentCharacter;

        [Header("Chart")]
        public ChartData chart;
        public AudioSource musicSource;

        [Header("Countdown")]
        public float countdownSeconds = 3f;

        public GameState State { get; private set; }
        public float SongTime { get; private set; }
        public float CountdownRemaining { get; private set; }
        public NoteOwner CurrentTurnOwner { get; private set; } = NoteOwner.Player;

        public System.Action<GameState> OnStateChanged;
        public System.Action<NoteOwner> OnTurnChanged;

        Coroutine playMusicRoutine;
        bool clearNotesPending;
        int nextNoteIndex = 0;

        void Start()
        {
            input.OnRestartPressed += Restart;
            if (playerHealthSystem != null) playerHealthSystem.OnHealthDepleted.AddListener(OnPlayerHealthDepleted);
            if (opponentHealthSystem != null) opponentHealthSystem.OnHealthDepleted.AddListener(OnOpponentHealthDepleted);

            if (opponentAI != null) opponentAI.SetActive(false);
            if (playerCharacter != null) playerCharacter.ForceIdle();
            if (opponentCharacter != null) opponentCharacter.ForceIdle();

            Restart();
        }

        void OnDestroy()
        {
            if (input != null) input.OnRestartPressed -= Restart;
            if (playerHealthSystem != null) playerHealthSystem.OnHealthDepleted.RemoveListener(OnPlayerHealthDepleted);
            if (opponentHealthSystem != null) opponentHealthSystem.OnHealthDepleted.RemoveListener(OnOpponentHealthDepleted);
        }

        public void Restart()
        {
            if (musicSource != null) musicSource.Stop();
            if (playMusicRoutine != null) StopCoroutine(playMusicRoutine);
            spawner.Begin(chart);
            SongTime = 0f;
            CountdownRemaining = countdownSeconds;
            clearNotesPending = false;
            nextNoteIndex = 0;

            if (playerHealthSystem != null) playerHealthSystem.ResetHealth();
            if (opponentHealthSystem != null) opponentHealthSystem.ResetHealth();

            if (opponentAI != null)
            {
                opponentAI.SetSongTime(0f);
                opponentAI.SetActive(gameMode == GameMode.Battle);
            }

            if (playerCharacter != null) playerCharacter.ForceIdle();
            if (opponentCharacter != null) opponentCharacter.ForceIdle();

            CurrentTurnOwner = GetNextNoteOwner();
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

            if (opponentAI != null && gameMode == GameMode.Battle)
            {
                opponentAI.SetSongTime(SongTime);
            }

            spawner.Tick(SongTime);
            judgement.CheckMisses();

            CheckTurnChange();

            if (State == GameState.Playing && spawner.Finished) SetState(GameState.Finished);
        }

        void CheckTurnChange()
        {
            if (chart == null || chart.notes.Count == 0) return;

            // Find the next upcoming note that hasn't been judged yet
            NoteOwner nextOwner = GetNextNoteOwner();
            
            if (nextOwner != CurrentTurnOwner)
            {
                CurrentTurnOwner = nextOwner;
                OnTurnChanged?.Invoke(CurrentTurnOwner);

                // Switch character animations
                if (CurrentTurnOwner == NoteOwner.Player)
                {
                    // Player's turn: opponent goes idle, player can dance
                    if (opponentCharacter != null) opponentCharacter.ForceIdle();
                }
                else
                {
                    // Opponent's turn: player goes idle, opponent can dance
                    if (playerCharacter != null) playerCharacter.ForceIdle();
                }
            }
        }

        NoteOwner GetNextNoteOwner()
        {
            if (chart == null || chart.notes.Count == 0) return NoteOwner.Player;

            // Look ahead to find the next unjudged note
            // We need to check active notes from spawner
            var activeNotes = spawner?.ActiveNotes;
            if (activeNotes != null)
            {
                foreach (var note in activeNotes)
                {
                    if (!note.Judged && !note.Holding)
                    {
                        return note.Data.owner;
                    }
                }
            }

            // Fallback: check chart notes by index
            while (nextNoteIndex < chart.notes.Count)
            {
                var note = chart.notes[nextNoteIndex];
                // Check if this note has already been spawned/judged
                bool isActive = activeNotes != null && activeNotes.Any(n => n.Data == note);
                bool isPast = SongTime > chart.BeatToTime(note.beat) + 1f; // 1 second grace
                
                if (!isActive && !isPast)
                {
                    return note.owner;
                }
                nextNoteIndex++;
            }

            return CurrentTurnOwner; // Keep current if no upcoming notes
        }

        void OnPlayerHealthDepleted()
        {
            if (State != GameState.Playing) return;

            if (musicSource != null) musicSource.Stop();
            clearNotesPending = true;
            SetState(GameState.Failed);
        }

        void OnOpponentHealthDepleted()
        {
            if (State != GameState.Playing) return;

            if (musicSource != null) musicSource.Stop();
            clearNotesPending = true;
            SetState(GameState.Finished);
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