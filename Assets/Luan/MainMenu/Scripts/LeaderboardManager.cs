using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

namespace Luan.MainMenu
{
    #region PlayFab Leaderboard Data Models

    [Serializable]
    public class PlayFabLeaderboardEntry
    {
        public string PlayFabId;
        public string DisplayName;
        public int StatValue;
        public int Position;
    }

    [Serializable]
    internal class PlayFabGetLeaderboardResult
    {
        public PlayFabLeaderboardEntry[] Leaderboard;
        public int Version;
    }

    [Serializable]
    internal class PlayFabGetLeaderboardResponse
    {
        public int code;
        public string status;
        public PlayFabGetLeaderboardResult data;
        public string errorMessage;
    }

    [Serializable]
    internal class PlayFabGetLeaderboardRequest
    {
        public string StatisticName;
        public int StartPosition;
        public int MaxResultsCount;
    }

    [Serializable]
    internal class PlayFabGetLeaderboardAroundPlayerRequest
    {
        public string StatisticName;
        public int MaxResultsCount;
    }

    [Serializable]
    internal class PlayFabStatisticUpdate
    {
        public string StatisticName;
        public int Value;
    }

    [Serializable]
    internal class PlayFabUpdatePlayerStatisticsRequest
    {
        public PlayFabStatisticUpdate[] Statistics;
    }

    [Serializable]
    internal class PlayFabUpdatePlayerStatisticsResponse
    {
        public int code;
        public string status;
        public string errorMessage;
    }

    #endregion

    /// <summary>
    /// Đại diện cho 1 hàng hiển thị trên bảng xếp hạng (từ Top 1 đến Top 10).
    /// </summary>
    [Serializable]
    public class LeaderboardRowUI
    {
        [Tooltip("Text TMP hiển thị Tên người chơi")]
        public TMP_Text displayNameTMP;

        [Tooltip("Text TMP hiển thị Điểm số")]
        public TMP_Text scoreTMP;

        [Tooltip("Root GameObject của hàng (Tùy chọn: ẩn nếu không có người chơi)")]
        public GameObject rowRoot;
    }

    /// <summary>
    /// Script điều khiển toàn bộ hệ thống Bảng Xếp Hạng / Thành Tích (Leaderboard).
    /// Tích hợp trực tiếp Microsoft PlayFab (Leaderboard API):
    /// - Lấy Top 10 người chơi có điểm cao nhất.
    /// - Lấy vị trí thứ hạng, tên hiển thị và điểm số hiện tại của người chơi đang đăng nhập.
    /// - Hỗ trợ nút Đóng trở lại Main Menu và tích hợp mượt mà với UIPanelAnimator.
    /// </summary>
    [AddComponentMenu("UI/Luan MainMenu/Leaderboard Manager (PlayFab)")]
    public class LeaderboardManager : MonoBehaviour
    {
        public static LeaderboardManager Instance { get; private set; }

        [Header("--- Panel Hoạt Họa (UIPanelAnimator) ---")]
        [Tooltip("Kéo UIPanelAnimator của Leaderboard Panel vào đây để mở/đóng mượt mà")]
        [SerializeField] private UIPanelAnimator leaderboardPanelAnimator;

        [Header("--- Cấu Hình PlayFab Leaderboard ---")]
        [Tooltip("Title ID của PlayFab (để trống script sẽ tự lấy từ AccountManager)")]
        [SerializeField] private string playFabTitleId = "";

        [Tooltip("Tên Statistic trên PlayFab GameManager (Ví dụ: HighScore, ĐiểmCao, Score...)")]
        [SerializeField] private string statisticName = "HighScore";

        [Header("--- Danh Sách 10 Hàng Xếp Hạng (Top 1 - 10) ---")]
        [Tooltip("Gán lần lượt các hàng từ Top 1 đến Top 10 (mỗi hàng gồm DisplayName TMP và Score TMP)")]
        [SerializeField] private LeaderboardRowUI[] top10Rows = new LeaderboardRowUI[10];

        [Header("--- Hạng Hiện Tại Của Người Chơi (Current Player Row) ---")]
        [Tooltip("Text TMP hiển thị thứ hạng của người chơi hiện tại (ví dụ: #1, #15, ---)")]
        [SerializeField] private TMP_Text currentRankTMP;

        [Tooltip("Text TMP hiển thị Tên người chơi hiện tại")]
        [SerializeField] private TMP_Text currentDisplayNameTMP;

        [Tooltip("Text TMP hiển thị Điểm số của người chơi hiện tại")]
        [SerializeField] private TMP_Text currentScoreTMP;

        [Header("--- Các Nút Bấm & Trạng Thái (Controls) ---")]
        [Tooltip("Nút ĐÓNG để trở về MainMenu")]
        [SerializeField] private Button closeButton;

        [Tooltip("Nút Làm Mới bảng xếp hạng (Tùy chọn)")]
        [SerializeField] private Button refreshButton;

        [Tooltip("GameObject hiển thị hiệu ứng xoay đang tải (Tùy chọn)")]
        [SerializeField] private GameObject loadingIndicator;

        [Tooltip("Text hiển thị thông báo trạng thái (Tùy chọn, ví dụ: 'Đang tải...', 'Lỗi kết nối')")]
        [SerializeField] private TMP_Text statusTMP;

        [Header("--- Tùy Chỉnh Định Dạng (Formatting) ---")]
        [Tooltip("Tiền tố thứ hạng (ví dụ '#' -> '#1', hoặc để trống -> '1')")]
        [SerializeField] private string rankPrefix = "#";

        [Tooltip("Rút gọn thứ hạng nếu quá lớn (ví dụ: rank 1000 -> 1k, 10000 -> 10k, 100000 -> 100k)")]
        [SerializeField] private bool useCompactRankFormat = true;

        [Tooltip("Bỏ dấu '#' khi thứ hạng từ 1,000 trở lên để tiết kiệm diện tích (ví dụ: '10k' thay vì '#10k')")]
        [SerializeField] private bool dropRankPrefixOnCompact = true;

        [Tooltip("Giới hạn hiển thị thứ hạng dạng '999+' hoặc '9999+' nếu vượt quá (mặc định: 999 -> '999+')")]
        [SerializeField] private int maxRankCap = 999;

        [Tooltip("Rút gọn số điểm thành đơn vị k, M (ví dụ: 1000 -> 1k, 100000 -> 100k, 1000000 -> 1M)")]
        [SerializeField] private bool useCompactScoreFormat = true;

        [Tooltip("Chữ cái viết thường cho k (1k thay vì 1K)")]
        [SerializeField] private bool useLowercaseK = true;

        [Tooltip("Định dạng số điểm có dấu phẩy phân cách hàng nghìn khi tắt rút gọn (ví dụ: 1,500 thay vì 1500)")]
        [SerializeField] private bool formatScoreWithCommas = true;

        [Tooltip("Ký tự hiển thị khi hàng chưa có người chơi")]
        [SerializeField] private string emptyPlaceholder = "-";

        [Tooltip("Độ dài tối đa của tên trước khi thêm '...' để không bị tràn khung")]
        [Range(4, 25)]
        [SerializeField] private int maxNameLength = 12;

        [Header("--- Tự Động Làm Mới Định Kỳ (Auto Refresh) ---")]
        [Tooltip("Có tự động cập nhật bảng xếp hạng trong lúc người chơi đang mở bảng không?")]
        [SerializeField] private bool autoRefresh = true;

        [Tooltip("Khoảng cách giữa mỗi lần quét cập nhật (giây, khuyến nghị từ 10s - 30s)")]
        [Range(5f, 60f)]
        [SerializeField] private float refreshInterval = 10f;

        [Header("--- Âm Thanh (Audio SFX) ---")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip buttonClickSound;

        private bool _isLoading = false;
        private Coroutine _autoRefreshCoroutine = null;

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

            if (leaderboardPanelAnimator == null)
            {
                leaderboardPanelAnimator = GetComponent<UIPanelAnimator>();
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseLeaderboard);
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(() => FetchLeaderboard());
            }
        }

        private void OnEnable()
        {
            // Tự động tải lại bảng xếp hạng mỗi khi mở panel Thành Tích
            FetchLeaderboard();

            if (autoRefresh)
            {
                if (_autoRefreshCoroutine != null) StopCoroutine(_autoRefreshCoroutine);
                _autoRefreshCoroutine = StartCoroutine(AutoRefreshRoutine());
            }
        }

        private void OnDisable()
        {
            if (_autoRefreshCoroutine != null)
            {
                StopCoroutine(_autoRefreshCoroutine);
                _autoRefreshCoroutine = null;
            }
        }

        private IEnumerator AutoRefreshRoutine()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(refreshInterval);
                if (gameObject.activeInHierarchy && !_isLoading)
                {
                    FetchLeaderboard(isSilentBackground: true);
                }
            }
        }

        private string GetEffectiveTitleId()
        {
            if (!string.IsNullOrEmpty(playFabTitleId) && playFabTitleId != "YOUR_TITLE_ID")
            {
                return playFabTitleId;
            }

            if (AccountManager.Instance != null && !string.IsNullOrEmpty(AccountManager.Instance.PlayFabTitleId))
            {
                return AccountManager.Instance.PlayFabTitleId;
            }

            return playFabTitleId;
        }

        private string GetSessionTicket()
        {
            if (AccountManager.Instance != null && AccountManager.Instance.CurrentUser != null)
            {
                return AccountManager.Instance.CurrentUser.sessionTicket;
            }

            return PlayerPrefs.GetString("PLAYFAB_SESSION_TICKET", "");
        }

        /// <summary>
        /// Tải dữ liệu Bảng Xếp Hạng Top 10 và Hạng của người chơi hiện tại từ PlayFab.
        /// </summary>
        public void FetchLeaderboard(bool isSilentBackground = false)
        {
            if (_isLoading) return;

            string titleId = GetEffectiveTitleId();
            string sessionTicket = GetSessionTicket();

            if (string.IsNullOrEmpty(titleId) || titleId == "YOUR_TITLE_ID")
            {
                if (!isSilentBackground) SetStatus("Chưa cấu hình PlayFab Title ID!");
                ClearLeaderboardUI();
                return;
            }

            if (string.IsNullOrEmpty(sessionTicket))
            {
                if (!isSilentBackground) SetStatus("Chưa đăng nhập! Vui lòng đăng nhập để xem thành tích.");
                ClearLeaderboardUI();
                UpdateCurrentPlayerDisplayUnauthenticated();
                return;
            }

            StartCoroutine(FetchLeaderboardRoutine(titleId, sessionTicket, isSilentBackground));
        }

        private IEnumerator FetchLeaderboardRoutine(string titleId, string sessionTicket, bool isSilentBackground)
        {
            _isLoading = true;
            if (!isSilentBackground)
            {
                SetLoadingState(true);
                SetStatus("Đang tải bảng xếp hạng...");
            }

            // 1. Tải Top 10 người chơi
            string urlTop10 = $"https://{titleId}.playfabapi.com/Client/GetLeaderboard";
            PlayFabGetLeaderboardRequest reqTop10Data = new PlayFabGetLeaderboardRequest
            {
                StatisticName = statisticName,
                StartPosition = 0,
                MaxResultsCount = 10
            };

            string jsonTop10 = JsonUtility.ToJson(reqTop10Data);
            PlayFabLeaderboardEntry[] top10Entries = null;

            using (UnityWebRequest req = new UnityWebRequest(urlTop10, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonTop10);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("X-Authorization", sessionTicket);

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        PlayFabGetLeaderboardResponse res = JsonUtility.FromJson<PlayFabGetLeaderboardResponse>(req.downloadHandler.text);
                        if (res != null && res.data != null && res.data.Leaderboard != null)
                        {
                            top10Entries = res.data.Leaderboard;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LeaderboardManager] Lỗi parse dữ liệu Top 10: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[LeaderboardManager] Tải Top 10 thất bại: {req.downloadHandler.text}");
                }
            }

            // Cập nhật giao diện Top 1 - 10
            PopulateTop10UI(top10Entries);

            // 2. Tải Hạng của chính người chơi hiện tại
            string urlAround = $"https://{titleId}.playfabapi.com/Client/GetLeaderboardAroundPlayer";
            PlayFabGetLeaderboardAroundPlayerRequest reqAroundData = new PlayFabGetLeaderboardAroundPlayerRequest
            {
                StatisticName = statisticName,
                MaxResultsCount = 1
            };

            string jsonAround = JsonUtility.ToJson(reqAroundData);
            PlayFabLeaderboardEntry playerEntry = null;

            using (UnityWebRequest req = new UnityWebRequest(urlAround, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonAround);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("X-Authorization", sessionTicket);

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        PlayFabGetLeaderboardResponse res = JsonUtility.FromJson<PlayFabGetLeaderboardResponse>(req.downloadHandler.text);
                        if (res != null && res.data != null && res.data.Leaderboard != null && res.data.Leaderboard.Length > 0)
                        {
                            playerEntry = res.data.Leaderboard[0];
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LeaderboardManager] Lỗi parse hạng người chơi: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[LeaderboardManager] Tải hạng người chơi thất bại: {req.downloadHandler.text}");
                }
            }

            // Cập nhật giao diện thanh xếp hạng hiện tại của người chơi
            PopulateCurrentPlayerUI(playerEntry);

            SetLoadingState(false);
            SetStatus("");
            _isLoading = false;
        }

        /// <summary>
        /// Điền dữ liệu vào danh sách Top 1 - 10 (chỉ lấy những người chơi có điểm > 0).
        /// </summary>
        private void PopulateTop10UI(PlayFabLeaderboardEntry[] entries)
        {
            List<PlayFabLeaderboardEntry> validEntries = new List<PlayFabLeaderboardEntry>();
            if (entries != null)
            {
                for (int j = 0; j < entries.Length; j++)
                {
                    if (entries[j] != null && entries[j].StatValue > 0)
                    {
                        validEntries.Add(entries[j]);
                    }
                }
            }

            int entryCount = validEntries.Count;

            for (int i = 0; i < top10Rows.Length; i++)
            {
                LeaderboardRowUI row = top10Rows[i];
                if (row == null) continue;

                if (i < entryCount)
                {
                    PlayFabLeaderboardEntry entry = validEntries[i];
                    string rawName = !string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : "Người chơi";
                    string displayName = FormatName(rawName);
                    string scoreText = FormatScore(entry.StatValue);

                    if (row.displayNameTMP != null)
                    {
                        row.displayNameTMP.textWrappingMode = TextWrappingModes.NoWrap;
                        row.displayNameTMP.overflowMode = TextOverflowModes.Ellipsis;
                        row.displayNameTMP.text = displayName;
                    }

                    if (row.scoreTMP != null)
                    {
                        row.scoreTMP.textWrappingMode = TextWrappingModes.NoWrap;
                        row.scoreTMP.text = scoreText;
                    }

                    if (row.rowRoot != null) row.rowRoot.SetActive(true);
                }
                else
                {
                    // Hàng chưa có người chơi hoặc điểm = 0
                    if (row.displayNameTMP != null) row.displayNameTMP.text = emptyPlaceholder;
                    if (row.scoreTMP != null) row.scoreTMP.text = emptyPlaceholder;
                }
            }
        }

        /// <summary>
        /// Điền dữ liệu vào thanh xếp hạng của người chơi hiện tại.
        /// </summary>
        private void PopulateCurrentPlayerUI(PlayFabLeaderboardEntry entry)
        {
            string myDisplayName = "";
            if (AccountManager.Instance != null && AccountManager.Instance.CurrentUser != null)
            {
                myDisplayName = !string.IsNullOrEmpty(AccountManager.Instance.CurrentUser.displayName)
                    ? AccountManager.Instance.CurrentUser.displayName
                    : AccountManager.Instance.CurrentUser.username;
            }

            // Chỉ hiển thị thứ hạng (Top 1, 2,...) khi người chơi thực sự có điểm (> 0)
            if (entry != null && entry.StatValue > 0)
            {
                int rank = entry.Position + 1; // PlayFab Position là 0-indexed
                string rankText = FormatRank(rank);
                string nameText = FormatName(!string.IsNullOrEmpty(entry.DisplayName) ? entry.DisplayName : myDisplayName);
                string scoreText = FormatScore(entry.StatValue);

                if (currentRankTMP != null)
                {
                    currentRankTMP.textWrappingMode = TextWrappingModes.NoWrap;
                    currentRankTMP.enableAutoSizing = true;
                    if (currentRankTMP.fontSizeMin < 8f) currentRankTMP.fontSizeMin = 8f;
                    currentRankTMP.text = rankText;
                }
                if (currentDisplayNameTMP != null)
                {
                    currentDisplayNameTMP.textWrappingMode = TextWrappingModes.NoWrap;
                    currentDisplayNameTMP.overflowMode = TextOverflowModes.Ellipsis;
                    currentDisplayNameTMP.text = nameText;
                }
                if (currentScoreTMP != null)
                {
                    currentScoreTMP.textWrappingMode = TextWrappingModes.NoWrap;
                    currentScoreTMP.text = scoreText;
                }
            }
            else
            {
                // Người chơi chưa có điểm nào (> 0) trên PlayFab -> Chưa xếp hạng!
                if (currentRankTMP != null)
                {
                    currentRankTMP.textWrappingMode = TextWrappingModes.NoWrap;
                    currentRankTMP.text = emptyPlaceholder; // "-"
                }
                if (currentDisplayNameTMP != null)
                {
                    currentDisplayNameTMP.textWrappingMode = TextWrappingModes.NoWrap;
                    currentDisplayNameTMP.overflowMode = TextOverflowModes.Ellipsis;
                    currentDisplayNameTMP.text = FormatName(myDisplayName);
                }
                if (currentScoreTMP != null)
                {
                    currentScoreTMP.textWrappingMode = TextWrappingModes.NoWrap;
                    currentScoreTMP.text = "0";
                }
            }
        }

        private void UpdateCurrentPlayerDisplayUnauthenticated()
        {
            if (currentRankTMP != null)
            {
                currentRankTMP.textWrappingMode = TextWrappingModes.NoWrap;
                currentRankTMP.text = emptyPlaceholder;
            }
            if (currentDisplayNameTMP != null)
            {
                currentDisplayNameTMP.textWrappingMode = TextWrappingModes.NoWrap;
                currentDisplayNameTMP.text = "Chưa đăng nhập";
            }
            if (currentScoreTMP != null)
            {
                currentScoreTMP.textWrappingMode = TextWrappingModes.NoWrap;
                currentScoreTMP.text = emptyPlaceholder;
            }
        }

        private void ClearLeaderboardUI()
        {
            for (int i = 0; i < top10Rows.Length; i++)
            {
                LeaderboardRowUI row = top10Rows[i];
                if (row == null) continue;

                if (row.displayNameTMP != null) row.displayNameTMP.text = emptyPlaceholder;
                if (row.scoreTMP != null) row.scoreTMP.text = emptyPlaceholder;
            }
        }

        /// <summary>
        /// Gửi cập nhật điểm số cao nhất lên PlayFab Leaderboard (gọi sau khi kết thúc ván chơi).
        /// </summary>
        public void SubmitScore(int score, Action<bool> onComplete = null)
        {
            string titleId = GetEffectiveTitleId();
            string sessionTicket = GetSessionTicket();

            if (string.IsNullOrEmpty(titleId) || string.IsNullOrEmpty(sessionTicket))
            {
                Debug.LogWarning("[LeaderboardManager] Không thể lưu điểm: Người chơi chưa đăng nhập PlayFab!");
                onComplete?.Invoke(false);
                return;
            }

            StartCoroutine(SubmitScoreRoutine(titleId, sessionTicket, score, onComplete));
        }

        private IEnumerator SubmitScoreRoutine(string titleId, string sessionTicket, int score, Action<bool> onComplete)
        {
            string url = $"https://{titleId}.playfabapi.com/Client/UpdatePlayerStatistics";
            PlayFabUpdatePlayerStatisticsRequest reqObj = new PlayFabUpdatePlayerStatisticsRequest
            {
                Statistics = new PlayFabStatisticUpdate[]
                {
                    new PlayFabStatisticUpdate
                    {
                        StatisticName = statisticName,
                        Value = score
                    }
                }
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
                    Debug.Log($"[LeaderboardManager] Lưu điểm {score} lên PlayFab thành công!");
                    onComplete?.Invoke(true);
                    // Tự động tải lại bảng nếu panel đang mở
                    if (gameObject.activeInHierarchy)
                    {
                        FetchLeaderboard();
                    }
                }
                else
                {
                    Debug.LogWarning($"[LeaderboardManager] Lưu điểm thất bại: {req.downloadHandler.text}");
                    onComplete?.Invoke(false);
                }
            }
        }

        #region Xử Lý Đóng Panel & Quay Lại MainMenu

        /// <summary>
        /// Gắn vào nút "ĐÓNG" trên bảng Thành Tích để quay lại Main Menu.
        /// </summary>
        public void CloseLeaderboard()
        {
            PlayButtonClickSound();

            if (leaderboardPanelAnimator != null)
            {
                leaderboardPanelAnimator.Hide();
            }
            else
            {
                gameObject.SetActive(false);
            }

            // Gọi MainMenuManager để hiện lại 3 nút menu chính và khung UserInfo
            if (MainMenuManager.Instance != null)
            {
                MainMenuManager.Instance.BackToMainMenu();
            }
        }

        #endregion

        #region Helpers & Formatters

        private string FormatName(string rawName)
        {
            if (string.IsNullOrEmpty(rawName)) return string.Empty;
            string trimmed = rawName.Trim();
            if (trimmed.Length > maxNameLength)
            {
                return trimmed.Substring(0, maxNameLength) + "...";
            }
            return trimmed;
        }

        private string FormatScore(long score)
        {
            if (score <= 0) return "0";

            if (useCompactScoreFormat)
            {
                string kSuffix = useLowercaseK ? "k" : "K";

                if (score >= 1_000_000_000)
                {
                    double b = score / 1_000_000_000.0;
                    return b.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "B";
                }
                if (score >= 1_000_000)
                {
                    double m = score / 1_000_000.0;
                    return m.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "M";
                }
                if (score >= 1_000)
                {
                    double k = score / 1000.0;
                    return k.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + kSuffix;
                }
                return score.ToString();
            }

            if (formatScoreWithCommas)
            {
                return score.ToString("N0");
            }
            return score.ToString();
        }

        private string FormatRank(int rank)
        {
            if (rank <= 0) return emptyPlaceholder;

            // Nếu rank vượt quá giới hạn (ví dụ > 999 thì hiện 999+)
            if (maxRankCap > 0 && rank > maxRankCap)
            {
                return dropRankPrefixOnCompact ? $"{maxRankCap}+" : $"{rankPrefix}{maxRankCap}+";
            }

            if (useCompactRankFormat && rank >= 1000)
            {
                string prefix = dropRankPrefixOnCompact ? "" : rankPrefix;
                string kSuffix = useLowercaseK ? "k" : "K";

                if (rank >= 1_000_000_000)
                {
                    double b = rank / 1_000_000_000.0;
                    return prefix + b.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "B";
                }
                if (rank >= 1_000_000)
                {
                    double m = rank / 1_000_000.0;
                    return prefix + m.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "M";
                }
                double k = rank / 1000.0;
                return prefix + k.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + kSuffix;
            }

            return $"{rankPrefix}{rank}";
        }

        private void SetStatus(string message)
        {
            if (statusTMP != null)
            {
                statusTMP.text = message;
            }
        }

        private void SetLoadingState(bool loading)
        {
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(loading);
            }
        }

        private void PlayButtonClickSound()
        {
            if (audioSource != null && buttonClickSound != null)
            {
                audioSource.PlayOneShot(buttonClickSound);
            }
        }

        #endregion

        #region Unity Editor Context Menu

        [ContextMenu("🧪 Điền Dữ Liệu Mẫu Thử Nghiệm UI (Mock Data)")]
        public void FillMockDataForTesting()
        {
            long[] sampleScores = new long[] { 2500000, 1850000, 1200000, 850000, 500000, 250000, 100000, 50000, 25000, 10000 };

            for (int i = 0; i < top10Rows.Length; i++)
            {
                if (top10Rows[i] == null) continue;
                if (top10Rows[i].displayNameTMP != null) top10Rows[i].displayNameTMP.text = $"Player_{i + 1}";
                long s = i < sampleScores.Length ? sampleScores[i] : 1000;
                if (top10Rows[i].scoreTMP != null) top10Rows[i].scoreTMP.text = FormatScore(s);
            }

            if (currentRankTMP != null)
            {
                currentRankTMP.textWrappingMode = TextWrappingModes.NoWrap;
                currentRankTMP.enableAutoSizing = true;
                if (currentRankTMP.fontSizeMin < 8f) currentRankTMP.fontSizeMin = 8f;
                currentRankTMP.text = FormatRank(12500); // Test rank 12,500 -> 12.5k
            }

            if (currentDisplayNameTMP != null) currentDisplayNameTMP.text = "BanDangChoi";
            if (currentScoreTMP != null) currentScoreTMP.text = FormatScore(75000);

            Debug.Log("[LeaderboardManager] Đã điền dữ liệu mẫu (rút gọn k, M và Rank) lên UI để bạn xem trước!");
        }

        #endregion
    }
}
