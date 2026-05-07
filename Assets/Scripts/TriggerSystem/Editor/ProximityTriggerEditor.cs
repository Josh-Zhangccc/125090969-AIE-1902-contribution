using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProximityTrigger))]
public class ProximityTriggerEditor : Editor
{
    private SerializedProperty checkIntervalProp;
    private SerializedProperty enableDebugLogProp;

    private bool showDetection = true;
    private bool showStatus = true;
    private bool showDebug = true;

    private void OnEnable()
    {
        checkIntervalProp  = serializedObject.FindProperty("checkInterval");
        enableDebugLogProp = serializedObject.FindProperty("enableDebugLog");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var trigger = (ProximityTrigger)target;

        // Detection section
        showDetection = EditorGUILayout.BeginFoldoutHeaderGroup(showDetection, "检测 / Detection");
        if (showDetection)
        {
            EditorGUILayout.PropertyField(checkIntervalProp);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(2);

        // Status section (read-only at runtime)
        showStatus = EditorGUILayout.BeginFoldoutHeaderGroup(showStatus, "状态 / Status");
        if (showStatus)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("Current Node", trigger.CurrentNodeId ?? "(none)");
            EditorGUI.EndDisabledGroup();

            if (Application.isPlaying && TourSessionManager.Instance != null)
            {
                var session = TourSessionManager.Instance;
                EditorGUILayout.LabelField("Visited", $"{session.InvalidatedCount} / {session.TotalVisibleCount}");
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(2);

        // Debug section
        showDebug = EditorGUILayout.BeginFoldoutHeaderGroup(showDebug, "调试 / Debug");
        if (showDebug)
        {
            EditorGUILayout.PropertyField(enableDebugLogProp);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
    }

    public override bool RequiresConstantRepaint()
    {
        return Application.isPlaying;
    }
}
