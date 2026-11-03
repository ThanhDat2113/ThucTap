using UnityEngine;

// Thong ke tung bai THEO TUNG DO KHO (Easy/Normal/Hard luu rieng), luu bang PlayerPrefs.
// Khong truyen do kho -> dung do kho dang chon (GameSession.SelectedDifficulty).
public static class SongStats
{
    private static string Key(SongData song, ChartDifficulty d, string field) { return "stats_" + song.name + "_" + (int)d + "_" + field; }
    // Du lieu cu (truoc khi tach do kho) duoc tinh la Normal
    private static string LegacyKey(SongData song, string field) { return "stats_" + song.name + "_" + field; }

    private static int GetInt(SongData song, ChartDifficulty d, string field, int def)
    {
        int legacy = d == ChartDifficulty.Normal ? PlayerPrefs.GetInt(LegacyKey(song, field), def) : def;
        return PlayerPrefs.GetInt(Key(song, d, field), legacy);
    }

    private static float GetFloat(SongData song, ChartDifficulty d, string field, float def)
    {
        float legacy = d == ChartDifficulty.Normal ? PlayerPrefs.GetFloat(LegacyKey(song, field), def) : def;
        return PlayerPrefs.GetFloat(Key(song, d, field), legacy);
    }

    private static ChartDifficulty Current { get { return GameSession.SelectedDifficulty; } }

    public static int GetBestScore(SongData song, ChartDifficulty d) { return GetInt(song, d, "best", 0); }
    public static int GetBestCombo(SongData song, ChartDifficulty d) { return GetInt(song, d, "combo", 0); }
    public static float GetBestAccuracy(SongData song, ChartDifficulty d) { return GetFloat(song, d, "acc", -1f); }
    public static int GetPlayCount(SongData song, ChartDifficulty d) { return GetInt(song, d, "plays", 0); }

    public static int GetBestScore(SongData song) { return GetBestScore(song, Current); }
    public static int GetBestCombo(SongData song) { return GetBestCombo(song, Current); }
    public static float GetBestAccuracy(SongData song) { return GetBestAccuracy(song, Current); }
    public static int GetPlayCount(SongData song) { return GetPlayCount(song, Current); }

    public static void IncrementPlayCount(SongData song)
    {
        var d = Current;
        PlayerPrefs.SetInt(Key(song, d, "plays"), GetPlayCount(song, d) + 1);
        PlayerPrefs.Save();
    }

    public static void SaveResult(SongData song, int score, int maxCombo, float accuracy)
    {
        var d = Current;
        if (score > GetBestScore(song, d))
        {
            PlayerPrefs.SetInt(Key(song, d, "best"), score);
            PlayerPrefs.SetInt(Key(song, d, "combo"), maxCombo);
        }
        if (accuracy > GetBestAccuracy(song, d))
            PlayerPrefs.SetFloat(Key(song, d, "acc"), accuracy);
        PlayerPrefs.Save();
    }
}
