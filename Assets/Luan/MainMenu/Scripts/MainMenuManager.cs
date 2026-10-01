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

        [Tooltip("Panel Cài Đặt (Tùy chọn)")]
        [SerializeField] private UIPanelAnimator settingsPanel;

        [Tooltip("Panel Thành Tích / Bảng xếp hạng (Tùy chọn)")]
        [SerializeField] private UIPanelAnimator leaderboardPanel;

        [Tooltip("Tham chiếu trực tiếp tới AccountManager (nếu để trống script sẽ tự tìm)")]
        [SerializeField] private AccountManager accountManager;

        [Header("--- Hiển Thị Thông Tin Người Chơi (User Info) ---")]
        [Tooltip("Text chào mừng người chơi (TextMeshPro)")]
        [SerializeField] private TMP_Text userGreetingTMP;
        [Tooltip("Text chào mừng người chơi (Legacy UI Text)")]
        [SerializeField] private Text userGreetingLegacy;

        [Tooltip("Nút mở Đăng nhập / Tài khoản")]
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

            // Nếu người chơi chưa đăng nhập: Ẩn 3 nút Menu chính và hiện bảng Đăng nhập trước
            if (showLoginOnStart && !hasSession)
            {
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.HideImmediate();
                }

                if (accountManager != null)
                {
                    accountManager.OpenAccountPanel(true);
                }
            }
            else
            {
                // Nếu đã đăng nhập sẵn rồi: Hiện 3 nút menu chính
                if (mainMenuPanel != null)
                {
                    mainMenuPanel.ShowImmediate();
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

            if (settingsPanel != null)
            {
                settingsPanel.Show();
            }
            else
            {
                Debug.Log("[MainMenuManager] Bấm Cài Đặt (Chưa gán settingsPanel trong Inspector)");
            }
        }

        /// <summary>
        /// Gắn vào nút "THÀNH TÍCH" (Leaderboard / Achievements Button).
        /// </summary>
        public void OnThanhTichClicked()
        {
            PlayButtonClickSound();

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

            if (mainMenuPanel != null && !mainMenuPanel.IsOpen)
            {
                mainMenuPanel.Show();
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

            // Đăng nhập thành công: Hiện MainMenu (3 nút) lên mượt mà bằng Animation!
            if (mainMenuPanel != null && !mainMenuPanel.IsOpen)
            {
                mainMenuPanel.Show();
            }
        }

        private void HandleRegisterSuccess(PlayFabUserData account)
        {
            UpdateUserDisplay();
        }

        private void HandleLogout()
        {
            UpdateUserDisplay();

            // Đăng xuất: Ẩn 3 nút và mở lại bảng Đăng nhập
            if (mainMenuPanel != null && mainMenuPanel.IsOpen)
            {
                mainMenuPanel.Hide();
            }

            if (accountManager != null)
            {
                accountManager.OpenAccountPanel(true);
            }
        }

        public void UpdateUserDisplay()
        {
            bool loggedIn = accountManager != null && accountManager.IsLoggedIn;
            string displayName = "Khách";

            if (loggedIn && accountManager.CurrentUser != null)
            {
                displayName = !string.IsNullOrEmpty(accountManager.CurrentUser.displayName)
                    ? accountManager.CurrentUser.displayName
                    : accountManager.CurrentUser.username;
            }

            string greetingText = loggedIn ? $"Xin chào, {displayName}!" : "Chưa đăng nhập";

            if (userGreetingTMP != null) userGreetingTMP.text = greetingText;
            if (userGreetingLegacy != null) userGreetingLegacy.text = greetingText;

            string btnText = loggedIn ? displayName : "Đăng Nhập";
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
