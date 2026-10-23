using UnityEngine;

namespace RhythmGame
{
    [RequireComponent(typeof(Animator))]
    public class CharacterController : MonoBehaviour
    {
    [Header("Refs")]
    public JudgementSystem judgementSystem;
    public GameManager gameManager;

        [Header("Animation Parameters")]
        [Tooltip("Tên parameter bool: true = Idle, false = Dance (loop)")]
        public string danceBool = "IsDancing";
        [Tooltip("Tùy chọn: Trigger để bắt đầu dance (nếu Animator cần)")]
        public string danceTrigger = "";

        [Header("Mode")]
        [Tooltip("Chỉ nhảy khi note của owner này được hit")]
        public NoteOwner owner = NoteOwner.Player;

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

        void Update()
        {
        }
        void OnDestroy()
        {
            if (judgementSystem != null)
            {
                judgementSystem.OnJudged -= OnJudged;
            }
        }

        void OnJudged(Judgement j, int points, bool isHit, NoteOwner noteOwner)
        {
            if (noteOwner != owner) return;

            if (!isDancing && isHit && (j == Judgement.Perfect || j == Judgement.Good || j == Judgement.Bad || j == Judgement.HoldComplete))
            {
                SetDancing(true);
            }
            else if (isDancing && j == Judgement.Miss)
            {
                SetDancing(false);
            }
        }

        public void TriggerDance()
        {
            if (!isDancing)
            {
                SetDancing(true);
            }
        }

        public void SetIdle()
        {
            SetDancing(false);
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