using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RhythmGame
{
    /// <summary>
    /// Dựng toàn bộ scene lúc runtime rồi nối các component (NotePool,
    /// NoteSpawner, InputHandler, JudgementSystem, GameManager, UIController)
    /// lại với nhau, để bạn chạy thử ngay mà không cần kéo thả tay trong Editor.
    ///
    /// CÁCH DÙNG: tạo 1 Empty GameObject trong scene rỗng, gắn script này, Play.
    ///
    /// KHI MỞ RỘNG: đây là phần nên thay thế dần bằng tay trong Editor.
    ///  - Kéo sprite/art thật vào, tạo prefab Note thật, gán vào "notePrefabOverride".
    ///  - Tạo file Chart Data thật (Assets/Create/RhythmGame/Chart Data), gán vào "chartOverride".
    ///  - Tự dựng Canvas + Text (hoặc TextMeshPro) đẹp hơn trong Editor, rồi gán
    ///    thẳng vào UIController thay vì để BuildUI() tự tạo.
    /// Các script còn lại (NoteSpawner, JudgementSystem, GameManager...) không
    /// cần đổi gì khi bạn làm việc này, vì chúng chỉ nhận reference qua field
    /// public, không quan tâm object được tạo bằng tay hay bằng code.
    /// </summary>
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Tuỳ chỉnh (để trống thì tự sinh nội dung test)")]
        public ChartData chartOverride;
        public NoteView notePrefabOverride;

        [Header("Layout")]
        public float laneSpacing = 1.8f;
        public float hitLineY = -3.3f;
        public float scrollSpeed = 7f;

        [Header("Máu (nhân vật)")]
        [Tooltip("Máu tối đa. Về 0 là thua, lượt chơi kết thúc.")]
        public float maxHealth = 100f;
        [Tooltip("Máu hồi khi bấm trúng note (Perfect).")]
        public float perfectHeal = 2f;
        [Tooltip("Máu hồi khi bấm trúng note (Good).")]
        public float goodHeal = 1.5f;
        [Tooltip("Máu hồi khi bấm trúng note (Bad).")]
        public float badHeal = 0.5f;
        [Tooltip("Máu hồi khi giữ xong hold note.")]
        public float holdCompleteHeal = 1f;
        [Tooltip("Máu mất mỗi lần miss note.")]
        public float missDamage = 8f;

        static readonly Color[] LaneColors =
        {
            new Color(0.76f, 0.29f, 0.60f),
            new Color(0.00f, 1.00f, 1.00f),
            new Color(0.07f, 0.98f, 0.02f),
            new Color(0.98f, 0.22f, 0.25f),
        };

        void Awake()
        {
            SetupCamera();
            Transform[] laneAnchors = BuildLanes(out SpriteRenderer[] receptors);

            NotePool pool = gameObject.AddComponent<NotePool>();
            pool.poolParent = new GameObject("NotePool").transform;
            pool.poolParent.SetParent(transform);
            pool.notePrefab = notePrefabOverride != null ? notePrefabOverride : BuildDefaultNotePrefab();

            NoteSpawner spawner = gameObject.AddComponent<NoteSpawner>();
            spawner.pool = pool;
            spawner.laneAnchors = laneAnchors;
            spawner.laneColors = LaneColors;
            spawner.hitLineY = hitLineY;
            spawner.scrollSpeed = scrollSpeed;
            spawner.spawnY = Camera.main.orthographicSize + 0.5f;

            InputHandler input = gameObject.AddComponent<InputHandler>();

            HealthSystem health = gameObject.AddComponent<HealthSystem>();
            health.maxHealth = maxHealth;
            health.perfectHeal = perfectHeal;
            health.goodHeal = goodHeal;
            health.badHeal = badHeal;
            health.holdCompleteHeal = holdCompleteHeal;
            health.missDamage = missDamage;
            health.SetHealth(maxHealth);

            JudgementSystem judgement = gameObject.AddComponent<JudgementSystem>();
            judgement.spawner = spawner;
            judgement.input = input;
            judgement.healthSystem = health;

            var audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
            judgement.audioSource = audioSource;
            // Gán SFX clip ở đây nếu đã có asset: judgement.perfectSfx = ...

            GameManager gm = gameObject.AddComponent<GameManager>();
            gm.spawner = spawner;
            gm.judgement = judgement;
            gm.input = input;
            gm.healthSystem = health;
            gm.chart = chartOverride != null ? chartOverride : BuildTestChart();

            var musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f; // 2D sound
            gm.musicSource = musicSource;

            ReceptorFlasher flasher = gameObject.AddComponent<ReceptorFlasher>();
            flasher.receptors = receptors;
            flasher.input = input;
            flasher.laneColors = LaneColors;

            BuildUI(gm, judgement, health);
        }

        void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = new GameObject("Main Camera").AddComponent<Camera>();
                cam.tag = "MainCamera";
            }
            cam.transform.position = new Vector3(0, 0, -10);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.07f, 0.11f);
        }

        Transform[] BuildLanes(out SpriteRenderer[] receptors)
        {
            var sprite = MakeWhiteSprite();
            float height = Camera.main.orthographicSize * 2f + 2f;
            var anchors = new Transform[4];
            receptors = new SpriteRenderer[4];

            for (int l = 0; l < 4; l++)
            {
                float x = (l - 1.5f) * laneSpacing;

                var bgGO = new GameObject("LaneBG_" + l);
                bgGO.transform.SetParent(transform);
                bgGO.transform.position = new Vector3(x, 0, 0);
                bgGO.transform.localScale = new Vector3(laneSpacing * 0.95f, height, 1f);
                var bgSr = bgGO.AddComponent<SpriteRenderer>();
                bgSr.sprite = sprite;
                bgSr.color = new Color(1, 1, 1, 0.05f);

                var anchorGO = new GameObject("LaneAnchor_" + l);
                anchorGO.transform.SetParent(transform);
                anchorGO.transform.position = new Vector3(x, 0, 0);
                anchors[l] = anchorGO.transform;

                var recGO = new GameObject("Receptor_" + l);
                recGO.transform.SetParent(transform);
                recGO.transform.position = new Vector3(x, hitLineY, 0);
                recGO.transform.localScale = new Vector3(laneSpacing * 0.85f, 0.5f, 1f);
                var recSr = recGO.AddComponent<SpriteRenderer>();
                recSr.sprite = sprite;
                recSr.color = Color.white;
                recSr.sortingOrder = 1;
                receptors[l] = recSr;
            }
            return anchors;
        }

        NoteView BuildDefaultNotePrefab()
        {
            var go = new GameObject("Note_Template");
            go.transform.SetParent(transform);
            go.SetActive(false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeWhiteSprite();
            sr.sortingOrder = 2;
            go.transform.localScale = new Vector3(laneSpacing * 0.85f, 0.5f, 1f);
            return go.AddComponent<NoteView>();
        }

        ChartData BuildTestChart()
        {
            var chart = ScriptableObject.CreateInstance<ChartData>();
            chart.bpm = 120f;
            chart.laneCount = 4;
            chart.firstNoteOffset = 0.5f;
            chart.GenerateTestChart();
            return chart;
        }

        Sprite MakeWhiteSprite()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        void BuildUI(GameManager gm, JudgementSystem judgement, HealthSystem health)
        {
            var canvasGO = new GameObject("Canvas");
            canvasGO.transform.SetParent(transform);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGO.AddComponent<GraphicRaycaster>();

            TMP_Text countdownText = MakeText(canvasGO.transform, "Countdown", 220, TextAlignmentOptions.Center,
                                               new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800, 400));
            TMP_Text judgeText = MakeText(canvasGO.transform, "Judge", 90, TextAlignmentOptions.Center,
                                           new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(800, 150));
            TMP_Text scoreText = MakeText(canvasGO.transform, "Score", 40, TextAlignmentOptions.TopLeft,
                                           new Vector2(0f, 1f), new Vector2(150, -40), new Vector2(400, 60));
            TMP_Text comboText = MakeText(canvasGO.transform, "Combo", 40, TextAlignmentOptions.TopLeft,
                                          new Vector2(0f, 1f), new Vector2(150, -90), new Vector2(400, 60));
            TMP_Text messageText = MakeText(canvasGO.transform, "Message", 70, TextAlignmentOptions.Center,
                                             new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 300));

            BuildHealthBar(canvasGO.transform, health);

            var ui = gameObject.AddComponent<UIController>();
            ui.gameManager = gm;
            ui.judgement = judgement;
            ui.healthSystem = health;
            ui.countdownText = countdownText;
            ui.judgeText = judgeText;
            ui.scoreText = scoreText;
            ui.comboText = comboText;
            ui.messageText = messageText;
        }

        void BuildHealthBar(Transform parent, HealthSystem health)
        {
            var sprite = MakeWhiteSprite();

            // Root: thanh nằm giữa cạnh trên, rộng 700px cao 34px.
            var root = new GameObject("HealthBar");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -30f);
            rt.sizeDelta = new Vector2(700f, 34f);

            // Nền thanh: giãn hết khung.
            Image bg = MakeImage("Background", rt, sprite, new Color(0f, 0f, 0f, 0.65f));
            var bgRect = bg.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Lớp ghost: phần máu vừa mất, tụt xuống chậm để thấy rõ.
            // Neo bám cạnh trái, bề rộng = anchorMax.x * bề rộng cha -> co từ phải sang trái.
            Image ghostImg = MakeImage("Ghost", rt, sprite, new Color(1f, 1f, 1f, 0.35f));
            RectTransform ghostRect = SetupBarRect(ghostImg.rectTransform, 26f);

            // Lớp máu chính.
            Image fillImg = MakeImage("Fill", rt, sprite, Color.green);
            RectTransform fillRect = SetupBarRect(fillImg.rectTransform, 26f);

            TMP_Text label = MakeText(rt, "Label", 24, TextAlignmentOptions.Center,
                                       new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 34f));

            var bar = root.AddComponent<HealthBar>();
            bar.Bind(health, ghostRect, fillRect, fillImg, label);
        }

        /// <summary>
        /// Layout cho lớp thanh: neo trái (x=0) tới neo phải (x=1), cao cố định,
        /// canh giữa theo trục Y. Đổi anchorMax.x để co giãn từ phải sang trái.
        /// </summary>
        RectTransform SetupBarRect(RectTransform r, float height)
        {
            r.anchorMin = new Vector2(0f, 0.5f);
            r.anchorMax = new Vector2(1f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.sizeDelta = new Vector2(0f, height);
            r.anchoredPosition = Vector2.zero;
            return r;
        }

        Image MakeImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(690f, 26f);

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        TMP_Text MakeText(Transform parent, string name, float fontSize, TextAlignmentOptions alignment,
                           Vector2 anchorPos, Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchorPos;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;

            var text = go.AddComponent<TextMeshProUGUI>();
            // Cần đã import TMP Essential Resources (Window > TextMeshPro > Import TMP Essential Resources)
            // để TMP_Settings.defaultFontAsset không bị null.
            if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = "";
            return text;
        }
    }
}
