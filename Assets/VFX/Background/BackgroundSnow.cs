using UnityEngine;

/// <summary>
/// Tuyết rơi: lắc lư nhẹ, hạt to rơi nhanh hơn hạt nhỏ, có thể thêm gió.
/// Gắn vào Main Camera. Kích thước tính theo % chiều cao màn hình.
/// </summary>
public class BackgroundSnow : MonoBehaviour
{
    [Header("Bông tuyết")]
    public int flakeCount = 90;
    public Color flakeColor = new Color(1f, 1f, 1f, 0.85f);
    [Tooltip("Kích thước hạt = % chiều cao màn hình")]
    public Vector2 flakeSizeRange = new Vector2(0.006f, 0.022f);
    [Tooltip("Tốc độ rơi (hạt nhỏ = chậm, hạt to = nhanh)")]
    public Vector2 fallSpeedRange = new Vector2(0.8f, 3f);

    [Header("Chuyển động")]
    [Tooltip("Biên độ lắc ngang = % chiều rộng màn hình")]
    public float swayAmount = 0.015f;
    public Vector2 swaySpeedRange = new Vector2(0.6f, 1.6f);
    [Tooltip("Gió: dương = sang phải, âm = sang trái")]
    public float wind = 0.4f;

    public float flakeDistance = 40f;

    Camera cam;
    float viewHeight, viewWidth;
    Transform[] flakes;
    float[] fallSpeeds, baseX, swaySpeeds, swayPhases, swayAmps;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundSnow: không tìm thấy Camera."); return; }
        Build();
    }

    float ViewHeightAt(float d)
    {
        return cam.orthographic ? cam.orthographicSize * 2f
            : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    void Build()
    {
        viewHeight = ViewHeightAt(flakeDistance);
        viewWidth = viewHeight * cam.aspect;

        var sprite = BuildFlakeSprite();
        flakes = new Transform[flakeCount];
        fallSpeeds = new float[flakeCount];
        baseX = new float[flakeCount];
        swaySpeeds = new float[flakeCount];
        swayPhases = new float[flakeCount];
        swayAmps = new float[flakeCount];

        for (int i = 0; i < flakeCount; i++)
        {
            var go = new GameObject("Snow_" + i);
            go.transform.SetParent(cam.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            float t = Random.value; // 0 = nhỏ/xa, 1 = to/gần
            sr.sortingOrder = -84 + Mathf.RoundToInt(t * 3f);
            Color c = flakeColor;
            c.a *= Mathf.Lerp(0.4f, 1f, t);
            sr.color = c;

            float size = viewHeight * Mathf.Lerp(flakeSizeRange.x, flakeSizeRange.y, t);
            go.transform.localScale = Vector3.one * (size / sprite.bounds.size.x);

            fallSpeeds[i] = Mathf.Lerp(fallSpeedRange.x, fallSpeedRange.y, t);
            swaySpeeds[i] = Random.Range(swaySpeedRange.x, swaySpeedRange.y);
            swayPhases[i] = Random.Range(0f, Mathf.PI * 2f);
            swayAmps[i] = viewWidth * swayAmount * Random.Range(0.5f, 1f);
            baseX[i] = Random.Range(-viewWidth * 0.5f, viewWidth * 0.5f);

            flakes[i] = go.transform;
            go.transform.localPosition = new Vector3(baseX[i],
                Random.Range(-viewHeight * 0.5f, viewHeight * 0.5f), flakeDistance);
        }
    }

    // Hạt tròn, lõi trắng đặc, mép mềm
    Sprite BuildFlakeSprite()
    {
        int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x / (n - 1f) - 0.5f) * 2f, v = (y / (n - 1f) - 0.5f) * 2f;
            float d = Mathf.Sqrt(u * u + v * v);
            float a = Mathf.Clamp01((1f - d) * 3f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 1f);
    }

    void Update()
    {
        if (flakes == null) return;
        float halfW = viewWidth * 0.5f, halfH = viewHeight * 0.5f;
        float t = Time.time;

        for (int i = 0; i < flakes.Length; i++)
        {
            baseX[i] += wind * Time.deltaTime;
            if (baseX[i] > halfW + 1f) baseX[i] = -halfW - 1f;
            else if (baseX[i] < -halfW - 1f) baseX[i] = halfW + 1f;

            Vector3 p = flakes[i].localPosition;
            p.y -= fallSpeeds[i] * Time.deltaTime;
            p.x = baseX[i] + Mathf.Sin(t * swaySpeeds[i] + swayPhases[i]) * swayAmps[i];

            if (p.y < -halfH - 1f)
            {
                p.y = halfH + 1f;
                baseX[i] = Random.Range(-halfW, halfW);
            }
            flakes[i].localPosition = p;
        }
    }
}
