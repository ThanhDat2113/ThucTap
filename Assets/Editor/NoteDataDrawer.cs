using UnityEditor;
using UnityEngine;

namespace RhythmGame.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ChartData))]
    public class NoteDataDrawer : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var bpmProp = serializedObject.FindProperty("bpm");
            var offsetProp = serializedObject.FindProperty("firstNoteOffset");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("songName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("audioClip"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("musicStartDelay"));
            EditorGUILayout.PropertyField(bpmProp);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("laneCount"));
            EditorGUILayout.PropertyField(offsetProp);

            var notesProp = serializedObject.FindProperty("notes");

            float bpm = bpmProp.floatValue;
            float offset = offsetProp.floatValue;
            float secPerBeat = bpm > 0f ? 60f / bpm : 0f;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Notes", EditorStyles.boldLabel);

            var headerStyle = new GUIStyle(EditorStyles.label);
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Beat", headerStyle, GUILayout.Width(70));
            GUILayout.Label("Lane", headerStyle, GUILayout.Width(50));
            GUILayout.Label("Type", headerStyle, GUILayout.Width(60));
            GUILayout.Label("Hold", headerStyle, GUILayout.Width(50));
            GUILayout.Label("= sec", headerStyle, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            var timeStyle = new GUIStyle(EditorStyles.label);
            timeStyle.alignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < notesProp.arraySize; i++)
            {
                if (serializedObject.targetObjects.Length > 1)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(notesProp.GetArrayElementAtIndex(i), GUIContent.none);
                    EditorGUILayout.EndHorizontal();
                    continue;
                }

                var element = notesProp.GetArrayElementAtIndex(i);
                var beatProp = element.FindPropertyRelative("beat");
                var laneProp = element.FindPropertyRelative("lane");
                var typeProp = element.FindPropertyRelative("type");
                var holdProp = element.FindPropertyRelative("holdBeats");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(beatProp, GUIContent.none, GUILayout.Width(70));
                EditorGUILayout.PropertyField(laneProp, GUIContent.none, GUILayout.Width(50));
                EditorGUILayout.PropertyField(typeProp, GUIContent.none, GUILayout.Width(60));
                EditorGUILayout.PropertyField(holdProp, GUIContent.none, GUILayout.Width(50));

                float time = offset + beatProp.floatValue * secPerBeat;
                GUILayout.Label(time.ToString("F3"), timeStyle, GUILayout.Width(70));

                if (GUILayout.Button("–", GUILayout.Width(20)))
                {
                    notesProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Note"))
            {
                notesProp.InsertArrayElementAtIndex(notesProp.arraySize);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
