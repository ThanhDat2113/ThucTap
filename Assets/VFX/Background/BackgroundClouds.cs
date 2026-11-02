using UnityEngine;

/// <summary>
/// Mây trôi ngang nhiều lớp (mây lớn = gần = trôi nhanh hơn, mây nhỏ = xa = trôi chậm).
/// Gắn vào Main Camera. Kích thước tính theo % chiều cao màn hình.
/// </summary>
public class BackgroundClouds : MonoBehaviour
{
    [Header("Mây")]
    public int cloudCount = 8;
    public Color cloudColor = new Color(1f, 1f, 1f, 0.55f);
    [Tooltip("Chiều rộng mây = % chiều cao màn hình (nhỏ nhất, lớn nhất)")]
    public Vector2 cloudWidthRange = new Vector2(0.25f, 0.7f);
    [Tooltip("Tốc độ trôi (nhỏ nhất ứng với mây nhỏ, lớn nhất ứng với mây lớn)")]
    public Vector2 driftSpeedRange = new Vector2(0.3f, 1.2f);
    [Tooltip("Vùng dọc mây xuất hiện: -1 = đáy, 1 = đỉnh")]
    public Vector2 verticalRange = new Vector2(0.0f, 0.9f);
    [Tooltip("1 = trôi sang phải, -1 = trôi sang trái")]
    public int direction = 1;
    [Range(1, 6)] public int shapeVariants = 3;
    public float cloudDistance = 44f;

    Camera cam;
    float viewHeight, viewWidth;
    Transform[] clouds;
    float[] speeds, halfWidths;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundClouds: không tìm thấy Camera."); return; }
        Build();
    }

    float ViewHeightAt(float d)
    {
        return cam.orthographic ? cam.orthographicSize * 2f
            : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    void Build()
    {
        viewHeight = ViewHeightAt(cloudDistance);
        viewWidth = viewHeight * cam.aspect;

        var sprites = new Sprite[shapeVariants];
        for (int i = 0; i < shapeVariants; i++) sprites[i] = BuildCloudSprite(1000 + i * 37);

        clouds = new Transform[cloudCount];
        speeds = new float[cloudCount];
        halfWidths = new float[cloudCount];

        for (int i = 0; i < cloudCount; i++)
        {
            var go = new GameObject("Cloud_" + i);
            go.transform.SetParent(cam.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            var sprite = sprites[Random.Range(0, sprites.Length)];
            sr.sprite = sprite;
            // mây lớn vẽ trước (gần hơn) -> sortingOrder cao hơn
            float t = Random.value;
            sr.sortingOrder = -91 + Mathf.RoundToInt(t * 4f);
            Color c = cloudColor;
            c.a *= Mathf.Lerp(0.6f, 1f, t);
            sr.color = c;

            float width = viewHeight * Mathf.Lerp(cloudWidthRange.x, cloudWidthRange.y, t);
            go.transform.localScale = Vector3.one * (width / sprite.bounds.size.x);

            speeds[i] = Mathf.Lerp(driftSpeedRange.x, driftSpeedRange.y, t) * direction;
            halfWidths[i] = width * 0.5f;
            clouds[i] = go.transform;
            go.transform.localPosition = new Vector3(
                Random.Range(-viewWidth * 0.5f, viewWidth * 0.5f),
                Random.Range(verticalRange.x, verticalRange.y) * viewHeight * 0.5f,
                cloudDistance);
        }
    }

    // Ghép nhiều hình tròn mềm thành đám mây, đáy phẳng
    Sprite BuildCloudSprite(int seed)
    {
        int w = 256, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        var rng = new System.Random(seed);
        int blobs = 6 + rng.Next(0, 3);
        var cx = new float[blobs];
        var cy = new float[blobs];
        var cr = new float[blobs];
        for (int i = 0; i < blobs; i++)
        {
            float t = (i + 0.5f) / blobs;
            cx[i] = Mathf.Lerp(w * 0.18f, w * 0.82f, t) + (float)(rng.NextDouble() - 0.5) * 12f;
            float mid = 1f - Mathf.Abs(t - 0.5f) * 2f; // giữa cao hơn hai bên
            cr[i] = Mathf.Lerp(h * 0.18f, h * 0.38f, mid) * (0.85f + (float)rng.NextDouble() * 0.3f);
            cy[i] = h * 0.3f + cr[i] * 0.45f;
        }

        float flatY = h * 0.22f;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float a = 0f;
            for (int i = 0; i < blobs; i++)
            {
                float dx = x - cx[i], dy = y - cy[i];
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                a = Mathf.Max(a, Mathf.Clamp01((cr[i] - d) / (cr[i] * 0.35f)));
            }
            if (y < flatY) a *= Mathf.Clamp01(y / flatY); // đáy mờ dần
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f);
    }

    void Update()
    {
        if (clouds == null) return;
        float limit = viewWidth * 0.5f;
        for (int i = 0; i < clouds.Length; i++)
        {
            Vector3 p = clouds[i].localPosition;
            p.x += speeds[i] * Time.deltaTime;
            if (direction >= 0 && p.x - halfWidths[i] > limit)
            {
                p.x = -limit - halfWidths[i];
                p.y = Random.Range(verticalRange.x, verticalRange.y) * viewHeight * 0.5f;
            }
            else if (direction < 0 && p.x + halfWidths[i] < -limit)
            {
                p.x = limit + halfWidths[i];
                p.y = Random.Range(verticalRange.x, verticalRange.y) * viewHeight * 0.5f;
            }
            clouds[i].localPosition = p;
        }
    }
}
