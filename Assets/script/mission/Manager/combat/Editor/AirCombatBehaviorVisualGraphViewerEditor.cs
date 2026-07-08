using UnityEditor;

[CustomEditor(typeof(AirCombatBehaviorVisualGraphViewer))]
public class AirCombatBehaviorVisualGraphViewerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("server"));
        serializedObject.ApplyModifiedProperties();

        AirCombatBehaviorServer server = ((AirCombatBehaviorVisualGraphViewer)target).Server;
        if (server == null)
        {
            EditorGUILayout.HelpBox("Assign an AirCombatBehaviorServer or Recorder.", MessageType.Info);
            return;
        }

        AirCombatBehaviorServerEditor.DrawVisualizations(server);
    }
}
