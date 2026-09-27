using UnityEngine;

namespace RhythmGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class NoteView : MonoBehaviour
    {
        public NoteData Data { get; private set; }
        public float TimeSeconds { get; private set; }
        public bool Judged { get; private set; }

        SpriteRenderer sr;
        Color baseColor;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        public void Setup(NoteData data, float timeSeconds, Color color)
        {
            Data = data;
            TimeSeconds = timeSeconds;
            Judged = false;
            color.a = 1f;
            baseColor = color;
            sr.color = color;
        }

        public void UpdatePosition(float songTime, float laneX, float hitLineY, float scrollSpeed)
        {
            float y = hitLineY + (TimeSeconds - songTime) * scrollSpeed;
            transform.position = new Vector3(laneX, y, 0f);
        }

        public void MarkJudged() => Judged = true;

        public void PlayMissVisual()
        {
            Color c = baseColor * 0.5f;
            c.a = 0.5f;
            sr.color = c;
        }
    }
}