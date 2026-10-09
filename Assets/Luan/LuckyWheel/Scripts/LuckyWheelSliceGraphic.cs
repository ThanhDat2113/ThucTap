using UnityEngine;
using UnityEngine.UI;

namespace Luan.LuckyWheel
{
    /// <summary>Draws one sector (or the fixed rim) from the original wheel sprite.</summary>
    [ExecuteAlways]
    public sealed class LuckyWheelSliceGraphic : MaskableGraphic
    {
        [SerializeField] private Sprite sourceSprite;
        [SerializeField, Range(0, 11)] private int sectorIndex;
        [SerializeField] private bool drawRim;
        [SerializeField, Range(0f, 1f)] private float rimInnerRadius = 0.90f;

        public override Texture mainTexture => sourceSprite != null ? sourceSprite.texture : Texture2D.whiteTexture;

        public void Configure(Sprite sprite, int index, bool rim)
        {
            sourceSprite = sprite;
            sectorIndex = index;
            drawRim = rim;
            raycastTarget = false;
            SetVerticesDirty();
            SetMaterialDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (sourceSprite == null) return;

            var rect = GetPixelAdjustedRect();
            var center = rect.center;
            var radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            var firstAngle = drawRim ? 0f : 105f - sectorIndex * 30f;
            var sweep = drawRim ? 360f : -30f;
            var steps = drawRim ? 144 : 16;
            var innerRadius = drawRim ? rimInnerRadius : 0f;
            var texture = sourceSprite.texture;
            var textureRect = sourceSprite.textureRect;

            for (var step = 0; step <= steps; step++)
            {
                var angle = (firstAngle + sweep * step / steps) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddVertex(mesh, center + direction * radius * innerRadius, direction * innerRadius, textureRect, texture);
                AddVertex(mesh, center + direction * radius, direction, textureRect, texture);
                if (step == 0) continue;
                var vertex = step * 2;
                mesh.AddTriangle(vertex - 2, vertex - 1, vertex);
                mesh.AddTriangle(vertex - 1, vertex + 1, vertex);
            }
        }

        private void AddVertex(VertexHelper mesh, Vector2 position, Vector2 unitDirection, Rect textureRect, Texture texture)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertex.uv0 = new Vector2(
                (textureRect.x + (unitDirection.x + 1f) * 0.5f * textureRect.width) / texture.width,
                (textureRect.y + (unitDirection.y + 1f) * 0.5f * textureRect.height) / texture.height);
            mesh.AddVert(vertex);
        }
    }
}
