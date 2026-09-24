using UnityEngine;

namespace RhythmGame.Core
{
    public static class SpriteFactory
    {
        public static Sprite WhiteSquare { get; } = CreateWhiteSquare();

        private static Sprite CreateWhiteSquare()
        {
            Texture2D tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Font Arial => arial ?? (arial = Resources.GetBuiltinResource<Font>("Arial.ttf"));
        private static Font arial;
    }
}
