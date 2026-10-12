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
        [Header("Refs")]
        public SpriteRenderer[] receptors;
        public InputHandler input;
        public JudgementSystem judgement;

        [Header("Visual Effects")]
        public ReceptorVisuals[] visuals;

        [Header("Fallback (nếu không dùng ReceptorVisuals)")]
        [Tooltip("Màu receptor khi đang giữ phím (nhấn).")]
        public Color pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

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

        void Awake()
        {
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
        }

        void OnDestroy()
        {
            if (judgement != null)
            {
                judgement.OnJudged -= OnJudged;
            }
        }

        void Update()
        {
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
            }
        }

        void OnJudged(Judgement j, int points, bool isHit)
        {
            int lane = GetLaneOfLastJudgement();
            if (lane >= 0 && lane < visuals.Length && visuals[lane] != null)
            {
                visuals[lane].PlayHitFeedback(j);
            }
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
    }
}
