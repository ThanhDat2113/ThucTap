using UnityEngine;
using UnityEngine.Events;

namespace RhythmGame
{
    /// <summary>
    /// Quản lý visual của receptor: glow, pulse, press animation, hit feedback.
    /// Tích hợp ReceptorVisuals cho hiệu ứng nâng cao.
    /// </summary>
    public class ReceptorFlasher : MonoBehaviour
    {
        public static ReceptorFlasher Instance { get; private set; }

        [Header("Refs")]
        public SpriteRenderer[] receptors;
        public InputHandler input;
        public JudgementSystem judgement;

        [Header("Visual Effects")]
        public ReceptorVisuals[] visuals;

        [Header("Fallback (nếu không dùng ReceptorVisuals)")]
        [Tooltip("Màu receptor khi đang giữ phím (nhấn).")]
        public Color pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Neon receptor (vạch bấm kiểu neon cùng hình với note)")]
        [Tooltip("Vẽ lại vạch bấm bằng khung neon hình bình hành giống note, thay cho khối trắng")]
        public bool useNeonReceptors = true;
        [Tooltip("Ẩn sprite receptor cũ (khối trắng). Tắt nếu muốn giữ cả hai.")]
        public bool hideOriginalSprite = true;
        [Tooltip("Bật = cùng kiểu hình với note (Slant, Street...). Tắt = luôn dùng viền Outline.")]
        public bool sameStyleAsNotes = true;
        [Range(0.2f, 1f)] public float idleBrightness = 0.65f;
        [Tooltip("Phóng to nhẹ khi đang giữ phím")]
        [Range(1f, 1.3f)] public float pressScale = 1.06f;
        [Tooltip("Tốc độ tắt của tia chớp khi đánh trúng")]
        public float flashDecay = 6f;

        [Header("Lane Colors")]
        public Color[] laneColors =
        {
            new Color(0.76f, 0.29f, 0.60f), // Lane 0 - Magenta
            new Color(0.00f, 1.00f, 1.00f), // Lane 1 - Cyan
            new Color(0.07f, 0.98f, 0.02f), // Lane 2 - Green
            new Color(0.98f, 0.22f, 0.25f), // Lane 3 - Red
        };

        Color[] idleColors;
        bool[] wasHeld;

        // lớp neon phủ lên từng receptor
        SpriteRenderer[] pads, flashPads;
        NoteSkin[] padSkins;
        float[] flash, pressAmt;
        Color[] padTint;

        void Awake()
        {
            Instance = this;
            if (visuals == null || visuals.Length == 0)
            {
                visuals = new ReceptorVisuals[receptors != null ? receptors.Length : 0];
                for (int i = 0; i < visuals.Length; i++)
                {
                    if (receptors[i] != null)
                        visuals[i] = receptors[i].GetComponent<ReceptorVisuals>();
                }
            }
        }

        void Start()
        {
            idleColors = new Color[receptors != null ? receptors.Length : 0];
            wasHeld = new bool[idleColors.Length];
            for (int i = 0; i < idleColors.Length; i++)
            {
                idleColors[i] = receptors[i] != null ? receptors[i].color : Color.white;

                if (visuals[i] != null)
                {
                    visuals[i].Initialize(i, idleColors[i]);
                }
            }

            if (judgement != null)
            {
                judgement.OnJudged += OnJudged;
            }

            if (useNeonReceptors) BuildNeon();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (judgement != null)
            {
                judgement.OnJudged -= OnJudged;
            }
        }

        int seenStyleVersion;

        void Update()
        {
            // kiểu note đổi (NoteThemeApplier đổi cảnh) -> vẽ lại vạch bấm cho cùng kiểu
            if (pads != null && seenStyleVersion != NoteSkin.StyleVersion)
            {
                seenStyleVersion = NoteSkin.StyleVersion;
                for (int i = 0; i < pads.Length; i++) RefreshPad(i);
            }

            if (input == null) return;

            for (int l = 0; l < idleColors.Length; l++)
            {
                if (receptors[l] == null) continue;

                bool held = input.IsHeld(l);

                if (visuals[l] != null)
                {
                    visuals[l].SetPressed(held);
                }
                else
                {
                    receptors[l].color = held ? pressedColor : idleColors[l];
                }

                if (held != wasHeld[l])
                {
                    wasHeld[l] = held;
                }

                AnimatePad(l, held);
            }
        }

        void OnJudged(Judgement j, int points, bool isHit)
        {
            int lane = GetLaneOfLastJudgement();
            if (lane >= 0 && lane < visuals.Length && visuals[lane] != null)
            {
                visuals[lane].PlayHitFeedback(j);
            }
            if (isHit) FlashLane(lane);
        }

        int GetLaneOfLastJudgement()
        {
            if (judgement == null || input == null) return -1;

            for (int l = 0; l < idleColors.Length; l++)
            {
                if (input.IsHeld(l)) return l;
            }
            return -1;
        }

        public void SetLaneColor(int lane, Color color)
        {
            if (laneColors != null && lane >= 0 && lane < laneColors.Length) laneColors[lane] = color;
            RefreshPad(lane);

            if (lane >= 0 && lane < visuals.Length && visuals[lane] != null)
            {
                visuals[lane].SetLaneColor(color);
            }
            else if (lane >= 0 && lane < receptors.Length && receptors[lane] != null)
            {
                receptors[lane].color = color;
                idleColors[lane] = color;
            }
        }

        // =====================================================================
        //  Neon receptor
        // =====================================================================

        /// <summary>Chớp sáng vạch bấm của một lane (NoteView tự gọi khi trúng note).</summary>
        public void FlashLane(int lane)
        {
            if (flash != null && lane >= 0 && lane < flash.Length) flash[lane] = 1f;
        }

        void BuildNeon()
        {
            int n = receptors != null ? receptors.Length : 0;
            pads = new SpriteRenderer[n];
            flashPads = new SpriteRenderer[n];
            padSkins = new NoteSkin[n];
            flash = new float[n];
            pressAmt = new float[n];
            padTint = new Color[n];

            for (int i = 0; i < n; i++)
            {
                var host = receptors[i];
                if (host == null) continue;
                Vector2 size = host.bounds.size;   // kích thước thật (world) của receptor cũ

                pads[i] = NewPad(host, "NeonPad", size, 1, false, out padSkins[i]);
                flashPads[i] = NewPad(host, "NeonFlash", size, 2, true, out NoteSkin _unused);
                flashPads[i].color = new Color(1f, 1f, 1f, 0f);

                RefreshPad(i);
                if (hideOriginalSprite) host.enabled = false;
            }
        }

        SpriteRenderer NewPad(SpriteRenderer host, string objName, Vector2 size, int orderOffset, bool filled, out NoteSkin skin)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(host.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerID = host.sortingLayerID;
            sr.sortingOrder = host.sortingOrder + orderOffset;

            skin = go.AddComponent<NoteSkin>();
            skin.sizeOverride = size;
            skin.style = NoteStyle.Outline;
            skin.ignoreGlobalStyle = !sameStyleAsNotes;   // false = theo kiểu note do NoteThemeApplier đặt
            skin.hollowFill = !filled;                    // khung rỗng; lớp chớp thì đặc và trắng
            skin.useBackgroundPalette = !filled;
            skin.fillLift = 1f;
            skin.Apply(sr);
            return sr;
        }

        void RefreshPad(int i)
        {
            if (pads == null || i < 0 || i >= pads.Length || pads[i] == null) return;
            Color lane = (laneColors != null && laneColors.Length > 0) ? laneColors[i % laneColors.Length] : Color.white;
            if (padSkins[i].TryApplyPalette(pads[i], lane, i)) padTint[i] = Color.white;   // sprite đã mang màu sẵn
            else padTint[i] = lane;                                                         // không có palette: nhân màu lane
        }

        void AnimatePad(int l, bool held)
        {
            if (pads == null || l >= pads.Length || pads[l] == null) return;
            float dt = Time.deltaTime;
            pressAmt[l] = Mathf.MoveTowards(pressAmt[l], held ? 1f : 0f, dt * 12f);
            flash[l] = Mathf.Max(0f, flash[l] - dt * flashDecay);

            float beat = AnimatedBackground.Instance != null ? AnimatedBackground.Instance.PulseValue : 0f;
            float v = Mathf.Clamp01(idleBrightness + 0.30f * pressAmt[l] + 0.15f * beat + 0.40f * flash[l]);
            Color t = padTint[l];
            pads[l].color = new Color(t.r * v, t.g * v, t.b * v, 1f);

            float sc = 1f + (pressScale - 1f) * pressAmt[l] + 0.06f * flash[l];
            var scale = new Vector3(sc, sc, 1f);
            pads[l].transform.localScale = scale;

            if (flashPads[l] != null)
            {
                flashPads[l].color = new Color(1f, 1f, 1f, 0.75f * flash[l]);
                flashPads[l].transform.localScale = scale;
            }
        }
    }
}