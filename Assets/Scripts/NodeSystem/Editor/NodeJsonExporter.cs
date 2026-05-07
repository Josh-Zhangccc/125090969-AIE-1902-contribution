using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NodeJsonExporter
{
    private static string OutputPath => $"Assets/StreamingAssets/nodes.{EditorSceneManager.GetActiveScene().name}.json";

    [InitializeOnLoadMethod]
    private static void RegisterAutoExport()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            ExportInternal(false);
    }

    [MenuItem("Tools/Tour System/Export Nodes to JSON")]
    public static void Export()
    {
        ExportInternal(true);
    }

    private static void ExportInternal(bool interactive)
    {
        var components = UnityEngine.Object.FindObjectsByType<NodeComponent>(FindObjectsSortMode.None);
        if (components.Length == 0)
        {
            if (interactive)
                EditorUtility.DisplayDialog("Export Nodes", "场景中未找到任何 NodeComponent。", "确定");
            else
                Debug.LogWarning("[Export] 场景中未找到任何 NodeComponent，跳过自动导出");
            return;
        }

        // Validate: duplicate IDs
        var seen = new HashSet<string>();
        bool hasError = false;
        foreach (var comp in components)
        {
            if (string.IsNullOrWhiteSpace(comp.id))
            {
                Debug.LogError($"[Export] {comp.name}: id 为空", comp);
                hasError = true;
                continue;
            }
            if (!seen.Add(comp.id))
            {
                Debug.LogError($"[Export] id \"{comp.id}\" 重复: {comp.name}", comp);
                hasError = true;
            }
        }

        if (hasError)
        {
            if (interactive)
            {
                if (!EditorUtility.DisplayDialog("Export Nodes",
                    "存在验证错误（见 Console），是否仍然导出？", "仍然导出", "取消"))
                    return;
            }
            else
            {
                Debug.LogWarning("[Export] 存在验证错误，但仍自动导出。详见上方错误日志。");
            }
        }

        var dtoList = new List<NodeJsonDto>();
        foreach (var comp in components)
            dtoList.Add(ToDto(comp));

        var root = new NodeListWrapper { nodes = dtoList };
        string json = JsonUtility.ToJson(root, true);

        var dir = Path.GetDirectoryName(OutputPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(OutputPath, json);
        AssetDatabase.Refresh();

        Debug.Log($"[Export] {(interactive ? "" : "自动")}已导出 {dtoList.Count} 个节点到 {OutputPath}");
        if (interactive)
            EditorUtility.DisplayDialog("Export Nodes",
                $"成功导出 {dtoList.Count} 个节点到:\n{OutputPath}", "确定");
    }

    private static NodeJsonDto ToDto(NodeComponent comp)
    {
        return new NodeJsonDto
        {
            id = comp.id,
            displayName = comp.displayName,
            shortDescription = comp.shortDescription,
            campusLocation = comp.campusLocation,
            isStartingNode = comp.isStartingNode,
            isVisible = comp.isVisible,
            position = Vec3ToJson(comp.transform.position),
            rotation = Vec3ToJson(comp.transform.eulerAngles),
            connectedNodes = new ConnectedNodesWrapper
            {
                to = ConvertConnectedTo(comp.connectedTo)
            },
            radius = comp.radius,
            thumbnail = comp.thumbnail,
            text = new BilingualTextWrapper
            {
                zh = new TextEntry { title = comp.zhText.title, file = comp.zhText.file },
                en = new TextEntry { title = comp.enText.title, file = comp.enText.file }
            },
            images = ConvertImages(comp.images),
            audio = new BilingualAudioWrapper
            {
                zh = new AudioEntry { file = comp.zhAudio.file, volume = comp.zhAudio.volume, autoPlay = comp.zhAudio.autoPlay },
                en = new AudioEntry { file = comp.enAudio.file, volume = comp.enAudio.volume, autoPlay = comp.enAudio.autoPlay }
            },
            cameraOverride = new CameraOverrideDto
            {
                enabled = comp.cameraOverride.enabled,
                target = Vec3ToJson(comp.cameraOverride.target),
                blendTime = comp.cameraOverride.blendTime,
                stayOnTarget = comp.cameraOverride.stayOnTarget,
                restoreAfterSeconds = comp.cameraOverride.restoreAfterSeconds
            },
            autoDismissDuration = comp.autoDismissDuration
        };
    }

    private static List<ConnectedNodeDto> ConvertConnectedTo(ConnectedNode[] arr)
    {
        var list = new List<ConnectedNodeDto>();
        if (arr == null) return list;
        foreach (var c in arr)
            list.Add(new ConnectedNodeDto { id = c.id, required = c.required });
        return list;
    }

    private static List<ImageEntry> ConvertImages(NodeImage[] arr)
    {
        var list = new List<ImageEntry>();
        if (arr == null) return list;
        foreach (var img in arr)
            list.Add(new ImageEntry { file = img.file, description = img.description });
        return list;
    }

    private static Vec3Json Vec3ToJson(Vector3 v) =>
        new Vec3Json { x = v.x, y = v.y, z = v.z };
}

// ── DTO classes (match JSON schema exactly) ──

    [Serializable]
    internal class NodeListWrapper { public List<NodeJsonDto> nodes; }

    [Serializable]
    internal class NodeJsonDto
    {
        public string id;
        public LocalizedString displayName;
        public LocalizedString shortDescription;
        public string campusLocation;
        public bool isStartingNode;
        public bool isVisible;
        public Vec3Json position;
        public Vec3Json rotation;
        public ConnectedNodesWrapper connectedNodes;
        public float radius;
        public string thumbnail;
        public BilingualTextWrapper text;
        public List<ImageEntry> images;
        public BilingualAudioWrapper audio;
        public CameraOverrideDto cameraOverride;
        public float autoDismissDuration;
    }

    [Serializable]
    internal class Vec3Json { public float x; public float y; public float z; }

    [Serializable]
    internal class ConnectedNodesWrapper { public List<ConnectedNodeDto> to; }

    [Serializable]
    internal class ConnectedNodeDto { public string id; public bool required; }

    [Serializable]
    internal class BilingualTextWrapper { public TextEntry zh; public TextEntry en; }

    [Serializable]
    internal class TextEntry { public string title; public string file; }

    [Serializable]
    internal class ImageEntry { public string file; public LocalizedString description; }

    [Serializable]
    internal class BilingualAudioWrapper { public AudioEntry zh; public AudioEntry en; }

    [Serializable]
    internal class AudioEntry { public string file; public float volume; public bool autoPlay; }

    [Serializable]
    internal class CameraOverrideDto
    {
        public bool enabled;
        public Vec3Json target;
        public float blendTime;
        public bool stayOnTarget;
        public float restoreAfterSeconds;
    }
