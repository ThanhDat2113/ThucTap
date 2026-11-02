using UnityEngine;

/// <summary>
/// Đom đóm: bay lượn ngẫu nhiên (Perlin noise) và sáng tắt nhịp nhàng.
/// Gắn vào Main Camera. Kích thước tính theo % chiều cao màn hình.
/// </summary>
public class BackgroundFireflies : MonoBehaviour
{
    [Header("Đom đóm")]
    public int fireflyCount = 28;
    public Color coreColor = new Color(1f, 1f, 0.6f, 1f);
    public Color glowColor = new Color(0.7f, 1f, 0.3f, 0.5f);
    [Tooltip("Kích thước = % chiều cao màn hình")]
    public Vector2 sizeRange = new Vector2(0.015f, 0.04f);

    [Header("Chuyển động")]
    [Tooltip("Tầm bay quanh vị trí gốc = % kích thước màn hình")]
    public Vector2 wanderRange = new Vector2(0.12f, 0.18f);
    public Vector2 wanderSpeedRange = new Vector2(0.1f, 0.35f);

    [Header("Nhấp nháy")]
    public Vector2 pulseSpeedRange = new Vector2(0.6f, 1.8f);
    [Tooltip("Càng cao càng có khoảng tối rõ giữa các lần sáng")]
    [Range(1f, 6f)] public float pulseSharpness = 2.5f;

    public float fireflyDistance = 38f;

    Camera cam;
    float viewHeight, viewWidth;
    Transform[] flies;
    SpriteRenderer[] renderers;
    Vector2[] origins;
    float[] seeds, wanderSpeeds, pulseSpeeds, pulsePhases, baseAlpha;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundFireflies: không tìm thấy Camera."); return; }
        Build();
    }

    float ViewHeightAt(float d)
    {
        return cam.orthographic ? cam.orthographicSize * 2f
            : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    void Build()
    {
        viewHeight = ViewHeightAt(fireflyDistance);
        viewWidth = viewHeight * cam.aspect;

        var sprite = BuildGlowSprite();
        flies = new Transform[fireflyCount];
        renderers = new SpriteRenderer[fireflyCount];
        origins = new Vector2[fireflyCount];
        seeds = new float[fireflyCount];
        wanderSpeeds = new float[fireflyCount];
        pulseSpeeds = new float[fireflyCount];
        pulsePhases = new float[fireflyCount];
        baseAlpha = new float[fireflyCount];

        for (int i = 0; i < fireflyCount; i++)
        {
            var go = new GameObject("Firefly_" + i);
            go.transform.SetParent(cam.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -86;

            float size = viewHeight * Random.Range(sizeRange.x, sizeRange.y);
            go.transform.localScale = Vector3.one * (size / sprite.bounds.size.x);

            origins[i] = new Vector2(
                Random.Range(-viewWidth * 0.5f, viewWidth * 0.5f),
                Random.Range(-viewHeight * 0.5f, viewHeight * 0.5f));
            seeds[i] = Random.Range(0f, 1000f);
            wanderSpeeds[i] = Random.Range(wanderSpeedRange.x, wanderSpeedRange.y);
            pulseSpeeds[i] = Random.Range(pulseSpeedRange.x, pulseSpeedRange.y);
            pulsePhases[i] = Random.Range(0f, Mathf.PI * 2f);
            baseAlpha[i] = Random.Range(0.6f, 1f);

            flies[i] = go.transform;
            renderers[i] = sr;
            go.transform.localPosition = new Vector3(origins[i].x, origins[i].y, fireflyDistance);
        }
    }

    // Lõi sáng đặc + vầng sáng mềm bao quanh (màu lõi pha sang màu glow ở rìa)
    Sprite BuildGlowSprite()
    {
        int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x / (n - 1f) - 0.5f) * 2f, v = (y / (n - 1f) - 0.5f) * 2f;
            float d = Mathf.Clamp01(Mathf.Sqrt(u * u + v * v));
            float core = Mathf.Clamp01(1f - d / 0.25f);          // lõi nhỏ ở giữa
            float glow = (1f - d) * (1f - d);                    // vầng sáng
            Color c = Color.Lerp(glowColor, coreColor, core);
            c.a = Mathf.Clamp01(Mathf.Max(core * coreColor.a, glow * glowColor.a));
            tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 1f);
    }

    void Update()
    {
        if (flies == null) return;
        float t = Time.time;
        float rx = viewWidth * wanderRange.x;
        float ry = viewHeight * wanderRange.y;

        for (int i = 0; i < flies.Length; i++)
        {
            float tt = t * wanderSpeeds[i];
            float ox = (Mathf.PerlinNoise(seeds[i], tt) - 0.5f) * 2f * rx;
            float oy = (Mathf.PerlinNoise(seeds[i] + 100f, tt) - 0.5f) * 2f * ry;
            flies[i].localPosition = new Vector3(origins[i].x + ox, origins[i].y + oy, fireflyDistance);

            float pulse = 0.5f + 0.5f * Mathf.Sin(t * pulseSpeeds[i] + pulsePhases[i]);
            pulse = Mathf.Pow(pulse, pulseSharpness);
            renderers[i].color = new Color(1f, 1f, 1f, baseAlpha[i] * pulse);
        }
    }
}
