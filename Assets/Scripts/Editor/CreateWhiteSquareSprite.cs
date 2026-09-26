#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RhythmGame.EditorTools
{
    /// <summary>
    /// Tạo 1 file PNG ô vuông trắng làm sprite asset thật (không phải texture
    /// sinh tạm lúc chạy), để bạn gắn vào Note prefab, receptor, lane background
    /// và chỉnh sửa (đổi màu, đổi ảnh khác) ngay trong Editor.
    /// Dùng: menu Tools > Rhythm Game > Create White Square Sprite.
    /// </summary>
    public static class CreateWhiteSquareSprite
    {
        [MenuItem("Tools/Rhythm Game/Create White Square Sprite")]
        public static void Create()
        {
            const string folder = "Assets/Sprites";
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string path = folder + "/WhiteSquare.png";
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels32(pixels);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();

            var asset = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
            Debug.Log("Da tao sprite tai " + path);
        }
    }
}
#endif
