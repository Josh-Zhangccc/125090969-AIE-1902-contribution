using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NodeComponent)), CanEditMultipleObjects]
public class NodeComponentEditor : Editor
{
    private SerializedProperty idProp;
    private SerializedProperty displayNameProp;
    private SerializedProperty shortDescriptionProp;
    private SerializedProperty campusLocationProp;
    private SerializedProperty isStartingNodeProp;
    private SerializedProperty isVisibleProp;
    private SerializedProperty radiusProp;
    private SerializedProperty autoDismissDurationProp;
    private SerializedProperty connectedToProp;
    private SerializedProperty thumbnailProp;
    private SerializedProperty zhTextProp;
    private SerializedProperty enTextProp;
    private SerializedProperty imagesProp;
    private SerializedProperty zhAudioProp;
    private SerializedProperty enAudioProp;
    private SerializedProperty cameraOverrideProp;

    private bool showIdentity = true;
    private bool showNavigation = true;
    private bool showText = true;
    private bool showMedia = true;
    private bool showAudio = true;
    private bool showCamera = true;

    private void OnEnable()
    {
        idProp               = serializedObject.FindProperty("id");
        displayNameProp      = serializedObject.FindProperty("displayName");
        shortDescriptionProp = serializedObject.FindProperty("shortDescription");
        campusLocationProp   = serializedObject.FindProperty("campusLocation");
        isStartingNodeProp   = serializedObject.FindProperty("isStartingNode");
        isVisibleProp        = serializedObject.FindProperty("isVisible");
        radiusProp           = serializedObject.FindProperty("radius");
        autoDismissDurationProp = serializedObject.FindProperty("autoDismissDuration");
        connectedToProp      = serializedObject.FindProperty("connectedTo");
        thumbnailProp        = serializedObject.FindProperty("thumbnail");
        zhTextProp           = serializedObject.FindProperty("zhText");
        enTextProp           = serializedObject.FindProperty("enText");
        imagesProp           = serializedObject.FindProperty("images");
        zhAudioProp          = serializedObject.FindProperty("zhAudio");
        enAudioProp          = serializedObject.FindProperty("enAudio");
        cameraOverrideProp   = serializedObject.FindProperty("cameraOverride");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var comp = (NodeComponent)target;

        // Header: type badge
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Node", EditorStyles.boldLabel, GUILayout.Width(50));
        var badge = comp.isVisible ? (comp.isStartingNode ? "POI · 起点" : "POI") : "隐形 · 路径";
        var badgeColor = comp.isVisible
            ? (comp.isStartingNode ? new Color(0.2f, 0.7f, 0.2f) : new Color(0.4f, 0.6f, 0.9f))
            : new Color(0.6f, 0.4f, 0.9f);
        var origColor = GUI.backgroundColor;
        GUI.backgroundColor = badgeColor;
        EditorGUILayout.LabelField(badge, new GUIStyle(EditorStyles.miniButton) { fixedWidth = 80 });
        GUI.backgroundColor = origColor;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // Identity section
        showIdentity = EditorGUILayout.BeginFoldoutHeaderGroup(showIdentity, "标识 / Identity");
        if (showIdentity)
        {
            EditorGUILayout.PropertyField(idProp);
            DrawLocalizedString(displayNameProp);
            DrawLocalizedString(shortDescriptionProp);
            EditorGUILayout.PropertyField(campusLocationProp);
            EditorGUILayout.PropertyField(isStartingNodeProp);
            EditorGUILayout.PropertyField(isVisibleProp);
            if (!comp.isVisible)
                EditorGUILayout.HelpBox("隐形节点不会出现在 UI 和地图中，仅用于路径约束和沿路触发", MessageType.Info);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(2);

        // Trigger section
        EditorGUILayout.PropertyField(radiusProp);
        EditorGUILayout.PropertyField(autoDismissDurationProp, new GUIContent("Auto Dismiss Duration"));
        if (!comp.isVisible && autoDismissDurationProp.floatValue <= 0f)
            EditorGUILayout.HelpBox("建议为隐形节点设置 Auto Dismiss Duration > 0，以自动关闭字幕 UI", MessageType.Warning);

        // Position (read-only, from Transform)
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.Vector3Field("Position (from Transform)", comp.transform.position);
        EditorGUILayout.Vector3Field("Rotation (from Transform)", comp.transform.eulerAngles);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(2);

        // Navigation section — match JSON: connectedNodes.to[]
        showNavigation = EditorGUILayout.BeginFoldoutHeaderGroup(showNavigation, "导航 / Navigation  (connectedNodes.to[])");
        if (showNavigation)
        {
            DrawArray_ConnectedTo(connectedToProp);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(2);

        // Thumbnail
        EditorGUILayout.PropertyField(thumbnailProp);

        // Text section — match JSON: text.zh / text.en
        showText = EditorGUILayout.BeginFoldoutHeaderGroup(showText, "文本 / Text");
        if (showText)
        {
            DrawLocalizedTextField(zhTextProp, "text.zh");
            DrawLocalizedTextField(enTextProp, "text.en");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(2);

        // Images section — match JSON: images[]
        showMedia = EditorGUILayout.BeginFoldoutHeaderGroup(showMedia, "图片 / Images  (images[])");
        if (showMedia)
        {
            DrawArray_Images(imagesProp);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(2);

        // Audio section — match JSON: audio.zh / audio.en
        showAudio = EditorGUILayout.BeginFoldoutHeaderGroup(showAudio, "音频 / Audio");
        if (showAudio)
        {
            DrawLocalizedAudioField(zhAudioProp, "audio.zh");
            DrawLocalizedAudioField(enAudioProp, "audio.en");
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(2);

        // Camera Override section — match JSON: cameraOverride
        showCamera = EditorGUILayout.BeginFoldoutHeaderGroup(showCamera, "视角控制 / Camera Override  (cameraOverride)");
        if (showCamera)
        {
            EditorGUILayout.PropertyField(cameraOverrideProp.FindPropertyRelative("enabled"));
            if (cameraOverrideProp.FindPropertyRelative("enabled").boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(cameraOverrideProp.FindPropertyRelative("target"));
                EditorGUILayout.PropertyField(cameraOverrideProp.FindPropertyRelative("blendTime"));
                EditorGUILayout.PropertyField(cameraOverrideProp.FindPropertyRelative("stayOnTarget"), new GUIContent("Stay On Target"));
                if (!cameraOverrideProp.FindPropertyRelative("stayOnTarget").boolValue)
                {
                    EditorGUILayout.PropertyField(cameraOverrideProp.FindPropertyRelative("restoreAfterSeconds"), new GUIContent("Restore After Seconds"));
                    if (cameraOverrideProp.FindPropertyRelative("restoreAfterSeconds").floatValue <= 0f)
                        EditorGUILayout.HelpBox("restoreAfterSeconds 为 0 时相机转向后将立即恢复。建议设为 > 0", MessageType.Warning);
                }
                EditorGUI.indentLevel--;
            }
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(6);

        // Validate button
        if (GUILayout.Button("Validate", GUILayout.Height(24)))
        {
            ValidateNode(comp);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ── Draw helpers ─────────────────────────────────────────────

    private static void DrawLocalizedString(SerializedProperty prop)
    {
        prop.isExpanded = EditorGUILayout.Foldout(prop.isExpanded, prop.displayName);
        if (prop.isExpanded)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("zh"), new GUIContent("zh"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("en"), new GUIContent("en"));
            EditorGUI.indentLevel--;
        }
    }

    private static void DrawLocalizedTextField(SerializedProperty prop, string jsonPath)
    {
        prop.isExpanded = EditorGUILayout.Foldout(prop.isExpanded, jsonPath);
        if (prop.isExpanded)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("title"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("file"));
            EditorGUI.indentLevel--;
        }
    }

    private static void DrawLocalizedAudioField(SerializedProperty prop, string jsonPath)
    {
        prop.isExpanded = EditorGUILayout.Foldout(prop.isExpanded, jsonPath);
        if (prop.isExpanded)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("file"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("volume"));
            EditorGUILayout.PropertyField(prop.FindPropertyRelative("autoPlay"));
            EditorGUI.indentLevel--;
        }
    }

    // ── Array drawers — matching JSON schema ─────────────────────

    private void DrawArray_ConnectedTo(SerializedProperty arrayProp)
    {
        // connectedNodes.to[] → each element: { id, required }
        EditorGUILayout.LabelField("connectedNodes.to", EditorStyles.miniLabel);

        int size = arrayProp.arraySize;
        EditorGUI.BeginChangeCheck();
        size = EditorGUILayout.IntField("Size", size);
        if (EditorGUI.EndChangeCheck())
            arrayProp.arraySize = size;

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            var elem = arrayProp.GetArrayElementAtIndex(i);
            var elemIdProp = elem.FindPropertyRelative("id");
            var elemRequiredProp = elem.FindPropertyRelative("required");

            string label = string.IsNullOrEmpty(elemIdProp.stringValue)
                ? $"to[{i}]"
                : $"to[{i}]  →  {elemIdProp.stringValue}";

            elem.isExpanded = EditorGUILayout.Foldout(elem.isExpanded, label);
            if (elem.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(elemIdProp, new GUIContent("id"));

                // Show connected GameObject name if found in scene
                if (!string.IsNullOrEmpty(elemIdProp.stringValue))
                {
                    var foundName = FindNodeNameById(elemIdProp.stringValue);
                    if (!string.IsNullOrEmpty(foundName))
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.LabelField("→ scene object", foundName, EditorStyles.miniLabel);
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.PropertyField(elemRequiredProp, new GUIContent("required"));
                EditorGUI.indentLevel--;
            }
        }
    }

    private static void DrawArray_Images(SerializedProperty arrayProp)
    {
        // images[] → each element: { file, description: { zh, en } }
        EditorGUILayout.LabelField("images", EditorStyles.miniLabel);

        int size = arrayProp.arraySize;
        EditorGUI.BeginChangeCheck();
        size = EditorGUILayout.IntField("Size", size);
        if (EditorGUI.EndChangeCheck())
            arrayProp.arraySize = size;

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            var elem = arrayProp.GetArrayElementAtIndex(i);
            var fileProp = elem.FindPropertyRelative("file");

            string label = string.IsNullOrEmpty(fileProp.stringValue)
                ? $"images[{i}]"
                : $"images[{i}]  →  {fileProp.stringValue}";

            elem.isExpanded = EditorGUILayout.Foldout(elem.isExpanded, label);
            if (elem.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(fileProp, new GUIContent("file"));

                var descProp = elem.FindPropertyRelative("description");
                descProp.isExpanded = EditorGUILayout.Foldout(descProp.isExpanded, "description");
                if (descProp.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(descProp.FindPropertyRelative("zh"), new GUIContent("zh"));
                    EditorGUILayout.PropertyField(descProp.FindPropertyRelative("en"), new GUIContent("en"));
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    private string FindNodeNameById(string nodeId)
    {
        var allComps = FindObjectsByType<NodeComponent>(FindObjectsSortMode.None);
        foreach (var c in allComps)
        {
            if (c.id == nodeId)
                return c.name;
        }
        return null;
    }

    private void ValidateNode(NodeComponent comp)
    {
        bool ok = true;

        if (string.IsNullOrWhiteSpace(comp.id))
        {
            Debug.LogError($"[Validate] {comp.name}: id 为空", comp);
            ok = false;
        }
        if (comp.isVisible)
        {
            if (string.IsNullOrWhiteSpace(comp.displayName.zh) && string.IsNullOrWhiteSpace(comp.displayName.en))
            {
                Debug.LogWarning($"[Validate] {comp.name}: displayName 未填写", comp);
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(comp.thumbnail))
                Debug.LogWarning($"[Validate] {comp.name}: thumbnail 为空", comp);
        }
        else
        {
            if (comp.autoDismissDuration <= 0f)
                Debug.LogWarning($"[Validate] {comp.name}: 隐形节点建议设置 autoDismissDuration > 0", comp);
        }

        // Check duplicate IDs in scene
        var allComps = FindObjectsByType<NodeComponent>(FindObjectsSortMode.None);
        foreach (var other in allComps)
        {
            if (other != comp && other.id == comp.id)
            {
                Debug.LogError($"[Validate] id \"{comp.id}\" 重复: {comp.name} 与 {other.name}", comp);
                ok = false;
            }
        }

        // Check connected IDs exist
        if (comp.connectedTo != null)
        {
            foreach (var conn in comp.connectedTo)
            {
                bool found = false;
                foreach (var other in allComps)
                {
                    if (other.id == conn.id) { found = true; break; }
                }
                if (!found)
                    Debug.LogWarning($"[Validate] {comp.name}: 连接的节点 \"{conn.id}\" 在场景中未找到", comp);
            }
        }

        if (ok)
            Debug.Log($"[Validate] {comp.name}: 通过 ✓", comp);
    }
}
