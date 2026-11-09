using UnityEngine;

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

    [Header("Thong tin hien o man gioi thieu bai (de trong thi hien ten ca si)")]
    public string lyricist;
    public string composer;
    public string arranger;

    [Tooltip("Giay bat dau doan nghe thu o man chon nhac (-1 = tu dong lay khoang 30% bai)")]
    public float previewStart = -1f;
}
