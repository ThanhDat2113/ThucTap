using UnityEngine;

/// <summary>
/// Mưa xiên nhiều lớp (giọt xa nhỏ + mờ + chậm, giọt gần to + rõ + nhanh) và sấm chớp thỉnh thoảng loé sáng.
/// Gắn vào Main Camera. Hợp với gradient preset "Rainy".
/// </summary>
public class BackgroundRain : MonoBehaviour
{
    [Header("Hạt mưa")]
    public int dropCount = 160;
    public Color dropColor = new Color(0.75f, 0.85f, 1f, 0.5f);
    [Tooltip("Độ dài vệt = % chiều cao màn hình")]
    public Vector2 lengthRange = new Vector2(0.04f, 0.11f);
    [Tooltip("Độ dày vệt = % chiều cao màn hình")]
    public Vector2 thicknessRange = new Vector2(0.0015f, 0.004f);
    [Tooltip("Tốc độ rơi = % chiều cao màn hình mỗi giây")]
    public Vector2 speedRange = new Vector2(1.2f, 2.4f);
    [Tooltip("Góc nghiêng (độ): dương = mưa xiên sang phải")]
    [Range(-45f, 45f)] public float angle = 12f;

    [Header("Sấm chớp")]
    public bool showLightning = true;
    public Color flashColor = new Color(0.8f, 0.86f, 1f, 1f);
    [Range(0f, 1f)] public float flashIntensity = 0.5f;
    public Vector2 flashIntervalRange = new Vector2(4f, 12f);

    public float distance = 35f;

    Camera cam;
    float viewHeight, viewWidth, margin;
    Transform[] drops;
    float[] speeds, lengths;
    Vector2 dir;

    SpriteRenderer flashSr;
    float flashTimer, flashT = -1f;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundRain: không tìm thấy Camera."); return; }

        viewHeight = ViewHeightAt(distance);
        viewWidth = viewHeight * cam.aspect;
        margin = viewHeight * 0.25f;

        float rad = angle * Mathf.Deg2Rad;
        dir = new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad));

        BuildDrops();
        if (showLightning) BuildFlash();
    }

    float ViewHeightAt(float d)
    {
        return cam.orthographic ? cam.orthographicSize * 2f
            : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    void BuildDrops()
    {
        var sprite = BuildStreakSprite();
        drops = new Transform[dropCount];
        speeds = new float[dropCount];
        lengths = new float[dropCount];

        for (int i = 0; i < dropCount; i++)
        {
            var go = new GameObject("Rain_" + i);
            go.transform.SetParent(cam.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            float t = Random.value; // 0 = xa, 1 = gần
            sr.sortingOrder = -83 + Mathf.RoundToInt(t * 2f);
            Color c = dropColor;
            c.a *= Mathf.Lerp(0.35f, 1f, t);
            sr.color = c;

            float len = viewHeight * Mathf.Lerp(lengthRange.x, lengthRange.y, t);
            float thick = viewHeight * Mathf.Lerp(thicknessRange.x, thicknessRange.y, t);
            go.transform.localScale = new Vector3(
                thick / sprite.bounds.size.x, len / sprite.bounds.size.y, 1f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            speeds[i] = viewHeight * Mathf.Lerp(speedRange.x, speedRange.y, t);
            lengths[i] = len;
            drops[i] = go.transform;
            go.transform.localPosition = new Vector3(
                Random.Range(-viewWidth * 0.5f - margin, viewWidth * 0.5f + margin),
                Random.Range(-viewHeight * 0.5f, viewHeight * 0.5f),
                distance);
        }
    }

    // Đầu giọt (sáng) ở dưới, đuôi mờ dần lên trên
    Sprite BuildStreakSprite()
    {
        int w = 4, h = 32;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float u = Mathf.Abs((x / (w - 1f) - 0.5f) * 2f);
            float along = Mathf.Pow(1f - y / (h - 1f), 1.5f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, along * (1f - u * u)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f);
    }

    void BuildFlash()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();

        var go = new GameObject("LightningFlash");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, distance);
        go.transform.localScale = new Vector3(viewWidth * 1.3f / 4f, viewHeight * 1.3f / 4f, 1f);
        flashSr = go.AddComponent<SpriteRenderer>();
        flashSr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1f);
        flashSr.sortingOrder = -80;
        flashSr.color = Color.clear;
        flashTimer = Random.Range(flashIntervalRange.x, flashIntervalRange.y);
    }

    void Update()
    {
        if (drops == null) return;

        float halfW = viewWidth * 0.5f + margin, halfH = viewHeight * 0.5f;
        for (int i = 0; i < drops.Length; i++)
        {
            Vector3 p = drops[i].localPosition;
            p.x += dir.x * speeds[i] * Time.deltaTime;
            p.y += dir.y * speeds[i] * Time.deltaTime;
            if (p.y < -halfH - lengths[i])
            {
                p.y = halfH + lengths[i];
                p.x = Random.Range(-halfW, halfW);
            }
            if (p.x > halfW) p.x -= halfW * 2f;
            else if (p.x < -halfW) p.x += halfW * 2f;
            drops[i].localPosition = p;
        }

        if (flashSr == null) return;
        if (flashT < 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f) flashT = 0f;
            return;
        }

        // chớp đôi: một tia mạnh rồi một tia yếu hơn
        flashT += Time.deltaTime;
        float a = Mathf.Exp(-flashT * 14f);
        if (flashT > 0.18f) a = Mathf.Max(a, 0.7f * Mathf.Exp(-(flashT - 0.18f) * 10f));
        Color fc = flashColor;
        fc.a = a * flashIntensity;
        flashSr.color = fc;

        if (flashT > 0.8f)
        {
            flashT = -1f;
            flashSr.color = Color.clear;
            flashTimer = Random.Range(flashIntervalRange.x, flashIntervalRange.y);
        }
    }
}
