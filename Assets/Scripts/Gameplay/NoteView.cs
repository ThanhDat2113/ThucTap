using UnityEngine;

namespace RhythmGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class NoteView : MonoBehaviour
    {
        [Header("Hold note")]
        [Tooltip("Thân của hold note. Để trống thì tự tạo.")]
        public SpriteRenderer body;
        [Range(0.1f, 1f)] public float bodyWidthRatio = 0.5f;
        [Range(0f, 1f)] public float bodyAlpha = 0.6f;
        [Tooltip("Sorting order của thân so với đầu note (NoteSkin sẽ tự đặt thân nằm dưới đầu).")]
        public int bodySortingOffset = 0;

        [Header("Tail marker")]
        [Tooltip("Sprite cho đuôi hold note (đánh dấu thời điểm thả). Để trống thì tự tạo.")]
        public SpriteRenderer tail;
        [Tooltip("Kích thước đuôi so với đầu note.")]
        [Range(0.3f, 1.5f)] public float tailScale = 1f;
        [Tooltip("Sorting order của đuôi so với đầu note.")]
        public int tailSortingOffset = 1;

        [Header("Visual Effects")]
        [Tooltip("Component hiệu ứng visual (glow, pulse, particles). Tự lấy nếu không gán.")]
        public NoteVisuals visuals;

        [Header("Đuôi sao chổi")]
        [Tooltip("Vệt sáng mờ dần phía trên note đang rơi, tạo cảm giác tốc độ")]
        public bool useTrail = true;
        [Tooltip("Chiều dài vệt (world units)")]
        [Range(0.2f, 3f)] public float trailLength = 1.1f;
        [Range(0f, 1f)] public float trailAlpha = 0.55f;
        [Tooltip("Bề rộng vệt so với bề rộng note")]
        [Range(0.3f, 1f)] public float trailWidthRatio = 0.7f;
        [Tooltip("Note mờ hiện dần trong khoảng này (giây) để không bị bật ra đột ngột. 0 = tắt")]
        [Range(0f, 0.5f)] public float spawnFadeTime = 0.18f;

        [Header("Skin")]
        [Tooltip("Vẽ sprite note đẹp bằng code. Tự thêm vào nếu prefab chưa có. Bỏ tick để dùng sprite gốc của prefab.")]
        public bool useSkin = true;
        public NoteSkin skin;

        public NoteData Data { get; private set; }
        public float TimeSeconds { get; private set; }
        public float HoldSeconds { get; private set; }
        public float TailTimeSeconds => TimeSeconds + HoldSeconds;
        public bool IsHold => HoldSeconds > 0f;
        public bool Judged { get; private set; }
        public bool Holding { get; private set; }
        public float TopY { get; private set; }

        SpriteRenderer sr;
        SpriteRenderer trail;
        static Sprite trailSprite;
        float setupTime;
        bool fadeIn;
        bool usedPalette;
        Color baseColor;
        int currentLane = 0;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (visuals == null) visuals = GetComponent<NoteVisuals>();

            if (useSkin)
            {
                if (skin == null) skin = GetComponent<NoteSkin>();
                if (skin == null) skin = gameObject.AddComponent<NoteSkin>();
                skin.Apply(sr);   // thay khối trắng bằng sprite đã vẽ
            }
        }

        public void Setup(NoteData data, float timeSeconds, float holdSeconds, Color color)
        {
            Data = data;
            TimeSeconds = timeSeconds;
            HoldSeconds = holdSeconds;
            Judged = false;
            Holding = false;
            color.a = 1f;
            Color laneColor = color;
            currentLane = data.lane;

            // Nếu skin tô màu theo background thì sprite đã mang màu sẵn, sr.color để trắng.
            // Ngược lại giữ cách cũ: sprite trắng/xám nhân với màu lane.
            usedPalette = skin != null && skin.TryApplyPalette(sr, laneColor, currentLane);
            if (usedPalette) color = Color.white;

            baseColor = color;
            sr.color = color;

            if (visuals != null)
            {
                visuals.Initialize(currentLane, laneColor);
            }

            setupTime = Time.time;
            fadeIn = spawnFadeTime > 0f;
            if (fadeIn) { Color c0 = sr.color; c0.a = 0f; sr.color = c0; }

            if (useTrail && !IsHold)
            {
                EnsureTrailObj();
                LayoutTrail();
                Color tc = usedPalette ? skin.GlowColor : laneColor;
                tc.a = fadeIn ? 0f : trailAlpha;
                trail.color = tc;
                trail.gameObject.SetActive(true);
            }
            else if (trail != null)
            {
                trail.gameObject.SetActive(false);
            }

            if (IsHold)
            {
                EnsureBody();
                EnsureTail();
                if (skin != null) skin.StyleHold(body, tail, sr);

                body.gameObject.SetActive(true);
                SetBodyColor(bodyAlpha);

                tail.gameObject.SetActive(true);
                SetTailColor(1f);
                tailScale = 1f; // force match head width
            }
            else
            {
                if (body != null) body.gameObject.SetActive(false);
                if (tail != null) tail.gameObject.SetActive(false);
            }
        }

        public void UpdatePosition(float songTime, float laneX, float hitLineY, float scrollSpeed)
        {
            if (fadeIn && !Judged)
            {
                float a = Mathf.Clamp01((Time.time - setupTime) / Mathf.Max(0.01f, spawnFadeTime));
                Color c = sr.color; c.a = a; sr.color = c;
                if (trail != null && trail.gameObject.activeSelf)
                {
                    Color tc = trail.color; tc.a = trailAlpha * a; trail.color = tc;
                }
                if (a >= 1f) fadeIn = false;
            }

            float headY = hitLineY + (TimeSeconds - songTime) * scrollSpeed;
            if (Holding) headY = hitLineY;
            transform.position = new Vector3(laneX, headY, 0f);

            TopY = headY;
            if (IsHold)
            {
                float tailY = hitLineY + (TailTimeSeconds - songTime) * scrollSpeed;
                TopY = Mathf.Max(headY, tailY);
                UpdateBody(headY, tailY);
                UpdateTail(tailY);
            }
        }

        public void MarkJudged()
        {
            Judged = true;
            if (trail != null) trail.gameObject.SetActive(false);
        }

        public void PlayHitEffect(Judgement judgement, Vector3 position)
        {
            if (visuals != null)
            {
                visuals.PlayHitEffect(judgement, position);
            }

            // vạch bấm của lane này chớp sáng
            if (ReceptorFlasher.Instance != null) ReceptorFlasher.Instance.FlashLane(currentLane);

            // nền phản ứng: vòng sóng trên sàn + trụ sáng tại điểm đánh
            if (AnimatedBackground.Instance != null)
            {
                bool perfect = judgement.ToString().IndexOf("Perfect", System.StringComparison.OrdinalIgnoreCase) >= 0;
                AnimatedBackground.Instance.Hit(position, perfect ? 1f : 0.7f);
            }
        }

        public void BeginHold()
        {
            Holding = true;
            SetBodyColor(0.9f);
            SetTailColor(0.9f);
        }

        public void BreakHold()
        {
            Holding = false;
            PlayMissVisual();
        }

        public void PlayMissVisual()
        {
            if (visuals != null)
            {
                visuals.PlayMissEffect();
            }
            if (trail != null) trail.gameObject.SetActive(false);
            if (AnimatedBackground.Instance != null) AnimatedBackground.Instance.Miss();

            Color c = baseColor * 0.5f;
            c.a = 0.5f;
            sr.color = c;

            if (IsHold)
            {
                if (body != null)
                {
                    Color b = baseColor * 0.5f;
                    b.a = 0.3f;
                    body.color = b;
                }
                if (tail != null)
                {
                    Color t = baseColor * 0.5f;
                    t.a = 0.3f;
                    tail.color = t;
                }
            }
        }

        // ---------- Đuôi sao chổi ----------
        void EnsureTrailObj()
        {
            if (trail != null) return;
            var go = new GameObject("Trail");
            go.transform.SetParent(transform, false);
            trail = go.AddComponent<SpriteRenderer>();
            trail.sprite = GetTrailSprite();
            trail.sortingLayerID = sr.sortingLayerID;
            trail.sortingOrder = sr.sortingOrder - 2;   // nằm dưới note và thân hold
        }

        void LayoutTrail()
        {
            if (trail == null) return;
            float psx = Mathf.Max(Mathf.Abs(transform.lossyScale.x), 0.0001f);
            float psy = Mathf.Max(Mathf.Abs(transform.lossyScale.y), 0.0001f);
            Vector2 coreLocal = skin != null
                ? skin.CoreLocalSize
                : (sr.sprite != null ? (Vector2)sr.sprite.bounds.size : Vector2.one);

            float widthWorld = coreLocal.x * psx * trailWidthRatio;
            // sprite đuôi rộng 0.25 và cao 1 đơn vị, gốc ở đáy -> kéo giãn để ra đúng kích thước thật
            trail.transform.localScale = new Vector3(widthWorld / (0.25f * psx), trailLength / psy, 1f);
            trail.transform.localPosition = new Vector3(0f, coreLocal.y * 0.35f, 0f);
        }

        // Dải đứng: đậm ở sát note, mờ dần lên phía trên, hai bên mép mờ
        static Sprite GetTrailSprite()
        {
            if (trailSprite != null) return trailSprite;
            const int W = 16, H = 64;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = y / (H - 1f);
                float along = Mathf.Pow(1f - v, 2f);
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W;
                    float side = Mathf.SmoothStep(0f, 1f, (1f - Mathf.Abs(2f * u - 1f)) * 1.6f);
                    byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(along * side) * 255f);
                    px[y * W + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            trailSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), H, 0, SpriteMeshType.FullRect);
            return trailSprite;
        }

        void EnsureBody()
        {
            if (body != null) return;

            var go = new GameObject("HoldBody");
            go.transform.SetParent(transform, false);
            body = go.AddComponent<SpriteRenderer>();
            body.sprite = sr.sprite;
            body.sortingLayerID = sr.sortingLayerID;
            body.sortingOrder = sr.sortingOrder + bodySortingOffset;
        }

        void EnsureTail()
        {
            if (tail != null) return;

            var go = new GameObject("HoldTail");
            go.transform.SetParent(transform, false);
            tail = go.AddComponent<SpriteRenderer>();
            tail.sprite = sr.sprite;
            tail.sortingLayerID = sr.sortingLayerID;
            tail.sortingOrder = sr.sortingOrder + tailSortingOffset;
        }

        void SetBodyColor(float alpha)
        {
            if (body == null) return;
            body.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        void SetTailColor(float alpha)
        {
            if (tail == null) return;
            tail.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }

        void UpdateBody(float headY, float tailY)
        {
            float length = Mathf.Max(0f, tailY - headY);
            float parentScaleY = Mathf.Max(transform.lossyScale.y, 0.0001f);

            // Sprite đầu có thêm vầng sáng nên lấy bề rộng phần thân thật (core), không dùng bounds của sprite
            float headWidth = skin != null
                ? skin.CoreLocalSize.x
                : (sr.sprite != null ? sr.sprite.bounds.size.x : 1f);
            Vector2 bodySize = body.sprite != null ? (Vector2)body.sprite.bounds.size : Vector2.one;

            float sx = headWidth * bodyWidthRatio / Mathf.Max(bodySize.x, 0.0001f);
            float sy = length / (parentScaleY * Mathf.Max(bodySize.y, 0.0001f));

            body.transform.localScale = new Vector3(sx, sy, 1f);
            body.transform.localPosition = new Vector3(0f, (length * 0.5f) / parentScaleY, 0f);
        }

        void UpdateTail(float tailY)
        {
            if (tail == null) return;

            float headWorldWidth = sr.bounds.size.x;
            Vector2 tailSize = tail.sprite != null ? (Vector2)tail.sprite.bounds.size : Vector2.one;
            float parentScaleX = Mathf.Max(transform.lossyScale.x, 0.0001f);
            float s = (headWorldWidth * tailScale) / (tailSize.x * parentScaleX);
            tail.transform.localScale = new Vector3(s, s, 1f);

            tail.transform.position = new Vector3(transform.position.x, tailY, 0f);
        }
    }
}