using UnityEngine;

/// <summary>
/// Cực quang: các dải sáng uốn lượn như tấm rèm, đổi màu chậm rãi.
/// Dùng Mesh có vertex color, cập nhật mỗi frame (nhẹ: vài trăm đỉnh).
/// Gắn vào Main Camera. Hợp với gradient preset "Aurora" và sao của BackgroundNightSky (tắt trăng nếu muốn).
/// </summary>
public class BackgroundAurora : MonoBehaviour
{
    [Header("Dải cực quang")]
    [Range(1, 6)] public int ribbonCount = 3;
    [Tooltip("Mỗi dải pha giữa 2 màu liên tiếp trong danh sách")]
    public Color[] palette =
    {
        new Color(0.2f, 1f, 0.55f, 1f),
        new Color(0.2f, 0.8f, 1f, 1f),
        new Color(0.7f, 0.35f, 1f, 1f)
    };
    [Range(0f, 1f)] public float intensity = 0.65f;

    [Header("Hình dạng")]
    [Tooltip("Độ cao chân dải: -1 = đáy, 1 = đỉnh")]
    [Range(-1f, 1f)] public float baseVertical = 0.05f;
    [Tooltip("Chiều cao dải = % chiều cao màn hình")]
    [Range(0.1f, 1f)] public float ribbonHeight = 0.55f;
    [Tooltip("Độ uốn lượn = % chiều cao màn hình")]
    [Range(0f, 0.5f)] public float waveAmount = 0.12f;
    public float waveSpeed = 0.35f;
    [Range(16, 96)] public int columns = 48;

    public float distance = 47f;

    class Ribbon
    {
        public Mesh mesh;
        public Vector3[] verts;
        public Color[] cols;
        public float phase, yShift, heightScale;
        public Color a, b;
    }

    const int Rows = 4;
    static readonly float[] RowFrac = { 0f, 0.12f, 0.55f, 1f };   // vị trí các hàng đỉnh theo chiều cao dải
    static readonly float[] RowAlpha = { 0f, 1f, 0.4f, 0f };       // sáng gắt ở chân, mờ dần lên đỉnh

    Camera cam;
    float viewHeight, viewWidth;
    Ribbon[] ribbons;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundAurora: không tìm thấy Camera."); return; }

        viewHeight = ViewHeightAt(distance);
        viewWidth = viewHeight * cam.aspect;
        if (palette == null || palette.Length == 0) palette = new[] { Color.green };

        var mat = new Material(Shader.Find("Sprites/Default")); // hỗ trợ vertex color + alpha
        ribbons = new Ribbon[ribbonCount];
        for (int i = 0; i < ribbonCount; i++)
        {
            var r = new Ribbon();
            r.phase = i * 1.7f + 0.3f;
            r.yShift = (i - (ribbonCount - 1) * 0.5f) * 0.07f;
            r.heightScale = 1f - i * 0.12f;
            r.a = palette[i % palette.Length];
            r.b = palette[(i + 1) % palette.Length];

            int vc = columns * Rows;
            r.verts = new Vector3[vc];
            r.cols = new Color[vc];
            var uv = new Vector2[vc];
            for (int c = 0; c < columns; c++)
                for (int k = 0; k < Rows; k++)
                    uv[c * Rows + k] = new Vector2(c / (columns - 1f), RowFrac[k]);

            var tris = new int[(columns - 1) * (Rows - 1) * 6];
            int ti = 0;
            for (int c = 0; c < columns - 1; c++)
                for (int k = 0; k < Rows - 1; k++)
                {
                    int i0 = c * Rows + k, i1 = (c + 1) * Rows + k, i2 = i0 + 1, i3 = i1 + 1;
                    tris[ti++] = i0; tris[ti++] = i2; tris[ti++] = i1;
                    tris[ti++] = i1; tris[ti++] = i2; tris[ti++] = i3;
                }

            r.mesh = new Mesh { name = "AuroraRibbon_" + i };
            r.mesh.MarkDynamic();
            r.mesh.vertices = r.verts;
            r.mesh.uv = uv;
            r.mesh.triangles = tris;
            r.mesh.colors = r.cols;

            var go = new GameObject("Aurora_" + i);
            go.transform.SetParent(cam.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = r.mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.sortingOrder = -89;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            ribbons[i] = r;
            UpdateRibbon(r, 0f);
        }
    }

    float ViewHeightAt(float d)
    {
        return cam.orthographic ? cam.orthographicSize * 2f
            : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    void UpdateRibbon(Ribbon r, float t)
    {
        for (int c = 0; c < columns; c++)
        {
            float u = c / (columns - 1f);

            float wave = Mathf.Sin(u * 5.5f + t * waveSpeed + r.phase) * 0.6f
                       + Mathf.Sin(u * 11f - t * waveSpeed * 1.3f + r.phase * 2f) * 0.4f;
            float baseY = viewHeight * (baseVertical * 0.5f + r.yShift + wave * waveAmount);
            float height = viewHeight * ribbonHeight * r.heightScale
                         * (0.55f + 0.45f * Mathf.PerlinNoise(u * 2.5f + r.phase, t * 0.25f));
            float x = (u - 0.5f) * viewWidth * 1.15f;

            Color col = Color.Lerp(r.a, r.b, Mathf.PerlinNoise(u * 1.7f + r.phase * 3f, t * 0.12f));
            float streak = 0.65f + 0.35f * Mathf.PerlinNoise(u * 20f + r.phase, t * 0.7f); // tia rèm
            float edge = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 0.6f);                         // mờ ở hai đầu

            for (int k = 0; k < Rows; k++)
            {
                int idx = c * Rows + k;
                r.verts[idx] = new Vector3(x, baseY + height * RowFrac[k], distance);
                Color cc = Color.Lerp(col, r.b, RowFrac[k] * 0.5f);
                cc.a = RowAlpha[k] * intensity * streak * edge;
                r.cols[idx] = cc;
            }
        }
        r.mesh.vertices = r.verts;
        r.mesh.colors = r.cols;
        r.mesh.RecalculateBounds();
    }

    void Update()
    {
        if (ribbons == null) return;
        float t = Time.time;
        for (int i = 0; i < ribbons.Length; i++) UpdateRibbon(ribbons[i], t);
    }
}
