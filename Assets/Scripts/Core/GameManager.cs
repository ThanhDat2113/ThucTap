using System.Collections.Generic;
using RhythmGame.Data;
using RhythmGame.Input;
using RhythmGame.Notes;
using RhythmGame.Scoring;
using RhythmGame.UI;
using UnityEngine;

namespace RhythmGame.Core
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-90)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Data")]
        [SerializeField] private GameConfig config;
        [SerializeField] private SongData song;

        [Header("Bootstrapped Components")]
        [SerializeField] private Conductor conductor;
        [SerializeField] private NoteSpawner noteSpawner;
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private HUDController hud;
        [SerializeField] private LaneUI laneUI;
        [SerializeField] private HitEffectSystem hitEffects;

        [Header("Demo")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool buildDemoSongIfEmpty = true;

        public GameConfig Config => config;
        public PlayerInputHandler InputHandler => inputHandler;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBootstrap()
        {
            if (Instance == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BootstrapSubsystems();

            if (config == null)
                config = CreateDefaultConfig();

            if (noteSpawner != null)
            {
                if (noteSpawner.config == null)
                    noteSpawner.config = config;
                if (noteSpawner.timingWindow == null)
                    noteSpawner.timingWindow = new TimingWindow
                    {
                        perfectSeconds = config.perfectWindow,
                        goodSeconds = config.goodWindow
                    };
            }

            if (inputHandler != null)
                inputHandler.worldCamera = Camera.main;
        }

        private void Start()
        {
            if (autoStart)
                StartGame();
        }

        public void StartGame()
        {
            ApplyScreenOrientation();
            SetupCamera();

            SongData runtimeSong = song != null ? song : (buildDemoSongIfEmpty ? CreateDemoSong() : null);
            if (runtimeSong == null)
                return;

            conductor.LoadSong(runtimeSong);
            if (noteSpawner != null)
                noteSpawner.Begin(runtimeSong);
            scoreManager?.ResetScore();
            conductor.StartSong();
        }

        private void BootstrapSubsystems()
        {
            conductor = conductor ? conductor : GetOrCreate<Conductor>("Conductor");
            noteSpawner = noteSpawner ? noteSpawner : GetOrCreate<NoteSpawner>("NoteSpawner");
            inputHandler = inputHandler ? inputHandler : GetOrCreate<PlayerInputHandler>("InputHandler");
            scoreManager = scoreManager ? scoreManager : (ScoreManager.Instance != null ? ScoreManager.Instance : GetOrCreate<ScoreManager>("ScoreManager"));
            hud = hud ? hud : GetOrCreate<HUDController>("HUD");
            laneUI = laneUI ? laneUI : GetOrCreate<LaneUI>("LaneUI");
            hitEffects = hitEffects ? hitEffects : GetOrCreate<HitEffectSystem>("HitEffects");

            if (noteSpawner != null && hitEffects != null)
                noteSpawner.HitFeedback += hitEffects.SpawnPopup;
        }

        private T GetOrCreate<T>(string name) where T : Component
        {
            T existing = FindAnyObjectByType<T>();
            if (existing != null)
                return existing;
            var go = new GameObject(name);
            go.transform.SetParent(transform, true);
            return go.AddComponent<T>();
        }

        private void ApplyScreenOrientation()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void SetupCamera()
        {
            if (config == null)
                return;

            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.1f);
            cam.cullingMask = ~0;

            float aspect = (float)Screen.width / Screen.height;
            float worldWidth = config.laneCount * config.laneWidth;
            float sizeForWidth = (worldWidth * 0.5f) / aspect;

            float floorY = config.hitPositionY - config.noteSpeed * 0.4f;
            float ceilY = config.spawnPositionY + 0.5f;
            float halfHeight = Mathf.Max(0.01f, (ceilY - floorY) * 0.5f);
            float centerY = (floorY + ceilY) * 0.5f;

            cam.orthographicSize = Mathf.Max(sizeForWidth, halfHeight);
            cam.transform.position = new Vector3(0f, centerY, -10f);
        }

        private GameConfig CreateDefaultConfig()
        {
            var cfg = ScriptableObject.CreateInstance<GameConfig>();
            cfg.laneCount = 4;
            cfg.laneWidth = 2.4f;
            cfg.laneStartX = -4.8f;
            cfg.hitPositionY = -3.2f;
            cfg.noteSpeed = 9f;
            cfg.spawnLeadTime = 1.4f;
            cfg.perfectWindow = 0.1f;
            cfg.goodWindow = 0.2f;
            cfg.laneKeys = new UnityEngine.InputSystem.Key[] { UnityEngine.InputSystem.Key.A, UnityEngine.InputSystem.Key.S, UnityEngine.InputSystem.Key.W, UnityEngine.InputSystem.Key.D };
            return cfg;
        }

        private SongData CreateDemoSong()
        {
            var s = ScriptableObject.CreateInstance<SongData>();
            s.songName = "Demo";
            s.bpm = 140f;
            s.offset = 0f;
            s.track = null;

            float beat = 60f / s.bpm;
            LaneID[] pattern = { LaneID.Left, LaneID.Down, LaneID.Up, LaneID.Right };
            var list = new List<NoteData>();
            for (int i = 0; i < 20; i++)
            {
                list.Add(new NoteData
                {
                    hitTime = i * beat,
                    lane = pattern[i % pattern.Length],
                    type = NoteType.Normal
                });
            }
            s.notes = list.ToArray();
            return s;
        }
    }
}
