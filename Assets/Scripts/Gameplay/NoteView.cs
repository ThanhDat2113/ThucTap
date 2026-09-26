using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Gắn lên prefab Note. Chỉ chịu trách nhiệm hiển thị + di chuyển,
    /// không biết gì về input, judgement hay lane logic.
    /// Khi có art thật: thay SpriteRenderer bằng sprite đẹp, thêm Animator
    /// và gọi Animator.Play(...) trong PlayHitVisual()/PlayMissVisual().
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class NoteView : MonoBehaviour
    {
        public NoteData Data { get; private set; }
        public bool Judged { get; private set; }

        SpriteRenderer sr;
        Color baseColor;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        /// <summary>Gọi khi note được lấy ra từ pool để dùng lại.</summary>
        public void Setup(NoteData data, Color color)
        {
            Data = data;
            Judged = false;
            color.a = 1f; // luôn hiện rõ khi spawn, phòng trường hợp laneColors bị thiếu Alpha trong Inspector
            baseColor = color;
            sr.color = color;
        }

        /// <summary>Cập nhật vị trí dựa trên đồng hồ bài hát, không dựa vào Time.deltaTime cộng dồn.</summary>
        public void UpdatePosition(float songTime, float laneX, float hitLineY, float scrollSpeed)
        {
            float y = hitLineY + (Data.time - songTime) * scrollSpeed;
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
