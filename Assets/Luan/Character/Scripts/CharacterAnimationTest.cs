using UnityEngine;
using UnityEngine.InputSystem;

namespace Luan.Character
{
    /// <summary>Play-mode preview for the Riff and Lumi sprite animations.</summary>
    public sealed class CharacterAnimationTest : MonoBehaviour
    {
        [Header("Scene characters")]
        [SerializeField] private SpriteRenderer riffRenderer;
        [SerializeField] private SpriteRenderer lumiRenderer;
        [SerializeField] private Sprite riffIdle;
        [SerializeField] private Sprite lumiIdle;

        [Header("Frames in playback order")]
        [SerializeField] private Sprite[] riffDance;
        [SerializeField] private Sprite[] lumiDance;
        [SerializeField] private Sprite[] riffDance2;
        [SerializeField] private Sprite[] lumiDance2;
        [SerializeField] private Sprite[] riffDance3;
        [SerializeField] private Sprite[] lumiDance3;
        [SerializeField] private Sprite[] riffDance4;
        [SerializeField] private Sprite[] lumiDance4;
        [SerializeField] private Sprite[] riffMiss;
        [SerializeField] private Sprite[] lumiMiss;

        [Header("Timing")]
        [SerializeField, Min(0.02f)] private float danceFrameSeconds = 0.04f;
        [SerializeField, Min(0.02f)] private float missFrameSeconds = 0.04f;

        private Performer riff;
        private Performer lumi;
        private int selected;
        private GUIStyle hudStyle;

        private void Awake()
        {
#if UNITY_EDITOR
            if (HasMissingReferences())
            {
                AutoLoadAllFrames();
            }
#endif

            var missing = GetMissingReferenceNames();
            if (missing.Count > 0)
            {
                Debug.LogError($"[CharacterAnimationTest] Thiếu renderer hoặc animation frames: {string.Join(", ", missing)}", this);
                enabled = false;
                return;
            }

            riff = new Performer(riffRenderer, riffIdle, new[] { riffDance, riffDance2, riffDance3, riffDance4 }, riffMiss);
            lumi = new Performer(lumiRenderer, lumiIdle, new[] { lumiDance, lumiDance2, lumiDance3, lumiDance4 }, lumiMiss);
            SelectDance(0, 0); // Play starts with Riff's first dance.
        }

        private bool HasMissingReferences()
        {
            return riffRenderer == null || lumiRenderer == null ||
                   riffIdle == null || lumiIdle == null ||
                   riffDance == null || riffDance.Length == 0 ||
                   lumiDance == null || lumiDance.Length == 0 ||
                   riffDance2 == null || riffDance2.Length == 0 ||
                   lumiDance2 == null || lumiDance2.Length == 0 ||
                   riffDance3 == null || riffDance3.Length == 0 ||
                   lumiDance3 == null || lumiDance3.Length == 0 ||
                   riffDance4 == null || riffDance4.Length == 0 ||
                   lumiDance4 == null || lumiDance4.Length == 0 ||
                   riffMiss == null || riffMiss.Length == 0 ||
                   lumiMiss == null || lumiMiss.Length == 0;
        }

        private System.Collections.Generic.List<string> GetMissingReferenceNames()
        {
            var list = new System.Collections.Generic.List<string>();
            if (riffRenderer == null) list.Add("riffRenderer");
            if (lumiRenderer == null) list.Add("lumiRenderer");
            if (riffIdle == null) list.Add("riffIdle");
            if (lumiIdle == null) list.Add("lumiIdle");
            if (riffDance == null || riffDance.Length == 0) list.Add("riffDance");
            if (lumiDance == null || lumiDance.Length == 0) list.Add("lumiDance");
            if (riffDance2 == null || riffDance2.Length == 0) list.Add("riffDance2");
            if (lumiDance2 == null || lumiDance2.Length == 0) list.Add("lumiDance2");
            if (riffDance3 == null || riffDance3.Length == 0) list.Add("riffDance3");
            if (lumiDance3 == null || lumiDance3.Length == 0) list.Add("lumiDance3");
            if (riffDance4 == null || riffDance4.Length == 0) list.Add("riffDance4");
            if (lumiDance4 == null || lumiDance4.Length == 0) list.Add("lumiDance4");
            if (riffMiss == null || riffMiss.Length == 0) list.Add("riffMiss");
            if (lumiMiss == null || lumiMiss.Length == 0) list.Add("lumiMiss");
            return list;
        }

        private void Update()
        {
            // Unity can reload scripts while Play mode is active, clearing these
            // nonserialized players without calling Awake again.
            if (riff == null || lumi == null)
            {
                Awake();
                if (!enabled) return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                    SelectDance(0, 0);
                else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                    SelectDance(1, 0);
                else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
                    SelectDance(0, 1);
                else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
                    SelectDance(1, 1);
                else if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame)
                    SelectDance(0, 2);
                else if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame)
                    SelectDance(1, 2);
                else if (keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame)
                    SelectDance(0, 3);
                else if (keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame)
                    SelectDance(1, 3);
                else if (keyboard.mKey.wasPressedThisFrame)
                    (selected == 0 ? riff : lumi).PlayMiss();
            }

            // Do not skip a run of drawings after an Editor stall or window focus change.
            float delta = Mathf.Min(Time.deltaTime, 0.05f);
            riff.Tick(delta, danceFrameSeconds, missFrameSeconds);
            lumi.Tick(delta, danceFrameSeconds, missFrameSeconds);
        }

        private void SelectDance(int character, int danceIndex)
        {
            selected = character;
            if (character == 0)
            {
                riff.PlayDance(danceIndex);
                lumi.PlayIdle();
            }
            else
            {
                riff.PlayIdle();
                lumi.PlayDance(danceIndex);
            }
        }

        private void OnGUI()
        {
            if (hudStyle == null)
            {
                hudStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true
                };
                hudStyle.normal.textColor = new Color(0.94f, 0.96f, 1f);
            }

            GUI.Label(new Rect(0, 14, Screen.width, 36),
                "Phím 1-8: Đổi điệu nhảy (Dance 1-4) | Phím M: Miss", hudStyle);
            GUI.Label(new Rect(0, 48, Screen.width, 30),
                selected == 0 ? "★ Đang chọn: RIFF" : "★ Đang chọn: LUMI", hudStyle);

            // Nút bấm trực tiếp trên màn hình Game để tiện test
            GUILayout.BeginArea(new Rect(20, Screen.height - 110, Screen.width - 40, 95));
            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>RIFF:</b>", GUILayout.Width(60));
            if (GUILayout.Button("Dance 1 (Phím 1)", GUILayout.Height(38))) SelectDance(0, 0);
            if (GUILayout.Button("Dance 2 (Phím 3)", GUILayout.Height(38))) SelectDance(0, 1);
            if (GUILayout.Button("Dance 3 (Phím 5)", GUILayout.Height(38))) SelectDance(0, 2);
            if (GUILayout.Button("Dance 4 (Phím 7)", GUILayout.Height(38))) SelectDance(0, 3);
            if (GUILayout.Button("Miss (M)", GUILayout.Height(38))) { selected = 0; riff.PlayMiss(); }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUILayout.BeginHorizontal();
            GUILayout.Label("<b>LUMI:</b>", GUILayout.Width(60));
            if (GUILayout.Button("Dance 1 (Phím 2)", GUILayout.Height(38))) SelectDance(1, 0);
            if (GUILayout.Button("Dance 2 (Phím 4)", GUILayout.Height(38))) SelectDance(1, 1);
            if (GUILayout.Button("Dance 3 (Phím 6)", GUILayout.Height(38))) SelectDance(1, 2);
            if (GUILayout.Button("Dance 4 (Phím 8)", GUILayout.Height(38))) SelectDance(1, 3);
            if (GUILayout.Button("Miss (M)", GUILayout.Height(38))) { selected = 1; lumi.PlayMiss(); }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (HasMissingReferences())
            {
                AutoLoadAllFrames();
            }
        }

        [ContextMenu("Tự Động Nạp Toàn Bộ Frame Nhân Vật (Auto Load Frames)")]
        public void AutoLoadAllFrames()
        {
            if (riffRenderer == null)
            {
                var riffGo = GameObject.Find("Riff");
                if (riffGo != null) riffRenderer = riffGo.GetComponent<SpriteRenderer>();
            }
            if (lumiRenderer == null)
            {
                var lumiGo = GameObject.Find("Lumi");
                if (lumiGo != null) lumiRenderer = lumiGo.GetComponent<SpriteRenderer>();
            }

            if (riffIdle == null)
                riffIdle = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luan/Character/Riff/Riff.png");
            if (lumiIdle == null)
                lumiIdle = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luan/Character/Lumi/Lumi.png");

            riffDance = LoadFramesFromAnimOrFolder("Assets/Luan/Character/Riff/Dance/Riff_Dance.anim", "Assets/Luan/Character/Riff/Dance/Frames", 128);
            riffDance2 = LoadFramesFromAnimOrFolder("Assets/Luan/Character/Riff/Dance2/Riff_Dance2.anim", "Assets/Luan/Character/Riff/Dance2/Frames", 128);
            riffDance3 = LoadFramesFromAnimOrFolder("Assets/Luan/Character/Riff/Dance3/Riff_Dance3.anim", "Assets/Luan/Character/Riff/Dance3/Frames", 128);
            riffDance4 = LoadFramesFromAnimOrFolder("Assets/Luan/Character/Riff/Dance4/Riff_Dance4.anim", "Assets/Luan/Character/Riff/Dance4/Frames", 128);
            riffMiss = LoadFramesFromAnimOrFolder("Assets/Luan/Character/Riff/Miss/Riff_Miss.anim", "Assets/Luan/Character/Riff/Miss/Frames", 73);

            lumiDance = LoadFramesFromAnimOrFolder("Assets/Luan/Character/Lumi/Dance/Lumi_Dance.anim", "Assets/Luan/Character/Lumi/Dance/Frames", 128);
            lumiDance2 = LoadFramesFromAnimOrFolder("Assets/Luan/Character/Lumi/Dance2/Lumi_Dance2.anim", "Assets/Luan/Character/Lumi/Dance2/Frames", 128);
            lumiDance3 = LoadFramesFromAnimOrFolder("Assets/Luan/Character/Lumi/Dance3/Lumi_Dance3.anim", "Assets/Luan/Character/Lumi/Dance3/Frames", 128);
            lumiDance4 = LoadFramesFromAnimOrFolder("Assets/Luan/Character/Lumi/Dance4/Lumi_Dance4.anim", "Assets/Luan/Character/Lumi/Dance4/Frames", 128);
            lumiMiss = LoadFramesFromAnimOrFolder("Assets/Luan/Character/Lumi/Miss/Lumi_Miss.anim", "Assets/Luan/Character/Lumi/Miss/Frames", 73);

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[CharacterAnimationTest] Đã tự động nạp đầy đủ Frame cho Riff và Lumi thành công!", this);
        }

        private static Sprite[] LoadFramesFromAnimOrFolder(string animPath, string folderPath, int expectedCount)
        {
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
            if (clip != null)
            {
                var bindings = UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(clip);
                if (bindings != null && bindings.Length > 0)
                {
                    var keyframes = UnityEditor.AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
                    if (keyframes != null && keyframes.Length > 0)
                    {
                        int count = Mathf.Min(expectedCount, keyframes.Length);
                        var sprites = new Sprite[count];
                        for (int i = 0; i < count; i++)
                        {
                            sprites[i] = keyframes[i].value as Sprite;
                        }
                        return sprites;
                    }
                }
            }

            if (System.IO.Directory.Exists(folderPath))
            {
                string[] files = System.IO.Directory.GetFiles(folderPath, "*.png");
                System.Array.Sort(files, (a, b) => UnityEditor.EditorUtility.NaturalCompare(a, b));
                int count = Mathf.Min(expectedCount, files.Length);
                var list = new Sprite[count];
                for (int i = 0; i < count; i++)
                {
                    list[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(files[i]);
                }
                return list;
            }

            return new Sprite[0];
        }
#endif

        private sealed class Performer
        {
            private enum Motion { Idle, Dance, Miss }

            private readonly SpriteRenderer renderer;
            private readonly Color spriteColor;
            private readonly Sprite idle;
            private readonly Sprite[][] dances;
            private readonly Sprite[] miss;
            private int danceIndex;
            private readonly Vector3 basePosition;
            private readonly Quaternion baseRotation;
            private readonly Vector3 baseScale;
            private Motion motion;
            private int frame;
            private float frameTimer;
            private float motionTime;

            public Performer(SpriteRenderer target, Sprite idleSprite, Sprite[][] danceFrames, Sprite[] missFrames)
            {
                renderer = target;
                Color originalColor = target.color;
                spriteColor = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);
                // Remove renderers created by earlier versions when scripts reload in Play mode.
                Transform oldBlend = target.transform.Find("Frame blend");
                if (oldBlend != null) Object.Destroy(oldBlend.gameObject);
                oldBlend = target.transform.Find("Dance frame blend");
                if (oldBlend != null) Object.Destroy(oldBlend.gameObject);
                idle = idleSprite;
                dances = danceFrames;
                miss = missFrames;
                basePosition = target.transform.localPosition;
                baseRotation = target.transform.localRotation;
                baseScale = target.transform.localScale;
                renderer.sprite = idle;
            }

            public void PlayIdle() => ChangeMotion(Motion.Idle);
            public void PlayDance(int index)
            {
                danceIndex = index;
                ChangeMotion(Motion.Dance);
            }
            public void PlayMiss() => ChangeMotion(Motion.Miss);

            private void ChangeMotion(Motion next)
            {
                motion = next;
                frame = 0;
                frameTimer = 0f;
                motionTime = 0f;
                renderer.color = spriteColor;
                ShowFrame(next == Motion.Idle ? idle : next == Motion.Dance ? dances[danceIndex][0] : miss[0]);
            }

            public void Tick(float delta, float danceStep, float missStep)
            {
                motionTime += delta;
                if (motion != Motion.Idle)
                {
                    frameTimer += delta;
                    float step = motion == Motion.Dance ? danceStep : MissStep(missStep, frame, miss.Length);
                    // Show at most one drawing per rendered update. Catching up several
                    // sprite frames in one update makes the motion visibly jump.
                    if (frameTimer >= step)
                    {
                        frameTimer -= step;
                        frameTimer = Mathf.Min(frameTimer, step);
                        frame++;
                        if (motion == Motion.Dance)
                        {
                            frame %= dances[danceIndex].Length;
                            ShowFrame(dances[danceIndex][frame]);
                        }
                        else if (frame >= miss.Length)
                        {
                            PlayIdle();
                        }
                        else
                        {
                            ShowFrame(miss[frame]);
                        }
                    }
                }

                float x = 0f, y = 0f, degrees = 0f, squash = 0f;
                if (motion == Motion.Idle)
                {
                    // A quiet breathing pose makes the return from Miss visibly settle.
                    y = Mathf.Sin(motionTime * 2f) * 0.018f;
                    squash = Mathf.Sin(motionTime * 2f) * 0.003f;
                }
                else if (motion == Motion.Dance)
                {
                    float playbackFrame = frame + Mathf.Clamp01(frameTimer / danceStep);
                    float beat = playbackFrame * Mathf.PI * 2f / dances[danceIndex].Length;
                    x = Mathf.Sin(beat) * 0.07f;
                    y = Mathf.Abs(Mathf.Sin(beat * 2f)) * 0.09f;
                    degrees = Mathf.Sin(beat) * 1.5f;
                }
                else if (motion == Motion.Miss)
                {
                    float playbackFrame = frame + frameTimer / missStep;
                    float phase = Mathf.Clamp01(playbackFrame / (miss.Length + 0.5f));
                    x = PoseCurve(phase, 0f, 0.10f, 0.08f, 0f);
                    y = PoseCurve(phase, 0f, -0.08f, -0.16f, 0f);
                    degrees = PoseCurve(phase, 0f, -5f, -8f, 0f);
                }

                float smooth = 1f - Mathf.Exp(-13f * delta);
                Transform tr = renderer.transform;
                tr.localPosition = Vector3.Lerp(tr.localPosition, basePosition + new Vector3(x, y, 0f), smooth);
                tr.localRotation = Quaternion.Slerp(tr.localRotation, baseRotation * Quaternion.Euler(0f, 0f, degrees), smooth);
                tr.localScale = Vector3.Lerp(tr.localScale,
                    new Vector3(baseScale.x * (1f - squash), baseScale.y * (1f + squash), baseScale.z), smooth);
            }

            private void ShowFrame(Sprite next)
            {
                if (next == null) return;
                renderer.sprite = next;
            }

            private static float MissStep(float baseStep, int index, int length)
            {
                // A short settle reads clearly without freezing before idle.
                return baseStep * (index == length - 1 ? 1.5f : 1f);
            }

            private static float PoseCurve(float t, float start, float fall, float crouch, float stand)
            {
                if (t < 0.25f) return Mathf.Lerp(start, fall, Mathf.SmoothStep(0f, 1f, t / 0.25f));
                if (t < 0.55f) return Mathf.Lerp(fall, crouch, Mathf.SmoothStep(0f, 1f, (t - 0.25f) / 0.3f));
                return Mathf.Lerp(crouch, stand, Mathf.SmoothStep(0f, 1f, (t - 0.55f) / 0.45f));
            }
        }
    }
}
