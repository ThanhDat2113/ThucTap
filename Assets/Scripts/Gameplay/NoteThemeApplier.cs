using System.Collections;
using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Làm note "ăn khớp" với từng cảnh background: mỗi cảnh một kiểu note (Neon / Glass / Capsule)
    /// và một bộ màu 4 lane riêng; receptor (vạch bấm) đổi màu theo.
    ///
    /// CÁCH DÙNG: WorldBackgroundSelector tự thêm và gọi script này mỗi khi đổi cảnh (bật "Match Notes To Scene").
    /// Cũng có thể gắn tay vào 1 GameObject bất kỳ và gọi ApplyForScene(BgScene.Forest).
    /// Chọn cảnh None sẽ trả note về màu và kiểu ban đầu của bạn.
    /// </summary>
    public class NoteThemeApplier : MonoBehaviour
    {
        [Header("Refs (để trống = tự tìm)")]
        public NoteSpawner spawner;
        public ReceptorFlasher receptors;

        [Header("Áp dụng những gì")]
        [Tooltip("Đổi kiểu note (Neon / Glass / Capsule) theo cảnh")]
        public bool overrideStyle = true;
        [Tooltip("Đổi màu 4 lane của note theo cảnh")]
        public bool overrideLaneColors = true;
        [Tooltip("Đổi màu receptor (vạch bấm) cho khớp màu note")]
        public bool overrideReceptors = true;
        [Tooltip("In 1 dòng vào Console mỗi lần áp theme để kiểm tra")]
        public bool debugLog = true;

        bool captured;
        Color[] defaultColors;
        NoteStyle defaultStyle = NoteStyle.Neon;
        Coroutine routine;

        public void ApplyForScene(BgScene scene)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(ApplyRoutine(scene));
        }

        IEnumerator ApplyRoutine(BgScene scene)
        {
            yield return null; // chờ các script khác chạy xong Start()

            if (!spawner) spawner = FindOne<NoteSpawner>();
            if (!receptors) receptors = FindOne<ReceptorFlasher>();
            NoteView[] notes = FindAll<NoteView>();
            if (!captured) Capture(notes);

            NoteStyle style;
            Color[] colors;
            bool hasTheme = GetTheme(scene, out style, out colors);
            if (!hasTheme)
            {
                style = defaultStyle;      // cảnh None: trả về như ban đầu
                colors = defaultColors;
            }

            // Note sinh ra sau này lấy màu từ spawner.laneColors (đọc mỗi lần Spawn)
            if (overrideLaneColors && colors != null && spawner != null)
            {
                if (!hasTheme)
                {
                    spawner.laneColors = (Color[])colors.Clone();   // trả về đúng mảng màu ban đầu
                }
                else
                {
                    // Mảng màu trong Inspector ngắn (vd chỉ 1 màu) thì phải nới ra,
                    // nếu không mọi lane dùng chung 1 màu (laneColors[lane % Length])
                    if (spawner.laneColors == null || spawner.laneColors.Length < colors.Length)
                        spawner.laneColors = new Color[colors.Length];
                    for (int i = 0; i < colors.Length; i++) spawner.laneColors[i] = colors[i];
                }
            }

            if (overrideReceptors && colors != null && receptors != null)
            {
                try
                {
                    for (int l = 0; l < colors.Length; l++) receptors.SetLaneColor(l, colors[l]);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("NoteThemeApplier: không đổi được màu receptor: " + e.Message);
                }
            }

            // Vẽ lại sprite của mọi note (kể cả note đang nằm sẵn trong pool)
            int withSkin = 0;
            if (overrideStyle)
            {
                // Pool chưa tạo note nào lúc này -> đặt kiểu toàn cục để note tạo sau cũng dùng đúng kiểu
                if (hasTheme) NoteSkin.SetGlobalStyle(style);
                else NoteSkin.ClearGlobalStyle();

                foreach (var note in notes)
                    if (note != null && note.skin != null) { note.skin.SetStyle(style); withSkin++; }
            }

            if (debugLog)
                Debug.Log("[NoteTheme] cảnh=" + scene + " | có theme=" + hasTheme + " | kiểu=" + style
                    + " | spawner=" + (spawner ? "ok" : "KHÔNG THẤY")
                    + " | số màu lane=" + (spawner != null && spawner.laneColors != null ? spawner.laneColors.Length : 0)
                    + " | receptor=" + (receptors ? "ok" : "không thấy")
                    + " | note có sẵn=" + notes.Length + " (có skin: " + withSkin + ")");

            routine = null;
        }

        void Capture(NoteView[] notes)
        {
            captured = true;
            if (spawner != null && spawner.laneColors != null)
                defaultColors = (Color[])spawner.laneColors.Clone();
            foreach (var n in notes)
                if (n != null && n.skin != null) { defaultStyle = n.skin.style; break; }
        }

        static Color[] Cols(params float[] v)
        {
            var c = new Color[v.Length / 3];
            for (int i = 0; i < c.Length; i++) c[i] = new Color(v[i * 3], v[i * 3 + 1], v[i * 3 + 2], 1f);
            return c;
        }

        /// <summary>Trả về false nếu cảnh không có theme (None) -> dùng giá trị mặc định ban đầu.</summary>
        static bool GetTheme(BgScene s, out NoteStyle style, out Color[] colors)
        {
            switch (s)
            {
                case BgScene.Synthwave:
                case BgScene.SynthBars:
                    style = NoteStyle.Neon;
                    colors = Cols(1.00f, 0.25f, 0.75f, 0.20f, 1.00f, 1.00f, 0.70f, 0.35f, 1.00f, 1.00f, 0.85f, 0.25f);
                    return true;
                case BgScene.Sunset:
                    style = NoteStyle.Glass;
                    colors = Cols(1.00f, 0.85f, 0.35f, 1.00f, 0.40f, 0.50f, 0.75f, 0.55f, 1.00f, 0.40f, 0.90f, 1.00f);
                    return true;
                case BgScene.Dream:
                    style = NoteStyle.Capsule;
                    colors = Cols(1.00f, 0.55f, 0.85f, 0.75f, 0.60f, 1.00f, 0.50f, 1.00f, 0.95f, 1.00f, 0.95f, 0.60f);
                    return true;
                case BgScene.Aurora:
                    style = NoteStyle.Neon;
                    colors = Cols(0.30f, 1.00f, 0.60f, 0.30f, 0.85f, 1.00f, 0.70f, 0.45f, 1.00f, 1.00f, 0.40f, 0.80f);
                    return true;
                case BgScene.Rainy:
                    style = NoteStyle.Glass;
                    colors = Cols(0.40f, 0.75f, 1.00f, 0.65f, 0.60f, 1.00f, 0.35f, 0.95f, 0.85f, 1.00f, 0.80f, 0.45f);
                    return true;
                case BgScene.Blossom:
                    style = NoteStyle.Capsule;
                    colors = Cols(0.90f, 0.20f, 0.50f, 0.60f, 0.25f, 0.75f, 0.10f, 0.70f, 0.70f, 1.00f, 0.50f, 0.25f);
                    return true;
                case BgScene.Night:
                    style = NoteStyle.Neon;
                    colors = Cols(0.55f, 0.65f, 1.00f, 1.00f, 0.85f, 0.40f, 1.00f, 0.50f, 0.80f, 0.50f, 1.00f, 0.80f);
                    return true;
                case BgScene.Day:
                    style = NoteStyle.Glass;
                    colors = Cols(1.00f, 0.55f, 0.15f, 1.00f, 0.30f, 0.60f, 0.20f, 0.80f, 0.40f, 0.55f, 0.30f, 0.90f);
                    return true;
                case BgScene.Winter:
                    style = NoteStyle.Glass;
                    colors = Cols(0.15f, 0.40f, 0.90f, 0.50f, 0.30f, 0.85f, 0.10f, 0.70f, 0.75f, 0.85f, 0.25f, 0.60f);
                    return true;
                case BgScene.Forest:
                    style = NoteStyle.Neon;
                    colors = Cols(0.70f, 1.00f, 0.30f, 0.20f, 1.00f, 0.80f, 1.00f, 0.90f, 0.30f, 1.00f, 0.50f, 0.70f);
                    return true;
                case BgScene.Autumn:
                    style = NoteStyle.Capsule;
                    colors = Cols(1.00f, 0.60f, 0.15f, 0.95f, 0.25f, 0.20f, 1.00f, 0.85f, 0.30f, 1.00f, 0.95f, 0.70f);
                    return true;
                case BgScene.Equalizer:
                    style = NoteStyle.Neon;
                    colors = Cols(1.00f, 0.30f, 0.70f, 0.30f, 1.00f, 1.00f, 0.70f, 1.00f, 0.40f, 0.80f, 0.50f, 1.00f);
                    return true;
                default:
                    style = NoteStyle.Neon;
                    colors = null;
                    return false;
            }
        }

        static T FindOne<T>() where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<T>();
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        static T[] FindAll<T>() where T : UnityEngine.Object
        {
#if UNITY_2023_1_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            return UnityEngine.Object.FindObjectsOfType<T>(true);
#endif
        }
    }
}