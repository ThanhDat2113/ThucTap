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
        [Tooltip("File Asset chứa lời thoại của Chapter này")]
        public DialogueStoryData dialogueAsset;
        [Tooltip("Avatar đối thủ riêng của Chapter này (VD: Boss 1, Boss 2 / DJ Null)")]
        public Sprite opponentAvatar;
        [Tooltip("Tên mặc định của đối thủ")]
        public string defaultOpponentName = "BOSS";
    }

    /// <summary>
    /// Điều phối hệ thống hội thoại (Story Dialogue) trước màn chơi.
    /// - Hỗ trợ hiển thị riêng biệt 2 nhân vật (Left: Main 1, Right: Boss 1, Boss 2,...).
    /// - Hỗ trợ ĐA CHAPTER: Tự động chạy đúng hội thoại theo Week/Chapter người chơi chọn (Chapter 1, Chapter 2,...).
    /// - Nhân vật đang nói sẽ sáng rõ, nổi bật (pop animation), nhân vật kia mờ đi nhẹ.
    /// - Dễ dàng nhập/sửa danh sách thoại trực tiếp trên Story Dialogue Editor Window.
    /// </summary>
    public class StoryDialogueManager : MonoBehaviour
    {
        public static StoryDialogueManager Instance { get; private set; }

        private static bool hasCompletedDialogueThisSession = false;

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

        [Header("Speaker Bên Trái (Main 1)")]
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

        private int currentLineIndex = -1;
        private bool isTyping = false;
        private bool isAutoMode = false;
        private Coroutine typingRoutine;
        private Coroutine transitionRoutine;
        private Coroutine autoRoutine;

        public List<ChapterDialogueEntry> Chapters => chapters;
        public int TestChapter { get => testChapter; set => testChapter = value; }

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
            return "BOSS " + GetCurrentChapterNumber();
        }

        public List<DialogueLine> DialogueLines
        {
            get
            {
                var cfg = GetCurrentChapterConfig();
                if (cfg != null && cfg.dialogueAsset != null && cfg.dialogueAsset.lines != null && cfg.dialogueAsset.lines.Count > 0)
                    return cfg.dialogueAsset.lines;

                if (dialogueDataAsset != null && dialogueDataAsset.lines != null && dialogueDataAsset.lines.Count > 0)
                    return dialogueDataAsset.lines;

                return dialogueLines;
            }
        }

        public DialogueStoryData DialogueDataAsset { get => dialogueDataAsset; set => dialogueDataAsset = value; }
        public Sprite PlayerAvatarSprite { get => playerAvatarSprite; set => playerAvatarSprite = value; }
        public Sprite OpponentAvatarSprite { get => opponentAvatarSprite; set => opponentAvatarSprite = value; }
        public SpeakerUI LeftSpeaker => leftSpeaker;
        public SpeakerUI RightSpeaker => rightSpeaker;

        private void OnValidate()
        {
            if ((dialogueLines == null || dialogueLines.Count == 0) && dialogueDataAsset == null && (chapters == null || chapters.Count == 0))
            {
                InitDefaultChapter1Lines();
            }
        }

        public static bool HasPendingDialogue()
        {
            if (Instance != null && Instance.testInEditor && Application.isEditor)
                return !hasCompletedDialogueThisSession;

            bool isStoryMode = StorySession.Active;
            bool hasValidWeek = StorySession.Week != null && StorySession.Week.weekNumber > 0;

            return isStoryMode && hasValidWeek && !hasCompletedDialogueThisSession;
        }

        public static void ResetSessionState()
        {
            hasCompletedDialogueThisSession = false;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureUI();
            HookEvents();

            if (leftSpeaker != null && leftSpeaker.nameText != null)
            {
                leftSpeaker.nameText.text = GetPlayerDisplayName();
            }
        }

        private void Start()
        {
            if (HasPendingDialogue())
            {
                StartDialogue();
            }
            else
            {
                if (mainCanvasGroup != null)
                {
                    mainCanvasGroup.alpha = 0f;
                    mainCanvasGroup.blocksRaycasts = false;
                    mainCanvasGroup.interactable = false;
                }
                if (dialogueCanvas != null)
                {
                    dialogueCanvas.gameObject.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
        }

        public void StartDialogue()
        {
            if (dialogueCanvas != null)
                dialogueCanvas.gameObject.SetActive(true);

            var activeLines = DialogueLines;
            if (activeLines == null || activeLines.Count == 0)
            {
                InitDefaultChapter1Lines();
                activeLines = DialogueLines;
            }

            currentLineIndex = -1;
            isAutoMode = false;
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

            // Kích hoạt bên nói, làm mờ/ẩn bên không nói
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

                // Hiệu ứng Pop nảy nhẹ nhân vật đang nói
                if (activeSpeaker.rootContainer != null)
                    StartCoroutine(AnimateSpeakerPop(activeSpeaker.rootContainer.transform));
            }

            if (inactiveSpeaker != null)
            {
                inactiveSpeaker.SetActiveState(false, hideInactiveSpeaker, Color.gray);
            }

            // Bắt đầu gõ chữ (tự động thay thế {PLAYER} bằng tên người chơi)
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
            if (isTyping)
            {
                if (typingRoutine != null) StopCoroutine(typingRoutine);
                isTyping = false;
                if (currentLineIndex >= 0 && currentLineIndex < dialogueLines.Count && dialogueText != null)
                {
                    dialogueText.text = dialogueLines[currentLineIndex].text;
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

            if (isAutoMode && !isTyping)
            {
                if (autoRoutine != null) StopCoroutine(autoRoutine);
                autoRoutine = StartCoroutine(WaitAndAutoAdvance());
            }
            else if (!isAutoMode && autoRoutine != null)
            {
                StopCoroutine(autoRoutine);
                autoRoutine = null;
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
            hasCompletedChapter1ThisSession = true;

            if (autoRoutine != null) { StopCoroutine(autoRoutine); autoRoutine = null; }
            if (typingRoutine != null) { StopCoroutine(typingRoutine); typingRoutine = null; }
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);

            transitionRoutine = StartCoroutine(AnimateCloseDialogue());
        }

        private IEnumerator AnimateCloseDialogue()
        {
            if (mainCanvasGroup != null)
            {
                mainCanvasGroup.interactable = false;
                mainCanvasGroup.blocksRaycasts = false;
            }

            Vector2 currentPos = dialogueContainer != null ? dialogueContainer.anchoredPosition : Vector2.zero;
            Vector2 targetPos = currentPos + new Vector2(0, -60f);

            float duration = 0.3f;
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
            if (dialogueCanvas != null) dialogueCanvas.gameObject.SetActive(false);

            StartGameplay();
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

        /// <summary>
        /// Lấy Tên hiển thị (DisplayName) từ tài khoản đang đăng nhập hoặc cache đã lưu.
        /// Nếu không có sẽ mặc định trả về "Kai".
        /// </summary>
        public static string GetPlayerDisplayName()
        {
            try
            {
                // 1. Thử lấy từ AccountManager singleton (nếu đang chạy game và đã đăng nhập)
                if (Luan.MainMenu.AccountManager.Instance != null && Luan.MainMenu.AccountManager.Instance.CurrentUser != null)
                {
                    var user = Luan.MainMenu.AccountManager.Instance.CurrentUser;
                    if (!string.IsNullOrEmpty(user.displayName)) return user.displayName;
                    if (!string.IsNullOrEmpty(user.username)) return user.username;
                }
            }
            catch { }

            // 2. Thử lấy từ cache PlayerPrefs đã lưu lúc đăng nhập trước đó
            string cachedDisplay = PlayerPrefs.GetString("PLAYFAB_CACHED_DISPLAYNAME", "");
            if (!string.IsNullOrEmpty(cachedDisplay)) return cachedDisplay;

            string cachedUser = PlayerPrefs.GetString("PLAYFAB_CACHED_USERNAME", "");
            if (!string.IsNullOrEmpty(cachedUser)) return cachedUser;

            string legacyUser = PlayerPrefs.GetString("ACCOUNT_USERNAME", "");
            if (!string.IsNullOrEmpty(legacyUser)) return legacyUser;

            // 3. Tên nhân vật mặc định nếu chạy thử trong Editor mà chưa có tài khoản nào
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
                if (string.IsNullOrEmpty(rawName)) return Instance != null ? Instance.GetActiveOpponentName() : "BOSS";
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
                    speakerName = "{PLAYER}",
                    side = SpeakerSide.Left,
                    text = "Khu phố này... ánh đèn neon mờ ảo và tiếng bass dội lên từ lòng đất. Đây chính là Underground Arena.",
                    nameColor = new Color(0.2f, 0.95f, 1f, 1f),
                    customAvatar = playerAvatarSprite
                },
                new DialogueLine
                {
                    speakerName = "BOSS 1",
                    side = SpeakerSide.Right,
                    text = "Này nhóc! Dám bước chân vào địa bàn của ta thách đấu à? Nhịp điệu đường phố ở đây không dành cho kẻ yếu tim đâu!",
                    nameColor = new Color(1f, 0.2f, 0.85f, 1f),
                    customAvatar = opponentAvatarSprite
                },
                new DialogueLine
                {
                    speakerName = "{PLAYER}",
                    side = SpeakerSide.Left,
                    text = "Tôi đến đây không phải để dạo chơi. Âm nhạc của tôi sẽ khiến cả khu phố này phải thức tỉnh!",
                    nameColor = new Color(0.2f, 0.95f, 1f, 1f),
                    customAvatar = playerAvatarSprite
                },
                new DialogueLine
                {
                    speakerName = "BOSS 1",
                    side = SpeakerSide.Right,
                    text = "Hahaha! Khá lắm, {PLAYER}! Trên sàn đấu này, chỉ có nốt nhạc chuẩn xác mới có tiếng nói. LÊN NHẠC ĐI!",
                    nameColor = new Color(1f, 0.2f, 0.85f, 1f),
                    customAvatar = opponentAvatarSprite
                }
            };
        }

        public void EnsureUI()
        {
            if (dialogueCanvas != null && mainCanvasGroup != null && dialogueText != null)
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
                var avLeft = container.Find("AvatarContainer") ?? container.Find("LeftAvatarContainer");
                if (avLeft != null)
                {
                    leftSpeaker.rootContainer = avLeft.gameObject;
                    leftSpeaker.canvasGroup = avLeft.GetComponent<CanvasGroup>() ?? avLeft.gameObject.AddComponent<CanvasGroup>();
                    var frame = avLeft.Find("AvatarFrame");
                    if (frame != null) leftSpeaker.avatarFrame = frame.GetComponent<Image>();
                    var img = avLeft.Find("AvatarImage") ?? frame?.Find("AvatarImage");
                    if (img != null) leftSpeaker.avatarImage = img.GetComponent<Image>();
                }
                var nameLeft = container.Find("NameTagContainer") ?? container.Find("LeftNameTagContainer");
                if (nameLeft != null)
                {
                    leftSpeaker.nameTagContainer = nameLeft.gameObject;
                    var nameBg = nameLeft.Find("NameTagBg");
                    if (nameBg != null) leftSpeaker.nameTagBg = nameBg.GetComponent<Image>();
                    var nTxt = nameLeft.Find("NameText");
                    if (nTxt != null) leftSpeaker.nameText = nTxt.GetComponent<TMP_Text>();
                }

                // Right Speaker
                var avRight = container.Find("RightAvatarContainer");
                if (avRight != null)
                {
                    rightSpeaker.rootContainer = avRight.gameObject;
                    rightSpeaker.canvasGroup = avRight.GetComponent<CanvasGroup>() ?? avRight.gameObject.AddComponent<CanvasGroup>();
                    var frame = avRight.Find("AvatarFrame");
                    if (frame != null) rightSpeaker.avatarFrame = frame.GetComponent<Image>();
                    var img = avRight.Find("AvatarImage") ?? frame?.Find("AvatarImage");
                    if (img != null) rightSpeaker.avatarImage = img.GetComponent<Image>();
                }
                var nameRight = container.Find("RightNameTagContainer");
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
            dimOverlay.color = new Color(0.015f, 0.008f, 0.035f, 0.72f);
            dimOverlay.raycastTarget = false;

            // 3. Dialogue Container
            var containerGo = new GameObject("DialogueContainer", typeof(RectTransform));
            containerGo.transform.SetParent(canvasGo.transform, false);
            dialogueContainer = containerGo.GetComponent<RectTransform>();
            dialogueContainer.anchorMin = new Vector2(0.5f, 0f);
            dialogueContainer.anchorMax = new Vector2(0.5f, 0f);
            dialogueContainer.pivot = new Vector2(0.5f, 0f);
            dialogueContainer.anchoredPosition = new Vector2(0f, 35f);
            dialogueContainer.sizeDelta = new Vector2(1920, 380);

            // 4. Dialogue Box
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

            // 5. Dialogue Text
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

            // 6. Next Indicator
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

            // 7. Left Speaker (Main 1 / Player DisplayName)
            string playerDisplayName = GetPlayerDisplayName();
            BuildSpeakerUI(containerGo.transform, "LeftSpeaker", -580f, -491f, frameSpr, nameTagSpr, playerAvatarSprite, playerDisplayName, new Color(0.2f, 0.95f, 1f, 1f), leftSpeaker);

            // 8. Right Speaker (Boss 1)
            BuildSpeakerUI(containerGo.transform, "RightSpeaker", 580f, 491f, frameSpr, nameTagSpr, opponentAvatarSprite, "BOSS 1", new Color(1f, 0.2f, 0.85f, 1f), rightSpeaker);

            // 9. Top Controls (Skip & Auto - Đặt góc trên bên phải màn hình để không che khung thoại)
            var topControlsGo = new GameObject("TopControls", typeof(RectTransform));
            topControlsGo.transform.SetParent(canvasGo.transform, false);
            var topRt = topControlsGo.GetComponent<RectTransform>();
            topRt.anchorMin = Vector2.zero;
            topRt.anchorMax = Vector2.one;
            topRt.offsetMin = topRt.offsetMax = Vector2.zero;

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
