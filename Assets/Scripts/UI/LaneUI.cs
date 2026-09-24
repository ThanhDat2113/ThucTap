using RhythmGame.Core;
using UnityEngine;

namespace RhythmGame.UI
{
    [DisallowMultipleComponent]
    public class LaneUI : MonoBehaviour
    {
        [Header("Lane Background")]
        public Color laneColor = new Color(0f, 0f, 0f, 0.25f);
        public Color hitLineColor = new Color(1f, 1f, 1f, 0.85f);
        public Color laneSeparatorColor = new Color(1f, 1f, 1f, 0.12f);

        [Header("Sizing")]
        public float hitLineThickness = 0.16f;

        private SpriteRenderer[] laneRenderers;
        private SpriteRenderer hitLineRenderer;
        private SpriteRenderer[] separators;

        private void Start()
        {
            if (GameManager.Instance == null || GameManager.Instance.Config == null)
                return;
            Build();
        }

        private void Build()
        {
            var cfg = GameManager.Instance.Config;
            int lanes = cfg.laneCount;

            laneRenderers = new SpriteRenderer[lanes];
            separators = new SpriteRenderer[lanes + 1];

            float centerY = (cfg.spawnPositionY + cfg.hitPositionY) * 0.5f;
            float travelHeight = Mathf.Max(0.01f, cfg.spawnPositionY - cfg.hitPositionY);

            for (int i = 0; i < lanes; i++)
            {
                var go = new GameObject($"Lane_{i}");
                go.transform.SetParent(transform, true);
                float x = cfg.GetLaneCenterX((LaneID)i);
                go.transform.localPosition = new Vector3(x, centerY, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.WhiteSquare;
                sr.size = new Vector2(cfg.laneWidth, travelHeight);
                sr.color = laneColor;
                sr.sortingOrder = 0;
                laneRenderers[i] = sr;
            }

            for (int i = 0; i <= lanes; i++)
            {
                float x = cfg.laneStartX + i * cfg.laneWidth;
                var go = new GameObject($"Separator_{i}");
                go.transform.SetParent(transform, true);
                go.transform.localPosition = new Vector3(x, centerY, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.WhiteSquare;
                sr.size = new Vector2(hitLineThickness * 0.5f, travelHeight);
                sr.color = laneSeparatorColor;
                sr.sortingOrder = 0;
                separators[i] = sr;
            }

            var hitGO = new GameObject("HitLine");
            hitGO.transform.SetParent(transform, true);
            hitGO.transform.localPosition = new Vector3(0f, cfg.hitPositionY, 0f);
            hitLineRenderer = hitGO.AddComponent<SpriteRenderer>();
            hitLineRenderer.sprite = SpriteFactory.WhiteSquare;
            hitLineRenderer.size = new Vector2(lanes * cfg.laneWidth, hitLineThickness);
            hitLineRenderer.color = hitLineColor;
            hitLineRenderer.sortingOrder = 1;
        }
    }
}
