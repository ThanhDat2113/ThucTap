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
        [SerializeField] private Sprite[] riffMiss;
        [SerializeField] private Sprite[] lumiMiss;

        [Header("Timing")]
        [SerializeField, Min(0.06f)] private float danceFrameSeconds = 0.16f;
        [SerializeField, Min(0.08f)] private float missFrameSeconds = 0.22f;
        [SerializeField, Range(0f, 0.14f)] private float frameBlendSeconds = 0f;

        private Performer riff;
        private Performer lumi;
        private int selected;
        private GUIStyle hudStyle;

        private void Awake()
        {
            if (riffRenderer == null || lumiRenderer == null ||
                riffDance == null || riffDance.Length == 0 ||
                lumiDance == null || lumiDance.Length == 0 ||
                riffDance2 == null || riffDance2.Length == 0 ||
                lumiDance2 == null || lumiDance2.Length == 0 ||
                riffMiss == null || riffMiss.Length == 0 ||
                lumiMiss == null || lumiMiss.Length == 0)
            {
                Debug.LogError("CharacterAnimationTest: missing renderer or animation frames.", this);
                enabled = false;
                return;
            }

            riff = new Performer(riffRenderer, riffIdle, riffDance, riffDance2, riffMiss);
            lumi = new Performer(lumiRenderer, lumiIdle, lumiDance, lumiDance2, lumiMiss);
            SelectDance(0, 0); // Play starts with Riff's first dance.
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame))
                SelectDance(0, 0);
            else if (keyboard != null && (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame))
                SelectDance(1, 0);
            else if (keyboard != null && (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame))
                SelectDance(0, 1);
            else if (keyboard != null && (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame))
                SelectDance(1, 1);
            else if (keyboard != null && keyboard.mKey.wasPressedThisFrame)
                (selected == 0 ? riff : lumi).PlayMiss();

            float delta = Time.deltaTime;
            riff.Tick(delta, danceFrameSeconds, missFrameSeconds, frameBlendSeconds);
            lumi.Tick(delta, danceFrameSeconds, missFrameSeconds, frameBlendSeconds);
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
                    fontSize = 21,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true
                };
                hudStyle.normal.textColor = new Color(0.94f, 0.96f, 1f);
            }

            GUI.Label(new Rect(0, 16, Screen.width, 60),
                "1: Riff Dance 1   2: Lumi Dance 1   3: Riff Dance 2   4: Lumi Dance 2   M: Miss", hudStyle);
            GUI.Label(new Rect(0, Screen.height - 54, Screen.width, 40),
                selected == 0 ? "Riff đang được chọn" : "Lumi đang được chọn", hudStyle);
        }

        private sealed class Performer
        {
            private enum Motion { Idle, Dance, Miss, Recover }

            private readonly SpriteRenderer renderer;
            private readonly SpriteRenderer blendRenderer;
            private readonly Sprite idle;
            private readonly Sprite[][] dances;
            private readonly Sprite[] miss;
            private int danceIndex;
            private readonly Vector3 basePosition;
            private readonly Quaternion baseRotation;
            private readonly Vector3 baseScale;
            private readonly Color baseColor;
            private Motion motion;
            private int frame;
            private float frameTimer;
            private float motionTime;
            private float blendTime;
            private bool blending;

            public Performer(SpriteRenderer target, Sprite idleSprite, Sprite[] dance1Frames, Sprite[] dance2Frames, Sprite[] missFrames)
            {
                renderer = target;
                idle = idleSprite;
                dances = new[] { dance1Frames, dance2Frames };
                miss = missFrames;
                basePosition = target.transform.localPosition;
                baseRotation = target.transform.localRotation;
                baseScale = target.transform.localScale;
                baseColor = target.color;

                GameObject overlay = new GameObject("Frame blend");
                overlay.transform.SetParent(target.transform, false);
                blendRenderer = overlay.AddComponent<SpriteRenderer>();
                blendRenderer.sharedMaterial = target.sharedMaterial;
                blendRenderer.sortingLayerID = target.sortingLayerID;
                blendRenderer.sortingOrder = target.sortingOrder + 1;
                blendRenderer.enabled = false;
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
                ShowFrame(next == Motion.Idle ? idle : next == Motion.Dance ? dances[danceIndex][0] :
                    next == Motion.Miss ? miss[0] : miss[miss.Length - 2]);
            }

            public void Tick(float delta, float danceStep, float missStep, float blendDuration)
            {
                motionTime += delta;
                if (motion != Motion.Idle)
                {
                    frameTimer += delta;
                    float step = motion == Motion.Dance ? danceStep : missStep;
                    while (frameTimer >= step)
                    {
                        frameTimer -= step;
                        frame++;
                        if (motion == Motion.Dance)
                        {
                            frame %= dances[danceIndex].Length;
                            ShowFrame(dances[danceIndex][frame]);
                        }
                        else if (frame >= miss.Length)
                        {
                            ChangeMotion(Motion.Recover);
                            break;
                        }
                        else if (motion == Motion.Recover)
                        {
                            if (frame >= miss.Length - 1)
                            {
                                PlayIdle();
                                break;
                            }
                            ShowFrame(miss[miss.Length - 2 - frame]);
                        }
                        else
                        {
                            ShowFrame(miss[frame]);
                        }
                    }
                }

                if (blending)
                {
                    blendTime += delta;
                    float t = blendDuration <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(blendTime / blendDuration));
                    renderer.color = WithAlpha(baseColor, 1f - t);
                    blendRenderer.color = WithAlpha(baseColor, t);
                    if (t >= 1f)
                    {
                        renderer.sprite = blendRenderer.sprite;
                        renderer.color = baseColor;
                        blendRenderer.enabled = false;
                        blending = false;
                    }
                }

                float x = 0f, y = 0f, degrees = 0f, squash = 0f;
                if (motion == Motion.Idle)
                {
                    // A quiet breathing pose makes the return from Miss visibly settle.
                    y = Mathf.Sin(motionTime * 2f) * 0.018f;
                    squash = Mathf.Sin(motionTime * 2f) * 0.007f;
                }
                else if (motion == Motion.Dance)
                {
                    float beat = motionTime * Mathf.PI * 2f / (danceStep * dances[danceIndex].Length);
                    x = Mathf.Sin(beat) * 0.07f;
                    y = Mathf.Abs(Mathf.Sin(beat * 2f)) * 0.09f;
                    degrees = Mathf.Sin(beat) * 3.5f;
                    squash = Mathf.Sin(beat * 2f) * 0.025f;
                }
                else if (motion == Motion.Miss || motion == Motion.Recover)
                {
                    float phase = Mathf.Clamp01(motionTime / (missStep * miss.Length));
                    if (motion == Motion.Recover) phase = 1f - phase;
                    x = Mathf.Sin(phase * Mathf.PI) * 0.12f;
                    y = -Mathf.Sin(phase * Mathf.PI) * 0.16f;
                    degrees = -Mathf.Sin(phase * Mathf.PI) * 9f;
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
                if (next == null || (!blending && renderer.sprite == next)) return;
                if (blending)
                {
                    renderer.sprite = blendRenderer.sprite;
                    renderer.color = baseColor;
                }
                blendRenderer.sprite = next;
                blendRenderer.color = WithAlpha(baseColor, 0f);
                blendRenderer.enabled = true;
                blendTime = 0f;
                blending = true;
            }

            private static Color WithAlpha(Color color, float alpha)
            {
                color.a *= alpha;
                return color;
            }
        }
    }
}
