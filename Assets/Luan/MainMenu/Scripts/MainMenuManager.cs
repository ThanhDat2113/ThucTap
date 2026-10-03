using System;
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
        [Tooltip("Có bắt buộc người chơi phải Đăng nhập tài khoản PlayFab trước khi BẮT ĐẦU chơi không?")]
        [SerializeField] private bool requireLoginToPlay = false;

        [Tooltip("Tên Scene tiếp theo khi bấm BẮT ĐẦU (ví dụ: 'GamePlay' hoặc 'SongSelect')")]
        [SerializeField] private string playSceneName = "";

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

            if (accountManager == null)
            {
                accountManager = FindFirstObjectByType<AccountManager>();
            }

            if (logoutButton != null)
            {
                logoutButton.onClick.AddListener(OnLogoutClicked);
            }
        }

        private void OnEnable()
        {
            // Lắng nghe sự kiện đăng nhập / đăng xuất từ AccountManager (PlayFab)
            AccountManager.OnLoginSuccess += HandleLoginSuccess;
            AccountManager.OnRegisterSuccess += HandleRegisterSuccess;
            AccountManager.OnLogout += HandleLogout;
        }

        private void OnDisable()
        {
            AccountManager.OnLoginSuccess -= HandleLoginSuccess;
            AccountManager.OnRegisterSuccess -= HandleRegisterSuccess;
            AccountManager.OnLogout -= HandleLogout;
        }

        private void Start()
        {
            UpdateUserDisplay();

            bool hasSession = accountManager != null && accountManager.IsLoggedIn;

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
                // Nếu đã đăng nhập sẵn rồi: Hiện 3 nút menu chính và UserInfo
                if (mainMenuPanel != null) mainMenuPanel.ShowImmediate();
                if (userInfoPanel != null)
                {
                    if (hasSession) userInfoPanel.ShowImmediate();
                    else userInfoPanel.HideImmediate();
                }
            }
        }

        #region Các Nút Bấm Main Menu (Button Actions)

        /// <summary>
        /// Gắn vào nút "BẮT ĐẦU" (Play Button).
        /// </summary>
        public void OnBatDauClicked()
        {
            PlayButtonClickSound();

            // Nếu cài đặt bắt buộc đăng nhập mà chưa đăng nhập PlayFab -> mở bảng đăng nhập
            if (requireLoginToPlay && (accountManager == null || !accountManager.IsLoggedIn))
            {
                Debug.Log("[MainMenuManager] Cần đăng nhập tài khoản PlayFab để bắt đầu chơi! Đang mở bảng đăng nhập...");
                OpenAccountPanel();
                return;
            }

            PlaySound(startGameSound);

            // Chuyển sang scene chơi game nếu có cấu hình
            if (!string.IsNullOrEmpty(playSceneName))
            {
                Debug.Log($"[MainMenuManager] Đang tải Scene trò chơi: {playSceneName}");
                SceneManager.LoadScene(playSceneName);
            }
            else
            {
                Debug.Log("[MainMenuManager] BẮT ĐẦU GAME! (Chưa cấu hình playSceneName)");
            }
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
        }

        /// <summary>
        /// Gắn vào nút "QUAY LẠI" (Back / Close Button) từ bất kỳ panel con nào để trở về MainMenu.
        /// </summary>
        public void BackToMainMenu()
        {
            PlayButtonClickSound();

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
        }

        /// <summary>
        /// Gắn vào nút "ĐĂNG XUẤT" (Logout Button).
        /// </summary>
        public void OnLogoutClicked()
        {
            PlayButtonClickSound();

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

            // Đăng nhập thành công: Hiện MainMenu (3 nút) và khung UserInfo trượt cùng lúc!
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

            // Đăng xuất: Ẩn 3 nút MainMenu và ẩn UserInfo, mở lại bảng Đăng nhập
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

        private void PlayButtonClickSound()
        {
            PlaySound(buttonClickSound);
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        #endregion
    }
}
