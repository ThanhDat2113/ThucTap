using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Luan.Dialogue;

namespace Luan.Dialogue.Editor
{
    [CustomEditor(typeof(StoryDialogueManager))]
    public class StoryDialogueManagerEditor : UnityEditor.Editor
    {
        private StoryDialogueManager manager;

        private void OnEnable()
        {
            manager = (StoryDialogueManager)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1. Tiêu đề Cyberpunk
            EditorGUILayout.Space(8);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.color = new Color(0.2f, 0.95f, 1f, 1f);
            EditorGUILayout.LabelField("⚡ STORY DIALOGUE MANAGER (2 SPEAKERS) ⚡", headerStyle);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Quản lý hội thoại đối đầu giữa Main 1 (Người chơi bên Trái) và Boss 1 (bên Phải).", MessageType.Info);
            EditorGUILayout.Space(4);

            // Thông tin tài khoản
            string currentDisplayName = StoryDialogueManager.GetPlayerDisplayName();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"👤 Tên Tài Khoản (DisplayName): <color=#00FFAA><b>{currentDisplayName}</b></color>", new GUIStyle(EditorStyles.label) { richText = true, fontSize = 12 });
            EditorGUILayout.LabelField("💡 Nhân vật Main 1 (Bên Trái) sẽ tự động hiển thị tên tài khoản này.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.6f);
            if (GUILayout.Button("📖 Mở Cửa Sổ Soạn Thoại Riêng Biệt (Story Dialogue Window)", GUILayout.Height(32)))
            {
                StoryDialogueEditorWindow.OpenWindow();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(8);

            // 2. Chế độ Test & Cấu hình cơ bản
            EditorGUILayout.LabelField("⚙️ Cấu Hình Chung", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("testInEditor"), new GUIContent("Test Trong Editor", "Bật để luôn chạy hội thoại khi bấm Play trực tiếp ở GameplayScene"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("testChapter"), new GUIContent("Chapter Test (Editor)", "Chọn Chapter (1, 2, 3...) để test khi bấm Play trong Editor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hideInactiveSpeaker"), new GUIContent("Ẩn Nhân Vật Không Nói", "Nếu bật sẽ ẩn hẳn bên kia, nếu tắt sẽ làm mờ 35%"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("typingSpeed"), new GUIContent("Tốc Độ Gõ Chữ (giây/ký tự)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoAdvanceDelay"), new GUIContent("Độ Trễ Tự Động (giây)"));

            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chapters"), new GUIContent("📚 Danh Sách Chapter Hội Thoại"), true);

            EditorGUILayout.Space(10);

            // 3. Avatar Mặc Định
            EditorGUILayout.LabelField("🎭 Avatar Mặc Định", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playerAvatarSprite"), new GUIContent("Avatar Main 1 (Trái)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("opponentAvatarSprite"), new GUIContent("Avatar Boss 1 (Phải)"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 4. DANH SÁCH CÂU THOẠI (NHẬP THOẠI Ở ĐÂY)
            var assetProp = serializedObject.FindProperty("dialogueDataAsset");
            if (assetProp != null)
            {
                EditorGUILayout.PropertyField(assetProp, new GUIContent("Dialogue Data Asset (Tuỳ chọn)"));
            }

            var linesProp = serializedObject.FindProperty("dialogueLines");
            int count = linesProp != null ? linesProp.arraySize : 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"💬 Danh Sách Lời Thoại ({count} câu)", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.2f, 0.8f, 1f);
            if (GUILayout.Button("⚡ Nạp Thoại Mẫu Chapter 1", GUILayout.Height(24), GUILayout.Width(190)))
            {
                Undo.RecordObject(manager, "Nạp thoại mẫu Chapter 1");
                manager.InitDefaultChapter1Lines();
                EditorUtility.SetDirty(manager);
                serializedObject.Update();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            if (count == 0)
            {
                EditorGUILayout.HelpBox("⚠️ Hiện tại chưa có câu thoại nào!\n👉 Bấm nút '⚡ Nạp Thoại Mẫu Chapter 1' ở trên để nạp 4 câu đối thoại mở màn, hoặc bấm '+ Thêm Câu Thoại Mới' bên dưới để tự gõ.", MessageType.Warning);
            }

            if (linesProp != null)
            {
                for (int i = 0; i < linesProp.arraySize; i++)
                {
                    var lineProp = linesProp.GetArrayElementAtIndex(i);
                    var speakerNameProp = lineProp.FindPropertyRelative("speakerName");
                    var sideProp = lineProp.FindPropertyRelative("side");
                    var textProp = lineProp.FindPropertyRelative("text");
                    var avatarProp = lineProp.FindPropertyRelative("customAvatar");

                    bool isLeft = sideProp.enumValueIndex == 0; // 0 = Left, 1 = Right

                    // Màu viền box theo bên nói
                    Color boxColor = isLeft ? new Color(0.15f, 0.45f, 0.65f, 0.35f) : new Color(0.65f, 0.15f, 0.5f, 0.35f);
                    Color titleColor = isLeft ? new Color(0.3f, 0.95f, 1f) : new Color(1f, 0.35f, 0.85f);

                    GUI.backgroundColor = isLeft ? new Color(0.85f, 0.95f, 1f) : new Color(1f, 0.88f, 0.98f);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    GUI.backgroundColor = Color.white;

                    // Dòng tiêu đề câu thoại
                    EditorGUILayout.BeginHorizontal();
                    GUI.color = titleColor;
                    string sideTag = isLeft ? $"[◄ NGƯỜI CHƠI ({currentDisplayName})]" : "[BOSS 1 - BÊN PHẢI ►]";
                    EditorGUILayout.LabelField($"Câu #{i + 1} {sideTag}", EditorStyles.boldLabel);
                    GUI.color = Color.white;

                    // Mũi tên lên / xuống
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("▲", GUILayout.Width(25)))
                    {
                        linesProp.MoveArrayElement(i, i - 1);
                        break;
                    }
                    GUI.enabled = i < linesProp.arraySize - 1;
                    if (GUILayout.Button("▼", GUILayout.Width(25)))
                    {
                        linesProp.MoveArrayElement(i, i + 1);
                        break;
                    }
                    GUI.enabled = true;

                    // Nút Xóa
                    GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        linesProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.EndHorizontal();

                    // Chọn Người Nói (Side)
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Phía Nói:");
                    int selectedSide = GUILayout.Toolbar(sideProp.enumValueIndex, new string[] { $"◄ Người Chơi ({currentDisplayName})", "Boss 1 (Bên Phải) ►" });
                    if (selectedSide != sideProp.enumValueIndex)
                    {
                        sideProp.enumValueIndex = selectedSide;
                        if (string.IsNullOrEmpty(speakerNameProp.stringValue) || speakerNameProp.stringValue == "MAIN 1" || speakerNameProp.stringValue == "BOSS 1" || speakerNameProp.stringValue == "{PLAYER}")
                        {
                            speakerNameProp.stringValue = selectedSide == 0 ? "{PLAYER}" : "BOSS 1";
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // Tên người nói
                    string nameLabel = isLeft ? $"Tên ({currentDisplayName}):" : "Tên Nhân Vật:";
                    EditorGUILayout.PropertyField(speakerNameProp, new GUIContent(nameLabel));

                    // Nội dung câu thoại
                    EditorGUILayout.LabelField("Nội Dung Lời Thoại:");
                    textProp.stringValue = EditorGUILayout.TextArea(textProp.stringValue, GUILayout.Height(50));

                    // Avatar riêng (nếu có)
                    EditorGUILayout.PropertyField(avatarProp, new GUIContent("Avatar Riêng (tuỳ chọn):"));

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(4);
                }

                // Nút Thêm câu thoại
                GUI.backgroundColor = new Color(0.3f, 1f, 0.5f);
                if (GUILayout.Button("+ Thêm Câu Thoại Mới", GUILayout.Height(32)))
                {
                    linesProp.InsertArrayElementAtIndex(linesProp.arraySize);
                    var newLine = linesProp.GetArrayElementAtIndex(linesProp.arraySize - 1);
                    newLine.FindPropertyRelative("speakerName").stringValue = "MAIN 1";
                    newLine.FindPropertyRelative("side").enumValueIndex = 0;
                    newLine.FindPropertyRelative("text").stringValue = "Nội dung câu thoại mới...";
                    newLine.FindPropertyRelative("customAvatar").objectReferenceValue = null;
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(12);

            // 5. Nút Build / Đồng bộ Scene
            EditorGUILayout.LabelField("🛠️ Đồng Bộ UI Vào Scene", EditorStyles.boldLabel);
            GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
            if (GUILayout.Button("⚡ Tự Động Xây Dựng / Đồng Bộ UI 2 Bên Vào GameplayScene", GUILayout.Height(34)))
            {
                StoryDialogueSceneBuilder.Build();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

