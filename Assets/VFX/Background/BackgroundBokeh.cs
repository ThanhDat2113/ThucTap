using UnityEngine;

/// <summary>
/// Bokeh mơ màng: các vòng sáng mềm có viền, trôi lên chậm rãi, phập phồng như ánh đèn mờ.
/// Vòng to = gần = mờ hơn và trôi nhanh hơn. Gắn vào Main Camera.
/// Hợp với gradient preset "Dream", "Synthwave" hoặc "Night".
/// </summary>
public class BackgroundBokeh : MonoBehaviour
{
    [Header("Vòng sáng")]
    public int count = 22;
    public Color[] palette =
    {
        new Color(1f, 0.5f, 0.8f, 1f),
        new Color(0.7f, 0.4f, 1f, 1f),
        new Color(0.4f, 0.85f, 1f, 1f),
        new Color(1f, 0.85f, 0.5f, 1f)
    };
    [Tooltip("Độ trong suốt (nhỏ nhất, lớn nhất)")]
    public Vector2 alphaRange = new Vector2(0.12f, 0.35f);
    [Tooltip("Đường kính = % chiều cao màn hình")]
    public Vector2 sizeRange = new Vector2(0.06f, 0.28f);

    [Header("Chuyển động")]
    [Tooltip("Tốc độ trôi lên (world units / giây)")]
    public Vector2 driftSpeedRange = new Vector2(0.1f, 0.5f);
    [Tooltip("Biên độ lắc ngang = % chiều rộng màn hình")]
    public float swayAmount = 0.03f;
    public Vector2 pulseSpeedRange = new Vector2(0.4f, 1.2f);

    public float distance = 43f;

    Camera cam;
    float viewHeight, viewWidth;
    Transform[] orbs;
    SpriteRenderer[] renderers;
    float[] baseX, drift, swaySpeed, phase, pulseSpeed, baseAlpha, baseSize;
    Color[] baseColor;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundBokeh: không tìm thấy Camera."); return; }
        Build();
    }

    float ViewHeightAt(float d)
    {
        return cam.orthographic ? cam.orthographicSize * 2f
            : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    void Build()
    {
        viewHeight = ViewHeightAt(distance);
        viewWidth = viewHeight * cam.aspect;
        if (palette == null || palette.Length == 0) palette = new[] { Color.white };

        var sprite = BuildOrbSprite();
        orbs = new Transform[count];
        renderers = new SpriteRenderer[count];
        baseX = new float[count]; drift = new float[count]; swaySpeed = new float[count];
        phase = new float[count]; pulseSpeed = new float[count];
        baseAlpha = new float[count]; baseSize = new float[count];
        baseColor = new Color[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Bokeh_" + i);
            go.transform.SetParent(cam.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            float t = Random.value; // 0 = nhỏ/xa, 1 = to/gần
            sr.sortingOrder = -87;
            baseColor[i] = palette[Random.Range(0, palette.Length)];
            baseAlpha[i] = Mathf.Lerp(alphaRange.y, alphaRange.x, t); // to thì mờ hơn
            baseSize[i] = viewHeight * Mathf.Lerp(sizeRange.x, sizeRange.y, t) / sprite.bounds.size.x;

            drift[i] = Mathf.Lerp(driftSpeedRange.x, driftSpeedRange.y, t);
            swaySpeed[i] = Random.Range(0.2f, 0.6f);
            phase[i] = Random.Range(0f, Mathf.PI * 2f);
            pulseSpeed[i] = Random.Range(pulseSpeedRange.x, pulseSpeedRange.y);
            baseX[i] = Random.Range(-viewWidth * 0.5f, viewWidth * 0.5f);

            renderers[i] = sr;
            orbs[i] = go.transform;
            go.transform.localPosition = new Vector3(baseX[i],
                Random.Range(-viewHeight * 0.5f, viewHeight * 0.5f), distance);
            go.transform.localScale = Vector3.one * baseSize[i];
        }
    }

    // Đĩa mờ nhẹ + viền sáng rõ hơn ở mép, giống ống kính bokeh
    Sprite BuildOrbSprite()
    {
        int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x / (n - 1f) - 0.5f) * 2f, v = (y / (n - 1f) - 0.5f) * 2f;
            float d = Mathf.Sqrt(u * u + v * v);
            float fill = 0.35f * Mathf.Clamp01(1f - d * d * 0.6f);
            float ringT = (d - 0.88f) / 0.06f;
            float ring = 0.9f * Mathf.Exp(-ringT * ringT);
            float a = Mathf.Clamp01(fill + ring) * Mathf.Clamp01((1f - d) * 12f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 1f);
    }

    void Update()
    {
        if (orbs == null) return;
        float halfW = viewWidth * 0.5f, halfH = viewHeight * 0.5f;
        float t = Time.time;

        for (int i = 0; i < orbs.Length; i++)
        {
            float margin = baseSize[i] * 64f; // nửa đường kính sprite (128px / 2)
            Vector3 p = orbs[i].localPosition;
            p.y += drift[i] * Time.deltaTime;
            p.x = baseX[i] + Mathf.Sin(t * swaySpeed[i] + phase[i]) * viewWidth * swayAmount;
            if (p.y > halfH + margin)
            {
                p.y = -halfH - margin;
                baseX[i] = Random.Range(-halfW, halfW);
            }
            orbs[i].localPosition = p;

            orbs[i].localScale = Vector3.one * baseSize[i] * (1f + 0.07f * Mathf.Sin(t * pulseSpeed[i] + phase[i]));
            Color c = baseColor[i];
            c.a = baseAlpha[i] * (0.75f + 0.25f * Mathf.Sin(t * pulseSpeed[i] * 0.7f + phase[i] * 1.3f));
            renderers[i].color = c;
        }
    }
}
