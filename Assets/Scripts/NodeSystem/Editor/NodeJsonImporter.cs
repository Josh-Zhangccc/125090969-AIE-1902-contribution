using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor 工具：从 nodes.{scene}.json 反向创建场景中的节点。
/// 菜单：Tools > Tour System > Import Nodes from JSON
/// </summary>
public static class NodeJsonImporter
{
    private const float SphereScale = 0.5f;
    private const float InvisibleSphereScale = 0.5f;

    [MenuItem("Tools/Tour System/Import Nodes from JSON")]
    public static void Import()
    {
        string path = EditorUtility.OpenFilePanel("选择节点 JSON 文件", Application.streamingAssetsPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Import] 读取文件失败: {e.Message}");
            EditorUtility.DisplayDialog("导入节点", $"读取文件失败:\n{e.Message}", "确定");
            return;
        }

        var wrapper = JsonUtility.FromJson<NodeListWrapper>(json);
        if (wrapper?.nodes == null || wrapper.nodes.Count == 0)
        {
            EditorUtility.DisplayDialog("导入节点", "JSON 中没有节点数据。", "确定");
            return;
        }

        // 冲突检测
        var existing = UnityEngine.Object.FindObjectsByType<NodeComponent>(FindObjectsSortMode.None);
        var existingIds = new HashSet<string>();
        foreach (var c in existing)
            if (!string.IsNullOrEmpty(c.id))
                existingIds.Add(c.id);

        var conflictIds = new HashSet<string>();
        foreach (var n in wrapper.nodes)
            if (existingIds.Contains(n.id))
                conflictIds.Add(n.id);

        int strategy = 0; // 0 = skip, 1 = cancel, 2 = replace
        if (conflictIds.Count > 0)
        {
            strategy = EditorUtility.DisplayDialogComplex("ID 冲突",
                $"场景中已有 {conflictIds.Count} 个相同 ID 的节点：\n{string.Join(", ", conflictIds)}",
                "跳过已存在", "取消", "替换已存在");
            if (strategy == 1) return; // cancel
        }

        // 替换模式：先删冲突节点
        if (strategy == 2 && conflictIds.Count > 0)
        {
            foreach (var c in existing)
            {
                if (conflictIds.Contains(c.id))
                    Undo.DestroyObjectImmediate(c.gameObject);
            }
        }

        // 确保 Nodes 父对象
        var nodesParent = GameObject.Find("Nodes");
        if (nodesParent == null)
        {
            nodesParent = new GameObject("Nodes");
            Undo.RegisterCreatedObjectUndo(nodesParent, "Import Nodes");
        }

        var created = new List<GameObject>();
        foreach (var node in wrapper.nodes)
        {
            if (string.IsNullOrWhiteSpace(node.id))
            {
                Debug.LogWarning("[Import] 跳过 id 为空的节点");
                continue;
            }

            if (strategy == 0 && conflictIds.Contains(node.id))
            {
                Debug.Log($"[Import] 跳过已存在: {node.id}");
                continue;
            }

            try
            {
                var go = CreateNodeGameObject(node, nodesParent.transform);
                created.Add(go);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Import] 创建节点 \"{node.id}\" 失败: {e.Message}");
            }
        }

        if (created.Count > 0)
            Selection.objects = created.ToArray();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        int skipped = wrapper.nodes.Count - created.Count;
        string msg = $"成功导入 {created.Count} 个节点到 \"Nodes\" 父对象下。";
        if (skipped > 0) msg += $"\n跳过 {skipped} 个节点。";

        Debug.Log($"[Import] {msg}");
        EditorUtility.DisplayDialog("导入节点", msg, "确定");
    }

    private static GameObject CreateNodeGameObject(NodeJsonDto node, Transform parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = node.id;
        go.transform.SetParent(parent);
        go.transform.position = Vec3FromJson(node.position);
        go.transform.eulerAngles = Vec3FromJson(node.rotation);
        go.transform.localScale = Vector3.one * (node.isVisible ? SphereScale : InvisibleSphereScale);

        // 移除球体自带的碰撞体（导航系统用 radius 做距离检测，不需要物理碰撞）
        var collider = go.GetComponent<SphereCollider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);

        // 材质颜色：可见 = 绿，不可见 = 蓝
        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = node.isVisible
                ? new Color(0.3f, 0.8f, 0.3f)
                : new Color(0.3f, 0.5f, 0.9f);
            renderer.sharedMaterial = mat;
        }

        Undo.RegisterCreatedObjectUndo(go, "Import Nodes");

        var comp = go.AddComponent<NodeComponent>();
        PopulateComponent(comp, node);

        return go;
    }

    private static void PopulateComponent(NodeComponent comp, NodeJsonDto node)
    {
        comp.id = node.id;
        comp.displayName = node.displayName;
        comp.shortDescription = node.shortDescription;
        comp.campusLocation = string.IsNullOrEmpty(node.campusLocation) ? "lower" : node.campusLocation;
        comp.isStartingNode = node.isStartingNode;
        comp.isVisible = node.isVisible;
        comp.radius = node.radius > 0f ? node.radius : 5f;
        comp.autoDismissDuration = node.autoDismissDuration;
        comp.thumbnail = node.thumbnail;

        // text
        if (node.text?.zh != null)
            comp.zhText = new LocalizedText { title = node.text.zh.title ?? "", file = node.text.zh.file ?? "" };
        if (node.text?.en != null)
            comp.enText = new LocalizedText { title = node.text.en.title ?? "", file = node.text.en.file ?? "" };

        // images
        if (node.images != null && node.images.Count > 0)
        {
            comp.images = new NodeImage[node.images.Count];
            for (int i = 0; i < node.images.Count; i++)
                comp.images[i] = new NodeImage { file = node.images[i].file, description = node.images[i].description };
        }

        // audio
        if (node.audio?.zh != null)
            comp.zhAudio = new LocalizedAudio { file = node.audio.zh.file ?? "", volume = node.audio.zh.volume, autoPlay = node.audio.zh.autoPlay };
        if (node.audio?.en != null)
            comp.enAudio = new LocalizedAudio { file = node.audio.en.file ?? "", volume = node.audio.en.volume, autoPlay = node.audio.en.autoPlay };

        // connectedTo
        if (node.connectedNodes?.to != null && node.connectedNodes.to.Count > 0)
        {
            comp.connectedTo = new ConnectedNode[node.connectedNodes.to.Count];
            for (int i = 0; i < node.connectedNodes.to.Count; i++)
                comp.connectedTo[i] = new ConnectedNode { id = node.connectedNodes.to[i].id, required = node.connectedNodes.to[i].required };
        }

        // cameraOverride
        if (node.cameraOverride != null)
        {
            comp.cameraOverride = new CameraOverrideData
            {
                enabled = node.cameraOverride.enabled,
                target = Vec3FromJson(node.cameraOverride.target),
                blendTime = node.cameraOverride.blendTime,
                stayOnTarget = node.cameraOverride.stayOnTarget,
                restoreAfterSeconds = node.cameraOverride.restoreAfterSeconds
            };
        }
    }

    private static Vector3 Vec3FromJson(Vec3Json v)
    {
        if (v == null) return Vector3.zero;
        return new Vector3(v.x, v.y, v.z);
    }
}
