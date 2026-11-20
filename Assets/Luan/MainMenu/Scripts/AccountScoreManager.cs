using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using RhythmGame;

namespace Luan.MainMenu
{
    /// <summary>
    /// Quản lý tính toán tổng điểm thành tựu của tài khoản người chơi.
    /// - Cộng dồn điểm cao nhất của TẤT CẢ các bài hát (kể cả Free Play / các bài chưa xếp hạng).
    /// - Cộng dồn điểm các Chapter Story Mode.
    /// - Tự động đồng bộ lên PlayFab Leaderboard ("HighScore" & "TotalScore") khi kết thúc ván chơi hoặc khi đăng nhập.
    /// </summary>
    public static class AccountScoreManager
    {
        // Danh sách tên các bài hát mặc định trong game để tính điểm kể cả khi chưa load asset
        private static readonly string[] KnownSongNames = new string[]
        {
            "Dance Alone",
            "Digital Days",
            "Gathers Under Night",
            "KUSANAGI",
            "Monody",
            "Nevada",
            "Reverence",
            "Save Me",
            "Shred"
        };

        private const string PrefsTotalScoreCacheKey = "ACCOUNT_TOTAL_ACHIEVEMENT_SCORE";
        private const string PrefsLastSubmittedScoreKey = "ACCOUNT_LAST_SUBMITTED_SCORE";

        private static bool _isSyncing = false;
        private static Runner _runner = null;

        private class Runner : MonoBehaviour
        {
            private void Awake()
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private static void EnsureRunner()
        {
            if (_runner == null)
            {
                var go = new GameObject("[AccountScoreManager_Runner]");
                _runner = go.AddComponent<Runner>();
            }
        }

        /// <summary>
        /// Tính tổng điểm thành tựu tích lũy của toàn bộ tài khoản:
        /// Cộng điểm cao nhất của từng bài hát trong Free Play (các bài chưa xếp hạng) + Story Chapters.
        /// </summary>
        public static int CalculateTotalAccountScore()
        {
            int totalScore = 0;
            HashSet<string> processedSongs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Quét danh sách bài hát cố định
            foreach (var songName in KnownSongNames)
            {
                if (processedSongs.Contains(songName)) continue;
                processedSongs.Add(songName);

                int best = GetBestScoreForSongName(songName);
                totalScore += best;
            }

            // 2. Quét thêm các SongData đang có trong bộ nhớ (nếu có bài mới thêm)
            var loadedSongs = Resources.FindObjectsOfTypeAll<SongData>();
            if (loadedSongs != null)
            {
                foreach (var s in loadedSongs)
                {
                    if (s == null || string.IsNullOrEmpty(s.name)) continue;
                    if (processedSongs.Contains(s.name)) continue;
                    processedSongs.Add(s.name);

                    int best = GetBestScoreForSong(s);
                    totalScore += best;
                }
            }

            // 3. Quét điểm từ các Chapter của Story Mode (Chapter 1 -> 4)
            for (int ch = 1; ch <= 4; ch++)
            {
                int chapterBest = GetBestScoreForChapter(ch);
                totalScore += chapterBest;
            }

            // 4. Kiểm tra điểm tích lũy bổ sung nếu có
            int bonus = PlayerPrefs.GetInt("ACCOUNT_ACCUMULATED_POINTS", 0);
            totalScore += bonus;

            // Lưu cache cục bộ
            PlayerPrefs.SetInt(PrefsTotalScoreCacheKey, totalScore);
            PlayerPrefs.Save();

            Debug.Log($"[AccountScoreManager] Tổng điểm thành tựu tính được: {totalScore:N0} (từ {processedSongs.Count} bài hát + 4 Story Chapters)");
            return totalScore;
        }

        private static int GetBestScoreForSongName(string songName)
        {
            int max = 0;

            // Kiểm tra theo từng độ khó: Easy (0), Normal (1), Hard (2)
            for (int d = 0; d <= 2; d++)
            {
                int s = PlayerPrefs.GetInt($"stats_{songName}_{d}_best", 0);
                if (s > max) max = s;
            }

            // Kiểm tra legacy key
            int legacy = PlayerPrefs.GetInt($"stats_{songName}_best", 0);
            if (legacy > max) max = legacy;

            // Kiểm tra key của GameplayResultScreen
            int gameplayBest = PlayerPrefs.GetInt($"best_score_{songName}", 0);
            if (gameplayBest > max) max = gameplayBest;

            return max;
        }

        private static int GetBestScoreForSong(SongData song)
        {
            if (song == null) return 0;
            int max = GetBestScoreForSongName(song.name);

            // Thử kiểm tra qua SongStats nếu có
            try
            {
                int easyScore = SongStats.GetBestScore(song, ChartDifficulty.Easy);
                int normalScore = SongStats.GetBestScore(song, ChartDifficulty.Normal);
                int hardScore = SongStats.GetBestScore(song, ChartDifficulty.Hard);
                max = Mathf.Max(max, Mathf.Max(easyScore, Mathf.Max(normalScore, hardScore)));
            }
            catch { }

            return max;
        }

        private static int GetBestScoreForChapter(int chapterNum)
        {
            int max = 0;
            // StoryProgress lưu theo slot độ khó 0, 1, 2 và các biến thể key
            for (int slot = 0; slot <= 2; slot++)
            {
                int s1 = PlayerPrefs.GetInt($"story_Week{chapterNum}_best_{slot}", 0);
                if (s1 > max) max = s1;

                int s2 = PlayerPrefs.GetInt($"story_week_{chapterNum}_best_{slot}", 0);
                if (s2 > max) max = s2;

                int s3 = PlayerPrefs.GetInt($"story_chapter_{chapterNum}_best_{slot}", 0);
                if (s3 > max) max = s3;

                int s4 = PlayerPrefs.GetInt($"story_Chapter{chapterNum}_best_{slot}", 0);
                if (s4 > max) max = s4;
            }

            int leg1 = PlayerPrefs.GetInt($"story_Week{chapterNum}_best", 0);
            if (leg1 > max) max = leg1;

            int leg2 = PlayerPrefs.GetInt($"story_chapter_{chapterNum}_best", 0);
            if (leg2 > max) max = leg2;

            return max;
        }

        /// <summary>
        /// Gọi khi người chơi hoàn thành 1 bài hát (cả Free Play lẫn Story Mode).
        /// Tự động tính lại tổng điểm thành tựu và gửi lên PlayFab Leaderboard.
        /// </summary>
        public static void OnSongCompleted(SongData song, int matchScore, bool cleared)
        {
            if (song != null)
            {
                Debug.Log($"[AccountScoreManager] Ghi nhận điểm hoàn thành bài '{song.songName}': {matchScore:N0} (Cleared: {cleared})");
            }

            int totalScore = CalculateTotalAccountScore();

            // Nếu người chơi đã đăng nhập, đồng bộ ngay lên PlayFab
            if (IsUserLoggedIn())
            {
                SyncScoreToPlayFab(null);
            }
            else
            {
                Debug.Log("[AccountScoreManager] Người chơi chưa đăng nhập PlayFab — điểm đã được lưu cục bộ và sẽ đồng bộ khi đăng nhập.");
            }
        }

        public static bool IsUserLoggedIn()
        {
            if (AccountManager.Instance != null && AccountManager.Instance.IsLoggedIn)
                return true;
            return !string.IsNullOrEmpty(PlayerPrefs.GetString("PLAYFAB_SESSION_TICKET", ""));
        }

        public static string GetSessionTicket()
        {
            if (AccountManager.Instance != null && AccountManager.Instance.CurrentUser != null)
                return AccountManager.Instance.CurrentUser.sessionTicket;
            return PlayerPrefs.GetString("PLAYFAB_SESSION_TICKET", "");
        }

        public static string GetTitleId()
        {
            if (AccountManager.Instance != null && !string.IsNullOrEmpty(AccountManager.Instance.PlayFabTitleId))
                return AccountManager.Instance.PlayFabTitleId;
            return "12FBEE";
        }

        /// <summary>
        /// Đồng bộ tổng điểm thành tựu lên PlayFab (StatisticName: HighScore).
        /// </summary>
        public static void SyncScoreToPlayFab(Action<bool, int> onComplete = null)
        {
            if (_isSyncing) return;

            string sessionTicket = GetSessionTicket();
            string titleId = GetTitleId();

            if (string.IsNullOrEmpty(sessionTicket) || string.IsNullOrEmpty(titleId))
            {
                onComplete?.Invoke(false, 0);
                return;
            }

            int totalScore = CalculateTotalAccountScore();
            int lastSubmitted = PlayerPrefs.GetInt(PrefsLastSubmittedScoreKey, -1);

            // Nếu điểm không đổi so với lần submit gần nhất thì chỉ thông báo thành công
            if (totalScore == lastSubmitted && totalScore > 0)
            {
                onComplete?.Invoke(true, totalScore);
                return;
            }

            EnsureRunner();
            _runner.StartCoroutine(SubmitRoutine(titleId, sessionTicket, totalScore, onComplete));
        }

        private static IEnumerator SubmitRoutine(string titleId, string sessionTicket, int totalScore, Action<bool, int> onComplete)
        {
            _isSyncing = true;
            string url = $"https://{titleId}.playfabapi.com/Client/UpdatePlayerStatistics";

            PlayFabUpdatePlayerStatisticsRequest reqObj = new PlayFabUpdatePlayerStatisticsRequest
            {
                Statistics = new PlayFabStatisticUpdate[]
                {
                    new PlayFabStatisticUpdate
                    {
                        StatisticName = "HighScore",
                        Value = totalScore
                    },
                    new PlayFabStatisticUpdate
                    {
                        StatisticName = "TotalScore",
                        Value = totalScore
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

                _isSyncing = false;

                if (req.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[AccountScoreManager] Đồng bộ thành công tổng điểm thành tựu {totalScore:N0} lên PlayFab!");
                    PlayerPrefs.SetInt(PrefsLastSubmittedScoreKey, totalScore);
                    PlayerPrefs.Save();

                    // Cập nhật dữ liệu tài khoản trong AccountManager
                    if (AccountManager.Instance != null && AccountManager.Instance.CurrentUser != null)
                    {
                        AccountManager.Instance.CurrentUser.highScore = totalScore;
                    }

                    // Cập nhật trực tiếp LeaderboardManager nếu đang mở
                    if (LeaderboardManager.Instance != null)
                    {
                        LeaderboardManager.Instance.NotifyLocalScoreUpdated(totalScore);
                    }

                    onComplete?.Invoke(true, totalScore);
                }
                else
                {
                    Debug.LogWarning($"[AccountScoreManager] Gửi điểm lên PlayFab thất bại: {req.downloadHandler.text}");
                    onComplete?.Invoke(false, totalScore);
                }
            }
        }
    }
}
