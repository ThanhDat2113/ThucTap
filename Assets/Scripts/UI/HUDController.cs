using RhythmGame.Core;
using RhythmGame.Scoring;
using UnityEngine;
using UnityEngine.UI;

namespace RhythmGame.UI
{
    [DisallowMultipleComponent]
    public class HUDController : MonoBehaviour
    {
        private Canvas canvas;
        private Text scoreText;
        private Text comboText;
        private Text accText;
        private Text statsText;

        private float comboPopTimer;
        private const float PopDuration = 0.4f;

        private void Start()
        {
            Build();
            var sm = ScoreManager.Instance;
            if (sm != null)
            {
                sm.OnJudged += HandleJudged;
                Refresh(sm);
            }
        }

        private void OnDestroy()
        {
            if (ScoreManager.Instance != null)
                ScoreManager.Instance.OnJudged -= HandleJudged;
        }

        private void Build()
        {
            var canvasGO = new GameObject("HUD Canvas");
            canvasGO.transform.SetParent(transform, false);
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();

            Font font = SpriteFactory.Arial;

            scoreText = CreateLabel("Score", TextAnchor.UpperLeft, new Vector2(22, -22), new Vector2(400, 56), font, 30);
            accText = CreateLabel("Accuracy", TextAnchor.UpperRight, new Vector2(-22, -22), new Vector2(260, 56), font, 30);
            statsText = CreateLabel("Stats", TextAnchor.LowerLeft, new Vector2(22, 22), new Vector2(420, 52), font, 26);
            comboText = CreateLabel("Combo", TextAnchor.MiddleCenter, Vector2.zero, new Vector2(480, 120), font, 72);
            comboText.gameObject.SetActive(false);
        }

        private Text CreateLabel(string name, TextAnchor anchor, Vector2 anchored, Vector2 size, Font font, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();

            switch (anchor)
            {
                case TextAnchor.UpperLeft: rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); break;
                case TextAnchor.UpperRight: rt.anchorMin = rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1); break;
                case TextAnchor.LowerLeft: rt.anchorMin = rt.anchorMax = new Vector2(0, 0); rt.pivot = new Vector2(0, 0); break;
                default: rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); break;
            }
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;

            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = anchor;
            t.resizeTextForBestFit = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private void HandleJudged(Judgement judgement, float offset)
        {
            if (ScoreManager.Instance == null)
                return;

            var sm = ScoreManager.Instance;
            if (sm.Combo > 10 || judgement == Judgement.Perfect)
            {
                comboPopTimer = PopDuration;
                comboText.text = sm.Combo.ToString();
                comboText.color = ColorForCombo(judgement);
                comboText.gameObject.SetActive(true);
            }
            Refresh(sm);
        }

        private void Update()
        {
            if (comboPopTimer > 0f)
            {
                comboPopTimer -= Time.deltaTime;
                float scale = 1f + Mathf.Sin((comboPopTimer / PopDuration) * Mathf.PI) * 0.15f;
                comboText.rectTransform.localScale = new Vector3(scale, scale, scale);
                if (comboPopTimer <= 0f)
                {
                    comboText.gameObject.SetActive(false);
                    comboText.rectTransform.localScale = Vector3.one;
                }
            }

            if (ScoreManager.Instance != null)
                Refresh(ScoreManager.Instance);
        }

        private void Refresh(ScoreManager sm)
        {
            if (scoreText != null)
                scoreText.text = $"SCORE\n{sm.Score:N0}";
            if (accText != null)
                accText.text = $"ACC\n{sm.Accuracy * 100f:F2}%";
            if (statsText != null)
                statsText.text = $"PERFECT  {sm.PerfectCount}   GOOD  {sm.GoodCount}   MISS  {sm.MissCount}";
        }

        private static Color ColorForCombo(Judgement j)
        {
            switch (j)
            {
                case Judgement.Perfect: return new Color(0.3f, 1f, 0.6f);
                case Judgement.Good: return new Color(1f, 0.9f, 0.3f);
                default: return new Color(1f, 0.4f, 0.4f);
            }
        }
    }
}
