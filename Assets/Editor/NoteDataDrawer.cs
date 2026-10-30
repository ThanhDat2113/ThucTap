using UnityEditor;
using UnityEngine;

namespace RhythmGame.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ChartData))]
    public class ChartDataEditor : UnityEditor.Editor
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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Battle Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("opponentNoteRatio"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("opponentAccuracy"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("battleSplitMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sectionLengthBeats"));

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
            GUILayout.Label("Beat", headerStyle, GUILayout.Width(60));
            GUILayout.Label("Lane", headerStyle, GUILayout.Width(45));
            GUILayout.Label("Type", headerStyle, GUILayout.Width(55));
            GUILayout.Label("Hold", headerStyle, GUILayout.Width(45));
            GUILayout.Label("Owner", headerStyle, GUILayout.Width(60));
            GUILayout.Label("= sec", headerStyle, GUILayout.Width(60));
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
                var ownerProp = element.FindPropertyRelative("owner");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(beatProp, GUIContent.none, GUILayout.Width(60));
                EditorGUILayout.PropertyField(laneProp, GUIContent.none, GUILayout.Width(45));
                EditorGUILayout.PropertyField(typeProp, GUIContent.none, GUILayout.Width(55));
                EditorGUILayout.PropertyField(holdProp, GUIContent.none, GUILayout.Width(45));
                EditorGUILayout.PropertyField(ownerProp, GUIContent.none, GUILayout.Width(60));

                float time = offset + beatProp.floatValue * secPerBeat;
                GUILayout.Label(time.ToString("F3"), timeStyle, GUILayout.Width(60));

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

            EditorGUILayout.Space();
            if (GUILayout.Button("Generate Test Chart"))
            {
                ((ChartData)target).GenerateTestChart();
                serializedObject.Update();
            }

            if (GUILayout.Button("Generate Battle Chart"))
            {
                ((ChartData)target).GenerateBattleChart();
                serializedObject.Update();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Convert Existing Chart", EditorStyles.boldLabel);
            if (GUILayout.Button("Convert to Battle (Half Split)"))
            {
                ((ChartData)target).ConvertToBattleHalfSplit();
                serializedObject.Update();
            }

            if (GUILayout.Button("Convert to Battle (Alternating Sections)"))
            {
                ((ChartData)target).ConvertToBattleAlternating();
                serializedObject.Update();
            }

            if (GUILayout.Button("Balance Note Count"))
            {
                ((ChartData)target).BalanceNoteCount();
                serializedObject.Update();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}