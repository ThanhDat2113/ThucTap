using UnityEngine;

namespace RhythmGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class NoteView : MonoBehaviour
    {
        [Header("Hold note")]
        [Tooltip("Thân của hold note. Để trống thì tự tạo từ sprite của đầu note.")]
        public SpriteRenderer body;
        [Range(0.1f, 1f)] public float bodyWidthRatio = 0.5f;
        [Range(0f, 1f)] public float bodyAlpha = 0.6f;
        [Tooltip("Sorting order của thân so với đầu note.")]
        public int bodySortingOffset = 0;

        public NoteData Data { get; private set; }
        public float TimeSeconds { get; private set; }
        public float HoldSeconds { get; private set; }
        public float TailTimeSeconds => TimeSeconds + HoldSeconds;
        public bool IsHold => HoldSeconds > 0f;
        public bool Judged { get; private set; }
        public bool Holding { get; private set; }
        public float TopY { get; private set; }

        SpriteRenderer sr;
        Color baseColor;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        public void Setup(NoteData data, float timeSeconds, float holdSeconds, Color color)
        {
            Data = data;
            TimeSeconds = timeSeconds;
            HoldSeconds = holdSeconds;
            Judged = false;
            Holding = false;
            color.a = 1f;
            baseColor = color;
            sr.color = color;

            if (IsHold)
            {
                EnsureBody();
                body.gameObject.SetActive(true);
                SetBodyColor(bodyAlpha);
            }
            else if (body != null)
            {
                body.gameObject.SetActive(false);
            }
        }

        public void UpdatePosition(float songTime, float laneX, float hitLineY, float scrollSpeed)
        {
            float headY = hitLineY + (TimeSeconds - songTime) * scrollSpeed;
            if (Holding) headY = hitLineY;
            transform.position = new Vector3(laneX, headY, 0f);

            TopY = headY;
            if (IsHold)
            {
                float tailY = hitLineY + (TailTimeSeconds - songTime) * scrollSpeed;
                TopY = Mathf.Max(headY, tailY);
                UpdateBody(headY, tailY);
            }
        }

        public void MarkJudged() => Judged = true;

        public void BeginHold()
        {
            Holding = true;
            SetBodyColor(0.9f);
        }

        public void BreakHold()
        {
            Holding = false;
            PlayMissVisual();
        }

        public void PlayMissVisual()
        {
            Color c = baseColor * 0.5f;
            c.a = 0.5f;
            sr.color = c;

            if (IsHold && body != null)
            {
                Color b = baseColor * 0.5f;
                b.a = 0.3f;
                body.color = b;
            }
        }

        void EnsureBody()
        {
            if (body != null) return;

            var go = new GameObject("HoldBody");
            go.transform.SetParent(transform, false);
            body = go.AddComponent<SpriteRenderer>();
            body.sprite = sr.sprite;
            body.sortingLayerID = sr.sortingLayerID;
            body.sortingOrder = sr.sortingOrder + bodySortingOffset;
        }

        void SetBodyColor(float alpha)
        {
            if (body == null) return;
            body.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        void UpdateBody(float headY, float tailY)
        {
            float length = Mathf.Max(0f, tailY - headY);
            float parentScaleY = Mathf.Max(transform.lossyScale.y, 0.0001f);

            Vector2 headSize = sr.sprite != null ? (Vector2)sr.sprite.bounds.size : Vector2.one;
            Vector2 bodySize = body.sprite != null ? (Vector2)body.sprite.bounds.size : Vector2.one;

            float sx = headSize.x * bodyWidthRatio / Mathf.Max(bodySize.x, 0.0001f);
            float sy = length / (parentScaleY * Mathf.Max(bodySize.y, 0.0001f));

            body.transform.localScale = new Vector3(sx, sy, 1f);
            body.transform.localPosition = new Vector3(0f, (length * 0.5f) / parentScaleY, 0f);
        }
    }
}