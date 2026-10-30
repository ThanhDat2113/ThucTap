using UnityEngine;

// Tien do Story Mode (luu PlayerPrefs) + luot choi dang chay (StorySession)
public static class StoryProgress
{
    private static string Key(StoryWeekData w, string f) { return "story_" + w.Id + "_" + f; }

    public static bool IsCleared(StoryWeekData w) { return w != null && PlayerPrefs.GetInt(Key(w, "clear"), 0) == 1; }
    public static int GetBestScore(StoryWeekData w, ChartDifficulty d) { return PlayerPrefs.GetInt(Key(w, "best_" + (int)d), 0); }
    public static float GetClearRate(StoryWeekData w, ChartDifficulty d) { return PlayerPrefs.GetFloat(Key(w, "acc_" + (int)d), -1f); }

    public static void SaveWeek(StoryWeekData w, ChartDifficulty d, int totalScore, float accuracy)
    {
        PlayerPrefs.SetInt(Key(w, "clear"), 1);
        if (totalScore > GetBestScore(w, d)) PlayerPrefs.SetInt(Key(w, "best_" + (int)d), totalScore);
        if (accuracy > GetClearRate(w, d)) PlayerPrefs.SetFloat(Key(w, "acc_" + (int)d), accuracy);
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
    private static int accCount;

    public static int TrackCount { get { return Week != null && Week.tracks != null ? Week.tracks.Length : 0; } }
    public static bool HasNext { get { return Active && TrackIndex < TrackCount - 1; } }
    public static float AverageAccuracy { get { return accCount > 0 ? accSum / accCount : -1f; } }

    public static void Begin(StoryWeekData week)
    {
        Active = true; Week = week; TrackIndex = 0; TotalScore = 0; accSum = 0f; accCount = 0; pendingScore = 0; pendingAcc = -1f;
        LastCleared = false; WeekCompleted = false;
        GameSession.SelectedSong = week.tracks[0].song;
    }

    private static int pendingScore;
    private static float pendingAcc = -1f;

    // Goi khi 1 bai ket thuc. Diem chi duoc cong khi sang bai tiep (choi lai bai thi ghi de).
    public static void Report(bool cleared, int score, float accuracy)
    {
        if (!Active) return;
        LastCleared = cleared;
        if (!cleared) return;
        pendingScore = score; pendingAcc = accuracy;
        if (TrackIndex >= TrackCount - 1)
        {
            WeekCompleted = true;
            int total = TotalScore + pendingScore;
            float sum = accSum + (pendingAcc >= 0f ? pendingAcc : 0f);
            int cnt = accCount + (pendingAcc >= 0f ? 1 : 0);
            StoryProgress.SaveWeek(Week, GameSession.SelectedDifficulty, total, cnt > 0 ? sum / cnt : -1f);
        }
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

    public static void End() { Active = false; }
}
