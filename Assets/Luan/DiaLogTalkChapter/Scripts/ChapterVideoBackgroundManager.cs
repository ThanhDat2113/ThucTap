using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using RhythmGame;

namespace Luan.Background
{
    [System.Serializable]
    public class ChapterVideoConfig
    {
        public int chapterNumber = 1;
        public string chapterName = "Chapter 1";
        public VideoClip noComboClip;
        public VideoClip comboClip;
    }

    [DisallowMultipleComponent]
    public class ChapterVideoBackgroundManager : MonoBehaviour
    {
        public static ChapterVideoBackgroundManager Instance { get; private set; }

        [Header("Chapter Video Configs (1 - 4)")]
        [SerializeField] private List<ChapterVideoConfig> chapterConfigs = new List<ChapterVideoConfig>();

        [Header("Combo Threshold")]
        [Tooltip("Số combo bắt đầu chuyển sang video có combo (yêu cầu: > 50)")]
        [SerializeField] private int comboThreshold = 50;

        [Tooltip("Thời gian chuyển cảnh mượt giữa No-Combo và High-Combo (giây)")]
        [Range(0.2f, 2.0f)]
        [SerializeField] private float comboTransitionDuration = 0.5f;

        [Header("Loop Smoothing")]
        [Tooltip("Bật hiệu ứng làm mượt điểm loop video")]
        [SerializeField] private bool enableLoopSmoothing = true;

        [Tooltip("Thời gian crossfade làm mượt khi video loop (giây)")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float loopCrossfadeDuration = 0.35f;

        [Tooltip("Cường độ nhịp sáng nhẹ tại điểm loop")]
        [Range(0f, 1f)]
        [SerializeField] private float loopPulseIntensity = 0.25f;

        [Header("Placement & Visual")]
        [Tooltip("Khoảng cách phía sau camera (đứng sau Lanes & Nhân vật, nhưng trước Skybox/Gradient cũ)")]
        [SerializeField] private float distance = 55f;

        [Tooltip("Tỷ lệ phóng to để phủ kín màn hình")]
        [SerializeField] private float overscan = 1.12f;

        [Range(0.5f, 1.5f)]
        [SerializeField] private float brightness = 1.0f;

        [Range(0f, 0.5f)]
        [SerializeField] private float vignette = 0.12f;

        [Header("Editor Test")]
        [Tooltip("Khi Play trực tiếp từ GameplayScene trong Editor: 0 = Free Mode (mặc định), 1-4 = Test Chapter tương ứng")]
        [Range(0, 4)]
        [SerializeField] private int editorTestChapter = 1;

        // Runtime objects
        private Camera targetCamera;
        private GameObject quadObject;
        private MeshRenderer quadRenderer;
        private Material blendMaterial;
        private MaterialPropertyBlock propBlock;

        private VideoPlayer playerNoCombo;
        private VideoPlayer playerCombo;
        private RenderTexture rtNoCombo;
        private RenderTexture rtCombo;
        private RenderTexture rtLoopSnap;

        private BattleUIController battleUI;
        private JudgementSystem judgementSystem;
        private WorldBackgroundGradientPro defaultGradientPro;

        private int activeChapter = 0;
        private float currentComboBlend = 0f;
        private float targetComboBlend = 0f;
        private float currentLoopBlend = 0f;
        private float currentLoopPulse = 0f;
        private bool isStoryActive = false;
        private bool hasCapturedLoopFrame = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            propBlock = new MaterialPropertyBlock();

            // Auto-populate default video clips if empty
            PopulateDefaultConfigsIfEmpty();
        }

        private void Reset()
        {
            PopulateDefaultConfigsIfEmpty();
        }

        private void OnValidate()
        {
            if (chapterConfigs == null || chapterConfigs.Count == 0)
            {
                PopulateDefaultConfigsIfEmpty();
            }
        }

        public void PopulateDefaultConfigsIfEmpty()
        {
            if (chapterConfigs != null && chapterConfigs.Count >= 4) return;

            chapterConfigs = new List<ChapterVideoConfig>();

#if UNITY_EDITOR
            string basePath = "Assets/Luan/DiaLogTalkChapter/VideoBackground";
            string[] names = new string[] { "Concrete Dreams", "Club Null", "Sân Khấu Ánh Sao", "Xưởng Âm Thanh" };

            for (int i = 1; i <= 4; i++)
            {
                var cfg = new ChapterVideoConfig();
                cfg.chapterNumber = i;
                cfg.chapterName = $"Chapter {i}: {names[i - 1]}";

                string noComboPath = $"{basePath}/Chapter {i}/videobackground_chapter{i}_nocombo_gwr_video_mvp.mp4";
                string comboPath = $"{basePath}/Chapter {i}/videobackground_chapter{i}_combo_gwr_video_mvp.mp4";

                cfg.noComboClip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(noComboPath);
                cfg.comboClip = UnityEditor.AssetDatabase.LoadAssetAtPath<VideoClip>(comboPath);

                chapterConfigs.Add(cfg);
            }
#endif
        }

        private void Start()
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
                targetCamera = GetComponent<Camera>();

            defaultGradientPro = FindFirstObjectByType<WorldBackgroundGradientPro>();
            battleUI = FindFirstObjectByType<BattleUIController>();
            judgementSystem = FindFirstObjectByType<JudgementSystem>();

            // Determine if Free Mode or Chapter Mode
            activeChapter = DetectActiveChapter();

            if (activeChapter <= 0)
            {
                // Free Mode: Giữ nguyên background mặc định, không can thiệp
                isStoryActive = false;
                if (quadObject != null) quadObject.SetActive(false);
                Debug.Log("[ChapterVideoBackground] FREE MODE: Sử dụng background mặc định.");
                return;
            }

            // Chapter Mode (1 - 4): Khởi tạo background video cho chapter đó
            isStoryActive = true;
            Debug.Log($"[ChapterVideoBackground] STORY MODE: Kích hoạt video background cho Chapter {activeChapter}.");

            ChapterVideoConfig cfg = GetConfigForChapter(activeChapter);
            if (cfg == null || cfg.noComboClip == null)
            {
                Debug.LogWarning($"[ChapterVideoBackground] Không tìm thấy VideoClip cho Chapter {activeChapter}. Sử dụng background mặc định.");
                isStoryActive = false;
                return;
            }

            SetupVideoPlayers(cfg);
            SetupBackgroundQuad();

            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.onPause.AddListener(OnGamePaused);
                PauseManager.Instance.onResume.AddListener(OnGameResumed);
            }

            // Nếu có WorldBackgroundGradientPro, ẩn quad của nó để tối ưu hiệu năng
            if (defaultGradientPro != null)
            {
                Transform oldQuad = targetCamera != null ? targetCamera.transform.Find("WorldBackgroundGradientPro") : null;
                if (oldQuad != null)
                    oldQuad.gameObject.SetActive(false);
            }
        }

        private int DetectActiveChapter()
        {
            // 1. Vào từ Story Mode thật sự
            if (StorySession.Active && StorySession.Week != null && StorySession.Week.weekNumber > 0)
            {
                return StorySession.Week.weekNumber;
            }

            // 2. Vào từ Free Mode Song List (đã chọn bài nhưng StorySession không Active)
            if (GameSession.SelectedSong != null && !StorySession.Active)
            {
                return 0; // 0 = Free Mode
            }

            // 3. Test trực tiếp trong Unity Editor
            if (Application.isEditor)
            {
                // Nếu developer đặt editorTestChapter = 0 -> test Free Mode trong Editor
                if (editorTestChapter == 0)
                    return 0;

                if (Luan.Dialogue.StoryDialogueManager.Instance != null && Luan.Dialogue.StoryDialogueManager.Instance.TestInEditor)
                {
                    return Luan.Dialogue.StoryDialogueManager.Instance.GetCurrentChapterNumber();
                }
                return editorTestChapter;
            }

            return 0; // Mặc định Free Mode
        }

        private ChapterVideoConfig GetConfigForChapter(int chapterNum)
        {
            if (chapterConfigs == null) return null;
            foreach (var c in chapterConfigs)
            {
                if (c != null && c.chapterNumber == chapterNum)
                    return c;
            }
            return null;
        }

        private void SetupVideoPlayers(ChapterVideoConfig cfg)
        {
            // Tạo RenderTextures 1920x1080
            rtNoCombo = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            rtNoCombo.name = $"RT_NoCombo_Ch{activeChapter}";
            rtNoCombo.Create();

            rtCombo = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            rtCombo.name = $"RT_Combo_Ch{activeChapter}";
            rtCombo.Create();

            rtLoopSnap = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            rtLoopSnap.name = $"RT_LoopSnap_Ch{activeChapter}";
            rtLoopSnap.Create();

            // Setup VideoPlayer No-Combo
            GameObject goNoCombo = new GameObject($"VP_NoCombo_Ch{activeChapter}");
            goNoCombo.transform.SetParent(transform, false);
            playerNoCombo = goNoCombo.AddComponent<VideoPlayer>();
            ConfigureVideoPlayer(playerNoCombo, cfg.noComboClip, rtNoCombo);

            // Setup VideoPlayer High-Combo
            GameObject goCombo = new GameObject($"VP_Combo_Ch{activeChapter}");
            goCombo.transform.SetParent(transform, false);
            playerCombo = goCombo.AddComponent<VideoPlayer>();
            ConfigureVideoPlayer(playerCombo, cfg.comboClip != null ? cfg.comboClip : cfg.noComboClip, rtCombo);

            // Bắt đầu phát cả 2 video đồng bộ
            playerNoCombo.Play();
            playerCombo.Play();
        }

        private void ConfigureVideoPlayer(VideoPlayer vp, VideoClip clip, RenderTexture targetRt)
        {
            vp.playOnAwake = false;
            vp.clip = clip;
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = targetRt;
            vp.isLooping = true;
            vp.skipOnDrop = true;

            // MUTE AUDIO HOÀN TOÀN
            vp.audioOutputMode = VideoAudioOutputMode.None;
            for (ushort i = 0; i < vp.audioTrackCount; i++)
            {
                vp.EnableAudioTrack(i, false);
            }
        }

        private void SetupBackgroundQuad()
        {
            if (targetCamera == null) return;

            quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObject.name = $"ChapterVideoBackground_Quad_Ch{activeChapter}";
            Destroy(quadObject.GetComponent<Collider>());

            quadObject.transform.SetParent(targetCamera.transform, false);
            quadObject.transform.localRotation = Quaternion.identity;

            float height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * targetCamera.aspect;
            quadObject.transform.localPosition = new Vector3(0f, 0f, distance);
            quadObject.transform.localScale = new Vector3(width * overscan, height * overscan, 1f);

            quadRenderer = quadObject.GetComponent<MeshRenderer>();
            quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;

            Shader shader = Shader.Find("Custom/ChapterBackgroundBlend");
            if (shader == null)
            {
                Debug.LogWarning("[ChapterVideoBackground] Không tìm thấy shader 'Custom/ChapterBackgroundBlend', dùng fallback Unlit/Texture.");
                shader = Shader.Find("Unlit/Texture");
            }

            blendMaterial = new Material(shader);
            blendMaterial.SetTexture("_MainTex", rtNoCombo);
            blendMaterial.SetTexture("_ComboTex", rtCombo);
            blendMaterial.SetTexture("_LoopTex", rtLoopSnap);
            blendMaterial.SetFloat("_Blend", 0f);
            blendMaterial.SetFloat("_LoopBlend", 0f);
            blendMaterial.SetFloat("_LoopPulse", 0f);
            blendMaterial.SetFloat("_Brightness", brightness);
            blendMaterial.SetFloat("_Vignette", vignette);

            quadRenderer.sharedMaterial = blendMaterial;
        }

        private void Update()
        {
            if (!isStoryActive || quadRenderer == null || playerNoCombo == null) return;

            // 1. Kiểm tra Combo từ BattleUIController hoặc JudgementSystem
            int currentCombo = GetCurrentCombo();

            // Ngưỡng: 0 - 50 là no combo (0), trên 50 là có combo (1)
            targetComboBlend = (currentCombo > comboThreshold) ? 1f : 0f;

            // Chuyển cảnh mượt
            if (comboTransitionDuration > 0.01f)
            {
                currentComboBlend = Mathf.MoveTowards(currentComboBlend, targetComboBlend, Time.deltaTime / comboTransitionDuration);
            }
            else
            {
                currentComboBlend = targetComboBlend;
            }

            float smoothBlend = Mathf.SmoothStep(0f, 1f, currentComboBlend);

            // 2. Xử lý làm mượt Loop video
            UpdateLoopSmoothing();

            // 3. Cập nhật shader properties qua Material & MaterialPropertyBlock
            if (blendMaterial != null)
            {
                blendMaterial.SetFloat("_Blend", smoothBlend);
                blendMaterial.SetFloat("_LoopBlend", currentLoopBlend);
                blendMaterial.SetFloat("_LoopPulse", currentLoopPulse);
            }

            if (propBlock != null)
            {
                quadRenderer.GetPropertyBlock(propBlock);
                propBlock.SetFloat("_Blend", smoothBlend);
                propBlock.SetFloat("_LoopBlend", currentLoopBlend);
                propBlock.SetFloat("_LoopPulse", currentLoopPulse);
                quadRenderer.SetPropertyBlock(propBlock);
            }

            // 4. Đồng bộ kích thước quad nếu màn hình xoay / đổi aspect ratio
            UpdateQuadScale();
        }

        private int GetCurrentCombo()
        {
            if (battleUI != null)
                return battleUI.playerCombo;
            return 0;
        }

        private void UpdateLoopSmoothing()
        {
            if (!enableLoopSmoothing || !playerNoCombo.isPlaying) return;

            double duration = playerNoCombo.length;
            double time = playerNoCombo.time;

            if (duration <= 0.5) return;

            // Chụp snapshot frame đầu của video khi bắt đầu vòng lặp mới
            if (time < 0.15 && !hasCapturedLoopFrame && rtNoCombo != null && rtLoopSnap != null)
            {
                Graphics.Blit(rtNoCombo, rtLoopSnap);
                hasCapturedLoopFrame = true;
            }
            else if (time > 0.4)
            {
                hasCapturedLoopFrame = false;
            }

            // Tính toán loop blend
            currentLoopBlend = 0f;
            currentLoopPulse = 0f;

            double timeToEnd = duration - time;
            if (timeToEnd <= loopCrossfadeDuration && timeToEnd > 0.0)
            {
                // Gần kết thúc video: blend dần vào frame 0 của vòng lặp tiếp theo
                currentLoopBlend = Mathf.Clamp01(1f - (float)(timeToEnd / loopCrossfadeDuration));
            }
            else if (time < loopCrossfadeDuration * 0.7f)
            {
                // Vừa bắt đầu vòng lặp mới: nhả dần snapshot về video thực tế
                float tNorm = (float)(time / (loopCrossfadeDuration * 0.7f));
                currentLoopBlend = Mathf.Clamp01(1f - tNorm);
                currentLoopPulse = Mathf.Sin(tNorm * Mathf.PI) * loopPulseIntensity;
            }
        }

        private void UpdateQuadScale()
        {
            if (quadObject == null || targetCamera == null) return;

            float height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * targetCamera.aspect;
            quadObject.transform.localScale = new Vector3(width * overscan, height * overscan, 1f);
        }

        private void OnGamePaused()
        {
            if (playerNoCombo != null && playerNoCombo.isPlaying) playerNoCombo.Pause();
            if (playerCombo != null && playerCombo.isPlaying) playerCombo.Pause();
        }

        private void OnGameResumed()
        {
            if (playerNoCombo != null && isStoryActive) playerNoCombo.Play();
            if (playerCombo != null && isStoryActive) playerCombo.Play();
        }

        private void OnDestroy()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.onPause.RemoveListener(OnGamePaused);
                PauseManager.Instance.onResume.RemoveListener(OnGameResumed);
            }

            if (playerNoCombo != null)
            {
                playerNoCombo.Stop();
                Destroy(playerNoCombo.gameObject);
            }

            if (playerCombo != null)
            {
                playerCombo.Stop();
                Destroy(playerCombo.gameObject);
            }

            if (rtNoCombo != null)
            {
                rtNoCombo.Release();
                Destroy(rtNoCombo);
            }

            if (rtCombo != null)
            {
                rtCombo.Release();
                Destroy(rtCombo);
            }

            if (rtLoopSnap != null)
            {
                rtLoopSnap.Release();
                Destroy(rtLoopSnap);
            }

            if (blendMaterial != null)
            {
                Destroy(blendMaterial);
            }

            if (quadObject != null)
            {
                Destroy(quadObject);
            }
        }
    }
}
