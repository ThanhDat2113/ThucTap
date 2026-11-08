using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Luan.Dialogue
{
    public enum SpeakerSide
    {
        Left,   // Main 1 / Player
        Right   // Boss 1 / Opponent
    }

    public enum DialoguePhase
    {
        None,
        IntroVideo,
        Intro,
        Backstory,
        PreBattle,
        PostBattle
    }

    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName = "MAIN 1";
        public SpeakerSide side = SpeakerSide.Left;
        [TextArea(2, 5)]
        public string text = "";
        public Sprite customAvatar = null;
        public Color nameColor = new Color(0.2f, 0.95f, 1f, 1f);
        [Tooltip("Mô tả hành động của nhân vật theo thoại")]
        public string actionDescription = "";
        [Tooltip("Ảnh minh họa toàn cảnh / backstory cutscene")]
        public Sprite illustration = null;
    }

    [System.Serializable]
    public class SpeakerUI
    {
        public GameObject rootContainer;
        public CanvasGroup canvasGroup;
        public Image avatarFrame;
        public Image avatarImage;
        public GameObject nameTagContainer;
        public Image nameTagBg;
        public TMP_Text nameText;

        public void SetActiveState(bool isSpeaking, bool hideWhenInactive, Color activeColor)
        {
            if (rootContainer == null) return;

            if (hideWhenInactive)
            {
                rootContainer.SetActive(isSpeaking);
            }
            else
            {
                rootContainer.SetActive(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = isSpeaking ? 1f : 0.35f;
            }

            if (avatarFrame != null)
            {
                avatarFrame.color = isSpeaking ? activeColor : new Color(0.4f, 0.4f, 0.4f, 0.8f);
            }

            if (nameTagContainer != null)
            {
                nameTagContainer.SetActive(isSpeaking);
            }
        }
    }

    [System.Serializable]
    public class ChapterDialogueEntry
    {
        [Tooltip("Số thứ tự Chapter (1 = Week 1, 2 = Week 2, 3 = Week 3, 4 = Week 4...)")]
        public int chapterNumber = 1;
        [Tooltip("Tên hiển thị Chapter")]
        public string chapterTitle = "Chapter 1";
        [Tooltip("File Asset chứa dữ liệu của Chapter này")]
        public DialogueStoryData dialogueAsset;
        [Tooltip("Avatar đối thủ riêng của Chapter này (VD: Boss 1, Boss 2 / DJ Null)")]
        public Sprite opponentAvatar;
        [Tooltip("Tên mặc định của đối thủ")]
        public string defaultOpponentName = "BOSS";
    }

    /// <summary>
    /// Điều phối hệ thống hội thoại (Story Dialogue) trước và sau trận đấu.
    /// - Hỗ trợ màn hình Intro giới thiệu Chapter Logo & cốt truyện nhân vật trước trận.
    /// - Hỗ trợ riêng biệt 2 nhân vật (Left: Main 1 / Player, Right: Boss 1 / Raze, Boss 2,...).
    /// - Hỗ trợ ĐA CHAPTER: Tự động chạy đúng hội thoại theo Week/Chapter người chơi chọn.
    /// - Tự động chạy lời thoại trước trận (Pre-Battle) -> Gameplay -> lời thoại sau trận (Post-Battle).
    /// </summary>
    public class StoryDialogueManager : MonoBehaviour
    {
        public static StoryDialogueManager Instance { get; private set; }

        // ★ Thêm: theo dõi xem dialogue đang chạy hay không (để GameManager không start gameplay lẫn vào)
        public bool IsPlaying { get; private set; }
        public bool LastBattleWon { get; private set; } = true;

        private bool hasCompletedDialogueThisSession = false;
        private bool hasCompletedPostBattleThisSession = false;

        [Header("Chapter Intro Video (~8s)")]
        [SerializeField] private GameObject videoContainer;
        [SerializeField] private CanvasGroup videoCanvasGroup;
        [SerializeField] private UnityEngine.UI.RawImage videoRawImage;
        [SerializeField] private UnityEngine.Video.VideoPlayer videoPlayer;
        [SerializeField] private UnityEngine.UI.Button videoSkipButton;
        [SerializeField] private TMPro.TMP_Text videoSkipText;
        private RenderTexture videoRenderTexture;
        private Coroutine videoRoutine;
        private bool skipVideoRequested;

        [Header("Tùy Chỉnh Kích Thước Ảnh Backstory (Layout Settings)")]
        [Tooltip("Tỉ lệ phóng to/thu nhỏ ảnh Backstory (1.0 = 100% full viền, 0.88 = 88% vừa vặn có lề viền đẹp)")]
        [Range(0.5f, 1.5f)]
        [SerializeField] private float backstoryScale = 0.88f;
        [Tooltip("Dịch chuyển vị trí ảnh lên / xuống theo trục Y (Pixel)")]
        [SerializeField] private float backstoryOffsetY = 0f;

        [Header("Backstory Transition & Pulse Prompt")]
        private List<Sprite> activeBackstoryImages = new List<Sprite>();
        [SerializeField] private CanvasGroup backstoryContentCanvasGroup;
        [SerializeField] private TMPro.TMP_Text backstoryFirstSlidePromptText;
        private Coroutine backstorySequenceRoutine;
        private Coroutine backstoryPromptPulseRoutine;
        private bool isWaitingForFirstSlideTap;
        private bool slideTapRequested;

        [Header("Chế độ kiểm thử")]
        [Tooltip("Bật để luôn chạy hội thoại khi bấm Play trực tiếp tại GameplayScene")]
        [SerializeField] private bool testInEditor = false;
        [Tooltip("Chapter chạy khi test trong Editor (1 = Chapter 1, 2 = Chapter 2,...)")]
        [SerializeField] private int testChapter = 1;

        [Header("Tuỳ chọn hiển thị")]
        [Tooltip("Ẩn hoàn toàn nhân vật không nói (nếu tắt thì chỉ làm mờ 35%)")]
        [SerializeField] private bool hideInactiveSpeaker = false;

        [Header("Canvas & Khung thoại")]
        [SerializeField] private Canvas dialogueCanvas;
        [SerializeField] private CanvasGroup mainCanvasGroup;
        [SerializeField] private RectTransform dialogueContainer;
        [SerializeField] private Image dimOverlay;
        [SerializeField] private Image dialogueBoxBg;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Button clickAreaButton;
        [SerializeField] private GameObject nextIndicator;

        [Header("Intro Cốt Truyện & Nhân Vật")]
        [SerializeField] private GameObject introContainer;
        [SerializeField] private CanvasGroup introCanvasGroup;
        [SerializeField] private GameObject topBanner;
        [SerializeField] private CanvasGroup logoCanvasGroup;
        [SerializeField] private Image chapterLogoImage;
        [SerializeField] private TMP_Text chapterTitleText;
        [SerializeField] private GameObject characterStoryCard;
        [SerializeField] private CanvasGroup storyCardCanvasGroup;
        [SerializeField] private TMP_Text characterStoryTitleText;
        [SerializeField] private TMP_Text characterStoryBodyText;
        [SerializeField] private Image characterIntroArtImage;
        [SerializeField] private Button tapToContinueButton;
        [SerializeField] private TMP_Text tapPromptText;
        [Tooltip("Thời gian hiển thị Logo chương (giây) trước khi fade nhẹ đi để hiện bảng câu chuyện")]
        [SerializeField] private float logoDisplayDuration = 3.0f;
        [Tooltip("Độ tối của overlay nền (0 = trong suốt, 1 = đen kịt). Giữ khoảng 0.6 - 0.65 để không đè tối mất khung cảnh")]
        [SerializeField] private float dimOverlayAlpha = 0.65f;

        [Header("Backstory Minh Họa & Cutscenes")]
        [SerializeField] private GameObject backstoryContainer;
        [SerializeField] private CanvasGroup backstoryCanvasGroup;
        [SerializeField] private Image backstoryIllustrationImage;
        [SerializeField] private GameObject backstorySubtitlePanel;
        [SerializeField] private TMP_Text backstorySpeakerText;
        [SerializeField] private TMP_Text backstoryActionText;
        [SerializeField] private TMP_Text backstoryDialogueText;
        [SerializeField] private TMP_Text backstorySlideCounterText;
        [SerializeField] private GameObject backstoryNextIndicator;
        [SerializeField] private Button backstoryTapButton;

        [Header("Speaker Bên Trái (Main 1 / Player)")]
        [SerializeField] private SpeakerUI leftSpeaker = new SpeakerUI();

        [Header("Speaker Bên Phải (Boss)")]
        [SerializeField] private SpeakerUI rightSpeaker = new SpeakerUI();

        [Header("Avatar Mặc Định")]
        [SerializeField] private Sprite playerAvatarSprite;
        [SerializeField] private Sprite opponentAvatarSprite;

        [Header("Nút Điều Khiển")]
        [SerializeField] private Button skipButton;
        [SerializeField] private Button autoButton;
        [SerializeField] private Image autoButtonGlow;

        [Header("Quản Lý Multi-Chapter")]
        [Tooltip("Danh sách cấu hình hội thoại cho từng Chapter")]
        [SerializeField] private List<ChapterDialogueEntry> chapters = new List<ChapterDialogueEntry>();

        [Header("Dữ Liệu Thoại Dự Phòng")]
        [Tooltip("Asset lưu dữ liệu thoại (dự phòng nếu không dùng danh sách chapters)")]
        [SerializeField] private DialogueStoryData dialogueDataAsset;
        [SerializeField] private List<DialogueLine> dialogueLines = new List<DialogueLine>();

        [Header("Âm Thanh")]
        [SerializeField] private AudioClip typewriterClip;
        [SerializeField] private AudioClip nextClip;
        [SerializeField] private AudioClip skipClip;

        [Header("Cấu Hình Tốc Độ")]
        [SerializeField] private float typingSpeed = 0.025f;
        [SerializeField] private float autoAdvanceDelay = 2.2f;

        private DialoguePhase currentPhase = DialoguePhase.None;
        private int currentLineIndex = -1;
        private bool isTyping = false;
        private bool isAutoMode = false;
        private Coroutine typingRoutine;
        private Coroutine transitionRoutine;
        private Coroutine autoRoutine;
        private Coroutine promptPulseRoutine;
        private Coroutine introSequenceRoutine;
        private List<BackstorySlide> activeBackstorySlides;
        private int currentBackstoryIndex = -1;

        private enum IntroStep
        {
            None,
            LogoShowcase,
            StoryCardShowcase
        }
        private IntroStep currentIntroStep = IntroStep.None;
        private bool skipLogoRequested = false;

        public List<ChapterDialogueEntry> Chapters => chapters;
        public int TestChapter { get => testChapter; set => testChapter = value; }
        public DialoguePhase CurrentPhase => currentPhase;

        public int GetCurrentChapterNumber()
        {
            if (StorySession.Active && StorySession.Week != null && StorySession.Week.weekNumber > 0)
                return StorySession.Week.weekNumber;
            return testChapter > 0 ? testChapter : 1;
        }

        public ChapterDialogueEntry GetCurrentChapterConfig()
        {
            int currentNum = GetCurrentChapterNumber();
            if (chapters != null && chapters.Count > 0)
            {
                foreach (var entry in chapters)
                {
                    if (entry != null && entry.chapterNumber == currentNum)
                        return entry;
                }
            }
            return null;
        }

        public Sprite GetActiveOpponentAvatar()
        {
            var cfg = GetCurrentChapterConfig();
            if (cfg != null && cfg.opponentAvatar != null)
                return cfg.opponentAvatar;
            return opponentAvatarSprite;
        }

        public string GetActiveOpponentName()
        {
            var cfg = GetCurrentChapterConfig();
            if (cfg != null && !string.IsNullOrEmpty(cfg.defaultOpponentName))
                return cfg.defaultOpponentName;
            return "Raze";
        }

        public List<DialogueLine> GetPreBattleLines()
        {
            var cfg = GetCurrentChapterConfig();
            if (cfg != null && cfg.dialogueAsset != null && cfg.dialogueAsset.preBattleLines != null && cfg.dialogueAsset.preBattleLines.Count > 0)
                return cfg.dialogueAsset.preBattleLines;

            if (dialogueDataAsset != null && dialogueDataAsset.preBattleLines != null && dialogueDataAsset.preBattleLines.Count > 0)
                return dialogueDataAsset.preBattleLines;

            if (dialogueDataAsset != null && dialogueDataAsset.lines != null && dialogueDataAsset.lines.Count > 0)
                return dialogueDataAsset.lines;

            return dialogueLines;
        }

        public List<DialogueLine> GetPostBattleLines(bool isWin)
        {
            var cfg = GetCurrentChapterConfig();
            var data = cfg != null ? cfg.dialogueAsset : dialogueDataAsset;

            if (data != null)
            {
                if (isWin)
                {
                    var win = data.GetWinLines();
                    if (win != null && win.Count > 0) return win;
                }
                else
                {
                    var lose = data.GetLoseLines();
                    if (lose != null && lose.Count > 0) return lose;
                }
            }

            if (isWin)
            {
                if (dialogueDataAsset != null && dialogueDataAsset.postBattleLines != null && dialogueDataAsset.postBattleLines.Count > 0)
                    return dialogueDataAsset.postBattleLines;
            }
            else
            {
                return GetDefaultDefeatLines();
            }

            return null;
        }

        public List<DialogueLine> GetPostBattleLines()
        {
            return GetPostBattleLines(LastBattleWon);
        }

        private List<DialogueLine> GetDefaultDefeatLines()
        {
            string oppName = GetActiveOpponentName();
            Sprite oppAvatar = GetActiveOpponentAvatar();
            string pName = GetPlayerDisplayName();
            Sprite pAvatar = PlayerAvatarSprite;

            return new List<DialogueLine>
            {
                new DialogueLine
                {
                    speakerName = oppName,
                    side = SpeakerSide.Right,
                    text = "Chỉ có thế thôi sao? Cậu vẫn chưa theo kịp nhịp của tôi đâu.",
                    customAvatar = oppAvatar,
                    nameColor = new Color(1.0f, 0.2f, 0.85f, 1f)
                },
                new DialogueLine
                {
                    speakerName = pName,
                    side = SpeakerSide.Left,
                    text = "Chưa xong đâu... Tôi sẽ quay lại và lấy chiếc huy hiệu đó!",
                    customAvatar = pAvatar,
                    nameColor = new Color(0.2f, 0.95f, 1f, 1f)
                }
            };
        }

        public List<DialogueLine> DialogueLines
        {
            get
            {
                if (currentPhase == DialoguePhase.PostBattle)
                {
                    var post = GetPostBattleLines();
                    if (post != null && post.Count > 0) return post;
                }
                return GetPreBattleLines();
            }
        }

        public DialogueStoryData DialogueDataAsset { get => dialogueDataAsset; set => dialogueDataAsset = value; }
        public Sprite PlayerAvatarSprite { get => playerAvatarSprite; set => playerAvatarSprite = value; }
        public Sprite OpponentAvatarSprite { get => opponentAvatarSprite; set => opponentAvatarSprite = value; }
        public SpeakerUI LeftSpeaker => leftSpeaker;
        public SpeakerUI RightSpeaker => rightSpeaker;

        public static bool HasPendingDialogue()
        {
            if (Instance == null)
            {
                Instance = FindFirstObjectByType<StoryDialogueManager>();
            }

            if (Instance != null && Instance.hasCompletedDialogueThisSession)
                return false;

            // 1. Chế độ kiểm thử trong Editor: Nếu testInEditor bật -> LUÔN CHẠY thoại Story!
            if (Application.isEditor && Instance != null && Instance.testInEditor)
                return true;

            // 2. Nếu đang trong Story Mode (StorySession.Active) -> LUÔN CHẠY nếu chưa xong trong lượt này!
            if (StorySession.Active)
                return true;

            // 3. Trong Editor nếu chưa chọn bài -> Chạy thoại test
            if (Application.isEditor && GameSession.SelectedSong == null)
                return true;

            // 4. Nếu người chơi vào từ Free Play -> Không chạy thoại Story
            return false;
        }

        public bool HasPendingPostBattleDialogue(bool isWin)
        {
            if (hasCompletedPostBattleThisSession)
                return false;

            // Nếu người chơi vào từ Free Play -> Không chạy thoại sau trận
            if (!StorySession.Active && GameSession.SelectedSong != null)
                return false;

            bool isStory = StorySession.Active;
            if (testInEditor && Application.isEditor) isStory = true;
            if (GameSession.SelectedSong == null && Application.isEditor) isStory = true;

            if (!isStory) return false;

            var post = GetPostBattleLines(isWin);
            return post != null && post.Count > 0;
        }

        public bool HasPendingPostBattleDialogue()
        {
            return HasPendingPostBattleDialogue(LastBattleWon);
        }

        public static void ResetSessionState()
        {
            if (Instance != null)
            {
                Instance.hasCompletedDialogueThisSession = false;
                Instance.hasCompletedPostBattleThisSession = false;
            }
        }

        private void OnEnable()
        {
            hasCompletedDialogueThisSession = false;
            hasCompletedPostBattleThisSession = false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            hasCompletedDialogueThisSession = false;
            hasCompletedPostBattleThisSession = false;
            currentPhase = DialoguePhase.None;

            EnsureUI();
            EnsureBackstoryUI();
            EnsureVideoUI();
            HideAllUI();

            // Nếu đang trong Story Mode / có Pending Dialogue, vô hiệu hoá hoàn toàn SongIntroScreen
            if (HasPendingDialogue())
            {
                var songIntro = FindFirstObjectByType<RhythmGame.SongIntroScreen>();
                if (songIntro != null)
                {
                    songIntro.IsPlaying = false;
                    songIntro.gameObject.SetActive(false);
                }
                var songIntroGo = GameObject.Find("SongIntroCanvas");
                if (songIntroGo != null)
                {
                    songIntroGo.SetActive(false);
                }
            }

            BindControlsFallback();
            HookEvents();

            if (leftSpeaker != null && leftSpeaker.nameText != null)
            {
                leftSpeaker.nameText.text = GetPlayerDisplayName();
            }
        }

        private void Start()
        {
            SubscribeGameManager();

            if (HasPendingDialogue())
            {
                // Story Mode: Tắt hoàn toàn SongIntroScreen nếu có trong scene để không bị đè và không chạy countdown bài hát
                if (RhythmGame.SongIntroScreen.Instance != null)
                {
                    RhythmGame.SongIntroScreen.Instance.IsPlaying = false;
                    RhythmGame.SongIntroScreen.Instance.gameObject.SetActive(false);
                }

                StartDialogue();
            }
            else
            {
                // Không có pending dialogue (Free Play hoặc Chapter đã hoàn thành thoại)
                HideAllUI();
            }
        }

        /// <summary>
        /// Duoc goi tu SongIntroScreen sau khi intro ket thuc.
        /// - Co pending dialogue -> chay dialogue (Story Mode).
        /// - Khong co -> bat dau gameplay ngay (Free Play hoac Chapter khong co thoai).
        /// </summary>
        public void BeginStoryFlow()
        {
            if (HasPendingDialogue())
            {
                Debug.Log("[StoryDialogueManager] BeginStoryFlow -> StartDialogue().");
                StartDialogue();
            }
            else
            {
                Debug.Log("[StoryDialogueManager] BeginStoryFlow -> khong co dialogue, vao gameplay.");
                StartGameplay();
            }
        }


        // ==========================================
        // CHAPTER INTRO VIDEO PLAYBACK (~8s)
        // ==========================================
        private TMPro.TMP_FontAsset ResolveFont()
        {
            if (dialogueText != null && dialogueText.font != null) return dialogueText.font;
            return TMPro.TMP_Settings.defaultFontAsset;
        }

        public void EnsureVideoUI()
        {
            if (videoContainer != null && videoPlayer != null && videoRawImage != null)
                return;

            if (dialogueCanvas == null)
            {
                EnsureUI();
            }
            if (dialogueCanvas == null) return;

            var existing = dialogueCanvas.transform.Find("ChapterVideoContainer");
            if (existing != null)
            {
                videoContainer = existing.gameObject;
                videoCanvasGroup = existing.GetComponent<CanvasGroup>() ?? existing.gameObject.AddComponent<CanvasGroup>();
                var raw = existing.Find("VideoRawImage");
                if (raw != null) videoRawImage = raw.GetComponent<UnityEngine.UI.RawImage>();
                videoPlayer = existing.GetComponent<UnityEngine.Video.VideoPlayer>() ?? existing.gameObject.AddComponent<UnityEngine.Video.VideoPlayer>();
                var skip = existing.Find("VideoSkipButton");
                if (skip != null) videoSkipButton = skip.GetComponent<UnityEngine.UI.Button>();
                return;
            }

            // Tạo khung phát video tự động phủ kín 1080p
            var vGo = new GameObject("ChapterVideoContainer", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup));
            vGo.transform.SetParent(dialogueCanvas.transform, false);
            var vRt = vGo.GetComponent<RectTransform>();
            vRt.anchorMin = Vector2.zero;
            vRt.anchorMax = Vector2.one;
            vRt.offsetMin = Vector2.zero;
            vRt.offsetMax = Vector2.zero;
            videoContainer = vGo;
            videoCanvasGroup = vGo.GetComponent<CanvasGroup>();

            // Nền đen tuyệt đối phía sau video
            var bgImg = vGo.GetComponent<UnityEngine.UI.Image>() ?? vGo.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = Color.black;
            bgImg.raycastTarget = false;

            // RawImage hiển thị texture video (tự động giữ tỉ lệ 16:9, căn giữa màn hình không méo)
            var rawGo = new GameObject("VideoRawImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.RawImage), typeof(UnityEngine.UI.AspectRatioFitter));
            rawGo.transform.SetParent(vGo.transform, false);
            var rawRt = rawGo.GetComponent<RectTransform>();
            rawRt.anchorMin = Vector2.zero;
            rawRt.anchorMax = Vector2.one;
            rawRt.offsetMin = Vector2.zero;
            rawRt.offsetMax = Vector2.zero;
            videoRawImage = rawGo.GetComponent<UnityEngine.UI.RawImage>();
            videoRawImage.color = Color.black;

            var arf = rawGo.GetComponent<UnityEngine.UI.AspectRatioFitter>();
            arf.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = 16f / 9f;

            // VideoPlayer component
            videoPlayer = vGo.AddComponent<UnityEngine.Video.VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;

            // Nút Bỏ Qua Video góc trên bên phải
            var skipGo = new GameObject("VideoSkipButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            skipGo.transform.SetParent(vGo.transform, false);
            var skipRt = skipGo.GetComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1f, 1f);
            skipRt.anchorMax = new Vector2(1f, 1f);
            skipRt.pivot = new Vector2(1f, 1f);
            skipRt.anchoredPosition = new Vector2(-40f, -40f);
            skipRt.sizeDelta = new Vector2(160f, 44f);

            var skipImg = skipGo.GetComponent<UnityEngine.UI.Image>();
            skipImg.color = new Color(0.08f, 0.08f, 0.15f, 0.85f);

            videoSkipButton = skipGo.GetComponent<UnityEngine.UI.Button>();
            videoSkipButton.onClick.AddListener(() => skipVideoRequested = true);

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            txtGo.transform.SetParent(skipGo.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var tmp = txtGo.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "BỎ QUA ▶";
            tmp.font = ResolveFont();
            tmp.fontSize = 18f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.95f, 1f, 0.9f);
            videoSkipText = tmp;

            vGo.SetActive(false);
        }

        public void PlayChapterIntroVideo(DialogueStoryData data, System.Action onComplete)
        {
            if (videoRoutine != null) StopCoroutine(videoRoutine);
            videoRoutine = StartCoroutine(RunChapterIntroVideo(data, onComplete));
        }

        private IEnumerator RunChapterIntroVideo(DialogueStoryData data, System.Action onComplete)
        {
            EnsureVideoUI();

            if (videoContainer == null || videoPlayer == null || data == null ||
                (data.introVideoClip == null && string.IsNullOrEmpty(data.introVideoUrl)))
            {
                onComplete?.Invoke();
                yield break;
            }

            currentPhase = DialoguePhase.IntroVideo;
            skipVideoRequested = false;

            // ★ QUAN TRỌNG: Kích hoạt dialogueCanvas và mainCanvasGroup để video hiển thị!
            if (dialogueCanvas != null)
            {
                dialogueCanvas.gameObject.SetActive(true);
                dialogueCanvas.sortingOrder = 600;
            }
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 1f;
                mainCanvasGroup.blocksRaycasts = true;
                mainCanvasGroup.interactable = true;
            }

            int width = 1920;
            int height = 1080;
            if (data.introVideoClip != null && data.introVideoClip.width > 0)
            {
                width = (int)data.introVideoClip.width;
                height = (int)data.introVideoClip.height;
            }

            CleanupVideoRenderTexture();
            videoRenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = $"RT_ChapterVideo_{data.chapterNumber}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            videoRenderTexture.Create();

            videoPlayer.targetTexture = videoRenderTexture;
            if (videoRawImage != null)
            {
                videoRawImage.texture = videoRenderTexture;
                videoRawImage.color = Color.white;
                var arf = videoRawImage.GetComponent<UnityEngine.UI.AspectRatioFitter>() ?? videoRawImage.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                arf.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent;
                arf.aspectRatio = height > 0 ? (float)width / height : (16f / 9f);
            }

            if (data.introVideoClip != null)
            {
                videoPlayer.source = UnityEngine.Video.VideoSource.VideoClip;
                videoPlayer.clip = data.introVideoClip;
            }
            else
            {
                videoPlayer.source = UnityEngine.Video.VideoSource.Url;
                videoPlayer.url = data.introVideoUrl;
            }

            videoPlayer.isLooping = false;
            // Mute hoàn toàn âm thanh video intro theo yêu cầu người dùng
            videoPlayer.audioOutputMode = UnityEngine.Video.VideoAudioOutputMode.None;
            for (ushort t = 0; t < videoPlayer.audioTrackCount; t++)
            {
                videoPlayer.EnableAudioTrack(t, false);
            }
            if (videoContainer != null)
            {
                var vAudio = videoContainer.GetComponent<AudioSource>();
                if (vAudio != null) { vAudio.mute = true; vAudio.Stop(); }
            }

            videoPlayer.Prepare();

            // Ẩn tất cả các container khác khi chạy intro video để không che đè
            if (introContainer != null) introContainer.SetActive(false);
            if (characterStoryCard != null) characterStoryCard.SetActive(false);
            if (backstoryContainer != null) backstoryContainer.SetActive(false);
            if (dialogueContainer != null) dialogueContainer.gameObject.SetActive(false);

            if (videoContainer != null)
            {
                videoContainer.SetActive(true);
                videoContainer.transform.SetAsLastSibling();
            }
            if (videoCanvasGroup != null) videoCanvasGroup.alpha = 0f;

            // Đảm bảo nút Bỏ qua ở góc trên bên phải (của dialog) luôn hiển thị trên video
            if (topControlsGroup != null)
            {
                topControlsGroup.SetActive(true);
                topControlsGroup.transform.SetAsLastSibling();
            }
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(true);
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(OnSkipClicked);
            }

            float waitPrepare = 0f;
            while (!videoPlayer.isPrepared && waitPrepare < 3.0f && !skipVideoRequested)
            {
                waitPrepare += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!skipVideoRequested)
            {
                videoPlayer.Play();

                // Fade In Video (0.35s)
                float fadeIn = 0.35f;
                float el = 0f;
                while (el < fadeIn)
                {
                    el += Time.unscaledDeltaTime;
                    if (videoCanvasGroup != null) videoCanvasGroup.alpha = Mathf.Clamp01(el / fadeIn);
                    yield return null;
                }
                if (videoCanvasGroup != null) videoCanvasGroup.alpha = 1f;

                float maxDuration = data.introVideoDuration > 0f ? data.introVideoDuration : 8f;
                if (data.introVideoClip != null && data.introVideoClip.length > 0)
                {
                    maxDuration = Mathf.Min(maxDuration, (float)data.introVideoClip.length);
                }

                float timer = 0f;
                while (timer < maxDuration && !skipVideoRequested && videoPlayer.isPlaying)
                {
                    timer += Time.unscaledDeltaTime;
                    if (IsSkipActionPressed() || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
                    {
                        skipVideoRequested = true;
                        break;
                    }
                    yield return null;
                }
            }

            // Fade Out Video (0.35s)
            float fadeOut = 0.35f;
            float elOut = 0f;
            while (elOut < fadeOut)
            {
                elOut += Time.unscaledDeltaTime;
                if (videoCanvasGroup != null) videoCanvasGroup.alpha = 1f - Mathf.Clamp01(elOut / fadeOut);
                yield return null;
            }

            if (videoPlayer != null) videoPlayer.Stop();
            if (videoContainer != null) videoContainer.SetActive(false);
            CleanupVideoRenderTexture();

            videoRoutine = null;
            currentPhase = DialoguePhase.None;

            onComplete?.Invoke();
        }

        private void CleanupVideoRenderTexture()
        {
            if (videoRenderTexture != null)
            {
                if (videoRenderTexture.IsCreated()) videoRenderTexture.Release();
                Destroy(videoRenderTexture);
                videoRenderTexture = null;
            }
        }

        private void OnDestroy()
        {
            if (backstorySequenceRoutine != null) { StopCoroutine(backstorySequenceRoutine); backstorySequenceRoutine = null; }
            if (backstoryPromptPulseRoutine != null) { StopCoroutine(backstoryPromptPulseRoutine); backstoryPromptPulseRoutine = null; }
            CleanupVideoRenderTexture();
            if (Instance == this) Instance = null;

            var gm = RhythmGame.GameManager.Instance ?? FindFirstObjectByType<RhythmGame.GameManager>();
            if (gm != null)
            {
                gm.OnStateChanged -= HandleGameStateChanged;
            }
        }

        private void SubscribeGameManager()
        {
            var gm = RhythmGame.GameManager.Instance ?? FindFirstObjectByType<RhythmGame.GameManager>();
            if (gm != null)
            {
                gm.OnStateChanged -= HandleGameStateChanged;
                gm.OnStateChanged += HandleGameStateChanged;
            }
        }

        private void HandleGameStateChanged(RhythmGame.GameState state)
        {
            if (state == RhythmGame.GameState.Finished)
            {
                LastBattleWon = true;
                if (HasPendingPostBattleDialogue(true))
                {
                    StartCoroutine(DelayedStartPostBattle(true));
                }
            }
            else if (state == RhythmGame.GameState.Failed)
            {
                LastBattleWon = false;
                if (HasPendingPostBattleDialogue(false))
                {
                    StartCoroutine(DelayedStartPostBattle(false));
                }
            }
        }

        private IEnumerator DelayedStartPostBattle(bool isWin)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            StartPostBattleDialogue(isWin);
        }

        private void HideAllUI()
        {
            if (introSequenceRoutine != null) { StopCoroutine(introSequenceRoutine); introSequenceRoutine = null; }
            if (autoRoutine != null) { StopCoroutine(autoRoutine); autoRoutine = null; }
            if (promptPulseRoutine != null) { StopCoroutine(promptPulseRoutine); promptPulseRoutine = null; }

            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0f;
                mainCanvasGroup.blocksRaycasts = false;
                mainCanvasGroup.interactable = false;
            }
            if (topBanner != null) topBanner.SetActive(false);
            if (characterStoryCard != null) characterStoryCard.SetActive(false);
            if (introContainer != null) introContainer.SetActive(false);
            if (backstoryContainer != null) backstoryContainer.SetActive(false);
            if (dialogueContainer != null) dialogueContainer.gameObject.SetActive(false);
            if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(false);
        }

        public void ApplyBackstoryLayout()
        {
            if (backstoryIllustrationImage != null)
            {
                var rt = backstoryIllustrationImage.rectTransform;
                rt.localScale = new Vector3(backstoryScale, backstoryScale, 1f);
                rt.anchoredPosition = new Vector2(0f, backstoryOffsetY);
            }
        }

        private void Update()
        {
            if (currentPhase == DialoguePhase.None) return;

#if UNITY_EDITOR
            if (currentPhase == DialoguePhase.Backstory)
            {
                ApplyBackstoryLayout();
            }
#endif

            if (IsSkipActionPressed())
            {
                OnSkipClicked();
                return;
            }

            if (IsAutoActionPressed())
            {
                OnAutoToggleClicked();
                return;
            }

            if (currentPhase == DialoguePhase.Intro)
            {
                if (IsNextActionPressed())
                {
                    OnTapToContinueClicked();
                }
            }
            else if (currentPhase == DialoguePhase.Backstory)
            {
                if (IsNextActionPressed())
                {
                    OnBackstoryTapClicked();
                }
            }
            else if (currentPhase == DialoguePhase.PreBattle || currentPhase == DialoguePhase.PostBattle)
            {
                if (IsNextActionPressed())
                {
                    OnDialogueAreaClicked();
                }
            }
        }

        private bool IsNextActionPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                return true;
#endif
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
        }

        private bool IsSkipActionPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.tabKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
                return true;
#endif
            return Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Escape);
        }

        private bool IsAutoActionPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.aKey.wasPressedThisFrame)
                return true;
#endif
            return Input.GetKeyDown(KeyCode.A);
        }

        private void BindControlsFallback()
        {
            if (dialogueCanvas == null)
            {
                var existingCanvas = GameObject.Find("StoryDialogueCanvas");
                if (existingCanvas != null) dialogueCanvas = existingCanvas.GetComponent<Canvas>();
            }

            if (dialogueCanvas != null)
            {
                if (topControlsGroup == null)
                {
                    var tc = dialogueCanvas.transform.Find("TopControls");
                    if (tc != null) topControlsGroup = tc.gameObject;
                }
                if (skipButton == null)
                {
                    var s = dialogueCanvas.transform.Find("TopControls/SkipButton");
                    if (s != null) skipButton = s.GetComponent<Button>();
                }
                if (autoButton == null)
                {
                    var a = dialogueCanvas.transform.Find("TopControls/AutoButton");
                    if (a != null)
                    {
                        autoButton = a.GetComponent<Button>();
                        autoButtonGlow = a.GetComponent<Image>();
                    }
                }
            }
        }

        private void HookEvents()
        {
            if (clickAreaButton != null)
            {
                clickAreaButton.onClick.RemoveAllListeners();
                clickAreaButton.onClick.AddListener(OnDialogueAreaClicked);
            }
            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(OnSkipClicked);
            }
            if (autoButton != null)
            {
                autoButton.onClick.RemoveAllListeners();
                autoButton.onClick.AddListener(OnAutoToggleClicked);
            }
            if (tapToContinueButton != null)
            {
                tapToContinueButton.onClick.RemoveAllListeners();
                tapToContinueButton.onClick.AddListener(OnTapToContinueClicked);
            }
        }

        public void StartDialogue()
        {
            IsPlaying = true;

            // Kích hoạt StoryDialogueCanvas ngay khi bắt đầu luồng cốt truyện
            if (dialogueCanvas != null)
            {
                dialogueCanvas.gameObject.SetActive(true);
                dialogueCanvas.sortingOrder = 600;
            }
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 1f;
                mainCanvasGroup.blocksRaycasts = true;
                mainCanvasGroup.interactable = true;
            }

            var cfg = GetCurrentChapterConfig();
            var data = cfg != null ? cfg.dialogueAsset : dialogueDataAsset;

            // 1. Kiểm tra phát Intro Video cho Chapter (~8s) nếu có cấu hình video
            bool hasVideo = data != null && (data.introVideoClip != null || !string.IsNullOrEmpty(data.introVideoUrl));
            if (hasVideo)
            {
                PlayChapterIntroVideo(data, () =>
                {
                    ProceedToBackstoryOrDialogue(data);
                });
            }
            else
            {
                ProceedToBackstoryOrDialogue(data);
            }
        }

        private void ProceedToBackstoryOrDialogue(DialogueStoryData data)
        {
            // 2. Thoại đầu dùng Backstory (ảnh minh họa đã vẽ sẵn thoại) thay cho dialog box thoại đầu
            var images = data != null ? data.GetBackstorySprites() : null;
            if (images != null && images.Count > 0)
            {
                EnsureUI();
                ShowBackstoryScreen(images);
                return;
            }

            // 3. Fallback: Nếu không có ảnh backstory nhưng có preBattleLines cũ
            var preLines = GetPreBattleLines();
            if (preLines != null && preLines.Count > 0)
            {
                StartPreBattleDialogue();
                return;
            }

            // Nếu không có gì thì vào gameplay ngay
            StartGameplay();
        }

        public void ShowIntroScreen()
        {
            IsPlaying = true;
            currentPhase = DialoguePhase.Intro;
            currentIntroStep = IntroStep.None;
            skipLogoRequested = false;

            if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(true);
            if (dialogueContainer != null) dialogueContainer.gameObject.SetActive(false);
            if (topControlsGroup != null) topControlsGroup.SetActive(true);
            UpdateAutoButtonVisual();

            EnsureIntroReferences();
            HookEvents();

            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 1f;
                mainCanvasGroup.blocksRaycasts = true;
                mainCanvasGroup.interactable = true;
            }

            if (introContainer != null)
            {
                introContainer.SetActive(true);
                if (introCanvasGroup != null)
                {
                    introCanvasGroup.alpha = 1f;
                    introCanvasGroup.blocksRaycasts = true;
                    introCanvasGroup.interactable = true;
                }

                var cfg = GetCurrentChapterConfig();
                var data = cfg != null ? cfg.dialogueAsset : dialogueDataAsset;

                // 1. Chapter Logo (Khởi tạo alpha = 0 để Fade In, GIỮ NGUYÊN 100% SCALE TỪ EDIT MODE)
                Sprite logoSpr = data != null ? data.chapterLogo : null;
                if (chapterLogoImage != null)
                {
                    if (logoSpr != null)
                    {
                        chapterLogoImage.sprite = logoSpr;
                        Color c = chapterLogoImage.color;
                        chapterLogoImage.color = new Color(c.r, c.g, c.b, 0f); // Bắt đầu ẩn hoàn toàn để Fade In
                        chapterLogoImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        chapterLogoImage.sprite = null;
                        chapterLogoImage.color = new Color(1f, 1f, 1f, 0f);
                    }
                }

                // 2. Chapter Title
                string cTitle = data != null && !string.IsNullOrEmpty(data.chapterTitle) ? data.chapterTitle : (cfg != null ? cfg.chapterTitle : $"CHAPTER {GetCurrentChapterNumber()}");
                if (chapterTitleText != null)
                {
                    chapterTitleText.text = cTitle.ToUpper();
                }

                // 3. Story Title
                string oppName = GetActiveOpponentName();
                string sTitle = data != null && !string.IsNullOrEmpty(data.characterStoryTitle) ? data.characterStoryTitle : ("CÂU CHUYỆN CỦA " + oppName).ToUpper();
                if (characterStoryTitleText != null)
                {
                    characterStoryTitleText.text = sTitle;
                }

                // 4. Story Body
                string sBody = data != null ? data.characterStory : "";
                if (characterStoryBodyText != null)
                {
                    characterStoryBodyText.text = ResolveDialogueText(sBody);
                }

                // 5. Opponent Intro Art
                Sprite introArt = data != null && data.characterIntroArt != null ? data.characterIntroArt : GetActiveOpponentAvatar();
                if (characterIntroArtImage != null)
                {
                    characterIntroArtImage.sprite = introArt;
                    characterIntroArtImage.color = introArt != null ? Color.white : new Color(0, 0, 0, 0);
                }

                // Ban đầu: Ẩn Story Card và Prompt để màn hình chỉ tập trung vào Logo (tiết kiệm diện tích)
                if (characterStoryCard != null)
                {
                    if (storyCardCanvasGroup != null)
                    {
                        storyCardCanvasGroup.alpha = 0f;
                        storyCardCanvasGroup.blocksRaycasts = false;
                        storyCardCanvasGroup.interactable = false;
                    }
                    characterStoryCard.SetActive(false);
                }

                if (tapPromptText != null)
                {
                    tapPromptText.gameObject.SetActive(false);
                }

                // Overlay mờ bắt đầu từ 0 và fade in mượt mà để không bị tối đè màn hình đột ngột
                if (dimOverlay != null)
                {
                    var c = dimOverlay.color;
                    dimOverlay.color = new Color(c.r, c.g, c.b, 0f);
                }

                if (introSequenceRoutine != null) StopCoroutine(introSequenceRoutine);
                introSequenceRoutine = StartCoroutine(RunIntroSequence(logoSpr != null));
            }
        }

        private IEnumerator RunIntroSequence(bool hasLogo)
        {
            float targetDim = Mathf.Clamp(dimOverlayAlpha, 0.4f, 0.85f);

            // ==========================================
            // GIAI ĐOẠN 1: ANIMATION LOGO CHAPTER (3 giây)
            // ==========================================
            if (hasLogo && (topBanner != null || chapterLogoImage != null))
            {
                currentIntroStep = IntroStep.LogoShowcase;
                skipLogoRequested = false;

                if (topBanner != null) topBanner.SetActive(true);
                if (chapterLogoImage != null)
                {
                    chapterLogoImage.gameObject.SetActive(true);
                    Color c = chapterLogoImage.color;
                    chapterLogoImage.color = new Color(c.r, c.g, c.b, 0f);
                }

                if (logoCanvasGroup != null)
                {
                    logoCanvasGroup.alpha = 0f;
                    logoCanvasGroup.blocksRaycasts = true;
                    logoCanvasGroup.interactable = true;
                }

                // 1. Fade in Logo & Dim Overlay (0.5s) - Giữ nguyên 100% scale từ Edit Mode, chỉ fade alpha
                float fadeInTime = 0.5f;
                float elapsed = 0f;

                while (elapsed < fadeInTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeInTime);
                    float easeAlpha = Mathf.SmoothStep(0f, 1f, t);

                    if (logoCanvasGroup != null) logoCanvasGroup.alpha = easeAlpha;
                    if (chapterLogoImage != null)
                    {
                        Color c = chapterLogoImage.color;
                        chapterLogoImage.color = new Color(c.r, c.g, c.b, easeAlpha);
                    }
                    if (chapterTitleText != null)
                    {
                        Color tc = chapterTitleText.color;
                        chapterTitleText.color = new Color(tc.r, tc.g, tc.b, easeAlpha);
                    }

                    if (dimOverlay != null)
                    {
                        var c = dimOverlay.color;
                        dimOverlay.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0f, targetDim, easeAlpha));
                    }
                    yield return null;
                }

                if (logoCanvasGroup != null) logoCanvasGroup.alpha = 1f;
                if (chapterLogoImage != null)
                {
                    Color c = chapterLogoImage.color;
                    chapterLogoImage.color = new Color(c.r, c.g, c.b, 1f);
                }
                if (dimOverlay != null)
                {
                    var c = dimOverlay.color;
                    dimOverlay.color = new Color(c.r, c.g, c.b, targetDim);
                }

                // 2. GIỮ LOGO 3 GIÂY (GIỮ NGUYÊN 100% SCALE GỐC TRONG EDIT MODE)
                float holdDuration = Mathf.Max(1.0f, logoDisplayDuration);
                float holdTimer = 0f;
                while (holdTimer < holdDuration && !skipLogoRequested)
                {
                    holdTimer += Time.unscaledDeltaTime;
                    yield return null;
                }

                // 3. Fade out Logo (0.45s) - Giữ nguyên 100% scale từ Edit Mode, chỉ fade alpha
                float fadeOutTime = 0.45f;
                elapsed = 0f;

                while (elapsed < fadeOutTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeOutTime);
                    float ease = Mathf.SmoothStep(0f, 1f, t);

                    if (logoCanvasGroup != null) logoCanvasGroup.alpha = 1f - ease;
                    if (chapterLogoImage != null)
                    {
                        Color c = chapterLogoImage.color;
                        chapterLogoImage.color = new Color(c.r, c.g, c.b, 1f - ease);
                    }
                    if (chapterTitleText != null)
                    {
                        Color tc = chapterTitleText.color;
                        chapterTitleText.color = new Color(tc.r, tc.g, tc.b, 1f - ease);
                    }
                    yield return null;
                }

                if (logoCanvasGroup != null) logoCanvasGroup.alpha = 0f;
                if (chapterLogoImage != null)
                {
                    Color c = chapterLogoImage.color;
                    chapterLogoImage.color = new Color(c.r, c.g, c.b, 0f);
                }
                if (topBanner != null) topBanner.SetActive(false);
            }
            else
            {
                // Nếu không có logo, fade in Dim Overlay trực tiếp
                float fadeInTime = 0.35f;
                float elapsed = 0f;
                while (elapsed < fadeInTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeInTime);
                    float ease = Mathf.SmoothStep(0f, 1f, t);
                    if (dimOverlay != null)
                    {
                        var c = dimOverlay.color;
                        dimOverlay.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0f, targetDim, ease));
                    }
                    yield return null;
                }
                if (dimOverlay != null)
                {
                    var c = dimOverlay.color;
                    dimOverlay.color = new Color(c.r, c.g, c.b, targetDim);
                }
            }

            // ==========================================
            // GIAI ĐOẠN 2: ANIMATION HIỆN BẢNG CÂU CHUYỆN (Story Card)
            // ==========================================
            currentIntroStep = IntroStep.StoryCardShowcase;

            if (characterStoryCard != null)
            {
                characterStoryCard.SetActive(true);
                if (storyCardCanvasGroup != null)
                {
                    storyCardCanvasGroup.alpha = 0f;
                    storyCardCanvasGroup.blocksRaycasts = true;
                    storyCardCanvasGroup.interactable = true;
                }
            }

            if (tapPromptText != null)
            {
                tapPromptText.gameObject.SetActive(true);
            }

            if (promptPulseRoutine != null) StopCoroutine(promptPulseRoutine);
            promptPulseRoutine = StartCoroutine(AnimatePromptPulse());

            // Fade in Story Card (0.45s) - Giữ nguyên 100% scale từ Edit Mode, chỉ fade alpha
            float cardFadeTime = 0.45f;
            float cardElapsed = 0f;
            while (cardElapsed < cardFadeTime)
            {
                cardElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(cardElapsed / cardFadeTime);
                float easeAlpha = Mathf.SmoothStep(0f, 1f, t);

                if (storyCardCanvasGroup != null) storyCardCanvasGroup.alpha = easeAlpha;
                yield return null;
            }
            if (storyCardCanvasGroup != null) storyCardCanvasGroup.alpha = 1f;

            // Nếu đang bật Auto Mode, tự động chờ người chơi đọc rồi chuyển tiếp
            if (isAutoMode)
            {
                if (autoRoutine != null) StopCoroutine(autoRoutine);
                autoRoutine = StartCoroutine(WaitAndAutoAdvanceIntro());
            }
        }

        private IEnumerator AnimatePromptPulse()
        {
            if (tapPromptText == null) yield break;

            Color baseColor = tapPromptText.color;
            while (currentPhase == DialoguePhase.Intro)
            {
                float alpha = Mathf.PingPong(Time.unscaledTime * 1.8f, 0.65f) + 0.35f;
                tapPromptText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
            tapPromptText.color = baseColor;
        }

        public void OnTapToContinueClicked()
        {
            if (currentPhase != DialoguePhase.Intro) return;

            if (currentIntroStep == IntroStep.LogoShowcase)
            {
                // Người chơi chạm vào màn hình khi logo đang hiện -> chuyển ngay sang fade logo và mở Story Card
                skipLogoRequested = true;
                return;
            }

            if (currentIntroStep == IntroStep.StoryCardShowcase)
            {
                if (nextClip != null && SFXManager.Instance != null)
                    SFXManager.Instance.PlaySFX(nextClip);

                if (transitionRoutine != null) StopCoroutine(transitionRoutine);
                transitionRoutine = StartCoroutine(TransitionFromIntroToPreBattle());
            }
        }

        private IEnumerator TransitionFromIntroToPreBattle()
        {
            if (introSequenceRoutine != null) { StopCoroutine(introSequenceRoutine); introSequenceRoutine = null; }
            if (promptPulseRoutine != null) { StopCoroutine(promptPulseRoutine); promptPulseRoutine = null; }

            if (introCanvasGroup != null)
            {
                introCanvasGroup.blocksRaycasts = false;
                introCanvasGroup.interactable = false;

                float dur = 0.25f;
                float elapsed = 0f;
                while (elapsed < dur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    introCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / dur);
                    yield return null;
                }
                introCanvasGroup.alpha = 0f;
            }

            if (introContainer != null) introContainer.SetActive(false);

            StartPreBattleDialogue();
        }

        public void StartPreBattleDialogue()
        {
            currentPhase = DialoguePhase.PreBattle;

            if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(true);
            if (introContainer != null) introContainer.SetActive(false);
            if (dialogueContainer != null) dialogueContainer.gameObject.SetActive(true);
            if (topControlsGroup != null) topControlsGroup.SetActive(true);

            var activeLines = DialogueLines;
            if (activeLines == null || activeLines.Count == 0)
            {
                InitDefaultChapter1Lines();
            }

            currentLineIndex = -1;
            UpdateAutoButtonVisual();

            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(AnimateOpenDialogue());
        }

        public void StartPostBattleDialogue(bool isWin)
        {
            LastBattleWon = isWin;
            currentPhase = DialoguePhase.PostBattle;
            IsPlaying = true;

            if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(true);
            if (introContainer != null) introContainer.SetActive(false);
            if (backstoryContainer != null) backstoryContainer.SetActive(false);
            if (dialogueContainer != null) dialogueContainer.gameObject.SetActive(true);
            if (topControlsGroup != null) topControlsGroup.SetActive(true);

            currentLineIndex = -1;
            UpdateAutoButtonVisual();

            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(AnimateOpenDialogue());
        }

        public void StartPostBattleDialogue()
        {
            StartPostBattleDialogue(LastBattleWon);
        }

        private IEnumerator AnimateOpenDialogue()
        {
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 0f;
                mainCanvasGroup.blocksRaycasts = true;
                mainCanvasGroup.interactable = true;
            }

            Vector2 targetPos = dialogueContainer != null ? dialogueContainer.anchoredPosition : Vector2.zero;
            Vector2 startPos = targetPos + new Vector2(0, -60f);

            if (dialogueContainer != null)
                dialogueContainer.anchoredPosition = startPos;

            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3f);

                if (mainCanvasGroup != null)
                    mainCanvasGroup.alpha = ease;

                if (dialogueContainer != null)
                    dialogueContainer.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);

                yield return null;
            }

            if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;
            if (dialogueContainer != null) dialogueContainer.anchoredPosition = targetPos;

            AdvanceLine();
        }

        public void AdvanceLine()
        {
            if (autoRoutine != null) { StopCoroutine(autoRoutine); autoRoutine = null; }
            if (typingRoutine != null) { StopCoroutine(typingRoutine); typingRoutine = null; }

            var activeLines = DialogueLines;
            currentLineIndex++;
            if (activeLines == null || currentLineIndex >= activeLines.Count)
            {
                FinishDialogue();
                return;
            }

            DisplayLine(activeLines[currentLineIndex]);
        }

        private void DisplayLine(DialogueLine line)
        {
            if (line == null) return;

            bool isLeft = line.side == SpeakerSide.Left;
            SpeakerUI activeSpeaker = isLeft ? leftSpeaker : rightSpeaker;
            SpeakerUI inactiveSpeaker = isLeft ? rightSpeaker : leftSpeaker;

            Color activeColor = line.nameColor != Color.white && line.nameColor.a > 0
                ? line.nameColor
                : (isLeft ? new Color(0.2f, 0.95f, 1f, 1f) : new Color(1f, 0.2f, 0.85f, 1f));

            string resolvedSpeaker = ResolveSpeakerName(line.speakerName, isLeft);

            if (activeSpeaker != null)
            {
                activeSpeaker.SetActiveState(true, false, activeColor);
                if (activeSpeaker.nameText != null)
                {
                    activeSpeaker.nameText.text = resolvedSpeaker;
                    activeSpeaker.nameText.color = activeColor;
                }
                if (activeSpeaker.avatarImage != null)
                {
                    Sprite spr = line.customAvatar;
                    if (spr == null)
                        spr = isLeft ? playerAvatarSprite : GetActiveOpponentAvatar();
                    activeSpeaker.avatarImage.sprite = spr;
                    activeSpeaker.avatarImage.color = spr != null ? Color.white : new Color(0, 0, 0, 0);
                }

                if (activeSpeaker.rootContainer != null)
                    StartCoroutine(AnimateSpeakerPop(activeSpeaker.rootContainer.transform));
            }

            if (inactiveSpeaker != null)
            {
                inactiveSpeaker.SetActiveState(false, hideInactiveSpeaker, Color.gray);
            }

            string resolvedContent = ResolveDialogueText(line.text);
            typingRoutine = StartCoroutine(TypewriterRoutine(resolvedContent));
        }

        private IEnumerator AnimateSpeakerPop(Transform target)
        {
            if (target == null) yield break;

            Vector3 baseScale = Vector3.one;
            Vector3 popScale = new Vector3(1.08f, 1.08f, 1f);

            float t = 0f;
            float dur = 0.15f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(popScale, baseScale, t / dur);
                yield return null;
            }
            target.localScale = baseScale;
        }

        private IEnumerator TypewriterRoutine(string fullText)
        {
            isTyping = true;
            if (nextIndicator != null) nextIndicator.SetActive(false);
            if (dialogueText != null) dialogueText.text = "";

            if (nextClip != null && SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(nextClip);

            int totalChars = fullText.Length;
            for (int i = 0; i <= totalChars; i++)
            {
                if (dialogueText != null)
                    dialogueText.text = fullText.Substring(0, i);

                if (i % 2 == 0 && typewriterClip != null && SFXManager.Instance != null)
                {
                    SFXManager.Instance.PlaySFX(typewriterClip);
                }

                yield return new WaitForSecondsRealtime(typingSpeed);
            }

            isTyping = false;
            if (nextIndicator != null) nextIndicator.SetActive(true);

            if (isAutoMode)
            {
                autoRoutine = StartCoroutine(WaitAndAutoAdvance());
            }
        }

        private IEnumerator WaitAndAutoAdvance()
        {
            yield return new WaitForSecondsRealtime(autoAdvanceDelay);
            AdvanceLine();
        }

        public void OnDialogueAreaClicked()
        {
            var activeLines = DialogueLines;
            if (isTyping)
            {
                if (typingRoutine != null) StopCoroutine(typingRoutine);
                isTyping = false;
                if (currentLineIndex >= 0 && activeLines != null && currentLineIndex < activeLines.Count && dialogueText != null)
                {
                    dialogueText.text = ResolveDialogueText(activeLines[currentLineIndex].text);
                }
                if (nextIndicator != null) nextIndicator.SetActive(true);

                if (isAutoMode)
                {
                    if (autoRoutine != null) StopCoroutine(autoRoutine);
                    autoRoutine = StartCoroutine(WaitAndAutoAdvance());
                }
            }
            else
            {
                AdvanceLine();
            }
        }

        public void OnSkipClicked()
        {
            if (skipClip != null && SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(skipClip);

            if (currentPhase == DialoguePhase.IntroVideo)
            {
                skipVideoRequested = true;
                return;
            }

            if (currentPhase == DialoguePhase.Backstory)
            {
                FinishBackstory();
                return;
            }

            FinishDialogue();
        }

        public void OnAutoToggleClicked()
        {
            isAutoMode = !isAutoMode;
            UpdateAutoButtonVisual();

            if (isAutoMode)
            {
                if (currentPhase == DialoguePhase.Intro)
                {
                    if (autoRoutine != null) StopCoroutine(autoRoutine);
                    autoRoutine = StartCoroutine(WaitAndAutoAdvanceIntro());
                }
                else if (currentPhase == DialoguePhase.Backstory)
                {
                    // Backstory đã tự động chuyển sau 3 giây mỗi slide
                }
                else if (currentPhase == DialoguePhase.PreBattle || currentPhase == DialoguePhase.PostBattle)
                {
                    if (!isTyping)
                    {
                        if (autoRoutine != null) StopCoroutine(autoRoutine);
                        autoRoutine = StartCoroutine(WaitAndAutoAdvance());
                    }
                }
            }
            else
            {
                if (autoRoutine != null)
                {
                    StopCoroutine(autoRoutine);
                    autoRoutine = null;
                }
            }
        }

        private IEnumerator WaitAndAutoAdvanceIntro()
        {
            yield return new WaitForSecondsRealtime(4.0f);
            if (currentPhase == DialoguePhase.Intro && isAutoMode && currentIntroStep == IntroStep.StoryCardShowcase)
            {
                OnTapToContinueClicked();
            }
        }

        private void UpdateAutoButtonVisual()
        {
            if (autoButtonGlow != null)
            {
                autoButtonGlow.color = isAutoMode ? new Color(0f, 1f, 0.7f, 1f) : new Color(1f, 1f, 1f, 0.5f);
            }
        }

        // ==========================================
        // BACKSTORY MINH HỌA (CHỈ CẦN THÊM ẢNH - ĐÃ CÓ SẴN THOẠI TRÊN TRANH)
        // (Fade in/Fade out từng ảnh, ảnh 1 text nháy đợi bấm, các ảnh sau 3s tự qua, nút skip ở góc trên bên phải)
        // ==========================================
        public void ShowBackstoryScreen(List<Sprite> images)
        {
            IsPlaying = true;
            currentPhase = DialoguePhase.Backstory;
            activeBackstoryImages = images;

            EnsureUI();
            EnsureBackstoryUI();

            if (dialogueCanvas != null)
            {
                dialogueCanvas.gameObject.SetActive(true);
                dialogueCanvas.sortingOrder = 600;
            }
            if (introContainer != null) introContainer.SetActive(false);
            if (characterStoryCard != null) characterStoryCard.SetActive(false);
            if (dialogueContainer != null) dialogueContainer.gameObject.SetActive(false);
            if (backstoryContainer != null)
            {
                backstoryContainer.SetActive(true);
                backstoryContainer.transform.SetAsLastSibling();
            }

            ApplyBackstoryLayout();

            // Ẩn bảng thoại bên dưới vì lời thoại đã được vẽ sẵn trực tiếp trên tranh ảnh
            if (backstorySubtitlePanel != null)
                backstorySubtitlePanel.SetActive(false);

            // Nút Bỏ qua ở góc trên bên phải (của dialog) luôn hiển thị và nằm trên cùng
            if (topControlsGroup != null)
            {
                topControlsGroup.SetActive(true);
                topControlsGroup.transform.SetAsLastSibling();
            }
            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(true);
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(OnSkipClicked);
            }

            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.alpha = 1f;
                mainCanvasGroup.blocksRaycasts = true;
                mainCanvasGroup.interactable = true;
            }

            if (backstoryCanvasGroup != null)
            {
                backstoryCanvasGroup.alpha = 1f;
                backstoryCanvasGroup.blocksRaycasts = true;
                backstoryCanvasGroup.interactable = true;
            }

            UpdateAutoButtonVisual();

            if (backstorySequenceRoutine != null) StopCoroutine(backstorySequenceRoutine);
            backstorySequenceRoutine = StartCoroutine(RunBackstorySequence());
        }

        // Tương thích ngược nếu còn gọi theo kiểu cũ
        public void ShowBackstoryScreen(List<BackstorySlide> slides)
        {
            var list = new List<Sprite>();
            if (slides != null)
            {
                foreach (var s in slides)
                {
                    if (s != null && s.illustration != null)
                        list.Add(s.illustration);
                }
            }
            ShowBackstoryScreen(list);
        }

        private IEnumerator RunBackstorySequence()
        {
            if (activeBackstoryImages == null || activeBackstoryImages.Count == 0)
            {
                FinishBackstory();
                yield break;
            }

            // Đảm bảo nút Bỏ qua ở góc trên bên phải luôn nằm trên cùng
            if (topControlsGroup != null)
            {
                topControlsGroup.SetActive(true);
                topControlsGroup.transform.SetAsLastSibling();
            }

            // Ẩn bảng thoại bên dưới để tranh ảnh được xem trọn vẹn 100% không bị che khuất
            if (backstorySubtitlePanel != null)
                backstorySubtitlePanel.SetActive(false);

            // Đặt alpha nội dung ảnh bắt đầu ở 0 để Fade In
            SetSlideContentAlpha(0f);

            for (int i = 0; i < activeBackstoryImages.Count; i++)
            {
                currentBackstoryIndex = i;
                Sprite sprite = activeBackstoryImages[i];

                // Gán ảnh trực tiếp
                if (backstoryIllustrationImage != null)
                {
                    backstoryIllustrationImage.sprite = sprite;
                    backstoryIllustrationImage.color = new Color(1f, 1f, 1f, 0f);
                }

                if (i == 0)
                {
                    // Ảnh đầu tiên: Giới thiệu câu chuyện nhân vật -> hiện text nhấp nháy "BẤM VÀO MÀN HÌNH ĐỂ BỎ QUA"
                    EnsureBackstoryPromptText();
                    if (backstoryFirstSlidePromptText != null)
                    {
                        backstoryFirstSlidePromptText.gameObject.SetActive(true);
                        backstoryFirstSlidePromptText.text = "BẤM VÀO MÀN HÌNH ĐỂ BỎ QUA ▶";
                        if (backstoryPromptPulseRoutine != null) StopCoroutine(backstoryPromptPulseRoutine);
                        backstoryPromptPulseRoutine = StartCoroutine(AnimateFirstSlidePromptPulse());
                    }
                }
                else
                {
                    // Từ ảnh thứ 2 trở đi: Ẩn text nhấp nháy của ảnh 1
                    if (backstoryFirstSlidePromptText != null)
                        backstoryFirstSlidePromptText.gameObject.SetActive(false);
                }

                // --- 1. ANIMATION FADE IN CHO ẢNH (0.35s) ---
                yield return StartCoroutine(FadeSlideContent(0f, 1f, 0.35f));

                // --- 2. HIỂN THỊ NỘI DUNG ---
                if (i == 0)
                {
                    // Ảnh đầu tiên: Đợi người chơi chạm màn hình
                    isWaitingForFirstSlideTap = true;
                    slideTapRequested = false;

                    while (isWaitingForFirstSlideTap && currentPhase == DialoguePhase.Backstory)
                    {
                        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                        {
                            isWaitingForFirstSlideTap = false;
                            break;
                        }
                        yield return null;
                    }
                }
                else
                {
                    // Các ảnh tiếp theo: Backstory có thoại -> 3s 1 ảnh tự động qua (hoặc bấm để qua sớm)
                    float duration = 3.0f;
                    float timer = 0f;
                    slideTapRequested = false;

                    while (timer < duration && !slideTapRequested && currentPhase == DialoguePhase.Backstory)
                    {
                        timer += Time.unscaledDeltaTime;
                        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                        {
                            slideTapRequested = true;
                            break;
                        }
                        yield return null;
                    }
                }

                // Nếu người chơi đã bấm nút Skip hoặc kết thúc
                if (currentPhase != DialoguePhase.Backstory)
                    yield break;

                // Dừng pulse text nếu vừa xong ảnh 0 trước khi fade out
                if (i == 0 && backstoryPromptPulseRoutine != null)
                {
                    StopCoroutine(backstoryPromptPulseRoutine);
                    backstoryPromptPulseRoutine = null;
                }

                // --- 3. ANIMATION FADE OUT CHO ẢNH (0.35s) ---
                yield return StartCoroutine(FadeSlideContent(1f, 0f, 0.35f));
            }

            // Hết tất cả các ảnh -> Vào gameplay!
            FinishBackstory();
        }

        private IEnumerator FadeSlideContent(float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float a = Mathf.Lerp(fromAlpha, toAlpha, Mathf.SmoothStep(0f, 1f, t));

                SetSlideContentAlpha(a);
                yield return null;
            }
            SetSlideContentAlpha(toAlpha);
        }

        private void SetSlideContentAlpha(float a)
        {
            if (backstoryContentCanvasGroup != null)
            {
                backstoryContentCanvasGroup.alpha = a;
            }

            if (backstoryIllustrationImage != null)
            {
                var c = backstoryIllustrationImage.color;
                backstoryIllustrationImage.color = new Color(c.r, c.g, c.b, a);
            }

            if (backstorySubtitlePanel != null)
            {
                var cg = backstorySubtitlePanel.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = a;
            }

            if (backstoryFirstSlidePromptText != null && currentBackstoryIndex == 0)
            {
                var c = backstoryFirstSlidePromptText.color;
                backstoryFirstSlidePromptText.color = new Color(c.r, c.g, c.b, a);
            }
        }

        private void ApplySlideData(BackstorySlide slide, int index)
        {
            if (slide == null) return;

            // Ảnh minh họa
            if (backstoryIllustrationImage != null)
            {
                backstoryIllustrationImage.sprite = slide.illustration;
                backstoryIllustrationImage.color = new Color(1f, 1f, 1f, 0f); // Bắt đầu ở alpha = 0 để Fade In
            }

            if (index == 0)
            {
                // ẢNH ĐẦU TIÊN: CÂU CHUYỆN NHÂN VẬT
                // Ẩn bảng thoại bên dưới để tôn trọn vẹn hình ảnh
                if (backstorySubtitlePanel != null)
                    backstorySubtitlePanel.SetActive(false);

                // Hiện text nhấp nháy "BẤM VÀO MÀN HÌNH ĐỂ BỎ QUA"
                EnsureBackstoryPromptText();
                if (backstoryFirstSlidePromptText != null)
                {
                    backstoryFirstSlidePromptText.gameObject.SetActive(true);
                    backstoryFirstSlidePromptText.text = "BẤM VÀO MÀN HÌNH ĐỂ BỎ QUA ▶";
                    if (backstoryPromptPulseRoutine != null) StopCoroutine(backstoryPromptPulseRoutine);
                    backstoryPromptPulseRoutine = StartCoroutine(AnimateFirstSlidePromptPulse());
                }
            }
            else
            {
                // CÁC ẢNH TIẾP THEO: BACKSTORY CÓ THOẠI
                if (backstoryFirstSlidePromptText != null)
                    backstoryFirstSlidePromptText.gameObject.SetActive(false);

                if (backstorySubtitlePanel != null)
                {
                    backstorySubtitlePanel.SetActive(true);
                    var cg = backstorySubtitlePanel.GetComponent<CanvasGroup>() ?? backstorySubtitlePanel.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                }

                if (backstorySpeakerText != null)
                {
                    backstorySpeakerText.text = ResolveSpeakerName(slide.speakerName, slide.speakerName != "RAZE");
                    backstorySpeakerText.color = slide.nameColor;
                }

                if (backstoryActionText != null)
                {
                    backstoryActionText.text = slide.actionDescription;
                    backstoryActionText.gameObject.SetActive(!string.IsNullOrEmpty(slide.actionDescription));
                }

                if (backstorySlideCounterText != null)
                {
                    backstorySlideCounterText.text = $"{index + 1} / {activeBackstorySlides.Count}";
                }

                if (backstoryDialogueText != null)
                {
                    backstoryDialogueText.text = ResolveDialogueText(slide.storyText);
                }
            }
        }

        private void EnsureBackstoryPromptText()
        {
            if (backstoryFirstSlidePromptText != null) return;
            if (backstoryContainer == null) return;

            var existing = backstoryContainer.transform.Find("FirstSlidePromptText");
            if (existing != null)
            {
                backstoryFirstSlidePromptText = existing.GetComponent<TMPro.TextMeshProUGUI>();
                return;
            }

            var promptGo = new GameObject("FirstSlidePromptText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
            promptGo.transform.SetParent(backstoryContainer.transform, false);
            var rt = promptGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 45f);
            rt.sizeDelta = new Vector2(850f, 50f);

            var tmp = promptGo.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "BẤM VÀO MÀN HÌNH ĐỂ BỎ QUA ▶";
            tmp.font = ResolveFont();
            tmp.fontSize = 24f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.2f, 0.95f, 1f, 0.95f);
            tmp.raycastTarget = false;
            backstoryFirstSlidePromptText = tmp;
        }

        private IEnumerator AnimateFirstSlidePromptPulse()
        {
            if (backstoryFirstSlidePromptText == null) yield break;

            Color baseColor = backstoryFirstSlidePromptText.color;
            while (currentPhase == DialoguePhase.Backstory && currentBackstoryIndex == 0)
            {
                float alpha = Mathf.PingPong(Time.unscaledTime * 2.0f, 0.65f) + 0.35f;
                backstoryFirstSlidePromptText.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                yield return null;
            }
            if (backstoryFirstSlidePromptText != null)
                backstoryFirstSlidePromptText.color = baseColor;
        }

        public void OnBackstoryTapClicked()
        {
            if (currentPhase != DialoguePhase.Backstory) return;

            if (nextClip != null && SFXManager.Instance != null)
                SFXManager.Instance.PlaySFX(nextClip);

            if (currentBackstoryIndex == 0)
            {
                // Chạm vào ảnh đầu tiên -> tiếp tục chuyển sang các ảnh backstory có thoại
                isWaitingForFirstSlideTap = false;
            }
            else
            {
                // Chạm vào các ảnh backstory tiếp theo -> chuyển sớm qua ảnh kế tiếp không cần đợi hết 3s
                slideTapRequested = true;
            }
        }

        private void FinishBackstory()
        {
            IsPlaying = false;
            hasCompletedDialogueThisSession = true;
            currentPhase = DialoguePhase.None;

            if (backstorySequenceRoutine != null) { StopCoroutine(backstorySequenceRoutine); backstorySequenceRoutine = null; }
            if (backstoryPromptPulseRoutine != null) { StopCoroutine(backstoryPromptPulseRoutine); backstoryPromptPulseRoutine = null; }
            if (autoRoutine != null) { StopCoroutine(autoRoutine); autoRoutine = null; }
            if (typingRoutine != null) { StopCoroutine(typingRoutine); typingRoutine = null; }
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);

            transitionRoutine = StartCoroutine(AnimateCloseBackstory());
        }

        private IEnumerator AnimateCloseBackstory()
        {
            currentPhase = DialoguePhase.None;
            IsPlaying = false;

            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.interactable = false;
                mainCanvasGroup.blocksRaycasts = false;
            }

            float duration = 0.35f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (backstoryCanvasGroup != null) backstoryCanvasGroup.alpha = 1f - t;
                if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f - t;
                yield return null;
            }

            if (backstoryContainer != null) backstoryContainer.SetActive(false);
            if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(false);

            StartGameplay();
        }

        public void FinishDialogue()
        {
            IsPlaying = false;
            if (currentPhase == DialoguePhase.PreBattle || currentPhase == DialoguePhase.Intro)
            {
                hasCompletedDialogueThisSession = true;
            }
            else if (currentPhase == DialoguePhase.PostBattle)
            {
                hasCompletedPostBattleThisSession = true;
            }

            if (introSequenceRoutine != null) { StopCoroutine(introSequenceRoutine); introSequenceRoutine = null; }
            if (autoRoutine != null) { StopCoroutine(autoRoutine); autoRoutine = null; }
            if (typingRoutine != null) { StopCoroutine(typingRoutine); typingRoutine = null; }
            if (promptPulseRoutine != null) { StopCoroutine(promptPulseRoutine); promptPulseRoutine = null; }
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);

            transitionRoutine = StartCoroutine(AnimateCloseDialogue());
        }

        private IEnumerator AnimateCloseDialogue()
        {
            DialoguePhase finishedPhase = currentPhase;
            currentPhase = DialoguePhase.None;

            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.interactable = false;
                mainCanvasGroup.blocksRaycasts = false;
            }

            Vector2 currentPos = dialogueContainer != null ? dialogueContainer.anchoredPosition : Vector2.zero;
            Vector2 targetPos = currentPos + new Vector2(0, -60f);

            float duration = 0.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = t * t * t;

                if (mainCanvasGroup != null)
                    mainCanvasGroup.alpha = 1f - ease;

                if (dialogueContainer != null)
                    dialogueContainer.anchoredPosition = Vector2.Lerp(currentPos, targetPos, ease);

                yield return null;
            }

            if (mainCanvasGroup != null) mainCanvasGroup.alpha = 0f;
            if (introContainer != null) introContainer.SetActive(false);
            if (dialogueContainer != null) dialogueContainer.gameObject.SetActive(false);
            if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(false);

            if (finishedPhase == DialoguePhase.PreBattle || finishedPhase == DialoguePhase.Intro)
            {
                StartGameplay();
            }
            else if (finishedPhase == DialoguePhase.PostBattle)
            {
                ShowPostBattleResults();
            }
        }

        private void ShowPostBattleResults()
        {
            var resultPanel = FindFirstObjectByType<RhythmGame.GameplayResultScreen>(FindObjectsInactive.Include);
            if (resultPanel != null)
            {
                resultPanel.ShowResult(LastBattleWon);
            }
            else
            {
                Debug.LogWarning("[StoryDialogueManager] Không tìm thấy GameplayResultScreen để hiển thị kết quả sau trận!");
            }
        }

        private void StartGameplay()
        {
            var gm = FindFirstObjectByType<RhythmGame.GameManager>();
            if (gm != null)
            {
                gm.Restart();
            }
            else
            {
                Debug.LogWarning("[StoryDialogueManager] Không tìm thấy RhythmGame.GameManager để bắt đầu màn chơi!");
            }
        }

        public static string GetPlayerDisplayName()
        {
            try
            {
                if (Luan.MainMenu.AccountManager.Instance != null && Luan.MainMenu.AccountManager.Instance.CurrentUser != null)
                {
                    var user = Luan.MainMenu.AccountManager.Instance.CurrentUser;
                    if (!string.IsNullOrEmpty(user.displayName)) return user.displayName;
                    if (!string.IsNullOrEmpty(user.username)) return user.username;
                }
            }
            catch { }

            string cachedDisplay = PlayerPrefs.GetString("PLAYFAB_CACHED_DISPLAYNAME", "");
            if (!string.IsNullOrEmpty(cachedDisplay)) return cachedDisplay;

            string cachedUser = PlayerPrefs.GetString("PLAYFAB_CACHED_USERNAME", "");
            if (!string.IsNullOrEmpty(cachedUser)) return cachedUser;

            string legacyUser = PlayerPrefs.GetString("ACCOUNT_USERNAME", "");
            if (!string.IsNullOrEmpty(legacyUser)) return legacyUser;

            return "Kai";
        }

        public static string ResolveSpeakerName(string rawName, bool isLeft)
        {
            string playerName = GetPlayerDisplayName();
            if (isLeft)
            {
                if (string.IsNullOrEmpty(rawName) ||
                    rawName.Equals("MAIN 1", System.StringComparison.OrdinalIgnoreCase) ||
                    rawName.Equals("Main 1", System.StringComparison.OrdinalIgnoreCase) ||
                    rawName.Equals("Main", System.StringComparison.OrdinalIgnoreCase) ||
                    rawName.Equals("Player", System.StringComparison.OrdinalIgnoreCase) ||
                    rawName.Equals("{PLAYER}", System.StringComparison.OrdinalIgnoreCase) ||
                    rawName.Equals("Kai", System.StringComparison.OrdinalIgnoreCase) ||
                    rawName.Equals("KAI", System.StringComparison.OrdinalIgnoreCase))
                {
                    return playerName;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(rawName)) return Instance != null ? Instance.GetActiveOpponentName() : "Raze";
            }

            return rawName.Replace("{PLAYER}", playerName).Replace("{player}", playerName);
        }

        public static string ResolveDialogueText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return "";
            string playerName = GetPlayerDisplayName();
            return rawText
                .Replace("{PLAYER}", playerName)
                .Replace("{player}", playerName)
                .Replace("{Player}", playerName)
                .Replace("[Tên người chơi]", playerName)
                .Replace("[tên người chơi]", playerName)
                .Replace("Main 1", playerName)
                .Replace("MAIN 1", playerName);
        }

        public void InitDefaultChapter1Lines()
        {
            string playerName = GetPlayerDisplayName();
            dialogueLines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    speakerName = "Raze",
                    side = SpeakerSide.Right,
                    text = "Này, coi chừng.",
                    nameColor = new Color(1f, 0.2f, 0.85f, 1f),
                    customAvatar = opponentAvatarSprite
                },
                new DialogueLine
                {
                    speakerName = "{PLAYER}",
                    side = SpeakerSide.Left,
                    text = "Của anh à?",
                    nameColor = new Color(0.2f, 0.95f, 1f, 1f),
                    customAvatar = playerAvatarSprite
                },
                new DialogueLine
                {
                    speakerName = "Raze",
                    side = SpeakerSide.Right,
                    text = "Ừ. Đá đổ là cậu tự lau đấy.",
                    nameColor = new Color(1f, 0.2f, 0.85f, 1f),
                    customAvatar = opponentAvatarSprite
                },
                new DialogueLine
                {
                    speakerName = "{PLAYER}",
                    side = SpeakerSide.Left,
                    text = "Tôi đang tìm Raze.",
                    nameColor = new Color(0.2f, 0.95f, 1f, 1f),
                    customAvatar = playerAvatarSprite
                }
            };
        }

        [SerializeField, HideInInspector] private GameObject topControlsGroup;

        private void EnsureIntroReferences()
        {
            if (dialogueCanvas == null)
            {
                var existingCanvas = GameObject.Find("StoryDialogueCanvas");
                if (existingCanvas != null) dialogueCanvas = existingCanvas.GetComponent<Canvas>();
            }

            var root = dialogueCanvas != null ? dialogueCanvas.transform : null;
            if (introContainer == null && root != null)
            {
                var t = root.Find("IntroContainer");
                if (t != null) introContainer = t.gameObject;
            }

            if (introContainer != null)
            {
                if (introCanvasGroup == null)
                    introCanvasGroup = introContainer.GetComponent<CanvasGroup>() ?? introContainer.AddComponent<CanvasGroup>();

                if (topBanner == null)
                {
                    var t = introContainer.transform.Find("TopBanner");
                    if (t != null) topBanner = t.gameObject;
                    else if (chapterLogoImage != null)
                        topBanner = chapterLogoImage.transform.parent != null ? chapterLogoImage.transform.parent.gameObject : chapterLogoImage.gameObject;
                }

                if (topBanner != null && logoCanvasGroup == null)
                {
                    logoCanvasGroup = topBanner.GetComponent<CanvasGroup>() ?? topBanner.AddComponent<CanvasGroup>();
                }

                if (characterStoryCard == null)
                {
                    var t = introContainer.transform.Find("CharacterStoryCard");
                    if (t != null) characterStoryCard = t.gameObject;
                    else if (characterStoryTitleText != null && characterStoryTitleText.transform.parent != null)
                        characterStoryCard = characterStoryTitleText.transform.parent.gameObject;
                }

                if (characterStoryCard != null && storyCardCanvasGroup == null)
                {
                    storyCardCanvasGroup = characterStoryCard.GetComponent<CanvasGroup>() ?? characterStoryCard.AddComponent<CanvasGroup>();
                }

                if (tapPromptText == null)
                {
                    var t = introContainer.transform.Find("BottomPrompt");
                    if (t != null) tapPromptText = t.GetComponent<TMP_Text>();
                }

                if (tapToContinueButton == null)
                {
                    var t = introContainer.transform.Find("TapScreenButton");
                    if (t != null) tapToContinueButton = t.GetComponent<Button>();
                }

                // Ẩn lớp phủ đen thứ 2 trong Intro để không bị đè tối 2 lớp
                var darkOverlay = introContainer.transform.Find("IntroDarkOverlay");
                if (darkOverlay != null)
                {
                    var darkImg = darkOverlay.GetComponent<Image>();
                    if (darkImg != null) darkImg.color = new Color(0, 0, 0, 0);
                    darkOverlay.gameObject.SetActive(false);
                }
            }
        }

        public void EnsureBackstoryUI()
        {
            if (dialogueCanvas == null) EnsureUI();
            if (dialogueCanvas == null) return;

            if (backstoryContainer == null)
            {
                var existing = dialogueCanvas.transform.Find("BackstoryContainer");
                if (existing != null)
                {
                    backstoryContainer = existing.gameObject;
                }
            }

            if (backstoryContainer != null)
            {
                backstoryCanvasGroup = backstoryContainer.GetComponent<CanvasGroup>() ?? backstoryContainer.AddComponent<CanvasGroup>();
                var img = backstoryContainer.transform.Find("IllustrationContainer/IllustrationImage") ?? backstoryContainer.transform.Find("IllustrationImage");
                if (img != null) backstoryIllustrationImage = img.GetComponent<Image>();
                var tap = backstoryContainer.transform.Find("TapScreenButton");
                if (tap != null)
                {
                    backstoryTapButton = tap.GetComponent<Button>();
                    backstoryTapButton.onClick.RemoveAllListeners();
                    backstoryTapButton.onClick.AddListener(OnBackstoryTapClicked);
                }
            }
            else
            {
                var bsGo = new GameObject("BackstoryContainer", typeof(RectTransform), typeof(CanvasGroup));
                bsGo.transform.SetParent(dialogueCanvas.transform, false);
                var bsRt = bsGo.GetComponent<RectTransform>();
                bsRt.anchorMin = Vector2.zero;
                bsRt.anchorMax = Vector2.one;
                bsRt.offsetMin = bsRt.offsetMax = Vector2.zero;
                backstoryContainer = bsGo;
                backstoryCanvasGroup = bsGo.GetComponent<CanvasGroup>();

                // 1. Dark Backdrop
                var bgGo = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bgGo.transform.SetParent(bsGo.transform, false);
                var bgRt = bgGo.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
                var bgImg = bgGo.GetComponent<Image>();
                bgImg.color = new Color(0.015f, 0.01f, 0.03f, 1f);
                bgImg.raycastTarget = false;

                // 2. Illustration Image (căn giữa màn hình 1920x1080, giữ tỉ lệ gốc và áp dụng scale tùy chỉnh)
                var illContainer = new GameObject("IllustrationContainer", typeof(RectTransform));
                illContainer.transform.SetParent(bsGo.transform, false);
                var illRt = illContainer.GetComponent<RectTransform>();
                illRt.anchorMin = Vector2.zero;
                illRt.anchorMax = Vector2.one;
                illRt.offsetMin = illRt.offsetMax = Vector2.zero;

                var imgGo = new GameObject("IllustrationImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imgGo.transform.SetParent(illContainer.transform, false);
                var imgRt = imgGo.GetComponent<RectTransform>();
                imgRt.anchorMin = new Vector2(0.5f, 0.5f);
                imgRt.anchorMax = new Vector2(0.5f, 0.5f);
                imgRt.pivot = new Vector2(0.5f, 0.5f);
                imgRt.anchoredPosition = new Vector2(0f, backstoryOffsetY);
                imgRt.sizeDelta = new Vector2(1920f, 1080f);
                imgRt.localScale = new Vector3(backstoryScale, backstoryScale, 1f);
                backstoryIllustrationImage = imgGo.GetComponent<Image>();
                backstoryIllustrationImage.preserveAspect = true;
                backstoryIllustrationImage.raycastTarget = false;

                // 3. Fullscreen transparent button to capture screen tap
                var tapGo = new GameObject("TapScreenButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                tapGo.transform.SetParent(bsGo.transform, false);
                var tapRt = tapGo.GetComponent<RectTransform>();
                tapRt.anchorMin = Vector2.zero;
                tapRt.anchorMax = Vector2.one;
                tapRt.offsetMin = tapRt.offsetMax = Vector2.zero;
                var tapImg = tapGo.GetComponent<Image>();
                tapImg.color = new Color(0f, 0f, 0f, 0f);
                tapImg.raycastTarget = true;
                backstoryTapButton = tapGo.GetComponent<Button>();
                backstoryTapButton.onClick.RemoveAllListeners();
                backstoryTapButton.onClick.AddListener(OnBackstoryTapClicked);
            }

            EnsureBackstoryPromptText();
        }

        public void EnsureUI()
        {
            if (dialogueCanvas != null && mainCanvasGroup != null && dialogueText != null && introContainer != null)
            {
                EnsureBackstoryUI();
                return;
            }

            var existingCanvas = GameObject.Find("StoryDialogueCanvas");
            if (existingCanvas != null)
            {
                dialogueCanvas = existingCanvas.GetComponent<Canvas>();
                mainCanvasGroup = existingCanvas.GetComponent<CanvasGroup>();
                BindReferences(existingCanvas.transform);
                return;
            }

            LoadAssetsAndBuildUI();
        }

        public void BindReferences(Transform root)
        {
            if (mainCanvasGroup == null) mainCanvasGroup = root.GetComponent<CanvasGroup>();
            var dim = root.Find("DimOverlay");
            if (dim != null) dimOverlay = dim.GetComponent<Image>();

            // Intro Container
            var intro = root.Find("IntroContainer");
            if (intro != null)
            {
                introContainer = intro.gameObject;
                introCanvasGroup = intro.GetComponent<CanvasGroup>() ?? intro.gameObject.AddComponent<CanvasGroup>();

                var topB = intro.Find("TopBanner");
                if (topB != null)
                {
                    topBanner = topB.gameObject;
                    logoCanvasGroup = topB.GetComponent<CanvasGroup>() ?? topB.gameObject.AddComponent<CanvasGroup>();
                }

                var logo = intro.Find("TopBanner/ChapterLogo");
                if (logo != null) chapterLogoImage = logo.GetComponent<Image>();

                var cTitle = intro.Find("TopBanner/ChapterTitleText");
                if (cTitle != null) chapterTitleText = cTitle.GetComponent<TMP_Text>();

                var card = intro.Find("CharacterStoryCard");
                if (card != null)
                {
                    characterStoryCard = card.gameObject;
                    storyCardCanvasGroup = card.GetComponent<CanvasGroup>() ?? card.gameObject.AddComponent<CanvasGroup>();

                    var sTitle = card.Find("StoryTitleText");
                    if (sTitle != null) characterStoryTitleText = sTitle.GetComponent<TMP_Text>();

                    var sBody = card.Find("CharacterStoryBodyText");
                    if (sBody != null) characterStoryBodyText = sBody.GetComponent<TMP_Text>();

                    var art = card.Find("OpponentPortraitContainer/PortraitFrame/PortraitImage");
                    if (art != null) characterIntroArtImage = art.GetComponent<Image>();
                }

                var prompt = intro.Find("BottomPrompt");
                if (prompt != null) tapPromptText = prompt.GetComponent<TMP_Text>();

                var tapBtn = intro.Find("TapScreenButton");
                if (tapBtn != null) tapToContinueButton = tapBtn.GetComponent<Button>();

                var darkOverlay = intro.Find("IntroDarkOverlay");
                if (darkOverlay != null)
                {
                    var darkImg = darkOverlay.GetComponent<Image>();
                    if (darkImg != null) darkImg.color = new Color(0, 0, 0, 0);
                    darkOverlay.gameObject.SetActive(false);
                }
            }

            // Backstory Container
            var bs = root.Find("BackstoryContainer");
            if (bs != null)
            {
                backstoryContainer = bs.gameObject;
                backstoryCanvasGroup = bs.GetComponent<CanvasGroup>() ?? bs.gameObject.AddComponent<CanvasGroup>();

                var img = bs.Find("IllustrationContainer/IllustrationImage");
                if (img != null) backstoryIllustrationImage = img.GetComponent<Image>();

                var sub = bs.Find("SubtitlePanel");
                if (sub != null)
                {
                    backstorySubtitlePanel = sub.gameObject;
                    var spk = sub.Find("SpeakerTag");
                    if (spk != null) backstorySpeakerText = spk.GetComponent<TMP_Text>();
                    var act = sub.Find("ActionTag");
                    if (act != null) backstoryActionText = act.GetComponent<TMP_Text>();
                    var dlg = sub.Find("DialogueText");
                    if (dlg != null) backstoryDialogueText = dlg.GetComponent<TMP_Text>();
                    var cnt = sub.Find("SlideCounterText");
                    if (cnt != null) backstorySlideCounterText = cnt.GetComponent<TMP_Text>();
                    var nxt = sub.Find("NextPrompt");
                    if (nxt != null) backstoryNextIndicator = nxt.gameObject;
                }

                var tapBtn = bs.Find("TapScreenButton");
                if (tapBtn != null) backstoryTapButton = tapBtn.GetComponent<Button>();
            }

            // Dialogue Container
            var container = root.Find("DialogueContainer");
            if (container != null)
            {
                dialogueContainer = container.GetComponent<RectTransform>();

                var box = container.Find("DialogueBox");
                if (box != null)
                {
                    dialogueBoxBg = box.GetComponent<Image>();
                    var txt = box.Find("DialogueText");
                    if (txt != null) dialogueText = txt.GetComponent<TMP_Text>();
                    var clickBtn = box.GetComponent<Button>();
                    if (clickBtn != null) clickAreaButton = clickBtn;
                    var next = box.Find("NextIndicator");
                    if (next != null) nextIndicator = next.gameObject;
                }

                // Left Speaker
                var avLeft = container.Find("LeftSpeaker_AvatarContainer") ?? container.Find("AvatarContainer") ?? container.Find("LeftAvatarContainer");
                var nameLeft = container.Find("LeftSpeaker_NameTagContainer") ?? container.Find("NameTagContainer") ?? container.Find("LeftNameTagContainer");
                var rootLeft = container.Find("LeftSpeaker");
                if (rootLeft != null)
                {
                    leftSpeaker.rootContainer = rootLeft.gameObject;
                    leftSpeaker.canvasGroup = rootLeft.GetComponent<CanvasGroup>();
                }
                if (avLeft != null)
                {
                    var frame = avLeft.Find("AvatarFrame");
                    if (frame != null) leftSpeaker.avatarFrame = frame.GetComponent<Image>();
                    var img = frame?.Find("AvatarImage") ?? avLeft.Find("AvatarImage");
                    if (img != null) leftSpeaker.avatarImage = img.GetComponent<Image>();
                }
                if (nameLeft != null)
                {
                    leftSpeaker.nameTagContainer = nameLeft.gameObject;
                    var nameBg = nameLeft.Find("NameTagBg");
                    if (nameBg != null) leftSpeaker.nameTagBg = nameBg.GetComponent<Image>();
                    var nTxt = nameLeft.Find("NameText");
                    if (nTxt != null) leftSpeaker.nameText = nTxt.GetComponent<TMP_Text>();
                }

                // Right Speaker
                var rootRight = container.Find("RightSpeaker");
                var avRight = container.Find("RightSpeaker_AvatarContainer") ?? container.Find("RightAvatarContainer");
                var nameRight = container.Find("RightSpeaker_NameTagContainer") ?? container.Find("RightNameTagContainer");
                if (rootRight != null)
                {
                    rightSpeaker.rootContainer = rootRight.gameObject;
                    rightSpeaker.canvasGroup = rootRight.GetComponent<CanvasGroup>();
                }
                if (avRight != null)
                {
                    var frame = avRight.Find("AvatarFrame");
                    if (frame != null) rightSpeaker.avatarFrame = frame.GetComponent<Image>();
                    var img = frame?.Find("AvatarImage") ?? avRight.Find("AvatarImage");
                    if (img != null) rightSpeaker.avatarImage = img.GetComponent<Image>();
                }
                if (nameRight != null)
                {
                    rightSpeaker.nameTagContainer = nameRight.gameObject;
                    var nameBg = nameRight.Find("NameTagBg");
                    if (nameBg != null) rightSpeaker.nameTagBg = nameBg.GetComponent<Image>();
                    var nTxt = nameRight.Find("NameText");
                    if (nTxt != null) rightSpeaker.nameText = nTxt.GetComponent<TMP_Text>();
                }
            }

            var controls = root.Find("TopControls");
            if (controls != null)
            {
                topControlsGroup = controls.gameObject;
                var skip = controls.Find("SkipButton");
                if (skip != null) skipButton = skip.GetComponent<Button>();
                var auto = controls.Find("AutoButton");
                if (auto != null)
                {
                    autoButton = auto.GetComponent<Button>();
                    autoButtonGlow = auto.GetComponent<Image>();
                }
            }
        }

        private void LoadAssetsAndBuildUI()
        {
#if UNITY_EDITOR
            string sheetPath = "Assets/Luan/DiaLogTalkChapter/SpriteAssets/Neon Cyberpunk Dialogue UI Sprite Sheet.png";
            var subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
            foreach (var a in subAssets)
            {
                if (a is Sprite s) sprites[s.name] = s;
            }

            if (playerAvatarSprite == null)
                playerAvatarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luan/DiaLogTalkChapter/Avatars/avatar_main1.png");
            if (opponentAvatarSprite == null)
                opponentAvatarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luan/DiaLogTalkChapter/Avatars/avatar_boss1.png");

            Sprite boxSpr = sprites.ContainsKey("Neon Cyberpunk Dialogue UI Sprite Sheet_1") ? sprites["Neon Cyberpunk Dialogue UI Sprite Sheet_1"] : null;
            Sprite frameSpr = sprites.ContainsKey("Neon Cyberpunk Dialogue UI Sprite Sheet_0") ? sprites["Neon Cyberpunk Dialogue UI Sprite Sheet_0"] : null;
            Sprite skipSpr = sprites.ContainsKey("Neon Cyberpunk Dialogue UI Sprite Sheet_2") ? sprites["Neon Cyberpunk Dialogue UI Sprite Sheet_2"] : null;
            Sprite autoSpr = sprites.ContainsKey("Neon Cyberpunk Dialogue UI Sprite Sheet_5") ? sprites["Neon Cyberpunk Dialogue UI Sprite Sheet_5"] : null;
            Sprite nameTagSpr = sprites.ContainsKey("Neon Cyberpunk Dialogue UI Sprite Sheet_9") ? sprites["Neon Cyberpunk Dialogue UI Sprite Sheet_9"] : null;
            Sprite nextSpr = sprites.ContainsKey("Neon Cyberpunk Dialogue UI Sprite Sheet_10") ? sprites["Neon Cyberpunk Dialogue UI Sprite Sheet_10"] : null;

            BuildUIGameObjects(boxSpr, frameSpr, skipSpr, autoSpr, nameTagSpr, nextSpr);
#endif
        }

        public void BuildUIGameObjects(Sprite boxSpr, Sprite frameSpr, Sprite skipSpr, Sprite autoSpr, Sprite nameTagSpr, Sprite nextSpr)
        {
            var oldCanvas = GameObject.Find("StoryDialogueCanvas");
            if (oldCanvas != null) DestroyImmediate(oldCanvas);

            // 1. Root Canvas
            var canvasGo = new GameObject("StoryDialogueCanvas");
            dialogueCanvas = canvasGo.AddComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogueCanvas.sortingOrder = 200;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            mainCanvasGroup = canvasGo.AddComponent<CanvasGroup>();

            // 2. Dim Overlay
            var dimGo = new GameObject("DimOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dimGo.transform.SetParent(canvasGo.transform, false);
            var dimRt = dimGo.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = dimRt.offsetMax = Vector2.zero;
            dimOverlay = dimGo.GetComponent<Image>();
            dimOverlay.color = new Color(0.015f, 0.008f, 0.035f, 0.88f);
            dimOverlay.raycastTarget = false;

            // 3. Intro Container (Cốt truyện & Logo Chapter)
            BuildIntroUI(canvasGo.transform, frameSpr);

            // 3.5 Backstory Container (Minh Họa Storyboard Cốt Truyện & Thoại Đầu)
            BuildBackstoryUI(canvasGo.transform, boxSpr);

            // 4. Dialogue Container
            var containerGo = new GameObject("DialogueContainer", typeof(RectTransform));
            containerGo.transform.SetParent(canvasGo.transform, false);
            dialogueContainer = containerGo.GetComponent<RectTransform>();
            dialogueContainer.anchorMin = new Vector2(0.5f, 0f);
            dialogueContainer.anchorMax = new Vector2(0.5f, 0f);
            dialogueContainer.pivot = new Vector2(0.5f, 0f);
            dialogueContainer.anchoredPosition = new Vector2(0f, 35f);
            dialogueContainer.sizeDelta = new Vector2(1920, 380);

            // 5. Dialogue Box
            var boxGo = new GameObject("DialogueBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            boxGo.transform.SetParent(containerGo.transform, false);
            var boxRt = boxGo.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0f);
            boxRt.anchorMax = new Vector2(0.5f, 0f);
            boxRt.pivot = new Vector2(0.5f, 0f);
            boxRt.anchoredPosition = new Vector2(0f, 20f);
            boxRt.sizeDelta = new Vector2(1380, 240);

            dialogueBoxBg = boxGo.GetComponent<Image>();
            dialogueBoxBg.sprite = boxSpr;
            dialogueBoxBg.preserveAspect = false;

            clickAreaButton = boxGo.GetComponent<Button>();
            clickAreaButton.transition = Selectable.Transition.None;

            // 6. Dialogue Text
            var textGo = new GameObject("DialogueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(boxGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(65, 25);
            textRt.offsetMax = new Vector2(-75, -35);

            dialogueText = textGo.GetComponent<TextMeshProUGUI>();
            dialogueText.fontSize = 32;
            dialogueText.lineSpacing = 1.25f;
            dialogueText.color = Color.white;
            dialogueText.alignment = TextAlignmentOptions.TopLeft;
            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            dialogueText.raycastTarget = false;

            // 7. Next Indicator
            var nextGo = new GameObject("NextIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            nextGo.transform.SetParent(boxGo.transform, false);
            var nextRt = nextGo.GetComponent<RectTransform>();
            nextRt.anchorMin = new Vector2(1f, 0f);
            nextRt.anchorMax = new Vector2(1f, 0f);
            nextRt.pivot = new Vector2(1f, 0f);
            nextRt.anchoredPosition = new Vector2(-40, 30);
            nextRt.sizeDelta = new Vector2(50, 30);

            var nextImg = nextGo.GetComponent<Image>();
            nextImg.sprite = nextSpr;
            nextImg.color = new Color(0.2f, 1f, 0.85f, 0.95f);
            nextImg.raycastTarget = false;
            nextIndicator = nextGo;

            // 8. Left Speaker (Main 1 / Player DisplayName)
            string playerDisplayName = GetPlayerDisplayName();
            BuildSpeakerUI(containerGo.transform, "LeftSpeaker", -580f, -491f, frameSpr, nameTagSpr, playerAvatarSprite, playerDisplayName, new Color(0.2f, 0.95f, 1f, 1f), leftSpeaker);

            // 9. Right Speaker (Raze / Boss 1)
            BuildSpeakerUI(containerGo.transform, "RightSpeaker", 580f, 491f, frameSpr, nameTagSpr, opponentAvatarSprite, "RAZE", new Color(1f, 0.2f, 0.85f, 1f), rightSpeaker);

            // 10. Top Controls (Skip & Auto)
            var topControlsGo = new GameObject("TopControls", typeof(RectTransform));
            topControlsGo.transform.SetParent(canvasGo.transform, false);
            var topRt = topControlsGo.GetComponent<RectTransform>();
            topRt.anchorMin = Vector2.zero;
            topRt.anchorMax = Vector2.one;
            topRt.offsetMin = topRt.offsetMax = Vector2.zero;
            topControlsGroup = topControlsGo;

            // Skip Button
            var skipGo = new GameObject("SkipButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            skipGo.transform.SetParent(topControlsGo.transform, false);
            var skipRt = skipGo.GetComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1f, 1f);
            skipRt.anchorMax = new Vector2(1f, 1f);
            skipRt.pivot = new Vector2(1f, 1f);
            skipRt.anchoredPosition = new Vector2(-45f, -40f);
            skipRt.sizeDelta = new Vector2(190, 80);
            var skipImg = skipGo.GetComponent<Image>();
            skipImg.sprite = skipSpr;
            skipImg.preserveAspect = true;
            skipButton = skipGo.GetComponent<Button>();

            // Auto Button
            var autoGo = new GameObject("AutoButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            autoGo.transform.SetParent(topControlsGo.transform, false);
            var autoRt = autoGo.GetComponent<RectTransform>();
            autoRt.anchorMin = new Vector2(1f, 1f);
            autoRt.anchorMax = new Vector2(1f, 1f);
            autoRt.pivot = new Vector2(1f, 1f);
            autoRt.anchoredPosition = new Vector2(-245f, -40f);
            autoRt.sizeDelta = new Vector2(190, 80);
            var autoImg = autoGo.GetComponent<Image>();
            autoImg.sprite = autoSpr;
            autoImg.preserveAspect = true;
            autoButton = autoGo.GetComponent<Button>();
            autoButtonGlow = autoImg;

            HookEvents();
        }

        private void BuildBackstoryUI(Transform parent, Sprite boxSpr)
        {
            var bsGo = new GameObject("BackstoryContainer", typeof(RectTransform), typeof(CanvasGroup));
            bsGo.transform.SetParent(parent, false);
            var bsRt = bsGo.GetComponent<RectTransform>();
            bsRt.anchorMin = Vector2.zero;
            bsRt.anchorMax = Vector2.one;
            bsRt.offsetMin = bsRt.offsetMax = Vector2.zero;

            backstoryContainer = bsGo;
            backstoryCanvasGroup = bsGo.GetComponent<CanvasGroup>();

            // 1. Dark Backdrop
            var bgGo = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(bsGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.015f, 0.01f, 0.03f, 1f);
            bgImg.raycastTarget = false;

            // 2. Fullscreen Illustration Image
            var illContainer = new GameObject("IllustrationContainer", typeof(RectTransform));
            illContainer.transform.SetParent(bsGo.transform, false);
            var illRt = illContainer.GetComponent<RectTransform>();
            illRt.anchorMin = Vector2.zero;
            illRt.anchorMax = Vector2.one;
            illRt.offsetMin = illRt.offsetMax = Vector2.zero;

            var imgGo = new GameObject("IllustrationImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imgGo.transform.SetParent(illContainer.transform, false);
            var imgRt = imgGo.GetComponent<RectTransform>();
            imgRt.anchorMin = Vector2.zero;
            imgRt.anchorMax = Vector2.one;
            imgRt.offsetMin = imgRt.offsetMax = Vector2.zero;
            backstoryIllustrationImage = imgGo.GetComponent<Image>();
            backstoryIllustrationImage.preserveAspect = true;
            backstoryIllustrationImage.raycastTarget = false;

            // 3. Subtitle / Dialogue Panel at bottom
            var subGo = new GameObject("SubtitlePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            subGo.transform.SetParent(bsGo.transform, false);
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 0f);
            subRt.anchorMax = new Vector2(0.5f, 0f);
            subRt.pivot = new Vector2(0.5f, 0f);
            subRt.anchoredPosition = new Vector2(0f, 25f);
            subRt.sizeDelta = new Vector2(1600f, 220f);
            backstorySubtitlePanel = subGo;

            var subBg = subGo.GetComponent<Image>();
            if (boxSpr != null)
            {
                subBg.sprite = boxSpr;
            }
            else
            {
                subBg.color = new Color(0.04f, 0.025f, 0.08f, 0.94f);
            }
            subBg.raycastTarget = false;

            // Speaker Tag
            var spkGo = new GameObject("SpeakerTag", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            spkGo.transform.SetParent(subGo.transform, false);
            var spkRt = spkGo.GetComponent<RectTransform>();
            spkRt.anchorMin = new Vector2(0f, 1f);
            spkRt.anchorMax = new Vector2(0f, 1f);
            spkRt.pivot = new Vector2(0f, 1f);
            spkRt.anchoredPosition = new Vector2(35f, -18f);
            spkRt.sizeDelta = new Vector2(350f, 40f);
            backstorySpeakerText = spkGo.GetComponent<TextMeshProUGUI>();
            backstorySpeakerText.fontSize = 26;
            backstorySpeakerText.fontStyle = FontStyles.Bold;
            backstorySpeakerText.color = new Color(0.2f, 0.95f, 1f, 1f);
            backstorySpeakerText.raycastTarget = false;

            // Action Tag
            var actGo = new GameObject("ActionTag", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            actGo.transform.SetParent(subGo.transform, false);
            var actRt = actGo.GetComponent<RectTransform>();
            actRt.anchorMin = new Vector2(0f, 1f);
            actRt.anchorMax = new Vector2(1f, 1f);
            actRt.pivot = new Vector2(0f, 1f);
            actRt.anchoredPosition = new Vector2(390f, -18f);
            actRt.sizeDelta = new Vector2(-540f, 40f);
            backstoryActionText = actGo.GetComponent<TextMeshProUGUI>();
            backstoryActionText.fontSize = 22;
            backstoryActionText.fontStyle = FontStyles.Italic;
            backstoryActionText.color = new Color(1f, 0.85f, 0.2f, 1f);
            backstoryActionText.raycastTarget = false;

            // Dialogue / Narration Text
            var dlgGo = new GameObject("DialogueText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            dlgGo.transform.SetParent(subGo.transform, false);
            var dlgRt = dlgGo.GetComponent<RectTransform>();
            dlgRt.anchorMin = new Vector2(0f, 0f);
            dlgRt.anchorMax = new Vector2(1f, 1f);
            dlgRt.offsetMin = new Vector2(40f, 35f);
            dlgRt.offsetMax = new Vector2(-40f, -65f);
            backstoryDialogueText = dlgGo.GetComponent<TextMeshProUGUI>();
            backstoryDialogueText.fontSize = 24;
            backstoryDialogueText.lineSpacing = 1.3f;
            backstoryDialogueText.color = Color.white;
            backstoryDialogueText.textWrappingMode = TextWrappingModes.Normal;
            backstoryDialogueText.raycastTarget = false;

            // Slide Counter
            var cntGo = new GameObject("SlideCounterText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            cntGo.transform.SetParent(subGo.transform, false);
            var cntRt = cntGo.GetComponent<RectTransform>();
            cntRt.anchorMin = new Vector2(1f, 1f);
            cntRt.anchorMax = new Vector2(1f, 1f);
            cntRt.pivot = new Vector2(1f, 1f);
            cntRt.anchoredPosition = new Vector2(-35f, -18f);
            cntRt.sizeDelta = new Vector2(120f, 40f);
            backstorySlideCounterText = cntGo.GetComponent<TextMeshProUGUI>();
            backstorySlideCounterText.fontSize = 22;
            backstorySlideCounterText.fontStyle = FontStyles.Bold;
            backstorySlideCounterText.color = new Color(0.2f, 0.95f, 1f, 1f);
            backstorySlideCounterText.alignment = TextAlignmentOptions.Right;
            backstorySlideCounterText.raycastTarget = false;

            // Next Indicator
            var nxtGo = new GameObject("NextPrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            nxtGo.transform.SetParent(subGo.transform, false);
            var nxtRt = nxtGo.GetComponent<RectTransform>();
            nxtRt.anchorMin = new Vector2(1f, 0f);
            nxtRt.anchorMax = new Vector2(1f, 0f);
            nxtRt.pivot = new Vector2(1f, 0f);
            nxtRt.anchoredPosition = new Vector2(-35f, 10f);
            nxtRt.sizeDelta = new Vector2(400f, 30f);
            var nxtTxt = nxtGo.GetComponent<TextMeshProUGUI>();
            nxtTxt.fontSize = 20;
            nxtTxt.fontStyle = FontStyles.Bold;
            nxtTxt.color = new Color(0f, 1f, 0.7f, 1f);
            nxtTxt.alignment = TextAlignmentOptions.Right;
            nxtTxt.text = "[ Nhấp chuột / Chạm để tiếp tục ▶ ]";
            nxtTxt.raycastTarget = false;
            backstoryNextIndicator = nxtGo;

            // 4. Tap Screen Button (Fullscreen)
            var tapGo = new GameObject("TapScreenButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tapGo.transform.SetParent(bsGo.transform, false);
            var tapRt = tapGo.GetComponent<RectTransform>();
            tapRt.anchorMin = Vector2.zero;
            tapRt.anchorMax = Vector2.one;
            tapRt.offsetMin = tapRt.offsetMax = Vector2.zero;
            var tapImg = tapGo.GetComponent<Image>();
            tapImg.color = new Color(0f, 0f, 0f, 0f);
            tapImg.raycastTarget = true;
            backstoryTapButton = tapGo.GetComponent<Button>();
            backstoryTapButton.onClick.AddListener(OnBackstoryTapClicked);

            // Mặc định ẩn BackstoryContainer
            bsGo.SetActive(false);
        }

        private void BuildIntroUI(Transform parent, Sprite frameSpr)
        {
            var introGo = new GameObject("IntroContainer", typeof(RectTransform), typeof(CanvasGroup));
            introGo.transform.SetParent(parent, false);
            var introRt = introGo.GetComponent<RectTransform>();
            introRt.anchorMin = Vector2.zero;
            introRt.anchorMax = Vector2.one;
            introRt.offsetMin = introRt.offsetMax = Vector2.zero;

            introContainer = introGo;
            introCanvasGroup = introGo.GetComponent<CanvasGroup>();

            // Dark Backdrop
            var bgGo = new GameObject("IntroDarkOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(introGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0.02f, 0.015f, 0.04f, 0.94f);
            bgImg.raycastTarget = false;

            // Fullscreen invisible tap button
            var tapGo = new GameObject("TapScreenButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tapGo.transform.SetParent(introGo.transform, false);
            var tapRt = tapGo.GetComponent<RectTransform>();
            tapRt.anchorMin = Vector2.zero;
            tapRt.anchorMax = Vector2.one;
            tapRt.offsetMin = tapRt.offsetMax = Vector2.zero;
            var tapImg = tapGo.GetComponent<Image>();
            tapImg.color = new Color(0f, 0f, 0f, 0f);
            tapImg.raycastTarget = true;
            tapToContinueButton = tapGo.GetComponent<Button>();

            // Top Banner: Logo & Chapter Title (Căn giữa màn hình cho giai đoạn hiện Logo 3s)
            var bannerGo = new GameObject("TopBanner", typeof(RectTransform), typeof(CanvasGroup));
            bannerGo.transform.SetParent(introGo.transform, false);
            var bannerRt = bannerGo.GetComponent<RectTransform>();
            bannerRt.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRt.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRt.pivot = new Vector2(0.5f, 0.5f);
            bannerRt.anchoredPosition = Vector2.zero;
            bannerRt.sizeDelta = new Vector2(960f, 360f);
            topBanner = bannerGo;
            logoCanvasGroup = bannerGo.GetComponent<CanvasGroup>();

            // Chapter Logo Image (Placeholder sẵn sàng nhận Sprite)
            var logoGo = new GameObject("ChapterLogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            logoGo.transform.SetParent(bannerGo.transform, false);
            var logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.5f, 1f);
            logoRt.anchorMax = new Vector2(0.5f, 1f);
            logoRt.pivot = new Vector2(0.5f, 1f);
            logoRt.anchoredPosition = new Vector2(0f, 0f);
            logoRt.sizeDelta = new Vector2(280f, 80f);
            chapterLogoImage = logoGo.GetComponent<Image>();
            chapterLogoImage.preserveAspect = true;
            chapterLogoImage.color = new Color(1f, 1f, 1f, 0f); // trong suốt mặc định nếu chưa có ảnh
            chapterLogoImage.raycastTarget = false;

            // Chapter Title Text
            var titleGo = new GameObject("ChapterTitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(bannerGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0f);
            titleRt.anchorMax = new Vector2(0.5f, 0f);
            titleRt.pivot = new Vector2(0.5f, 0f);
            titleRt.anchoredPosition = new Vector2(0f, 5f);
            titleRt.sizeDelta = new Vector2(1000f, 50f);
            chapterTitleText = titleGo.GetComponent<TextMeshProUGUI>();
            chapterTitleText.fontSize = 36;
            chapterTitleText.fontStyle = FontStyles.Bold;
            chapterTitleText.color = new Color(0.2f, 0.95f, 1f, 1f);
            chapterTitleText.alignment = TextAlignmentOptions.Center;
            chapterTitleText.raycastTarget = false;
            chapterTitleText.text = "CHAPTER 1: CONCRETE DREAMS";

            // Character Story Card
            var cardGo = new GameObject("CharacterStoryCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            characterStoryCard = cardGo;
            storyCardCanvasGroup = cardGo.GetComponent<CanvasGroup>();
            cardGo.transform.SetParent(introGo.transform, false);
            var cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(0f, -20f);
            cardRt.sizeDelta = new Vector2(1380f, 620f);

            var cardBg = cardGo.GetComponent<Image>();
            cardBg.color = new Color(0.045f, 0.035f, 0.085f, 0.94f);
            cardBg.raycastTarget = false;

            // Story Title (CÂU CHUYỆN CỦA RAZE)
            var sTitleGo = new GameObject("StoryTitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            sTitleGo.transform.SetParent(cardGo.transform, false);
            var sTitleRt = sTitleGo.GetComponent<RectTransform>();
            sTitleRt.anchorMin = new Vector2(0.5f, 1f);
            sTitleRt.anchorMax = new Vector2(0.5f, 1f);
            sTitleRt.pivot = new Vector2(0.5f, 1f);
            sTitleRt.anchoredPosition = new Vector2(0f, -25f);
            sTitleRt.sizeDelta = new Vector2(1200f, 50f);
            characterStoryTitleText = sTitleGo.GetComponent<TextMeshProUGUI>();
            characterStoryTitleText.fontSize = 32;
            characterStoryTitleText.fontStyle = FontStyles.Bold;
            characterStoryTitleText.color = new Color(1f, 0.2f, 0.85f, 1f);
            characterStoryTitleText.alignment = TextAlignmentOptions.Center;
            characterStoryTitleText.raycastTarget = false;
            characterStoryTitleText.text = "CÂU CHUYỆN CỦA RAZE";

            // Opponent Portrait Container
            var portGo = new GameObject("OpponentPortraitContainer", typeof(RectTransform));
            portGo.transform.SetParent(cardGo.transform, false);
            var portRt = portGo.GetComponent<RectTransform>();
            portRt.anchorMin = new Vector2(0f, 0.5f);
            portRt.anchorMax = new Vector2(0f, 0.5f);
            portRt.pivot = new Vector2(0.5f, 0.5f);
            portRt.anchoredPosition = new Vector2(250f, -20f);
            portRt.sizeDelta = new Vector2(320f, 440f);

            // Frame
            var fGo = new GameObject("PortraitFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fGo.transform.SetParent(portGo.transform, false);
            var fRt = fGo.GetComponent<RectTransform>();
            fRt.anchorMin = new Vector2(0.5f, 0.5f);
            fRt.anchorMax = new Vector2(0.5f, 0.5f);
            fRt.pivot = new Vector2(0.5f, 0.5f);
            fRt.anchoredPosition = new Vector2(0f, 30f);
            fRt.sizeDelta = new Vector2(280f, 280f);
            var fImg = fGo.GetComponent<Image>();
            fImg.sprite = frameSpr;
            fImg.preserveAspect = true;
            fImg.raycastTarget = false;

            // Inner Portrait Image
            var pImgGo = new GameObject("PortraitImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pImgGo.transform.SetParent(fGo.transform, false);
            var pImgRt = pImgGo.GetComponent<RectTransform>();
            pImgRt.anchorMin = new Vector2(0.12f, 0.12f);
            pImgRt.anchorMax = new Vector2(0.88f, 0.88f);
            pImgRt.offsetMin = pImgRt.offsetMax = Vector2.zero;
            characterIntroArtImage = pImgGo.GetComponent<Image>();
            characterIntroArtImage.sprite = opponentAvatarSprite;
            characterIntroArtImage.preserveAspect = true;
            characterIntroArtImage.raycastTarget = false;

            // Opponent Name label
            var oNameGo = new GameObject("OpponentIntroNameText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            oNameGo.transform.SetParent(portGo.transform, false);
            var oNameRt = oNameGo.GetComponent<RectTransform>();
            oNameRt.anchorMin = new Vector2(0.5f, 0f);
            oNameRt.anchorMax = new Vector2(0.5f, 0f);
            oNameRt.pivot = new Vector2(0.5f, 0f);
            oNameRt.anchoredPosition = new Vector2(0f, 30f);
            oNameRt.sizeDelta = new Vector2(280f, 45f);
            var oNameTxt = oNameGo.GetComponent<TextMeshProUGUI>();
            oNameTxt.fontSize = 28;
            oNameTxt.fontStyle = FontStyles.Bold;
            oNameTxt.color = new Color(1f, 0.25f, 0.85f, 1f);
            oNameTxt.alignment = TextAlignmentOptions.Center;
            oNameTxt.text = "RAZE";
            oNameTxt.raycastTarget = false;

            // Story Body Text
            var sBodyGo = new GameObject("CharacterStoryBodyText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            sBodyGo.transform.SetParent(cardGo.transform, false);
            var sBodyRt = sBodyGo.GetComponent<RectTransform>();
            sBodyRt.anchorMin = new Vector2(0f, 0f);
            sBodyRt.anchorMax = new Vector2(1f, 1f);
            sBodyRt.offsetMin = new Vector2(450f, 50f);
            sBodyRt.offsetMax = new Vector2(-60f, -85f);
            characterStoryBodyText = sBodyGo.GetComponent<TextMeshProUGUI>();
            characterStoryBodyText.fontSize = 25;
            characterStoryBodyText.lineSpacing = 1.35f;
            characterStoryBodyText.color = new Color(0.9f, 0.92f, 0.96f, 1f);
            characterStoryBodyText.alignment = TextAlignmentOptions.TopLeft;
            characterStoryBodyText.textWrappingMode = TextWrappingModes.Normal;
            characterStoryBodyText.raycastTarget = false;

            // Bottom Prompt (Blinking tap prompt)
            var promptGo = new GameObject("BottomPrompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            promptGo.transform.SetParent(introGo.transform, false);
            var promptRt = promptGo.GetComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(0.5f, 0f);
            promptRt.anchorMax = new Vector2(0.5f, 0f);
            promptRt.pivot = new Vector2(0.5f, 0f);
            promptRt.anchoredPosition = new Vector2(0f, 35f);
            promptRt.sizeDelta = new Vector2(900f, 45f);
            tapPromptText = promptGo.GetComponent<TextMeshProUGUI>();
            tapPromptText.fontSize = 22;
            tapPromptText.fontStyle = FontStyles.Bold;
            tapPromptText.color = new Color(0f, 1f, 0.7f, 1f);
            tapPromptText.alignment = TextAlignmentOptions.Center;
            tapPromptText.text = "CHẠM VÀO MÀN HÌNH ĐỂ TIẾP TỤC  ▶";
            tapPromptText.raycastTarget = false;
        }

        private void BuildSpeakerUI(Transform parent, string prefix, float avatarX, float nameX, Sprite frameSpr, Sprite nameTagSpr, Sprite avatarSpr, string defaultName, Color nameCol, SpeakerUI targetUI)
        {
            var speakerGo = new GameObject(prefix, typeof(RectTransform), typeof(CanvasGroup));
            speakerGo.transform.SetParent(parent, false);
            var speakerRt = speakerGo.GetComponent<RectTransform>();
            speakerRt.anchorMin = Vector2.zero;
            speakerRt.anchorMax = Vector2.one;
            speakerRt.offsetMin = speakerRt.offsetMax = Vector2.zero;

            targetUI.rootContainer = speakerGo;
            targetUI.canvasGroup = speakerGo.GetComponent<CanvasGroup>();

            // Avatar Container
            var avGo = new GameObject(prefix + "_AvatarContainer", typeof(RectTransform));
            avGo.transform.SetParent(speakerGo.transform, false);
            var avRt = avGo.GetComponent<RectTransform>();
            avRt.anchorMin = new Vector2(0.5f, 0f);
            avRt.anchorMax = new Vector2(0.5f, 0f);
            avRt.pivot = new Vector2(0.5f, 0f);
            avRt.anchoredPosition = new Vector2(avatarX, 130f);
            avRt.sizeDelta = new Vector2(230, 230);

            // Frame
            var frameGo = new GameObject("AvatarFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frameGo.transform.SetParent(avGo.transform, false);
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = frameRt.offsetMax = Vector2.zero;
            targetUI.avatarFrame = frameGo.GetComponent<Image>();
            targetUI.avatarFrame.sprite = frameSpr;
            targetUI.avatarFrame.preserveAspect = true;
            targetUI.avatarFrame.raycastTarget = false;

            // Inner Image
            var innerGo = new GameObject("AvatarImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            innerGo.transform.SetParent(frameGo.transform, false);
            var innerRt = innerGo.GetComponent<RectTransform>();
            innerRt.anchorMin = new Vector2(0.12f, 0.12f);
            innerRt.anchorMax = new Vector2(0.88f, 0.88f);
            innerRt.offsetMin = innerRt.offsetMax = Vector2.zero;
            targetUI.avatarImage = innerGo.GetComponent<Image>();
            targetUI.avatarImage.sprite = avatarSpr;
            targetUI.avatarImage.preserveAspect = true;
            targetUI.avatarImage.raycastTarget = false;

            // NameTag Container
            var nameGo = new GameObject(prefix + "_NameTagContainer", typeof(RectTransform));
            nameGo.transform.SetParent(speakerGo.transform, false);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.5f, 0f);
            nameRt.anchorMax = new Vector2(0.5f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.anchoredPosition = new Vector2(nameX, 292f);
            nameRt.sizeDelta = new Vector2(432.5f, 103f);
            targetUI.nameTagContainer = nameGo;

            var nameBgGo = new GameObject("NameTagBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            nameBgGo.transform.SetParent(nameGo.transform, false);
            var nameBgRt = nameBgGo.GetComponent<RectTransform>();
            nameBgRt.anchorMin = Vector2.zero;
            nameBgRt.anchorMax = Vector2.one;
            nameBgRt.offsetMin = nameBgRt.offsetMax = Vector2.zero;
            targetUI.nameTagBg = nameBgGo.GetComponent<Image>();
            targetUI.nameTagBg.sprite = nameTagSpr;
            targetUI.nameTagBg.preserveAspect = false;
            targetUI.nameTagBg.raycastTarget = false;

            var nameTxtGo = new GameObject("NameText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            nameTxtGo.transform.SetParent(nameGo.transform, false);
            var nameTxtRt = nameTxtGo.GetComponent<RectTransform>();
            nameTxtRt.anchorMin = Vector2.zero;
            nameTxtRt.anchorMax = Vector2.one;
            nameTxtRt.offsetMin = new Vector2(25, 10);
            nameTxtRt.offsetMax = new Vector2(-25, -10);
            targetUI.nameText = nameTxtGo.GetComponent<TextMeshProUGUI>();
            targetUI.nameText.fontSize = 28;
            targetUI.nameText.fontStyle = FontStyles.Bold;
            targetUI.nameText.alignment = TextAlignmentOptions.Center;
            targetUI.nameText.color = nameCol;
            targetUI.nameText.raycastTarget = false;
            targetUI.nameText.text = defaultName;
        }
    }
}
