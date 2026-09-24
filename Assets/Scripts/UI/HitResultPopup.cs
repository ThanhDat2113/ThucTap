using RhythmGame.Core;
using UnityEngine;

namespace RhythmGame.UI
{
    [DisallowMultipleComponent]
    public class HitResultPopup : MonoBehaviour
    {
        private const float Duration = 0.45f;
        private const float RiseAmount = 1.6f;
        private const float ScaleOvershoot = 1.3f;

        private SpriteRenderer spriteRenderer;
        private Vector3 startPosition;
        private float startTime;

        private void Awake()
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = SpriteFactory.WhiteSquare;
            spriteRenderer.sortingOrder = 10;
            startPosition = transform.position;
            startTime = Time.time;
        }

        public void Configure(Judgement judgement)
        {
            spriteRenderer.color = ColorFor(judgement);
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = new Vector2(1.2f, 1.2f);
        }

        private void Update()
        {
            float t = (Time.time - startTime) / Duration;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            float height = Mathf.Sin(t * Mathf.PI) * ScaleOvershoot;
            transform.localScale = Vector3.one * Mathf.Lerp(1f, height, t);
            transform.position = startPosition + Vector3.up * (t * RiseAmount);

            Color c = spriteRenderer.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            spriteRenderer.color = c;
        }

        private static Color ColorFor(Judgement j)
        {
            switch (j)
            {
                case Judgement.Perfect: return new Color(0.2f, 1f, 0.4f);
                case Judgement.Good: return new Color(1f, 0.85f, 0.2f);
                case Judgement.Miss: return new Color(1f, 0.25f, 0.25f);
                default: return Color.white;
            }
        }
    }
}
