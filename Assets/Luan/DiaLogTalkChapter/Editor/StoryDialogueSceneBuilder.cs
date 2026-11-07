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

            ConfigureChapter1Backstory(chap1Asset, boss1Avatar, main1Avatar);

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

        public static void ConfigureChapter1Backstory(DialogueStoryData chap1Asset, Sprite boss1Avatar, Sprite main1Avatar)
        {
            if (chap1Asset == null) return;

            string dir = "Assets/Luan/DiaLogTalkChapter/Backstory/";
            Sprite act1 = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "chapter1_act1_story_melodia.png");
            Sprite act2 = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "chapter1_act2_story_raze.png");
            Sprite act3 = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "chapter1_act3_story_crown.png");
            Sprite act4 = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "chapter1_act4_dialogue_who_are_you.png");
            Sprite act5 = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "chapter1_act5_dialogue_im_raze.png");
            Sprite act6 = AssetDatabase.LoadAssetAtPath<Sprite>(dir + "chapter1_act6_dialogue_showdown.png");

            chap1Asset.characterStoryTitle = "Câu chuyện của Raze";
            chap1Asset.characterStory = "Raze lớn lên tại khu phố phía Tây. Khi còn nhỏ, anh thường bị đuổi khỏi những sân khấu lớn vì không được đào tạo bài bản.\n\nRaze cùng những người bạn biến một quảng trường bỏ hoang thành nơi biểu diễn. Anh phủ kín các bức tường bằng graffiti, tổ chức những cuộc dance battle và dần khiến khu vực này trở nên nổi tiếng.\n\nSau nhiều năm, Raze trở thành nhà vô địch của khu phố. Biểu tượng vương miện trên trang phục là lời nhắc rằng không ai được phép xem thường anh thêm lần nữa. Tuy nhiên, Raze ngày càng sợ mất đi vị trí mình đã rất vất vả mới giành được. Anh bắt đầu thách đấu và đuổi đi bất kỳ ai muốn bước qua khu phố.";
            if (boss1Avatar != null) chap1Asset.characterIntroArt = boss1Avatar;

            chap1Asset.backstorySlides.Clear();
            chap1Asset.backstorySlides.Add(new BackstorySlide
            {
                illustration = act1,
                speakerName = "DẪN CHUYỆN",
                nameColor = new Color(0.2f, 0.95f, 1f, 1f),
                actionDescription = "[Nhân vật chính đeo tai nghe, nhìn về thành phố Melodia]",
                storyText = "Melodia tổ chức giải đấu đường phố Bắt Lấy Giai Điệu. Một vũ công trẻ quyết định dấn thân với một đôi giày, một chiếc máy nghe nhạc và một cơ hội duy nhất."
            });
            chap1Asset.backstorySlides.Add(new BackstorySlide
            {
                illustration = act2,
                speakerName = "DẪN CHUYỆN",
                nameColor = new Color(1f, 0.85f, 0.2f, 1f),
                actionDescription = "[Raze nhảy breakdance, phủ kín quảng trường bỏ hoang bằng graffiti]",
                storyText = "Từng bị đuổi khỏi các sân khấu lớn, Raze cùng bạn bè biến quảng trường hoang thành thánh địa vũ đạo và dần trở thành nhà vô địch của khu phố."
            });
            chap1Asset.backstorySlides.Add(new BackstorySlide
            {
                illustration = act3,
                speakerName = "DẪN CHUYỆN",
                nameColor = new Color(1f, 0.2f, 0.85f, 1f),
                actionDescription = "[Raze đứng trước bức tường vương miện, đề phòng mọi kẻ thách đấu]",
                storyText = "Biểu tượng vương miện là lời nhắc không ai được xem thường anh. Nhưng nỗi sợ mất ngôi khiến Raze bắt đầu thách đấu và đuổi bất kỳ ai muốn bước qua khu phố."
            });
            chap1Asset.backstorySlides.Add(new BackstorySlide
            {
                illustration = act4,
                speakerName = "RAZE & {PLAYER}",
                nameColor = new Color(1f, 0.2f, 0.85f, 1f),
                actionDescription = "[Raze tựa tường khoanh tay nhìn xuống | Người chơi chỉ tay kiên định]",
                storyText = "Raze: \"Cậu đang tìm ai?\"\n\nNgười chơi: \"Tôi tìm Raze. Tôi muốn lấy Huy hiệu Nhịp điệu.\""
            });
            chap1Asset.backstorySlides.Add(new BackstorySlide
            {
                illustration = act5,
                speakerName = "RAZE & {PLAYER}",
                nameColor = new Color(1f, 0.2f, 0.85f, 1f),
                actionDescription = "[Raze lắc bình sơn, chỉ ngón cái ngạo nghễ | Người chơi siết chặt nắm đấm]",
                storyText = "Raze: \"Tôi là Raze. Nhưng muốn lấy huy hiệu thì cậu phải thắng tôi.\"\n\nNgười chơi: \"Tôi biết.\""
            });
            chap1Asset.backstorySlides.Add(new BackstorySlide
            {
                illustration = act6,
                speakerName = "RAZE & {PLAYER}",
                nameColor = new Color(0.2f, 0.95f, 1f, 1f),
                actionDescription = "[Raze chỉ ra sàn đấu thách thức | Người chơi vào thế nhảy rực lửa]",
                storyText = "Raze: \"Tân binh mà gan đấy. Lên sân khấu đi.\"\n\nNgười chơi: \"Bắt đầu thôi!\""
            });

            // Post-Battle Lines (Thoại thắng / thua tiếp tục dùng Dialog)
            chap1Asset.postBattleLines.Clear();
            chap1Asset.postBattleLines.Add(new DialogueLine { speakerName = "Raze", side = SpeakerSide.Right, text = "Không tệ. Cậu thắng rồi.", customAvatar = boss1Avatar, nameColor = new Color(1f, 0.2f, 0.85f, 1f) });
            chap1Asset.postBattleLines.Add(new DialogueLine { speakerName = "{PLAYER}", side = SpeakerSide.Left, text = "Vậy huy hiệu thuộc về tôi?", customAvatar = main1Avatar, nameColor = new Color(0.2f, 0.95f, 1f, 1f) });
            chap1Asset.postBattleLines.Add(new DialogueLine { speakerName = "Raze", side = SpeakerSide.Right, text = "Phải. Cầm lấy và đừng để thua Nox.", customAvatar = boss1Avatar, nameColor = new Color(1f, 0.2f, 0.85f, 1f) });
            chap1Asset.postBattleLines.Add(new DialogueLine { speakerName = "{PLAYER}", side = SpeakerSide.Left, text = "Anh đang cổ vũ tôi sao?", customAvatar = main1Avatar, nameColor = new Color(0.2f, 0.95f, 1f, 1f) });
            chap1Asset.postBattleLines.Add(new DialogueLine { speakerName = "Raze", side = SpeakerSide.Right, text = "Tôi chỉ không muốn mình thua một kẻ yếu.", customAvatar = boss1Avatar, nameColor = new Color(1f, 0.2f, 0.85f, 1f) });
            chap1Asset.postBattleLines.Add(new DialogueLine { speakerName = "{PLAYER}", side = SpeakerSide.Left, text = "Tôi sẽ nhớ câu đó.", customAvatar = main1Avatar, nameColor = new Color(0.2f, 0.95f, 1f, 1f) });

            EditorUtility.SetDirty(chap1Asset);
            AssetDatabase.SaveAssets();
        }
    }
}
