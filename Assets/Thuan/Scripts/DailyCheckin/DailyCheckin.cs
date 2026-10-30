using System;
using System.Globalization;
using UnityEngine;

// Trang thai diem danh hang ngay, luu bang PlayerPrefs.
// - Moi ngay (theo gio may) nhan 1 lan.
// - Chu ky 7 ngay: nhan xong ngay 7 thi hom sau quay lai ngay 1.
// - Bo lo 1 ngay: chuoi dang nhap (streak) ve 0, nhung tien do chu ky van giu nguyen.
public static class DailyCheckin
{
    public const int CycleLength = 7;

    private const string LastClaimKey = "daily_last_claim";
    private const string ClaimedKey = "daily_claimed_count";
    private const string StreakKey = "daily_streak";
    private const string DateFormat = "yyyy-MM-dd";

    public static event Action Changed;

    private static DateTime Today { get { return DateTime.Now.Date; } }

    public static DateTime? LastClaimDate
    {
        get
        {
            string s = PlayerPrefs.GetString(LastClaimKey, "");
            DateTime d;
            if (DateTime.TryParseExact(s, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out d)) return d;
            return null;
        }
    }

    public static bool CanClaimToday
    {
        get { var last = LastClaimDate; return last == null || last.Value < Today; }
    }

    // So ngay da nhan trong chu ky hien tai (0..7)
    public static int ClaimedInCycle
    {
        get
        {
            int c = Mathf.Clamp(PlayerPrefs.GetInt(ClaimedKey, 0), 0, CycleLength);
            if (CanClaimToday && c >= CycleLength) return 0; // het chu ky -> bat dau lai
            return c;
        }
    }

    // Ngay dang duoc hien la "hom nay" (0..6)
    public static int TodayIndex
    {
        get { return CanClaimToday ? ClaimedInCycle : Mathf.Max(0, ClaimedInCycle - 1); }
    }

    private static bool StreakAlive
    {
        get { var last = LastClaimDate; return last != null && (Today - last.Value).TotalDays <= 1; }
    }

    public static int Streak
    {
        get { return StreakAlive ? PlayerPrefs.GetInt(StreakKey, 0) : 0; }
    }

    public static TimeSpan TimeUntilNextDay
    {
        get { return Today.AddDays(1) - DateTime.Now; }
    }

    // Tra ve index ngay vua nhan, -1 neu hom nay da nhan roi
    public static int Claim()
    {
        if (!CanClaimToday) return -1;
        int index = ClaimedInCycle;
        int streak = StreakAlive ? PlayerPrefs.GetInt(StreakKey, 0) + 1 : 1;

        PlayerPrefs.SetInt(ClaimedKey, index + 1);
        PlayerPrefs.SetInt(StreakKey, streak);
        PlayerPrefs.SetString(LastClaimKey, Today.ToString(DateFormat, CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
        if (Changed != null) Changed();
        return index;
    }

    // ---- Dung de test ----
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(LastClaimKey);
        PlayerPrefs.DeleteKey(ClaimedKey);
        PlayerPrefs.DeleteKey(StreakKey);
        PlayerPrefs.Save();
        if (Changed != null) Changed();
    }

    // Gia lap sang ngay moi: lui ngay nhan cuoi ve hom qua (giu streak)
    public static void SimulateNextDay()
    {
        if (LastClaimDate == null) return;
        PlayerPrefs.SetString(LastClaimKey, Today.AddDays(-1).ToString(DateFormat, CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
        if (Changed != null) Changed();
    }
}
