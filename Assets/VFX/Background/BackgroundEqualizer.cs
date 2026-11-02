using UnityEngine;
using RhythmGame;

/// <summary>
/// Equalizer: các thanh nhảy theo phổ âm thanh của bài nhạc đang phát (bass ở giữa, treble hai bên nếu bật Mirrored).
/// Có phản chiếu mờ phía dưới. Khi chưa có nhạc (đếm ngược) các thanh gợn nhẹ; khi pause thì đứng yên.
/// Gắn vào Main Camera. Kích thước tính theo % chiều cao màn hình.
/// </summary>
public class BackgroundEqualizer : MonoBehaviour
{
    [Header("Nguồn âm thanh (để trống = tự lấy nhạc của GameManager)")]
    public AudioSource source;

    [Header("Thanh")]
    [Range(8, 96)] public int barCount = 40;
    [Tooltip("Cả dãy thanh chiếm bao nhiêu % chiều rộng màn hình")]
    [Range(0.2f, 1f)] public float widthFraction = 0.92f;
    [Tooltip("Thanh chiếm bao nhiêu % ô của nó (còn lại là khe hở)")]
    [Range(0.1f, 1f)] public float barFill = 0.7f;
    [Tooltip("Vị trí chân thanh: -1 = đáy, 1 = đỉnh (cảnh Synthwave: đặt 0 để đứng trên đường chân trời)")]
    [Range(-1f, 1f)] public float baseline = -0.35f;
    [Tooltip("Chiều cao tối đa = % chiều cao màn hình")]
    [Range(0.05f, 1f)] public float maxHeight = 0.35f;
    [Tooltip("Đối xứng trái - phải, bass ở giữa")]
    public bool mirrored = true;
    public bool reflection = true;
    [Range(0f, 1f)] public float reflectionAlpha = 0.25f;

    [Header("Màu (từ bass -> treble)")]
    public Color lowColor = new Color(1f, 0.25f, 0.7f, 0.85f);
    public Color highColor = new Color(0.3f, 1f, 1f, 0.85f);

    [Header("Độ nhạy")]
    [Range(0.3f, 3f)] public float sensitivity = 1f;
    [Tooltip("Tốc độ thanh vọt lên")]
    public float attack = 25f;
    [Tooltip("Tốc độ thanh hạ xuống")]
    public float decay = 7f;
    [Tooltip("Chiều cao tối thiểu khi im lặng")]
    [Range(0f, 0.2f)] public float idleHeight = 0.04f;

    public float distance = 37f;
    public int sortingOrder = -85;

    const int SpectrumSize = 512;
    const int UsableBins = 200; // ~ tới 9 kHz, phần cao hơn gần như không có năng lượng

    Camera cam;
    float viewHeight, viewWidth, barWidth;
    SpriteRenderer[] bars, refl;
    int[] bandOf, binStart, binEnd;
    float[] bandVal, bandPeak;
    readonly float[] spectrum = new float[SpectrumSize];

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundEqualizer: không tìm thấy Camera."); enabled = false; return; }

        if (!source)
        {
            var gm = FindOne<GameManager>();
            if (gm) source = gm.musicSource;
        }
        Build();
    }

    static T FindOne<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
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

        // nhóm tần số (band) và dải bin FFT của từng band, chia theo thang bình phương (bass chi tiết hơn)
        int bands = mirrored ? Mathf.CeilToInt(barCount * 0.5f) : barCount;
        binStart = new int[bands];
        binEnd = new int[bands];
        bandVal = new float[bands];
        bandPeak = new float[bands];
        for (int k = 0; k < bands; k++)
        {
            float a = k / (float)bands, b = (k + 1) / (float)bands;
            binStart[k] = Mathf.FloorToInt(a * a * UsableBins);
            binEnd[k] = Mathf.Min(SpectrumSize - 1, Mathf.Max(binStart[k] + 1, Mathf.FloorToInt(b * b * UsableBins)));
        }

        // sprite vuông trắng, gốc ở đáy để thanh mọc lên từ chân
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f), 1f);

        float totalW = viewWidth * widthFraction;
        float slot = totalW / barCount;
        barWidth = slot * barFill;
        float y = baseline * viewHeight * 0.5f;
        float center = (barCount - 1) * 0.5f;

        bars = new SpriteRenderer[barCount];
        refl = reflection ? new SpriteRenderer[barCount] : null;
        bandOf = new int[barCount];

        for (int i = 0; i < barCount; i++)
        {
            float kf = Mathf.Abs(i - center);
            bandOf[i] = mirrored ? Mathf.Min(bands - 1, Mathf.FloorToInt(kf)) : i;
            float t = mirrored ? (center > 0f ? kf / center : 0f) : i / (float)Mathf.Max(1, barCount - 1);
            Color c = Color.Lerp(lowColor, highColor, t);
            float x = -totalW * 0.5f + (i + 0.5f) * slot;

            bars[i] = MakeBar("EqBar_" + i, sprite, new Vector3(x, y, distance), c);
            if (reflection)
            {
                Color rc = c;
                rc.a = reflectionAlpha;
                refl[i] = MakeBar("EqRefl_" + i, sprite, new Vector3(x, y, distance), rc);
            }
        }
        ApplyHeights();
    }

    SpriteRenderer MakeBar(string objName, Sprite sprite, Vector3 localPos, Color color)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = localPos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    void Update()
    {
        if (bars == null) return;
        if (PauseManager.IsPaused) return; // đứng yên khi pause

        float dt = Time.deltaTime;
        bool live = source != null && source.isPlaying;
        if (live) source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        for (int k = 0; k < bandVal.Length; k++)
        {
            float target;
            if (live)
            {
                float raw = 0f;
                for (int b = binStart[k]; b < binEnd[k]; b++) raw = Mathf.Max(raw, spectrum[b]);

                // tự cân bằng: mỗi band so với đỉnh gần đây của chính nó -> bass và treble đều nhảy rõ
                bandPeak[k] = Mathf.Max(bandPeak[k] * Mathf.Exp(-dt * 0.3f), raw);
                target = Mathf.Clamp01(raw / Mathf.Max(bandPeak[k], 0.002f) * sensitivity);
                target = Mathf.Pow(target, 1.3f);
            }
            else
            {
                target = 0.12f * (0.5f + 0.5f * Mathf.Sin(Time.time * 1.6f + k * 0.6f));
            }

            float speed = target > bandVal[k] ? attack : decay;
            bandVal[k] = Mathf.Lerp(bandVal[k], target, 1f - Mathf.Exp(-dt * speed));
        }
        ApplyHeights();
    }

    void ApplyHeights()
    {
        for (int i = 0; i < bars.Length; i++)
        {
            float v = bandVal[bandOf[i]];
            float h = viewHeight * maxHeight * (idleHeight + (1f - idleHeight) * v);
            bars[i].transform.localScale = new Vector3(barWidth / 4f, h / 4f, 1f);
            if (refl != null)
                refl[i].transform.localScale = new Vector3(barWidth / 4f, -h * 0.6f / 4f, 1f); // lật xuống dưới chân
        }
    }
}
