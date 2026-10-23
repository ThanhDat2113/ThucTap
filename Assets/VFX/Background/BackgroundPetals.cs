using UnityEngine;

/// <summary>
/// Cánh hoa anh đào rơi: xoay tròn, lật nghiêng như 3D, đung đưa theo gió.
/// Muốn thành lá mùa thu: đổi colorA/colorB sang cam - đỏ - vàng.
/// Gắn vào Main Camera. Hợp với gradient preset "Blossom" hoặc "Dream".
/// </summary>
public class BackgroundPetals : MonoBehaviour
{
    [Header("Cánh hoa")]
    public int petalCount = 45;
    [Tooltip("Mỗi cánh có màu ngẫu nhiên giữa colorA và colorB")]
    public Color colorA = new Color(1f, 0.80f, 0.88f, 0.95f);
    public Color colorB = new Color(1f, 0.55f, 0.72f, 0.95f);
    [Tooltip("Kích thước = % chiều cao màn hình")]
    public Vector2 sizeRange = new Vector2(0.015f, 0.04f);

    [Header("Chuyển động")]
    public Vector2 fallSpeedRange = new Vector2(0.6f, 1.8f);
    [Tooltip("Biên độ lắc ngang = % chiều rộng màn hình")]
    public float swayAmount = 0.04f;
    public Vector2 swaySpeedRange = new Vector2(0.5f, 1.3f);
    [Tooltip("Gió: dương = sang phải, âm = sang trái")]
    public float wind = 0.5f;
    [Tooltip("Tốc độ xoay (độ / giây)")]
    public Vector2 spinSpeedRange = new Vector2(-90f, 90f);
    [Tooltip("Tốc độ lật nghiêng")]
    public Vector2 flipSpeedRange = new Vector2(1.5f, 4f);

    public float distance = 39f;

    Camera cam;
    float viewHeight, viewWidth;
    Transform[] petals;
    float[] baseX, fall, swaySpeed, swayPhase, swayAmp, spin, flipSpeed, flipPhase, rotPhase;
    Vector2[] baseScale;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundPetals: không tìm thấy Camera."); return; }
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

        var sprite = BuildPetalSprite();
        petals = new Transform[petalCount];
        baseX = new float[petalCount]; fall = new float[petalCount];
        swaySpeed = new float[petalCount]; swayPhase = new float[petalCount]; swayAmp = new float[petalCount];
        spin = new float[petalCount]; flipSpeed = new float[petalCount];
        flipPhase = new float[petalCount]; rotPhase = new float[petalCount];
        baseScale = new Vector2[petalCount];

        for (int i = 0; i < petalCount; i++)
        {
            var go = new GameObject("Petal_" + i);
            go.transform.SetParent(cam.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;

            float t = Random.value; // 0 = nhỏ/xa, 1 = to/gần
            sr.sortingOrder = -83 + Mathf.RoundToInt(t * 3f);
            Color c = Color.Lerp(colorA, colorB, Random.value);
            c.a *= Mathf.Lerp(0.6f, 1f, t);
            sr.color = c;

            float size = viewHeight * Mathf.Lerp(sizeRange.x, sizeRange.y, t);
            baseScale[i] = new Vector2(size / sprite.bounds.size.x, size / sprite.bounds.size.y);

            fall[i] = Mathf.Lerp(fallSpeedRange.x, fallSpeedRange.y, t);
            swaySpeed[i] = Random.Range(swaySpeedRange.x, swaySpeedRange.y);
            swayPhase[i] = Random.Range(0f, Mathf.PI * 2f);
            swayAmp[i] = viewWidth * swayAmount * Random.Range(0.5f, 1f);
            spin[i] = Random.Range(spinSpeedRange.x, spinSpeedRange.y);
            flipSpeed[i] = Random.Range(flipSpeedRange.x, flipSpeedRange.y);
            flipPhase[i] = Random.Range(0f, Mathf.PI * 2f);
            rotPhase[i] = Random.Range(0f, 360f);
            baseX[i] = Random.Range(-viewWidth * 0.5f, viewWidth * 0.5f);

            petals[i] = go.transform;
            go.transform.localPosition = new Vector3(baseX[i],
                Random.Range(-viewHeight * 0.5f, viewHeight * 0.5f), distance);
        }
    }

    // Cánh hoa: hình trứng, đầu rộng có khía nhỏ, có gân giữa mờ
    Sprite BuildPetalSprite()
    {
        int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x / (n - 1f) - 0.5f) * 2f, v = (y / (n - 1f) - 0.5f) * 2f;
            float hw = 0.6f * Mathf.Sqrt(Mathf.Max(0f, 1f - v * v)) * (1f + 0.25f * v); // rộng dần lên đầu cánh
            float a = Mathf.Clamp01((hw - Mathf.Abs(u)) * n * 0.5f);
            float nd = Mathf.Sqrt(u * u + (v - 1f) * (v - 1f));                          // khía ở đầu cánh
            a *= Mathf.Clamp01((nd - 0.2f) * n * 0.5f);
            float shade = (0.85f + 0.15f * (v + 1f) * 0.5f) * (1f - 0.12f * Mathf.Exp(-u * u * 60f));
            tex.SetPixel(x, y, new Color(shade, shade, shade, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 1f);
    }

    void Update()
    {
        if (petals == null) return;
        float halfW = viewWidth * 0.5f, halfH = viewHeight * 0.5f;
        float t = Time.time;

        for (int i = 0; i < petals.Length; i++)
        {
            baseX[i] += wind * Time.deltaTime;
            if (baseX[i] > halfW + 1f) baseX[i] = -halfW - 1f;
            else if (baseX[i] < -halfW - 1f) baseX[i] = halfW + 1f;

            Vector3 p = petals[i].localPosition;
            p.y -= fall[i] * Time.deltaTime;
            p.x = baseX[i] + Mathf.Sin(t * swaySpeed[i] + swayPhase[i]) * swayAmp[i];
            if (p.y < -halfH - 1f)
            {
                p.y = halfH + 1f;
                baseX[i] = Random.Range(-halfW, halfW);
            }
            petals[i].localPosition = p;

            // lật nghiêng giả 3D bằng cách co giãn trục dọc
            float flip = 0.25f + 0.75f * Mathf.Abs(Mathf.Cos(t * flipSpeed[i] + flipPhase[i]));
            petals[i].localScale = new Vector3(baseScale[i].x, baseScale[i].y * flip, 1f);
            petals[i].localRotation = Quaternion.Euler(0f, 0f, rotPhase[i] + spin[i] * t);
        }
    }
}
