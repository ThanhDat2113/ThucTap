using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Luan.MainMenu.Editor
{
    public static class QuickMenuSceneBuilder
    {
        [MenuItem("Tools/Luan/Build MainMenu Quick Menu")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before rebuilding the MainMenu Quick Menu.");
                return;
            }
            const string path = "Assets/Luan/Scenes/MainMenu.unity";
            ImportAtlas();
            if (EditorSceneManager.GetActiveScene().path != path)
                EditorSceneManager.OpenScene(path);
            var menu = Object.FindFirstObjectByType<QuickMenu>();
            if (menu == null)
            {
                Debug.LogError("QuickMenu component is missing from MainmenuManager.");
                return;
            }
            menu.CreateInScene();
            var root = GameObject.Find("QuickMenu").transform;
            Wire(root.Find("ButtonMucLuc"), menu.Toggle);
            Wire(root.Find("Shop"), menu.OpenShop);
            Wire(root.Find("LuckyWheel"), menu.OpenLuckyWheel);
            Wire(root.Find("Chọn Nhân Vật"), menu.OpenCharacter);
            Wire(root.Find("Điểm danh hằng ngày"), menu.OpenDaily);

            var mainMenuMgr = Object.FindFirstObjectByType<MainMenuManager>();
            if (mainMenuMgr != null)
            {
                var anim = root.GetComponent<UIPanelAnimator>();
                var so = new SerializedObject(mainMenuMgr);
                var prop = so.FindProperty("quickMenuPanel");
                if (prop != null)
                {
                    prop.objectReferenceValue = anim;
                    so.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("MainMenu QuickMenu hierarchy and sprites saved in scene.");
        }

        private static void ImportAtlas()
        {
            const string path = "Assets/Luan/MainMenu/Resources/QuickMenu/MucLuc.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("Cannot import MucLuc.png sprite sheet.");
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.spritesheet = new[]
            {
                Slice("Shop", 0, 512, 512, 512),
                Slice("Menu", 512, 512, 512, 512),
                Slice("Character", 1024, 512, 512, 512),
                Slice("LuckyWheel", 0, 0, 512, 512),
                Slice("Daily", 512, 0, 512, 512),
                Slice("Dot", 1024, 0, 512, 512),
            };
            importer.SaveAndReimport();
        }

        private static SpriteMetaData Slice(string name, float x, float y, float width, float height)
        {
            return new SpriteMetaData
            {
                name = name,
                rect = new Rect(x, y, width, height),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(.5f, .5f),
            };
        }
        private static void Wire(Transform target, UnityAction action)
        {
            var button = target.GetComponent<Button>();
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, action);
            EditorUtility.SetDirty(button);
        }
    }
}
