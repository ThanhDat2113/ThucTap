using System.Collections.Generic;
using UnityEngine;
using RhythmGame;

/// <summary>
/// Làm MỌI cảnh background "sống" theo gameplay:
///  - Đập theo nhịp (beat) của bài, đồng bộ với SongTime của GameManager (tự dừng khi pause).
///  - Phản ứng theo âm trầm (bass) của nhạc.
///  - Càng combo cao nền càng sáng / mạnh hơn.
///  - Chớp đỏ khi Miss, chớp nhẹ khi Perfect.
///  - Vòng sóng toả ra theo nhịp.
/// Hỗ trợ: Synthwave (mặt trời, lưới, sao), cực quang, mây, tuyết, mưa, hoa, bokeh, đom đóm, núi, bụi, trăng, nền gradient.
///
/// CÁCH DÙNG: gắn vào Main Camera (hoặc để WorldBackgroundSelector tự thêm: bật "Dynamic Background").
/// Ô GameManager / JudgementSystem để trống thì tự tìm. Không đụng tới camera, lane hay note.
/// </summary>
public class BackgroundBeatReactive : MonoBehaviour
{
    [Header("Nguồn dữ liệu (để trống = tự tìm)")]
    public GameManager gameManager;
    public JudgementSystem judgement;

    [Header("Bật / tắt từng hiệu ứng")]
    public bool reactSun = true;
    public bool reactGrid = true;
    public bool reactStars = true;
    public bool reactGradient = true;
    public bool reactAurora = true;
    [Tooltip("Mây, tuyết, mưa, hoa, bokeh, đom đóm, núi, bụi, trăng")]
    public bool reactScene = true;
    public bool beatRings = true;
    public bool judgementFlash = true;
    public bool useAudioSpectrum = true;

    [Header("Nhịp")]
    [Tooltip("Càng lớn xung nhịp tắt càng nhanh (gọn, giật). Nhỏ = mượt, kéo dài")]
    [Range(1f, 12f)] public float pulseDecay = 5f;

    [Header("Cường độ")]
    [Tooltip("Độ mạnh hiệu ứng khi combo = 0")]
    [Range(0f, 1f)] public float baseIntensity = 0.55f;
    [Tooltip("Combo đạt bao nhiêu thì hiệu ứng mạnh tối đa")]
    public int comboForMaxIntensity = 40;
    public float intensityResponse = 2f;
    [Tooltip("Nhân độ mạnh của TOÀN BỘ hiệu ứng. Thấy nền động chưa đủ rõ thì tăng lên")]
    [Range(0.2f, 3f)] public float strength = 1.5f;
    [Tooltip("Nhân thêm độ mạnh cho nhóm mây / tuyết / mưa / hoa / bokeh / đom đóm / núi")]
    [Range(0f, 2f)] public float sceneStrength = 1f;

    [Header("Mặt trời")]
    [Tooltip("Mặt trời phình to thêm bao nhiêu % mỗi nhịp")]
    public float sunPulse = 0.08f;

    [Header("Lưới neon")]
    [Range(0f, 1f)] public float gridBrighten = 0.45f;
    [Tooltip("Lưới chạy nhanh thêm tối đa bao nhiêu lần khi combo cao")]
    public float comboScrollBoost = 0.8f;
    public float bassScrollBoost = 0.6f;

    [Header("Nền gradient")]
    [Tooltip("Nền tối đi bao nhiêu giữa hai nhịp (0 = không nhấp nháy)")]
    [Range(0f, 0.5f)] public float gradientDip = 0.15f;

    [Header("Vòng sóng theo nhịp")]
    public int ringEveryBeats = 2;
    [Tooltip("Kích thước vòng = % chiều cao màn hình (lúc đầu / lúc tan)")]
    public float ringStartSize = 0.1f;
    public float ringEndSize = 0.9f;
    [Range(0f, 1f)] public float ringAlpha = 0.5f;
    public float ringDistance = 39f;

    [Header("Chớp theo phán định")]
    public Color missColor = new Color(1f, 0.1f, 0.15f, 1f);
    [Range(0f, 1f)] public float missAlpha = 0.2f;
    public Color perfectColor = new Color(1f, 0.6f, 0.95f, 1f);
    [Range(0f, 1f)] public float perfectAlpha = 0.05f;
    public float overlayDistance = 34f;

    [Header("Âm thanh")]
    [Tooltip("Độ nhạy với âm trầm. Thấy nền không phản ứng theo nhạc thì tăng lên")]
    public float audioSensitivity = 8f;

    [Header("Gỡ lỗi")]
    [Tooltip("In vào Console mỗi 2 giây: script có tìm thấy GameManager / các background không, nhịp và combo hiện tại")]
    public bool debugLog = false;

    [Header("Hai màu luân phiên theo nhịp")]
    public Color colorA = new Color(1f, 0.3f, 0.8f, 1f);
    public Color colorB = new Color(0.3f, 1f, 1f, 1f);

    // ---- trạng thái ----
    Camera cam;
    Transform camTf;
    float nextFind;
    float nextLog;
    int combo;
    float intensity;
    float kick;
    float bass;
    float energy;   // tổng hợp nhịp + bass + combo, dùng cho nhóm cảnh khác
    int lastBeat = int.MinValue;
    int colorIndex;
    readonly float[] spectrum = new float[256];

    // ---- đối tượng background tìm được ----
    BackgroundSynthwave syn;
    float baseScroll;
    Color baseGrid;
    readonly List<LineRenderer> vLines = new List<LineRenderer>();

    BackgroundNightSky sky;
    Color baseStar;

    BackgroundAurora aurora;
    float baseAuroraIntensity, baseAuroraWave;

    WorldBackgroundGradientPro grad;
    Material gradMat;

    Transform sunTf, sunCaptured;
    Vector3 sunBase;

    // ---- nhóm sprite theo tên (mây, tuyết, mưa, ...) ----
    class Group
    {
        public readonly string prefix;
        public readonly bool rewritesColor, rewritesScale; // script gốc có tự ghi lại màu / scale mỗi frame không
        public readonly float tint, alphaBoost, scaleBoost;
        public readonly List<SpriteRenderer> items = new List<SpriteRenderer>();
        public Group(string p, bool rc, bool rs, float t, float a, float s)
        { prefix = p; rewritesColor = rc; rewritesScale = rs; tint = t; alphaBoost = a; scaleBoost = s; }
    }

    readonly Group[] groups =
    {
        new Group("Cloud_",    false, false, 0.35f, 0.15f, 0f),
        new Group("Snow_",     false, false, 0.30f, 0.40f, 0.35f),
        new Group("Rain_",     false, false, 0.30f, 0.35f, 0f),
        new Group("Petal_",    false, true,  0.25f, 0.10f, 0.12f),
        new Group("Bokeh_",    true,  true,  0.30f, 0.50f, 0.12f),
        new Group("Firefly_",  true,  false, 0.20f, 0.50f, 0.35f),
        new Group("Mountain_", false, false, 0.18f, 0f,    0f),
        new Group("Dust_",     false, false, 0.30f, 0.50f, 0.50f),
        new Group("Moon",      false, false, 0.20f, 0f,    0.04f),
    };
    readonly Dictionary<SpriteRenderer, Color> baseColors = new Dictionary<SpriteRenderer, Color>();
    readonly Dictionary<SpriteRenderer, Vector3> baseScales = new Dictionary<SpriteRenderer, Vector3>();

    // ---- vòng sóng + lớp chớp ----
    class Ring { public SpriteRenderer sr; public float age, life; public bool active; }
    Ring[] rings;
    Sprite ringSprite;
    SpriteRenderer flashSr;
    Color flashColor;
    float flashAlpha;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundBeatReactive: không tìm thấy Camera."); enabled = false; return; }
        camTf = cam.transform;
        intensity = baseIntensity * strength;

        if (!gameManager) gameManager = FindOne<GameManager>();
        if (!judgement) judgement = FindOne<JudgementSystem>();
        if (judgement) judgement.OnJudged += OnJudged;
        if (gameManager) gameManager.OnStateChanged += OnStateChanged;

        BuildRingsAndFlash();
    }

    void OnDestroy()
    {
        if (judgement) judgement.OnJudged -= OnJudged;
        if (gameManager) gameManager.OnStateChanged -= OnStateChanged;
    }

    void OnDisable() { Restore(); }

    static T FindOne<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>();
#else
        return UnityEngine.Object.FindObjectOfType<T>();
#endif
    }

    // Trả các giá trị đã chỉnh về như cũ khi tắt script
    void Restore()
    {
        if (syn) { syn.scrollSpeed = baseScroll; syn.gridColor = baseGrid; }
        if (sky) sky.starColor = baseStar;
        if (aurora) { aurora.intensity = baseAuroraIntensity; aurora.waveSpeed = baseAuroraWave; }
        if (sunTf && sunCaptured == sunTf) sunTf.localScale = sunBase;
        if (gradMat) gradMat.color = Color.white;
        foreach (var g in groups)
            foreach (var r in g.items)
            {
                if (!r) continue;
                if (!g.rewritesColor && baseColors.TryGetValue(r, out Color c)) r.color = c;
                if (!g.rewritesScale && baseScales.TryGetValue(r, out Vector3 s)) r.transform.localScale = s;
            }
    }

    float ViewHeightAt(float d)
    {
        return cam.orthographic ? cam.orthographicSize * 2f
            : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    // ---------- Sự kiện game ----------
    void OnStateChanged(GameState s)
    {
        if (s == GameState.Countdown)
        {
            combo = 0;
            kick = 0f;
            lastBeat = int.MinValue;
        }
    }

    void OnJudged(Judgement j, int points, bool isHit, NoteOwner owner)
    {
        switch (j)
        {
            case Judgement.Perfect:
                combo++;
                kick = 1f;
                if (judgementFlash) Flash(perfectColor, perfectAlpha);
                break;
            case Judgement.Good:
            case Judgement.HoldComplete:
                combo++;
                kick = Mathf.Max(kick, 0.5f);
                break;
            case Judgement.Bad:
                combo = 0;
                break;
            case Judgement.Miss:
                combo = 0;
                if (judgementFlash) Flash(missColor, missAlpha);
                break;
        }
    }

    void Flash(Color c, float alpha)
    {
        flashColor = c;
        flashAlpha = Mathf.Max(flashAlpha, alpha);
    }

    // ---------- Tìm các background đang chạy (Selector có thể thêm / xoá lúc đang chơi) ----------
    T GetFrom<T>() where T : Component
    {
        var c = GetComponent<T>();
        if (!c) c = camTf.GetComponent<T>();
        return c;
    }

    void FindTargets()
    {
        var s = GetFrom<BackgroundSynthwave>();
        if (s != syn)
        {
            syn = s;
            vLines.Clear();
            if (syn) { baseScroll = syn.scrollSpeed; baseGrid = syn.gridColor; }
        }
        if (syn && vLines.Count == 0)
        {
            foreach (Transform c in camTf)
                if (c.name.StartsWith("GridV_", System.StringComparison.Ordinal))
                {
                    var lr = c.GetComponent<LineRenderer>();
                    if (lr) vLines.Add(lr);
                }
        }

        var k = GetFrom<BackgroundNightSky>();
        if (k != sky)
        {
            sky = k;
            if (sky) baseStar = sky.starColor;
        }

        var a = GetFrom<BackgroundAurora>();
        if (a != aurora)
        {
            aurora = a;
            if (aurora) { baseAuroraIntensity = aurora.intensity; baseAuroraWave = aurora.waveSpeed; }
        }

        var g = GetFrom<WorldBackgroundGradientPro>();
        if (g != grad)
        {
            grad = g;
            gradMat = null;
            if (grad) grad.breathe = 0f; // để script này tự điều khiển độ sáng nền
        }

        if (!sunTf)
        {
            var t = camTf.Find("SynthSun");
            if (!t) t = camTf.Find("Sun");
            sunTf = t;
        }

        CollectGroups();
    }

    // Gom các sprite của mây / tuyết / mưa / ... và nhớ màu + scale gốc (chỉ nhớ lần đầu thấy, tránh nhớ nhầm giá trị đã chỉnh)
    void CollectGroups()
    {
        foreach (var gr in groups) gr.items.Clear();

        foreach (Transform c in camTf)
        {
            foreach (var gr in groups)
            {
                if (!c.name.StartsWith(gr.prefix, System.StringComparison.Ordinal)) continue;
                var sr = c.GetComponent<SpriteRenderer>();
                if (sr)
                {
                    gr.items.Add(sr);
                    if (!gr.rewritesColor && !baseColors.ContainsKey(sr)) baseColors[sr] = sr.color;
                    if (!gr.rewritesScale && !baseScales.ContainsKey(sr)) baseScales[sr] = c.localScale;
                }
                break;
            }
        }
        Prune(baseColors);
        Prune(baseScales);
    }

    static void Prune<T>(Dictionary<SpriteRenderer, T> d)
    {
        List<SpriteRenderer> dead = null;
        foreach (var key in d.Keys)
            if (!key)
            {
                if (dead == null) dead = new List<SpriteRenderer>();
                dead.Add(key);
            }
        if (dead != null) foreach (var key in dead) d.Remove(key);
    }

    // ---------- Vòng sóng + lớp chớp ----------
    void BuildRingsAndFlash()
    {
        int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x / (n - 1f) - 0.5f) * 2f, v = (y / (n - 1f) - 0.5f) * 2f;
            float d = Mathf.Sqrt(u * u + v * v);
            float t = (d - 0.9f) / 0.05f;
            float a = Mathf.Exp(-t * t) * Mathf.Clamp01((1f - d) * 15f);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        ringSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 1f);

        rings = new Ring[4];
        for (int i = 0; i < rings.Length; i++)
        {
            var go = new GameObject("BeatRing_" + i);
            go.transform.SetParent(camTf, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ringSprite;
            sr.sortingOrder = -86;
            sr.enabled = false;
            rings[i] = new Ring { sr = sr };
        }

        var ftex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        ftex.SetPixels(px);
        ftex.Apply();

        float h = ViewHeightAt(overlayDistance);
        var fgo = new GameObject("BeatFlash");
        fgo.transform.SetParent(camTf, false);
        fgo.transform.localPosition = new Vector3(0f, 0f, overlayDistance);
        fgo.transform.localScale = new Vector3(h * cam.aspect * 1.3f / 4f, h * 1.3f / 4f, 1f);
        flashSr = fgo.AddComponent<SpriteRenderer>();
        flashSr.sprite = Sprite.Create(ftex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1f);
        flashSr.sortingOrder = -79;
        flashSr.color = Color.clear;
    }

    void SpawnRing(float spb)
    {
        Ring r = null;
        foreach (var x in rings) if (!x.active) { r = x; break; }
        if (r == null) r = rings[0];

        float vh = ViewHeightAt(ringDistance);
        Vector2 center = sunTf ? new Vector2(sunTf.localPosition.x, sunTf.localPosition.y)
                               : new Vector2(0f, vh * 0.12f);
        r.sr.transform.localPosition = new Vector3(center.x, center.y, ringDistance);
        r.age = 0f;
        r.life = Mathf.Clamp(spb * 2f, 0.5f, 1.6f);
        r.active = true;
        r.sr.enabled = true;
    }

    void UpdateRings(float dt)
    {
        if (rings == null) return;
        float vh = ViewHeightAt(ringDistance);
        foreach (var r in rings)
        {
            if (!r.active) continue;
            r.age += dt;
            float t = r.age / r.life;
            if (t >= 1f) { r.active = false; r.sr.enabled = false; continue; }

            float ease = 1f - (1f - t) * (1f - t);
            float size = vh * Mathf.Lerp(ringStartSize, ringEndSize, ease);
            r.sr.transform.localScale = Vector3.one * (size / ringSprite.bounds.size.x);
            Color c = ColorForBeat(colorIndex);
            c.a = ringAlpha * intensity * (1f - t) * (1f - t);
            r.sr.color = c;
        }
    }

    Color ColorForBeat(int i) { return (i & 1) == 0 ? colorA : colorB; }

    // ---------- Vòng lặp chính ----------
    void Update()
    {
        if (!cam) return;
        if (Time.unscaledTime >= nextFind) { FindTargets(); nextFind = Time.unscaledTime + 1f; }

        float dt = Time.deltaTime; // = 0 khi pause nên mọi thứ tự đứng yên
        bool paused = PauseManager.IsPaused;
        bool playing = gameManager != null && gameManager.State == GameState.Playing;

        // 1) Xung nhịp, đồng bộ với đồng hồ bài hát
        float pulse = 0f;
        if (playing && gameManager.chart != null)
        {
            float spb = gameManager.chart.SecondsPerBeat;
            if (spb > 0.0001f)
            {
                float beatPos = (gameManager.SongTime - gameManager.chart.BeatToTime(0)) / spb;
                float phase = beatPos - Mathf.Floor(beatPos);
                pulse = Mathf.Exp(-phase * pulseDecay);

                int beat = Mathf.FloorToInt(beatPos);
                if (beat != lastBeat)
                {
                    lastBeat = beat;
                    if (beat >= 0 && !paused)
                    {
                        colorIndex++;
                        if (beatRings && ringEveryBeats > 0 && beat % ringEveryBeats == 0) SpawnRing(spb);
                    }
                }
            }
        }

        // 2) Âm trầm của nhạc
        if (useAudioSpectrum && !paused)
        {
            float target = 0f;
            var src = gameManager != null ? gameManager.musicSource : null;
            if (playing && src != null && src.isPlaying)
            {
                src.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
                float sum = 0f;
                for (int i = 0; i < 4; i++) sum += spectrum[i];
                target = Mathf.Clamp01(sum * audioSensitivity);
            }
            float speed = target > bass ? 20f : 6f;
            bass = Mathf.Lerp(bass, target, 1f - Mathf.Exp(-dt * speed));
        }

        // 3) Cường độ theo combo
        float comboT = Mathf.Clamp01(combo / (float)Mathf.Max(1, comboForMaxIntensity));
        intensity = Mathf.MoveTowards(intensity, Mathf.Lerp(baseIntensity, 1f, comboT) * strength, intensityResponse * dt);
        kick = Mathf.Max(0f, kick - dt * 4f);
        energy = Mathf.Clamp01(intensity * (pulse + bass * 0.6f) + kick * 0.4f);

        ApplySun(pulse);
        ApplyGrid(pulse, comboT);
        ApplyStars(pulse);
        ApplyAurora(pulse, comboT);
        ApplyGradient(pulse, playing);
        UpdateRings(dt);
        UpdateFlash(dt, paused);

        if (debugLog && Time.unscaledTime >= nextLog)
        {
            nextLog = Time.unscaledTime + 2f;
            float spbDbg = (gameManager != null && gameManager.chart != null) ? gameManager.chart.SecondsPerBeat : -1f;
            int items = 0;
            foreach (var gr in groups) items += gr.items.Count;
            Debug.Log("[BeatReactive] GameManager=" + (gameManager ? "ok" : "KHÔNG THẤY")
                + " | state=" + (gameManager ? gameManager.State.ToString() : "-")
                + " | spb=" + spbDbg.ToString("F3")
                + " | songTime=" + (gameManager ? gameManager.SongTime.ToString("F2") : "-")
                + " | pulse=" + pulse.ToString("F2") + " bass=" + bass.ToString("F2")
                + " combo=" + combo + " intensity=" + intensity.ToString("F2")
                + " | synthwave=" + (syn ? "có" : "không") + " sao=" + (sky ? "có" : "không")
                + " aurora=" + (aurora ? "có" : "không") + " gradient=" + (grad ? "có" : "không")
                + " mặt trời=" + (sunTf ? "có" : "không") + " sprite nhóm=" + items);
        }
    }

    // Chạy SAU Update của các script background -> ghi đè lên màu / scale mà chúng vừa tính xong
    void LateUpdate()
    {
        if (!reactScene || camTf == null) return;
        float e = Mathf.Clamp01(energy * sceneStrength);
        Color beat = ColorForBeat(colorIndex);

        foreach (var g in groups)
        {
            for (int i = 0; i < g.items.Count; i++)
            {
                var r = g.items[i];
                if (!r) continue;

                Color cur;
                if (g.rewritesColor) cur = r.color;
                else if (!baseColors.TryGetValue(r, out cur)) continue;

                Color c = Color.Lerp(cur, beat, g.tint * e);
                c.a = Mathf.Clamp01(cur.a * (1f + g.alphaBoost * e));
                r.color = c;

                if (g.scaleBoost > 0f)
                {
                    Vector3 cs;
                    if (g.rewritesScale) cs = r.transform.localScale;
                    else if (!baseScales.TryGetValue(r, out cs)) continue;
                    r.transform.localScale = cs * (1f + g.scaleBoost * e);
                }
            }
        }
    }

    void ApplySun(float pulse)
    {
        if (!reactSun || !sunTf) return;
        if (sunTf != sunCaptured) { sunCaptured = sunTf; sunBase = sunTf.localScale; }
        float k = 1f + intensity * sunPulse * (pulse + kick * 0.8f + bass * 1.2f);
        sunTf.localScale = sunBase * k;
    }

    void ApplyGrid(float pulse, float comboT)
    {
        if (!reactGrid || !syn) return;
        syn.scrollSpeed = baseScroll * (1f + comboScrollBoost * comboT + bassScrollBoost * bass);

        float w = Mathf.Clamp01(intensity * (pulse * gridBrighten + bass * 0.3f) + kick * 0.3f);
        Color t = Color.Lerp(baseGrid, Color.white, w);
        t.a = baseGrid.a;
        syn.gridColor = t; // BackgroundSynthwave đọc lại màu này mỗi frame cho các đường ngang
        for (int i = 0; i < vLines.Count; i++)
            if (vLines[i]) vLines[i].endColor = t;
    }

    void ApplyStars(float pulse)
    {
        if (!reactStars || !sky) return;
        sky.starColor = Color.Lerp(baseStar, ColorForBeat(colorIndex), Mathf.Clamp01(intensity * pulse * 0.6f));
    }

    void ApplyAurora(float pulse, float comboT)
    {
        if (!reactAurora || !aurora) return;
        aurora.intensity = Mathf.Clamp01(baseAuroraIntensity * (1f + intensity * (pulse * 0.5f + bass * 0.8f)));
        aurora.waveSpeed = baseAuroraWave * (1f + comboT);
    }

    void ApplyGradient(float pulse, bool playing)
    {
        if (!reactGradient || !grad) return;
        if (!gradMat)
        {
            var t = camTf.Find("WorldBackgroundGradientPro");
            if (t)
            {
                var mr = t.GetComponent<MeshRenderer>();
                if (mr) gradMat = mr.sharedMaterial;
            }
            if (!gradMat) return;
        }
        float dip = playing ? gradientDip * intensity * (1f - pulse) : 0f;
        float k = Mathf.Clamp01(1f - dip + kick * 0.05f);
        gradMat.color = new Color(k, k, k, 1f);
    }

    void UpdateFlash(float dt, bool paused)
    {
        if (!flashSr) return;
        flashAlpha *= Mathf.Exp(-dt * 8f);
        Color c = flashColor;
        c.a = paused ? 0f : flashAlpha;
        flashSr.color = c;
    }
}