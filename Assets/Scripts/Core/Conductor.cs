using UnityEngine;
using RhythmGame.Data;

namespace RhythmGame.Core
{
    [DisallowMultipleComponent]
    public class Conductor : MonoBehaviour
    {
        public static Conductor Instance { get; private set; }

        public SongData Song { get; private set; }
        public AudioSource AudioSourceRef { get; private set; }

        public float CurrentSongPosition { get; private set; }
        public bool IsPlaying { get; private set; }

        private double dspTimeOfStart;
        private bool hasStarted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            AudioSourceRef = GetComponent<AudioSource>();
            if (AudioSourceRef == null)
                AudioSourceRef = gameObject.AddComponent<AudioSource>();
            AudioSourceRef.playOnAwake = false;
            AudioSourceRef.spatialBlend = 0f;
        }

        public void LoadSong(SongData songData)
        {
            Song = songData;
            AudioSourceRef.clip = songData != null ? songData.track : null;
        }

        public void StartSong()
        {
            if (Song == null)
                return;

            if (Song.track != null)
            {
                AudioSourceRef.clip = Song.track;
                AudioSourceRef.Play();
            }

            dspTimeOfStart = AudioSettings.dspTime;
            hasStarted = true;
            IsPlaying = true;
        }

        public void StopSong()
        {
            AudioSourceRef.Stop();
            IsPlaying = false;
            hasStarted = false;
        }

        private void Update()
        {
            if (!hasStarted)
                return;

            double offsetSeconds = Song != null ? Song.offset : 0f;
            CurrentSongPosition = (float)(AudioSettings.dspTime - dspTimeOfStart) + (float)offsetSeconds;
            if (CurrentSongPosition < 0f)
                CurrentSongPosition = 0f;
        }

        public float BeatToSeconds(float beat)
        {
            if (Song == null)
                return 0f;
            return (beat * 60f) / Song.bpm;
        }

        public float SecondsToBeat(float seconds)
        {
            if (Song == null || Song.bpm <= 0f)
                return 0f;
            return (seconds * Song.bpm) / 60f;
        }

        public float GetStepDuration()
        {
            return Song != null && Song.bpm > 0f ? 60f / Song.bpm : 0f;
        }
    }
}
