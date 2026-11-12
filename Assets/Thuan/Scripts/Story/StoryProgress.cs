using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Tien do Story Mode (luu PlayerPrefs) + luot choi dang chay (StorySession)
public static class StoryProgress
{
#if UNITY_EDITOR
    public const string AutoResetPlayModeEditorPref = "Story_AutoResetOnPlayMode";

    public static bool AutoResetOnPlayMode
    {
        get { return EditorPrefs.GetBool(AutoResetPlayModeEditorPref, true); }
        set { EditorPrefs.SetBool(AutoResetPlayModeEditorPref, value); }
    }

    public static void ResetAllInEditor()
    {
        var guids = AssetDatabase.FindAssets("t:StoryWeekData");
        var weeks = new StoryWeekData[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            weeks[i] = AssetDatabase.LoadAssetAtPath<StoryWeekData>(AssetDatabase.GUIDToAssetPath(guids[i]));
        ResetAll(weeks);
        PlayerPrefs.DeleteKey("story_last_week");
        PlayerPrefs.Save();
    }
#endif

    private static string Key(StoryWeekData w, string f) { return "story_" + w.Id + "_" + f; }
    private static string KeyByNumber(int weekNumber, string f) { return "story_Week" + weekNumber + "_" + f; }

    public static bool IsCleared(StoryWeekData w) { return w != null && PlayerPrefs.GetInt(Key(w, "clear"), 0) == 1; }
    // Story Mode khong chon do kho: luon choi muc Normal
    public const ChartDifficulty Difficulty = ChartDifficulty.Normal;
    private static string Slot { get { return ((int)Difficulty).ToString(); } }

    public static int GetBestScore(StoryWeekData w) { return PlayerPrefs.GetInt(Key(w, "best_" + Slot), 0); }
    public static float GetClearRate(StoryWeekData w) { return PlayerPrefs.GetFloat(Key(w, "acc_" + Slot), -1f); }

    public static void SaveWeek(StoryWeekData w, int totalScore, float accuracy)
    {
        if (w == null) return;
        PlayerPrefs.SetInt(Key(w, "clear"), 1);
        if (totalScore > GetBestScore(w)) PlayerPrefs.SetInt(Key(w, "best_" + Slot), totalScore);
        if (accuracy > GetClearRate(w)) PlayerPrefs.SetFloat(Key(w, "acc_" + Slot), accuracy);
        PlayerPrefs.Save();
    }

    // Dung khi thang 1 tran truc tiep trong GameplayScene (khong di qua StoryModeController)
    public static void SaveChapterByNumber(int weekNumber, int totalScore, float accuracy)
    {
        if (weekNumber <= 0) return;
        PlayerPrefs.SetInt(KeyByNumber(weekNumber, "clear"), 1);
        string bestKey = KeyByNumber(weekNumber, "best_" + Slot);
        string accKey = KeyByNumber(weekNumber, "acc_" + Slot);
        if (totalScore > PlayerPrefs.GetInt(bestKey, 0)) PlayerPrefs.SetInt(bestKey, totalScore);
        if (accuracy > PlayerPrefs.GetFloat(accKey, -1f)) PlayerPrefs.SetFloat(accKey, accuracy);
        PlayerPrefs.Save();
    }

    // Hang theo do chinh xac
    // Dung chung thang hang voi man tong ket
    public static string Rank(float accuracy) { return GameplayResultScreen.RankOf(true, accuracy); }

    public static void ResetAll(StoryWeekData[] weeks)
    {
        foreach (var w in weeks)
        {
            if (w == null) continue;
            PlayerPrefs.DeleteKey(Key(w, "clear"));
            for (int d = 0; d < 3; d++) { PlayerPrefs.DeleteKey(Key(w, "best_" + d)); PlayerPrefs.DeleteKey(Key(w, "acc_" + d)); }
        }
        PlayerPrefs.Save();
    }
}

public static class StorySession
{
    public static bool Active { get; private set; }
    public static StoryWeekData Week { get; private set; }
    public static int TrackIndex { get; private set; }
    public static int TotalScore { get; private set; }
    public static bool LastCleared { get; private set; }
    public static bool WeekCompleted { get; private set; }

    private static float accSum;
    private static ChartDifficulty savedDifficulty;
    private static int accCount;

    public static int TrackCount { get { return Week != null && Week.tracks != null ? Week.tracks.Length : 0; } }
    public static bool HasNext { get { return Active && TrackIndex < TrackCount - 1; } }
    public static float AverageAccuracy { get { return accCount > 0 ? accSum / accCount : -1f; } }

    public static void Begin(StoryWeekData week)
    {
        Active = true; Week = week; TrackIndex = 0; TotalScore = 0; accSum = 0f; accCount = 0; pendingScore = 0; pendingAcc = -1f;
        LastCleared = false; WeekCompleted = false;
        // giu lai do kho cua Free Play, Story luon choi Normal
        savedDifficulty = GameSession.SelectedDifficulty;
        GameSession.SelectedDifficulty = StoryProgress.Difficulty;
        GameSession.SelectedSong = week.tracks[0].song;
    }

    private static int pendingScore;
    private static float pendingAcc = -1f;

    // Goi khi 1 bai ket thuc. Thang 1 tran trong Story Mode se hoan thanh Chapter & unlock Chapter ke tiep ngay.
    public static void Report(bool cleared, int score, float accuracy)
    {
        if (!Active) return;
        LastCleared = cleared;
        if (!cleared) return;
        pendingScore = score; pendingAcc = accuracy;
        WeekCompleted = true;
        int total = TotalScore + pendingScore;
        float sum = accSum + (pendingAcc >= 0f ? pendingAcc : 0f);
        int cnt = accCount + (pendingAcc >= 0f ? 1 : 0);
        StoryProgress.SaveWeek(Week, total, cnt > 0 ? sum / cnt : -1f);
    }

    public static int RunScore { get { return TotalScore + (LastCleared ? pendingScore : 0); } }

    public static void Advance()
    {
        if (!HasNext || !LastCleared) return;
        TotalScore += pendingScore;
        if (pendingAcc >= 0f) { accSum += pendingAcc; accCount++; }
        pendingScore = 0; pendingAcc = -1f;
        TrackIndex++;
        LastCleared = false;
        GameSession.SelectedSong = Week.tracks[TrackIndex].song;
    }

    public static void End()
    {
        if (Active) GameSession.SelectedDifficulty = savedDifficulty;
        Active = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticStateOnPlay()
    {
        Active = false;
        Week = null;
        TrackIndex = 0;
        TotalScore = 0;
        LastCleared = false;
        WeekCompleted = false;
        accSum = 0f;
        savedDifficulty = ChartDifficulty.Normal;
        accCount = 0;
        pendingScore = 0;
        pendingAcc = -1f;
        GameSession.SelectedSong = null;
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetStoryProgressOnPlayInEditor()
    {
        if (StoryProgress.AutoResetOnPlayMode)
        {
            StoryProgress.ResetAllInEditor();
            Debug.Log("[Story] Da restart tien do Story Mode khi vao Play Mode.");
        }
    }
#endif
}
