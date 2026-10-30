using System;
using UnityEngine;

[Serializable]
public class StoryTrack
{
    public string title;
    [Tooltip("De trong = bai chua co -> tuan hien COMING SOON")]
    public SongData song;

    public string DisplayTitle { get { return song != null && !string.IsNullOrEmpty(song.songName) ? song.songName : title; } }
}

// 1 tuan trong Story Mode: 3 bai choi lien tiep
[CreateAssetMenu(fileName = "Week", menuName = "Thuan/Story Week")]
public class StoryWeekData : ScriptableObject
{
    public int weekNumber = 1;
    public string title = "CONCRETE DREAMS";
    [TextArea(3, 5)] public string description;

    [Header("Hinh")]
    public Sprite cardNormal;
    public Sprite cardSelected;
    public Sprite detailArt;

    [Header("Doi thu tren san khau: 0 = Graffiti King, 1 = DJ Null")]
    public int opponent;

    [Header("Bai hat")]
    public StoryTrack[] tracks = new StoryTrack[3];

    public string Id { get { return name; } }

    // Co du bai hat de choi chua
    public bool Playable
    {
        get
        {
            if (tracks == null || tracks.Length == 0) return false;
            foreach (var t in tracks) if (t == null || t.song == null) return false;
            return true;
        }
    }
}
