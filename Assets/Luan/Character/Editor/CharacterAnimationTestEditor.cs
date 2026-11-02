using UnityEditor;
using UnityEngine;

namespace Luan.Character.Editor
{
    [CustomEditor(typeof(CharacterAnimationTest))]
    public class CharacterAnimationTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var test = (CharacterAnimationTest)target;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🎭 BẢNG KIỂM TRA FRAME NHÂN VẬT (CHARACTER PREVIEW)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Công cụ kiểm tra chuỗi frame chuyển động cho 2 nhân vật: Riff và Lumi (4 điệu nhảy + 1 miss cho mỗi nhân vật).", MessageType.Info);
            EditorGUILayout.Space(4);

            // Nút nạp tự động toàn bộ frames
            GUI.backgroundColor = new Color(0.2f, 0.85f, 0.4f);
            if (GUILayout.Button("⚡ Nạp Lại Toàn Bộ Frame Tự Động (Auto-Load All Frames)", GUILayout.Height(36)))
            {
                test.AutoLoadAllFrames();
                EditorUtility.SetDirty(test);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(8);

            // Hiển thị Inspector mặc định
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("🎮 ĐIỀU KHIỂN CHUYỂN ĐỘNG TRONG PLAY MODE", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Bấm các phím 1-8 trên bàn phím hoặc dùng phím M để test Miss, hoặc bấm trực tiếp các nút trên màn hình Game.", MessageType.None);
            }
        }
    }
}

