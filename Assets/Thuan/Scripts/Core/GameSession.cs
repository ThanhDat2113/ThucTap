using UnityEngine;

public enum ChartDifficulty { Easy = 0, Normal = 1, Hard = 2 }

// Luu lua chon cua nguoi choi de man Gameplay doc lai
public static class GameSession
{
    private const string DifficultyKey = "SelectedDifficulty";

    public static SongData SelectedSong { get; set; }

    public static ChartDifficulty SelectedDifficulty
    {
        get { return (ChartDifficulty)PlayerPrefs.GetInt(DifficultyKey, (int)ChartDifficulty.Normal); }
        set { PlayerPrefs.SetInt(DifficultyKey, (int)value); }
    }
}
