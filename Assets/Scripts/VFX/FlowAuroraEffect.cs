using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Hạt sáng bay lên ở hai bên màn hình khi đang In the Flow.
    /// Bản nâng cấp: bảng màu neon (cyan / hồng / tím / trắng) thay vì một màu vàng, hạt kim cương + chấm sáng mờ,
    /// mờ vào / mờ ra mượt, đung đưa nhẹ, mật độ giảm dần khi thanh Flow cạn, và fade-out khi hết Flow.
    /// Các ô cũ (particleCount, speed, flyDirection, ...) giữ nguyên tên nên cấu hình hiện có vẫn dùng được.
    /// </summary>
    public class FlowAuroraEffect : MonoBehaviour
    {
        public FlowSystem flowSystem;

        [Header("Subtle defaults")]
        [Tooltip("Số hạt. Nên 40–80 để không rối mắt.")]
        public int particleCount = 18;

        [Tooltip("Màu dự phòng, chỉ dùng khi tắt 'Use Neon Palette'.")]
        public Color particleColor = new Color(1f, 0.9f, 0.4f, 0.3f);

        [Tooltip("Tốc độ bay.")]
        public float speed = 1.2f;

        public Vector3 flyDirection = Vector3.up;
        public Vector3 spawnOffset = Vector3.zero;
        public float lifetime = 3.5f;

        [Header("Màu & hình dạng")]
        [Tooltip("Bật = mỗi hạt lấy ngẫu nhiên một màu trong bảng bên dưới.")]
        public bool useNeonPalette = true;
        public Color[] palette =
        {
            new Color(0.13f, 0.85f, 1.00f),  // cyan
            new Color(1.00f, 0.18f, 0.53f),  // hồng
            new Color(0.71f, 0.33f, 1.00f),  // tím
            new Color(1.00f, 1.00f, 1.00f),  // trắng
        };
        [Tooltip("Tỉ lệ hạt hình kim cương (còn lại là chấm sáng mờ).")]
        [Range(0f, 1f)] public float diamondRatio = 0.5f;
        [Tooltip("Độ đục tối đa của hạt.")]
        [Range(0f, 1f)] public float peakAlpha = 1f;
        [Tooltip("Nhân kích thước hạt (tăng nếu thấy hạt quá nhỏ).")]
        public float visualBoost = 3f;
        [Tooltip("Biên độ đung đưa ngang.")]
        public float swayAmount = 0.25f;
        [Tooltip("Tốc độ xoay của hạt kim cương (độ / giây).")]
        public float spinSpeed = 60f;
        [Tooltip("Mật độ hạt khi thanh Flow gần cạn (1 = không giảm).")]
        [Range(0f, 1f)] public float lowFlowDensity = 0.35f;
        [Tooltip("Tốc độ hiện / ẩn khi vào / ra khỏi Flow.")]
        public float fadeSpeed = 3f;

        [Header("Layout")]
        [Tooltip("Chỉ spawn ở 2 dải mép trái/phải để tránh vùng note.")]
        public bool spawnOnSides = true;

        [Tooltip("Độ rộng mỗi dải spawn ở mỗi bên.")]
        public float sideBandWidth = 2f;

        public float spawnHalfWidth = 8f;
        public float spawnHalfHeight = 6f;

        [Tooltip("Khoảng hở trên đỉnh trước khi tái sinh hạt.")]
        public float recycleMargin = 1f;

        [Header("Rendering")]
        [Tooltip("Sorting layer — đặt trùng tên layer của background.")]
        public string sortingLayerName = "Default";

        [Tooltip("Sorting order âm để hạt nằm sau note.")]
        public int sortingOrder = -10;

        [Tooltip("Kích thước gốc của hạt (world units).")]
        public float baseSize = 0.08f;

        [Tooltip("Biên độ nhấp nháy kích thước (0 = tắt).")]
        [Range(0f, 0.5f)] public float sizePulse = 0.15f;

        public float pulseSpeed = 2f;

        private const float GlowScale = 2.2f; // chấm sáng mờ vẽ to hơn kim cương cho cân mắt

        private GameObject[] particles;
        private SpriteRenderer[] renderers;
        private Vector3[] pos;
        private float[] timers, baseScales, startY, swayPhase, baseAlpha, spin, density;
        private Color[] baseColor;
        private bool[] isDiamond;
        private Sprite diamondSprite, glowSprite;
        private float global;     // 0..1: hệ số hiện / ẩn chung
        private bool visible;

        private void Start()
        {
            if (flowSystem == null) flowSystem = FindFirstObjectByType<FlowSystem>();
            Build();
            SetVisible(false);
        }

        // ================= Dựng =================

        private void Build()
        {
            diamondSprite = MakeSprite(64, (nx, ny) => Mathf.Clamp01((1f - (Mathf.Abs(nx) + Mathf.Abs(ny))) * 6f));
            glowSprite = MakeSprite(64, (nx, ny) => Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(nx * nx + ny * ny)), 1.6f));

            int n = Mathf.Max(1, particleCount);
            particles = new GameObject[n];
            renderers = new SpriteRenderer[n];
            pos = new Vector3[n];
            timers = new float[n];
            baseScales = new float[n];
            startY = new float[n];
            swayPhase = new float[n];
            baseAlpha = new float[n];
            spin = new float[n];
            density = new float[n];
            baseColor = new Color[n];
            isDiamond = new bool[n];

            for (int i = 0; i < n; i++)
            {
                GameObject go = new GameObject("AuroraParticle_" + i);
                go.transform.SetParent(transform, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = sortingOrder;
                particles[i] = go;
                renderers[i] = sr;
                density[i] = 1f;
                InitParticle(i, true);
            }
        }

        // Đặt lại một hạt: vị trí, màu, hình dạng mới
        private void InitParticle(int i, bool randomProgress)
        {
            float progress = randomProgress ? Random.value : 0f;
            Vector3 p = PickSpawnPosition();
            p.z = 0f;
            startY[i] = p.y;
            pos[i] = p + flyDirection.normalized * (progress * speed * lifetime);
            timers[i] = progress * lifetime;

            isDiamond[i] = Random.value < diamondRatio;
            renderers[i].sprite = isDiamond[i] ? diamondSprite : glowSprite;
            baseScales[i] = baseSize * visualBoost * Random.Range(0.7f, 1.3f) * (isDiamond[i] ? 1f : GlowScale);
            baseAlpha[i] = Random.Range(0.8f, 1f);
            swayPhase[i] = Random.Range(0f, 6.28f);
            spin[i] = (Random.value < 0.5f ? -1f : 1f) * spinSpeed * Random.Range(0.5f, 1.2f);
            baseColor[i] = (useNeonPalette && palette != null && palette.Length > 0)
                ? palette[Random.Range(0, palette.Length)]
                : particleColor;
            particles[i].transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }

        private Vector3 PickSpawnPosition()
        {
            float x;
            if (spawnOnSides)
            {
                bool left = Random.value < 0.5f;
                float band = Mathf.Min(sideBandWidth, spawnHalfWidth * 0.5f);
                x = left
                    ? Random.Range(-spawnHalfWidth, -spawnHalfWidth + band)
                    : Random.Range(spawnHalfWidth - band, spawnHalfWidth);
            }
            else
            {
                x = Random.Range(-spawnHalfWidth, spawnHalfWidth);
            }
            float y = Random.Range(-spawnHalfHeight, spawnHalfHeight);
            return new Vector3(x, y, 0f) + spawnOffset;
        }

        private void SetVisible(bool on)
        {
            visible = on;
            if (particles == null) return;
            for (int i = 0; i < particles.Length; i++)
                if (particles[i] != null) particles[i].SetActive(on);
        }

        // ================= Update =================

        private void Update()
        {
            if (particles == null || flowSystem == null) return;
            float dt = Time.deltaTime;

            global = Mathf.MoveTowards(global, flowSystem.IsFlowActive ? 1f : 0f, fadeSpeed * dt);
            if (global <= 0f)
            {
                if (visible) SetVisible(false);
                return;
            }
            if (!visible)
            {
                // vừa vào Flow: rải lại tất cả hạt để không hiện ở vị trí cũ
                for (int i = 0; i < particles.Length; i++) InitParticle(i, true);
                SetVisible(true);
            }

            Vector3 dir = flyDirection.normalized;
            float recycleY = spawnOffset.y + spawnHalfHeight + recycleMargin;
            float intensity = Mathf.Lerp(lowFlowDensity, 1f, flowSystem.NormalizedFlow);
            int n = particles.Length;

            for (int i = 0; i < n; i++)
            {
                // mật độ giảm dần khi Flow cạn: các hạt có chỉ số cao mờ đi trước
                bool allowed = (i + 0.5f) / n <= intensity;
                density[i] = Mathf.MoveTowards(density[i], allowed ? 1f : 0f, dt * 2f);

                timers[i] += dt;
                pos[i] += dir * speed * dt;

                float byHeight = Mathf.InverseLerp(startY[i], recycleY, pos[i].y);
                float prog = Mathf.Max(timers[i] / Mathf.Max(0.01f, lifetime), byHeight);
                if (prog >= 1f)
                {
                    InitParticle(i, false);
                    prog = 0f;
                }

                // mờ vào ở đầu vòng đời, mờ ra ở cuối
                float env = Mathf.Clamp01(prog / 0.12f) * Mathf.Clamp01((1f - prog) / 0.35f);

                Transform tr = particles[i].transform;
                tr.position = pos[i] + Vector3.right * (Mathf.Sin(Time.time * 1.3f + swayPhase[i]) * swayAmount);

                float pulse = sizePulse > 0f ? 1f + sizePulse * Mathf.Sin(Time.time * pulseSpeed + i * 0.7f) : 1f;
                tr.localScale = Vector3.one * (baseScales[i] * pulse);
                if (isDiamond[i]) tr.Rotate(0f, 0f, spin[i] * dt, Space.World);

                Color c = baseColor[i];
                c.a = baseAlpha[i] * env * peakAlpha * global * density[i];
                renderers[i].color = c;
            }
        }

        // ================= Sprite tạo bằng code =================

        private static Sprite MakeSprite(int n, System.Func<float, float, float> alphaAt)
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
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), n); // 1 đơn vị world
        }
    }
}