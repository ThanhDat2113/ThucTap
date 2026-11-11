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
            // Tự động đồng bộ số liệu nếu người dùng vừa kéo thả đối tượng trong Scene / Hierarchy
            if (!EditorApplication.isPlaying)
            {
                if (manager.BackstoryPromptText != null)
                {
                    var rtPrompt = manager.BackstoryPromptText.rectTransform;
                    if (serializedObject.FindProperty("firstSlidePromptOffsetX").floatValue != rtPrompt.anchoredPosition.x ||
                        serializedObject.FindProperty("firstSlidePromptOffsetY").floatValue != rtPrompt.anchoredPosition.y)
                    {
                        serializedObject.FindProperty("firstSlidePromptOffsetX").floatValue = rtPrompt.anchoredPosition.x;
                        serializedObject.FindProperty("firstSlidePromptOffsetY").floatValue = rtPrompt.anchoredPosition.y;
                    }
                }
                if (manager.BackstoryIllustrationImage != null)
                {
                    var rtImg = manager.BackstoryIllustrationImage.rectTransform;
                    if (Mathf.Abs(serializedObject.FindProperty("backstoryScale").floatValue - rtImg.localScale.x) > 0.001f && rtImg.localScale.x > 0.05f)
                    {
                        serializedObject.FindProperty("backstoryScale").floatValue = rtImg.localScale.x;
                    }
                }
            }

            GUI.backgroundColor = new Color(0.1f, 0.85f, 1f, 1f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField("🖼️ CHỈNH SCALE ẢNH BACKSTORY & VỊ TRÍ CHỮ NHẮC", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Bạn có thể chỉnh bằng 2 cách rất linh hoạt:\n1. Kéo thả tự do bằng chuột trong Scene: Bấm nút 'Bật Preview' bên dưới, rồi bấm 'Chọn Chữ' hoặc 'Chọn Ảnh' để kéo thả trực tiếp.\n2. Chỉnh thông số số học tại các thanh trượt bên dưới.", MessageType.Info);
            
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("📐 Scale & Vị Trí Ảnh Minh Họa", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backstoryScale"), new GUIContent("Scale Ảnh (Phóng To / Thu Nhỏ)", "1.0 = Phủ kín 100% full màn hình Mobile. Kéo < 1.0 (vd: 0.9, 0.85) để có viền lề đẹp."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("backstoryOffsetY"), new GUIContent("Vị Trí Y Của Ảnh (Pixel)"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                manager.ApplyBackstoryLayout();
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("💬 Vị Trí & Nội Dung Chữ Nhắc Chạm (Ở Ảnh Đầu)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showFirstSlidePrompt"), new GUIContent("Bật Chữ Nhắc"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("firstSlidePromptTextString"), new GUIContent("Nội Dung Chữ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("firstSlidePromptColor"), new GUIContent("Màu Chữ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("firstSlidePromptFontSize"), new GUIContent("Cỡ Chữ"));

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("firstSlidePromptOffsetX"), new GUIContent("Vị Trí X (Trái / Phải)", "0 = chính giữa. Âm = sang trái, Dương = sang phải."));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("firstSlidePromptOffsetY"), new GUIContent("Vị Trí Y (Lên / Xuống)", "Mặc định = 45. Tăng lên để đẩy chữ lên cao, giảm xuống để hạ sát đáy."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                manager.SetPromptPosition(new Vector2(
                    serializedObject.FindProperty("firstSlidePromptOffsetX").floatValue,
                    serializedObject.FindProperty("firstSlidePromptOffsetY").floatValue
                ));
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.2f, 0.95f, 0.7f);
            if (GUILayout.Button("👁️ Bật Preview (Scene)", GUILayout.Height(32)))
            {
                manager.PreviewBackstoryInEditMode();
                EditorUtility.SetDirty(manager);
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            if (GUILayout.Button("❌ Tắt Preview", GUILayout.Height(32)))
            {
                manager.HideBackstoryInEditMode();
                EditorUtility.SetDirty(manager);
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎯 Chọn Chữ Trong Hierarchy", GUILayout.Height(26)))
            {
                manager.EnsureBackstoryUI();
                if (manager.BackstoryPromptText != null)
                {
                    Selection.activeGameObject = manager.BackstoryPromptText.gameObject;
                    EditorGUIUtility.PingObject(Selection.activeGameObject);
                }
            }
            if (GUILayout.Button("🎯 Chọn Ảnh Trong Hierarchy", GUILayout.Height(26)))
            {
                manager.EnsureBackstoryUI();
                if (manager.BackstoryIllustrationImage != null)
                {
                    Selection.activeGameObject = manager.BackstoryIllustrationImage.gameObject;
                    EditorGUIUtility.PingObject(Selection.activeGameObject);
                }
            }
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
