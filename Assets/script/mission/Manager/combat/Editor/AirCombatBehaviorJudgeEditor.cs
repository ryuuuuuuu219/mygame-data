using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AirCombatBehaviorJudge))]
[CanEditMultipleObjects]
public class AirCombatBehaviorJudgeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Evaluate"))
        {
            Undo.RecordObjects(targets, "Evaluate Air Combat Behavior");
            for (int i = 0; i < targets.Length; i++)
            {
                AirCombatBehaviorJudge selectedJudge = (AirCombatBehaviorJudge)targets[i];
                selectedJudge.Evaluate();
                EditorUtility.SetDirty(selectedJudge);
            }
        }

        AirCombatBehaviorJudge judge = (AirCombatBehaviorJudge)target;
        if (string.IsNullOrEmpty(judge.LatestResult))
            return;

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(
            judge.LatestResult,
            EditorStyles.textArea,
            GUILayout.MinHeight(64f));

        if (GUILayout.Button("Copy Result"))
            EditorGUIUtility.systemCopyBuffer = judge.LatestResult;
    }
}
