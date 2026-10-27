using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    // Giữ nguyên thứ tự các giá trị đã có để prefab cũ không bị đổi style. Kiểu mới luôn thêm vào cuối.
    public enum NoteStyle
    {
        Neon, Glass, Capsule, Custom,
        Outline, Crystal, Flat, Soft,
        Slant, Bubble, Laser, Petal, Cyber, Candy, Gem, Arrow,
        Street
    }

    // Cách chọn màu note so với nền
    public enum NoteHarmony
    {
        Match,    // dùng đúng các màu của nền (hài hoà, nhưng dễ chìm nếu nền cùng tông)
        Accent,   // màu đầu note lệch tông so với nền để nổi bật hơn
        Auto      // nền neon rực -> Accent, nền nhạt / xám -> Match
    }

    /// <summary>
    /// Tự vẽ sprite note đẹp bằng code (viền sáng, vầng sáng, gradient, nhiều hình dạng) thay cho khối trắng của prefab.
    /// Hình được vẽ theo kích thước THẬT của note nên góc bo không bị méo dù prefab scale lệch trục.
    ///
    /// Kiểu note:
    ///   Neon / Glass / Capsule / Outline / Crystal / Flat / Soft : hình chữ nhật bo góc, khác nhau về viền và độ sáng
    ///   Slant  - hình bình hành nghiêng        Bubble - bong bóng bóng loáng
    ///   Laser  - tia sáng mảnh, glow lớn        Petal  - cánh hoa nhọn hai đầu
    ///   Cyber  - cắt góc, có rãnh kỹ thuật      Candy  - kẹo có sọc chéo
    ///   Gem    - đá quý lục giác có mặt cắt     Arrow  - mũi tên chỉ xuống
    ///   Street - bảng kim loại tối có lưới loa, viền neon, vệt sơn graffiti (giống nền sân khấu neon)
    ///   Custom - tự kéo các thanh trượt
    ///
    /// Màu: nếu trong scene có AnimatedBackground (hoặc WorldBackgroundGradientPro) và bật Use Background Palette
    /// thì note được tô gradient theo màu nền, pha thêm màu lane (Lane Tint).
    ///
    /// API cho NoteThemeApplier: SetGlobalStyle(style), ClearGlobalStyle(), SetStyle(style).
    /// </summary>
    public class NoteSkin : MonoBehaviour
    {
        [Header("Kiểu note")]
        public NoteStyle style = NoteStyle.Neon;
        [Tooltip("Kích thước phần thân note (world units: rộng, cao). 0 = giữ nguyên kích thước hiện tại của prefab")]
        public Vector2 sizeOverride = Vector2.zero;

        [Header("Màu theo background")]
        [Tooltip("Tô note bằng gradient lấy từ nền (AnimatedBackground hoặc WorldBackgroundGradientPro)")]
        public bool useBackgroundPalette = true;
        [Tooltip("Tự tìm trong scene nếu để trống (thường nằm trên Main Camera)")]
        public WorldBackgroundGradientPro background;
        [Tooltip("Mức pha màu lane vào màu note. 0 = giống hệt nền, 1 = hoàn toàn màu lane")]
        [Range(0f, 1f)] public float laneTint = 0.35f;
        [Tooltip("Match = cùng tông nền, Accent = đầu note lệch tông để nổi bật, Auto = tự chọn theo nền")]
        public NoteHarmony harmony = NoteHarmony.Auto;
        [Tooltip("Mức lệch tông của Accent (0.5 = màu đối hoàn toàn, 0.42 = gần đối)")]
        [Range(0f, 0.5f)] public float accentShift = 0.42f;

        [Header("Màu dự phòng (dùng khi không tìm thấy nền AnimatedBackground / WorldBackgroundGradientPro)")]
        public Color fallbackTop = new Color(0.20f, 0.08f, 0.45f);
        public Color fallbackMiddle = new Color(1.00f, 0.25f, 0.85f);
        public Color fallbackBottom = new Color(0.25f, 0.90f, 1.00f);

        [Header("Ruột note")]
        [Tooltip("Làm sáng và đậm màu phần ruột note. 0 = ruột tối như kiểu gốc, 1 = ruột sáng đầy màu")]
        [Range(0f, 1f)] public float fillLift = 0.55f;
        [Tooltip("Ruột luôn đặc (không trong suốt) cho mọi kiểu, kể cả Outline / Glass")]
        public bool solidFill = true;

        [Header("Tách note khỏi nền")]
        [Tooltip("Viền tối mảnh bao quanh note, nằm giữa note và vầng sáng, giúp note không chìm vào nền nhiều màu")]
        [Range(0f, 1f)] public float edgeShadow = 0.5f;

        [Header("Tuỳ chỉnh (chỉ có tác dụng khi Style = Custom)")]
        [Tooltip("Độ bo góc, tính theo % chiều cao note")]
        [Range(0f, 0.5f)] public float cornerRadius = 0.3f;
        [Tooltip("Độ dày viền sáng, tính theo % chiều cao note")]
        [Range(0f, 0.5f)] public float borderWidth = 0.12f;
        [Tooltip("Vầng sáng bên ngoài, tính theo % chiều cao note")]
        [Range(0f, 2f)] public float glowSize = 0.7f;
        [Range(0f, 1f)] public float glowAlpha = 0.55f;
        [Range(0f, 1f)] public float fillBottom = 0.25f;
        [Range(0f, 1f)] public float fillTop = 0.45f;
        [Range(0f, 1f)] public float fillAlpha = 0.9f;
        [Range(0f, 1f)] public float shine = 0f;
        [Range(0f, 1f)] public float innerGlow = 0.5f;

        public Sprite HeadSprite { get; private set; }
        public Sprite BodySprite { get; private set; }
        /// <summary>Kích thước phần thân note (không tính vầng sáng) theo đơn vị local của prefab.</summary>
        public Vector2 CoreLocalSize { get; private set; } = Vector2.one;

        struct Params
        {
            public float corner, border, glowSize, glowAlpha, fillBottom, fillTop, fillAlpha, shine, innerGlow;
            public int shape;      // 0 chữ nhật bo, 1 nghiêng, 2 cắt góc, 3 mũi tên, 4 cánh hoa, 5 lục giác
            public int pattern;    // 0 không, 1 sọc kẹo, 2 mặt cắt đá quý, 3 rãnh cyber, 4 vệt bóng bong bóng
            public float thin;     // 1 = cao đủ chiều cao note, nhỏ hơn = mảnh hơn (Laser)
            public float shadow;   // độ đậm viền tối quanh note
            public float lift;     // mức làm sáng ruột note
        }

        struct Pal
        {
            public bool on;
            public Color bot, top, glow, border, shadow;
        }

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        // vệt sơn graffiti của kiểu Street: x, y (theo tỉ lệ nửa rộng / nửa cao note), bán kính (theo chiều cao note)
        static readonly Vector3[] Splats =
        {
            new Vector3(-0.78f,  0.35f, 0.10f),
            new Vector3(-0.55f, -0.45f, 0.05f),
            new Vector3( 0.62f,  0.40f, 0.07f),
            new Vector3( 0.86f, -0.20f, 0.12f),
            new Vector3( 0.10f,  0.55f, 0.04f),
            new Vector3(-0.20f, -0.55f, 0.06f)
        };

        // kiểu toàn cục: khi đặt, MỌI note (kể cả note tạo sau) dùng kiểu này thay vì style riêng
        static bool globalSet;
        static NoteStyle globalStyle = NoteStyle.Neon;

        public static void SetGlobalStyle(NoteStyle s) { globalStyle = s; globalSet = true; }
        public static void ClearGlobalStyle() { globalSet = false; }

        NoteStyle EffectiveStyle { get { return globalSet ? globalStyle : style; } }

        // đo kích thước note một lần duy nhất (trước khi sprite bị thay bằng sprite có vầng sáng)
        bool measured;
        Vector2 core;
        float sx, sy;
        bool bgSearched;
        AnimatedBackground animBg;

        SpriteRenderer headRenderer;
        bool paletteActive;
        Color lastLane = Color.white;
        int lastLaneIndex = -1;

        /// <summary>Đổi kiểu cho note này và vẽ lại sprite ngay.</summary>
        public void SetStyle(NoteStyle s)
        {
            style = s;
            Refresh();
        }

        void Refresh()
        {
            if (headRenderer == null) return;
            if (paletteActive) TryApplyPalette(headRenderer, lastLane, lastLaneIndex);
            else Apply(headRenderer);
        }

        static Params P(float corner, float border, float glowSize, float glowAlpha,
                        float fillBottom, float fillTop, float fillAlpha, float shine, float innerGlow,
                        int shape = 0, int pattern = 0, float thin = 1f)
        {
            return new Params
            {
                corner = corner, border = border, glowSize = glowSize, glowAlpha = glowAlpha,
                fillBottom = fillBottom, fillTop = fillTop, fillAlpha = fillAlpha, shine = shine, innerGlow = innerGlow,
                shape = shape, pattern = pattern, thin = thin
            };
        }

        Params GetParams()
        {
            Params p = StyleParams();
            p.shadow = edgeShadow;
            p.lift = fillLift;
            // ruột đầy màu: nâng độ sáng của dải gradient ruột và không để trong suốt
            if (EffectiveStyle != NoteStyle.Custom)   // Custom giữ nguyên các thanh trượt do bạn tự đặt
            {
                p.fillBottom = Mathf.Lerp(p.fillBottom, 0.85f, fillLift);
                p.fillTop = Mathf.Lerp(p.fillTop, 1.00f, fillLift);
                if (solidFill) p.fillAlpha = Mathf.Max(p.fillAlpha, 0.95f);
            }
            return p;
        }

        Params StyleParams()
        {
            switch (EffectiveStyle)
            {
                //                       corner border glowSz glowA  fillB  fillT  fillA  shine  inner  shape pat thin
                case NoteStyle.Neon:    return P(0.30f, 0.12f, 0.70f, 0.55f, 0.25f, 0.45f, 0.90f, 0.00f, 0.50f);
                case NoteStyle.Glass:   return P(0.35f, 0.06f, 0.45f, 0.40f, 0.60f, 0.95f, 0.80f, 0.35f, 0.30f);
                case NoteStyle.Capsule: return P(0.50f, 0.08f, 0.60f, 0.50f, 0.70f, 0.90f, 0.95f, 0.20f, 0.30f);
                case NoteStyle.Outline: return P(0.30f, 0.10f, 0.60f, 0.50f, 0.10f, 0.25f, 0.18f, 0.00f, 0.60f);
                case NoteStyle.Crystal: return P(0.20f, 0.07f, 0.50f, 0.45f, 0.55f, 1.00f, 0.85f, 0.60f, 0.40f);
                case NoteStyle.Flat:    return P(0.15f, 0.00f, 0.00f, 0.00f, 0.85f, 0.95f, 1.00f, 0.00f, 0.00f);
                case NoteStyle.Soft:    return P(0.50f, 0.04f, 1.20f, 0.60f, 0.50f, 0.80f, 0.90f, 0.10f, 0.50f);
                case NoteStyle.Slant:   return P(0.18f, 0.10f, 0.65f, 0.55f, 0.30f, 0.50f, 0.92f, 0.05f, 0.50f, 1, 0);
                case NoteStyle.Bubble:  return P(0.50f, 0.05f, 0.50f, 0.45f, 0.45f, 0.80f, 0.72f, 0.15f, 0.40f, 0, 4);
                case NoteStyle.Laser:   return P(0.50f, 0.30f, 0.95f, 0.70f, 0.60f, 0.90f, 1.00f, 0.00f, 0.30f, 0, 0, 0.30f);
                case NoteStyle.Petal:   return P(0.00f, 0.07f, 0.60f, 0.45f, 0.45f, 0.85f, 0.92f, 0.25f, 0.40f, 4, 0);
                case NoteStyle.Cyber:   return P(0.05f, 0.07f, 0.60f, 0.50f, 0.30f, 0.55f, 0.92f, 0.05f, 0.50f, 2, 3);
                case NoteStyle.Candy:   return P(0.50f, 0.07f, 0.45f, 0.40f, 0.75f, 0.95f, 1.00f, 0.25f, 0.20f, 0, 1);
                case NoteStyle.Gem:     return P(0.05f, 0.06f, 0.55f, 0.50f, 0.50f, 0.90f, 0.92f, 0.30f, 0.30f, 5, 2);
                case NoteStyle.Arrow:   return P(0.10f, 0.09f, 0.65f, 0.55f, 0.35f, 0.60f, 0.95f, 0.10f, 0.45f, 3, 0);
                case NoteStyle.Street:  return P(0.12f, 0.07f, 0.60f, 0.50f, 0.20f, 0.30f, 0.97f, 0.00f, 0.00f, 0, 5);
                default:                return P(cornerRadius, borderWidth, glowSize, glowAlpha,
                                                 fillBottom, fillTop, fillAlpha, shine, innerGlow);
            }
        }

        void Measure(SpriteRenderer head)
        {
            if (measured) return;
            Vector3 ls = transform.lossyScale;
            sx = Mathf.Max(Mathf.Abs(ls.x), 0.0001f);
            sy = Mathf.Max(Mathf.Abs(ls.y), 0.0001f);

            core = (sizeOverride.x > 0f && sizeOverride.y > 0f) ? sizeOverride : (Vector2)head.bounds.size;
            if (core.x < 0.01f || core.y < 0.01f) core = new Vector2(1.5f, 0.35f);
            CoreLocalSize = new Vector2(core.x / sx, core.y / sy);
            measured = true;
        }

        /// <summary>Dựng sprite trắng/xám theo kích thước thật của note (dùng khi chưa có palette của background).</summary>
        public void Apply(SpriteRenderer head)
        {
            headRenderer = head;
            paletteActive = false;
            Measure(head);
            Pal none = new Pal { on = false };
            HeadSprite = GetHeadSprite(GetParams(), none);
            BodySprite = GetBodySprite(none);
            head.sprite = HeadSprite;
        }

        /// <summary>
        /// Tô note bằng gradient của background (đã pha màu lane). Trả về true nếu thành công;
        /// khi đó sprite đã mang màu sẵn nên NoteView phải để sr.color = trắng.
        /// </summary>
        public bool TryApplyPalette(SpriteRenderer head, Color laneColor, int laneIndex = -1)
        {
            headRenderer = head;
            if (!useBackgroundPalette) { paletteActive = false; return false; }
            Measure(head);

            if (!bgSearched)
            {
                animBg = Object.FindFirstObjectByType<AnimatedBackground>();
                if (animBg == null && background == null)
                    background = Object.FindFirstObjectByType<WorldBackgroundGradientPro>();
                bgSearched = true;
            }

            // ưu tiên AnimatedBackground (theme hiện tại), không có thì dùng gradient cũ
            Color T, M, B;
            if (animBg != null)
            {
                T = animBg.paletteTop; M = animBg.paletteMiddle; B = animBg.paletteBottom;
            }
            else if (background != null)
            {
                T = background.topColor; M = background.middleColor; B = background.bottomColor;
            }
            else
            {
                // không có nền nào để lấy màu: dùng bảng màu dự phòng để note luôn có màu, không bị trắng / đen
                T = fallbackTop; M = fallbackMiddle; B = fallbackBottom;
            }

            Pal pal = MakePalette(T, M, B, laneColor, laneIndex);

            HeadSprite = GetHeadSprite(GetParams(), pal);
            BodySprite = GetBodySprite(pal);
            head.sprite = HeadSprite;

            paletteActive = true;
            lastLane = laneColor;
            lastLaneIndex = laneIndex;
            return true;
        }

        /// <summary>Gán sprite cho thân và đuôi của hold note (đuôi dùng chung hình với đầu).</summary>
        public void StyleHold(SpriteRenderer body, SpriteRenderer tail, SpriteRenderer head)
        {
            if (body != null)
            {
                body.sprite = BodySprite;
                body.sortingLayerID = head.sortingLayerID;
                body.sortingOrder = head.sortingOrder - 1;   // thân nằm dưới đầu note
            }
            if (tail != null)
            {
                tail.sprite = HeadSprite;
                tail.sortingLayerID = head.sortingLayerID;
                tail.sortingOrder = head.sortingOrder + 1;
            }
        }

        // ---------- Palette ----------

        static float Lum(Color c) { return 0.299f * c.r + 0.587f * c.g + 0.114f * c.b; }

        static Color Boost(Color c, float satMul, float minV, float maxV)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            s = Mathf.Clamp01(s * satMul);
            v = Mathf.Clamp(v, minV, maxV);
            Color o = Color.HSVToRGB(h, s, v);
            o.a = 1f;
            return o;
        }

        static float Sat(Color c)
        {
            Color.RGBToHSV(c, out float h, out float sat, out float v);
            return sat;
        }

        static Color ShiftHue(Color c, float turns)
        {
            Color.RGBToHSV(c, out float h, out float sat, out float v);
            Color o = Color.HSVToRGB(Mathf.Repeat(h + turns, 1f), sat, v);
            o.a = 1f;
            return o;
        }

        // Đẩy độ sáng của màu ra xa độ sáng nền cho tới khi đủ tương phản
        static Color Ensure(Color c, float bgLum, float minDelta)
        {
            Color.RGBToHSV(c, out float h, out float sat, out float v);
            float dir = bgLum < 0.5f ? 1f : -1f;
            for (int i = 0; i < 10; i++)
            {
                Color t = Color.HSVToRGB(h, sat, v);
                if (Mathf.Abs(Lum(t) - bgLum) >= minDelta) break;
                v = Mathf.Clamp01(v + dir * 0.08f);
                sat = Mathf.Clamp01(sat - dir * 0.03f);   // sáng hơn thì bớt đậm một chút, tối hơn thì đậm thêm
                if ((dir > 0f && v >= 1f) || (dir < 0f && v <= 0f)) break;
            }
            Color o = Color.HSVToRGB(h, sat, v);
            o.a = 1f;
            return o;
        }

        static readonly Color[] DefaultLaneColors =
        {
            new Color(1.00f, 0.30f, 0.75f),
            new Color(0.20f, 0.90f, 1.00f),
            new Color(0.70f, 0.40f, 1.00f),
            new Color(1.00f, 0.85f, 0.30f)
        };

        Pal MakePalette(Color T, Color M, Color B, Color lane, int laneIndex)
        {
            // màu lane trắng / xám (chưa được đặt) thì pha vào chỉ làm note nhạt đi:
            // dùng bảng màu lane mặc định để 4 lane vẫn khác màu nhau
            float tint = laneTint;
            if (Sat(lane) < 0.15f)
            {
                if (laneIndex >= 0)
                {
                    lane = DefaultLaneColors[laneIndex % DefaultLaneColors.Length];
                    tint = Mathf.Max(laneTint, 0.5f);
                }
                else tint = 0f;
            }

            float lT = Lum(T), lM = Lum(M), lB = Lum(B);
            float lum = (lT + lM + lB) / 3f;
            float bgLum = 0.5f * lM + 0.25f * (lT + lB);   // vùng giữa màn hình (chỗ có note) quan trọng nhất
            float avgSat = (Sat(T) + Sat(M) + Sat(B)) / 3f;
            bool dark = lum < 0.6f;
            bool accent = harmony == NoteHarmony.Accent
                          || (harmony == NoteHarmony.Auto && dark && avgSat > 0.5f);

            Pal pal = new Pal { on = true };
            if (dark)
            {
                // nền tối: note sáng rực
                pal.bot = Boost(M, 1.1f, 0.80f, 1f);
                pal.top = Boost(B, 1.0f, 0.95f, 1f);
                if (accent) pal.top = ShiftHue(pal.top, accentShift);
                pal.border = Color.Lerp(pal.top, Color.white, 0.75f);
                pal.shadow = Color.Lerp(T, Color.black, 0.85f);
            }
            else
            {
                // nền sáng: note đậm hơn để không chìm
                pal.bot = Boost(T, 2.2f, 0.45f, 0.60f);
                pal.top = Boost(T, 2.0f, 0.70f, 0.85f);
                if (accent) pal.top = ShiftHue(pal.top, accentShift);
                pal.border = Boost(T, 2.2f, 0.30f, 0.40f);
                pal.shadow = Color.Lerp(T, Color.black, 0.70f);
            }
            pal.glow = pal.top;

            // pha màu lane để vẫn phân biệt được các lane
            lane.a = 1f;
            pal.bot = Color.Lerp(pal.bot, lane, tint);
            pal.top = Color.Lerp(pal.top, lane, tint);
            pal.glow = Color.Lerp(pal.glow, lane, tint);
            pal.border = Color.Lerp(pal.border, lane, tint * 0.4f);

            // đảm bảo note luôn đủ tương phản với độ sáng của nền
            pal.bot = Ensure(pal.bot, bgLum, 0.28f);
            pal.top = Ensure(pal.top, bgLum, 0.28f);
            pal.border = Ensure(pal.border, bgLum, 0.45f);
            return pal;
        }

        static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }

        Sprite GetHeadSprite(Params p, Pal pal)
        {
            string key = string.Format("head|{0:F3}|{1:F3}|{2:F3}|{3:F3}|{4:F3}|{5:F3}|{6:F3}|{7:F3}|{8:F3}|{9:F3}|{10:F3}|{11:F3}|{12:F3}|{13}|{14}|{15}|{16:F2}|{17}|{18:F2}|{19:F2}",
                core.x, core.y, sx, sy, p.corner, p.border, p.glowSize, p.glowAlpha, p.fillBottom, p.fillTop, p.fillAlpha, p.shine, p.innerGlow,
                p.shape, p.pattern, "s", p.thin,
                pal.on ? Hex(pal.bot) + Hex(pal.top) + Hex(pal.glow) + Hex(pal.border) + Hex(pal.shadow) : "g",
                p.shadow, p.lift);
            if (!cache.TryGetValue(key, out Sprite s) || s == null)
            {
                s = BuildHead(core, sx, sy, p, pal);
                cache[key] = s;
            }
            return s;
        }

        Sprite GetBodySprite(Pal pal)
        {
            string key = "body|" + (pal.on ? Hex(pal.bot) + Hex(pal.top) + Hex(pal.border) : "g");
            if (!cache.TryGetValue(key, out Sprite s) || s == null)
            {
                s = BuildBody(pal);
                cache[key] = s;
            }
            return s;
        }

        static byte To8(float x) { return (byte)Mathf.RoundToInt(Mathf.Clamp01(x) * 255f); }

        // ---------- Hình dạng (khoảng cách có dấu, d < 0 là bên trong) ----------

        static float RoundBox(float x, float y, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x) - (hx - r);
            float qy = Mathf.Abs(y) - (hy - r);
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                   + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        // hình chữ nhật bị cắt bốn góc theo mặt phẳng 45 độ; c = độ lớn vết cắt
        static float Chamfer(float x, float y, float hx, float hy, float c)
        {
            float qx = Mathf.Abs(x) - hx;
            float qy = Mathf.Abs(y) - hy;
            float box = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f);
            float plane = (Mathf.Abs(x) + Mathf.Abs(y) - (hx + hy - c)) * 0.70711f;
            return Mathf.Max(box, plane);
        }

        static float Sdf(int shape, float wx, float wy, float hx, float hy, float r)
        {
            switch (shape)
            {
                case 1: // hình bình hành nghiêng
                {
                    float k = 0.5f;
                    return RoundBox(wx - k * wy, wy, Mathf.Max(hx - k * hy, 0.0001f), hy, r);
                }
                case 2: return Chamfer(wx, wy, hx, hy, 0.9f * hy);   // cắt góc (Cyber)
                case 5: return Chamfer(wx, wy, hx, hy, hy);          // lục giác (Gem)
                case 3: // mũi tên chỉ xuống
                {
                    float ax = Mathf.Max(hx - r, 0.0001f), ay = Mathf.Max(hy - r, 0.0001f);
                    float s = 0.8f * ay / ax;
                    float dTop = wy - ay;
                    float dSide = Mathf.Abs(wx) - ax;
                    float dBot = (-ay + s * Mathf.Abs(wx) - wy) / Mathf.Sqrt(1f + s * s);
                    return Mathf.Max(Mathf.Max(dTop, dSide), dBot) - r;
                }
                case 4: // cánh hoa: nhọn hai đầu
                {
                    float u = wx / hx;
                    float f = Mathf.Abs(wy) / hy + u * u - 1f;
                    return f * hy * 0.8f;
                }
                default: return RoundBox(wx, wy, hx, hy, r);
            }
        }

        // ---------- Vẽ sprite ----------

        // Vẽ hình bằng khoảng cách có dấu (SDF) trong toạ độ WORLD:
        // mỗi pixel biết nó nằm ở đâu trong thế giới thật nên góc bo luôn tròn, không bị méo theo scale.
        Sprite BuildHead(Vector2 core, float sx, float sy, Params p, Pal pal)
        {
            float cw = core.x, ch = core.y;
            float pad = p.glowSize * ch;
            float Wt = cw + 2f * pad, Ht = ch + 2f * pad;

            // số pixel sao cho sau khi bị scale của prefab kéo giãn, pixel vẫn vuông trong thế giới thật
            float ratio = (Ht / Wt) * (sx / sy);
            int W, H;
            if (ratio <= 1f) { W = 512; H = Mathf.Max(16, Mathf.RoundToInt(512f * ratio)); }
            else { H = 512; W = Mathf.Max(16, Mathf.RoundToInt(512f / ratio)); }

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[W * H];

            float hx = cw * 0.5f;
            float hy = ch * 0.5f * Mathf.Clamp(p.thin, 0.1f, 1f);   // nửa chiều cao của HÌNH (Laser mảnh hơn note)
            float r = Mathf.Clamp(p.corner * ch, 0f, Mathf.Min(hx, hy));
            float bw = p.border * ch;
            float e = Mathf.Max(Wt / W, Ht / H);        // độ rộng mép mượt (1 pixel)
            float glowLen = Mathf.Max(pad, 0.0001f);
            float ringW = Mathf.Max(0.06f * ch, 0.0001f);   // bề dày viền tối quanh note

            for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
            {
                float wx = ((i + 0.5f) / W - 0.5f) * Wt;
                float wy = ((j + 0.5f) / H - 0.5f) * Ht;

                float d = Sdf(p.shape, wx, wy, hx, hy, r);   // d < 0: bên trong, d > 0: bên ngoài

                float inside = Mathf.Clamp01(0.5f - d / e);
                float edge = bw > 0f ? Mathf.Clamp01(0.5f + (d + bw) / e) : 0f;   // 1 ở dải viền sát mép

                // ruột note: gradient dọc + dải bóng + sáng dần về phía viền
                float t = Mathf.Clamp01(wy / (2f * hy) + 0.5f);
                float shineTerm = p.shine * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.95f, t)) * 0.5f;
                float innerTerm = p.innerGlow * 0.35f * Mathf.Exp(Mathf.Min(d, 0f) / (0.35f * ch));
                float fillV = Mathf.Lerp(p.fillBottom, p.fillTop, t);

                // hoạ tiết bên trong từng kiểu
                float pat = 0f, mul = 1f;
                if (p.pattern != 0 && d < 0f)
                {
                    switch (p.pattern)
                    {
                        case 1: // sọc chéo của kẹo
                        {
                            float sv = Mathf.Sin((wx + 0.8f * wy) / (0.45f * ch) * Mathf.PI);
                            pat = 0.10f * Mathf.Clamp(sv * 3f, -1f, 1f);
                            break;
                        }
                        case 2: // mặt cắt của đá quý
                        {
                            float ang = Mathf.Atan2(wy / hy, wx / hx);
                            float f = Mathf.Floor(ang / (Mathf.PI * 0.25f));
                            pat = 0.16f * Mathf.Sin(f * 2.4f);
                            break;
                        }
                        case 3: // rãnh kỹ thuật của Cyber
                        {
                            float u = Mathf.Repeat(wx / (0.4f * hx) + 0.5f, 1f);
                            float ln = Mathf.Abs(u - 0.5f);
                            mul = 1f - 0.35f * (1f - Mathf.SmoothStep(0f, 0.05f, ln));
                            break;
                        }
                        case 4: // vệt bóng của bong bóng
                        {
                            float ex = (wx + 0.3f * hx) / (0.45f * hx);
                            float ey = (wy - 0.35f * hy) / (0.30f * hy);
                            pat = 0.5f * Mathf.Exp(-(ex * ex + ey * ey));
                            break;
                        }
                    }
                }

                // vầng sáng bên ngoài
                float glow = 0f;
                if (d > 0f && pad > 0f)
                {
                    glow = p.glowAlpha * Mathf.Exp(-3f * d / glowLen);
                    glow *= Mathf.Clamp01((pad - d) / (0.25f * pad));   // tắt hẳn trước mép texture
                }

                // viền tối mảnh sát mép note, nằm dưới vầng sáng: tách note khỏi nền nhiều màu
                float ringA = 0f;
                if (d > 0f && p.shadow > 0f && pad > 0f)
                    ringA = p.shadow * 0.85f * Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(d / ringW));
                float glowK = glow * (1f - ringA);
                float outA = ringA + glowK;

                float insideA = Mathf.Lerp(p.fillAlpha, 1f, edge);
                float a = Mathf.Lerp(outA, insideA, inside);

                // kiểu Street: thân kim loại tối + lưới loa + đường neon bên trong + viền neon + vệt sơn
                if (p.pattern == 5)
                {
                    Color cb = pal.on ? pal.bot : Color.white;
                    Color ct = pal.on ? pal.top : Color.white;
                    Color cs = pal.on ? pal.shadow : Color.black;
                    Color neonC = Color.Lerp(cb, ct, t);

                    Color fillS = Color.Lerp(cs, neonC, Mathf.Lerp(0.20f, 0.70f, p.lift));
                    fillS = Color.Lerp(fillS, neonC, 0.10f * t);

                    // lưới loa: các lỗ tròn xếp so le
                    float gs = 0.075f * ch;
                    float gx = Mathf.Repeat(wx / gs, 1f) - 0.5f;
                    float stag = Mathf.Abs(Mathf.Floor(wx / gs) % 2f) * 0.5f;
                    float gy = Mathf.Repeat(wy / gs + stag, 1f) - 0.5f;
                    float hole = Mathf.SmoothStep(0.30f, 0.20f, Mathf.Sqrt(gx * gx + gy * gy));
                    fillS = new Color(fillS.r * (1f - 0.45f * hole), fillS.g * (1f - 0.45f * hole), fillS.b * (1f - 0.45f * hole), 1f);

                    // đường neon chạy bên trong, cách mép một đoạn
                    float inset = bw * 2.2f;
                    float lnS = Mathf.SmoothStep(bw * 0.35f, 0f, Mathf.Abs(d + inset));
                    fillS = Color.Lerp(fillS, neonC, lnS * 0.95f);

                    // vệt sơn graffiti, xen kẽ hai màu
                    float splat = 0f;
                    int splatIdx = 0;
                    if (d < 0f)
                    {
                        for (int k = 0; k < Splats.Length; k++)
                        {
                            float dist = new Vector2(wx - Splats[k].x * hx, wy - Splats[k].y * hy).magnitude;
                            float rr = Splats[k].z * ch;
                            float sp = Mathf.SmoothStep(rr, rr * 0.8f, dist);
                            if (sp > splat) { splat = sp; splatIdx = k; }
                        }
                    }
                    Color splatC = (splatIdx % 2 == 0) ? cb : ct;
                    fillS = Color.Lerp(fillS, Color.Lerp(splatC, Color.white, 0.15f), splat * 0.85f);

                    // viền neon sáng
                    fillS = Color.Lerp(fillS, Color.Lerp(neonC, Color.white, 0.35f), edge);

                    Color gC = pal.on ? pal.glow : Color.white;
                    Color outS = outA > 0.0001f
                        ? new Color((cs.r * ringA + gC.r * glowK) / outA,
                                    (cs.g * ringA + gC.g * glowK) / outA,
                                    (cs.b * ringA + gC.b * glowK) / outA, 1f)
                        : gC;
                    Color rgbS = Color.Lerp(outS, fillS, inside);
                    if (pal.on)
                    {
                        px[j * W + i] = new Color32(To8(rgbS.r), To8(rgbS.g), To8(rgbS.b), To8(a));
                    }
                    else
                    {
                        byte g8 = To8(rgbS.r);
                        px[j * W + i] = new Color32(g8, g8, g8, To8(a));
                    }
                    continue;
                }

                if (pal.on)
                {
                    float bright = (0.6f + 0.4f * fillV + shineTerm + innerTerm + pat) * mul;
                    Color g = Color.Lerp(pal.bot, pal.top, t);
                    Color fill = new Color(g.r * bright, g.g * bright, g.b * bright, 1f);
                    fill = Color.Lerp(fill, pal.border, edge);
                    Color outC = outA > 0.0001f
                        ? new Color((pal.shadow.r * ringA + pal.glow.r * glowK) / outA,
                                    (pal.shadow.g * ringA + pal.glow.g * glowK) / outA,
                                    (pal.shadow.b * ringA + pal.glow.b * glowK) / outA, 1f)
                        : pal.glow;
                    Color rgb = Color.Lerp(outC, fill, inside);
                    px[j * W + i] = new Color32(To8(rgb.r), To8(rgb.g), To8(rgb.b), To8(a));
                }
                else
                {
                    float v = (fillV + shineTerm + innerTerm + pat) * mul;
                    v = Mathf.Lerp(Mathf.Clamp01(v), 1f, edge);
                    float outL = outA > 0.0001f ? glowK / outA : 1f;   // viền tối = đen, vầng sáng = trắng
                    float c = Mathf.Lerp(outL, v, inside);
                    byte c8 = To8(c);
                    px[j * W + i] = new Color32(c8, c8, c8, To8(a));
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            float ppu = W * sx / Wt;   // để chiều rộng sprite trong thế giới = Wt
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
        }

        // Thân hold note: dải đứng, hai mép sáng, giữa sáng nhẹ. Bị kéo dãn theo chiều dọc nên không có góc bo.
        Sprite BuildBody(Pal pal)
        {
            int W = 64, H = 4;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[W * H];
            Color mid = pal.on ? Color.Lerp(pal.bot, pal.top, 0.5f) : Color.white;
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W;
                float de = Mathf.Min(u, 1f - u);
                float line = Mathf.Clamp01(1f - de / 0.16f);
                line *= line;
                float center = 1f - Mathf.Abs(2f * u - 1f);
                float v = Mathf.Clamp01(0.45f + 0.30f * center + 0.60f * line);
                float a = Mathf.Clamp01(0.55f + 0.45f * line);

                Color32 c;
                if (pal.on)
                {
                    float k = 0.55f + 0.45f * center;
                    Color f = new Color(mid.r * k, mid.g * k, mid.b * k, 1f);
                    f = Color.Lerp(f, pal.border, line * 0.7f);
                    c = new Color32(To8(f.r), To8(f.g), To8(f.b), To8(a));
                }
                else
                {
                    byte v8 = To8(v);
                    c = new Color32(v8, v8, v8, To8(a));
                }
                for (int y = 0; y < H; y++) px[y * W + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        }
    }
}