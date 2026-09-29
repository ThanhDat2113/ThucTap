using UnityEngine;

[CreateAssetMenu(fileName = "NewSong", menuName = "Music/Song")]
public class SongData : ScriptableObject
{
    public string songName;
    public AudioClip clip;
    public Sprite coverArt;
    [Range(1, 5)] public int difficulty = 3;
}