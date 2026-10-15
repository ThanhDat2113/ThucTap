using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#region PlayFab Currency Data Models

[Serializable]
public class PlayFabStatisticUpdate
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

[Serializable]
public class PlayFabStatisticValue
{
    public string StatisticName;
    public int Value;
    public int Version;
}

[Serializable]
internal class PlayFabGetPlayerStatisticsRequest
{
    public string[] StatisticNames;
}

[Serializable]
internal class PlayFabGetPlayerStatisticsResult
{
    public PlayFabStatisticValue[] Statistics;
}

[Serializable]
internal class PlayFabGetPlayerStatisticsResponse
{
    public int code;
    public string status;
    public PlayFabGetPlayerStatisticsResult data;
    public string errorMessage;
}

#endregion

/// <summary>
/// Ví tiền người chơi (Vàng, Kim Cương, Vé Quay) + đồ đã sở hữu.
/// Lưu trữ cục bộ bằng PlayerPrefs và tự động đồng bộ hóa lên PlayFab (Player Statistics).
/// </summary>
public static class Wallet
{
    public const int StartCoins = 10000;
    public const int StartGems = 1000;

    private const string CoinKey = "wallet_coins";
    private const string GemKey = "wallet_gems";
    private const string OwnedPrefix = "owned_";
    private const string TicketKey = "wallet_tickets";

    public static event Action Changed;

    public static int Coins { get { return PlayerPrefs.GetInt(CoinKey, StartCoins); } }
    public static int Gems { get { return PlayerPrefs.GetInt(GemKey, StartGems); } }
    public static int Tickets { get { return PlayerPrefs.GetInt(TicketKey, 0); } }

    public static int Get(CurrencyType type) { return type == CurrencyType.Coin ? Coins : Gems; }

    public static void Add(CurrencyType type, int amount)
    {
        int newAmount = Mathf.Max(0, Get(type) + amount);
        PlayerPrefs.SetInt(type == CurrencyType.Coin ? CoinKey : GemKey, newAmount);
        PlayerPrefs.Save();
        Changed?.Invoke();
        RequestDebouncedSyncToPlayFab();
    }

    public static void AddTickets(int amount)
    {
        int newAmount = Mathf.Max(0, Tickets + amount);
        PlayerPrefs.SetInt(TicketKey, newAmount);
        PlayerPrefs.Save();
        Changed?.Invoke();
        RequestDebouncedSyncToPlayFab();
    }

    public static void SetCoins(int amount, bool notify = true)
    {
        PlayerPrefs.SetInt(CoinKey, Mathf.Max(0, amount));
        PlayerPrefs.Save();
        if (notify) Changed?.Invoke();
        RequestDebouncedSyncToPlayFab();
    }

    public static void SetGems(int amount, bool notify = true)
    {
        PlayerPrefs.SetInt(GemKey, Mathf.Max(0, amount));
        PlayerPrefs.Save();
        if (notify) Changed?.Invoke();
        RequestDebouncedSyncToPlayFab();
    }

    public static void SetTickets(int amount, bool notify = true)
    {
        PlayerPrefs.SetInt(TicketKey, Mathf.Max(0, amount));
        PlayerPrefs.Save();
        if (notify) Changed?.Invoke();
        RequestDebouncedSyncToPlayFab();
    }

    public static void SetCurrencies(int coins, int gems, int tickets, bool notify = true)
    {
        PlayerPrefs.SetInt(CoinKey, Mathf.Max(0, coins));
        PlayerPrefs.SetInt(GemKey, Mathf.Max(0, gems));
        PlayerPrefs.SetInt(TicketKey, Mathf.Max(0, tickets));
        PlayerPrefs.Save();
        if (notify) Changed?.Invoke();
        RequestDebouncedSyncToPlayFab();
    }

    // Tặng đồ miễn phí (vd: quà điểm danh)
    public static void GrantItem(string itemId)
    {
        PlayerPrefs.SetInt(OwnedPrefix + itemId, 1);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    public static bool IsOwned(string itemId) { return PlayerPrefs.GetInt(OwnedPrefix + itemId, 0) == 1; }

    public static bool TryBuy(ShopItemData item)
    {
        if (item == null || IsOwned(item.Id)) return false;
        if (Get(item.currency) < item.price) return false;
        PlayerPrefs.SetInt(item.currency == CurrencyType.Coin ? CoinKey : GemKey, Get(item.currency) - item.price);
        PlayerPrefs.SetInt(OwnedPrefix + item.Id, 1);
        PlayerPrefs.Save();
        Changed?.Invoke();
        RequestDebouncedSyncToPlayFab();
        return true;
    }

    #region Đồng Bộ Hóa PlayFab (PlayFab Sync Layer)

    private static bool _isSyncing = false;
    private static bool _isDirty = false;
    private static WalletRunner _runner = null;

    private static WalletRunner EnsureRunner()
    {
        if (_runner == null)
        {
            var go = new GameObject("[WalletSyncRunner]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<WalletRunner>();
        }
        return _runner;
    }

    private class WalletRunner : MonoBehaviour
    {
        private Coroutine _debounceCoroutine;

        public void QueueSync(float delay)
        {
            if (_debounceCoroutine != null)
            {
                StopCoroutine(_debounceCoroutine);
            }
            _debounceCoroutine = StartCoroutine(DebounceRoutine(delay));
        }

        private IEnumerator DebounceRoutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _debounceCoroutine = null;
            yield return Wallet.SyncToPlayFabRoutine();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause && Wallet._isDirty)
            {
                StartCoroutine(Wallet.SyncToPlayFabRoutine());
            }
        }
    }

    private static string GetEffectiveTitleId()
    {
        if (Luan.MainMenu.AccountManager.Instance != null && !string.IsNullOrEmpty(Luan.MainMenu.AccountManager.Instance.PlayFabTitleId))
        {
            return Luan.MainMenu.AccountManager.Instance.PlayFabTitleId;
        }
        return "12FBEE";
    }

    private static string GetSessionTicket()
    {
        if (Luan.MainMenu.AccountManager.Instance != null && Luan.MainMenu.AccountManager.Instance.CurrentUser != null)
        {
            return Luan.MainMenu.AccountManager.Instance.CurrentUser.sessionTicket;
        }
        return PlayerPrefs.GetString("PLAYFAB_SESSION_TICKET", "");
    }

    public static bool IsLoggedIn
    {
        get
        {
            if (Luan.MainMenu.AccountManager.Instance != null)
            {
                return Luan.MainMenu.AccountManager.Instance.IsLoggedIn;
            }
            return !string.IsNullOrEmpty(PlayerPrefs.GetString("PLAYFAB_SESSION_TICKET", ""));
        }
    }

    /// <summary>
    /// Yêu cầu đồng bộ lên PlayFab với cơ chế chống spam (debounce 1.5 giây).
    /// </summary>
    public static void RequestDebouncedSyncToPlayFab()
    {
        if (!IsLoggedIn) return;
        _isDirty = true;
        if (Application.isPlaying)
        {
            EnsureRunner().QueueSync(1.5f);
        }
    }

    /// <summary>
    /// Xử lý cập nhật số dư từ các Statistic trả về từ PlayFab (Coins, Gems, Tickets).
    /// </summary>
    public static void ProcessPlayFabStatistics(PlayFabStatisticValue[] stats)
    {
        if (stats == null || stats.Length == 0)
        {
            // Tài khoản mới tạo chưa có chỉ số trên server -> Cấp 10.000 Vàng và 1.000 Kim Cương khởi tạo
            SetCurrencies(StartCoins, StartGems, 0);
            SyncToPlayFab();
            return;
        }

        int foundCoins = -1;
        int foundGems = -1;
        int foundTickets = -1;

        for (int i = 0; i < stats.Length; i++)
        {
            var s = stats[i];
            if (s == null) continue;
            string name = s.StatisticName;
            if (string.Equals(name, "Coins", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Gold", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Vang", StringComparison.OrdinalIgnoreCase))
            {
                foundCoins = s.Value;
            }
            else if (string.Equals(name, "Gems", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(name, "Diamonds", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(name, "KimCuong", StringComparison.OrdinalIgnoreCase))
            {
                foundGems = s.Value;
            }
            else if (string.Equals(name, "Tickets", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(name, "SpinTickets", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(name, "VeQuay", StringComparison.OrdinalIgnoreCase))
            {
                foundTickets = s.Value;
            }
        }

        bool changed = false;
        if (foundCoins >= 0)
        {
            PlayerPrefs.SetInt(CoinKey, foundCoins);
            changed = true;
        }
        else
        {
            // Tài khoản chưa có dữ liệu Vàng -> Khởi tạo 10.000 Vàng
            PlayerPrefs.SetInt(CoinKey, StartCoins);
            changed = true;
        }

        if (foundGems >= 0)
        {
            PlayerPrefs.SetInt(GemKey, foundGems);
            changed = true;
        }
        else
        {
            // Tài khoản chưa có dữ liệu Kim Cương -> Khởi tạo 1.000 Kim Cương
            PlayerPrefs.SetInt(GemKey, StartGems);
            changed = true;
        }

        if (foundTickets >= 0)
        {
            PlayerPrefs.SetInt(TicketKey, foundTickets);
            changed = true;
        }

        if (foundCoins < 0 || foundGems < 0 || foundTickets < 0)
        {
            // Nếu server còn thiếu chỉ số nào, đẩy chỉ số cục bộ lên PlayFab
            SyncToPlayFab();
        }

        if (changed)
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Đồng bộ ngay lập tức các chỉ số Coins, Gems, Tickets lên PlayFab.
    /// </summary>
    public static void SyncToPlayFab(Action<bool> onComplete = null)
    {
        if (!IsLoggedIn || !Application.isPlaying)
        {
            onComplete?.Invoke(false);
            return;
        }
        EnsureRunner().StartCoroutine(SyncToPlayFabRoutine(onComplete));
    }

    public static IEnumerator SyncToPlayFabRoutine(Action<bool> onComplete = null)
    {
        if (_isSyncing)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        string titleId = GetEffectiveTitleId();
        string sessionTicket = GetSessionTicket();

        if (string.IsNullOrEmpty(titleId) || string.IsNullOrEmpty(sessionTicket))
        {
            onComplete?.Invoke(false);
            yield break;
        }

        _isSyncing = true;
        int cCoins = Coins;
        int cGems = Gems;
        int cTickets = Tickets;

        string url = $"https://{titleId}.playfabapi.com/Client/UpdatePlayerStatistics";
        PlayFabUpdatePlayerStatisticsRequest reqObj = new PlayFabUpdatePlayerStatisticsRequest
        {
            Statistics = new PlayFabStatisticUpdate[]
            {
                new PlayFabStatisticUpdate { StatisticName = "Coins", Value = cCoins },
                new PlayFabStatisticUpdate { StatisticName = "Gems", Value = cGems },
                new PlayFabStatisticUpdate { StatisticName = "Tickets", Value = cTickets }
            }
        };

        string json = JsonUtility.ToJson(reqObj);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Authorization", sessionTicket);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                _isDirty = false;
                Debug.Log($"[Wallet] Đồng bộ tiền tệ lên PlayFab thành công: Vàng={cCoins}, Kim Cương={cGems}, Vé={cTickets}");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogWarning($"[Wallet] Đồng bộ tiền tệ lên PlayFab thất bại: {req.downloadHandler.text}");
                onComplete?.Invoke(false);
            }
        }

        _isSyncing = false;
    }

    /// <summary>
    /// Kéo dữ liệu tiền tệ mới nhất từ PlayFab về máy.
    /// </summary>
    public static void FetchFromPlayFab(Action<bool> onComplete = null)
    {
        if (!IsLoggedIn || !Application.isPlaying)
        {
            onComplete?.Invoke(false);
            return;
        }
        EnsureRunner().StartCoroutine(FetchFromPlayFabRoutine(onComplete));
    }

    public static IEnumerator FetchFromPlayFabRoutine(Action<bool> onComplete = null)
    {
        string titleId = GetEffectiveTitleId();
        string sessionTicket = GetSessionTicket();

        if (string.IsNullOrEmpty(titleId) || string.IsNullOrEmpty(sessionTicket))
        {
            onComplete?.Invoke(false);
            yield break;
        }

        string url = $"https://{titleId}.playfabapi.com/Client/GetPlayerStatistics";
        PlayFabGetPlayerStatisticsRequest reqObj = new PlayFabGetPlayerStatisticsRequest
        {
            StatisticNames = new string[] { "Coins", "Gold", "Gems", "Diamonds", "Tickets", "SpinTickets" }
        };

        string json = JsonUtility.ToJson(reqObj);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Authorization", sessionTicket);

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    PlayFabGetPlayerStatisticsResponse res = JsonUtility.FromJson<PlayFabGetPlayerStatisticsResponse>(req.downloadHandler.text);
                    if (res != null && res.data != null && res.data.Statistics != null)
                    {
                        ProcessPlayFabStatistics(res.data.Statistics);
                        onComplete?.Invoke(true);
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Wallet] Lỗi parse dữ liệu PlayFab GetPlayerStatistics: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[Wallet] Lấy dữ liệu PlayFab thất bại: {req.downloadHandler.text}");
            }
        }

        onComplete?.Invoke(false);
    }

    #endregion
}
