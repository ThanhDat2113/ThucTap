using RhythmGame.Core;
using RhythmGame.Data;
using UnityEngine;

namespace RhythmGame.Notes
{
    [DisallowMultipleComponent]
    public class Note : MonoBehaviour
    {
        private const int HeadSortingOrder = 5;
        private const int BodySortingOrder = 4;
        private const float FlashDuration = 0.12f;

        [Header("Visuals")]
        [SerializeField] private Color[] laneColors = new Color[0];

        private GameConfig config;
        private SpriteRenderer headRenderer;
        private SpriteRenderer bodyRenderer;
        private Transform bodyTransform;

        public LaneID Lane { get; private set; }
        public float HitTime { get; private set; }
        public NoteType Type { get; private set; }
        public float HoldDuration { get; private set; }
        public bool IsHit { get; private set; }
        public bool IsMissed { get; private set; }
        public bool IsHoldHeadHit { get; private set; }
        public bool IsTailCleared { get; set; }

        public System.Action<Note> OnMissed;

        private Color baseHeadColor;
        private Color flashColor;
        private float flashEndTime;

        private void Awake()
        {
            EnsureVisuals();
        }

        private void EnsureVisuals()
        {
            var headGO = new GameObject("Head", typeof(SpriteRenderer));
            headGO.transform.SetParent(transform, false);
            headRenderer = headGO.GetComponent<SpriteRenderer>();
            headRenderer.sprite = SpriteFactory.WhiteSquare;
            headRenderer.sortingOrder = HeadSortingOrder;

            var bodyGO = new GameObject("Body", typeof(SpriteRenderer));
            bodyTransform = bodyGO.transform;
            bodyTransform.SetParent(transform, false);
            bodyRenderer = bodyGO.GetComponent<SpriteRenderer>();
            bodyRenderer.sprite = SpriteFactory.WhiteSquare;
            bodyRenderer.sortingOrder = BodySortingOrder;
            bodyRenderer.enabled = false;
        }

        public void Setup(NoteData data, GameConfig gameConfig)
        {
            config = gameConfig;
            Lane = data.lane;
            HitTime = data.hitTime;
            Type = data.type;
            HoldDuration = data.holdDuration;

            baseHeadColor = ResolveLaneColor(Lane);
            headRenderer.color = baseHeadColor;

            bodyRenderer.enabled = Type == NoteType.Hold;
        }

        private Color ResolveLaneColor(LaneID lane)
        {
            int index = (int)lane;
            if (laneColors != null && index < laneColors.Length && laneColors[index] != default(Color))
                return laneColors[index];

            switch (lane)
            {
                case LaneID.Left: return new Color(1f, 0.35f, 0.6f);
                case LaneID.Down: return new Color(0.15f, 1f, 0.4f);
                case LaneID.Up: return new Color(1f, 0.9f, 0.25f);
                case LaneID.Right: return new Color(0.3f, 0.7f, 1f);
                default: return Color.white;
            }
        }

        private void Update()
        {
            if (config == null || Conductor.Instance == null)
                return;

            float songPos = Conductor.Instance.CurrentSongPosition;
            float remaining = HitTime - songPos;
            float y = config.hitPositionY + remaining * config.noteSpeed;

            float laneX = config.GetLaneCenterX(Lane);
            transform.position = new Vector3(laneX, y, 0f);

            UpdateBody(y);
            UpdateFlash();

            if (!IsHit && !IsMissed && !IsHoldHeadHit && songPos > HitTime + config.goodWindow)
            {
                ReportMiss();
            }
        }

        private void UpdateBody(float headY)
        {
            if (Type != NoteType.Hold || bodyRenderer == null)
                return;

            bodyRenderer.enabled = true;
            float songPos = Conductor.Instance.CurrentSongPosition;
            float tailTime = HitTime + HoldDuration;
            float tailY = config.hitPositionY + (tailTime - songPos) * config.noteSpeed;

            float length = Mathf.Max(0.02f, headY - tailY);
            bodyTransform.localPosition = new Vector3(0f, -length * 0.5f, 0f);
            bodyTransform.localScale = new Vector3(config.laneWidth * 0.45f, length, 1f);
        }

        private void UpdateFlash()
        {
            if (Time.time < flashEndTime)
            {
                float t = (flashEndTime - Time.time) / FlashDuration;
                headRenderer.color = Color.Lerp(flashColor, baseHeadColor, t);
            }
            else if (headRenderer.color != baseHeadColor)
            {
                headRenderer.color = baseHeadColor;
            }
        }

        private static Color FlashColorFor(Judgement judgement)
        {
            switch (judgement)
            {
                case Judgement.Perfect: return new Color(0.5f, 1f, 0.7f);
                case Judgement.Good: return new Color(1f, 0.9f, 0.4f);
                case Judgement.Miss: return new Color(1f, 0.3f, 0.3f);
                default: return Color.white;
            }
        }

        public void ApplyHeadJudgement(Judgement judgement, float offset)
        {
            if (IsMissed)
                return;

            IsHit = true;
            if (Type == NoteType.Hold)
                IsHoldHeadHit = true;

            flashColor = FlashColorFor(judgement);
            flashEndTime = Time.time + FlashDuration;

            if (Type == NoteType.Normal)
                Destroy(gameObject, FlashDuration);
        }

        public void ClearHoldTail()
        {
            IsTailCleared = true;
            flashColor = FlashColorFor(Judgement.Perfect);
            flashEndTime = Time.time + FlashDuration;
        }

        private void ReportMiss()
        {
            IsMissed = true;
            OnMissed?.Invoke(this);
            flashColor = FlashColorFor(Judgement.Miss);
            if (Type == NoteType.Normal)
                Destroy(gameObject, FlashDuration);
        }

        public void Kill()
        {
            Destroy(gameObject);
        }
    }
}
