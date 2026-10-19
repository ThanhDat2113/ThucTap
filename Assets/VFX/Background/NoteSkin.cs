using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    // Giữ nguyên thứ tự 4 giá trị đầu để prefab cũ không bị đổi style. Skin mới nằm phía sau Custom.
    public enum NoteStyle { Neon, Glass, Capsule, Custom, Outline, Crystal, Flat, Soft }

    /// <summary>
    /// Tự vẽ sprite note đẹp bằng code (viền neon, vầng sáng, gradient) thay cho khối trắng của prefab.
    /// Hình được vẽ theo kích thước THẬT của note nên góc bo không bị méo dù prefab scale lệch trục.
    /// Màu vẫn do NoteView tô theo màu lane (sr.color), nên Miss / Hold vẫn tự đổi màu như cũ.
    ///
    /// CÁCH DÙNG: không cần làm gì thêm, NoteView tự thêm component này vào note.
    /// Muốn đổi skin: chọn prefab note, mở component NoteSkin trong Inspector, đổi Style.
    ///
    /// Các skin dựng sẵn:
    ///   Neon    - viền sáng + vầng sáng mạnh, ruột tối (hợp nhạc điện tử)
    ///   Glass   - kính mờ, có vệt bóng phía trên
    ///   Capsule - bo tròn hẳn hai đầu, mềm mại
    ///   Outline - chỉ có viền sáng, ruột gần như trong suốt (tối giản, dễ nhìn nền)
    ///   Crystal - ruột sáng, vệt bóng mạnh, góc vuông hơn (cảm giác pha lê)
    ///   Flat    - màu phẳng, không glow (dành cho máy yếu / phong cách tối giản)
    ///   Soft    - vầng sáng rất lớn, bo tròn (phong cách mơ màng)
    ///   Custom  - tự kéo các thanh trượt bên dưới
    /// </summary>
    public class NoteSkin : MonoBehaviour
    {
        [Header("Kiểu note")]
        public NoteStyle style = NoteStyle.Neon;
        [Tooltip("Kích thước phần thân note (world units: rộng, cao). 0 = giữ nguyên kích thước hiện tại của prefab")]
        public Vector2 sizeOverride = Vector2.zero;

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
        }

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        static Params P(float corner, float border, float glowSize, float glowAlpha,
                        float fillBottom, float fillTop, float fillAlpha, float shine, float innerGlow)
        {
            return new Params
            {
                corner = corner, border = border, glowSize = glowSize, glowAlpha = glowAlpha,
                fillBottom = fillBottom, fillTop = fillTop, fillAlpha = fillAlpha, shine = shine, innerGlow = innerGlow
            };
        }

        Params GetParams()
        {
            switch (style)
            {
                //                      corner border glowSz glowA  fillB  fillT  fillA  shine  inner
                case NoteStyle.Neon:    return P(0.30f, 0.12f, 0.70f, 0.55f, 0.25f, 0.45f, 0.90f, 0.00f, 0.50f);
                case NoteStyle.Glass:   return P(0.35f, 0.06f, 0.45f, 0.40f, 0.60f, 0.95f, 0.80f, 0.35f, 0.30f);
                case NoteStyle.Capsule: return P(0.50f, 0.08f, 0.60f, 0.50f, 0.70f, 0.90f, 0.95f, 0.20f, 0.30f);
                case NoteStyle.Outline: return P(0.30f, 0.10f, 0.60f, 0.50f, 0.10f, 0.25f, 0.18f, 0.00f, 0.60f);
                case NoteStyle.Crystal: return P(0.20f, 0.07f, 0.50f, 0.45f, 0.55f, 1.00f, 0.85f, 0.60f, 0.40f);
                case NoteStyle.Flat:    return P(0.15f, 0.00f, 0.00f, 0.00f, 0.85f, 0.95f, 1.00f, 0.00f, 0.00f);
                case NoteStyle.Soft:    return P(0.50f, 0.04f, 1.20f, 0.60f, 0.50f, 0.80f, 0.90f, 0.10f, 0.50f);
                default:                return P(cornerRadius, borderWidth, glowSize, glowAlpha,
                                                 fillBottom, fillTop, fillAlpha, shine, innerGlow);
            }
        }

        /// <summary>Dựng sprite theo kích thước thật của note và gán cho đầu note.</summary>
        public void Apply(SpriteRenderer head)
        {
            Vector3 ls = transform.lossyScale;
            float sx = Mathf.Max(Mathf.Abs(ls.x), 0.0001f);
            float sy = Mathf.Max(Mathf.Abs(ls.y), 0.0001f);

            Vector2 core = (sizeOverride.x > 0f && sizeOverride.y > 0f) ? sizeOverride : (Vector2)head.bounds.size;
            if (core.x < 0.01f || core.y < 0.01f) core = new Vector2(1.5f, 0.35f);
            CoreLocalSize = new Vector2(core.x / sx, core.y / sy);

            Params p = GetParams();
            string key = string.Format("head|{0:F3}|{1:F3}|{2:F3}|{3:F3}|{4:F3}|{5:F3}|{6:F3}|{7:F3}|{8:F3}|{9:F3}|{10:F3}|{11:F3}|{12:F3}",
                core.x, core.y, sx, sy, p.corner, p.border, p.glowSize, p.glowAlpha, p.fillBottom, p.fillTop, p.fillAlpha, p.shine, p.innerGlow);
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

        // Vẽ hình chữ nhật bo góc bằng khoảng cách có dấu (SDF) trong toạ độ WORLD:
        // mỗi pixel biết nó nằm ở đâu trong thế giới thật nên góc bo luôn tròn, không bị méo theo scale.
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
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[W * H];

            float hx = cw * 0.5f, hy = ch * 0.5f;
            float r = Mathf.Clamp(p.corner * ch, 0f, Mathf.Min(hx, hy));
            float bw = p.border * ch;
            float e = Mathf.Max(Wt / W, Ht / H);        // độ rộng mép mượt (1 pixel)
            float glowLen = Mathf.Max(pad, 0.0001f);

            for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
            {
                float wx = ((i + 0.5f) / W - 0.5f) * Wt;
                float wy = ((j + 0.5f) / H - 0.5f) * Ht;

                float qx = Mathf.Abs(wx) - (hx - r);
                float qy = Mathf.Abs(wy) - (hy - r);
                float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                          + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;   // d < 0: bên trong, d > 0: bên ngoài

                float inside = Mathf.Clamp01(0.5f - d / e);
                float edge = bw > 0f ? Mathf.Clamp01(0.5f + (d + bw) / e) : 0f;   // 1 ở dải viền sát mép

                // ruột note: gradient dọc + dải bóng + sáng dần về phía viền
                float t = Mathf.Clamp01(wy / ch + 0.5f);
                float v = Mathf.Lerp(p.fillBottom, p.fillTop, t);
                v += p.shine * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.95f, t)) * 0.5f;
                v += p.innerGlow * 0.35f * Mathf.Exp(Mathf.Min(d, 0f) / (0.35f * ch));
                v = Mathf.Lerp(Mathf.Clamp01(v), 1f, edge);

                // vầng sáng bên ngoài
                float glow = 0f;
                if (d > 0f && pad > 0f)
                {
                    glow = p.glowAlpha * Mathf.Exp(-3f * d / glowLen);
                    glow *= Mathf.Clamp01((pad - d) / (0.25f * pad));   // tắt hẳn trước mép texture
                }

                float insideA = Mathf.Lerp(p.fillAlpha, 1f, edge);
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