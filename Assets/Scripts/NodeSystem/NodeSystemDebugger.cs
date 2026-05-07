using System.Collections;
using System.Text;
using UnityEngine;

/// <summary>
/// 挂载到 NodeRegistry 所在 GameObject。
/// 在所有 NodeComponent 注册完成后打印已注册节点 id 列表。
/// </summary>
[DefaultExecutionOrder(100)]
public class NodeSystemDebugger : MonoBehaviour
{
    private IEnumerator Start()
    {
        // 等待一帧确保所有 NodeComponent.Start() 已执行完毕
        yield return null;

        var registry = NodeRegistry.Instance;
        if (registry == null)
        {
            Debug.LogError("[NodeSystemDebugger] 场景中未找到 NodeRegistry.Instance");
            yield break;
        }

        var sb = new StringBuilder();
        sb.AppendLine("[NodeSystemDebugger] 已注册节点 id 列表：");

        int count = 0;
        foreach (var node in registry.AllNodes)
        {
            sb.AppendLine($"  [{count}] {node.id}  (GameObject: {node.name})");
            count++;
        }

        if (count == 0)
            sb.AppendLine("  (无 — 请检查各 NodeComponent 的 id 字段是否已填写)");

        Debug.Log(sb.ToString());
    }
}
