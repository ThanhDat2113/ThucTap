using UnityEngine;

/// <summary>
/// Núi nhiều lớp trôi parallax vô tận (lớp xa nhạt + chậm, lớp gần đậm + nhanh).
/// Hình núi được sinh bằng code, liền mạch (tile) nên cuộn mãi không thấy mối nối.
/// Gắn vào Main Camera. Hợp với gradient preset "Sunset" hoặc "Dream", kết hợp tốt với mặt trời của BackgroundExtras.
/// </summary>
public class BackgroundMountains : MonoBehaviour
{
    [Header("Các lớp núi")]
    [Range(1, 6)] public int layerCount = 4;
    [Tooltip("Màu lớp xa nhất (nên gần màu sương/chân trời)")]
    public Color farColor = new Color(0.78f, 0.52f, 0.72f, 1f);
    [Tooltip("Màu lớp gần nhất")]
    public Color nearColor = new Color(0.14f, 0.07f, 0.28f, 1f);
    public int seed = 7;

    [Header("Kích thước & vị trí (% chiều cao màn hình)")]
    [Tooltip("Chiều cao tối đa của lớp xa / lớp gần")]
    [Range(0.05f, 1f)] public float farHeight = 0.40f;
    [Range(0.05f, 1f)] public float nearHeight = 0.28f;
    [Tooltip("Chân núi cách đáy màn hình bao nhiêu: lớp xa / lớp gần")]
    [Range(0f, 1f)] public float farBase = 0.30f;
    [Range(0f, 1f)] public float nearBase = 0f;

    [Header("Chuyển động (world units / giây)")]
    public float farSpeed = 0.1f;
    public float nearSpeed = 0.8f;
    [Tooltip("Bật = núi trôi sang trái (cảm giác nhân vật chạy sang phải)")]
    public bool scrollLeft = true;

    public float distance = 41f;

    Camera cam;
    float viewHeight, viewWidth, tileW;
    Transform[] tiles;   // mỗi lớp 2 tile ghép nối nhau
    float[] layerSpeeds;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundMountains: không tìm thấy Camera."); return; }
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
        tileW = viewWidth * 1.05f;

        tiles = new Transform[layerCount * 2];
        layerSpeeds = new float[layerCount];

        for (int i = 0; i < layerCount; i++)
        {
            float t = layerCount == 1 ? 1f : i / (layerCount - 1f); // 0 = xa, 1 = gần
            var sprite = BuildMountainSprite(seed + i * 101);
            Color col = Color.Lerp(farColor, nearColor, t);
            float hh = viewHeight * Mathf.Lerp(farHeight, nearHeight, t);
            float baseY = -viewHeight * 0.5f + viewHeight * Mathf.Lerp(farBase, nearBase, t);
            layerSpeeds[i] = Mathf.Lerp(farSpeed, nearSpeed, t);

            for (int k = 0; k < 2; k++)
            {
                var go = new GameObject("Mountain_" + i + "_" + k);
                go.transform.SetParent(cam.transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = col;
                sr.sortingOrder = -90 + i;
                go.transform.localScale = new Vector3(
                    tileW / sprite.bounds.size.x, hh / sprite.bounds.size.y, 1f);
                go.transform.localPosition = new Vector3(k * tileW, baseY, distance);
                tiles[i * 2 + k] = go.transform;
            }
        }
    }

    // Đường núi = tổng các sóng sin có tần số nguyên (1 - |sin| cho đỉnh nhọn) -> tuần hoàn, ghép tile liền mạch
    Sprite BuildMountainSprite(int sd)
    {
        int w = 1024, h = 256;
        var rng = new System.Random(sd);
        int[] freqs = { 2, 3, 5, 8, 13 };
        float[] amps = { 1f, 0.7f, 0.45f, 0.28f, 0.16f };
        float[] phases = new float[freqs.Length];
        float ampSum = 0f;
        for (int k = 0; k < freqs.Length; k++)
        {
            phases[k] = (float)(rng.NextDouble() * Mathf.PI * 2f);
            ampSum += amps[k];
        }

        float minFrac = 0.35f; // đỉnh thấp nhất (so với đỉnh cao nhất)
        var edge = new float[w];
        for (int x = 0; x < w; x++)
        {
            float u = x / (float)w;
            float s = 0f;
            for (int k = 0; k < freqs.Length; k++)
                s += amps[k] * (1f - Mathf.Abs(Mathf.Sin(Mathf.PI * freqs[k] * u + phases[k])));
            edge[x] = Mathf.Lerp(minFrac, 1f, s / ampSum) * (h - 2);
        }

        var pixels = new Color32[w * h];
        for (int x = 0; x < w; x++)
        for (int y = 0; y < h; y++)
        {
            float a = Mathf.Clamp01(edge[x] - y);                          // mép trên mượt 1px
            float k = Mathf.Clamp01(y / Mathf.Max(1f, edge[x]));
            byte shade = (byte)(Mathf.Lerp(0.78f, 1f, k) * 255f);          // sáng dần về phía đỉnh
            pixels[y * w + x] = new Color32(shade, shade, shade, (byte)(a * 255f));
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), 1f); // pivot ở chân núi
    }

    void Update()
    {
        if (tiles == null) return;
        float dir = scrollLeft ? -1f : 1f;
        for (int i = 0; i < layerCount; i++)
        {
            float dx = layerSpeeds[i] * dir * Time.deltaTime;
            for (int k = 0; k < 2; k++)
            {
                var tr = tiles[i * 2 + k];
                Vector3 p = tr.localPosition;
                p.x += dx;
                if (scrollLeft && p.x <= -tileW) p.x += tileW * 2f;
                else if (!scrollLeft && p.x >= tileW) p.x -= tileW * 2f;
                tr.localPosition = p;
            }
        }
    }
}
