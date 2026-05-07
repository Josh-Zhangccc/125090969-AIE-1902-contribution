using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单例，管理本次导览中的节点无效化状态。触发过的节点会被标记为失效，
/// 后续检测跳过。调用 Reset() 清除全部失效标记，开始新一次导览。
/// </summary>
[DefaultExecutionOrder(-200)]
public class TourSessionManager : MonoBehaviour
{
    public static TourSessionManager Instance { get; private set; }

    private readonly HashSet<string> invalidated = new();

    /// <summary>某节点被无效化时触发</summary>
    public event Action<string> OnInvalidated;

    /// <summary>导览重置时触发</summary>
    public event Action OnReset;

    /// <summary>已失效节点数</summary>
    public int InvalidatedCount => invalidated.Count;

    /// <summary>可见节点总数</summary>
    public int TotalVisibleCount
    {
        get
        {
            if (NodeRegistry.Instance == null) return 0;
            int count = 0;
            foreach (var _ in NodeRegistry.Instance.VisibleNodes) count++;
            return count;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[TourSessionManager] 场景中存在多个实例，保留第一个");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Reset();
    }

    /// <summary>标记节点为已触发（无效化）</summary>
    public void Invalidate(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return;
        if (invalidated.Add(nodeId))
            OnInvalidated?.Invoke(nodeId);
    }

    /// <summary>查询某节点是否已失效</summary>
    public bool IsInvalidated(string nodeId)
    {
        return invalidated.Contains(nodeId);
    }

    /// <summary>清除所有失效标记，开始新导览</summary>
    public void Reset()
    {
        invalidated.Clear();
        OnReset?.Invoke();
    }

    /// <summary>返回尚未被访问的可见节点 ID 列表</summary>
    public IReadOnlyList<string> GetUnvisitedVisibleNodes()
    {
        var result = new List<string>();
        if (NodeRegistry.Instance == null) return result;
        foreach (var node in NodeRegistry.Instance.VisibleNodes)
        {
            if (!invalidated.Contains(node.id))
                result.Add(node.id);
        }
        return result;
    }
}
