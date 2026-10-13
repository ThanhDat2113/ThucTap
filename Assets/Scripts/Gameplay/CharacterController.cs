using UnityEngine;

namespace RhythmGame
{
    [RequireComponent(typeof(Animator))]
    public class CharacterController : MonoBehaviour
    {
        [Header("Refs")]
        public JudgementSystem judgementSystem;

        [Header("Animation Parameters")]
        [Tooltip("Tên parameter bool: true = Idle, false = Dance (loop)")]
        public string danceBool = "IsDancing";
        [Tooltip("Tùy chọn: Trigger để bắt đầu dance (nếu Animator cần)")]
        public string danceTrigger = "";

        Animator animator;
        bool isDancing;

        void Awake()
        {
            animator = GetComponent<Animator>();
        }

        void Start()
        {
            if (judgementSystem != null)
            {
                judgementSystem.OnJudged += OnJudged;
            }
            SetDancing(false);
        }

        void OnDestroy()
        {
            if (judgementSystem != null)
            {
                judgementSystem.OnJudged -= OnJudged;
            }
        }

        void OnJudged(Judgement j, int points, bool isHit)
        {
            if (!isDancing && isHit && (j == Judgement.Perfect || j == Judgement.Good || j == Judgement.Bad || j == Judgement.HoldComplete))
            {
                // Chưa dance -> bắt đầu dance
                SetDancing(true);
            }
            else if (isDancing && j == Judgement.Miss)
            {
                // Đang dance -> miss -> về idle
                SetDancing(false);
            }
            // Đang dance + hit note -> không làm gì (tiếp tục dance loop)
        }

        void SetDancing(bool dancing)
        {
            isDancing = dancing;

            if (!string.IsNullOrEmpty(danceBool))
            {
                animator.SetBool(danceBool, dancing);
            }

            if (dancing && !string.IsNullOrEmpty(danceTrigger))
            {
                animator.SetTrigger(danceTrigger);
            }
        }

        public void ForceIdle()
        {
            SetDancing(false);
        }
    }
}