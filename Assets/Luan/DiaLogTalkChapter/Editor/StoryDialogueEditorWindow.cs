using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Luan.Dialogue;

namespace Luan.Dialogue.Editor
{
    public class StoryDialogueEditorWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private StoryDialogueManager manager;
        private int selectedChapterIndex = 0;
        private int selectedDialogueSubTab = 0; // 0: Pre-Battle, 1: Post-Battle

        [MenuItem("Tools/Luan/Story Dialogue Editor (Soạn Lời Thoại)")]
        [MenuItem("Window/Story Dialogue Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<StoryDialogueEditorWindow>("Story Dialogue");
            window.minSize = new Vector2(680, 780);
            window.Show();
        }

        private void OnEnable()
        {
            FindManager();
        }

        private void OnFocus()
        {
            FindManager();
        }

        private void FindManager()
        {
            manager = Object.FindFirstObjectByType<StoryDialogueManager>();
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // 1. HEADER
            EditorGUILayout.Space(8);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.color = new Color(0.2f, 0.95f, 1f, 1f);
            EditorGUILayout.LabelField("⚡ BẢNG SOẠN LỜI THOẠI & CỐT TRUYỆN (MULTI-CHAPTER) ⚡", titleStyle);
            GUI.color = Color.white;

            // 2. THÔNG TIN TÀI KHOẢN (DISPLAY NAME)
            string currentDisplayName = StoryDialogueManager.GetPlayerDisplayName();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"👤 Tên Tài Khoản Hiện Tại: <color=#00FFAA><b>{currentDisplayName}</b></color>", new GUIStyle(EditorStyles.label) { richText = true, fontSize = 12 });
            EditorGUILayout.LabelField("💡 Nhân vật Main 1 (Bên Trái) sẽ tự động hiển thị tên tài khoản này.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // 3. KIỂM TRA SCENE & MANAGER
            var currentScene = EditorSceneManager.GetActiveScene();
            if (manager == null)
            {
                FindManager();
            }

            if (manager == null)
            {
                EditorGUILayout.HelpBox($"Không tìm thấy '[StoryDialogueManager]' trong scene '{currentScene.name}'.\nBạn cần mở scene 'GameplayScene' để bắt đầu soạn thoại.", MessageType.Warning);

                if (GUILayout.Button("📂 Mở GameplayScene Ngay Bây Giờ", GUILayout.Height(36)))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.OpenScene("Assets/Scenes/GameplayScene.unity");
                        FindManager();
                    }
                }
                EditorGUILayout.EndScrollView();
                return;
            }

            if (manager.Chapters == null || manager.Chapters.Count == 0)
            {
                StoryDialogueSceneBuilder.Build();
            }

            var chapters = manager.Chapters;

            // 4. CHAPTER SELECTOR TABS
            EditorGUILayout.LabelField("📚 Chọn Chapter Cần Chỉnh Sửa:", EditorStyles.boldLabel);
            int tabCount = chapters.Count + 1;
            string[] tabNames = new string[tabCount];
            for (int c = 0; c < chapters.Count; c++)
            {
                var entry = chapters[c];
                tabNames[c] = $"Chapter {entry.chapterNumber}: {entry.chapterTitle}";
            }
            tabNames[tabCount - 1] = "➕ Thêm Chapter Mới";

            int newTab = GUILayout.Toolbar(Mathf.Clamp(selectedChapterIndex, 0, tabCount - 2), tabNames, GUILayout.Height(32));

            if (newTab == tabCount - 1)
            {
                CreateNewChapter(chapters.Count + 1);
                selectedChapterIndex = chapters.Count - 1;
            }
            else
            {
                selectedChapterIndex = newTab;
            }

            EditorGUILayout.Space(6);

            if (selectedChapterIndex >= chapters.Count)
                selectedChapterIndex = chapters.Count - 1;

            var activeChapter = chapters[selectedChapterIndex];
            DialogueStoryData currentData = activeChapter.dialogueAsset;

            // 5. CẤU HÌNH CHAPTER ĐANG CHỌN
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUI.color = new Color(0.2f, 0.95f, 1f, 1f);
            EditorGUILayout.LabelField($"⚙️ Cấu Hình Chapter {activeChapter.chapterNumber}: {activeChapter.chapterTitle}", EditorStyles.boldLabel);
            GUI.color = Color.white;

            bool isCurrentTest = manager.TestChapter == activeChapter.chapterNumber;
            GUI.backgroundColor = isCurrentTest ? new Color(0.2f, 1f, 0.6f) : Color.white;
            string testBtnText = isCurrentTest ? "✓ Đang Chọn Để Test Trong Editor" : "🧪 Đặt Làm Chapter Test Trong Editor";
            if (GUILayout.Button(testBtnText, GUILayout.Height(22)))
            {
                Undo.RecordObject(manager, "Chọn Chapter Test");
                manager.TestChapter = activeChapter.chapterNumber;
                EditorUtility.SetDirty(manager);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tiêu đề Chapter:", GUILayout.Width(110));
            string newTitle = EditorGUILayout.TextField(activeChapter.chapterTitle);
            if (newTitle != activeChapter.chapterTitle)
            {
                Undo.RecordObject(manager, "Đổi tên Chapter");
                activeChapter.chapterTitle = newTitle;
                if (currentData != null) currentData.chapterTitle = newTitle;
                EditorUtility.SetDirty(manager);
                if (currentData != null) EditorUtility.SetDirty(currentData);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tên Đối Thủ (Boss):", GUILayout.Width(110));
            string newOppName = EditorGUILayout.TextField(activeChapter.defaultOpponentName);
            if (newOppName != activeChapter.defaultOpponentName)
            {
                Undo.RecordObject(manager, "Đổi tên Đối Thủ");
                activeChapter.defaultOpponentName = newOppName;
                EditorUtility.SetDirty(manager);
            }

            EditorGUILayout.LabelField("Avatar Đối Thủ:", GUILayout.Width(95));
            Sprite newOppAvatar = (Sprite)EditorGUILayout.ObjectField(activeChapter.opponentAvatar, typeof(Sprite), false, GUILayout.Width(110));
            if (newOppAvatar != activeChapter.opponentAvatar)
            {
                Undo.RecordObject(manager, "Đổi Avatar Đối Thủ");
                activeChapter.opponentAvatar = newOppAvatar;
                EditorUtility.SetDirty(manager);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("File Dữ Liệu Thoại:", GUILayout.Width(110));
            var newAsset = (DialogueStoryData)EditorGUILayout.ObjectField(activeChapter.dialogueAsset, typeof(DialogueStoryData), false);
            if (newAsset != activeChapter.dialogueAsset)
            {
                Undo.RecordObject(manager, "Gán Asset Thoại");
                activeChapter.dialogueAsset = newAsset;
                EditorUtility.SetDirty(manager);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // 6. GIỚI THIỆU CHAPTER & CỐT TRUYỆN (INTRO SCREEN)
            if (currentData != null)
            {
                GUI.backgroundColor = new Color(0.92f, 0.95f, 1f);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUI.backgroundColor = Color.white;

                GUI.color = new Color(1f, 0.4f, 0.85f, 1f);
                EditorGUILayout.LabelField("📖 Màn Hình Giới Thiệu (Chapter Logo & Cốt Truyện Nhân Vật)", EditorStyles.boldLabel);
                GUI.color = Color.white;
                EditorGUILayout.HelpBox("Màn hình này sẽ hiện ra trước khi vào trận. Người chơi đọc sơ về nhân vật rồi chạm vào màn hình để bắt đầu đối thoại.", MessageType.None);

                EditorGUILayout.Space(4);

                // Logo Chapter Image slot
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Logo Chapter (Sprite):", GUILayout.Width(140));
                Sprite newLogo = (Sprite)EditorGUILayout.ObjectField(currentData.chapterLogo, typeof(Sprite), false);
                if (newLogo != currentData.chapterLogo)
                {
                    RecordUndo(currentData, "Đổi Logo Chapter");
                    currentData.chapterLogo = newLogo;
                    MarkDirty(currentData);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("   💡 Bạn có thể kéo thả Sprite Logo Chapter vào ô trên khi làm xong ảnh.", EditorStyles.miniLabel);

                EditorGUILayout.Space(4);

                // Story Title
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Tiêu Đề Giới Thiệu:", GUILayout.Width(140));
                string newStoryTitle = EditorGUILayout.TextField(currentData.characterStoryTitle);
                if (newStoryTitle != currentData.characterStoryTitle)
                {
                    RecordUndo(currentData, "Đổi Tiêu Đề Cốt Truyện");
                    currentData.characterStoryTitle = newStoryTitle;
                    MarkDirty(currentData);
                }
                EditorGUILayout.EndHorizontal();

                // Character Intro Art
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Ảnh Nhân Vật Intro:", GUILayout.Width(140));
                Sprite newIntroArt = (Sprite)EditorGUILayout.ObjectField(currentData.characterIntroArt, typeof(Sprite), false);
                if (newIntroArt != currentData.characterIntroArt)
                {
                    RecordUndo(currentData, "Đổi Ảnh Intro Nhân Vật");
                    currentData.characterIntroArt = newIntroArt;
                    MarkDirty(currentData);
                }
                EditorGUILayout.EndHorizontal();

                // Story Text Body
                EditorGUILayout.LabelField("Nội Dung Cốt Truyện / Giới Thiệu:");
                string newStory = EditorGUILayout.TextArea(currentData.characterStory, GUILayout.Height(100));
                if (newStory != currentData.characterStory)
                {
                    RecordUndo(currentData, "Sửa Nội Dung Cốt Truyện");
                    currentData.characterStory = newStory;
                    MarkDirty(currentData);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            // 7. SOẠN LỜI THOẠI (PRE-BATTLE VS POST-BATTLE)
            int preCount = currentData != null ? currentData.preBattleLines.Count : manager.DialogueLines.Count;
            int postCount = currentData != null ? currentData.postBattleLines.Count : 0;

            string[] subTabLabels = new string[]
            {
                $"⚔️ Thoại Trước Trận ({preCount} câu)",
                $"🏆 Thoại Sau Trận ({postCount} câu)"
            };

            selectedDialogueSubTab = GUILayout.Toolbar(selectedDialogueSubTab, subTabLabels, GUILayout.Height(30));
            EditorGUILayout.Space(6);

            List<DialogueLine> activeLinesList = null;
            if (currentData != null)
            {
                activeLinesList = selectedDialogueSubTab == 0 ? currentData.preBattleLines : currentData.postBattleLines;
            }
            else
            {
                activeLinesList = manager.DialogueLines;
            }

            string subTabDesc = selectedDialogueSubTab == 0
                ? "Các câu thoại diễn ra TRƯỚC khi nốt nhạc bắt đầu rơi."
                : "Các câu thoại diễn ra SAU khi trận đấu kết thúc thành công (thắng).";
            EditorGUILayout.HelpBox(subTabDesc, MessageType.Info);

            EditorGUILayout.Space(4);

            if (activeLinesList == null || activeLinesList.Count == 0)
            {
                EditorGUILayout.HelpBox($"⚠️ Hiện chưa có câu thoại nào trong danh sách này.\nBấm '+ Thêm Câu Thoại Mới' bên dưới để bắt đầu soạn.", MessageType.Warning);
            }
            else
            {
                for (int i = 0; i < activeLinesList.Count; i++)
                {
                    var line = activeLinesList[i];
                    bool isLeft = line.side == SpeakerSide.Left;

                    Color accentColor = isLeft ? new Color(0.2f, 0.95f, 1f) : new Color(1f, 0.25f, 0.85f);
                    string opponentDisplayName = !string.IsNullOrEmpty(activeChapter.defaultOpponentName) ? activeChapter.defaultOpponentName : "Raze";

                    GUI.backgroundColor = isLeft ? new Color(0.88f, 0.96f, 1f) : new Color(1f, 0.90f, 0.98f);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUI.backgroundColor = Color.white;

                    // Header dòng thoại
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = accentColor;
                    string sideTag = isLeft ? $"[◄ NGƯỜI CHƠI ({currentDisplayName})]" : $"[{opponentDisplayName} (BÊN PHẢI) ►]";
                    EditorGUILayout.LabelField($"Câu #{i + 1}  {sideTag}", EditorStyles.boldLabel);
                    GUI.color = Color.white;

                    if (GUILayout.Button("▲", GUILayout.Width(26), GUILayout.Height(20)) && i > 0)
                    {
                        RecordUndo(currentData, "Di chuyển câu thoại lên");
                        var temp = activeLinesList[i];
                        activeLinesList[i] = activeLinesList[i - 1];
                        activeLinesList[i - 1] = temp;
                        MarkDirty(currentData);
                        break;
                    }
                    if (GUILayout.Button("▼", GUILayout.Width(26), GUILayout.Height(20)) && i < activeLinesList.Count - 1)
                    {
                        RecordUndo(currentData, "Di chuyển câu thoại xuống");
                        var temp = activeLinesList[i];
                        activeLinesList[i] = activeLinesList[i + 1];
                        activeLinesList[i + 1] = temp;
                        MarkDirty(currentData);
                        break;
                    }

                    GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                    if (GUILayout.Button("✕ Xóa", GUILayout.Width(55), GUILayout.Height(20)))
                    {
                        RecordUndo(currentData, "Xóa câu thoại");
                        activeLinesList.RemoveAt(i);
                        MarkDirty(currentData);
                        GUI.backgroundColor = Color.white;
                        break;
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(4);

                    // Chọn bên nói
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Bên Nói:");
                    if (GUILayout.Toggle(isLeft, $"◄ Người Chơi ({currentDisplayName})", "Button", GUILayout.Height(24)))
                    {
                        if (!isLeft)
                        {
                            RecordUndo(currentData, "Đổi bên nói sang Trái");
                            line.side = SpeakerSide.Left;
                            if (string.IsNullOrEmpty(line.speakerName) || line.speakerName == opponentDisplayName)
                                line.speakerName = "{PLAYER}";
                            MarkDirty(currentData);
                        }
                    }
                    if (GUILayout.Toggle(!isLeft, $"{opponentDisplayName} (Bên Phải) ►", "Button", GUILayout.Height(24)))
                    {
                        if (isLeft)
                        {
                            RecordUndo(currentData, "Đổi bên nói sang Phải");
                            line.side = SpeakerSide.Right;
                            if (string.IsNullOrEmpty(line.speakerName) || line.speakerName == "{PLAYER}" || line.speakerName == "MAIN 1")
                                line.speakerName = opponentDisplayName;
                            MarkDirty(currentData);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // Tên người nói
                    EditorGUILayout.BeginHorizontal();
                    string nameHint = isLeft ? $"Tên ({currentDisplayName}):" : "Tên Đối Thủ:";
                    string newName = EditorGUILayout.TextField(nameHint, line.speakerName);
                    if (newName != line.speakerName)
                    {
                        RecordUndo(currentData, "Sửa tên người nói");
                        line.speakerName = newName;
                        MarkDirty(currentData);
                    }
                    line.nameColor = EditorGUILayout.ColorField(GUIContent.none, line.nameColor, false, false, false, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();

                    // Nội dung thoại
                    EditorGUILayout.LabelField("Nội Dung Lời Thoại:");
                    string newText = EditorGUILayout.TextArea(line.text, GUILayout.Height(50));
                    if (newText != line.text)
                    {
                        RecordUndo(currentData, "Sửa nội dung thoại");
                        line.text = newText;
                        MarkDirty(currentData);
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(4);
                }
            }

            EditorGUILayout.Space(6);

            // 8. NÚT THÊM CÂU THOẠI & LƯU
            GUI.backgroundColor = new Color(0.3f, 1f, 0.5f);
            string addLabel = selectedDialogueSubTab == 0 ? "➕ Thêm Câu Thoại Trước Trận (Pre-Battle)" : "➕ Thêm Câu Thoại Sau Trận (Post-Battle)";
            if (GUILayout.Button(addLabel, GUILayout.Height(36)))
            {
                RecordUndo(currentData, "Thêm câu thoại mới");
                string oppName = !string.IsNullOrEmpty(activeChapter.defaultOpponentName) ? activeChapter.defaultOpponentName : "Raze";
                activeLinesList.Add(new DialogueLine
                {
                    speakerName = "{PLAYER}",
                    side = SpeakerSide.Left,
                    text = "Nhập nội dung câu thoại của bạn vào đây...",
                    nameColor = new Color(0.2f, 0.95f, 1f, 1f),
                    customAvatar = manager.PlayerAvatarSprite
                });
                MarkDirty(currentData);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button("💾 Lưu Thay Đổi Vào Scene & File Asset", GUILayout.Height(34)))
            {
                MarkDirty(currentData);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                EditorSceneManager.SaveScene(manager.gameObject.scene);
                Debug.Log($"<color=#00FFAA>[StoryDialogueEditor] Đã lưu thành công dữ liệu Chapter {activeChapter.chapterNumber}!</color>");
            }

            GUI.backgroundColor = new Color(0.2f, 0.9f, 1f);
            if (GUILayout.Button("⚡ Build Lại Toàn Bộ UI Vào GameplayScene", GUILayout.Height(34)))
            {
                StoryDialogueSceneBuilder.Build();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);

            EditorGUILayout.HelpBox(
                "💡 GHI CHÚ QUAN TRỌNG:\n" +
                "• Lời thoại trước trận: 29 câu đối đầu giữa {PLAYER} và Raze.\n" +
                "• Lời thoại sau trận: 37 câu đối thoại sau chiến thắng (hơn đúng 1 điểm, nhận huy hiệu).\n" +
                "• Dùng từ khóa {PLAYER} hoặc [Tên người chơi] để tự động thay bằng Tên Tài Khoản thực tế.",
                MessageType.Info
            );

            EditorGUILayout.EndScrollView();
        }

        private void CreateNewChapter(int nextNum)
        {
            string newAssetPath = $"Assets/Luan/DiaLogTalkChapter/Data/Chapter{nextNum}_Dialogue.asset";
            var newAsset = AssetDatabase.LoadAssetAtPath<DialogueStoryData>(newAssetPath);
            if (newAsset == null)
            {
                newAsset = ScriptableObject.CreateInstance<DialogueStoryData>();
                newAsset.chapterNumber = nextNum;
                newAsset.chapterTitle = $"Chapter {nextNum}";
                newAsset.characterStoryTitle = $"Câu chuyện của BOSS {nextNum}";
                newAsset.characterStory = "Nội dung giới thiệu về nhân vật...";
                newAsset.preBattleLines = new List<DialogueLine>
                {
                    new DialogueLine
                    {
                        speakerName = "{PLAYER}",
                        side = SpeakerSide.Left,
                        text = $"Lời thoại mở đầu Chapter {nextNum} của Người Chơi...",
                        customAvatar = manager.PlayerAvatarSprite
                    },
                    new DialogueLine
                    {
                        speakerName = $"BOSS {nextNum}",
                        side = SpeakerSide.Right,
                        text = $"Lời thoại đáp lại của Boss {nextNum}...",
                        customAvatar = manager.OpponentAvatarSprite
                    }
                };
                newAsset.postBattleLines = new List<DialogueLine>();
                AssetDatabase.CreateAsset(newAsset, newAssetPath);
                AssetDatabase.SaveAssets();
            }

            var newEntry = new ChapterDialogueEntry
            {
                chapterNumber = nextNum,
                chapterTitle = $"Chapter {nextNum}",
                dialogueAsset = newAsset,
                opponentAvatar = manager.OpponentAvatarSprite,
                defaultOpponentName = $"BOSS {nextNum}"
            };

            Undo.RecordObject(manager, "Thêm Chapter mới");
            manager.Chapters.Add(newEntry);
            EditorUtility.SetDirty(manager);
            AssetDatabase.SaveAssets();
        }

        private void RecordUndo(DialogueStoryData asset, string action)
        {
            if (asset != null)
                Undo.RecordObject(asset, action);
            else if (manager != null)
                Undo.RecordObject(manager, action);
        }

        private void MarkDirty(DialogueStoryData asset)
        {
            if (asset != null)
                EditorUtility.SetDirty(asset);
            if (manager != null)
            {
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
        }
    }
}
