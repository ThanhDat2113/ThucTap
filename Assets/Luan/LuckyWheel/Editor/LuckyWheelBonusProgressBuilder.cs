using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Luan.LuckyWheel.Editor
{
    public static class LuckyWheelBonusProgressBuilder
    {
        [MenuItem("Tools/Luan/Fix Lucky Wheel Bonus Progress Fill")]
        public static void Fix()
        {
            const string scenePath = "Assets/Luan/Scenes/LuckyWheel.unity";
            const string spritePath = "Assets/Luan/LuckyWheel/Sprites/BonusProgressFill_Neon.png";
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Stop Play Mode before editing the bonus progress UI.");
                return;
            }
            if (EditorSceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath);

            var panel = GameObject.Find("LuckyWheelCanvas")?.transform.Find("Footer/BonusProgress") as RectTransform;
            var fill = panel?.Find("EditableContent/ProgressFill")?.GetComponent<Image>();
            if (panel == null || fill == null)
            {
                Debug.LogError("Không tìm thấy BonusProgress/EditableContent/ProgressFill.");
                return;
            }

            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                Debug.LogError("Không nạp được BonusProgressFill_Neon.png.");
                return;
            }

            fill.sprite = sprite;
            fill.color = Color.white;
            fill.preserveAspect = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            var rect = fill.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(0f, .5f);
            rect.anchoredPosition = new Vector2(-panel.rect.width * .305f, -panel.rect.height * .13f);
            rect.sizeDelta = new Vector2(panel.rect.width * .535f, panel.rect.height * 1.5f);

            var data = panel.GetComponentInParent<LuckyWheelEditablePanels>();
            if (data != null)
            {
                data.Refresh();
                EditorUtility.SetDirty(data);
            }
            EditorUtility.SetDirty(fill);
            EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);
            EditorSceneManager.SaveScene(panel.gameObject.scene);
            Debug.Log("Đã căn ProgressFill vào đúng rãnh và thay bằng sprite neon riêng.");
        }
    }
}
