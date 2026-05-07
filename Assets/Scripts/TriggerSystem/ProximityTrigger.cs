using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 挂载到玩家，按间隔检测与最近有效节点的距离。进入/离开节点半径时发出事件，
/// 并在进入时自动调用 TourSessionManager.Invalidate 标记该节点。
/// </summary>
public class ProximityTrigger : MonoBehaviour
{
    [Header("Detection")]
    [Min(0.05f)]
    public float checkInterval = 0.25f;

    [Header("Debug")]
    public bool enableDebugLog = true;

    /// <summary>进入节点时触发（EventArgs.NodeId 为新节点 ID）</summary>
    public event EventHandler<NodeTriggerEventArgs> OnNodeEnter;

    /// <summary>离开节点时触发（EventArgs.NodeId 为旧节点 ID）</summary>
    public event EventHandler<NodeTriggerEventArgs> OnNodeExit;

    /// <summary>进入可见节点范围但未触发（等待玩家按键确认）</summary>
    public event EventHandler<NodeTriggerEventArgs> OnNodeApproached;

    /// <summary>离开可见节点范围（未触发状态下）</summary>
    public event Action OnApproachCleared;

    /// <summary>所有可见节点均已触发过</summary>
    public event Action OnAllVisited;

    /// <summary>当前所处节点的 ID，不在任何节点内时为 null</summary>
    public string CurrentNodeId { get; private set; }

    /// <summary>当前靠近但未触发的可见节点，等待玩家按键确认</summary>
    public NodeComponent ApproachedNodeComponent { get; private set; }

    /// <summary>是否有可见节点处于 approached 状态</summary>
    public bool HasApproachedNode => ApproachedNodeComponent != null;

    private Coroutine checkRoutine;
    private bool allVisitedFired;

    private void OnEnable()
    {
        allVisitedFired = false;
        checkRoutine = StartCoroutine(CheckLoop());
    }

    private void OnDisable()
    {
        if (checkRoutine != null)
        {
            StopCoroutine(checkRoutine);
            checkRoutine = null;
        }
    }

    private IEnumerator CheckLoop()
    {
        var wait = new WaitForSeconds(checkInterval);
        while (isActiveAndEnabled)
        {
            Check();
            yield return wait;
        }
    }

    private void Check()
    {
        if (TourSessionManager.Instance == null || NodeRegistry.Instance == null)
            return;

        string insideId = null;
        NodeComponent insideComp = null;
        bool isApproach = false;

        // 优先检查是否仍在当前已触发节点内
        if (CurrentNodeId != null)
        {
            var currentComp = NodeRegistry.Instance.GetComponentForNode(CurrentNodeId);
            if (currentComp != null)
            {
                float dist = Vector3.Distance(transform.position, currentComp.Position);
                if (dist <= currentComp.radius)
                {
                    insideId = CurrentNodeId;
                    insideComp = currentComp;
                }
            }
        }

        // 其次检查是否仍在 approached（未触发）节点内
        if (insideId == null && ApproachedNodeComponent != null)
        {
            float dist = Vector3.Distance(transform.position, ApproachedNodeComponent.Position);
            if (dist <= ApproachedNodeComponent.radius)
            {
                insideId = ApproachedNodeComponent.id;
                insideComp = ApproachedNodeComponent;
                isApproach = true;
            }
        }

        // 不在任何已知节点内，查找新的有效节点
        if (insideId == null)
        {
            var nearest = FindNearestValidNode();
            if (nearest != null)
            {
                float dist = Vector3.Distance(transform.position, nearest.Position);
                if (dist <= nearest.radius)
                {
                    insideId = nearest.id;
                    insideComp = nearest;
                    isApproach = nearest.isVisible;
                }
            }
        }

        // 判断状态是否变化
        bool sameTriggered = insideId != null && insideId == CurrentNodeId && !isApproach;
        bool sameApproached = isApproach && insideId == ApproachedNodeComponent?.id;
        bool sameNothing = insideId == null && CurrentNodeId == null && ApproachedNodeComponent == null;
        if (sameTriggered || sameApproached || sameNothing)
            return;

        // 离开已触发节点
        if (CurrentNodeId != null && !sameTriggered)
        {
            var exitComp = NodeRegistry.Instance.GetComponentForNode(CurrentNodeId);
            if (enableDebugLog)
                Debug.Log($"[ProximityTrigger] Exit: {CurrentNodeId}");
            OnNodeExit?.Invoke(this, new NodeTriggerEventArgs { NodeId = CurrentNodeId, NodeComponent = exitComp });
            CurrentNodeId = null;
        }

        // 离开 approached 节点（未触发就走了）
        if (ApproachedNodeComponent != null && !sameApproached)
        {
            if (enableDebugLog)
                Debug.Log($"[ProximityTrigger] Approach cleared: {ApproachedNodeComponent.id}");
            OnApproachCleared?.Invoke();
            ApproachedNodeComponent = null;
        }

        // 进入新状态
        if (insideId != null && insideComp != null)
        {
            if (isApproach)
            {
                // 可见节点：仅 approach，等待玩家按键触发
                ApproachedNodeComponent = insideComp;
                if (enableDebugLog)
                    Debug.Log($"[ProximityTrigger] Approached: {insideId}");
                OnNodeApproached?.Invoke(this, new NodeTriggerEventArgs { NodeId = insideId, NodeComponent = insideComp });
            }
            else
            {
                // 不可见节点：自动触发
                TourSessionManager.Instance.Invalidate(insideId);
                CurrentNodeId = insideId;
                if (enableDebugLog)
                    Debug.Log($"[ProximityTrigger] Enter: {insideId}");
                OnNodeEnter?.Invoke(this, new NodeTriggerEventArgs { NodeId = insideId, NodeComponent = insideComp });

                if (!allVisitedFired && AllVisibleNodesVisited())
                {
                    allVisitedFired = true;
                    OnAllVisited?.Invoke();
                }
            }
        }
    }

    /// <summary>手动触发当前 approached 的可见节点</summary>
    public void TriggerApproachedNode()
    {
        if (ApproachedNodeComponent == null)
        {
            Debug.LogWarning("[ProximityTrigger] TriggerApproachedNode 调用时没有 approached 节点");
            return;
        }

        var comp = ApproachedNodeComponent;
        string nodeId = comp.id;

        TourSessionManager.Instance.Invalidate(nodeId);
        CurrentNodeId = nodeId;
        ApproachedNodeComponent = null;

        if (enableDebugLog)
            Debug.Log($"[ProximityTrigger] Triggered by user: {nodeId}");

        OnNodeEnter?.Invoke(this, new NodeTriggerEventArgs { NodeId = nodeId, NodeComponent = comp });

        if (!allVisitedFired && AllVisibleNodesVisited())
        {
            allVisitedFired = true;
            OnAllVisited?.Invoke();
        }
    }

    /// <summary>清除当前节点状态（配合 TourSessionManager.Reset 使用）</summary>
    public void ResetDetection()
    {
        CurrentNodeId = null;
        ApproachedNodeComponent = null;
        allVisitedFired = false;
    }

    /// <summary>所有可见节点是否均已失效</summary>
    private bool AllVisibleNodesVisited()
    {
        foreach (var node in NodeRegistry.Instance.VisibleNodes)
        {
            if (!TourSessionManager.Instance.IsInvalidated(node.id))
                return false;
        }
        return true;
    }

    /// <summary>在所有未失效节点中找距离最近的</summary>
    private NodeComponent FindNearestValidNode()
    {
        NodeComponent nearest = null;
        float minDist = float.MaxValue;
        var pos = transform.position;

        foreach (var node in NodeRegistry.Instance.AllNodes)
        {
            if (TourSessionManager.Instance.IsInvalidated(node.id))
                continue;
            float d = Vector3.Distance(pos, node.Position);
            if (d < minDist)
            {
                minDist = d;
                nearest = node;
            }
        }
        return nearest;
    }

    private void OnDrawGizmos()
    {
        if (!isActiveAndEnabled || NodeRegistry.Instance == null)
            return;

        var target = FindNearestValidNode();
        if (target == null) return;

        float dist = Vector3.Distance(transform.position, target.Position);
        bool inRange = dist <= target.radius;

        Gizmos.color = inRange ? Color.green : Color.gray;
        Gizmos.DrawLine(transform.position, target.Position);

        // 在目标位置画一个小圆标记
        Gizmos.DrawWireSphere(target.Position, 0.2f);
    }
}
