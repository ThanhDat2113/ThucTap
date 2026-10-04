using UnityEngine;

// Thong ke tung bai, luu bang PlayerPrefs. Gameplay goi SaveResult() khi choi xong.
public static class SongStats
{
    private static string Key(SongData song, string field) { return "stats_" + song.name + "_" + field; }

    public static int GetBestScore(SongData song) { return PlayerPrefs.GetInt(Key(song, "best"), 0); }
    public static int GetBestCombo(SongData song) { return PlayerPrefs.GetInt(Key(song, "combo"), 0); }
    public static float GetBestAccuracy(SongData song) { return PlayerPrefs.GetFloat(Key(song, "acc"), -1f); }
    public static int GetPlayCount(SongData song) { return PlayerPrefs.GetInt(Key(song, "plays"), 0); }

    public static void IncrementPlayCount(SongData song)
    {
        PlayerPrefs.SetInt(Key(song, "plays"), GetPlayCount(song) + 1);
        PlayerPrefs.Save();
    }

    public static void SaveResult(SongData song, int score, int maxCombo, float accuracy)
    {
        if (score > GetBestScore(song))
        {
            PlayerPrefs.SetInt(Key(song, "best"), score);
            PlayerPrefs.SetInt(Key(song, "combo"), maxCombo);
        }
        if (accuracy > GetBestAccuracy(song))
            PlayerPrefs.SetFloat(Key(song, "acc"), accuracy);
        PlayerPrefs.Save();
    }
}
