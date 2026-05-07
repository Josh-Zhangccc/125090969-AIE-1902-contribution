using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class NodeRegistry : MonoBehaviour
{
    public static NodeRegistry Instance { get; private set; }

    private readonly Dictionary<string, NodeComponent> nodes = new();
    private readonly Dictionary<string, List<string>> derivedFrom = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[NodeRegistry] 场景中存在多个 NodeRegistry，保留第一个");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RebuildDerivedFrom();
    }

    /// <summary>注册节点（由 NodeComponent.Start 调用）</summary>
    public void Register(NodeComponent comp)
    {
        if (string.IsNullOrWhiteSpace(comp.id))
        {
            Debug.LogError($"[NodeRegistry] 节点 {comp.name} 的 id 为空，跳过注册", comp);
            return;
        }
        if (nodes.ContainsKey(comp.id))
        {
            Debug.LogError($"[NodeRegistry] 节点 id \"{comp.id}\" 重复（{comp.name}），跳过注册", comp);
            return;
        }
        nodes[comp.id] = comp;
    }

    /// <summary>注销节点（由 NodeComponent.OnDestroy 调用）</summary>
    public void Unregister(NodeComponent comp)
    {
        nodes.Remove(comp.id);
    }

    /// <summary>根据所有节点的 to 反向推导 from</summary>
    public void RebuildDerivedFrom()
    {
        derivedFrom.Clear();
        foreach (var (nodeId, comp) in nodes)
        {
            if (comp.connectedTo == null) continue;
            foreach (var conn in comp.connectedTo)
            {
                if (!derivedFrom.ContainsKey(conn.id))
                    derivedFrom[conn.id] = new List<string>();
                derivedFrom[conn.id].Add(nodeId);
            }
        }
    }

    /// <summary>获取某节点的上游节点 ID 列表（运行时从 to 推导）</summary>
    public IReadOnlyList<string> GetFromNodes(string nodeId)
    {
        return derivedFrom.TryGetValue(nodeId, out var list) ? list : System.Array.Empty<string>();
    }

    /// <summary>获取节点组件</summary>
    public NodeComponent GetComponentForNode(string id)
    {
        nodes.TryGetValue(id, out var comp);
        return comp;
    }

    /// <summary>获取节点纯数据</summary>
    public NodeData GetNodeData(string id)
    {
        return nodes.TryGetValue(id, out var comp) ? comp.ToNodeData() : null;
    }

    /// <summary>所有已注册节点</summary>
    public IEnumerable<NodeComponent> AllNodes => nodes.Values;

    /// <summary>所有可见节点（用于 UI）</summary>
    public IEnumerable<NodeComponent> VisibleNodes => nodes.Values.Where(n => n.isVisible);

    /// <summary>所有起点节点</summary>
    public IEnumerable<NodeComponent> StartingNodes => nodes.Values.Where(n => n.isStartingNode);

    /// <summary>按距离查找最近节点</summary>
    public NodeComponent GetNearestNode(Vector3 pos)
    {
        NodeComponent nearest = null;
        float minDist = float.MaxValue;
        foreach (var comp in nodes.Values)
        {
            float d = Vector3.Distance(pos, comp.Position);
            if (d < minDist)
            {
                minDist = d;
                nearest = comp;
            }
        }
        return nearest;
    }

    /// <summary>按园区筛选</summary>
    public IEnumerable<NodeComponent> GetNodesByCampus(string campus)
    {
        return nodes.Values.Where(n => n.campusLocation == campus);
    }

    /// <summary>导出所有节点为纯数据列表</summary>
    public List<NodeData> ExportAllNodeData()
    {
        RebuildDerivedFrom();
        var list = new List<NodeData>();
        foreach (var comp in nodes.Values)
            list.Add(comp.ToNodeData());
        return list;
    }
}
