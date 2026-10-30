using System.Collections.Generic;
using RhythmGame;
using UnityEngine;

/// <summary>Một background: 1 ảnh + bảng màu cho note + vị trí đèn sân khấu.</summary>
[System.Serializable]
public class BackgroundTheme
{
    public string name = "Theme";
    [Tooltip("Ảnh nền (nên 16:9, vùng giữa tối và thoáng để dễ nhìn note)")]
    public Texture2D image;

    [Header("Note hợp với nền")]
    [Tooltip("Tự lấy màu chủ đạo từ chính tấm ảnh (màu note, tia đèn, equalizer, hạt sáng đều theo ảnh). Tắt nếu muốn tự đặt màu bên dưới.")]
    public bool autoPalette = true;
    [Tooltip("Tự đổi kiểu note khi dùng theme này (Street = bảng kim loại graffiti neon giống nền sân khấu)")]
    public bool setNoteStyle = true;
    public NoteStyle noteStyle = NoteStyle.Street;

    [Header("Màu note (NoteSkin lấy theo; bị ghi đè khi bật Auto Palette)")]
    public Color paletteTop = new Color(0.25f, 0.10f, 0.55f);
    public Color paletteMiddle = new Color(1.00f, 0.20f, 0.85f);
    public Color paletteBottom = new Color(0.20f, 0.90f, 1.00f);

    [Header("Hiệu ứng")]
    [Tooltip("Cao độ mép trên của mặt sàn (0 = đáy ảnh, 1 = đỉnh ảnh). Phần dưới mức này sẽ gợn sóng.")]
    [Range(0f, 1f)] public float floorY = 0.34f;
    public Color beamColorA = new Color(0.2f, 0.85f, 1f);
    public Color beamColorB = new Color(1f, 0.25f, 0.85f);
    [Tooltip("Vị trí 4 đèn sân khấu trong ảnh (0..1, gốc ở góc dưới trái). Đặt y = -1 để tắt tia.")]
    public Vector2[] beamOrigins =
    {
        new Vector2(0.440f, 0.90f), new Vector2(0.510f, 0.90f),
        new Vector2(0.556f, 0.90f), new Vector2(0.604f, 0.90f)
    };

    [Header("Mặt trăng / mặt trời")]
    [Tooltip("Vị trí tâm quầng sáng trong ảnh (0..1, gốc ở góc dưới trái)")]
    public Vector2 focusPoint = new Vector2(0.58f, 0.74f);
    public Color focusColor = new Color(1f, 0.70f, 0.85f);
    [Range(0.02f, 0.5f)] public float focusRadius = 0.12f;

    [Header("Mưa")]
    [Tooltip("Vệt mưa rơi trên nền (0 = tắt). Hợp với cảnh Rainy / Night.")]
    [Range(0f, 1f)] public float rain = 0f;

    [Header("Cửa sổ toà nhà lập loè")]
    [Tooltip("Vùng có toà nhà trong ảnh: x0, y0, x1, y1 (0..1, gốc ở góc dưới trái)")]
    public Vector4 twinkleRect = new Vector4(0.30f, 0.38f, 0.82f, 0.80f);
}

/// <summary>
/// Background động nhiều theme, chạy bằng 1 Quad con của Camera + shader RhythmGame/AnimatedNeonBackground.
/// Hiệu ứng: lắc/zoom nhẹ như máy quay, đèn neon chớp theo beat nhạc, tia đèn sân khấu quét,
/// sàn phản chiếu gợn sóng, đổi sắc nhẹ theo thời gian, tối dần ở giữa để giữ gameplay dễ nhìn.
///
/// CÁCH DÙNG: gắn vào Main Camera, kéo file shader vào ô Shader, thêm theme (ảnh + màu).
/// NHỚ TẮT / GỠ WorldBackgroundGradientPro trên Main Camera, nếu không nó sẽ vẽ đè lên.
/// </summary>
public class AnimatedBackground : MonoBehaviour
{
    public static AnimatedBackground Instance { get; private set; }

    [Header("Danh sách background")]
    public List<BackgroundTheme> themes = new List<BackgroundTheme> { new BackgroundTheme() };
    public int startIndex = 0;
    public bool randomOnStart = false;
    [Tooltip("Thời gian chuyển mờ khi đổi theme (giây)")]
    public float fadeTime = 1.2f;

    [Header("Shader (kéo AnimatedNeonBackground vào đây để build không bị thiếu)")]
    public Shader shader;

    [Header("Nhạc")]
    [Tooltip("Nguồn nhạc chính. Để trống = nghe toàn bộ âm thanh của game (cả tiếng hit)")]
    public AudioSource audioSource;
    public bool useAudioSpectrum = true;
    [Range(0.5f, 2f)] public float beatSensitivity = 1f;
    [Tooltip("Dùng khi không bắt được beat từ nhạc: tự chớp đều theo BPM")]
    public bool useBpmPulse = false;
    public float bpm = 120f;
    [Range(0f, 1.5f)] public float pulseStrength = 0.8f;
    [Tooltip("Tốc độ tắt dần của mỗi lần chớp")]
    public float pulseDecay = 5f;

    [Header("Chuyển động")]
    [Tooltip("Phóng to nền sẵn một chút để có chỗ lắc mà không lộ mép")]
    [Range(1.03f, 1.3f)] public float baseZoom = 1.08f;
    [Range(0f, 0.015f)] public float swayAmount = 0.008f;
    public float swaySpeed = 0.35f;
    [Range(0f, 0.03f)] public float pulseZoom = 0.012f;
    [Tooltip("Biên độ đổi sắc (radian)")]
    [Range(0f, 0.6f)] public float hueRange = 0.18f;

    [Header("Hiệu ứng ánh sáng")]
    [Range(0f, 3f)] public float neonBoost = 1.2f;
    [Range(0f, 0.015f)] public float floorWave = 0.004f;
    [Range(0f, 1f)] public float beamStrength = 0.35f;
    [Range(0f, 0.5f)] public float flicker = 0.15f;
    [Range(0f, 1f)] public float vignette = 0.35f;

    [Header("Giữ gameplay dễ nhìn (toạ độ theo màn hình 0..1)")]
    [Range(0f, 1f)] public float laneCenterX = 0.5f;
    [Range(0f, 0.5f)] public float laneHalfWidth = 0.17f;
    [Range(0.01f, 0.5f)] public float laneSoftness = 0.12f;
    [Tooltip("Mức làm tối vùng giữa. Tăng nếu note khó nhìn.")]
    [Range(0f, 0.9f)] public float laneDim = 0.35f;

    [Header("Hiệu ứng nổi bật")]
    [Tooltip("Thanh equalizer nhảy theo nhạc ở hai bên màn hình (nằm ngoài vùng lane)")]
    [Range(0f, 1.5f)] public float barsStrength = 0.5f;
    [Range(0.1f, 0.6f)] public float barHeight = 0.35f;
    [Tooltip("Vị trí bắt đầu của thanh tính từ giữa màn hình. Phải lớn hơn Lane Half Width.")]
    [Range(0.2f, 0.45f)] public float barInner = 0.30f;
    [Tooltip("Hạt sáng bay lên")]
    [Range(0f, 1.5f)] public float particleStrength = 0.6f;
    [Tooltip("Lệch màu RGB mỗi nhịp, mạnh ở hai bên")]
    [Range(0f, 0.01f)] public float aberration = 0.004f;
    [Tooltip("Quầng sáng quanh mặt trăng / mặt trời")]
    [Range(0f, 1.5f)] public float focusStrength = 0.6f;
    [Tooltip("Cửa sổ toà nhà lập loè")]
    [Range(0f, 1f)] public float twinkle = 0.5f;

    [Header("Fever (combo cao) và Miss")]
    [Tooltip("Từ combo này nền vào chế độ Fever: màu rực hơn, nhiều tia sáng và hạt hơn. Tự đếm combo từ các lần trúng / miss.")]
    public int feverCombo = 50;
    [Range(0f, 2f)] public float feverStrength = 1f;
    [Tooltip("Nhiễu hình, mất màu và ngả đỏ trong chốc lát mỗi khi miss. 0 = tắt")]
    [Range(0f, 1.5f)] public float missGlitch = 1f;
    [Tooltip("Lưới ánh sáng chạy trên sàn nước")]
    [Range(0f, 1.5f)] public float floorGrid = 0.35f;

    [Header("Phản ứng khi đánh trúng note")]
    [Tooltip("Vòng sóng trên sàn + trụ sáng vươn lên tại điểm đánh (NoteView tự gọi khi trúng note)")]
    [Range(0f, 1.5f)] public float hitStrength = 0.6f;
    public float hitDuration = 0.55f;

    [Header("Sóng xung kích")]
    [Range(0f, 1.5f)] public float rippleStrength = 1f;
    public float rippleDuration = 0.9f;
    [Tooltip("Tự phát sóng khi gặp beat rất mạnh")]
    public bool autoBurstOnBigBeat = true;
    [Tooltip("Phát sóng mỗi khi combo chia hết cho số này (0 = tắt)")]
    public int burstEveryCombo = 50;

    [Header("Camera")]
    public float distance = 60f;

    // màu hiện tại của theme, NoteSkin đọc để tô note cho hợp nền
    public Color paletteTop { get { var c = Current; return c != null ? c.paletteTop : new Color(0.25f, 0.10f, 0.55f); } }
    public Color paletteMiddle { get { var c = Current; return c != null ? c.paletteMiddle : new Color(1f, 0.2f, 0.85f); } }
    public Color paletteBottom { get { var c = Current; return c != null ? c.paletteBottom : new Color(0.2f, 0.9f, 1f); } }
    public int ThemeIndex { get { return index; } }
    /// <summary>Độ mạnh nhịp hiện tại (0..1.5), cho các thành phần khác nhấp nháy theo nhạc.</summary>
    public float PulseValue { get { return pulse; } }
    /// <summary>Mức năng lượng nhạc đã làm mượt (0..1).</summary>
    public float EnergyValue { get { return energy; } }

    BackgroundTheme Current
    {
        get
        {
            if (themes == null || themes.Count == 0) return null;
            return themes[Mathf.Clamp(index, 0, themes.Count - 1)];
        }
    }

    Camera cam;
    GameObject quad;
    Material mat;
    Texture2D curTex, prevTex;
    float blend = 1f;
    int index = -1;
    Vector2 uvScale = Vector2.one;
    float imageAspect = 16f / 9f;
    float lastAspect, lastOrtho;

    float pulse, energy, comboLevel, comboSmooth;
    float peak = 0.0001f, avg, lastBeat, beatClock;
    readonly float[] spec = new float[512];

    // equalizer
    Texture2D specTex;
    readonly Color32[] specPx = new Color32[16];
    readonly float[] bands = new float[16];
    readonly float[] bandPeak = new float[16];
    readonly float[] rawBands = new float[16];
    float specPeak = 0.0005f;

    // phản ứng khi trúng note: 4 ô xoay vòng (x, y theo màn hình 0..1, tuổi 0..1, độ mạnh)
    readonly Vector4[] hits = new Vector4[4];
    int hitIdx;

    // fever / miss
    int autoCombo, comboValue;
    float fever, glitch;
    bool feverOn;

    // sóng xung kích
    float ripple = 1f;
    bool rippleOn;
    float lastBurst = -10f;
    int lastBurstCombo;

    void Awake()
    {
        Instance = this;

        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        if (cam == null) { Debug.LogError("AnimatedBackground: không tìm thấy Camera."); enabled = false; return; }
        if (themes == null || themes.Count == 0) { Debug.LogError("AnimatedBackground: chưa có theme nào."); enabled = false; return; }

        if (shader == null) shader = Shader.Find("RhythmGame/AnimatedNeonBackground");
        if (shader == null) { Debug.LogError("AnimatedBackground: không tìm thấy shader AnimatedNeonBackground."); enabled = false; return; }

        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "AnimatedBackground";
        var col = quad.GetComponent<Collider>();
        if (col != null) Destroy(col);
        quad.transform.SetParent(cam.transform, false);
        quad.transform.localRotation = Quaternion.identity;

        var mr = quad.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        specTex = new Texture2D(16, 1, TextureFormat.RGBA32, false);
        specTex.filterMode = FilterMode.Point;
        specTex.wrapMode = TextureWrapMode.Clamp;

        mat = new Material(shader);
        mr.sharedMaterial = mat;

        int start = randomOnStart ? UnityEngine.Random.Range(0, themes.Count)
                                  : Mathf.Clamp(startIndex, 0, themes.Count - 1);
        SetTheme(start, true);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (mat != null) Destroy(mat);
        if (specTex != null) Destroy(specTex);
    }

    // ---------- API gọi từ gameplay ----------

    /// <summary>Chớp nền một nhịp. Gọi khi Perfect / Great / bắt đầu bài... (strength 0..1)</summary>
    public void Pulse(float strength = 1f)
    {
        pulse = Mathf.Min(1.5f, Mathf.Max(pulse, strength * pulseStrength));
    }

    /// <summary>Combo càng cao nền càng sáng và sôi động.</summary>
    public void SetCombo(int combo)
    {
        autoCombo = combo;
        ApplyCombo(combo);
    }

    /// <summary>Gọi khi miss: nền nhiễu hình ngắn, combo tự đếm về 0. NoteView tự gọi.</summary>
    public void Miss(float strength = 1f)
    {
        glitch = Mathf.Max(glitch, Mathf.Clamp01(strength) * missGlitch);
        autoCombo = 0;
        ApplyCombo(0);
    }

    void ApplyCombo(int combo)
    {
        comboValue = combo;
        comboLevel = Mathf.Clamp01(combo / 100f);

        if (combo < lastBurstCombo) lastBurstCombo = 0;
        if (burstEveryCombo > 0 && combo > 0 && combo % burstEveryCombo == 0 && combo != lastBurstCombo)
        {
            lastBurstCombo = combo;
            Burst();
        }
    }

    /// <summary>Phát một đợt sóng xung kích lan ra từ giữa màn hình (dùng cho mốc combo, drop, vào chorus...).</summary>
    public void Burst()
    {
        ripple = 0f;
        rippleOn = true;
        lastBurst = Time.time;
        Pulse(1f);
    }

    /// <summary>Gọi khi đánh trúng note: tạo vòng sóng trên sàn và trụ sáng tại vị trí đó. NoteView tự gọi.</summary>
    public void Hit(Vector3 worldPos, float strength = 1f)
    {
        if (cam == null || mat == null) return;
        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        hits[hitIdx] = new Vector4(vp.x, vp.y, 0f, Mathf.Clamp(strength, 0.2f, 1.5f));
        hitIdx = (hitIdx + 1) % hits.Length;
        Pulse(0.25f * strength);
        autoCombo++;
        ApplyCombo(autoCombo);
    }

    public void SetBpm(float newBpm)
    {
        bpm = Mathf.Max(30f, newBpm);
    }

    public void SetTheme(int i, bool instant = false)
    {
        if (themes == null || themes.Count == 0 || mat == null) return;
        i = Mathf.Clamp(i, 0, themes.Count - 1);
        var th = themes[i];
        if (th.image == null) { Debug.LogWarning("AnimatedBackground: theme '" + th.name + "' chưa gán ảnh."); return; }

        th.image.wrapMode = TextureWrapMode.Clamp;
        prevTex = (instant || curTex == null) ? th.image : curTex;
        curTex = th.image;
        blend = (instant || prevTex == curTex) ? 1f : 0f;
        index = i;

        if (th.autoPalette) ExtractPalette(th);
        if (th.setNoteStyle) NoteSkin.SetGlobalStyle(th.noteStyle);

        Layout();
    }

    // Trích 2 màu chủ đạo (rực và nổi bật nhất) + màu trung bình của ảnh, rồi gán cho palette / tia đèn / quầng sáng
    void ExtractPalette(BackgroundTheme th)
    {
        const int W = 48, H = 27;
        var rt = RenderTexture.GetTemporary(W, H, 0, RenderTextureFormat.ARGB32);
        var prevActive = RenderTexture.active;
        Graphics.Blit(th.image, rt);
        RenderTexture.active = rt;
        var tmp = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tmp.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        RenderTexture.active = prevActive;
        RenderTexture.ReleaseTemporary(rt);
        Color32[] px = tmp.GetPixels32();
        Destroy(tmp);

        const int BINS = 12;
        float[] binW = new float[BINS];
        Vector3[] binSum = new Vector3[BINS];
        Vector3 allSum = Vector3.zero;

        for (int k = 0; k < px.Length; k++)
        {
            Color c = px[k];
            allSum += new Vector3(c.r, c.g, c.b);
            Color.RGBToHSV(c, out float h, out float sat, out float v);
            float w = sat * v;
            w *= w;                       // ưu tiên điểm vừa rực vừa sáng (đèn neon, graffiti)
            if (w < 0.02f) continue;
            int bin = Mathf.Min(BINS - 1, (int)(h * BINS));
            binW[bin] += w;
            binSum[bin] += new Vector3(c.r, c.g, c.b) * w;
        }

        int a = 0;
        for (int b = 1; b < BINS; b++) if (binW[b] > binW[a]) a = b;
        if (binW[a] <= 0f) return;        // ảnh gần như xám: giữ màu đặt tay

        int second = -1;
        for (int b = 0; b < BINS; b++)
        {
            int dd = Mathf.Min((b - a + BINS) % BINS, (a - b + BINS) % BINS);
            if (dd < 2) continue;         // bỏ các màu quá gần màu chính
            if (binW[b] < binW[a] * 0.2f) continue;
            if (second < 0 || binW[b] > binW[second]) second = b;
        }

        Vector3 va = binSum[a] / binW[a];
        Color colA = new Color(va.x, va.y, va.z, 1f);
        Color colB;
        if (second >= 0)
        {
            Vector3 vb = binSum[second] / binW[second];
            colB = new Color(vb.x, vb.y, vb.z, 1f);
        }
        else
        {
            Color.RGBToHSV(colA, out float h, out float sat, out float v);
            colB = Color.HSVToRGB(Mathf.Repeat(h + 0.4f, 1f), sat, v);   // ảnh chỉ có một tông: lấy màu lệch tông
        }

        Vector3 avg = allSum / px.Length;
        th.paletteTop = new Color(avg.x, avg.y, avg.z, 1f);   // màu trung bình -> NoteSkin biết nền tối hay sáng
        th.paletteMiddle = colA;
        th.paletteBottom = colB;
        th.beamColorA = colB;
        th.beamColorB = colA;
        th.focusColor = Color.Lerp(colA, Color.white, 0.4f);
    }

    public void SetThemeByName(string themeName)
    {
        for (int i = 0; i < themes.Count; i++)
            if (themes[i].name == themeName) { SetTheme(i); return; }
        Debug.LogWarning("AnimatedBackground: không có theme tên '" + themeName + "'.");
    }

    [ContextMenu("Next Theme")]
    public void NextTheme()
    {
        if (themes == null || themes.Count == 0) return;
        SetTheme((index + 1) % themes.Count);
    }

    // ---------- Nội bộ ----------

    void Layout()
    {
        lastAspect = cam.aspect;
        lastOrtho = cam.orthographicSize;

        float dist = Mathf.Clamp(distance, cam.nearClipPlane + 0.5f,
                                 Mathf.Max(cam.nearClipPlane + 1f, cam.farClipPlane - 0.5f));
        float h = cam.orthographic
            ? cam.orthographicSize * 2f
            : 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float w = h * cam.aspect;

        quad.transform.localPosition = new Vector3(0f, 0f, dist);
        quad.transform.localScale = new Vector3(w, h, 1f);

        if (curTex != null)
        {
            imageAspect = (float)curTex.width / curTex.height;
            float ca = cam.aspect;
            // cover-fit: phủ kín màn hình, phần thừa của ảnh bị cắt
            uvScale = ca > imageAspect ? new Vector2(1f, imageAspect / ca) : new Vector2(ca / imageAspect, 1f);
        }
    }

    void Update()
    {
        if (mat == null) return;
        float dt = Time.deltaTime;

        if (!Mathf.Approximately(cam.aspect, lastAspect) || !Mathf.Approximately(cam.orthographicSize, lastOrtho))
            Layout();

        Analyze(dt);
        pulse *= Mathf.Exp(-pulseDecay * dt);
        comboSmooth = Mathf.Lerp(comboSmooth, comboLevel, 1f - Mathf.Exp(-2f * dt));
        if (blend < 1f) blend = Mathf.MoveTowards(blend, 1f, dt / Mathf.Max(0.01f, fadeTime));
        if (rippleOn)
        {
            ripple += dt / Mathf.Max(0.1f, rippleDuration);
            if (ripple >= 1f) { ripple = 1f; rippleOn = false; }
        }
        glitch = Mathf.Max(0f, glitch - dt * 3.5f);

        // Fever: vào mượt khi combo vượt ngưỡng, tạo một đợt sóng xung kích khi bắt đầu
        float fc = Mathf.Max(2, feverCombo);
        float feverTarget = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((comboValue - fc * 0.5f) / (fc * 0.5f)));
        fever = Mathf.Lerp(fever, feverTarget, 1f - Mathf.Exp(-2f * dt));
        if (!feverOn && fever > 0.6f) { feverOn = true; Burst(); }
        else if (feverOn && fever < 0.3f) feverOn = false;

        for (int k = 0; k < hits.Length; k++)
        {
            if (hits[k].w <= 0f) continue;
            hits[k].z += dt / Mathf.Max(0.1f, hitDuration);
            if (hits[k].z >= 1f) hits[k].w = 0f;
        }

        float e = Mathf.Clamp01(energy * 0.8f + comboSmooth * 0.4f);
        float t = Time.time;

        Vector2 sway = new Vector2(Mathf.Sin(t * swaySpeed) * swayAmount,
                                   Mathf.Sin(t * swaySpeed * 0.7f + 1.3f) * swayAmount * 0.6f);
        float zoom = baseZoom + pulse * pulseZoom + e * 0.01f;
        float hue = Mathf.Sin(t * 0.12f) * hueRange + e * hueRange * 0.3f;

        mat.SetTexture("_MainTex", curTex);
        mat.SetTexture("_PrevTex", prevTex);
        mat.SetFloat("_Blend", blend);
        mat.SetVector("_UVScale", new Vector4(uvScale.x, uvScale.y, 0f, 0f));
        mat.SetVector("_UVOffset", new Vector4(sway.x, sway.y, 0f, 0f));
        mat.SetFloat("_Zoom", zoom);
        mat.SetFloat("_Aspect", imageAspect);
        mat.SetFloat("_Pulse", pulse);
        mat.SetFloat("_Energy", e);
        mat.SetFloat("_HueShift", hue);
        mat.SetFloat("_NeonBoost", neonBoost);
        mat.SetFloat("_FloorWave", floorWave);
        mat.SetFloat("_BeamStrength", beamStrength);
        mat.SetFloat("_Flicker", flicker);
        mat.SetFloat("_LaneCenter", laneCenterX);
        mat.SetFloat("_LaneHalf", laneHalfWidth);
        mat.SetFloat("_LaneSoft", laneSoftness);
        mat.SetFloat("_LaneDim", laneDim);
        mat.SetFloat("_Vignette", vignette);

        mat.SetTexture("_SpecTex", specTex);
        mat.SetFloat("_ScreenAspect", cam.aspect);
        mat.SetFloat("_BarStrength", barsStrength);
        mat.SetFloat("_BarHeight", barHeight);
        mat.SetFloat("_BarInner", barInner);
        mat.SetFloat("_ParticleStrength", particleStrength);
        mat.SetFloat("_Aberration", aberration);
        mat.SetFloat("_FocusStrength", focusStrength);
        mat.SetFloat("_Twinkle", twinkle);
        mat.SetVector("_Ripple", new Vector4(ripple, rippleOn ? rippleStrength : 0f, 0f, 0f));
        mat.SetFloat("_HitStrength", hitStrength);
        mat.SetFloat("_Fever", fever * feverStrength);
        mat.SetFloat("_Glitch", glitch);
        mat.SetFloat("_FloorGrid", floorGrid);
        for (int k = 0; k < hits.Length; k++) mat.SetVector("_Hit" + k, hits[k]);

        var th = Current;
        if (th != null)
        {
            mat.SetFloat("_FloorY", th.floorY);
            mat.SetVector("_Focus", new Vector4(th.focusPoint.x, th.focusPoint.y, th.focusRadius, 0f));
            mat.SetColor("_FocusCol", th.focusColor);
            mat.SetVector("_TwinkleRect", th.twinkleRect);
            mat.SetFloat("_Rain", th.rain);
            mat.SetColor("_BeamColA", th.beamColorA);
            mat.SetColor("_BeamColB", th.beamColorB);
            for (int k = 0; k < 4; k++)
            {
                Vector2 o = (th.beamOrigins != null && k < th.beamOrigins.Length) ? th.beamOrigins[k] : new Vector2(0.5f, -1f);
                mat.SetVector("_BeamO" + k, new Vector4(o.x, o.y, 0f, 0f));
            }
        }
    }

    // 16 dải tần (chia theo thang log) cho equalizer. Mỗi dải tự cân bằng âm lượng nên đều nhảy đẹp.
    void UpdateBands(float dt, bool have)
    {
        if (have)
        {
            float maxRaw = 0f;
            for (int k = 0; k < 16; k++)
            {
                int a = Mathf.Max(1, Mathf.FloorToInt(Mathf.Pow(2f, k * 7f / 16f)));
                int b = Mathf.Max(a + 1, Mathf.FloorToInt(Mathf.Pow(2f, (k + 1) * 7f / 16f)));
                b = Mathf.Min(b, spec.Length);
                float sum = 0f;
                for (int n = a; n < b; n++) sum += spec[n];
                rawBands[k] = sum / Mathf.Max(1, b - a);
                maxRaw = Mathf.Max(maxRaw, rawBands[k]);
            }
            specPeak = Mathf.Max(maxRaw, specPeak * Mathf.Exp(-0.3f * dt), 0.0005f);

            for (int k = 0; k < 16; k++)
            {
                bandPeak[k] = Mathf.Max(rawBands[k], bandPeak[k] * Mathf.Exp(-0.2f * dt));
                float denom = Mathf.Max(bandPeak[k], specPeak * 0.1f);
                float target = Mathf.Sqrt(Mathf.Clamp01(rawBands[k] / denom));
                float rate = target > bands[k] ? 30f : 8f;
                bands[k] = Mathf.Lerp(bands[k], target, 1f - Mathf.Exp(-rate * dt));
            }
        }
        else
        {
            for (int k = 0; k < 16; k++)
                bands[k] = Mathf.Lerp(bands[k], 0f, 1f - Mathf.Exp(-6f * dt));
        }

        // dự phòng khi không có phổ (dùng chế độ BPM): thanh nhảy theo mỗi lần chớp
        float tNow = Time.time;
        for (int k = 0; k < 16; k++)
        {
            float fake = pulse * 0.6f * (0.6f + 0.4f * Mathf.Sin(tNow * 3f + k * 0.9f));
            if (fake > bands[k]) bands[k] = fake;
            byte v = (byte)Mathf.RoundToInt(Mathf.Clamp01(bands[k]) * 255f);
            specPx[k] = new Color32(v, v, v, 255);
        }
        specTex.SetPixels32(specPx);
        specTex.Apply(false);
    }

    // Bắt beat từ dải bass của phổ âm thanh, tự cân bằng âm lượng nên bài nào cũng chạy được
    void Analyze(float dt)
    {
        float level = 0f;
        bool haveSpec = false;

        if (useAudioSpectrum)
        {
            bool ok = true;
            if (audioSource != null)
            {
                if (audioSource.isPlaying) audioSource.GetSpectrumData(spec, 0, FFTWindow.BlackmanHarris);
                else ok = false;
            }
            else
            {
                AudioListener.GetSpectrumData(spec, 0, FFTWindow.BlackmanHarris);
            }

            if (ok)
            {
                haveSpec = true;
                float bass = 0f;
                for (int i = 0; i < 4; i++) bass += spec[i];   // 4 bin đầu ~ dưới 170 Hz
                bass *= 0.25f;

                peak = Mathf.Max(bass, peak * Mathf.Exp(-0.25f * dt), 0.0001f);
                level = Mathf.Clamp01(bass / peak);

                float thr = (avg * 1.3f + 0.08f) / beatSensitivity;
                if (level > thr && level > 0.4f && Time.time - lastBeat > 0.17f)
                {
                    lastBeat = Time.time;
                    Pulse(level);
                    if (autoBurstOnBigBeat && level > 0.92f && Time.time - lastBurst > 2.5f) Burst();
                }
                avg = Mathf.Lerp(avg, level, dt * 2f);
            }
        }

        UpdateBands(dt, haveSpec);

        energy = Mathf.Lerp(energy, level, 1f - Mathf.Exp(-8f * dt));

        if (useBpmPulse)
        {
            beatClock += dt;
            float period = 60f / Mathf.Max(30f, bpm);
            if (beatClock >= period)
            {
                beatClock -= period;
                Pulse(0.8f);
            }
        }
    }
}