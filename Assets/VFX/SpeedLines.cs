using UnityEngine;

/// <summary>
/// Các vệt sáng mảnh chạy chéo ở nền (như trong ảnh tham chiếu).
/// Gán 1 sprite hình chữ nhật trắng (ví dụ sprite "Square" mặc định của Unity, hoặc sprite có đầu mờ).
/// Đặt object này phía sau lane (Sorting Order thấp) nhưng trên ảnh nền.
/// </summary>
public class SpeedLines : MonoBehaviour
{
    public Sprite lineSprite;
    public int count = 30;
    public Vector2 area = new Vector2(22f, 12f);        // vùng hiển thị (world units)
    public float angleDeg = 12f;                        // vệt nghiêng, hướng lên bên phải
    public Vector2 speedRange = new Vector2(6f, 16f);
    public Vector2 lengthRange = new Vector2(1.5f, 5f);
    public float thickness = 0.04f;
    public Vector2 alphaRange = new Vector2(0.15f, 0.5f);
    public Color tint = Color.white;
    public int sortingOrder = -10;

    [Tooltip("Nhân tốc độ khi có beat mạnh; chỉnh từ script khác nếu muốn")]
    public float speedMultiplier = 1f;

    Transform[] lines;
    float[] speeds;
    Vector2 half;

    void Start()
    {
        half = area * 0.5f;
        lines = new Transform[count];
        speeds = new float[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Streak");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = lineSprite;
            sr.sortingOrder = sortingOrder;
            sr.color = new Color(tint.r, tint.g, tint.b, Random.Range(alphaRange.x, alphaRange.y));
            lines[i] = go.transform;
            Respawn(i, true);
        }
    }

    void Respawn(int i, bool anywhere)
    {
        float x = anywhere ? Random.Range(-half.x, half.x) : half.x + 1f;
        float y = Random.Range(-half.y, half.y);
        lines[i].localPosition = new Vector3(x, y, 0f);
        lines[i].localRotation = Quaternion.Euler(0f, 0f, angleDeg);
        lines[i].localScale = new Vector3(Random.Range(lengthRange.x, lengthRange.y), thickness, 1f);
        speeds[i] = Random.Range(speedRange.x, speedRange.y);
    }

    void Update()
    {
        // Bay dọc theo trục của vệt, xuống-trái
        Vector3 dir = Quaternion.Euler(0f, 0f, angleDeg) * Vector3.left;

        for (int i = 0; i < count; i++)
        {
            lines[i].localPosition += dir * (speeds[i] * speedMultiplier * Time.deltaTime);
            Vector3 p = lines[i].localPosition;
            if (p.x < -half.x - 6f || p.y < -half.y - 2f || p.y > half.y + 2f)
                Respawn(i, false);
        }
    }
}
