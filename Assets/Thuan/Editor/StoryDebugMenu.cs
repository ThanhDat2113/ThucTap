using UnityEditor;
using UnityEngine;

// Menu test Story Mode: Unity -> Thuan -> Story
public static class StoryDebugMenu
{
    private static StoryWeekData[] AllWeeks()
    {
        var guids = AssetDatabase.FindAssets("t:StoryWeekData");
        var list = new StoryWeekData[guids.Length];
        for (int i = 0; i < guids.Length; i++) list[i] = AssetDatabase.LoadAssetAtPath<StoryWeekData>(AssetDatabase.GUIDToAssetPath(guids[i]));
        return list;
    }

    [MenuItem("Thuan/Story/Reset tien do Story Mode")]
    public static void ResetStory()
    {
        StoryProgress.ResetAll(AllWeeks());
        Debug.Log("[Story] Da reset tien do - chi con Week 1 mo khoa.");
    }

    [MenuItem("Thuan/Story/Danh dau da qua tat ca cac tuan")]
    public static void ClearAll()
    {
        foreach (var w in AllWeeks()) StoryProgress.SaveWeek(w, 0, -1f);
        Debug.Log("[Story] Da danh dau qua tat ca cac tuan.");
    }

    private const string AutoResetMenu = "Thuan/Story/Tu dong reset tien do khi Play Mode";

    [MenuItem(AutoResetMenu)]
    public static void ToggleAutoResetOnPlay()
    {
        bool next = !StoryProgress.AutoResetOnPlayMode;
        StoryProgress.AutoResetOnPlayMode = next;
        Debug.Log("[Story] Tu dong reset khi Play Mode: " + (next ? "BAT" : "TAT"));
    }

    [MenuItem(AutoResetMenu, true)]
    private static bool ValidateToggleAutoResetOnPlay()
    {
        Menu.SetChecked(AutoResetMenu, StoryProgress.AutoResetOnPlayMode);
        return true;
    }
}
