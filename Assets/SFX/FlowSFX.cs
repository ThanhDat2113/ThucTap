using UnityEngine;
using RhythmGame;

/// <summary>
/// Âm thanh cho Flow, TỰ TỔNG HỢP bằng code (không cần file âm thanh):
///  - Vào Flow: tiếng "boom" trầm + quét lên + chuông lấp lánh.
///  - Pháo hoa: tiếng vút lên khi phóng, tiếng nổ lốp bốp khi nổ (đồng bộ với FlowVFX).
///  - Trong Flow: lớp âm lấp lánh nhẹ chạy nền (mờ dần vào / ra).
///  - Hết Flow: tiếng "tụt năng lượng" đi xuống.
/// Muốn dùng âm thanh của riêng bạn thì kéo file vào các ô Clip bên dưới, ô nào trống sẽ dùng âm tổng hợp.
/// Đặt vào bất kỳ GameObject nào trong scene (cần có AudioListener, thường ở Main Camera).
/// </summary>
public class FlowSFX : MonoBehaviour
{
    [SerializeField] private FlowSystem flowSystem;

    [Header("Âm lượng (0-1)")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float activateVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float fireworkVolume = 0.6f;
    [SerializeField, Range(0f, 1f)] private float loopVolume = 0.25f;
    [SerializeField, Range(0f, 1f)] private float endVolume = 0.7f;

    [Header("Bật / tắt")]
    [SerializeField] private bool useFireworkSounds = true;
    [SerializeField] private bool useLoop = true;
    [SerializeField] private bool useEndSound = true;

    [Header("File âm thanh của bạn (để trống = dùng âm tổng hợp bằng code)")]
    [SerializeField] private AudioClip activateClip;
    [SerializeField] private AudioClip launchClip;
    [SerializeField] private AudioClip popClip;
    [SerializeField] private AudioClip loopClip;
    [SerializeField] private AudioClip endClip;

    private const int SampleRate = 44100;
    private const int PoolSize = 12;

    private AudioSource[] pool;
    private AudioSource loopSource;
    private int cursor;
    private bool active;

    private AudioClip synthActivate, synthLoop, synthEnd;
    private AudioClip[] synthLaunch, synthPop;

    private void Awake()
    {
        pool = new AudioSource[PoolSize];
        for (int i = 0; i < PoolSize; i++) pool[i] = NewSource(false);
        loopSource = NewSource(true);

        synthActivate = MakeActivate();
        synthEnd = MakeEnd();
        synthLoop = MakeLoop();
        synthLaunch = new[] { MakeLaunch(3), MakeLaunch(7), MakeLaunch(13) };
        synthPop = new[] { MakePop(3, 150f, 260f, 1f), MakePop(7, 115f, 200f, 1.1f), MakePop(13, 180f, 320f, 0.9f), MakePop(21, 130f, 150f, 1.2f) };
    }

    private AudioSource NewSource(bool loop)
    {
        AudioSource src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        return src;
    }

    private void Start()
    {
        if (flowSystem == null) flowSystem = FindFirstObjectByType<FlowSystem>();
        if (flowSystem == null)
        {
            Debug.LogWarning("[FlowSFX] Không tìm thấy FlowSystem trong scene - âm thanh Flow sẽ không tự phát.");
            return;
        }
        flowSystem.OnFlowStateChanged += OnFlow;
    }

    private void OnEnable()
    {
        FlowVFX.FireworkLaunched += OnFireworkLaunched;
        FlowVFX.FireworkExploded += OnFireworkExploded;
    }

    private void OnDisable()
    {
        FlowVFX.FireworkLaunched -= OnFireworkLaunched;
        FlowVFX.FireworkExploded -= OnFireworkExploded;
    }

    private void OnDestroy()
    {
        if (flowSystem != null) flowSystem.OnFlowStateChanged -= OnFlow;
    }

    /// <summary>Nghe thử ngay không cần đầy thanh Flow: bấm ⋮ trên component (lúc đang Play) -> Test Flow Sound.</summary>
    [ContextMenu("Test Flow Sound")]
    private void TestSound() { OnFlow(true); CancelInvoke(nameof(EndTest)); Invoke(nameof(EndTest), 4f); }
    private void EndTest() { OnFlow(false); }

    // ================= Phát âm =================

    private void OnFlow(bool on)
    {
        active = on;
        if (on)
        {
            Play(activateClip != null ? activateClip : synthActivate, activateVolume, 1f);
            if (useLoop)
            {
                AudioClip lc = loopClip != null ? loopClip : synthLoop;
                if (loopSource.clip != lc) loopSource.clip = lc;
                loopSource.volume = 0f;
                if (!loopSource.isPlaying) loopSource.Play();
            }
        }
        else if (useEndSound)
        {
            Play(endClip != null ? endClip : synthEnd, endVolume, 1f);
        }
    }

    private void OnFireworkLaunched()
    {
        if (!useFireworkSounds) return;
        AudioClip c = launchClip != null ? launchClip : synthLaunch[Random.Range(0, synthLaunch.Length)];
        Play(c, fireworkVolume * 0.75f, Random.Range(0.95f, 1.1f));
    }

    private void OnFireworkExploded()
    {
        if (!useFireworkSounds) return;
        AudioClip c = popClip != null ? popClip : synthPop[Random.Range(0, synthPop.Length)];
        Play(c, fireworkVolume, Random.Range(0.93f, 1.08f));
    }

    private void Play(AudioClip clip, float volume, float pitch)
    {
        if (clip == null) return;
        // tìm nguồn đang rảnh, nếu không có thì dùng vòng tròn
        AudioSource src = null;
        for (int i = 0; i < pool.Length; i++)
        {
            cursor = (cursor + 1) % pool.Length;
            if (!pool[cursor].isPlaying) { src = pool[cursor]; break; }
        }
        if (src == null) { cursor = (cursor + 1) % pool.Length; src = pool[cursor]; }
        src.clip = clip;
        src.volume = volume * masterVolume;
        src.pitch = pitch;
        src.Play();
    }

    private void Update()
    {
        // lớp âm nền: mờ dần vào khi Flow, mờ dần ra khi hết
        if (loopSource != null && loopSource.isPlaying)
        {
            float target = (active && useLoop) ? loopVolume * masterVolume : 0f;
            loopSource.volume = Mathf.MoveTowards(loopSource.volume, target, Time.unscaledDeltaTime * 1.5f);
            if (!active && loopSource.volume <= 0.001f) loopSource.Stop();
        }
    }

    // ================= Tổng hợp âm thanh =================

    private static float Soft(float x) { return (float)System.Math.Tanh(x); }

    private static AudioClip Build(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>Vào Flow: boom trầm + tiếng ồn + quét lên + chuông lấp lánh.</summary>
    private static AudioClip MakeActivate()
    {
        int n = (int)(1.2f * SampleRate);
        float[] d = new float[n];
        System.Random rng = new System.Random(11);
        float ph = 0f, ph2 = 0f, lp = 0f;
        float[] bell = { 1568f, 2093f, 2637f, 3136f, 3951f };

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;

            float f = Mathf.Lerp(130f, 45f, 1f - Mathf.Exp(-t * 6f));
            ph += 2f * Mathf.PI * f / SampleRate;
            float boom = Mathf.Sin(ph) * Mathf.Exp(-t * 4.5f) * 0.9f;

            float nz = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (nz - lp) * 0.12f;
            float noise = lp * Mathf.Exp(-t * 8f) * 1.4f;

            float sweep = 0f;
            if (t < 0.35f)
            {
                float f2 = 400f * Mathf.Pow(6f, t / 0.35f);
                ph2 += 2f * Mathf.PI * f2 / SampleRate;
                sweep = Mathf.Sin(ph2) * Mathf.Sin(Mathf.PI * t / 0.35f) * 0.22f;
            }

            float spark = 0f;
            for (int k = 0; k < bell.Length; k++)
            {
                float tk = t - (0.05f + k * 0.075f);
                if (tk > 0f) spark += Mathf.Sin(2f * Mathf.PI * bell[k] * tk) * Mathf.Exp(-tk * 10f) * 0.16f;
            }

            d[i] = Soft(boom + noise + sweep + spark);
        }
        return Build("FlowActivate", d);
    }

    /// <summary>Hết Flow: tụt năng lượng đi xuống.</summary>
    private static AudioClip MakeEnd()
    {
        int n = (int)(0.8f * SampleRate);
        float[] d = new float[n];
        System.Random rng = new System.Random(5);
        float ph = 0f, ph2 = 0f, lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float u = t / 0.8f;
            float f = 1400f * Mathf.Pow(110f / 1400f, u);
            ph += 2f * Mathf.PI * f / SampleRate;
            float sweep = Mathf.Sin(ph) * Mathf.Exp(-t * 4.5f) * 0.5f;

            ph2 += 2f * Mathf.PI * 80f / SampleRate;
            float thump = Mathf.Sin(ph2) * Mathf.Exp(-t * 9f) * 0.5f;

            float nz = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (nz - lp) * 0.2f;
            float noise = lp * Mathf.Exp(-t * 10f) * 0.4f;

            d[i] = Soft(sweep + thump + noise);
        }
        return Build("FlowEnd", d);
    }

    /// <summary>Lớp âm lấp lánh chạy nền 2 giây, lặp liền mạch (tần số là bội của 0.5 Hz).</summary>
    private static AudioClip MakeLoop()
    {
        const float len = 2f;
        int n = (int)(len * SampleRate);
        float[] d = new float[n];
        float[] freqs = { 1500f, 1875f, 2250f, 2750f };
        float[] trem = { 0.5f, 1f, 1.5f, 0.5f };
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float v = 0f;
            for (int k = 0; k < freqs.Length; k++)
                v += Mathf.Sin(2f * Mathf.PI * freqs[k] * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * trem[k] * t + k)) * 0.05f;
            v += Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.03f + Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.02f;
            d[i] = v;
        }
        return Build("FlowLoop", d);
    }

    /// <summary>
    /// Tiếng phóng pháo: "pốc" của ống phóng, tiếng xì của ngòi, và tiếng huýt cao dần lên (có rung nhẹ) rồi tắt ở đỉnh.
    /// </summary>
    private static AudioClip MakeLaunch(int seed)
    {
        const float dur = 0.75f;
        int n = (int)(dur * SampleRate);
        float[] d = new float[n];
        System.Random rng = new System.Random(seed);
        float phW = 0f, phM = 0f, lp = 0f, lpH = 0f;

        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float u = t / dur;
            float nz = (float)(rng.NextDouble() * 2.0 - 1.0);

            // 1) "pốc" của ống phóng: thump trầm + chút nhiễu thấp
            float pf = Mathf.Lerp(140f, 55f, 1f - Mathf.Exp(-t * 40f));
            phM += 2f * Mathf.PI * pf / SampleRate;
            float mortar = Mathf.Sin(phM) * Mathf.Exp(-t * 28f) * 0.9f;
            lp += (nz - lp) * 0.4f;
            float thud = lp * Mathf.Exp(-t * 40f) * 0.8f;

            // 2) tiếng xì của ngòi / thuốc phóng: nhiễu dải cao, nhỏ dần
            lpH += (nz - lpH) * 0.25f;
            float hiss = (nz - lpH) * Mathf.Clamp01(t / 0.04f) * Mathf.Exp(-u * 1.2f) * 0.55f;

            // 3) tiếng huýt: cao dần (tăng tốc) + rung nhẹ, tắt dần gần đỉnh
            float wEnv = Mathf.Clamp01(u / 0.15f) * (1f - Mathf.SmoothStep(0.8f, 1f, u));
            float wf = 650f + 2200f * u * u + 25f * Mathf.Sin(2f * Mathf.PI * 7f * t);
            phW += 2f * Mathf.PI * wf / SampleRate;
            float whistle = Mathf.Sin(phW) * wEnv * 0.22f;

            d[i] = Soft(mortar + thud + hiss + whistle);
        }
        return Build("FireworkLaunch" + seed, d);
    }

    /// <summary>
    /// Tiếng pháo nổ: tiếng "tạch" sắc ngắn, boom trầm + gầm, tiếng xè, hàng trăm tiếng lách tách thưa dần,
    /// và vài lớp vọng để nghe như nổ trên không xa xa.
    /// </summary>
    private static AudioClip MakePop(int seed, float boomF, float crackleRate, float crackleGain)
    {
        const float body = 1.8f;
        const float tail = 0.6f;
        int nBody = (int)(body * SampleRate);
        int n = nBody + (int)(tail * SampleRate);
        float[] d = new float[n];
        System.Random rng = new System.Random(seed);
        float ph = 0f, lpBoom = 0f, lpMid = 0f;

        for (int i = 0; i < nBody; i++)
        {
            float t = i / (float)SampleRate;
            float nz = (float)(rng.NextDouble() * 2.0 - 1.0);

            // 1) "tạch": nhiễu toàn dải rất ngắn
            float crack = nz * Mathf.Exp(-t * 260f) * 1.2f;

            // 2) boom: sin giảm tần + gầm nhiễu thấp
            float f = Mathf.Lerp(boomF * 1.5f, boomF * 0.45f, 1f - Mathf.Exp(-t * 9f));
            ph += 2f * Mathf.PI * f / SampleRate;
            float sine = Mathf.Sin(ph) * Mathf.Exp(-t * 5.5f);
            lpBoom += (nz - lpBoom) * 0.02f;
            float rumble = lpBoom * Mathf.Exp(-t * 4f) * 9f;

            // 3) tiếng xè: nhiễu dải cao giảm dần
            lpMid += (nz - lpMid) * 0.3f;
            float hiss = (nz - lpMid) * Mathf.Exp(-t * 6f) * 0.35f;

            d[i] = crack + sine + rumble + hiss;
        }

        // 4) lách tách: mật độ dày lúc đầu, thưa dần (phân bố theo hàm mũ)
        const float k = 2.2f;
        int clicks = (int)(crackleRate * (1f - Mathf.Exp(-k * body)) / k);
        for (int c = 0; c < clicks; c++)
        {
            float u = (float)rng.NextDouble();
            float ct = 0.04f - Mathf.Log(1f - u * (1f - Mathf.Exp(-k * (body - 0.04f)))) / k;
            int s0 = (int)(ct * SampleRate);
            int len = (int)((0.0004f + (float)rng.NextDouble() * 0.0012f) * SampleRate);
            float amp = (0.08f + (float)rng.NextDouble() * 0.27f) * crackleGain * (1f - 0.5f * ct / body);
            float sign = rng.NextDouble() < 0.5 ? -1f : 1f;
            for (int j = 0; j < len && s0 + j < n; j++)
            {
                float nz = (float)(rng.NextDouble() * 2.0 - 1.0);
                d[s0 + j] += (nz * 0.7f + sign * 0.3f) * Mathf.Exp(-5f * j / Mathf.Max(1, len)) * amp;
            }
        }

        // 5) vọng: vài lớp trễ của âm gốc, nghe như nổ trên không
        float[] dry = (float[])d.Clone();
        int[] delays = { (int)(0.083f * SampleRate), (int)(0.141f * SampleRate), (int)(0.233f * SampleRate), (int)(0.37f * SampleRate) };
        float[] gains = { 0.38f, 0.28f, 0.2f, 0.12f };
        for (int e = 0; e < delays.Length; e++)
            for (int i = delays[e]; i < n; i++)
                d[i] += dry[i - delays[e]] * gains[e];

        // tắt dần ở cuối để không bị "click"
        int fade = (int)(0.15f * SampleRate);
        for (int i = 0; i < n; i++)
        {
            float g = i > n - fade ? (n - i) / (float)fade : 1f;
            d[i] = Soft(d[i] * 1.1f) * g;
        }
        return Build("FireworkPop" + seed, d);
    }
}