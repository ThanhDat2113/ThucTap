using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Luan.MainMenu
{
    /// <summary>
    /// Script điều phối và quản lý toàn bộ luồng giao diện Main Menu.
    /// Tích hợp đóng mở mượt mà giữa MainMenu và các màn phụ (Đăng nhập/Đăng ký PlayFab, Cài đặt, Thành tích).
    /// Tự động cập nhật trạng thái hiển thị thông tin người chơi khi đăng nhập hoặc đăng xuất.
    /// </summary>
    [AddComponentMenu("UI/Luan MainMenu/Main Menu Manager")]
    public class MainMenuManager : MonoBehaviour
    {
        public static MainMenuManager Instance { get; private set; }

        [Header("--- Quản Lý Panel (UIPanelAnimators) ---")]
        [Tooltip("Panel chính chứa các nút Bắt đầu, Cài đặt, Thành tích...")]
        [SerializeField] private UIPanelAnimator mainMenuPanel;

        [Tooltip("Panel Thông tin người dùng (UserInfo) - trượt cùng lúc với 3 nút MainMenu")]
        [SerializeField] private UIPanelAnimator userInfoPanel;

        [Tooltip("Panel Cài Đặt (Tùy chọn)")]
        [SerializeField] private UIPanelAnimator settingsPanel;

        [Tooltip("Panel Thành Tích / Bảng xếp hạng (Tùy chọn)")]
        [SerializeField] private UIPanelAnimator leaderboardPanel;

        [Tooltip("Tham chiếu trực tiếp tới AccountManager (nếu để trống script sẽ tự tìm)")]
        [SerializeField] private AccountManager accountManager;

        [Header("--- Điều Khiển Vị Trí Logo (Logo Position & Slide) ---")]
        [Tooltip("RectTransform của Logo (nếu để trống script sẽ tự tìm 'Title_logo_RawImage')")]
        [SerializeField] private RectTransform logoRectTransform;

        [Tooltip("Vị trí Logo bên trái (khi đã đăng nhập xong - vị trí mặc định ban đầu)")]
        [SerializeField] private Vector2 logoLeftPosition = new Vector2(-549.5f, 320f);

        [Tooltip("Vị trí Logo bên phải (khi mới vào game chưa đăng nhập hoặc khi đăng xuất - vị trí bạn tùy chỉnh)")]
        [SerializeField] private Vector2 logoRightPosition = new Vector2(549.5f, 320f);

        [Tooltip("Thời gian trượt của Logo giữa 2 vị trí (giây)")]
        [Range(0.1f, 2.0f)]
        [SerializeField] private float logoSlideDuration = 0.5f;

        [Tooltip("Đường cong gia tốc trượt mượt mà (Ease Curve)")]
        [SerializeField] private AnimationCurve logoSlideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("--- Hiển Thị Thông Tin Người Chơi (User Info) ---")]
        [Tooltip("Text hiển thị tên người chơi trong khung UserInfo (TMP)")]
        [SerializeField] private TMP_Text userNameTMP;

        [Tooltip("Độ dài tối đa của tên trước khi thêm '...' để không bị tràn khung hay xuống dòng")]
        [Range(4, 25)]
        [SerializeField] private int maxNameLength = 12;

        [Tooltip("Nút Đăng Xuất (Logout Button)")]
        [SerializeField] private Button logoutButton;

        [Tooltip("Text chào mừng người chơi (Tùy chọn, TextMeshPro)")]
        [SerializeField] private TMP_Text userGreetingTMP;
        [Tooltip("Text chào mừng người chơi (Legacy UI Text)")]
        [SerializeField] private Text userGreetingLegacy;

        [Tooltip("Nút mở Đăng nhập / Tài khoản (Tùy chọn)")]
        [SerializeField] private Button accountButton;
        [Tooltip("Text trên nút Account (ví dụ: 'Đăng Nhập' khi chưa login, hoặc Username khi đã login)")]
        [SerializeField] private TMP_Text accountButtonTMP;
        [SerializeField] private Text accountButtonLegacy;

        [Header("--- Cấu Hình Khởi Động (Startup Flow) ---")]
        [Tooltip("Khi vừa mở game, có tự động hiện bảng Đăng Nhập trước và ẩn 3 nút Menu không? (Nếu đang test chưa làm bảng đăng nhập thì bỏ tick ô này)")]
        [SerializeField] private bool showLoginOnStart = false;

        [Header("--- Cấu Hình Trò Chơi (Game Settings) ---")]
        [Tooltip("Nút Bắt Đầu chơi (Play Button) - Tự động tìm nếu để trống")]
        [SerializeField] private Button playButton;

        [Tooltip("Có bắt buộc người chơi phải Đăng nhập tài khoản PlayFab trước khi BẮT ĐẦU chơi không?")]
        [SerializeField] private bool requireLoginToPlay = false;

        [Tooltip("Tên Scene tiếp theo khi bấm BẮT ĐẦU (ví dụ: 'Thuan_SongList')")]
        [SerializeField] private string playSceneName = "Thuan_SongList";

        [Header("--- Âm Thanh (Audio SFX) ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip startGameSound;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

#if UNITY_ANDROID || UNITY_IOS
            // Khóa màn hình ngang (Landscape), chặn hoàn toàn màn hình dọc (Portrait)
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
#endif

            if (accountManager == null)
            {
                accountManager = FindFirstObjectByType<AccountManager>();
            }

            FindLogoIfNeeded();

            if (!TryGetComponent<VideoOptimizationManager>(out _))
            {
                gameObject.AddComponent<VideoOptimizationManager>();
            }

            if (logoutButton != null)
            {
                logoutButton.onClick.AddListener(OnLogoutClicked);
            }

            // Tự động tìm ButtonPlay nếu chưa kéo thả vào Inspector
            if (playButton == null)
            {
                var allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var b in allButtons)
                {
                    if (b != null && (b.name == "ButtonPlay" || b.name == "ButtonBatDau"))
                    {
                        playButton = b;
                        break;
                    }
                }
            }

            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnBatDauClicked);
                playButton.onClick.AddListener(OnBatDauClicked);
            }

            // Tự động tải âm thanh mặc định nếu chưa gán trong Inspector
            if (buttonClickSound == null)
            {
                buttonClickSound = Resources.Load<AudioClip>("Audio/select_001");
#if UNITY_EDITOR
                if (buttonClickSound == null)
                    buttonClickSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thuan/Audio/SFX/select_001.wav");
#endif
            }

            if (startGameSound == null || (startGameSound != null && startGameSound.name == "menu"))
            {
                startGameSound = Resources.Load<AudioClip>("Audio/confirmation_001");
#if UNITY_EDITOR
                if (startGameSound == null || startGameSound.name == "menu")
                    startGameSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thuan/Audio/SFX/confirmation_001.wav");
#endif
            }
        }

        private void OnEnable()
        {
            // Lắng nghe sự kiện đăng nhập / đăng xuất từ AccountManager (PlayFab)
            AccountManager.OnLoginSuccess += HandleLoginSuccess;
            AccountManager.OnRegisterSuccess += HandleRegisterSuccess;
            AccountManager.OnLogout += HandleLogout;

            // Lắng nghe sự kiện mở / đóng bảng Cài Đặt để tự động trượt Logo tương ứng
            if (settingsPanel != null)
            {
                settingsPanel.onShowStarted.AddListener(SlideLogoToRight);
                settingsPanel.onHideStarted.AddListener(SlideLogoToLeft);
            }
        }

        private void OnDisable()
        {
            AccountManager.OnLoginSuccess -= HandleLoginSuccess;
            AccountManager.OnRegisterSuccess -= HandleRegisterSuccess;
            AccountManager.OnLogout -= HandleLogout;

            if (settingsPanel != null)
            {
                settingsPanel.onShowStarted.RemoveListener(SlideLogoToRight);
                settingsPanel.onHideStarted.RemoveListener(SlideLogoToLeft);
            }
        }

        private void Start()
        {
            SetupButtonSounds();

            if (accountManager == null)
            {
                accountManager = AccountManager.Instance != null ? AccountManager.Instance : FindFirstObjectByType<AccountManager>();
            }

            // Tự động tìm SettingsPanel nếu chưa gán trong Inspector
            if (settingsPanel == null)
            {
                var sm = FindFirstObjectByType<SettingsManager>();
                if (sm != null)
                {
                    settingsPanel = sm.GetComponent<UIPanelAnimator>();
                    if (settingsPanel != null)
                    {
                        settingsPanel.onShowStarted.AddListener(SlideLogoToRight);
                        settingsPanel.onHideStarted.AddListener(SlideLogoToLeft);
                    }
                }
            }

            UpdateUserDisplay();

            bool hasSession = accountManager != null && accountManager.IsLoggedIn;

            // Thiết lập vị trí Logo ban đầu ngay khi vào game:
            // Chưa đăng nhập -> Logo đặt ở bên phải (vị trí bạn tùy chỉnh) để nhường chỗ trống phóng to bảng Đăng nhập/Đăng ký bên trái
            // Đã đăng nhập sẵn -> Logo đặt ở bên trái (vị trí gốc)
            if (hasSession)
            {
                SetLogoPositionImmediate(logoLeftPosition);
            }
            else
            {
                SetLogoPositionImmediate(logoRightPosition);
            }

            // Nếu người chơi chưa đăng nhập: Ẩn 3 nút Menu chính và UserInfo, hiện bảng Đăng nhập trước
            if (showLoginOnStart && !hasSession)
            {
                if (mainMenuPanel != null) mainMenuPanel.HideImmediate();
                if (userInfoPanel != null) userInfoPanel.HideImmediate();

                if (accountManager != null)
                {
                    accountManager.OpenAccountPanel(true);
                }
            }
            else
            {
                // Nếu đã đăng nhập sẵn rồi (hoặc vừa từ SongList back về):
                if (accountManager != null)
                {
                    accountManager.CloseAccountPanel();
                }

                // Chạy animation xuất hiện mượt mà cho MainMenu và UserInfo khi vừa vào / back về
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.HideImmediate();
                    mainMenuPanel.Show();
                }

                if (userInfoPanel != null)
                {
                    if (hasSession)
                    {
                        userInfoPanel.HideImmediate();
                        userInfoPanel.Show();
                    }
                    else
                    {
                        userInfoPanel.HideImmediate();
                    }
                }
            }
        }

        #region Các Nút Bấm Main Menu (Button Actions)

        /// <summary>
        /// Gắn vào nút "BẮT ĐẦU" (Play Button).
        /// </summary>
        public void OnBatDauClicked()
        {
            if (startGameSound != null)
            {
                PlaySound(startGameSound);
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayConfirmSound();
            }
            else
            {
                PlayButtonClickSound();
            }

            // Nếu cài đặt bắt buộc đăng nhập mà chưa đăng nhập PlayFab -> mở bảng đăng nhập
            if (requireLoginToPlay && (accountManager == null || !accountManager.IsLoggedIn))
            {
                Debug.Log("[MainMenuManager] Cần đăng nhập tài khoản PlayFab để bắt đầu chơi! Đang mở bảng đăng nhập...");
                OpenAccountPanel();
                return;
            }

            // Chuyển sang scene bài hát (mặc định Thuan_SongList)
            string targetScene = !string.IsNullOrEmpty(playSceneName) ? playSceneName : "Thuan_SongList";
            Debug.Log($"[MainMenuManager] Đang tải Scene trò chơi: {targetScene}");
            SceneManager.LoadScene(targetScene);
        }

        /// <summary>
        /// Gắn vào nút "CÀI ĐẶT" (Settings Button).
        /// </summary>
        public void OnCaiDatClicked()
        {
            PlayButtonClickSound();

            if (mainMenuPanel != null && mainMenuPanel.IsOpen)
            {
                mainMenuPanel.Hide();
            }

            if (userInfoPanel != null && userInfoPanel.IsOpen)
            {
                userInfoPanel.Hide();
            }

            if (settingsPanel != null)
            {
                settingsPanel.Show();
            }
            else
            {
                Debug.LogWarning("[MainMenuManager] Bấm Cài Đặt nhưng chưa gán settingsPanel trong Inspector!");
            }

            // Mở Cài Đặt: Logo trượt mượt mà sang vị trí bên phải
            SlideLogoToPosition(logoRightPosition);
        }

        /// <summary>
        /// Gắn vào nút "THÀNH TÍCH" (Leaderboard / Achievements Button).
        /// </summary>
        public void OnThanhTichClicked()
        {
            PlayButtonClickSound();

            if (mainMenuPanel != null && mainMenuPanel.IsOpen)
            {
                mainMenuPanel.Hide();
            }

            if (userInfoPanel != null && userInfoPanel.IsOpen)
            {
                userInfoPanel.Hide();
            }

            if (leaderboardPanel != null)
            {
                leaderboardPanel.Show();
            }
            else
            {
                Debug.Log("[MainMenuManager] Bấm Thành Tích (Chưa gán leaderboardPanel trong Inspector)");
            }
        }

        /// <summary>
        /// Gắn vào nút mở Đăng Nhập / Quản lý Tài Khoản PlayFab (Account Button).
        /// </summary>
        public void OpenAccountPanel()
        {
            PlayButtonClickSound();

            if (mainMenuPanel != null && mainMenuPanel.IsOpen)
            {
                mainMenuPanel.Hide();
            }

            if (userInfoPanel != null && userInfoPanel.IsOpen)
            {
                userInfoPanel.Hide();
            }

            if (accountManager != null)
            {
                accountManager.OpenAccountPanel(true);
            }
            else
            {
                Debug.LogWarning("[MainMenuManager] Không tìm thấy AccountManager trong Scene!");
            }

            // Nếu mở bảng tài khoản mà chưa đăng nhập -> trượt Logo sang phải để nhường không gian
            if (accountManager == null || !accountManager.IsLoggedIn)
            {
                SlideLogoToPosition(logoRightPosition);
            }
        }

        /// <summary>
        /// Gắn vào nút "QUAY LẠI" (Back / Close Button) từ bất kỳ panel con nào để trở về MainMenu.
        /// </summary>
        public void BackToMainMenu()
        {
            PlayCloseSound();

            if (settingsPanel != null && settingsPanel.IsOpen)
            {
                settingsPanel.Hide();
            }

            if (leaderboardPanel != null && leaderboardPanel.IsOpen)
            {
                leaderboardPanel.Hide();
            }

            if (accountManager != null)
            {
                accountManager.CloseAuth();
            }

            // Hiện lại 3 nút Main Menu
            if (mainMenuPanel != null && !mainMenuPanel.IsOpen)
            {
                mainMenuPanel.Show();
            }

            // Hiện lại khung UserInfo nếu đã đăng nhập
            bool loggedIn = accountManager != null && accountManager.IsLoggedIn;
            if (loggedIn && userInfoPanel != null && !userInfoPanel.IsOpen)
            {
                userInfoPanel.Show();
            }

            // Khi quay lại Menu chính: Đưa Logo trượt mượt mà về lại vị trí ban đầu bên trái
            SlideLogoToPosition(logoLeftPosition);
        }

        /// <summary>
        /// Gắn vào nút "ĐĂNG XUẤT" (Logout Button).
        /// </summary>
        public void OnLogoutClicked()
        {
            PlayCloseSound();

            if (accountManager != null)
            {
                accountManager.Logout();
            }
            else if (AccountManager.Instance != null)
            {
                AccountManager.Instance.Logout();
            }
        }

        /// <summary>
        /// Gắn vào nút "THOÁT" (Quit Button).
        /// </summary>
        public void OnThoatClicked()
        {
            PlayButtonClickSound();
            Debug.Log("[MainMenuManager] Thoát trò chơi!");

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Xử Lý Trạng Thái Người Dùng (User State)

        private void HandleLoginSuccess(PlayFabUserData account)
        {
            UpdateUserDisplay();

            // Đăng nhập thành công: Trượt Logo mượt mà về vị trí bên trái!
            SlideLogoToPosition(logoLeftPosition);

            // Hiện MainMenu (3 nút) và khung UserInfo trượt cùng lúc!
            if (mainMenuPanel != null && !mainMenuPanel.IsOpen)
            {
                mainMenuPanel.Show();
            }

            if (userInfoPanel != null && !userInfoPanel.IsOpen)
            {
                userInfoPanel.Show();
            }
        }

        private void HandleRegisterSuccess(PlayFabUserData account)
        {
            UpdateUserDisplay();
        }

        private void HandleLogout()
        {
            UpdateUserDisplay();

            // Đăng xuất: Trượt Logo mượt mà sang vị trí bên phải để nhường không gian bên trái cho bảng Đăng nhập/Đăng ký!
            SlideLogoToPosition(logoRightPosition);

            // Ẩn 3 nút MainMenu và ẩn UserInfo, mở lại bảng Đăng nhập
            if (mainMenuPanel != null && mainMenuPanel.IsOpen)
            {
                mainMenuPanel.Hide();
            }

            if (userInfoPanel != null && userInfoPanel.IsOpen)
            {
                userInfoPanel.Hide();
            }

            if (accountManager != null)
            {
                accountManager.OpenAccountPanel(true);
            }
        }

        /// <summary>
        /// Cắt ngắn tên nếu quá dài để không bị đẩy xuống dòng và thêm '...'
        /// </summary>
        private string FormatDisplayName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return string.Empty;

            string trimmed = rawName.Trim();
            if (trimmed.Length > maxNameLength)
            {
                return trimmed.Substring(0, maxNameLength) + "...";
            }
            return trimmed;
        }

        public void UpdateUserDisplay()
        {
            bool loggedIn = accountManager != null && accountManager.IsLoggedIn;
            string displayName = "";

            if (loggedIn && accountManager.CurrentUser != null)
            {
                displayName = !string.IsNullOrEmpty(accountManager.CurrentUser.displayName)
                    ? accountManager.CurrentUser.displayName
                    : accountManager.CurrentUser.username;
            }

            string formattedName = FormatDisplayName(displayName);

            // 1. Hiển thị tên người chơi trong khung UserInfo
            if (userNameTMP != null)
            {
                userNameTMP.textWrappingMode = TextWrappingModes.NoWrap; // Không bao giờ tự động xuống dòng
                userNameTMP.overflowMode = TextOverflowModes.Ellipsis; // Đảm bảo TMP thêm ... nếu chạm biên RectTransform
                userNameTMP.text = formattedName;
            }

            // 2. Text chào mừng hoặc text trên nút cũ (nếu có dùng)
            string greetingText = loggedIn ? formattedName : "";
            if (userGreetingTMP != null) userGreetingTMP.text = greetingText;
            if (userGreetingLegacy != null) userGreetingLegacy.text = greetingText;

            string btnText = loggedIn ? formattedName : "Đăng Nhập";
            if (accountButtonTMP != null) accountButtonTMP.text = btnText;
            if (accountButtonLegacy != null) accountButtonLegacy.text = btnText;
        }

        #endregion

        #region Audio Helpers

        private void SetupButtonSounds()
        {
            // Tự động tìm tất cả các nút Button trong Canvas để gắn âm thanh phản hồi
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var btn in allButtons)
            {
                if (btn == null) continue;

                // Các nút chính đã có hàm xử lý trong MainMenuManager thì không gắn để tránh trùng lặp âm
                if (btn == logoutButton || btn == playButton) continue;
                string btnName = btn.name;
                if (btnName.Contains("BatDau") || btnName.Contains("Play") || btnName.Contains("CaiDat") || btnName.Contains("ThanhTich"))
                    continue;

                // Nếu nút chưa có component âm thanh thì tự động gắn UIButtonSound
                if (btn.GetComponent<UIButtonSound>() == null)
                {
                    UIButtonSound btnSound = btn.gameObject.AddComponent<UIButtonSound>();
                    string lower = btnName.ToLower();
                    if (lower.Contains("close") || lower.Contains("dong") || lower.Contains("back") || lower.Contains("thoat") || lower.Contains("huy"))
                    {
                        btnSound.SetSoundType(ButtonSoundType.Close);
                    }
                    else if (lower.Contains("confirm") || lower.Contains("xacnhan"))
                    {
                        btnSound.SetSoundType(ButtonSoundType.Confirm);
                    }
                    else
                    {
                        btnSound.SetSoundType(ButtonSoundType.Select);
                    }
                }
            }
        }

        private void PlayButtonClickSound()
        {
            if (buttonClickSound != null)
            {
                PlaySound(buttonClickSound);
            }
            else if (AudioManager.Instance != null && AudioManager.Instance.ButtonSelectClip != null)
            {
                PlaySound(AudioManager.Instance.ButtonSelectClip);
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySelectSound();
            }
        }

        private void PlayCloseSound()
        {
            if (AudioManager.Instance != null && AudioManager.Instance.ButtonCloseClip != null)
            {
                PlaySound(AudioManager.Instance.ButtonCloseClip);
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCloseSound();
            }
            else
            {
                PlayButtonClickSound();
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip == null) return;

            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.PlaySFX(clip);
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(clip);
            }
            else if (audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region Điều Khiển Vị Trí & Hiệu Ứng Trượt Logo (Logo Slide System)

        private Coroutine _logoSlideRoutine;

        private void FindLogoIfNeeded()
        {
            if (logoRectTransform == null)
            {
                var logoGO = GameObject.Find("Title_logo_RawImage");
                if (logoGO != null)
                {
                    logoRectTransform = logoGO.GetComponent<RectTransform>();
                }
                else
                {
                    var allRects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var r in allRects)
                    {
                        if (r != null && r.name.ToLower().Contains("logo"))
                        {
                            logoRectTransform = r;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Trượt Logo mượt mà đến vị trí mục tiêu theo đường cong gia tốc.
        /// </summary>
        public void SlideLogoToPosition(Vector2 targetPos, float duration = -1f)
        {
            FindLogoIfNeeded();
            if (logoRectTransform == null) return;

            // Nếu đã ở đúng vị trí (sai số nhỏ hơn 0.1px) thì không cần chạy coroutine
            if ((logoRectTransform.anchoredPosition - targetPos).sqrMagnitude < 0.01f)
            {
                logoRectTransform.anchoredPosition = targetPos;
                return;
            }

            if (duration < 0f) duration = logoSlideDuration;

            if (_logoSlideRoutine != null)
            {
                StopCoroutine(_logoSlideRoutine);
            }
            _logoSlideRoutine = StartCoroutine(SlideLogoRoutine(targetPos, duration));
        }

        private IEnumerator SlideLogoRoutine(Vector2 targetPos, float duration)
        {
            Vector2 startPos = logoRectTransform.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveT = logoSlideCurve != null ? logoSlideCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);
                logoRectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, curveT);
                yield return null;
            }

            logoRectTransform.anchoredPosition = targetPos;
            _logoSlideRoutine = null;
        }

        /// <summary>
        /// Đặt vị trí Logo ngay lập tức (không chạy hiệu ứng trượt). Dùng khi vừa vào game.
        /// </summary>
        public void SetLogoPositionImmediate(Vector2 targetPos)
        {
            FindLogoIfNeeded();
            if (logoRectTransform == null) return;

            if (_logoSlideRoutine != null)
            {
                StopCoroutine(_logoSlideRoutine);
                _logoSlideRoutine = null;
            }
            logoRectTransform.anchoredPosition = targetPos;
        }

        public void SlideLogoToLeft() => SlideLogoToPosition(logoLeftPosition);
        public void SlideLogoToRight() => SlideLogoToPosition(logoRightPosition);

#if UNITY_EDITOR
        private void OnValidate()
        {
            FindLogoIfNeeded();
        }

        [ContextMenu("Logo/Lưu vị trí hiện tại làm Vị Trí Bên Phải (Right)")]
        public void SaveCurrentAsRightPosition()
        {
            FindLogoIfNeeded();
            if (logoRectTransform != null)
            {
                Undo.RecordObject(this, "Save Logo Right Position");
                logoRightPosition = logoRectTransform.anchoredPosition;
                EditorUtility.SetDirty(this);
                Debug.Log($"[MainMenuManager] Đã lưu Vị Trí Bên Phải của Logo: {logoRightPosition}");
            }
            else
            {
                Debug.LogWarning("[MainMenuManager] Chưa tìm thấy logoRectTransform để lưu vị trí!");
            }
        }

        [ContextMenu("Logo/Lưu vị trí hiện tại làm Vị Trí Bên Trái (Left)")]
        public void SaveCurrentAsLeftPosition()
        {
            FindLogoIfNeeded();
            if (logoRectTransform != null)
            {
                Undo.RecordObject(this, "Save Logo Left Position");
                logoLeftPosition = logoRectTransform.anchoredPosition;
                EditorUtility.SetDirty(this);
                Debug.Log($"[MainMenuManager] Đã lưu Vị Trí Bên Trái của Logo: {logoLeftPosition}");
            }
            else
            {
                Debug.LogWarning("[MainMenuManager] Chưa tìm thấy logoRectTransform để lưu vị trí!");
            }
        }

        [ContextMenu("Logo/Đưa Logo về Vị Trí Bên Phải (Right)")]
        public void MoveLogoToRightInEditor()
        {
            FindLogoIfNeeded();
            if (logoRectTransform != null)
            {
                Undo.RecordObject(logoRectTransform, "Move Logo To Right");
                logoRectTransform.anchoredPosition = logoRightPosition;
                EditorUtility.SetDirty(logoRectTransform);
                Debug.Log($"[MainMenuManager] Đã chuyển Logo về Vị Trí Bên Phải: {logoRightPosition}");
            }
            else
            {
                Debug.LogWarning("[MainMenuManager] Chưa tìm thấy logoRectTransform để di chuyển!");
            }
        }

        [ContextMenu("Logo/Đưa Logo về Vị Trí Bên Trái (Left)")]
        public void MoveLogoToLeftInEditor()
        {
            FindLogoIfNeeded();
            if (logoRectTransform != null)
            {
                Undo.RecordObject(logoRectTransform, "Move Logo To Left");
                logoRectTransform.anchoredPosition = logoLeftPosition;
                EditorUtility.SetDirty(logoRectTransform);
                Debug.Log($"[MainMenuManager] Đã chuyển Logo về Vị Trí Bên Trái: {logoLeftPosition}");
            }
            else
            {
                Debug.LogWarning("[MainMenuManager] Chưa tìm thấy logoRectTransform để di chuyển!");
            }
        }
#endif

        #endregion
    }
}
