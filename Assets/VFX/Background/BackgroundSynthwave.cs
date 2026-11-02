using UnityEngine;

/// <summary>
/// Phong cách Synthwave / Outrun: mặt trời retro có sọc, mặt sàn tối và lưới neon trôi về phía người chơi.
/// Gắn vào Main Camera. Hợp với gradient preset "Synthwave" và có thể kết hợp thêm sao của BackgroundNightSky.
/// Kích thước tính theo % chiều cao màn hình.
/// </summary>
public class BackgroundSynthwave : MonoBehaviour
{
    [Header("Đường chân trời")]
    [Tooltip("-1 = đáy, 1 = đỉnh")]
    [Range(-1f, 1f)] public float horizon = 0f;
    public float distance = 40f;

    [Header("Mặt trời retro")]
    public bool showSun = true;
    public Color sunTopColor = new Color(1f, 0.92f, 0.35f, 1f);
    public Color sunBottomColor = new Color(1f, 0.2f, 0.6f, 1f);
    public Color glowColor = new Color(1f, 0.3f, 0.7f, 0.4f);
    [Tooltip("Đường kính mặt trời = % chiều cao màn hình")]
    [Range(0.1f, 1f)] public float sunSize = 0.45f;
    [Range(-1f, 1f)] public float sunHorizontal = 0f;
    [Range(4, 30)] public int stripeCount = 14;

    [Header("Mặt sàn (che phần dưới mặt trời)")]
    public bool showFloor = true;
    public Color floorTopColor = new Color(0.22f, 0.02f, 0.32f, 1f);
    public Color floorBottomColor = new Color(0.04f, 0f, 0.10f, 1f);

    [Header("Lưới neon")]
    public bool showGrid = true;
    public Color gridColor = new Color(1f, 0.25f, 0.9f, 0.9f);
    public Color horizonLineColor = new Color(0.4f, 1f, 1f, 1f);
    public int horizontalLines = 10;
    public int verticalLines = 15;
    [Tooltip("Độ loe của các đường dọc ở đáy (lớn hơn 1 = tràn ra ngoài màn hình)")]
    public float verticalSpread = 2.4f;
    [Tooltip("Số đường lưới trôi qua mỗi giây")]
    public float scrollSpeed = 1.2f;
    [Tooltip("Độ dày đường = % chiều cao màn hình")]
    public float lineWidth = 0.004f;

    Camera cam;
    float viewHeight, viewWidth, horizonY, bottomY;
    Material lineMat;
    LineRenderer[] hLines;
    float scroll;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundSynthwave: không tìm thấy Camera."); return; }

        viewHeight = ViewHeightAt(distance);
        viewWidth = viewHeight * cam.aspect;
        horizonY = horizon * viewHeight * 0.5f;
        bottomY = -viewHeight * 0.5f * 1.05f;

        if (showSun) BuildSun();
        if (showFloor) BuildFloor();
        if (showGrid) BuildGrid();
    }

    float ViewHeightAt(float d)
    {
        return cam.orthographic ? cam.orthographicSize * 2f
            : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    // ---------- Mặt trời ----------
    void BuildSun()
    {
        var go = new GameObject("SynthSun");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(
            sunHorizontal * viewWidth * 0.5f,
            horizonY + viewHeight * sunSize * 0.12f,
            distance);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BuildSunSprite();
        sr.sortingOrder = -90;
        float target = viewHeight * sunSize * 1.8f; // sprite gồm cả glow nên to hơn đĩa 1.8 lần
        go.transform.localScale = Vector3.one * (target / sr.sprite.bounds.size.x);
    }

    Sprite BuildSunSprite()
    {
        int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float discR = 0.5f / 1.8f;
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
                c.a = Mathf.Clamp01((discR - d) * n * 0.5f);
                if (t < 0.55f)
                {
                    // sọc ngang: càng xuống thấp khe càng to
                    float s = t / 0.55f;
                    float band = Mathf.Repeat(t * stripeCount, 1f);
                    float gap = Mathf.Lerp(0.55f, 0f, s);
                    c.a *= Mathf.Clamp01((band - gap) * 40f);
                }
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

    // ---------- Mặt sàn ----------
    void BuildFloor()
    {
        float floorH = horizonY - (-viewHeight * 0.5f * 1.1f);
        if (floorH <= 0f) return;

        int w = 4, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(floorBottomColor, floorTopColor, y / (h - 1f));
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();

        var go = new GameObject("SynthFloor");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, horizonY, distance);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), 1f); // pivot ở mép trên
        sr.sortingOrder = -88;
        go.transform.localScale = new Vector3(viewWidth * 1.2f / w, floorH / h, 1f);
    }

    // ---------- Lưới neon ----------
    LineRenderer MakeLine(string name, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(cam.transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = lineMat;
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.sortingOrder = order;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        return lr;
    }

    void BuildGrid()
    {
        lineMat = new Material(Shader.Find("Sprites/Default"));
        float lw = viewHeight * lineWidth;

        // Đường dọc: hội tụ về điểm biến mất ở đường chân trời
        for (int i = 0; i < verticalLines; i++)
        {
            float f = verticalLines == 1 ? 0f : (i / (verticalLines - 1f) - 0.5f) * 2f;
            var lr = MakeLine("GridV_" + i, -87);
            lr.SetPosition(0, new Vector3(0f, horizonY, distance));
            lr.SetPosition(1, new Vector3(f * viewWidth * 0.5f * verticalSpread, bottomY, distance));
            lr.startWidth = lw * 0.1f;
            lr.endWidth = lw * 1.4f;
            Color c0 = gridColor; c0.a = 0f;
            lr.startColor = c0;
            lr.endColor = gridColor;
        }

        // Đường ngang: trôi dần về phía đáy, khoảng cách giãn ra theo phối cảnh
        hLines = new LineRenderer[horizontalLines];
        for (int i = 0; i < horizontalLines; i++) hLines[i] = MakeLine("GridH_" + i, -87);

        // Vạch sáng ở đường chân trời
        var hl = MakeLine("HorizonLine", -86);
        hl.SetPosition(0, new Vector3(-viewWidth * 0.6f, horizonY, distance));
        hl.SetPosition(1, new Vector3(viewWidth * 0.6f, horizonY, distance));
        hl.startWidth = hl.endWidth = lw * 1.2f;
        hl.startColor = hl.endColor = horizonLineColor;
    }

    void Update()
    {
        if (hLines == null) return;
        scroll = Mathf.Repeat(scroll + scrollSpeed * Time.deltaTime, 1f);
        float lw = viewHeight * lineWidth;
        float span = viewWidth * 0.6f;

        for (int i = 0; i < hLines.Length; i++)
        {
            float p = (i + scroll) / hLines.Length; // 0 = chân trời, 1 = đáy
            float v = p * p;                        // phối cảnh: càng gần càng giãn
            float y = Mathf.Lerp(horizonY, bottomY, v);
            var lr = hLines[i];
            lr.SetPosition(0, new Vector3(-span, y, distance));
            lr.SetPosition(1, new Vector3(span, y, distance));
            lr.startWidth = lr.endWidth = lw * Mathf.Lerp(0.3f, 1.4f, v);
            Color c = gridColor;
            c.a *= Mathf.Clamp01(v * 4f);
            lr.startColor = lr.endColor = c;
        }
    }
}
