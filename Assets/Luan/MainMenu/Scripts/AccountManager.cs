using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

namespace Luan.MainMenu
{
    #region PlayFab Data Models

    [Serializable]
    public class PlayFabUserData
    {
        public string playFabId;
        public string sessionTicket;
        public string username;
        public string displayName;
        public int highScore;
    }

    [Serializable]
    internal class PlayFabRegisterRequest
    {
        public string TitleId;
        public string Username;
        public string Email;
        public string Password;
        public bool RequireBothUsernameAndEmail;
        public string DisplayName;
    }

    [Serializable]
    internal class PlayFabLoginRequest
    {
        public string TitleId;
        public string Username;
        public string Password;
    }

    [Serializable]
    internal class PlayFabUpdateUserTitleDisplayNameRequest
    {
        public string DisplayName;
    }

    [Serializable]
    internal class PlayFabResponseEnvelope
    {
        public int code;
        public string status;
        public string error;
        public int errorCode;
        public string errorMessage;
    }

    [Serializable]
    internal class PlayFabRegisterResponse
    {
        public int code;
        public string status;
        public PlayFabRegisterResult data;
        public string error;
        public int errorCode;
        public string errorMessage;
    }

    [Serializable]
    internal class PlayFabRegisterResult
    {
        public string PlayFabId;
        public string SessionTicket;
        public string Username;
    }

    [Serializable]
    internal class PlayFabLoginResponse
    {
        public int code;
        public string status;
        public PlayFabLoginResult data;
        public string error;
        public int errorCode;
        public string errorMessage;
    }

    [Serializable]
    internal class PlayFabLoginResult
    {
        public string PlayFabId;
        public string SessionTicket;
    }

    [Serializable]
    internal class PlayFabTitleInfo
    {
        public string DisplayName;
    }

    [Serializable]
    internal class PlayFabUserAccountInfo
    {
        public string PlayFabId;
        public string Username;
        public PlayFabTitleInfo TitleInfo;
    }

    [Serializable]
    internal class PlayFabGetAccountInfoResult
    {
        public PlayFabUserAccountInfo AccountInfo;
    }

    [Serializable]
    internal class PlayFabGetAccountInfoResponse
    {
        public int code;
        public string status;
        public PlayFabGetAccountInfoResult data;
        public string errorMessage;
    }

    #endregion

    /// <summary>
    /// Script quản lý Đăng ký & Đăng nhập tài khoản bằng Microsoft PlayFab Cloud.
    /// Thiết kế tinh gọn: Không cần Email, chỉ cần Tài khoản + Mật khẩu + Xác nhận mật khẩu.
    /// Hỗ trợ tùy chọn thêm Tên hiển thị (nếu để trống tự động lấy tên tài khoản).
    /// Hỗ trợ animation chuyển đổi mượt mà giữa Panel Đăng nhập và Panel Đăng ký.
    /// Tương thích cả TextMeshPro (TMP_InputField) và Canvas UI thông thường (InputField).
    /// </summary>
    [AddComponentMenu("UI/Luan MainMenu/Account Manager (PlayFab)")]
    public class AccountManager : MonoBehaviour
    {
        public static AccountManager Instance { get; private set; }

        [Header("--- Cấu Hình PlayFab (Title ID) ---")]
        [Tooltip("Title ID của dự án bạn trên Microsoft PlayFab Game Manager (Ví dụ: 144, ABCD1...)")]
        [SerializeField] private string playFabTitleId = "YOUR_TITLE_ID";

        [Header("--- UI Panel Animators ---")]
        [Tooltip("Panel Đăng nhập (gắn UIPanelAnimator)")]
        [SerializeField] private UIPanelAnimator loginPanel;

        [Tooltip("Panel Đăng ký (gắn UIPanelAnimator)")]
        [SerializeField] private UIPanelAnimator registerPanel;

        [Tooltip("Panel khung viền bao bọc cả popup nếu có (tùy chọn)")]
        [SerializeField] private UIPanelAnimator accountContainerPanel;

        [Header("--- Ô Nhập Đăng Nhập (Login: Chỉ cần Tài khoản & Mật khẩu) ---")]
        [Tooltip("Tên tài khoản đăng nhập (TMP)")]
        [SerializeField] private TMP_InputField loginUsernameTMP;
        [Tooltip("Tên tài khoản đăng nhập (Legacy)")]
        [SerializeField] private InputField loginUsernameLegacy;

        [Tooltip("Mật khẩu đăng nhập (TMP)")]
        [SerializeField] private TMP_InputField loginPasswordTMP;
        [Tooltip("Mật khẩu đăng nhập (Legacy)")]
        [SerializeField] private InputField loginPasswordLegacy;

        [Tooltip("Ô tick Ghi nhớ mật khẩu / Đăng nhập (Toggle)")]
        [SerializeField] private Toggle rememberPasswordToggle;

        [Header("--- Ô Nhập Đăng Ký (Register: Tài khoản, Tên tùy chọn, Mật khẩu) ---")]
        [Tooltip("Tên tài khoản đăng ký (TMP)")]
        [SerializeField] private TMP_InputField regUsernameTMP;
        [Tooltip("Tên tài khoản đăng ký (Legacy)")]
        [SerializeField] private InputField regUsernameLegacy;

        [Tooltip("Tùy chọn: Tên hiển thị/nhân vật (TMP). Nếu để trống sẽ lấy luôn Tên tài khoản")]
        [SerializeField] private TMP_InputField regDisplayNameTMP;
        [Tooltip("Tùy chọn: Tên hiển thị/nhân vật (Legacy). Nếu để trống sẽ lấy luôn Tên tài khoản")]
        [SerializeField] private InputField regDisplayNameLegacy;

        [Tooltip("Mật khẩu đăng ký (TMP)")]
        [SerializeField] private TMP_InputField regPasswordTMP;
        [Tooltip("Mật khẩu đăng ký (Legacy)")]
        [SerializeField] private InputField regPasswordLegacy;

        [Tooltip("Nhập lại mật khẩu đăng ký (TMP)")]
        [SerializeField] private TMP_InputField regConfirmPasswordTMP;
        [Tooltip("Nhập lại mật khẩu đăng ký (Legacy)")]
        [SerializeField] private InputField regConfirmPasswordLegacy;

        [Header("--- Thông Báo Phản Hồi (Feedback Text) ---")]
        [SerializeField] private TMP_Text loginFeedbackTMP;
        [SerializeField] private Text loginFeedbackLegacy;
        [SerializeField] private TMP_Text regFeedbackTMP;
        [SerializeField] private Text regFeedbackLegacy;

        [Header("--- Âm Thanh (Audio SFX - Tùy chọn) ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip errorSound;
        [SerializeField] private AudioClip switchSound;

        // Lưu trữ Session cục bộ
        private const string PrefsSessionTicketKey = "PLAYFAB_SESSION_TICKET";
        private const string PrefsPlayFabIdKey = "PLAYFAB_USER_ID";
        private const string PrefsUsernameKey = "PLAYFAB_CACHED_USERNAME";
        private const string PrefsDisplayNameKey = "PLAYFAB_CACHED_DISPLAYNAME";
        private const string PrefsRememberMeKey = "PLAYFAB_REMEMBER_ME";
        private const string PrefsCachedPasswordKey = "PLAYFAB_CACHED_PASSWORD";

        private PlayFabUserData _currentUser = null;
        private bool _isBusy = false;

        public PlayFabUserData CurrentUser => _currentUser;
        public bool IsLoggedIn => _currentUser != null && !string.IsNullOrEmpty(_currentUser.sessionTicket);
        public string PlayFabTitleId => playFabTitleId;

        // Sự kiện thông báo khi đăng nhập / đăng xuất
        public static event Action<PlayFabUserData> OnLoginSuccess;
        public static event Action<PlayFabUserData> OnRegisterSuccess;
        public static event Action OnLogout;

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
        }

        private void Start()
        {
            LoadRememberedCredentials();
            CheckCachedSession();
        }

        [ContextMenu("🗑️ XÓA DỮ LIỆU ĐĂNG NHẬP ĐÃ LƯU (Clear Saved Session)")]
        public void ContextClearSavedSession()
        {
            Logout();
            Debug.Log("[AccountManager] Đã xóa toàn bộ session và tài khoản đã lưu trên máy này! Lần sau vào game sẽ như mới.");
        }

        private void LoadRememberedCredentials()
        {
            bool remember = PlayerPrefs.GetInt(PrefsRememberMeKey, 0) == 1;

            if (rememberPasswordToggle != null)
            {
                rememberPasswordToggle.isOn = remember;
            }

            if (remember)
            {
                string savedUser = PlayerPrefs.GetString(PrefsUsernameKey, "");
                string savedPass = PlayerPrefs.GetString(PrefsCachedPasswordKey, "");

                SetInputText(loginUsernameTMP, loginUsernameLegacy, savedUser);
                SetInputText(loginPasswordTMP, loginPasswordLegacy, savedPass);
            }
        }

        #region Mở / Đóng Bảng & Chuyển Đổi Mượt Giữa Login Và Register

        /// <summary>
        /// Mở popup tài khoản (mặc định mở màn Đăng nhập).
        /// </summary>
        public void OpenAccountPanel(bool openLoginFirst = true)
        {
            if (accountContainerPanel != null)
            {
                accountContainerPanel.Show();
            }

            ClearFeedbacks();
            ClearInputs();

            if (openLoginFirst)
            {
                if (registerPanel != null) registerPanel.HideImmediate();
                if (loginPanel != null) loginPanel.Show();
            }
            else
            {
                if (loginPanel != null) loginPanel.HideImmediate();
                if (registerPanel != null) registerPanel.Show();
            }
        }

        // Alias tương thích
        public void OpenAuth(bool openLoginFirst = true) => OpenAccountPanel(openLoginFirst);

        /// <summary>
        /// Đóng toàn bộ popup tài khoản quay về MainMenu.
        /// </summary>
        public void CloseAccountPanel(Action onComplete = null)
        {
            if (accountContainerPanel != null)
            {
                accountContainerPanel.Hide();
            }

            if (loginPanel != null && loginPanel.IsOpen)
            {
                loginPanel.Hide(onComplete);
            }
            else if (registerPanel != null && registerPanel.IsOpen)
            {
                registerPanel.Hide(onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }

            ClearFeedbacks();
            ClearInputs();
        }

        // Alias tương thích
        public void CloseAuth(Action onComplete = null) => CloseAccountPanel(onComplete);

        /// <summary>
        /// Chuyển mượt mà từ màn Đăng Nhập sang màn Đăng Ký.
        /// </summary>
        public void SwitchToRegister()
        {
            if (_isBusy)
            {
                Debug.LogWarning("[AccountManager] Đang bận xử lý request PlayFab, không thể chuyển tab!");
                return;
            }

            PlaySound(switchSound);
            ClearFeedbacks();
            ClearInputs();

            Debug.Log($"[AccountManager] SwitchToRegister: loginPanel={loginPanel}, registerPanel={registerPanel}");

            if (loginPanel != null && registerPanel != null)
            {
                loginPanel.SwitchTo(registerPanel);
            }
            else if (registerPanel != null)
            {
                registerPanel.Show();
            }
            else
            {
                Debug.LogError("[AccountManager] registerPanel đang là NULL trong Inspector của AccountManager!");
            }
        }

        /// <summary>
        /// Chuyển mượt mà từ màn Đăng Ký sang màn Đăng Nhập.
        /// </summary>
        public void SwitchToLogin()
        {
            if (_isBusy)
            {
                Debug.LogWarning("[AccountManager] Đang bận xử lý request PlayFab, không thể chuyển tab!");
                return;
            }

            PlaySound(switchSound);
            ClearFeedbacks();
            ClearInputs();

            Debug.Log($"[AccountManager] SwitchToLogin: loginPanel={loginPanel}, registerPanel={registerPanel}");

            if (registerPanel != null && loginPanel != null)
            {
                registerPanel.SwitchTo(loginPanel);
            }
            else if (loginPanel != null)
            {
                loginPanel.Show();
            }
            else
            {
                Debug.LogError("[AccountManager] loginPanel đang là NULL trong Inspector của AccountManager!");
            }
        }

        #endregion

        #region Đăng Nhập (Login: Chỉ cần Username và Password)

        /// <summary>
        /// Gọi khi người chơi bấm nút "ĐĂNG NHẬP".
        /// </summary>
        public void OnLoginButtonClicked()
        {
            if (_isBusy) return;

            string username = GetInputText(loginUsernameTMP, loginUsernameLegacy).Trim();
            string password = GetInputText(loginPasswordTMP, loginPasswordLegacy);

            if (string.IsNullOrEmpty(username))
            {
                ShowLoginFeedback("Vui lòng nhập tên tài khoản!", Color.red);
                PlaySound(errorSound);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowLoginFeedback("Vui lòng nhập mật khẩu!", Color.red);
                PlaySound(errorSound);
                return;
            }

            if (string.IsNullOrEmpty(playFabTitleId) || playFabTitleId.Equals("YOUR_TITLE_ID", StringComparison.OrdinalIgnoreCase))
            {
                ShowLoginFeedback("Lỗi: Chưa cấu hình PlayFab Title ID trong Inspector!", Color.yellow);
                Debug.LogError("[PlayFab] Vui lòng nhập Title ID của bạn vào trường playFabTitleId trên AccountManager!");
                return;
            }

            ShowLoginFeedback("Đang đăng nhập PlayFab...", Color.cyan);
            StartCoroutine(LoginPlayFabRoutine(username, password));
        }

        private IEnumerator LoginPlayFabRoutine(string username, string password)
        {
            _isBusy = true;
            string url = $"https://{playFabTitleId}.playfabapi.com/Client/LoginWithPlayFab";

            PlayFabLoginRequest requestData = new PlayFabLoginRequest
            {
                TitleId = playFabTitleId,
                Username = username,
                Password = password
            };

            string jsonBody = JsonUtility.ToJson(requestData);

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                _isBusy = false;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    PlayFabLoginResponse res = JsonUtility.FromJson<PlayFabLoginResponse>(req.downloadHandler.text);
                    if (res != null && res.data != null)
                    {
                        yield return FetchAccountInfoRoutine(res.data.SessionTicket, res.data.PlayFabId, username, password);
                    }
                    else
                    {
                        ShowLoginFeedback("Phản hồi không hợp lệ từ máy chủ!", Color.red);
                        PlaySound(errorSound);
                    }
                }
                else
                {
                    HandlePlayFabError(req.downloadHandler.text, req.error, isLogin: true);
                }
            }
        }

        private IEnumerator FetchAccountInfoRoutine(string sessionTicket, string playFabId, string username, string password)
        {
            string url = $"https://{playFabTitleId}.playfabapi.com/Client/GetAccountInfo";
            string realDisplayName = username;

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                string reqJson = $"{{\"PlayFabId\":\"{playFabId}\"}}";
                byte[] bodyRaw = Encoding.UTF8.GetBytes(reqJson);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("X-Authorization", sessionTicket);

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        PlayFabGetAccountInfoResponse accRes = JsonUtility.FromJson<PlayFabGetAccountInfoResponse>(req.downloadHandler.text);
                        if (accRes != null && accRes.data != null && accRes.data.AccountInfo != null)
                        {
                            if (accRes.data.AccountInfo.TitleInfo != null && !string.IsNullOrEmpty(accRes.data.AccountInfo.TitleInfo.DisplayName))
                            {
                                realDisplayName = accRes.data.AccountInfo.TitleInfo.DisplayName;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[AccountManager] Không thể parse DisplayName từ PlayFab: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[AccountManager] GetAccountInfo thất bại: {req.downloadHandler.text}");
                }
            }

            // Fallback lưu trữ cục bộ theo username nếu PlayFab chưa kịp cập nhật hoặc rỗng
            string localCachedDisplayName = PlayerPrefs.GetString(PrefsDisplayNameKey + "_" + username, "");
            if ((string.IsNullOrEmpty(realDisplayName) || realDisplayName == username) && !string.IsNullOrEmpty(localCachedDisplayName))
            {
                realDisplayName = localCachedDisplayName;
                // Đồng bộ ngược lại lên server PlayFab nếu server bị thiếu
                StartCoroutine(UpdateDisplayNameRoutine(sessionTicket, realDisplayName));
            }
            else if (!string.IsNullOrEmpty(realDisplayName))
            {
                PlayerPrefs.SetString(PrefsDisplayNameKey + "_" + username, realDisplayName);
            }

            _currentUser = new PlayFabUserData
            {
                playFabId = playFabId,
                sessionTicket = sessionTicket,
                username = username,
                displayName = realDisplayName
            };

            // Lưu session và thông tin nếu người chơi tick "Nhớ mật khẩu"
            bool remember = rememberPasswordToggle == null || rememberPasswordToggle.isOn;
            if (remember)
            {
                PlayerPrefs.SetInt(PrefsRememberMeKey, 1);
                PlayerPrefs.SetString(PrefsCachedPasswordKey, password);
                PlayerPrefs.SetString(PrefsPlayFabIdKey, playFabId);
                PlayerPrefs.SetString(PrefsSessionTicketKey, sessionTicket);
                PlayerPrefs.SetString(PrefsUsernameKey, username);
                PlayerPrefs.SetString(PrefsDisplayNameKey, realDisplayName);
            }
            else
            {
                PlayerPrefs.SetInt(PrefsRememberMeKey, 0);
                PlayerPrefs.DeleteKey(PrefsCachedPasswordKey);
                PlayerPrefs.DeleteKey(PrefsSessionTicketKey);
                PlayerPrefs.DeleteKey(PrefsPlayFabIdKey);
            }
            PlayerPrefs.Save();

            ShowLoginFeedback("Đăng nhập thành công!", Color.green);
            PlaySound(successSound);

            // Đợi 0.6 giây để người chơi thấy thông báo, sau đó chạy animation ẩn panel và báo cho MainMenu hiện 3 nút lên
            StartCoroutine(DelayedCloseAndNotifySuccess(0.6f));
        }

        private IEnumerator DelayedCloseAndNotifySuccess(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            CloseAccountPanel(() =>
            {
                OnLoginSuccess?.Invoke(_currentUser);
            });
        }

        #endregion

        #region Đăng Ký (Register: Tài khoản, Tên tùy chọn, Mật khẩu, Xác nhận mật khẩu)

        /// <summary>
        /// Gọi khi người chơi bấm nút "ĐĂNG KÝ".
        /// </summary>
        public void OnRegisterButtonClicked()
        {
            if (_isBusy) return;

            string username = GetInputText(regUsernameTMP, regUsernameLegacy).Trim();
            string displayName = GetInputText(regDisplayNameTMP, regDisplayNameLegacy).Trim();
            string password = GetInputText(regPasswordTMP, regPasswordLegacy);
            string confirmPass = GetInputText(regConfirmPasswordTMP, regConfirmPasswordLegacy);

            if (string.IsNullOrEmpty(username))
            {
                ShowRegisterFeedback("Vui lòng nhập tên tài khoản!", Color.red);
                PlaySound(errorSound);
                return;
            }

            if (username.Length < 3 || username.Length > 20)
            {
                ShowRegisterFeedback("Tên tài khoản phải từ 3 đến 20 ký tự!", Color.red);
                PlaySound(errorSound);
                return;
            }

            // Nếu người chơi không nhập tên hiển thị riêng, lấy luôn Tên tài khoản làm tên hiển thị
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = username;
            }
            else if (displayName.Length < 3 || displayName.Length > 25)
            {
                ShowRegisterFeedback("Tên hiển thị phải từ 3 đến 25 ký tự!", Color.red);
                PlaySound(errorSound);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowRegisterFeedback("Vui lòng nhập mật khẩu!", Color.red);
                PlaySound(errorSound);
                return;
            }

            if (password.Length < 6 || password.Length > 100)
            {
                ShowRegisterFeedback("Mật khẩu phải từ 6 ký tự trở lên!", Color.red);
                PlaySound(errorSound);
                return;
            }

            if (password != confirmPass)
            {
                ShowRegisterFeedback("Mật khẩu xác nhận không khớp!", Color.red);
                PlaySound(errorSound);
                return;
            }

            if (string.IsNullOrEmpty(playFabTitleId) || playFabTitleId.Equals("YOUR_TITLE_ID", StringComparison.OrdinalIgnoreCase))
            {
                ShowRegisterFeedback("Lỗi: Chưa cấu hình PlayFab Title ID trong Inspector!", Color.yellow);
                Debug.LogError("[PlayFab] Vui lòng nhập Title ID của bạn vào trường playFabTitleId trên AccountManager!");
                return;
            }

            ShowRegisterFeedback("Đang tạo tài khoản...", Color.cyan);
            StartCoroutine(RegisterPlayFabRoutine(username, displayName, password));
        }

        private IEnumerator RegisterPlayFabRoutine(string username, string displayName, string password)
        {
            _isBusy = true;
            string url = $"https://{playFabTitleId}.playfabapi.com/Client/RegisterPlayFabUser";

            // Tự động tạo email định danh ngầm để PlayFab không bao giờ từ chối dù Title Settings có bật yêu cầu email hay không
            string hiddenEmail = $"{username.ToLower()}@catchthebeat.local";

            PlayFabRegisterRequest requestData = new PlayFabRegisterRequest
            {
                TitleId = playFabTitleId,
                Username = username,
                Email = hiddenEmail,
                Password = password,
                RequireBothUsernameAndEmail = false,
                DisplayName = displayName
            };

            string jsonBody = JsonUtility.ToJson(requestData);

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                _isBusy = false;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    PlayFabRegisterResponse res = JsonUtility.FromJson<PlayFabRegisterResponse>(req.downloadHandler.text);
                    if (res != null && res.data != null)
                    {
                        PlayFabUserData newUser = new PlayFabUserData
                        {
                            playFabId = res.data.PlayFabId,
                            sessionTicket = res.data.SessionTicket,
                            username = username,
                            displayName = displayName
                        };

                        // Lưu tên hiển thị
                        PlayerPrefs.SetString(PrefsDisplayNameKey, displayName);
                        PlayerPrefs.SetString(PrefsDisplayNameKey + "_" + username, displayName);
                        PlayerPrefs.Save();

                        // Cập nhật Display Name lên PlayFab server
                        StartCoroutine(UpdateDisplayNameRoutine(res.data.SessionTicket, displayName));

                        ShowRegisterFeedback("Đăng ký thành công! Đang chuyển sang Đăng nhập...", Color.green);
                        PlaySound(successSound);

                        OnRegisterSuccess?.Invoke(newUser);

                        // Tự động điền username sang ô Login và chuyển mượt sang Login
                        SetInputText(loginUsernameTMP, loginUsernameLegacy, username);
                        ClearInputText(loginPasswordTMP, loginPasswordLegacy);

                        Invoke(nameof(SwitchToLogin), 1.2f);
                    }
                    else
                    {
                        ShowRegisterFeedback("Phản hồi không hợp lệ từ máy chủ!", Color.red);
                        PlaySound(errorSound);
                    }
                }
                else
                {
                    HandlePlayFabError(req.downloadHandler.text, req.error, isLogin: false);
                }
            }
        }

        private IEnumerator UpdateDisplayNameRoutine(string sessionTicket, string displayName)
        {
            if (string.IsNullOrEmpty(sessionTicket) || string.IsNullOrEmpty(displayName)) yield break;

            string url = $"https://{playFabTitleId}.playfabapi.com/Client/UpdateUserTitleDisplayName";
            PlayFabUpdateUserTitleDisplayNameRequest reqObj = new PlayFabUpdateUserTitleDisplayNameRequest
            {
                DisplayName = displayName
            };

            string jsonBody = JsonUtility.ToJson(reqObj);

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("X-Authorization", sessionTicket);

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[AccountManager] Cập nhật DisplayName '{displayName}' lên PlayFab thành công!");
                }
                else
                {
                    Debug.LogWarning($"[AccountManager] Cập nhật DisplayName thất bại: {req.downloadHandler.text}");
                }
            }
        }

        #endregion

        #region Xử Lý Lỗi PlayFab (Error Handling)

        private void HandlePlayFabError(string responseText, string networkError, bool isLogin)
        {
            string friendlyMessage = "Lỗi kết nối máy chủ!";

            if (!string.IsNullOrEmpty(responseText))
            {
                try
                {
                    PlayFabResponseEnvelope errorEnvelope = JsonUtility.FromJson<PlayFabResponseEnvelope>(responseText);
                    if (errorEnvelope != null)
                    {
                        switch (errorEnvelope.error)
                        {
                            case "AccountNotFound":
                                friendlyMessage = "Tài khoản không tồn tại!";
                                break;
                            case "InvalidPassword":
                                friendlyMessage = "Mật khẩu không chính xác!";
                                break;
                            case "NameNotAvailable":
                                friendlyMessage = "Tên tài khoản này đã có người đăng ký!";
                                break;
                            case "InvalidUsername":
                                friendlyMessage = "Tên tài khoản không hợp lệ (từ 3-20 ký tự, không dấu cách)!";
                                break;
                            case "AccountBanned":
                                friendlyMessage = "Tài khoản đã bị tạm khóa!";
                                break;
                            default:
                                friendlyMessage = !string.IsNullOrEmpty(errorEnvelope.errorMessage)
                                    ? errorEnvelope.errorMessage
                                    : "Đã xảy ra lỗi (" + errorEnvelope.error + ")";
                                break;
                        }
                    }
                }
                catch
                {
                    friendlyMessage = !string.IsNullOrEmpty(networkError) ? networkError : "Lỗi mạng!";
                }
            }
            else
            {
                friendlyMessage = networkError;
            }

            PlaySound(errorSound);

            if (isLogin)
            {
                ShowLoginFeedback(friendlyMessage, Color.red);
            }
            else
            {
                ShowRegisterFeedback(friendlyMessage, Color.red);
            }
        }

        #endregion

        #region Đăng Xuất (Logout) & Ghi Nhớ Session

        public void Logout()
        {
            _currentUser = null;
            PlayerPrefs.DeleteKey(PrefsSessionTicketKey);
            PlayerPrefs.DeleteKey(PrefsPlayFabIdKey);
            PlayerPrefs.DeleteKey(PrefsUsernameKey);
            PlayerPrefs.DeleteKey(PrefsDisplayNameKey);
            PlayerPrefs.DeleteKey(PrefsCachedPasswordKey);
            PlayerPrefs.SetInt(PrefsRememberMeKey, 0);
            if (rememberPasswordToggle != null) rememberPasswordToggle.isOn = false;
            PlayerPrefs.Save();

            ClearInputs();
            ClearFeedbacks();

            OnLogout?.Invoke();
        }

        private void CheckCachedSession()
        {
            // Chỉ tự động khôi phục phiên đăng nhập nếu người chơi có tick "Ghi nhớ"
            bool remember = PlayerPrefs.GetInt(PrefsRememberMeKey, 0) == 1;
            if (!remember) return;

            if (PlayerPrefs.HasKey(PrefsSessionTicketKey) && PlayerPrefs.HasKey(PrefsUsernameKey))
            {
                string ticket = PlayerPrefs.GetString(PrefsSessionTicketKey);
                string playFabId = PlayerPrefs.GetString(PrefsPlayFabIdKey, "");
                string username = PlayerPrefs.GetString(PrefsUsernameKey, "");
                string displayName = PlayerPrefs.GetString(PrefsDisplayNameKey, username);

                if (!string.IsNullOrEmpty(ticket) && !string.IsNullOrEmpty(username))
                {
                    _currentUser = new PlayFabUserData
                    {
                        playFabId = playFabId,
                        sessionTicket = ticket,
                        username = username,
                        displayName = string.IsNullOrEmpty(displayName) ? username : displayName
                    };

                    OnLoginSuccess?.Invoke(_currentUser);
                }
            }
        }

        #endregion

        #region UI Helpers

        private string GetInputText(TMP_InputField tmp, InputField legacy)
        {
            if (tmp != null) return tmp.text;
            if (legacy != null) return legacy.text;
            return string.Empty;
        }

        private void SetInputText(TMP_InputField tmp, InputField legacy, string value)
        {
            if (tmp != null) tmp.text = value;
            if (legacy != null) legacy.text = value;
        }

        private void ClearInputText(TMP_InputField tmp, InputField legacy)
        {
            if (tmp != null) tmp.text = string.Empty;
            if (legacy != null) legacy.text = string.Empty;
        }

        public void ClearInputs()
        {
            ClearInputText(loginPasswordTMP, loginPasswordLegacy);
            ClearInputText(regUsernameTMP, regUsernameLegacy);
            ClearInputText(regDisplayNameTMP, regDisplayNameLegacy);
            ClearInputText(regPasswordTMP, regPasswordLegacy);
            ClearInputText(regConfirmPasswordTMP, regConfirmPasswordLegacy);
        }

        public void ShowLoginFeedback(string message, Color color)
        {
            if (loginFeedbackTMP != null)
            {
                loginFeedbackTMP.text = message;
                loginFeedbackTMP.color = color;
            }
            if (loginFeedbackLegacy != null)
            {
                loginFeedbackLegacy.text = message;
                loginFeedbackLegacy.color = color;
            }
        }

        public void ShowRegisterFeedback(string message, Color color)
        {
            if (regFeedbackTMP != null)
            {
                regFeedbackTMP.text = message;
                regFeedbackTMP.color = color;
            }
            if (regFeedbackLegacy != null)
            {
                regFeedbackLegacy.text = message;
                regFeedbackLegacy.color = color;
            }
        }

        public void ClearFeedbacks()
        {
            if (loginFeedbackTMP != null) loginFeedbackTMP.text = string.Empty;
            if (loginFeedbackLegacy != null) loginFeedbackLegacy.text = string.Empty;
            if (regFeedbackTMP != null) regFeedbackTMP.text = string.Empty;
            if (regFeedbackLegacy != null) regFeedbackLegacy.text = string.Empty;
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
