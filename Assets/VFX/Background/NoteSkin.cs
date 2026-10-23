using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    // Giữ nguyên thứ tự 4 giá trị đầu để không lệch giá trị đã lưu trong Inspector
    public enum NoteStyle
    {
        Neon, Glass, Capsule, Custom,
        Outline, Cyber, Slant, Gem, Arrow, Petal, Bubble, Pixel, Laser, Candy, Flat
    }

    public enum NoteShape { Rounded, Chamfer, Slant, Gem, Arrow, Ellipse }

    /// <summary>
    /// Tự vẽ sprite note đẹp bằng code thay cho khối trắng của prefab. 15 kiểu có sẵn:
    ///   Neon, Glass, Capsule        : bo góc, viền sáng / kính / viên thuốc
    ///   Outline                     : chỉ có viền (gọn, hợp nền rối)
    ///   Cyber                       : vuông vát góc + vạch scanline
    ///   Slant                       : hình bình hành nghiêng (tốc độ)
    ///   Gem                         : hình ngọc nhọn hai đầu, có vạch sáng giữa
    ///   Arrow                       : mũi tên chỉ xuống vạch bấm
    ///   Petal                       : cánh hoa / chiếc lá (oval mềm)
    ///   Bubble                      : bong bóng trong suốt, có điểm bóng
    ///   Pixel                       : khối pixel retro
    ///   Laser                       : tia sáng, lõi trắng nóng
    ///   Candy                       : viên kẹo sọc chéo
    ///   Flat                        : phẳng, tối giản (hợp nền sáng)
    ///   Custom                      : tự chỉnh mọi thông số bên dưới
    /// Hình được vẽ theo kích thước THẬT của note nên không bị méo dù prefab scale lệch trục.
    /// Màu vẫn do NoteView tô theo màu lane (sr.color) nên Miss / Hold tự đổi màu như cũ.
    /// </summary>
    public class NoteSkin : MonoBehaviour
    {
        [Header("Kiểu note")]
        public NoteStyle style = NoteStyle.Neon;
        [Tooltip("Kích thước phần thân note (world units: rộng, cao). 0 = giữ nguyên kích thước hiện tại của prefab")]
        public Vector2 sizeOverride = Vector2.zero;

        [Header("Tuỳ chỉnh (chỉ có tác dụng khi Style = Custom)")]
        public NoteShape shape = NoteShape.Rounded;
        [Tooltip("Độ bo góc (chỉ cho Rounded), % chiều cao note")]
        [Range(0f, 0.5f)] public float cornerRadius = 0.3f;
        [Tooltip("Độ dày viền sáng, % chiều cao note")]
        [Range(0f, 0.5f)] public float borderWidth = 0.12f;
        [Tooltip("Vầng sáng bên ngoài, % chiều cao note")]
        [Range(0f, 2f)] public float glowSize = 0.7f;
        [Range(0f, 1f)] public float glowAlpha = 0.55f;
        [Range(0f, 1f)] public float fillBottom = 0.25f;
        [Range(0f, 1f)] public float fillTop = 0.45f;
        [Range(0f, 1f)] public float fillAlpha = 0.9f;
        [Range(0f, 1f)] public float shine = 0f;
        [Range(0f, 1f)] public float innerGlow = 0.5f;
        [Tooltip("Điểm bóng loáng phía trên (kiểu bong bóng)")]
        [Range(0f, 1f)] public float specular = 0f;
        [Tooltip("Vạch sáng trắng chạy dọc giữa note (kiểu tia laser)")]
        [Range(0f, 1f)] public float centerLine = 0f;
        [Range(0f, 1f)] public float scanlines = 0f;
        [Range(0f, 1f)] public float stripes = 0f;
        [Tooltip("0 = mịn. Số khác 0 = vẽ khối pixel, số hàng pixel trên chiều cao note")]
        [Range(0, 12)] public int pixelRows = 0;

        public Sprite HeadSprite { get; private set; }
        public Sprite BodySprite { get; private set; }
        /// <summary>Kích thước phần thân note (không tính vầng sáng) theo đơn vị local của prefab.</summary>
        public Vector2 CoreLocalSize { get; private set; } = Vector2.one;

        SpriteRenderer headSr;
        bool coreMeasured;
        Vector2 coreMeasuredSize;   // kích thước thân note lúc đầu (đo 1 lần, tránh đo lại phần vầng sáng)

        struct Params
        {
            public NoteShape shape;
            public float corner, border, glowSize, glowAlpha, fillBottom, fillTop, fillAlpha;
            public float shine, innerGlow, specular, centerLine, scan, stripe;
            public int pixelRows;

            public string Key()
            {
                return string.Format("{0}|{1:F3}|{2:F3}|{3:F3}|{4:F3}|{5:F3}|{6:F3}|{7:F3}|{8:F3}|{9:F3}|{10:F3}|{11:F3}|{12:F3}|{13:F3}|{14}",
                    (int)shape, corner, border, glowSize, glowAlpha, fillBottom, fillTop, fillAlpha,
                    shine, innerGlow, specular, centerLine, scan, stripe, pixelRows);
            }
        }

        static Params P(NoteShape shape, float corner, float border, float glowSize, float glowAlpha,
                        float fillBottom, float fillTop, float fillAlpha, float shine, float innerGlow,
                        float specular, float centerLine, float scan, float stripe, int pixelRows)
        {
            return new Params
            {
                shape = shape, corner = corner, border = border, glowSize = glowSize, glowAlpha = glowAlpha,
                fillBottom = fillBottom, fillTop = fillTop, fillAlpha = fillAlpha, shine = shine, innerGlow = innerGlow,
                specular = specular, centerLine = centerLine, scan = scan, stripe = stripe, pixelRows = pixelRows
            };
        }

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        // Kiểu note áp cho TOÀN BỘ note, kể cả note được pool tạo ra về sau (NotePool không tạo sẵn note)
        static bool hasGlobalStyle;
        static NoteStyle globalStyle;
        public static void SetGlobalStyle(NoteStyle s) { hasGlobalStyle = true; globalStyle = s; }
        public static void ClearGlobalStyle() { hasGlobalStyle = false; }
        NoteStyle EffectiveStyle { get { return hasGlobalStyle ? globalStyle : style; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            hasGlobalStyle = false;
            cache.Clear();   // texture cũ đã bị huỷ giữa các lần Play nếu tắt Domain Reload
        }

        Params GetParams()
        {
            //                                    hình               bo    viền  glowS glowA fillB fillT fillA shine inner spec  line  scan  stripe pixel
            switch (EffectiveStyle)
            {
                case NoteStyle.Neon:    return P(NoteShape.Rounded, .30f, .12f, .70f, .55f, .32f, .58f, .90f, 0f,   .5f, 0f,   0f,   0f,  0f,  0);
                case NoteStyle.Glass:   return P(NoteShape.Rounded, .35f, .06f, .45f, .40f, .60f, .95f, .80f, .35f, .3f, 0f,   0f,   0f,  0f,  0);
                case NoteStyle.Capsule: return P(NoteShape.Rounded, .50f, .08f, .60f, .50f, .70f, .90f, .95f, .20f, .3f, 0f,   0f,   0f,  0f,  0);
                case NoteStyle.Outline: return P(NoteShape.Rounded, .28f, .14f, .60f, .55f, .55f, .55f, .12f, 0f,   .6f, 0f,   0f,   0f,  0f,  0);
                case NoteStyle.Cyber:   return P(NoteShape.Chamfer, 0f,   .10f, .50f, .50f, .20f, .40f, .90f, 0f,   .5f, 0f,   0f,   .6f, 0f,  0);
                case NoteStyle.Slant:   return P(NoteShape.Slant,   0f,   .10f, .55f, .50f, .35f, .65f, .90f, 0f,   .4f, 0f,   0f,   0f,  0f,  0);
                case NoteStyle.Gem:     return P(NoteShape.Gem,     0f,   .09f, .55f, .50f, .55f, .95f, .90f, .30f, .3f, 0f,   .55f, 0f,  0f,  0);
                case NoteStyle.Arrow:   return P(NoteShape.Arrow,   0f,   .10f, .50f, .50f, .35f, .70f, .90f, 0f,   .4f, 0f,   0f,   0f,  0f,  0);
                case NoteStyle.Petal:   return P(NoteShape.Ellipse, 0f,   .03f, .50f, .45f, .75f, .95f, .95f, .25f, .25f, .35f, 0f,   0f,  0f,  0);
                case NoteStyle.Bubble:  return P(NoteShape.Rounded, .50f, .05f, .35f, .35f, .55f, .75f, .55f, 0f,   .4f, .9f,  0f,   0f,  0f,  0);
                case NoteStyle.Pixel:   return P(NoteShape.Rounded, .20f, .16f, 0f,   0f,   .40f, .65f, 1.0f, 0f,   .2f, 0f,   0f,   0f,  0f,  6);
                case NoteStyle.Laser:   return P(NoteShape.Rounded, .50f, .05f, .90f, .60f, .25f, .25f, .35f, 0f,   .5f, 0f,   1f,   0f,  0f,  0);
                case NoteStyle.Candy:   return P(NoteShape.Rounded, .50f, .08f, .30f, .35f, .75f, .95f, 1.0f, 0f,   .2f, .3f,  0f,   0f,  .9f, 0);
                case NoteStyle.Flat:    return P(NoteShape.Rounded, .30f, .06f, .15f, .25f, .85f, .92f, 1.0f, 0f,   .1f, 0f,   0f,   0f,  0f,  0);
                default:
                    return P(shape, cornerRadius, borderWidth, glowSize, glowAlpha, fillBottom, fillTop, fillAlpha,
                             shine, innerGlow, specular, centerLine, scanlines, stripes, pixelRows);
            }
        }

        /// <summary>Dựng sprite theo kích thước thật của note và gán cho đầu note.</summary>
        public void Apply(SpriteRenderer head)
        {
            Vector3 ls = transform.lossyScale;
            float sx = Mathf.Max(Mathf.Abs(ls.x), 0.0001f);
            float sy = Mathf.Max(Mathf.Abs(ls.y), 0.0001f);

            headSr = head;
            if (!coreMeasured)
            {
                coreMeasuredSize = (Vector2)head.bounds.size;
                coreMeasured = true;
            }
            Vector2 core = (sizeOverride.x > 0f && sizeOverride.y > 0f) ? sizeOverride : coreMeasuredSize;
            if (core.x < 0.01f || core.y < 0.01f) core = new Vector2(1.5f, 0.35f);
            CoreLocalSize = new Vector2(core.x / sx, core.y / sy);

            Params p = GetParams();
            string key = string.Format("head|{0:F3}|{1:F3}|{2:F3}|{3:F3}|{4}", core.x, core.y, sx, sy, p.Key());
            if (!cache.TryGetValue(key, out Sprite head_) || head_ == null)
            {
                head_ = BuildHead(core, sx, sy, p);
                cache[key] = head_;
            }
            HeadSprite = head_;

            if (!cache.TryGetValue("body", out Sprite body_) || body_ == null)
            {
                body_ = BuildBody();
                cache["body"] = body_;
            }
            BodySprite = body_;

            head.sprite = HeadSprite;
        }

        /// <summary>Đổi kiểu note lúc đang chạy (dùng khi đổi cảnh nền) và vẽ lại sprite.</summary>
        public void SetStyle(NoteStyle s)
        {
            style = s;
            if (headSr != null) Apply(headSr);
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

        // ---------- Hình dạng (khoảng cách có dấu, toạ độ WORLD, âm = bên trong) ----------
        static Vector2[] BuildPoly(NoteShape shape, float hx, float hy)
        {
            switch (shape)
            {
                case NoteShape.Chamfer:
                {
                    float k = Mathf.Min(hy * 0.85f, hx * 0.45f);
                    return new[]
                    {
                        new Vector2(-hx + k, hy), new Vector2(hx - k, hy), new Vector2(hx, hy - k), new Vector2(hx, -hy + k),
                        new Vector2(hx - k, -hy), new Vector2(-hx + k, -hy), new Vector2(-hx, -hy + k), new Vector2(-hx, hy - k)
                    };
                }
                case NoteShape.Slant:
                {
                    float s = Mathf.Min(hy, hx * 0.4f);
                    return new[] { new Vector2(-hx + s, hy), new Vector2(hx, hy), new Vector2(hx - s, -hy), new Vector2(-hx, -hy) };
                }
                case NoteShape.Gem:
                {
                    float tip = Mathf.Min(hy * 1.1f, hx * 0.45f);
                    return new[]
                    {
                        new Vector2(-hx, 0f), new Vector2(-hx + tip, hy), new Vector2(hx - tip, hy),
                        new Vector2(hx, 0f), new Vector2(hx - tip, -hy), new Vector2(-hx + tip, -hy)
                    };
                }
                case NoteShape.Arrow:   // mũi tên chỉ xuống vạch bấm
                    return new[] { new Vector2(-hx, hy), new Vector2(hx, hy), new Vector2(hx, 0f), new Vector2(0f, -hy), new Vector2(-hx, 0f) };
                default:
                    return null;
            }
        }

        static float PolySdf(Vector2 pt, Vector2[] v)
        {
            float d = Vector2.Dot(pt - v[0], pt - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = pt - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = pt.y >= v[i].y, c2 = pt.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        static float Sdf(Vector2 pt, NoteShape shape, float hx, float hy, float r, Vector2[] poly)
        {
            switch (shape)
            {
                case NoteShape.Rounded:
                {
                    float qx = Mathf.Abs(pt.x) - (hx - r);
                    float qy = Mathf.Abs(pt.y) - (hy - r);
                    return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                           + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
                }
                case NoteShape.Ellipse:
                {
                    // xấp xỉ khoảng cách tới elip: chính xác hơn nhiều so với nhân theo bán kính nhỏ nhất
                    float k0 = new Vector2(pt.x / hx, pt.y / hy).magnitude;
                    float k1 = new Vector2(pt.x / (hx * hx), pt.y / (hy * hy)).magnitude;
                    return k1 > 1e-9f ? k0 * (k0 - 1f) / k1 : -Mathf.Min(hx, hy);
                }
                default:
                    return PolySdf(new Vector2(pt.x, pt.y + 2e-4f * hy), poly);   // lệch nhẹ để tránh trùng đúng mép ngang
            }
        }

        Sprite BuildHead(Vector2 core, float sx, float sy, Params p)
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
            tex.filterMode = p.pixelRows > 0 ? FilterMode.Point : FilterMode.Bilinear;
            var px = new Color32[W * H];

            float hx = cw * 0.5f, hy = ch * 0.5f;
            float r = Mathf.Clamp(p.corner * ch, 0f, Mathf.Min(hx, hy));
            float bw = p.border * ch;
            float e = Mathf.Max(Wt / W, Ht / H);        // độ rộng mép mượt (1 pixel)
            float glowLen = Mathf.Max(pad, 0.0001f);
            Vector2[] poly = BuildPoly(p.shape, hx, hy);
            float cell = p.pixelRows > 0 ? ch / p.pixelRows : 0f;
            float lineSig = 0.08f * ch;

            for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
            {
                float wx = ((i + 0.5f) / W - 0.5f) * Wt;
                float wy = ((j + 0.5f) / H - 0.5f) * Ht;
                if (cell > 0f)   // kiểu Pixel: làm tròn toạ độ về ô lưới -> mép răng cưa, vầng sáng thành bậc
                {
                    wx = (Mathf.Floor(wx / cell) + 0.5f) * cell;
                    wy = (Mathf.Floor(wy / cell) + 0.5f) * cell;
                }

                float d = Sdf(new Vector2(wx, wy), p.shape, hx, hy, r, poly);   // d < 0: bên trong
                float inside = Mathf.Clamp01(0.5f - d / e);
                float edge = Mathf.Clamp01(0.5f + (d + bw) / e);                // 1 ở dải viền sát mép

                // ruột note: gradient dọc + dải bóng + sáng dần về phía viền
                float t = Mathf.Clamp01(wy / ch + 0.5f);
                float v = Mathf.Lerp(p.fillBottom, p.fillTop, t);
                v += p.shine * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.95f, t)) * 0.5f;
                v += p.innerGlow * 0.35f * Mathf.Exp(Mathf.Min(d, 0f) / (0.35f * ch));

                if (p.scan > 0f)    // vạch ngang kiểu màn hình CRT
                    v *= 1f - p.scan * 0.5f * (0.5f + 0.5f * Mathf.Sin(wy / ch * 18.85f));
                if (p.stripe > 0f)  // sọc chéo kiểu kẹo
                {
                    float st = Mathf.Repeat((wx + wy) / (ch * 0.9f), 1f);
                    float band = Mathf.SmoothStep(0f, 1f, Mathf.Abs(st * 2f - 1f));
                    v *= 1f - p.stripe * 0.35f * (1f - band);
                }

                float fa = p.fillAlpha;
                if (p.centerLine > 0f)   // lõi trắng nóng chạy dọc giữa
                {
                    float cl = Mathf.Exp(-(wy * wy) / (2f * lineSig * lineSig));
                    v = Mathf.Lerp(v, 1f, p.centerLine * cl);
                    fa = Mathf.Max(fa, p.centerLine * cl);
                }
                if (p.specular > 0f)     // điểm bóng loáng phía trên
                {
                    float dy = wy - 0.30f * ch;
                    float sp = Mathf.Exp(-(dy * dy) / (2f * (0.12f * ch) * (0.12f * ch)));
                    float fx = Mathf.Clamp01(1f - Mathf.Pow(Mathf.Abs(wx) / (hx * 0.85f), 4f));
                    float sv = p.specular * sp * fx;
                    v = Mathf.Lerp(v, 1f, sv * 0.85f);
                    fa = Mathf.Max(fa, sv * 0.9f);
                }
                v = Mathf.Lerp(Mathf.Clamp01(v), 1f, edge);

                // vầng sáng bên ngoài
                float glow = 0f;
                if (d > 0f && pad > 0f)
                {
                    glow = p.glowAlpha * Mathf.Exp(-3f * d / glowLen);
                    glow *= Mathf.Clamp01((pad - d) / (0.25f * pad));   // tắt hẳn trước mép texture
                }

                float insideA = Mathf.Lerp(fa, 1f, edge);
                float a = Mathf.Lerp(glow, insideA, inside);
                float c = Mathf.Lerp(1f, v, inside);

                byte c8 = (byte)Mathf.RoundToInt(Mathf.Clamp01(c) * 255f);
                px[j * W + i] = new Color32(c8, c8, c8, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
            }

            tex.SetPixels32(px);
            tex.Apply();
            float ppu = W * sx / Wt;   // để chiều rộng sprite trong thế giới = Wt
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
        }

        // Thân hold note: dải đứng, hai mép sáng, giữa sáng nhẹ. Bị kéo dãn theo chiều dọc nên không có góc bo.
        Sprite BuildBody()
        {
            int W = 64, H = 4;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[W * H];
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W;
                float de = Mathf.Min(u, 1f - u);
                float line = Mathf.Clamp01(1f - de / 0.16f);
                line *= line;
                float center = 1f - Mathf.Abs(2f * u - 1f);
                float v = Mathf.Clamp01(0.45f + 0.30f * center + 0.60f * line);
                float a = Mathf.Clamp01(0.55f + 0.45f * line);
                byte v8 = (byte)Mathf.RoundToInt(v * 255f);
                byte a8 = (byte)Mathf.RoundToInt(a * 255f);
                for (int y = 0; y < H; y++) px[y * W + x] = new Color32(v8, v8, v8, a8);
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
        }
    }
}