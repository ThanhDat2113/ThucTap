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
            EditorGUILayout.LabelField("⚡ STORY DIALOGUE MANAGER (INTRO & BATTLE) ⚡", headerStyle);
            GUI.color = Color.white;
            EditorGUILayout.HelpBox("Quản lý toàn bộ tiến trình Story Mode: Màn Intro Cốt truyện -> Thoại trước trận -> Trận đấu -> Thoại sau trận.", MessageType.Info);
            EditorGUILayout.Space(4);

            // Thông tin tài khoản
            string currentDisplayName = StoryDialogueManager.GetPlayerDisplayName();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"👤 Tên Tài Khoản (DisplayName): <color=#00FFAA><b>{currentDisplayName}</b></color>", new GUIStyle(EditorStyles.label) { richText = true, fontSize = 12 });
            EditorGUILayout.LabelField("💡 Nhân vật Main 1 (Bên Trái) sẽ tự động hiển thị tên tài khoản này.", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.2f, 0.9f, 0.6f);
            if (GUILayout.Button("📖 Mở Cửa Sổ Soạn Thoại & Cốt Truyện (Story Dialogue Editor)", GUILayout.Height(36)))
            {
                StoryDialogueEditorWindow.OpenWindow();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(8);

            // 2. Chế độ Test & Cấu hình cơ bản
            EditorGUILayout.LabelField("⚙️ Cấu Hình Chung", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("testInEditor"), new GUIContent("Test Trong Editor", "Bật để luôn chạy Intro và Dialogue khi bấm Play trực tiếp ở GameplayScene"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("testChapter"), new GUIContent("Chapter Test (Editor)", "Chọn Chapter (1, 2, 3...) để test khi bấm Play trong Editor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hideInactiveSpeaker"), new GUIContent("Ẩn Nhân Vật Không Nói", "Nếu bật sẽ ẩn hẳn bên kia, nếu tắt sẽ làm mờ 35%"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("typingSpeed"), new GUIContent("Tốc Độ Gõ Chữ (giây/ký tự)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoAdvanceDelay"), new GUIContent("Độ Trễ Tự Động (giây)"));

            EditorGUILayout.Space(10);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chapters"), new GUIContent("📚 Danh Sách Chapter Hội Thoại"), true);

            EditorGUILayout.Space(10);

            // 2.5 Cấu hình Ảnh Backstory Cutscenes & Chữ Nhắc Chạm
            EditorGUILayout.LabelField("🖼️ Cấu Hình Backstory Cutscene & Chữ Nhắc (Edit Mode)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("backstoryScale"), new GUIContent("Scale Ảnh (Thu nhỏ / Phóng to)", "1.0 = Phủ kín full màn hình Mobile tràn viền. Giảm xuống ví dụ 0.9 nếu muốn có viền."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backstoryOffsetY"), new GUIContent("Vị Trí Y Của Ảnh (Pixel)"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("💬 Chữ Nhắc Chạm (Ở Ảnh Đầu Tiên)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showFirstSlidePrompt"), new GUIContent("Bật Chữ Nhắc Ở Ảnh 1"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("firstSlidePromptTextString"), new GUIContent("Nội Dung Chữ Nhắc"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("firstSlidePromptColor"), new GUIContent("Màu Chữ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("firstSlidePromptFontSize"), new GUIContent("Cỡ Chữ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("firstSlidePromptOffsetX"), new GUIContent("Vị Trí X (Trái / Phải)", "0 = chính giữa. Số âm = dịch sang trái, số dương = dịch sang phải"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("firstSlidePromptOffsetY"), new GUIContent("Vị Trí Y (Lên / Xuống)", "Mặc định = 45. Tăng lên để đẩy chữ lên cao, giảm xuống để hạ sát đáy"));

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.95f, 0.7f);
            if (GUILayout.Button("👁️ Bật Preview Ảnh & Chữ (Scene)", GUILayout.Height(30)))
            {
                manager.PreviewBackstoryInEditMode();
                EditorUtility.SetDirty(manager);
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("❌ Tắt Preview", GUILayout.Height(30)))
            {
                manager.HideBackstoryInEditMode();
                EditorUtility.SetDirty(manager);
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 3. Avatar Mặc Định
            EditorGUILayout.LabelField("🎭 Avatar Mặc Định", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playerAvatarSprite"), new GUIContent("Avatar Main 1 (Trái)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("opponentAvatarSprite"), new GUIContent("Avatar Boss 1 / Raze (Phải)"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // 4. UI References
            EditorGUILayout.LabelField("🖥️ UI References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("introContainer"), new GUIContent("Intro Container"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chapterLogoImage"), new GUIContent("Logo Chapter Image"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("chapterTitleText"), new GUIContent("Chapter Title Text"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("characterStoryTitleText"), new GUIContent("Story Title Text"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("characterStoryBodyText"), new GUIContent("Story Body Text"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("dialogueContainer"), new GUIContent("Dialogue Container"));

            EditorGUILayout.Space(12);

            GUI.backgroundColor = new Color(0.2f, 0.9f, 1f);
            if (GUILayout.Button("⚡ Rebuild Toàn Bộ UI Vào Scene", GUILayout.Height(32)))
            {
                StoryDialogueSceneBuilder.Build();
            }
            GUI.backgroundColor = Color.white;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
