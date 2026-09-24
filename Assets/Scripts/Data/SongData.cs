using UnityEngine;

namespace RhythmGame.Data
{
    [CreateAssetMenu(menuName = "RhythmGame/Song Data", fileName = "NewSong")]
    public class SongData : ScriptableObject
    {
        public string songName = "Untitled";
        public float bpm = 150f;
        public float offset = 0f;
        public AudioClip track;
        public NoteData[] notes;
    }
}
