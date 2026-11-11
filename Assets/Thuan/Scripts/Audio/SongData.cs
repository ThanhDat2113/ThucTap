using UnityEngine;
using RhythmGame;

[CreateAssetMenu(fileName = "NewSong", menuName = "Music/Song")]
public class SongData : ScriptableObject
{
    public string songName;
    public string artist;
    public AudioClip clip;
    public Sprite coverArt;
    [Range(1, 5)] public int difficulty = 3;
    [Tooltip("0 = chua biet, se hien --")]
    public int bpm;

    [Header("Chart data theo do kho")]
    [Tooltip("Chart cho Easy. Neu trong bo se fallback sang chart cu (chart).")]
    public ChartData easyChart;
    [Tooltip("Chart cho Normal. Neu trong bo se fallback sang chart cu (chart).")]
    public ChartData normalChart;
    [Tooltip("Chart cho Hard. Neu trong bo se fallback sang chart cu (chart).")]
    public ChartData hardChart;

    [Header("Chart cu (fallback neu chua chuyen du lieu sang 3 o tren)")]
    public ChartData chart;

    [Header("Thong tin hien o man gioi thieu bai (de trong thi hien ten ca si)")]
    public string lyricist;
    public string composer;
    public string arranger;

    /// <summary>
    /// Lay ChartData phu hop voi do kho hien tai (GameSession.SelectedDifficulty).
    /// Neu chua gan chart cho do kho do, fallback ve chart cu (de khong gap loi o cac asset cu).
    /// </summary>
    public ChartData GetChartForDifficulty(ChartDifficulty d)
    {
        switch (d)
        {
            case ChartDifficulty.Easy:
                return easyChart != null ? easyChart : chart;
            case ChartDifficulty.Normal:
                return normalChart != null ? normalChart : chart;
            case ChartDifficulty.Hard:
                return hardChart != null ? hardChart : chart;
            default:
                return chart;
        }
    }
}
