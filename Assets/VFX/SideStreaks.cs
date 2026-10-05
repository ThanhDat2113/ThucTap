using UnityEngine;

/// <summary>
/// Các vệt sáng mảnh, chạy chéo, CHỈ ở 2 dải bên trái/phải màn hình.
/// Gắn vào Main Camera (cùng WorldBackgroundGradient).
/// Đã sửa: bề dày/độ dài tính theo kích thước thật của sprite (trước đây mỏng < 1 pixel).
/// </summary>
public class SideStreaks : MonoBehaviour
{
    [Header("Số lượng & màu")]
    public int count = 24;
    public Color tint = new Color(1f, 1f, 1f, 0.5f);

    [Header("Vùng né ở giữa (0 = không né, 0.9 = né gần hết)")]
    [Range(0f, 0.9f)] public float centerGapRatio = 0.42f;

    [Header("Hình dạng vệt (đơn vị world, ở khoảng cách 'distance')")]
    public float angleDeg = 14f;
    public Vector2 lengthRange = new Vector2(2f, 6f);
    [Tooltip("Bề dày thật. ~0.04 ≈ 1 pixel ở 1080p. Nên để 0.08 - 0.15 cho dễ thấy")]
    public float thickness = 0.1f;
    public Vector2 speedRange = new Vector2(6f, 14f);

    [Header("Vị trí")]
    public float distance = 40f;

    Camera cam;
    Transform[] streaks;
    float[] speeds;
    float viewHeight, viewWidth, gapHalfWidth;
    Sprite sprite;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("SideStreaks: không tìm thấy Camera."); return; }

        if (cam.orthographic) viewHeight = cam.orthographicSize * 2f;
        else viewHeight = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        viewWidth = viewHeight * cam.aspect;
        gapHalfWidth = viewWidth * 0.5f * centerGapRatio;

        sprite = BuildLineSprite();
        streaks = new Transform[count];
        speeds = new float[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Streak_" + i);
            go.transform.SetParent(cam.transform, false);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -80;
            Color c = tint;
            c.a *= Random.Range(0.5f, 1f);
            sr.color = c;

            streaks[i] = go.transform;
            speeds[i] = Random.Range(speedRange.x, speedRange.y);
            Respawn(i, true);
        }
    }

    void Respawn(int i, bool anywhere)
    {
        bool rightSide = Random.value > 0.5f;
        float edge = viewWidth * 0.5f;
        float x = rightSide
            ? Random.Range(gapHalfWidth, edge)
            : Random.Range(-edge, -gapHalfWidth);

        float y = anywhere
            ? Random.Range(-viewHeight * 0.5f, viewHeight * 0.5f)
            : viewHeight * 0.5f + 2f;

        streaks[i].localPosition = new Vector3(x, y, distance);
        streaks[i].localRotation = Quaternion.Euler(0f, 0f, angleDeg);

        // Sprite có bounds.size = (w, h) world unit ở scale 1 -> chia ra để ra đúng kích thước mong muốn
        float len = Random.Range(lengthRange.x, lengthRange.y);
        Vector3 size = sprite.bounds.size;
        streaks[i].localScale = new Vector3(thickness / size.x, len / size.y, 1f);
    }

    void Update()
    {
        if (streaks == null) return;
        Vector3 dir = Quaternion.Euler(0f, 0f, angleDeg) * Vector3.down;
        float edge = viewWidth * 0.5f;

        for (int i = 0; i < streaks.Length; i++)
        {
            streaks[i].localPosition += dir * (speeds[i] * Time.deltaTime);
            Vector3 p = streaks[i].localPosition;

            bool below = p.y < -viewHeight * 0.5f - 3f;
            bool inGap = Mathf.Abs(p.x) < gapHalfWidth;       // trôi vào vùng giữa
            bool outside = Mathf.Abs(p.x) > edge + 2f;        // trôi ra khỏi mép
            if (below || inGap || outside) Respawn(i, false);
        }
    }

    Sprite BuildLineSprite()
    {
        var tex = new Texture2D(4, 64, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 64; y++)
        {
            float t = Mathf.Abs((y / 63f) - 0.5f) * 2f;
            float a = 1f - Mathf.Pow(t, 2f);
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        // PPU = 1 -> sprite rộng 4 unit, cao 64 unit; sẽ được scale lại theo bounds.size
        return Sprite.Create(tex, new Rect(0, 0, 4, 64), new Vector2(0.5f, 0.5f), 1f);
    }
}