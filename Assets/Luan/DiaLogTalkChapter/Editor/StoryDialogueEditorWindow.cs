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

        [MenuItem("Tools/Luan/Story Dialogue Editor (Soạn Lời Thoại)")]
        [MenuItem("Window/Story Dialogue Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<StoryDialogueEditorWindow>("Story Dialogue");
            window.minSize = new Vector2(620, 720);
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
            EditorGUILayout.LabelField("⚡ BẢNG SOẠN LỜI THOẠI CỐT TRUYỆN (MULTI-CHAPTER) ⚡", titleStyle);
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

            // Đảm bảo có ít nhất 2 chapter mặc định
            if (manager.Chapters == null || manager.Chapters.Count == 0)
            {
                StoryDialogueSceneBuilder.Build();
            }

            var chapters = manager.Chapters;

            // 4. CHAPTER SELECTOR TABS
            EditorGUILayout.LabelField("📚 Chọn Chapter Cần Soạn Lời Thoại:", EditorStyles.boldLabel);
            int tabCount = chapters.Count + 1; // +1 cho tab [+ Thêm Chapter]
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
                // Người dùng bấm vào "Thêm Chapter Mới"
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

            // 5. CẤU HÌNH CHAPTER ĐANG CHỌN
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            GUI.color = new Color(0.2f, 0.95f, 1f, 1f);
            EditorGUILayout.LabelField($"⚙️ Cấu Hình Chapter {activeChapter.chapterNumber}: {activeChapter.chapterTitle}", EditorStyles.boldLabel);
            GUI.color = Color.white;

            // Nút test chapter này
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
                EditorUtility.SetDirty(manager);
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

            // 6. DANH SÁCH LỜI THOẠI CỦA CHAPTER
            DialogueStoryData currentData = activeChapter.dialogueAsset;
            List<DialogueLine> lines = null;
            if (currentData != null)
            {
                lines = currentData.lines;
            }
            else
            {
                lines = manager.DialogueLines;
            }

            if (lines == null || lines.Count == 0)
            {
                EditorGUILayout.HelpBox($"⚠️ Chapter {activeChapter.chapterNumber} hiện chưa có câu thoại nào!\nBấm '+ Thêm Câu Thoại Mới' bên dưới để bắt đầu soạn thoại.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"💬 Danh Sách Lời Thoại Chapter {activeChapter.chapterNumber} ({lines.Count} câu):", EditorStyles.boldLabel);

                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    bool isLeft = line.side == SpeakerSide.Left;

                    Color accentColor = isLeft ? new Color(0.2f, 0.95f, 1f) : new Color(1f, 0.25f, 0.85f);
                    string opponentDisplayName = !string.IsNullOrEmpty(activeChapter.defaultOpponentName) ? activeChapter.defaultOpponentName : "BOSS";

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
                        var temp = lines[i];
                        lines[i] = lines[i - 1];
                        lines[i - 1] = temp;
                        MarkDirty(currentData);
                        break;
                    }
                    if (GUILayout.Button("▼", GUILayout.Width(26), GUILayout.Height(20)) && i < lines.Count - 1)
                    {
                        RecordUndo(currentData, "Di chuyển câu thoại xuống");
                        var temp = lines[i];
                        lines[i] = lines[i + 1];
                        lines[i + 1] = temp;
                        MarkDirty(currentData);
                        break;
                    }

                    GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                    if (GUILayout.Button("✕ Xóa", GUILayout.Width(55), GUILayout.Height(20)))
                    {
                        RecordUndo(currentData, "Xóa câu thoại");
                        lines.RemoveAt(i);
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
                    EditorGUILayout.EndHorizontal();

                    // Nội dung thoại
                    EditorGUILayout.LabelField("Nội Dung Lời Thoại:");
                    string newText = EditorGUILayout.TextArea(line.text, GUILayout.Height(55));
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

            // 7. NÚT THÊM CÂU THOẠI & LƯU
            GUI.backgroundColor = new Color(0.3f, 1f, 0.5f);
            if (GUILayout.Button($"➕ Thêm Câu Thoại Mới Vào Chapter {activeChapter.chapterNumber}", GUILayout.Height(36)))
            {
                RecordUndo(currentData, "Thêm câu thoại mới");
                lines.Add(new DialogueLine
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
            GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
            if (GUILayout.Button("💾 Lưu Thay Đổi Vào Scene & File Asset", GUILayout.Height(32)))
            {
                MarkDirty(currentData);
                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                EditorSceneManager.SaveScene(manager.gameObject.scene);
                Debug.Log($"<color=#00FFAA>[StoryDialogueEditor] Đã lưu thành công lời thoại Chapter {activeChapter.chapterNumber}!</color>");
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(12);

            // 8. GỢI Ý & HƯỚNG DẪN
            EditorGUILayout.HelpBox(
                "💡 MẸO ĐA CHAPTER (MULTI-CHAPTER):\n" +
                "• Bạn có thể tạo lời thoại cho Chapter 1, Chapter 2, Chapter 3,... bằng các Tab ở trên.\n" +
                "• Khi người chơi chọn Week tương ứng trong Story Mode, game sẽ tự động load đúng lời thoại và Avatar của Boss Chapter đó.\n" +
                "• Dùng từ khóa {PLAYER} trong câu thoại (VD: 'Chào {PLAYER}!') để tự động thay bằng Tên Tài Khoản thực tế của người chơi.",
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
                newAsset.chapterTitle = $"Chapter {nextNum}";
                newAsset.lines = new List<DialogueLine>
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
