using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Luan.Dialogue;

namespace Luan.Dialogue.Editor
{
    [InitializeOnLoad]
    public static class StoryDialogueSceneAutoBuilder
    {
        static StoryDialogueSceneAutoBuilder()
        {
            EditorApplication.delayCall += CheckAndBuild;
        }

        private static void CheckAndBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            var activeScene = EditorSceneManager.GetActiveScene();
            if (activeScene.path == "Assets/Scenes/GameplayScene.unity")
            {
                var canvas = GameObject.Find("StoryDialogueCanvas");
                if (canvas != null && canvas.transform.Find("IntroContainer") == null)
                {
                    Debug.Log("<color=#00FF99>[StoryDialogueSceneAutoBuilder] Phát hiện chưa có IntroContainer trong GameplayScene, tự động build...</color>");
                    StoryDialogueSceneBuilder.Build();
                }
            }
        }
    }

    public static class StoryDialogueSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/GameplayScene.unity";
        private const string SheetPath = "Assets/Luan/DiaLogTalkChapter/SpriteAssets/Neon Cyberpunk Dialogue UI Sprite Sheet.png";
        private const string Main1AvatarPath = "Assets/Luan/DiaLogTalkChapter/Avatars/avatar_main1.png";
        private const string Boss1AvatarPath = "Assets/Luan/DiaLogTalkChapter/Avatars/avatar_boss1.png";

        [MenuItem("Tools/Luan/Build Story Dialogue in GameplayScene")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[StoryDialogueSceneBuilder] Dung Play Mode truoc khi build Story Dialogue UI.");
                return;
            }

            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            // Load sprites
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(SheetPath);
            Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
            foreach (var a in subAssets)
            {
                if (a is Sprite s) sprites[s.name] = s;
            }

            Sprite boxSpr = sprites.ContainsKey("Neon Cyberpunk Dialogue UI Sprite Sheet_1") ? sprites["Neon Cyberpunk Dialogue UI Sprite Sheet_1"] : null;
            Sprite frameSpr = sprites.ContainsKey("Neon Cyberpunk Dialogue UI Sprite Sheet_0") ? sprites["Neon Cyberpunk Dialogue UI Sprite Sheet_0"] : null;
            Sprite skipSpr = sprites.ContainsKey("Neon Cyberpunk Dialogue UI Sprite Sheet_2") ? sprites["Neon Cyberpunk Dialogue UI Sprite Sheet_2"] : null;
            Sprite autoSpr = sprites.ContainsKey("Neon Cyberpunk Dialogue UI Sprite Sheet_5") ? sprites["Neon Cyberpunk Dialogue UI Sprite Sheet_5"] : null;
            Sprite nameTagSpr = sprites.ContainsKey("Neon Cyberpunk Dialogue UI Sprite Sheet_9") ? sprites["Neon Cyberpunk Dialogue UI Sprite Sheet_9"] : null;
            Sprite nextSpr = sprites.ContainsKey("Neon Cyberpunk Dialogue UI Sprite Sheet_10") ? sprites["Neon Cyberpunk Dialogue UI Sprite Sheet_10"] : null;

            Sprite main1Avatar = AssetDatabase.LoadAssetAtPath<Sprite>(Main1AvatarPath);
            Sprite boss1Avatar = AssetDatabase.LoadAssetAtPath<Sprite>(Boss1AvatarPath);

            // Tao hoac lay StoryDialogueManager trong scene
            var managerGo = GameObject.Find("[StoryDialogueManager]");
            if (managerGo == null)
            {
                managerGo = new GameObject("[StoryDialogueManager]");
            }

            var manager = managerGo.GetComponent<StoryDialogueManager>();
            if (manager == null)
            {
                manager = managerGo.AddComponent<StoryDialogueManager>();
            }

            // Gan avatar cho manager
            var so = new SerializedObject(manager);
            var playerProp = so.FindProperty("playerAvatarSprite");
            if (playerProp != null) playerProp.objectReferenceValue = main1Avatar;
            var oppProp = so.FindProperty("opponentAvatarSprite");
            if (oppProp != null) oppProp.objectReferenceValue = boss1Avatar;
            so.ApplyModifiedProperties();

            // Build UI Intro + 2 Speaker Dialogue
            manager.BuildUIGameObjects(boxSpr, frameSpr, skipSpr, autoSpr, nameTagSpr, nextSpr);

            // Tao hoac load Chapter1_Dialogue & Chapter2_Dialogue assets
            string chap1Path = "Assets/Luan/DiaLogTalkChapter/Data/Chapter1_Dialogue.asset";
            var chap1Asset = AssetDatabase.LoadAssetAtPath<DialogueStoryData>(chap1Path);
            if (chap1Asset == null)
            {
                chap1Asset = ScriptableObject.CreateInstance<DialogueStoryData>();
                chap1Asset.chapterNumber = 1;
                chap1Asset.chapterTitle = "Chapter 1: Concrete Dreams";
                chap1Asset.characterStoryTitle = "Câu chuyện của Raze";
                chap1Asset.characterIntroArt = boss1Avatar;
                AssetDatabase.CreateAsset(chap1Asset, chap1Path);
                AssetDatabase.SaveAssets();
            }
            else
            {
                if (chap1Asset.characterIntroArt == null)
                {
                    chap1Asset.characterIntroArt = boss1Avatar;
                    EditorUtility.SetDirty(chap1Asset);
                }
            }

            string chap2Path = "Assets/Luan/DiaLogTalkChapter/Data/Chapter2_Dialogue.asset";
            var chap2Asset = AssetDatabase.LoadAssetAtPath<DialogueStoryData>(chap2Path);
            Sprite boss2Avatar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Luan/DiaLogTalkChapter/Avatars/avatar_boss2.png");

            manager.DialogueDataAsset = chap1Asset;

            // Cấu hình Multi-Chapter
            manager.Chapters.Clear();
            manager.Chapters.Add(new ChapterDialogueEntry
            {
                chapterNumber = 1,
                chapterTitle = "Concrete Dreams",
                dialogueAsset = chap1Asset,
                opponentAvatar = boss1Avatar,
                defaultOpponentName = "Raze"
            });
            manager.Chapters.Add(new ChapterDialogueEntry
            {
                chapterNumber = 2,
                chapterTitle = "Higher Together",
                dialogueAsset = chap2Asset,
                opponentAvatar = boss2Avatar,
                defaultOpponentName = "DJ NULL"
            });

            EditorUtility.SetDirty(manager);

            // Save scene
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#00FF99>[StoryDialogueSceneBuilder] Da build thanh cong Story Dialogue UI (Intro + Pre/Post Battle) vao GameplayScene!</color>");
        }
    }
}
