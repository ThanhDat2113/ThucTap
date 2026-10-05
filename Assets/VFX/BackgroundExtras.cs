using UnityEngine;

/// <summary>
/// Mặt trời + hạt bụi sáng trôi lên, làm con của Camera.
/// Gắn vào Main Camera. Nếu Camera đang có script BackgroundExtras cũ thì thay bằng file này.
/// Đã sửa: mặt trời không còn nằm giữa (bị con đường che), kích thước tính theo % chiều cao màn hình.
/// </summary>
public class BackgroundExtras : MonoBehaviour
{
    [Header("Mặt trời")]
    public bool showSun = true;
    public Color sunTopColor = new Color(1f, 0.92f, 0.45f, 1f);
    public Color sunBottomColor = new Color(1f, 0.45f, 0.35f, 1f);
    public Color glowColor = new Color(1f, 0.6f, 0.5f, 0.45f);
    [Tooltip("Đường kính mặt trời = % chiều cao màn hình")]
    [Range(0.05f, 1f)] public float sunSize = 0.28f;
    [Tooltip("Lệch ngang: -1 = mép trái, 1 = mép phải, 0 = giữa (sẽ bị đường che!)")]
    [Range(-1f, 1f)] public float sunHorizontal = -0.7f;
    [Tooltip("Lệch dọc: -1 = đáy, 1 = đỉnh")]
    [Range(-1f, 1f)] public float sunVertical = 0.45f;
    public float sunDistance = 45f;

    [Header("Hạt bụi")]
    public bool showParticles = true;
    public int particleCount = 40;
    public Color particleColor = new Color(1f, 0.75f, 0.9f, 0.6f);
    [Tooltip("Kích thước hạt = % chiều cao màn hình")]
    public Vector2 particleSizeRange = new Vector2(0.008f, 0.025f);
    public Vector2 driftSpeedRange = new Vector2(0.5f, 2f);
    public float particleDistance = 42f;

    Camera cam;
    float viewHeight, viewWidth;
    Transform[] parts;
    float[] speeds;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundExtras: không tìm thấy Camera."); return; }

        if (showSun) BuildSun();
        if (showParticles) BuildParticles();
    }

    float ViewHeightAt(float d)
    {
        return cam.orthographic ? cam.orthographicSize * 2f
            : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    // ---------- Mặt trời ----------
    void BuildSun()
    {
        float h = ViewHeightAt(sunDistance);
        float w = h * cam.aspect;

        var go = new GameObject("Sun");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(sunHorizontal * w * 0.5f, sunVertical * h * 0.5f, sunDistance);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BuildSunSprite();
        sr.sortingOrder = -90;
        float target = h * sunSize * 1.8f; // sprite gồm cả vầng sáng (glow) nên to hơn đĩa 1.8 lần
        go.transform.localScale = Vector3.one * (target / sr.sprite.bounds.size.x);
    }

    Sprite BuildSunSprite()
    {
        int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float discR = 0.5f / 1.8f; // bán kính đĩa theo tỉ lệ texture (0..0.5)
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = x / (n - 1f) - 0.5f, v = y / (n - 1f) - 0.5f;
            float d = Mathf.Sqrt(u * u + v * v);
            Color c;
            if (d <= discR)
            {
                float t = (v + discR) / (2f * discR); // 0 = dưới, 1 = trên
                c = Color.Lerp(sunBottomColor, sunTopColor, t);
                c.a = Mathf.Clamp01((discR - d) * n * 0.5f); // mép mượt
            }
            else
            {
                float g = Mathf.Clamp01(1f - (d - discR) / (0.5f - discR));
                c = glowColor;
                c.a = glowColor.a * g * g;
            }
            tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 1f);
    }

    // ---------- Hạt bụi ----------
    void BuildParticles()
    {
        viewHeight = ViewHeightAt(particleDistance);
        viewWidth = viewHeight * cam.aspect;

        var sprite = BuildDotSprite();
        parts = new Transform[particleCount];
        speeds = new float[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            var go = new GameObject("Dust_" + i);
            go.transform.SetParent(cam.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -85;
            Color c = particleColor;
            c.a *= Random.Range(0.4f, 1f);
            sr.color = c;

            float size = viewHeight * Random.Range(particleSizeRange.x, particleSizeRange.y);
            go.transform.localScale = Vector3.one * (size / sprite.bounds.size.x);

            parts[i] = go.transform;
            speeds[i] = Random.Range(driftSpeedRange.x, driftSpeedRange.y);
            go.transform.localPosition = new Vector3(
                Random.Range(-viewWidth * 0.5f, viewWidth * 0.5f),
                Random.Range(-viewHeight * 0.5f, viewHeight * 0.5f),
                particleDistance);
        }
    }

    Sprite BuildDotSprite()
    {
        int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x / (n - 1f) - 0.5f) * 2f, v = (y / (n - 1f) - 0.5f) * 2f;
            float a = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 1f);
    }

    void Update()
    {
        if (parts == null) return;
        for (int i = 0; i < parts.Length; i++)
        {
            Vector3 p = parts[i].localPosition;
            p.y += speeds[i] * Time.deltaTime;
            if (p.y > viewHeight * 0.5f + 1f)
            {
                p.y = -viewHeight * 0.5f - 1f;
                p.x = Random.Range(-viewWidth * 0.5f, viewWidth * 0.5f);
            }
            parts[i].localPosition = p;
        }
    }
}