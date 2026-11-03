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
        Intro,
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

        private bool hasCompletedDialogueThisSession = false;
        private bool hasCompletedPostBattleThisSession = false;

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

        public List<DialogueLine> GetPostBattleLines()
        {
            var cfg = GetCurrentChapterConfig();
            if (cfg != null && cfg.dialogueAsset != null && cfg.dialogueAsset.postBattleLines != null && cfg.dialogueAsset.postBattleLines.Count > 0)
                return cfg.dialogueAsset.postBattleLines;

            if (dialogueDataAsset != null && dialogueDataAsset.postBattleLines != null && dialogueDataAsset.postBattleLines.Count > 0)
                return dialogueDataAsset.postBattleLines;

            return null;
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
            if (Instance != null && Instance.hasCompletedDialogueThisSession)
                return false;

            // 1. Nếu đang trong Story Mode (StorySession.Active) -> LUÔN CHẠY nếu chưa xong trong lượt này!
            if (StorySession.Active)
                return true;

            // 2. Nếu người chơi vào từ Free Play (chọn bài từ tab Free Play) -> Không chạy thoại
            if (!StorySession.Active && GameSession.SelectedSong != null)
                return false;

            // 3. Chế độ kiểm thử trong Editor (bấm Play trực tiếp tại GameplayScene)
            if (Application.isEditor)
            {
                if (Instance != null && Instance.testInEditor)
                    return true;
                if (GameSession.SelectedSong == null)
                    return true;
            }

            return false;
        }

        public bool HasPendingPostBattleDialogue()
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

            var post = GetPostBattleLines();
            return post != null && post.Count > 0;
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
                StartDialogue();
            }
            else
            {
                HideAllUI();
            }
        }

        private void OnDestroy()
        {
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
                if (HasPendingPostBattleDialogue())
                {
                    StartCoroutine(DelayedStartPostBattle());
                }
            }
        }

        private IEnumerator DelayedStartPostBattle()
        {
            yield return new WaitForSecondsRealtime(0.2f);
            StartPostBattleDialogue();
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
            if (dialogueContainer != null) dialogueContainer.gameObject.SetActive(false);
            if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (currentPhase == DialoguePhase.None) return;

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
            var cfg = GetCurrentChapterConfig();
            var data = cfg != null ? cfg.dialogueAsset : dialogueDataAsset;
            bool hasIntro = introContainer != null && (data != null && (!string.IsNullOrEmpty(data.characterStory) || data.chapterLogo != null || !string.IsNullOrEmpty(data.characterStoryTitle)));

            if (hasIntro)
            {
                ShowIntroScreen();
            }
            else
            {
                StartPreBattleDialogue();
            }
        }

        public void ShowIntroScreen()
        {
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

                // 1. Chapter Logo (Khởi tạo sẵn sàng cho Animation Fade In & Scale Pop)
                Sprite logoSpr = data != null ? data.chapterLogo : null;
                if (chapterLogoImage != null)
                {
                    if (logoSpr != null)
                    {
                        chapterLogoImage.sprite = logoSpr;
                        chapterLogoImage.color = new Color(1f, 1f, 1f, 0f); // Bắt đầu ẩn hoàn toàn để chạy Fade In
                        chapterLogoImage.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
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
                if (chapterLogoImage != null) chapterLogoImage.gameObject.SetActive(true);

                if (logoCanvasGroup != null)
                {
                    logoCanvasGroup.alpha = 0f;
                    logoCanvasGroup.blocksRaycasts = true;
                    logoCanvasGroup.interactable = true;
                }

                // 1. ANIMATION FADE IN & SCALE POP (0.6s)
                float fadeInTime = 0.6f;
                float elapsed = 0f;
                Vector3 initialScale = new Vector3(0.8f, 0.8f, 1f);
                Vector3 baseScale = Vector3.one;

                while (elapsed < fadeInTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeInTime);
                    float easeAlpha = Mathf.SmoothStep(0f, 1f, t);
                    float easeScale = 1f - Mathf.Pow(1f - t, 3); // Cubic ease out pop

                    if (logoCanvasGroup != null) logoCanvasGroup.alpha = easeAlpha;
                    if (chapterLogoImage != null)
                    {
                        chapterLogoImage.color = new Color(1f, 1f, 1f, easeAlpha);
                        chapterLogoImage.transform.localScale = Vector3.LerpUnclamped(initialScale, baseScale, easeScale);
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
                    chapterLogoImage.color = Color.white;
                    chapterLogoImage.transform.localScale = Vector3.one;
                }
                if (dimOverlay != null)
                {
                    var c = dimOverlay.color;
                    dimOverlay.color = new Color(c.r, c.g, c.b, targetDim);
                }

                // 2. GIỮ LOGO 3 GIÂY KÈM HIỆU ỨNG NHỊP THỞ (BREATHING PULSE)
                float holdDuration = Mathf.Max(1.0f, logoDisplayDuration);
                float holdTimer = 0f;
                while (holdTimer < holdDuration && !skipLogoRequested)
                {
                    holdTimer += Time.unscaledDeltaTime;
                    // Nhịp thở nhẹ nhàng 1.0 -> 1.03 để logo sống động
                    float pulse = 1f + 0.025f * Mathf.Sin(holdTimer * 3.5f);
                    if (chapterLogoImage != null)
                    {
                        chapterLogoImage.transform.localScale = new Vector3(pulse, pulse, 1f);
                    }
                    yield return null;
                }

                // 3. ANIMATION FADE OUT & SCALE EXPAND (0.45s)
                float fadeOutTime = 0.45f;
                elapsed = 0f;
                Vector3 currentScale = chapterLogoImage != null ? chapterLogoImage.transform.localScale : Vector3.one;
                Vector3 dissipateScale = currentScale * 1.07f; // bung nhẹ ra khi tan biến

                while (elapsed < fadeOutTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeOutTime);
                    float ease = Mathf.SmoothStep(0f, 1f, t);

                    if (logoCanvasGroup != null) logoCanvasGroup.alpha = 1f - ease;
                    if (chapterLogoImage != null)
                    {
                        chapterLogoImage.color = new Color(1f, 1f, 1f, 1f - ease);
                        chapterLogoImage.transform.localScale = Vector3.Lerp(currentScale, dissipateScale, ease);
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
                    chapterLogoImage.color = new Color(1f, 1f, 1f, 0f);
                    chapterLogoImage.transform.localScale = Vector3.one;
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
                characterStoryCard.transform.localScale = new Vector3(0.94f, 0.94f, 1f);
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

            // Fade in Story Card mượt mà kết hợp scale pop (0.5s)
            float cardFadeTime = 0.5f;
            float cardElapsed = 0f;
            Vector3 cardStartScale = new Vector3(0.94f, 0.94f, 1f);
            while (cardElapsed < cardFadeTime)
            {
                cardElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(cardElapsed / cardFadeTime);
                float easeAlpha = Mathf.SmoothStep(0f, 1f, t);
                float easeScale = 1f - Mathf.Pow(1f - t, 3);

                if (storyCardCanvasGroup != null) storyCardCanvasGroup.alpha = easeAlpha;
                if (characterStoryCard != null)
                {
                    characterStoryCard.transform.localScale = Vector3.LerpUnclamped(cardStartScale, Vector3.one, easeScale);
                }
                yield return null;
            }
            if (storyCardCanvasGroup != null) storyCardCanvasGroup.alpha = 1f;
            if (characterStoryCard != null) characterStoryCard.transform.localScale = Vector3.one;

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

        public void StartPostBattleDialogue()
        {
            currentPhase = DialoguePhase.PostBattle;

            if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(true);
            if (introContainer != null) introContainer.SetActive(false);
            if (dialogueContainer != null) dialogueContainer.gameObject.SetActive(true);
            if (topControlsGroup != null) topControlsGroup.SetActive(true);

            currentLineIndex = -1;
            UpdateAutoButtonVisual();

            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = StartCoroutine(AnimateOpenDialogue());
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

        public void FinishDialogue()
        {
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
            var battleUI = FindFirstObjectByType<RhythmGame.BattleUIController>(FindObjectsInactive.Include);
            RhythmGame.TurnResultPanel resultPanel = battleUI != null ? battleUI.turnResultPanel : null;
            if (resultPanel == null)
            {
                resultPanel = FindFirstObjectByType<RhythmGame.TurnResultPanel>(FindObjectsInactive.Include);
            }

            if (resultPanel != null)
            {
                resultPanel.ShowFinalResult(RhythmGame.GameState.Finished);
            }
            else
            {
                Debug.LogWarning("[StoryDialogueManager] Không tìm thấy TurnResultPanel để hiển thị kết quả sau trận!");
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

        public void EnsureUI()
        {
            if (dialogueCanvas != null && mainCanvasGroup != null && dialogueText != null && introContainer != null)
                return;

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
