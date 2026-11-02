using UnityEngine;

public enum BgGradientPreset
{
    Custom,
    Synthwave,
    Sunset,
    Dream,
    Aurora,
    Rainy,
    Blossom,
    Night,
    Day,
    Winter,
    Forest
}

/// <summary>
/// Gradient 3 màu có sẵn nhiều preset, thêm vignette (tối dần ở góc) và hiệu ứng "thở" nhẹ.
/// Là 1 Quad con của Camera, nằm thật xa phía sau mọi thứ (không phải UI Overlay nên không che gameplay).
///
/// CÁCH DÙNG: gắn thẳng vào Main Camera, chọn preset. Chọn Custom để tự đặt màu.
/// Lưu ý: distance phải LỚN HƠN khoảng cách của các script hiệu ứng (tối đa 50) để gradient luôn nằm sau cùng.
/// </summary>
public class WorldBackgroundGradientPro : MonoBehaviour
{
    [Header("Preset (khác Custom sẽ ghi đè 3 màu bên dưới khi chạy)")]
    public BgGradientPreset preset = BgGradientPreset.Synthwave;

    [Header("Màu gradient (trên -> giữa -> dưới)")]
    public Color topColor = new Color(0.45f, 0.15f, 0.55f);
    public Color middleColor = new Color(0.7f, 0.2f, 0.6f);
    public Color bottomColor = new Color(0.95f, 0.25f, 0.65f);
    [Tooltip("Vị trí màu giữa tính từ đáy: 0 = đáy, 1 = đỉnh")]
    [Range(0.05f, 0.95f)] public float middlePosition = 0.5f;

    [Header("Hiệu ứng")]
    [Tooltip("Tối dần ở bốn góc, tạo cảm giác có chiều sâu")]
    [Range(0f, 1f)] public float vignette = 0.35f;
    [Tooltip("Độ sáng nền phập phồng nhẹ (0 = tắt)")]
    [Range(0f, 0.3f)] public float breathe = 0.05f;
    public float breatheSpeed = 0.6f;

    [Header("Khoảng cách đặt phía sau Camera (world units)")]
    public float distance = 60f;
    [Tooltip("Phóng to thêm so với khung nhìn vừa đủ, để phủ kín kể cả khi camera xoay nhẹ")]
    public float overscan = 1.15f;

    public int resolution = 128;
    public int textureWidth = 64;

    Material mat;

    void Start()
    {
        var cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("WorldBackgroundGradientPro: không tìm thấy Camera."); return; }

        ApplyPreset();

        var quadGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadGO.name = "WorldBackgroundGradientPro";
        Destroy(quadGO.GetComponent<Collider>()); // không cần va chạm

        quadGO.transform.SetParent(cam.transform, false);
        quadGO.transform.localRotation = Quaternion.identity;

        float heightWorld = cam.orthographic
            ? cam.orthographicSize * 2f
            : 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float widthWorld = heightWorld * cam.aspect;

        quadGO.transform.localPosition = new Vector3(0f, 0f, distance);
        quadGO.transform.localScale = new Vector3(widthWorld * overscan, heightWorld * overscan, 1f);

        var mr = quadGO.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.sortingOrder = -100; // luôn vẽ trước các hiệu ứng khác

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        mat = new Material(shader);
        mat.mainTexture = BuildGradientTexture();
        mr.material = mat;
    }

    void Set(Color top, Color mid, Color bottom, float midPos)
    {
        topColor = top;
        middleColor = mid;
        bottomColor = bottom;
        middlePosition = midPos;
    }

    void ApplyPreset()
    {
        switch (preset)
        {
            case BgGradientPreset.Synthwave:
                Set(new Color(0.05f, 0.00f, 0.18f), new Color(0.45f, 0.05f, 0.50f), new Color(1.00f, 0.35f, 0.55f), 0.5f); break;
            case BgGradientPreset.Sunset:
                Set(new Color(0.15f, 0.12f, 0.38f), new Color(0.85f, 0.30f, 0.45f), new Color(1.00f, 0.70f, 0.35f), 0.4f); break;
            case BgGradientPreset.Dream:
                Set(new Color(0.25f, 0.12f, 0.45f), new Color(0.55f, 0.25f, 0.65f), new Color(0.95f, 0.55f, 0.75f), 0.5f); break;
            case BgGradientPreset.Aurora:
                Set(new Color(0.01f, 0.03f, 0.12f), new Color(0.02f, 0.12f, 0.22f), new Color(0.05f, 0.25f, 0.30f), 0.55f); break;
            case BgGradientPreset.Rainy:
                Set(new Color(0.10f, 0.13f, 0.18f), new Color(0.22f, 0.27f, 0.33f), new Color(0.38f, 0.44f, 0.50f), 0.5f); break;
            case BgGradientPreset.Blossom:
                Set(new Color(0.98f, 0.74f, 0.84f), new Color(1.00f, 0.85f, 0.88f), new Color(1.00f, 0.95f, 0.90f), 0.5f); break;
            case BgGradientPreset.Night:
                Set(new Color(0.03f, 0.04f, 0.16f), new Color(0.16f, 0.12f, 0.38f), new Color(0.55f, 0.28f, 0.55f), 0.5f); break;
            case BgGradientPreset.Day:
                Set(new Color(0.20f, 0.50f, 0.92f), new Color(0.55f, 0.80f, 0.98f), new Color(1.00f, 0.88f, 0.72f), 0.5f); break;
            case BgGradientPreset.Winter:
                Set(new Color(0.28f, 0.38f, 0.58f), new Color(0.50f, 0.66f, 0.84f), new Color(0.82f, 0.92f, 0.98f), 0.45f); break;
            case BgGradientPreset.Forest:
                Set(new Color(0.02f, 0.07f, 0.10f), new Color(0.04f, 0.16f, 0.15f), new Color(0.12f, 0.28f, 0.16f), 0.55f); break;
            case BgGradientPreset.Custom:
            default: break;
        }
    }

    Texture2D BuildGradientTexture()
    {
        int w = Mathf.Max(4, textureWidth);
        var tex = new Texture2D(w, resolution, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < resolution; y++)
        {
            float t = (float)y / (resolution - 1); // 0 = dưới texture, 1 = trên
            Color c = t < middlePosition
                ? Color.Lerp(bottomColor, middleColor, t / middlePosition)
                : Color.Lerp(middleColor, topColor, (t - middlePosition) / (1f - middlePosition));

            float ny = (t - 0.5f) * 2f;
            for (int x = 0; x < w; x++)
            {
                float nx = (x / (w - 1f) - 0.5f) * 2f;
                float r = Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f;   // 0 = tâm, 1 = góc
                float vig = 1f - vignette * r * r;
                tex.SetPixel(x, y, new Color(c.r * vig, c.g * vig, c.b * vig, 1f));
            }
        }
        tex.Apply();
        return tex;
    }

    void Update()
    {
        if (mat == null || breathe <= 0f) return;
        float k = 1f - breathe * (0.5f + 0.5f * Mathf.Sin(Time.time * breatheSpeed));
        mat.color = new Color(k, k, k, 1f);
    }
}
