using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RhythmGame;

/// <summary>
/// Hiệu ứng Flow dựng hoàn toàn bằng code (không cần asset), tự nghe FlowSystem.
/// Bảng màu neon cyan / hồng / tím / xanh điện, cùng tông với Song List.
///  - Lúc thanh Flow đầy (vào trạng thái In the Flow): chớp sáng lạnh, 2 vòng sóng, và PHUN RA hàng chục hạt neon to
///    (kim cương + quả cầu phát sáng) bắn lên như pháo hoa rồi rơi xuống.
///  - PHÁO HOA hai bên: nhiều quả bắn từ dưới lên ở cả hai bên màn hình, nổ thành chùm tia sáng neon rồi rơi xuống.
///  - Trong lúc Flow: viền màn hình phát sáng cyan -> tím -> hồng, hạt kim cương / chấm sáng bay lên hai bên.
///  - Hết Flow: mọi thứ mờ dần.
/// Gắn vào 1 object stretch full dưới Canvas (đặt TRƯỚC BattleHUD và ResultPanel trong Hierarchy).
/// Dùng Time.deltaTime nên tự đứng yên khi pause.
/// </summary>
public class FlowVFX : MonoBehaviour
{
    [SerializeField] private FlowSystem flowSystem;

    [Header("Bật / tắt từng hiệu ứng")]
    [Tooltip("Chớp sáng toàn màn hình lúc thanh Flow đầy (mặc định tắt)")]
    [SerializeField] private bool showFlash = false;
    [Tooltip("Chớp sáng trắng tại điểm nổ của mỗi quả pháo (mặc định tắt)")]
    [SerializeField] private bool showExplosionFlash = false;
    [Tooltip("Vòng sóng lan ra ở giữa lúc thanh Flow đầy (mặc định tắt)")]
    [SerializeField] private bool showRings = false;
    [SerializeField] private bool useSpray = true;
    [SerializeField] private bool useEdgeGlow = true;
    [SerializeField] private bool useParticles = true;

    [Header("Phun hạt (mạnh, nhanh)")]
    [Tooltip("Số hạt bắn ra cùng lúc khi thanh Flow đầy")]
    [SerializeField] private int burstCount = 120;
    [Tooltip("Số hạt phun liên tục mỗi giây trong lúc đang Flow")]
    [SerializeField] private float streamRate = 45f;
    [Tooltip("Nhân lực phun (1 = mặc định, cao hơn = bắn nhanh và xa hơn)")]
    [SerializeField] private float burstPower = 1f;
    [Tooltip("Nhân kích thước hạt phun (1 = mặc định)")]
    [SerializeField] private float burstSize = 1f;

    [Header("Pháo hoa hai bên (khi thanh Flow đầy)")]
    [SerializeField] private bool useFireworks = true;
    [Tooltip("Số quả pháo bắn ở mỗi bên")]
    [SerializeField, Range(1, 4)] private int fireworksPerSide = 3;
    [Tooltip("Số tia lửa mỗi quả")]
    [SerializeField] private int sparksPerFirework = 90;
    [Tooltip("Nhân độ lớn pháo hoa (kích thước tia lửa + bán kính vụ nổ). 1 = mặc định, 1.5 = to hơn nữa")]
    [SerializeField] private float fireworkScale = 1f;
    [Tooltip("Nhân vật bên trái (để trống = tự tìm object tên Main1)")]
    [SerializeField] private Transform leftCharacter;
    [Tooltip("Nhân vật bên phải (để trống = tự tìm object tên Boss1)")]
    [SerializeField] private Transform rightCharacter;
    [Tooltip("Điểm nổ cao hơn đỉnh đầu nhân vật bao nhiêu (đơn vị canvas): thấp nhất / cao nhất")]
    [SerializeField] private float aboveHeadMin = 160f;
    [SerializeField] private float aboveHeadMax = 420f;
    [Tooltip("Độ lệch ngang ngẫu nhiên quanh nhân vật")]
    [SerializeField] private float sideSpreadPx = 80f;
    [Tooltip("Chỉnh chiều cao 'đỉnh đầu' nếu đo bị lệch (cộng lên / trừ xuống, đơn vị canvas)")]
    [SerializeField] private float headYOffset = 0f;
    [Tooltip("Vị trí ngang dự phòng khi không tìm thấy nhân vật (tỉ lệ bề rộng, tính từ giữa màn hình)")]
    [SerializeField, Range(0.2f, 0.5f)] private float sideX = 0.40f;

    [Header("Vị trí đầu nhân vật")]
    [Tooltip("Bật = tự đo vị trí Main1 / Boss1 (có thể lệch nếu prefab có sprite lạ). Tắt (mặc định) = dùng vị trí nhập tay bên dưới, ổn định hơn")]
    [SerializeField] private bool autoDetectCharacters = false;
    [Tooltip("Đỉnh đầu nhân vật TRÁI, theo tỉ lệ canvas: x từ -0.5 (mép trái) đến 0.5 (mép phải), y từ -0.5 (đáy) đến 0.5 (đỉnh), 0 = giữa màn hình")]
    [SerializeField] private Vector2 leftHeadAt = new Vector2(-0.435f, 0.14f);
    [Tooltip("Đỉnh đầu nhân vật PHẢI, cùng cách tính như trên")]
    [SerializeField] private Vector2 rightHeadAt = new Vector2(0.425f, 0.13f);
    [Tooltip("Độ cao chân nhân vật (nơi pháo phóng lên), theo tỉ lệ chiều cao canvas")]
    [SerializeField, Range(-0.5f, 0.5f)] private float feetAtY = -0.2f;

    [Header("Điều chỉnh")]
    [Tooltip("Tâm vòng sóng và vòi phun chính, tính từ giữa màn hình (hệ 1920x1080, Y âm = xuống dưới)")]
    [SerializeField] private Vector2 burstCenter = new Vector2(0f, -150f);
    [Tooltip("Độ sáng của viền màn hình khi Flow (1 = rực nhất)")]
    [SerializeField, Range(0f, 1f)] private float edgeIntensity = 1f;
    [Tooltip("Số hạt bay lên mỗi giây trong lúc Flow")]
    [SerializeField] private float particleRate = 70f;

    private static readonly Color Pink = Hex("FF2E88");
    private static readonly Color Cyan = Hex("22D9FF");
    private static readonly Color Purple = Hex("B455FF");
    private static readonly Color Blue = Hex("3D7BFF");
    private static readonly Color Magenta = Hex("FF4DFF");
    private static readonly Color Cool = new Color(0.82f, 0.95f, 1f);
    private static readonly Color[] SprayColors = { Cyan, Pink, Purple, Blue, Magenta };

    private const int RingCount = 2;
    private const int PoolSize = 180;
    private const int SprayPool = 320;
    private const int SparkPool = 1100;
    private const int RocketPool = 8;
    private const float RocketGravity = 3200f;

    private class Particle
    {
        public RectTransform rt;
        public Image img;
        public bool alive;
        public float life, maxLife, vy, x, swayAmp, swayPhase, baseAlpha;
        public Color color;
    }

    private class SprayParticle
    {
        public RectTransform rt;
        public Image core, halo;
        public bool alive;
        public float life, maxLife, spin, rot;
        public Vector2 pos, vel;
        public Color color;
    }

    private class Rocket
    {
        public RectTransform rt;
        public Image img;
        public bool alive, launched;
        public float delay, apexY, trailAcc;
        public Vector2 pos, vel;
        public Color color, color2;
    }

    private class Spark
    {
        public RectTransform rt;
        public Image img;
        public bool alive, grow;
        public float life, maxLife, grav, drag, phase;
        public Vector2 pos, vel;
        public Color color;
    }

    private RectTransform self;
    private Sprite glowSprite, diamondSprite, ringSprite, edgeSprite, starSprite;
    private Image flash, edge;
    private readonly Image[] rings = new Image[RingCount];
    private readonly float[] ringT = new float[RingCount];
    private readonly bool[] ringOn = new bool[RingCount];
    private readonly float[] ringDelay = { 0f, 0.14f };
    private readonly List<Particle> pool = new List<Particle>();
    private readonly List<SprayParticle> spray = new List<SprayParticle>();
    private RectTransform particleRoot, sprayRoot, fireworkRoot;
    private readonly List<Rocket> rockets = new List<Rocket>();
    private readonly List<Spark> sparks = new List<Spark>();
    private int sparkCursor;
    private Image markerL, markerR;
    private float markerT;

    private bool active;
    private float flow01;
    private float flashT = 99f, edgeAlpha, spawnAcc, streamAcc;

    private void Awake()
    {
        self = (RectTransform)transform;
        // Luôn phủ kín Canvas. Nếu object này quên đặt stretch full (mặc định chỉ 100x100 ở giữa màn hình)
        // thì mọi toạ độ bị co về giữa màn hình, khiến pháo hoa và hạt dồn vào giữa.
        self.pivot = new Vector2(.5f, .5f);
        self.anchorMin = Vector2.zero;
        self.anchorMax = Vector2.one;
        self.offsetMin = Vector2.zero;
        self.offsetMax = Vector2.zero;
        self.localScale = Vector3.one;
        glowSprite = MakeGlowSprite();
        diamondSprite = MakeDiamondSprite();
        ringSprite = MakeRingSprite();
        edgeSprite = MakeEdgeSprite();
        starSprite = MakeStarSprite();
        Build();
    }

    private void Start()
    {
        if (flowSystem == null) flowSystem = FindFirstObjectByType<FlowSystem>();
        if (flowSystem == null)
        {
            Debug.LogWarning("[FlowVFX] Không tìm thấy FlowSystem trong scene - hiệu ứng sẽ không tự chạy.");
            return;
        }
        flowSystem.OnFlowStateChanged += OnFlowState;
        flowSystem.OnFlowAmountChanged += OnFlowAmount;
        Debug.Log("[FlowVFX] Đã sẵn sàng, đang nghe FlowSystem.");
    }

    /// <summary>Thử hiệu ứng ngay mà không cần đầy thanh Flow: bấm ⋮ trên component này (lúc đang Play) -> Test Flow Burst.</summary>
    [ContextMenu("Test Flow Burst")]
    private void TestBurst()
    {
        active = true;
        flow01 = 1f;
        PlayBurst();
        CancelInvoke(nameof(EndTest));
        Invoke(nameof(EndTest), 4f);

        // hiện 2 chấm trắng đúng chỗ pháo sẽ nổ phía trên đầu, để chỉnh vị trí cho chuẩn
        markerL.rectTransform.anchoredPosition = leftHead;
        markerR.rectTransform.anchoredPosition = rightHead;
        markerL.gameObject.SetActive(true);
        markerR.gameObject.SetActive(true);
        markerT = 3f;
    }

    private void EndTest()
    {
        if (flowSystem == null || !flowSystem.IsFlowActive) active = false;
    }

    private void OnDestroy()
    {
        if (flowSystem == null) return;
        flowSystem.OnFlowStateChanged -= OnFlowState;
        flowSystem.OnFlowAmountChanged -= OnFlowAmount;
    }

    private void OnFlowAmount(float normalized) { flow01 = normalized; }

    private void OnFlowState(bool on)
    {
        active = on;
        Debug.Log("[FlowVFX] Flow " + (on ? "BẬT - bắn hiệu ứng" : "TẮT"));
        if (on) PlayBurst();
    }

    private void PlayBurst()
    {
        flashT = 0f;
        for (int i = 0; i < RingCount; i++)
        {
            ringT[i] = -ringDelay[i];
            ringOn[i] = showRings;
        }
        RefreshAnchors();
        if (useSpray) PlaySpray();
        PlayFireworks();
    }

    // ================= Phun hạt =================

    private float CanvasW { get { return self.rect.width > 1f ? self.rect.width : 1920f; } }
    private float CanvasH { get { return self.rect.height > 1f ? self.rect.height : 1080f; } }

    private SprayParticle FreeSpray()
    {
        for (int i = 0; i < spray.Count; i++) if (!spray[i].alive) return spray[i];
        return null;
    }

    // Bắn một hạt neon từ 'origin' theo góc ngẫu nhiên trong [angleMin, angleMax] (độ, 90 = thẳng lên)
    private void Shoot(Vector2 origin, float angleMin, float angleMax, float speedMin, float speedMax,
        float sizeMin, float sizeMax, float delayMax)
    {
        SprayParticle p = FreeSpray();
        if (p == null) return;
        float k = CanvasH / 1080f;

        float rad = Random.Range(angleMin, angleMax) * Mathf.Deg2Rad;
        float speed = Random.Range(speedMin, speedMax) * k * burstPower;
        p.vel = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * speed;
        p.pos = origin;
        p.maxLife = Random.Range(0.8f, 1.5f);
        p.life = -Random.Range(0f, delayMax);
        p.spin = Random.Range(-540f, 540f);
        p.rot = Random.Range(0f, 360f);
        p.color = SprayColors[Random.Range(0, SprayColors.Length)];

        float size = Random.Range(sizeMin, sizeMax) * burstSize;
        bool orb = Random.value < 0.3f;             // 30% quả cầu sáng, còn lại kim cương
        p.core.sprite = orb ? glowSprite : diamondSprite;
        p.core.rectTransform.sizeDelta = new Vector2(size, size);
        p.halo.rectTransform.sizeDelta = new Vector2(size * 2.6f, size * 2.6f);

        p.alive = true;
        p.rt.anchoredPosition = origin;
        p.rt.gameObject.SetActive(p.life >= 0f);
    }

    // Nổ khi thanh Flow đầy: bắn mạnh và nhanh từ trên đầu hai nhân vật (không còn vòi ở giữa màn hình)
    private void PlaySpray()
    {
        for (int i = 0; i < burstCount; i++)
        {
            Vector2 origin = (i % 2 == 0) ? leftHead : rightHead;
            bool fan = Random.value < 0.75f;
            Shoot(origin, fan ? 30f : 0f, fan ? 150f : 360f, 1400f, 3000f, 30f, 90f, 0.06f);
        }
    }

    // Phun liên tục trong lúc đang Flow, cũng từ đầu hai nhân vật
    private void SpawnStream()
    {
        Vector2 origin = Random.value < 0.5f ? leftHead : rightHead;
        Shoot(origin, 60f, 120f, 1100f, 2300f, 22f, 60f, 0f);
    }

    private void UpdateSpray(float dt)
    {
        float k = CanvasH / 1080f;
        for (int i = 0; i < spray.Count; i++)
        {
            SprayParticle p = spray[i];
            if (!p.alive) continue;

            if (p.life < 0f)
            {
                p.life += dt;
                if (p.life >= 0f) p.rt.gameObject.SetActive(true);
                else continue;
            }
            p.life += dt;
            if (p.life >= p.maxLife)
            {
                p.alive = false;
                p.rt.gameObject.SetActive(false);
                continue;
            }

            p.vel.y -= 900f * k * dt;                // trọng lực nhẹ để hạt bay xa
            p.vel *= 1f - 0.35f * dt;                // lực cản nhỏ
            p.pos += p.vel * dt;

            float t = p.life / p.maxLife;
            float s = Mathf.Lerp(1f, 0.4f, t);
            p.rot += p.spin * dt;
            p.rt.anchoredPosition = p.pos;
            p.rt.localScale = new Vector3(s, s, 1f);
            p.rt.localEulerAngles = new Vector3(0f, 0f, p.rot);

            float a = Mathf.Clamp01(p.life / 0.04f) * (1f - Mathf.SmoothStep(0.6f, 1f, t));
            Color core = Color.Lerp(p.color, Color.white, 0.4f);   // lõi sáng, quầng ngoài đậm màu -> hiệu ứng neon
            core.a = a;
            p.core.color = core;
            Color halo = p.color;
            halo.a = a * 0.55f;
            p.halo.color = halo;
        }
    }

    // ================= Vị trí hai nhân vật =================

    private Vector2 leftHead, leftFeet, rightHead, rightFeet;
    private Transform foundLeft, foundRight;

    // Xác định vị trí đầu / chân của hai nhân vật (toạ độ canvas, gốc ở giữa màn hình)
    private void RefreshAnchors()
    {
        float w = CanvasW, h = CanvasH;
        leftHead = new Vector2(leftHeadAt.x * w, leftHeadAt.y * h);
        rightHead = new Vector2(rightHeadAt.x * w, rightHeadAt.y * h);
        leftFeet = new Vector2(leftHead.x, feetAtY * h);
        rightFeet = new Vector2(rightHead.x, feetAtY * h);
        string mode = "nhập tay";

        if (autoDetectCharacters)
        {
            Transform a = leftCharacter != null ? leftCharacter : (foundLeft != null ? foundLeft : (foundLeft = FindByName("Main1")));
            Transform b = rightCharacter != null ? rightCharacter : (foundRight != null ? foundRight : (foundRight = FindByName("Boss1")));
            Vector2 ah, af, bh, bf;
            if (Measure(a, out ah, out af) && Measure(b, out bh, out bf))
            {
                Vector2 lh = ah, lf = af, rh = bh, rf = bf;
                if (lh.x > rh.x) { lh = bh; lf = bf; rh = ah; rf = af; }
                // chỉ nhận kết quả đo khi hợp lý: hai nhân vật nằm hai bên, đầu cao hơn chân
                if (lh.x < -0.15f * w && rh.x > 0.15f * w && lh.y > lf.y && rh.y > rf.y)
                {
                    leftHead = lh; leftFeet = lf; rightHead = rh; rightFeet = rf;
                    mode = "tự đo";
                }
                else mode = "nhập tay (kết quả tự đo không hợp lý)";
            }
            else mode = "nhập tay (không đo được Main1/Boss1)";
        }

        leftHead.y += headYOffset;
        rightHead.y += headYOffset;
        Debug.Log("[FlowVFX] Đầu nhân vật (" + mode + "): trái " + leftHead + ", phải " + rightHead + " | canvas " + w + "x" + h);
    }

    private static Transform FindByName(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.transform : null;
    }

    private bool Measure(Transform t, out Vector2 head, out Vector2 feet)
    {
        head = feet = Vector2.zero;
        if (t == null) return false;
        Camera cam = Camera.main;
        if (cam == null) return false;

        bool has = false;
        Bounds b = new Bounds();
        SpriteRenderer[] srs = t.GetComponentsInChildren<SpriteRenderer>(false);
        for (int i = 0; i < srs.Length; i++)
        {
            if (!srs[i].enabled || srs[i].sprite == null) continue;
            if (!has) { b = srs[i].bounds; has = true; }
            else b.Encapsulate(srs[i].bounds);
        }
        if (!has) return false;

        return ToCanvas(cam, new Vector3(b.center.x, b.max.y, b.center.z), out head)
            && ToCanvas(cam, new Vector3(b.center.x, b.min.y, b.center.z), out feet);
    }

    private bool ToCanvas(Camera cam, Vector3 world, out Vector2 local)
    {
        Vector3 sp = cam.WorldToScreenPoint(world);
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(self, sp, uiCam, out local);
    }

    // ================= Pháo hoa =================

    private void PlayFireworks()
    {
        if (!useFireworks) return;
        int per = Mathf.Clamp(fireworksPerSide, 1, 4);
        for (int side = -1; side <= 1; side += 2)
            for (int j = 0; j < per; j++)
                LaunchRocket(side, j * 0.3f + Random.Range(0f, 0.12f) + (side > 0 ? 0.12f : 0f));
    }

    private void LaunchRocket(int side, float delay)
    {
        Rocket r = null;
        for (int i = 0; i < rockets.Count; i++) if (!rockets[i].alive) { r = rockets[i]; break; }
        if (r == null) return;

        float w = CanvasW, h = CanvasH, k = h / 1080f;
        Vector2 head = side < 0 ? leftHead : rightHead;
        Vector2 feet = side < 0 ? leftFeet : rightFeet;
        float startX = head.x + Random.Range(-sideSpreadPx, sideSpreadPx);
        float startY = feet.y;                    // phóng từ chân nhân vật
        // nổ ngay phía trên đầu nhân vật (giới hạn để không ra khỏi mép trên)
        r.apexY = Mathf.Min(head.y + Random.Range(aboveHeadMin, Mathf.Max(aboveHeadMin, aboveHeadMax)), 0.5f * h - 100f);
        float vy = Mathf.Sqrt(2f * RocketGravity * k * Mathf.Max(100f, r.apexY - startY));
        r.pos = new Vector2(startX, startY);
        r.vel = new Vector2(-side * Random.Range(0f, 90f) * k, vy);
        r.delay = delay;
        r.launched = false;
        r.trailAcc = 0f;
        r.alive = true;
        int c1 = Random.Range(0, SprayColors.Length);
        int c2 = (c1 + Random.Range(1, SprayColors.Length)) % SprayColors.Length;
        r.color = SprayColors[c1];
        r.color2 = SprayColors[c2];
        r.img.color = Color.Lerp(r.color, Color.white, 0.7f);
        r.rt.gameObject.SetActive(false);
    }

    private void UpdateFireworks(float dt)
    {
        float k = CanvasH / 1080f;
        for (int i = 0; i < rockets.Count; i++)
        {
            Rocket r = rockets[i];
            if (!r.alive) continue;
            if (!r.launched)
            {
                r.delay -= dt;
                if (r.delay > 0f) continue;
                r.launched = true;
                r.rt.gameObject.SetActive(true);
            }

            r.vel.y -= RocketGravity * k * dt;
            r.pos += r.vel * dt;
            r.rt.anchoredPosition = r.pos;

            // vệt đuôi sáng
            r.trailAcc += dt * 70f;
            while (r.trailAcc >= 1f)
            {
                r.trailAcc -= 1f;
                Vector2 jitter = Random.insideUnitCircle * 6f;
                Emit(r.pos + jitter, new Vector2(Random.Range(-40f, 40f), -120f * k), r.color,
                    Random.Range(26f, 44f) * fireworkScale, 0.45f, 0f, 2f, false, glowSprite);
            }

            if (r.vel.y <= 80f * k || r.pos.y >= r.apexY)
            {
                Explode(r);
                r.alive = false;
                r.rt.gameObject.SetActive(false);
            }
        }
    }

    private void Explode(Rocket r)
    {
        float k = CanvasH / 1080f;
        Vector2 c = r.pos;
        if (showExplosionFlash)
            Emit(c, Vector2.zero, Color.white, 460f * fireworkScale, 0.28f, 0f, 0f, true, glowSprite);   // chớp sáng lúc nổ

        int n = Mathf.Max(10, sparksPerFirework);
        for (int i = 0; i < n; i++)
        {
            float ang = (i / (float)n) * Mathf.PI * 2f + Random.Range(-0.12f, 0.12f);
            float bias = Random.value < 0.55f ? Random.Range(0.92f, 1f) : Random.Range(0.35f, 0.9f); // vành ngoài + lõi trong
            float speed = 880f * k * fireworkScale * bias;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

            float pick = Random.value;
            Color col = pick < 0.2f ? Color.white : (pick < 0.5f ? r.color2 : r.color);
            Emit(c, dir * speed, col, Random.Range(30f, 62f) * fireworkScale, Random.Range(1.1f, 1.8f),
                420f * k, 1.7f, false, Random.value < 0.75f ? starSprite : glowSprite);
        }
    }

    private void Emit(Vector2 pos, Vector2 vel, Color color, float size, float life, float grav, float drag,
        bool grow, Sprite sprite)
    {
        Spark s = null;
        for (int n = 0; n < sparks.Count; n++)
        {
            sparkCursor = (sparkCursor + 1) % sparks.Count;
            if (!sparks[sparkCursor].alive) { s = sparks[sparkCursor]; break; }
        }
        if (s == null) return;

        s.alive = true;
        s.life = 0f;
        s.maxLife = life;
        s.pos = pos;
        s.vel = vel;
        s.color = color;
        s.grav = grav;
        s.drag = drag;
        s.grow = grow;
        s.phase = Random.Range(0f, 6.28f);
        s.img.sprite = sprite;
        s.rt.sizeDelta = new Vector2(size, size);
        s.rt.anchoredPosition = pos;
        s.rt.localScale = Vector3.one;
        s.rt.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 45f));
        s.rt.gameObject.SetActive(true);
    }

    private void UpdateSparks(float dt)
    {
        for (int i = 0; i < sparks.Count; i++)
        {
            Spark s = sparks[i];
            if (!s.alive) continue;
            s.life += dt;
            if (s.life >= s.maxLife)
            {
                s.alive = false;
                s.rt.gameObject.SetActive(false);
                continue;
            }

            s.vel.y -= s.grav * dt;
            s.vel *= Mathf.Max(0f, 1f - s.drag * dt);
            s.pos += s.vel * dt;

            float t = s.life / s.maxLife;
            float a = Mathf.Clamp01(s.life / 0.03f) * (1f - Mathf.SmoothStep(0.45f, 1f, t));
            if (s.grow) a *= 0.8f;
            else a *= 0.75f + 0.25f * Mathf.Sin(Time.time * 38f + s.phase);   // lấp lánh
            float sc = s.grow ? Mathf.Lerp(0.5f, 1.6f, t) : Mathf.Lerp(1f, 0.4f, t);

            s.rt.anchoredPosition = s.pos;
            s.rt.localScale = new Vector3(sc, sc, 1f);
            Color c = s.color;
            c.a = Mathf.Clamp01(a);
            s.img.color = c;
        }
    }

    // ================= Update =================

    private void Update()
    {
        float dt = Time.deltaTime;
        float t = Time.time;

        if (markerT > 0f)
        {
            markerT -= dt;
            if (markerT <= 0f) { markerL.gameObject.SetActive(false); markerR.gameObject.SetActive(false); }
        }

        UpdateFlash(dt);
        UpdateRings(dt);
        UpdateSpray(dt);
        UpdateFireworks(dt);
        UpdateSparks(dt);
        if (active && useSpray)
        {
            streamAcc += dt * streamRate;
            while (streamAcc >= 1f) { streamAcc -= 1f; SpawnStream(); }
        }
        else streamAcc = 0f;

        // --- viền phát sáng ---
        float target = 0f;
        if (active && useEdgeGlow)
            target = edgeIntensity * (0.85f + 0.15f * Mathf.Sin(t * 3.5f)) * Mathf.Lerp(0.6f, 1f, flow01);
        edgeAlpha = Mathf.Lerp(edgeAlpha, target, 1f - Mathf.Exp(-6f * dt));
        Color ec = GradientAt(0.5f + 0.5f * Mathf.Sin(t * 1.6f)); // cyan -> tím -> hồng
        ec.a = edgeAlpha;
        edge.color = ec;

        // --- hạt bay lên ---
        if (active && useParticles)
        {
            spawnAcc += dt * particleRate;
            while (spawnAcc >= 1f) { spawnAcc -= 1f; SpawnParticle(); }
        }
        else spawnAcc = 0f;
        UpdateParticles(dt);
    }

    private void UpdateFlash(float dt)
    {
        const float dur = 0.4f;
        if (!showFlash || flashT >= dur)
        {
            if (flash.enabled) flash.enabled = false;
            return;
        }
        flashT += dt;
        float a = Mathf.Clamp01(1f - flashT / dur);
        flash.enabled = true;
        flash.color = new Color(Cool.r, Cool.g, Cool.b, 0.5f * a * a);
    }

    private void UpdateRings(float dt)
    {
        const float dur = 0.75f;
        for (int i = 0; i < RingCount; i++)
        {
            if (!ringOn[i]) { if (rings[i].gameObject.activeSelf) rings[i].gameObject.SetActive(false); continue; }
            ringT[i] += dt;
            if (ringT[i] < 0f) continue;
            float p = ringT[i] / dur;
            if (p >= 1f) { ringOn[i] = false; continue; }
            if (!rings[i].gameObject.activeSelf) rings[i].gameObject.SetActive(true);
            float s = Mathf.Lerp(0.3f, 3.4f, EaseOut(p));
            rings[i].rectTransform.localScale = new Vector3(s, s, 1f);
            Color c = i == 0 ? Cyan : Pink;
            c.a = (1f - p) * 0.9f;
            rings[i].color = c;
        }
    }

    private void SpawnParticle()
    {
        Particle p = null;
        for (int i = 0; i < pool.Count; i++) if (!pool[i].alive) { p = pool[i]; break; }
        if (p == null) return;

        float w = self.rect.width > 1f ? self.rect.width : 1920f;
        float h = self.rect.height > 1f ? self.rect.height : 1080f;
        bool diamond = Random.value < 0.5f;
        float size = diamond ? Random.Range(14f, 36f) : Random.Range(18f, 56f);
        p.img.sprite = diamond ? diamondSprite : glowSprite;
        p.alive = true;
        p.life = 0f;
        p.maxLife = Random.Range(1.4f, 2.4f);
        // 60% hạt bay ở hai bên màn hình, 40% rải gần giữa
        float side = Random.value < 0.6f
            ? (Random.value < 0.5f ? -1f : 1f) * Random.Range(0.28f, 0.5f)
            : Random.Range(-0.3f, 0.3f);
        p.x = side * w;
        p.vy = Random.Range(260f, 620f) * (h / 1080f);
        p.swayAmp = Random.Range(10f, 40f);
        p.swayPhase = Random.Range(0f, 6.28f);
        p.baseAlpha = Random.Range(0.75f, 1f);
        p.color = SprayColors[Random.Range(0, SprayColors.Length)];
        p.rt.sizeDelta = new Vector2(size, size);
        p.rt.anchoredPosition = new Vector2(p.x, Random.Range(-h * 0.5f, h * 0.1f));
        p.img.color = new Color(p.color.r, p.color.g, p.color.b, 0f);
        p.rt.gameObject.SetActive(true);
    }

    private void UpdateParticles(float dt)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            Particle p = pool[i];
            if (!p.alive) continue;
            p.life += dt;
            if (p.life >= p.maxLife)
            {
                p.alive = false;
                p.rt.gameObject.SetActive(false);
                continue;
            }
            Vector2 pos = p.rt.anchoredPosition;
            pos.y += p.vy * dt;
            pos.x = p.x + Mathf.Sin(p.life * 3f + p.swayPhase) * p.swayAmp;
            p.rt.anchoredPosition = pos;

            float fadeIn = Mathf.Clamp01(p.life / 0.15f);
            float fadeOut = 1f - p.life / p.maxLife;
            p.img.color = new Color(p.color.r, p.color.g, p.color.b, p.baseAlpha * fadeIn * fadeOut);
        }
    }

    private static float EaseOut(float p) { p = Mathf.Clamp01(p); return 1f - Mathf.Pow(1f - p, 3f); }

    // ================= Dựng UI =================

    private void Build()
    {
        // viền phát sáng
        edge = MakeImage("EdgeGlow", transform, edgeSprite);
        Stretch(edge.rectTransform);
        edge.color = new Color(1f, 1f, 1f, 0f);

        // hạt bay lên
        particleRoot = MakeNode("Particles", transform);
        Stretch(particleRoot);
        for (int i = 0; i < PoolSize; i++)
        {
            Image img = MakeImage("P" + i, particleRoot, glowSprite);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            img.gameObject.SetActive(false);
            pool.Add(new Particle { rt = rt, img = img });
        }

        // hạt phun (to, neon): lõi + quầng sáng
        sprayRoot = MakeNode("Spray", transform);
        Stretch(sprayRoot);
        for (int i = 0; i < SprayPool; i++)
        {
            RectTransform rt = MakeNode("S" + i, sprayRoot);
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);

            Image halo = MakeImage("Halo", rt, glowSprite);          // quầng sáng vẽ trước (nằm sau)
            RectTransform hrt = halo.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(.5f, .5f);
            hrt.pivot = new Vector2(.5f, .5f);
            Image core = MakeImage("Core", rt, diamondSprite);       // lõi sáng vẽ sau (nằm trên)
            RectTransform crt = core.rectTransform;
            crt.anchorMin = crt.anchorMax = new Vector2(.5f, .5f);
            crt.pivot = new Vector2(.5f, .5f);
            rt.gameObject.SetActive(false);
            spray.Add(new SprayParticle { rt = rt, core = core, halo = halo });
        }

        // pháo hoa: tia lửa (pool) + đầu tên lửa
        fireworkRoot = MakeNode("Fireworks", transform);
        Stretch(fireworkRoot);
        for (int i = 0; i < SparkPool; i++)
        {
            Image img = MakeImage("F" + i, fireworkRoot, starSprite);
            RectTransform rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            img.gameObject.SetActive(false);
            sparks.Add(new Spark { rt = rt, img = img });
        }
        for (int i = 0; i < RocketPool; i++)
        {
            Image head = MakeImage("Rocket" + i, fireworkRoot, glowSprite);
            RectTransform rt = head.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(70f, 70f);
            head.gameObject.SetActive(false);
            rockets.Add(new Rocket { rt = rt, img = head });
        }

        // vòng sóng
        for (int i = 0; i < RingCount; i++)
        {
            Image ring = MakeImage("Ring" + i, transform, ringSprite);
            RectTransform rt = ring.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
            rt.pivot = new Vector2(.5f, .5f);
            rt.sizeDelta = new Vector2(600f, 600f);
            rt.anchoredPosition = burstCenter;
            ring.gameObject.SetActive(false);
            rings[i] = ring;
        }

        // chấm đánh dấu vị trí đầu nhân vật (chỉ hiện khi bấm Test Flow Burst)
        markerL = MakeImage("MarkerLeft", transform, glowSprite);
        markerR = MakeImage("MarkerRight", transform, glowSprite);
        foreach (Image m in new[] { markerL, markerR })
        {
            RectTransform mr = m.rectTransform;
            mr.anchorMin = mr.anchorMax = new Vector2(.5f, .5f);
            mr.pivot = new Vector2(.5f, .5f);
            mr.sizeDelta = new Vector2(90f, 90f);
            m.color = Color.white;
            m.gameObject.SetActive(false);
        }

        // chớp sáng (trên cùng)
        flash = MakeImage("Flash", transform, null);
        Stretch(flash.rectTransform);
        flash.color = new Color(Cool.r, Cool.g, Cool.b, 0f);
        flash.enabled = false;
    }

    private static Image MakeImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private static RectTransform MakeNode(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    // ================= Sprite tạo bằng code =================

    /// <summary>Chấm sáng mờ dần ra rìa (hạt, quầng sáng).</summary>
    private static Sprite MakeGlowSprite()
    {
        const int n = 64;
        return BuildSprite(n, (nx, ny) =>
        {
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            return Mathf.Pow(Mathf.Clamp01(1f - r), 1.5f);
        });
    }

    /// <summary>Hạt hình kim cương sắc nét, cùng motif với biểu tượng độ khó trong Song List.</summary>
    private static Sprite MakeDiamondSprite()
    {
        const int n = 64;
        return BuildSprite(n, (nx, ny) => Mathf.Clamp01((1f - (Mathf.Abs(nx) + Mathf.Abs(ny))) * 8f));
    }

    /// <summary>Tia lửa hình sao 4 cánh có lõi sáng mờ (pháo hoa).</summary>
    private static Sprite MakeStarSprite()
    {
        const int n = 64;
        return BuildSprite(n, (nx, ny) =>
        {
            float v = Mathf.Sqrt(Mathf.Abs(nx)) + Mathf.Sqrt(Mathf.Abs(ny));
            float star = Mathf.Clamp01((1f - v) * 5f);
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            float core = 0.4f * Mathf.Pow(Mathf.Clamp01(1f - r), 2f);
            return Mathf.Max(star, core);
        });
    }

    /// <summary>Vòng tròn mảnh có quầng sáng nhẹ (sóng xung kích).</summary>
    private static Sprite MakeRingSprite()
    {
        const int n = 256;
        return BuildSprite(n, (nx, ny) =>
        {
            float r = Mathf.Sqrt(nx * nx + ny * ny);
            float line = Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.05f, 2f));
            float halo = 0.15f * Mathf.Exp(-Mathf.Pow((r - 0.82f) / 0.18f, 2f));
            return Mathf.Clamp01(line + halo);
        });
    }

    /// <summary>Trong suốt ở giữa, sáng dần về 4 cạnh (viền màn hình).</summary>
    private static Sprite MakeEdgeSprite()
    {
        const int n = 128;
        return BuildSprite(n, (nx, ny) =>
        {
            float r = Mathf.Sqrt(nx * nx + ny * ny) / 1.414f;
            float box = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
            float v = r * 0.5f + box * 0.5f;
            float a = Mathf.Clamp01((v - 0.30f) / 0.62f);
            return Mathf.Pow(a, 1.4f);
        });
    }

    private static Sprite BuildSprite(int n, System.Func<float, float, float> alphaAt)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        {
            float ny = (y + 0.5f) / n * 2f - 1f;
            for (int x = 0; x < n; x++)
            {
                float nx = (x + 0.5f) / n * 2f - 1f;
                byte a = (byte)(Mathf.Clamp01(alphaAt(nx, ny)) * 255f);
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f);
    }

    /// <summary>Cyan -> tím -> hồng, khớp nền Song List và thanh Flow.</summary>
    private static Color GradientAt(float t)
    {
        return t < 0.5f ? Color.Lerp(Cyan, Purple, t * 2f) : Color.Lerp(Purple, Pink, (t - 0.5f) * 2f);
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color c);
        return c;
    }
}