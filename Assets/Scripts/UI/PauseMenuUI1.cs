using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RhythmGame
{
    /// <summary>
    /// Giao diện menu tạm dừng phong cách neon graffiti, hợp với gameplay và nền sân khấu:
    ///  - Bảng kính tối hình bình hành nghiêng (cùng ngôn ngữ hình với note), viền neon hồng -> cyan, vầng sáng, vạch quét
    ///  - Tiêu đề glitch tách màu RGB, equalizer nhỏ nhảy ở đáy bảng
    ///  - Nút đang chọn sáng lên, phóng nhẹ, nhấp nháy. Chọn bằng chuột / cảm ứng / mũi tên / W S, xác nhận bằng Enter
    ///  - Thẻ Score / Combo / Max Combo + thanh tiến độ (chỉ hiện khi có dữ liệu, xem SetStats)
    ///  - Số đếm ngược to có hiệu ứng nảy
    ///  - Số nút tuỳ ý (2 đến 5), chữ dùng UI.Text thường nên dùng được đúng font của game
    ///
    /// Lớp này CHỈ vẽ giao diện. Việc tạm dừng, đếm giờ, restart, về sảnh... do PauseManager làm
    /// (PauseManager tự tạo và điều khiển component này khi bật "Use Neon Menu").
    /// Chạy được khi Time.timeScale = 0.
    /// </summary>
    public class PauseMenuUI : MonoBehaviour
    {
        public struct Item
        {
            public string label;
            public Sprite sprite;          // để trống = nút neon tự vẽ
            public System.Action action;
            public bool danger;            // true = nút màu đỏ cam (Quit / Main Menu)
        }

        public struct Stats
        {
            public string song;
            public int score, combo, maxCombo;
            public float progress;         // 0..1
        }

        [Header("Giao diện")]
        [Tooltip("Font chữ. Để trống = font hệ thống. PauseManager tự truyền font của nó vào đây.")]
        public Font font;
        public string title = "PAUSE GAME";
        [Tooltip("Hiện dòng hướng dẫn phím ở đáy bảng (UP / DOWN, ENTER, ESC)")]
        public bool showHint = false;
        [Tooltip("Phải nhỏ hơn Sorting Order của nút pause ở góc (100) để nút góc vẫn bấm được khi đang pause")]
        public int canvasSortingOrder = 99;
        [Tooltip("Lấy màu theo nền AnimatedBackground nếu có")]
        public bool useBackgroundColors = true;
        public Color colorA = new Color(1.00f, 0.25f, 0.80f);
        public Color colorB = new Color(0.20f, 0.90f, 1.00f);

        [Header("Hiệu ứng")]
        [Range(0.1f, 1f)] public float introTime = 0.35f;
        [Range(0f, 1f)] public float backdropDarkness = 0.72f;

        enum State { Idle, Countdown }

        // ---------- dữ liệu ----------
        readonly List<Item> items = new List<Item>();
        Stats stats;
        bool hasStats;

        // ---------- trạng thái ----------
        bool built, shown;
        string builtKey = "";
        State state = State.Idle;
        float intro, time, glitchTimer, bigGlitch, cdStart;
        int selected;
        float panelH, panelFit = 1f;
        Vector2 glitchOffset, titleBase;
        Font uiFont;

        // ---------- đối tượng UI ----------
        GameObject root;
        Image backdrop;
        RectTransform panel;
        CanvasGroup panelGroup;
        Text titleMain, titleCyan, titlePink, songText, scoreVal, comboVal, maxVal, progressText, countdownText;
        RectTransform underline, progressFill;
        RectTransform[] bars;
        const int BarCount = 24;
        RectTransform[] btnRect;
        Image[] btnImg;
        Text[] btnLabel;
        Sprite[] spNormal, spSelected;
        bool[] btnIsSprite;
        float[] btnScale;
        float[] btnBaseY;
        const float ProgressW = 640f;

        // =====================================================================
        //  API
        // =====================================================================

        public void SetItems(List<Item> newItems)
        {
            items.Clear();
            if (newItems != null) items.AddRange(newItems);
        }

        public void SetStats(string song, int score, int combo, int maxCombo, float progress01)
        {
            stats = new Stats { song = song ?? "", score = score, combo = combo, maxCombo = maxCombo, progress = Mathf.Clamp01(progress01) };
            hasStats = true;
            if (built && shown) ApplyStats();
        }

        public void SetStats(Stats s) { SetStats(s.song, s.score, s.combo, s.maxCombo, s.progress); }

        /// <summary>Không có dữ liệu thì menu ẩn thẻ thông tin và thanh tiến độ, gọn lại.</summary>
        public void ClearStats() { hasStats = false; }

        public void Show()
        {
            EnsureBuilt();
            root.SetActive(true);
            panel.gameObject.SetActive(true);
            countdownText.gameObject.SetActive(false);
            shown = true;
            state = State.Idle;
            intro = 0f;
            selected = 0;
            ApplySelection();
            ApplyStats();
        }

        public void Hide()
        {
            shown = false;
            state = State.Idle;
            if (root != null) root.SetActive(false);
        }

        /// <summary>Hiện số đếm ngược to ở giữa màn hình (bảng menu ẩn đi). Gọi lại mỗi giây với số mới. Hide() để kết thúc.</summary>
        public void ShowCountdown(int n)
        {
            EnsureBuilt();
            if (state != State.Countdown)
            {
                state = State.Countdown;
                shown = true;
                root.SetActive(true);
                panel.gameObject.SetActive(false);
                backdrop.color = new Color(0.02f, 0f, 0.06f, backdropDarkness * 0.5f);
            }
            countdownText.gameObject.SetActive(true);
            countdownText.text = n.ToString();
            cdStart = time;
        }

        [ContextMenu("Show Preview")]
        public void ShowPreview()
        {
            if (items.Count == 0)
            {
                items.Add(new Item { label = "RESUME" });
                items.Add(new Item { label = "RESTART" });
                items.Add(new Item { label = "LOBBY" });
                items.Add(new Item { label = "MAIN MENU", danger = true });
            }
            SetStats("Song Title", 123456, 87, 152, 0.62f);
            Show();
        }

        public void Select(int index)
        {
            if (!shown || state != State.Idle || items.Count == 0) return;
            index = Mathf.Clamp(index, 0, items.Count - 1);
            if (index == selected) return;
            selected = index;
            ApplySelection();
        }

        public void Activate(int index)
        {
            if (!shown || state != State.Idle) return;
            if (index < 0 || index >= items.Count) return;
            var act = items[index].action;
            if (act != null) act();
        }

        // =====================================================================
        //  Vòng lặp
        // =====================================================================

        void Update()
        {
            if (!shown) return;
            float dt = Time.unscaledDeltaTime;
            time += dt;

            if (state == State.Countdown)
            {
                UpdateCountdown();
                return;
            }

            intro = Mathf.MoveTowards(intro, 1f, dt / Mathf.Max(0.05f, introTime));
            float e = EaseOutBack(intro);
            panel.localScale = Vector3.one * (Mathf.LerpUnclamped(0.86f, 1f, e) * panelFit);
            panel.anchoredPosition = new Vector2(0f, Mathf.Lerp(-40f, 0f, intro));
            panelGroup.alpha = Mathf.Clamp01(intro * 2f);
            backdrop.color = new Color(0.02f, 0f, 0.06f, backdropDarkness * Mathf.Clamp01(intro * 1.5f));

            UpdateInput();
            UpdateTitleGlitch(dt);
            UpdateButtons(dt);
            UpdateBars();

            underline.sizeDelta = new Vector2(360f + 40f * Mathf.Sin(time * 2.2f), 4f);
        }

        void UpdateInput()
        {
            int n = items.Count;
            if (n == 0) return;
            if (PressedUp()) { selected = (selected + n - 1) % n; ApplySelection(); }
            else if (PressedDown()) { selected = (selected + 1) % n; ApplySelection(); }
            if (PressedConfirm()) Activate(selected);
        }

        void UpdateTitleGlitch(float dt)
        {
            glitchTimer -= dt;
            if (glitchTimer <= 0f)
            {
                glitchTimer = Random.Range(0.05f, 0.12f);
                glitchOffset = new Vector2(Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f));
                if (Random.value < 0.06f) bigGlitch = 0.15f;
            }
            bigGlitch -= dt;
            float amp = bigGlitch > 0f ? 14f : 3.5f;
            titleCyan.rectTransform.anchoredPosition = titleBase + new Vector2(-amp, 0f) + glitchOffset * amp * 0.5f;
            titlePink.rectTransform.anchoredPosition = titleBase + new Vector2(amp, 0f) - glitchOffset * amp * 0.5f;
            titleMain.rectTransform.anchoredPosition = titleBase + (bigGlitch > 0f ? glitchOffset * 5f : Vector2.zero);
        }

        void UpdateButtons(float dt)
        {
            for (int i = 0; i < btnRect.Length; i++)
            {
                bool sel = i == selected;
                btnScale[i] = Mathf.Lerp(btnScale[i], sel ? 1.06f : 1f, 1f - Mathf.Exp(-14f * dt));
                btnRect[i].localScale = new Vector3(btnScale[i], btnScale[i], 1f);

                float pulse = sel ? 0.88f + 0.12f * Mathf.Sin(time * 7f) : 1f;
                float baseTone = (btnIsSprite[i] && !sel) ? 0.55f : 1f;   // nút dùng ảnh có sẵn: mờ đi khi không được chọn
                float v = pulse * baseTone;
                btnImg[i].color = new Color(v, v, v, 1f);
            }
        }

        void UpdateBars()
        {
            for (int i = 0; i < BarCount; i++)
            {
                float n = Mathf.PerlinNoise(i * 0.35f, time * 1.4f);
                bars[i].sizeDelta = new Vector2(12f, 6f + 44f * n * n);
            }
        }

        void UpdateCountdown()
        {
            float t = Mathf.Clamp01(time - cdStart);               // 0 -> 1 trong mỗi giây
            float punch = 1f + 0.7f * Mathf.Pow(1f - t, 3f);       // to lúc mới hiện rồi thu lại
            countdownText.rectTransform.localScale = Vector3.one * punch;
            Color c = Color.Lerp(colorA, colorB, ((int)(cdStart * 10f)) % 2 == 0 ? 0.2f : 0.8f);
            c.a = Mathf.Clamp01(1.2f - t * 0.6f);
            countdownText.color = c;
        }

        // =====================================================================
        //  Hiển thị
        // =====================================================================

        void ApplyStats()
        {
            if (!hasStats || songText == null) return;
            songText.text = stats.song;
            scoreVal.text = stats.score.ToString("N0");
            comboVal.text = stats.combo.ToString("N0");
            maxVal.text = stats.maxCombo.ToString("N0");
            progressFill.sizeDelta = new Vector2(ProgressW * stats.progress, progressFill.sizeDelta.y);
            progressText.text = "PROGRESS  " + Mathf.RoundToInt(stats.progress * 100f) + "%";
        }

        void ApplySelection()
        {
            if (btnImg == null) return;
            for (int i = 0; i < btnImg.Length; i++)
            {
                bool sel = i == selected;
                btnImg[i].sprite = sel ? spSelected[i] : spNormal[i];
                btnLabel[i].color = sel ? Color.white : new Color(1f, 1f, 1f, 0.65f);
                btnRect[i].anchoredPosition = new Vector2(sel ? 14f : 0f, btnBaseY[i]);
            }
        }

        // =====================================================================
        //  Dựng UI
        // =====================================================================

        string ComputeKey()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(hasStats).Append('|').Append(showHint).Append('|').Append(title).Append('|');
            foreach (var it in items)
                sb.Append(it.label).Append(',').Append(it.danger).Append(',').Append(it.sprite != null ? it.sprite.name : "-").Append(';');
            return sb.ToString();
        }

        void EnsureBuilt()
        {
            string key = ComputeKey();
            if (built && key != builtKey)
            {
                if (root != null) Destroy(root);
                built = false;
            }
            if (!built) Build(key);
        }

        void Build(string key)
        {
            built = true;
            builtKey = key;

            uiFont = font != null ? font : Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Roboto", "Helvetica" }, 32);
            PickColors();

            // Canvas riêng
            var cgo = new GameObject("PauseMenuCanvas", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = canvasSortingOrder;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            root = cgo;

            // nền tối (chặn bấm xuyên xuống gameplay) + vignette
            backdrop = NewImage("Backdrop", root.transform, null, new Color(0.02f, 0f, 0.06f, 0f));
            Stretch(backdrop.rectTransform);
            backdrop.raycastTarget = true;
            var vig = NewImage("Vignette", root.transform, MakeVignette(), new Color(0f, 0f, 0f, 0.8f));
            Stretch(vig.rectTransform);

            // ----- bố cục: các toạ độ "fromTop" tính từ mép trên bảng đi xuống (số âm) -----
            const float PW = 800f;
            const float spacing = 100f;
            int n = Mathf.Max(1, items.Count);
            float yTitle = -95f, ySong = -170f, yLine = -205f;
            float yCards = -285f, yProgText = -352f, yProg = -380f;
            float firstBtn = hasStats ? -455f : -285f;
            float lastBtn = firstBtn - (n - 1) * spacing;
            float barsBase = lastBtn - 42f - 62f;
            float hintY = barsBase - 28f;
            panelH = -(showHint ? hintY - 34f : barsBase - 40f);
            panelFit = Mathf.Min(1f, 1000f / (panelH + 80f));
            float H = panelH;

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(root.transform, false);
            panel = panelGo.GetComponent<RectTransform>();
            panel.sizeDelta = new Vector2(PW, H);
            panelGroup = panelGo.AddComponent<CanvasGroup>();

            Color panelTop = new Color(0.10f, 0.04f, 0.22f, 0.93f);
            Color panelBot = new Color(0.03f, 0.01f, 0.09f, 0.95f);
            var bg = NewImage("PanelBg", panel, MakeShape(PW, H, 44f, 20f, 3f, 40f, 0.6f, panelTop, panelBot, colorA, colorB, 0.10f), Color.white);
            bg.rectTransform.sizeDelta = new Vector2(PW + 80f, H + 80f);
            bg.raycastTarget = true;

            // tiêu đề 3 lớp để tách màu RGB
            titleBase = new Vector2(0f, Y(yTitle));
            titleCyan = NewText("TitleCyan", panel, title, 92, new Color(colorB.r, colorB.g, colorB.b, 0.9f), titleBase, new Vector2(760f, 140f));
            titlePink = NewText("TitlePink", panel, title, 92, new Color(colorA.r, colorA.g, colorA.b, 0.9f), titleBase, new Vector2(760f, 140f));
            titleMain = NewText("Title", panel, title, 92, Color.white, titleBase, new Vector2(760f, 140f));

            var ul = NewImage("Underline", panel, null, colorA);
            underline = ul.rectTransform;
            underline.anchoredPosition = new Vector2(0f, Y(yLine));
            underline.sizeDelta = new Vector2(360f, 4f);

            if (hasStats)
            {
                songText = NewText("Song", panel, "", 32, colorB, new Vector2(0f, Y(ySong)), new Vector2(720f, 50f));
                scoreVal = NewStatCard("SCORE", -240f, Y(yCards));
                comboVal = NewStatCard("COMBO", 0f, Y(yCards));
                maxVal = NewStatCard("MAX COMBO", 240f, Y(yCards));

                var pbg = NewImage("ProgressBg", panel, null, new Color(1f, 1f, 1f, 0.10f));
                pbg.rectTransform.anchoredPosition = new Vector2(0f, Y(yProg));
                pbg.rectTransform.sizeDelta = new Vector2(ProgressW, 10f);
                var pf = NewImage("ProgressFill", pbg.transform, null, colorB);
                progressFill = pf.rectTransform;
                progressFill.anchorMin = progressFill.anchorMax = new Vector2(0f, 0.5f);
                progressFill.pivot = new Vector2(0f, 0.5f);
                progressFill.anchoredPosition = Vector2.zero;
                progressFill.sizeDelta = new Vector2(0f, 10f);
                progressText = NewText("ProgressText", panel, "PROGRESS", 22, new Color(1f, 1f, 1f, 0.65f), new Vector2(0f, Y(yProgText)), new Vector2(640f, 30f));
            }

            // nút
            btnRect = new RectTransform[n];
            btnImg = new Image[n];
            btnLabel = new Text[n];
            spNormal = new Sprite[n];
            spSelected = new Sprite[n];
            btnIsSprite = new bool[n];
            btnScale = new float[n];
            btnBaseY = new float[n];

            Color danger1 = new Color(1.00f, 0.30f, 0.35f);
            Color danger2 = new Color(1.00f, 0.60f, 0.20f);
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                btnScale[i] = 1f;
                btnBaseY[i] = Y(firstBtn - i * spacing);

                Vector2 size;
                if (it.sprite != null)
                {
                    // dùng ảnh nút có sẵn của game
                    btnIsSprite[i] = true;
                    spNormal[i] = spSelected[i] = it.sprite;
                    float h = 100f;
                    float w = Mathf.Min(640f, h * it.sprite.rect.width / Mathf.Max(1f, it.sprite.rect.height));
                    size = new Vector2(w, h);
                }
                else
                {
                    Color eL = it.danger ? danger1 : colorA;
                    Color eR = it.danger ? danger2 : colorB;
                    Color fillN = new Color(0.07f, 0.03f, 0.16f, 0.90f);
                    Color fillS = Color.Lerp(fillN, eL, 0.30f);
                    fillS.a = 0.95f;
                    spNormal[i] = MakeShape(560f, 84f, 30f, 14f, 2f, 22f, 0.12f, fillN, fillN * 0.6f, eL * 0.55f, eR * 0.55f, 0.06f);
                    spSelected[i] = MakeShape(560f, 84f, 30f, 14f, 3f, 30f, 0.75f, fillS, fillN, eL, eR, 0.08f);
                    size = new Vector2(560f + 44f, 84f + 44f);
                }

                btnImg[i] = NewImage("Button" + i, panel, spNormal[i], Color.white);
                btnRect[i] = btnImg[i].rectTransform;
                btnRect[i].sizeDelta = size;
                btnRect[i].anchoredPosition = new Vector2(0f, btnBaseY[i]);
                btnImg[i].raycastTarget = true;

                btnLabel[i] = NewText("Label", btnRect[i], it.label, 44, Color.white, Vector2.zero, new Vector2(480f, 72f));
                btnLabel[i].resizeTextForBestFit = true;
                btnLabel[i].resizeTextMinSize = 16;
                btnLabel[i].resizeTextMaxSize = 44;
                btnLabel[i].horizontalOverflow = HorizontalWrapMode.Wrap;
                btnLabel[i].verticalOverflow = VerticalWrapMode.Truncate;

                int idx = i;
                AddTrigger(btnImg[i].gameObject, EventTriggerType.PointerEnter, _ => Select(idx));
                AddTrigger(btnImg[i].gameObject, EventTriggerType.PointerClick, _ => Activate(idx));
            }

            // equalizer nhỏ ở đáy bảng
            bars = new RectTransform[BarCount];
            float totalW = BarCount * 22f;
            for (int i = 0; i < BarCount; i++)
            {
                var b = NewImage("Bar" + i, panel, null, Color.Lerp(colorA, colorB, i / (BarCount - 1f)));
                var rt = b.rectTransform;
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(-totalW * 0.5f + 11f + i * 22f, Y(barsBase));
                rt.sizeDelta = new Vector2(12f, 8f);
                bars[i] = rt;
            }

            if (showHint)
                NewText("Hint", panel, "UP / DOWN  select      ENTER  confirm      ESC  resume", 20,
                        new Color(1f, 1f, 1f, 0.5f), new Vector2(0f, Y(hintY)), new Vector2(760f, 28f));

            // số đếm ngược (nằm ngoài bảng để bảng ẩn đi vẫn thấy)
            countdownText = NewText("Countdown", root.transform, "3", 300, Color.white, Vector2.zero, new Vector2(600f, 400f));

            root.SetActive(false);
        }

        // đổi toạ độ "tính từ mép trên bảng" sang toạ độ trong panel (gốc ở giữa)
        float Y(float fromTop) { return panelH * 0.5f + fromTop; }

        void PickColors()
        {
            if (!useBackgroundColors || AnimatedBackground.Instance == null) return;
            colorA = Vivid(AnimatedBackground.Instance.paletteMiddle);
            colorB = Vivid(AnimatedBackground.Instance.paletteBottom);
        }

        static Color Vivid(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            Color o = Color.HSVToRGB(h, Mathf.Max(s, 0.6f), Mathf.Max(v, 0.95f));
            o.a = 1f;
            return o;
        }

        Text NewStatCard(string label, float x, float y)
        {
            Color fill = new Color(0.07f, 0.03f, 0.16f, 0.9f);
            var card = NewImage("Card" + label, panel,
                MakeShape(220f, 96f, 22f, 12f, 2f, 14f, 0.35f, fill, fill * 0.6f, colorA * 0.8f, colorB * 0.8f, 0.06f), Color.white);
            card.rectTransform.sizeDelta = new Vector2(220f + 28f, 96f + 28f);
            card.rectTransform.anchoredPosition = new Vector2(x, y);

            NewText("Label", card.rectTransform, label, 20, new Color(1f, 1f, 1f, 0.65f), new Vector2(0f, 24f), new Vector2(200f, 28f));
            return NewText("Value", card.rectTransform, "0", 38, Color.white, new Vector2(0f, -12f), new Vector2(200f, 50f));
        }

        // ---------- helper tạo UI ----------

        Image NewImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        Text NewText(string name, Transform parent, string content, int size, Color color, Vector2 pos, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = uiFont;
            t.text = content;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            return t;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void AddTrigger(GameObject go, EventTriggerType type, UnityAction<BaseEventData> callback)
        {
            var trig = go.GetComponent<EventTrigger>();
            if (trig == null) trig = go.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(callback);
            trig.triggers.Add(entry);
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        // ---------- vẽ sprite ----------

        // Hình bình hành nghiêng bo góc, viền neon chuyển màu trái -> phải, vầng sáng ngoài, vạch quét mờ bên trong.
        // w,h = kích thước phần thân (đơn vị UI); glow = bề dày vầng sáng thêm ra mỗi phía. Texture vẽ ở độ phân giải gấp đôi.
        static Sprite MakeShape(float w, float h, float slant, float radius, float border, float glow, float glowAlpha,
                                Color fillTop, Color fillBottom, Color edgeL, Color edgeR, float scan)
        {
            const int S = 2;
            int W = Mathf.CeilToInt((w + 2f * glow) * S);
            int H = Mathf.CeilToInt((h + 2f * glow) * S);
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[W * H];

            float hx = (w - slant) * 0.5f * S;
            float hy = h * 0.5f * S;
            float r = Mathf.Min(radius * S, Mathf.Min(hx, hy));
            float k = slant / h;                 // độ nghiêng: đỉnh lệch sang phải, đáy lệch sang trái
            float bw = border * S;
            float padS = Mathf.Max(glow * S, 1f);

            for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
            {
                float cx = i + 0.5f - W * 0.5f;
                float cy = j + 0.5f - H * 0.5f;
                float x = cx - k * cy;

                float qx = Mathf.Abs(x) - (hx - r);
                float qy = Mathf.Abs(cy) - (hy - r);
                float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                          + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;

                float inside = Mathf.Clamp01(0.5f - d);
                float edge = Mathf.Clamp01(0.5f + (d + bw));

                float ty = Mathf.Clamp01(cy / (2f * hy) + 0.5f);
                float tx = Mathf.Clamp01(cx / (W * 0.5f) * 0.5f + 0.5f);
                Color edgeC = Color.Lerp(edgeL, edgeR, tx);
                edgeC.a = 1f;

                Color fill = Color.Lerp(fillBottom, fillTop, ty);
                float line = (j % (3 * S) < S) ? scan : 0f;      // vạch quét ngang
                fill = Color.Lerp(fill, new Color(edgeC.r, edgeC.g, edgeC.b, fill.a), line);
                Color inC = Color.Lerp(fill, edgeC, edge);

                float g = 0f;
                if (d > 0f)
                {
                    g = glowAlpha * Mathf.Exp(-3f * d / padS);
                    g *= Mathf.Clamp01((padS - d) / (0.25f * padS));
                }
                Color outC = new Color(edgeC.r, edgeC.g, edgeC.b, g);

                px[j * W + i] = Color.Lerp(outC, inC, inside);
            }

            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), S, 0, SpriteMeshType.FullRect);
        }

        // ---------- Nút pause ở góc (dùng bởi PauseManager) ----------

        public const float CornerW = 92f, CornerH = 76f, CornerGlow = 16f;

        /// <summary>Màu neon hiện tại (theo AnimatedBackground nếu có, không thì hồng / cyan).</summary>
        public static void NeonColors(out Color a, out Color b)
        {
            a = new Color(1.00f, 0.25f, 0.80f);
            b = new Color(0.20f, 0.90f, 1.00f);
            if (AnimatedBackground.Instance != null)
            {
                a = Vivid(AnimatedBackground.Instance.paletteMiddle);
                b = Vivid(AnimatedBackground.Instance.paletteBottom);
            }
        }

        /// <summary>Khung kính nghiêng có viền neon + vầng sáng. Kích thước logic = (CornerW, CornerH) + 2 * CornerGlow mỗi chiều.</summary>
        public static Sprite MakeCornerFrame(Color a, Color b)
        {
            Color fillTop = new Color(0.12f, 0.05f, 0.26f, 0.94f);
            Color fillBot = new Color(0.04f, 0.01f, 0.10f, 0.96f);
            return MakeShape(CornerW, CornerH, 18f, 12f, 3f, CornerGlow, 0.65f, fillTop, fillBot, a, b, 0.08f);
        }

        /// <summary>Icon trắng có vầng sáng: pause = hai thanh nghiêng, play = tam giác bo góc. Kích thước logic 64 x 64.</summary>
        public static Sprite MakeIcon(bool pause, Color glowColor)
        {
            const int S = 2;
            const int N = 64;
            int W = N * S;
            var tex = new Texture2D(W, W, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[W * W];
            float padS = 14f * S;
            Color core = Color.Lerp(Color.white, glowColor, 0.12f);
            core.a = 1f;

            for (int j = 0; j < W; j++)
            for (int i = 0; i < W; i++)
            {
                float x = i + 0.5f - W * 0.5f;
                float y = j + 0.5f - W * 0.5f;
                float d;
                if (pause)
                {
                    const float k = 0.22f;   // nghiêng cùng hướng với khung
                    float d1 = RoundBoxSdf((x - 11f * S) - k * y, y, 5f * S, 17f * S, 3f * S);
                    float d2 = RoundBoxSdf((x + 11f * S) - k * y, y, 5f * S, 17f * S, 3f * S);
                    d = Mathf.Min(d1, d2);
                }
                else
                {
                    Vector2 pt = new Vector2(x, y);
                    Vector2 va = new Vector2(-9f * S, -13f * S);
                    Vector2 vb = new Vector2(-9f * S, 13f * S);
                    Vector2 vc = new Vector2(15f * S, 0f);
                    d = SdTriangle(pt, va, vb, vc) - 4f * S;   // trừ bán kính -> góc bo tròn
                }

                float inside = Mathf.Clamp01(0.5f - d);
                float g = 0f;
                if (d > 0f)
                {
                    g = 0.85f * Mathf.Exp(-d / (5f * S));
                    g *= Mathf.Clamp01((padS - d) / (0.3f * padS));
                }
                Color outC = new Color(glowColor.r, glowColor.g, glowColor.b, g);
                px[j * W + i] = Color.Lerp(outC, core, inside);
            }

            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, W), new Vector2(0.5f, 0.5f), S, 0, SpriteMeshType.FullRect);
        }

        static float RoundBoxSdf(float x, float y, float hx, float hy, float r)
        {
            float qx = Mathf.Abs(x) - (hx - r);
            float qy = Mathf.Abs(y) - (hy - r);
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        static float Cross2(Vector2 u, Vector2 v) { return u.x * v.y - u.y * v.x; }

        static float SdTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d = Mathf.Min(SegDist(p, a, b), Mathf.Min(SegDist(p, b, c), SegDist(p, c, a)));
            float s1 = Cross2(b - a, p - a), s2 = Cross2(c - b, p - b), s3 = Cross2(a - c, p - c);
            bool inside = (s1 >= 0f && s2 >= 0f && s3 >= 0f) || (s1 <= 0f && s2 <= 0f && s3 <= 0f);
            return inside ? -d : d;
        }

        static Sprite MakeVignette()
        {
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color32[N * N];
            for (int j = 0; j < N; j++)
            for (int i = 0; i < N; i++)
            {
                float nx = (i + 0.5f) / N * 2f - 1f;
                float ny = (j + 0.5f) / N * 2f - 1f;
                float r = Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f;
                float a = Mathf.SmoothStep(0.35f, 1f, r);
                px[j * N + i] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        // ---------- đọc phím (chạy được cả Input System mới lẫn Input cũ) ----------

        static bool PressedUp()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return true;
#endif
            return false;
        }

        static bool PressedDown()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) return true;
#endif
            return false;
        }

        static bool PressedConfirm()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) return true;
#endif
            return false;
        }
    }
}
