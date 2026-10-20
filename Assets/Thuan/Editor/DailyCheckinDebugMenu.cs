using UnityEditor;
using UnityEngine;

// Menu test diem danh: Unity -> Thuan -> Daily Check-in
public static class DailyCheckinDebugMenu
{
    [MenuItem("Thuan/Daily Check-in/Reset diem danh (ve ngay 1)")]
    public static void ResetCheckin()
    {
        DailyCheckin.ResetAll();
        Debug.Log("[Daily] Da reset diem danh ve ngay 1.");
    }

    [MenuItem("Thuan/Daily Check-in/Gia lap sang ngay moi")]
    public static void NextDay()
    {
        DailyCheckin.SimulateNextDay();
        Debug.Log("[Daily] Da gia lap sang ngay moi - co the nhan qua tiep. Ngay hien tai: " + (DailyCheckin.TodayIndex + 1));
    }
}
