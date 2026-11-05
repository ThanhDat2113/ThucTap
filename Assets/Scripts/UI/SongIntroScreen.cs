using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RhythmGame
{
    /// <summary>
    /// Man hinh intro bai hat: hien thong tin bai (tu Thuan.SongData) voi cac hieu ung UI
    /// song dong (giong SongIntro cua Thuan): background zoom 1.08->1, cover pop EaseOutBack,
    /// title truot tu trai + typewriter, credits truot tu duoi len, badge stamp + shake cover,
    /// floaters lo lung (sine + rotation). Sau do dem nguoc 3 2 1 vao gameplay.
    /// </summary>
    public class SongIntroScreen : MonoBehaviour
    {
        public static SongIntroScreen Instance { get; private set; }
        public bool IsPlaying { get; private set; }

        [Header("Refs")]
        public GameManager gameManager;

        [Header("UI (bat buoc)")]
        public TMP_Text songNameText;       // title
        public TMP_Text artistText;         // credits
        public TMP_Text difficultyText;     // badge
        public TMP_Text countdownText;
        public Image coverImage;

        [Header("Root phu (tuy chon - de hieu ung pop/stamp ro hon)")]
        [Tooltip("Root chua cover. Bo trong se dung coverImage.rectTransform.")]
        public RectTransform coverRoot;

        [Tooltip("Root chua badge do kho. Bo trong se dung difficultyText.rectTransform.")]
        public RectTransform badgeRoot;

        [Header("Background (tuy chon - zoom tu 1.08 -> 1)")]
        public RectTransform background;

        [Header("Stage + CanvasGroup (tuy chon - fade out muot + scale nhe)")]
        public RectTransform stage;   // khung 1920x1080 neu co
        public CanvasGroup group;     // group cua man intro neu co

        [Header("Floaters - not nhac / vuong mien bay lo lung (uu tien)")]
        public RectTransform[] floaters;

        [Header("Legacy: danh sach float (dung khi floaters rong)")]
        public List<RectTransform> floatObjects;

        [Header("Settings")]
        [Tooltip("Tong thoi gian intro (tinh tu luc bat dau animation toi khi an di).")]
        public float introDuration = 2.5f;

        [Tooltip("Thoi gian dem nguoc 3 2 1 sau intro.")]
        public float countdownSeconds = 3f;

        [Tooltip("Thoi gian toi thieu truoc khi cho phep skip.")]
        public float minTimeBeforeSkip = 0.8f;

        [Header("Audio (tuy chon)")]
        public AudioClip whooshClip;
        public AudioClip stampClip;

        private Vector2[] floaterBase;
        private Vector3[] floaterBaseEuler;
        private bool skipRequested;

        private void Awake()
        {
            Instance = this;
            IsPlaying = true;
            KeepOnTop();
        }

        private void Start()
        {
            // ★ Intro phải chạy CẢ trong Story Mode (flow: Intro → Dialogue → Gameplay)
            StartCoroutine(IntroSequence());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (stage != null) FitStage();
        }

        // ---------------- Public helpers ----------------

        private void KeepOnTop()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) return;
            var layers = SortingLayer.layers;
            if (layers.Length > 0) canvas.sortingLayerID = layers[layers.Length - 1].id;
            canvas.sortingOrder = 500;
        }

        // ---------------- Timeline ----------------

        private IEnumerator IntroSequence()
        {
            // Lay thong tin bai tu GameSession (Thuan.SongData, global namespace)
            var song = GameSession.SelectedSong;
            if (song == null)
            {
                if (songNameText != null) songNameText.text = "Chua chon bai";
                if (artistText != null) artistText.text = "";
                if (difficultyText != null) difficultyText.text = "";
                if (coverImage != null) coverImage.enabled = false;
                if (countdownText != null) countdownText.text = "";
                yield return new WaitForSecondsRealtime(0.5f);
                if (gameManager != null) gameManager.Restart();
                IsPlaying = false;
                yield break;
            }

            Fill(song);

            // Nhac chi bat dau sau countdown (trong GameManager.ScheduleMusic), khong phat o day.

            // Resolve danh sach floaters: uu tien field moi, fallback list cu, cuoi cung la UI chinh
            var fList = new List<RectTransform>();
            if (floaters != null && floaters.Length > 0) fList.AddRange(floaters);
            else if (floatObjects != null && floatObjects.Count > 0) fList.AddRange(floatObjects);
            else
            {
                if (songNameText != null) fList.Add(songNameText.rectTransform);
                if (artistText != null) fList.Add(artistText.rectTransform);
                if (difficultyText != null) fList.Add(difficultyText.rectTransform);
                if (coverImage != null) fList.Add(coverImage.rectTransform);
            }
            floaters = fList.ToArray();

            floaterBase = new Vector2[floaters.Length];
            floaterBaseEuler = new Vector3[floaters.Length];
            for (int i = 0; i < floaters.Length; i++)
            {
                if (floaters[i] == null) continue;
                floaterBase[i] = floaters[i].anchoredPosition;
                floaterBaseEuler[i] = floaters[i].localEulerAngles;
            }

            // Trang thai bat dau: an het cac phan tu
            HideElements();

            var titleBase = songNameText != null ? songNameText.rectTransform.anchoredPosition : Vector2.zero;
            var creditBase = artistText != null ? artistText.rectTransform.anchoredPosition : Vector2.zero;

            // Cho frame on dinh de tranh giat khi scene vua load
            float waited = 0f;
            int stable = 0;
            while (stable < 4 && waited < 2f)
            {
                float d = Time.unscaledDeltaTime;
                waited += d;
                stable = d < 0.05f ? stable + 1 : 0;
                yield return null;
            }

            PlaySound(whooshClip);

            // Cache root refs 1 lan
            var coverT = coverRoot != null ? coverRoot : (coverImage != null ? coverImage.rectTransform : null);
            var badgeT = badgeRoot != null ? badgeRoot : (difficultyText != null ? difficultyText.rectTransform : null);

            float t = 0f;
            bool stamped = false;
            const float animDone = 1.1f;                       // thoi diem moi animation ket thuc
            float total = Mathf.Max(introDuration, animDone);   // tong thoi gian intro

            while (t < total)
            {
                // Clamp delta de frame giat khong lam animation nhay coc
                t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                if (t > minTimeBeforeSkip && SkipPressed()) skipRequested = true;
                if (skipRequested) break;

                // 1) Background zoom 1.08 -> 1
                if (background != null)
                    SetScale(background, Mathf.Lerp(1.08f, 1f, EaseOut(t / 0.9f)));

                // 2) Cover pop-in (vuot qua 1 roi ve 1) + fade
                float c = Mathf.Clamp01((t - 0.15f) / 0.45f);
                if (coverT != null)
                {
                    SetScale(coverT, Mathf.LerpUnclamped(0.55f, 1f, EaseOutBack(c)));
                    SetAlpha(coverT, Mathf.Clamp01(c * 2.5f));
                }

                // 3) Title truot tu trai + typewriter
                float ti = Mathf.Clamp01((t - 0.4f) / 0.4f);
                if (songNameText != null)
                {
                    songNameText.rectTransform.anchoredPosition = titleBase + new Vector2(-90f * (1f - EaseOut(ti)), 0f);
                    SetAlpha(songNameText, ti);
                    int charCount = songNameText.textInfo.characterCount;
                    float typed = EaseOut(Mathf.Clamp01((t - 0.4f) / 0.5f));
                    songNameText.maxVisibleCharacters = Mathf.CeilToInt(charCount * typed) + (ti >= 1f ? 999 : 0);
                }

                // 4) Artist/credits truot tu duoi len
                float cr = Mathf.Clamp01((t - 0.65f) / 0.35f);
                if (artistText != null)
                {
                    artistText.rectTransform.anchoredPosition = creditBase + new Vector2(0f, -25f * (1f - EaseOut(cr)));
                    SetAlpha(artistText, cr);
                }

                // 5) Badge stamp + shake cover khi dong dau
                float b = Mathf.Clamp01((t - 0.8f) / 0.22f);
                if (badgeT != null)
                {
                    SetAlpha(badgeT, b);
                    SetScale(badgeT, Mathf.Lerp(2.2f, 1f, EaseOut(b)));
                    if (b >= 1f && !stamped)
                    {
                        stamped = true;
                        PlaySound(stampClip);
                        if (coverT != null) StartCoroutine(Shake(coverT, 0.18f, 7f));
                    }
                }

                // 6) Floaters lo lung + nghieng nhe
                for (int i = 0; i < floaters.Length; i++)
                {
                    if (floaters[i] == null) continue;
                    float fa = Mathf.Clamp01((t - 0.3f - i * 0.07f) / 0.35f);
                    SetAlpha(floaters[i], fa);
                    floaters[i].anchoredPosition =
                        floaterBase[i] + new Vector2(0f,
                            Mathf.Sin(t * 2.2f + i * 1.1f) * 10f + (1f - EaseOut(fa)) * -30f);
                    floaters[i].localEulerAngles =
                        floaterBaseEuler[i] + new Vector3(0f, 0f, Mathf.Sin(t * 1.6f + i) * 6f);
                }

                yield return null;
            }

            // Hoan tat hien thi neu bi skip giua chung
            if (songNameText != null)
            {
                songNameText.maxVisibleCharacters = 99999;
                songNameText.rectTransform.anchoredPosition = titleBase;
            }
            if (artistText != null) artistText.rectTransform.anchoredPosition = creditBase;
            if (coverT != null) SetScale(coverT, 1f);
            if (badgeT != null) SetScale(badgeT, 1f);
            if (background != null) SetScale(background, 1f);
            for (int i = 0; i < floaters.Length; i++)
            {
                if (floaters[i] == null) continue;
                floaters[i].anchoredPosition = floaterBase[i];
                floaters[i].localEulerAngles = floaterBaseEuler[i];
            }

            // Fade out group + scale nhe (giong SongIntro)
            if (group != null)
            {
                float o = 0f;
                float startAlpha = group.alpha;
                const float fadeDur = 0.4f;
                while (o < fadeDur)
                {
                    o += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                    float k = Mathf.Clamp01(o / fadeDur);
                    group.alpha = Mathf.Lerp(startAlpha, 0f, k);
                    if (stage != null) SetScale(stage, StageScale() * (1f + 0.06f * k));
                    yield return null;
                }
            }

            // An cac phan tu info (khi group da mo, an ngay khong bi lo)
            HideInfoElements();

            // Reset group/stage
            if (group != null) group.alpha = 1f;
            if (stage != null) SetScale(stage, StageScale());

            // Dem nguoc 3 2 1 -> GO! (SongIntroScreen quan ly countdown duy nhat)
            float countdownRemaining = countdownSeconds;
            while (countdownRemaining > 0f)
            {
                countdownRemaining -= Time.unscaledDeltaTime;
                int num = Mathf.CeilToInt(countdownRemaining);
                if (countdownText != null && num > 0)
                    countdownText.text = num.ToString();
                yield return null;
            }

            // Hien "GO!" du lau de thay
            if (countdownText != null) countdownText.text = "GO!";
            yield return new WaitForSecondsRealtime(0.35f);
            if (countdownText != null) countdownText.text = "";

            // ★ An han UI intro truoc khi chuyen sang dialogue/gameplay
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            if (stage != null) stage.gameObject.SetActive(false);
            if (background != null) background.gameObject.SetActive(false);

            // ★ Story Mode: chuyen sang Pre-battle Dialogue (không gọi Restart ngay)
            var storyDlg = Luan.Dialogue.StoryDialogueManager.Instance;
            if (StorySession.Active
                && storyDlg != null
                && Luan.Dialogue.StoryDialogueManager.HasPendingDialogue())
            {
                Debug.Log("[SongIntroScreen] Intro xong -> chuyen sang dialogue cot truyen.");
                IsPlaying = false;
                storyDlg.BeginStoryFlow();
                yield break;
            }

            // ★ Free Play hoac Story Mode khong co dialogue: vao gameplay luon
            if (gameManager != null)
            {
                float saved = gameManager.countdownSeconds;
                gameManager.countdownSeconds = 0f;
                gameManager.Restart();
                gameManager.countdownSeconds = saved;
            }

            IsPlaying = false;
        }

        // ---------------- Helpers ----------------

        private void Fill(SongData song)
        {
            if (songNameText != null)
            {
                songNameText.text = string.IsNullOrEmpty(song.songName) ? "Demo Track" : song.songName;
                songNameText.ForceMeshUpdate();   // de textInfo.characterCount chinh xac
            }
            if (artistText != null)
                artistText.text = string.IsNullOrEmpty(song.artist) ? "Unknown Artist" : song.artist;
            if (difficultyText != null)
                difficultyText.text = $"Do kho {song.difficulty}/5";
            if (coverImage != null)
            {
                coverImage.sprite = song.coverArt;
                coverImage.enabled = song.coverArt != null;
            }
        }

        private void HideElements()
        {
            if (background != null) SetScale(background, 1.08f);

            var coverT = coverRoot != null ? coverRoot : (coverImage != null ? coverImage.rectTransform : null);
            if (coverT != null) { SetScale(coverT, 0.55f); SetAlpha(coverT, 0f); }

            if (songNameText != null) { SetAlpha(songNameText, 0f); songNameText.maxVisibleCharacters = 0; }
            if (artistText != null) SetAlpha(artistText, 0f);

            var badgeT = badgeRoot != null ? badgeRoot : (difficultyText != null ? difficultyText.rectTransform : null);
            if (badgeT != null) SetAlpha(badgeT, 0f);

            if (floaters != null) foreach (var f in floaters) if (f != null) SetAlpha(f, 0f);
        }

        private void HideInfoElements()
        {
            if (songNameText != null) songNameText.gameObject.SetActive(false);
            if (artistText != null) artistText.gameObject.SetActive(false);
            if (difficultyText != null) difficultyText.gameObject.SetActive(false);
            if (coverImage != null) coverImage.gameObject.SetActive(false);
            if (coverRoot != null && (coverImage == null || coverRoot.gameObject != coverImage.gameObject))
                coverRoot.gameObject.SetActive(false);
            if (badgeRoot != null && (difficultyText == null || badgeRoot.gameObject != difficultyText.gameObject))
                badgeRoot.gameObject.SetActive(false);

            // Ẩn background — tránh nó ở lại vĩnh viễn khi vào gameplay
            if (background != null) background.gameObject.SetActive(false);

            if (floaters != null)
                foreach (var f in floaters) if (f != null) f.gameObject.SetActive(false);
        }

        private static bool SkipPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
#else
            return Input.anyKeyDown;
#endif
        }

        private IEnumerator Shake(RectTransform rt, float time, float power)
        {
            if (rt == null) yield break;
            var basePos = rt.anchoredPosition;
            float t = 0f;
            while (t < time)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - t / time;
                rt.anchoredPosition = basePos + Random.insideUnitCircle * power * k;
                yield return null;
            }
            rt.anchoredPosition = basePos;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && SFXManager.Instance != null) SFXManager.Instance.PlaySFX(clip);
        }

        // ---------------- Stage fit (neu co dung khung 1920x1080) ----------------

        private float StageScale()
        {
            if (stage == null) return 1f;
            var parent = stage.parent as RectTransform;
            if (parent == null) return 1f;
            var size = parent.rect.size;
            if (size.x < 2f || size.y < 2f) return 1f;
            return Mathf.Max(size.x / 1920f, size.y / 1080f);
        }

        private void FitStage()
        {
            if (stage == null) return;
            float s = StageScale();
            // Cho phep scale nhe (pop 6%) khong bi FitStage keo ve ngay
            if (stage.localScale.x < s * 0.999f || stage.localScale.x > s * 1.07f)
                stage.localScale = new Vector3(s, s, 1f);
        }

        // ---------------- Easing / tween static ----------------

        private static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - Mathf.Pow(1f - x, 3f); }

        private static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        private static void SetScale(Transform t, float s)
        {
            if (t != null) t.localScale = new Vector3(s, s, 1f);
        }

        private static void SetAlpha(Component c, float a)
        {
            if (c == null) return;
            var cg = c.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = a; return; }
            var g = c.GetComponent<Graphic>();
            if (g != null) { var col = g.color; col.a = a; g.color = col; }
        }
    }
}
