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
}
