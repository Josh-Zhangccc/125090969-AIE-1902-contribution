using System;

/// <summary>
/// 进入/离开节点的触发事件参数。同时携带 nodeId（日志/比对用）和 NodeComponent（下游直接使用）。
/// </summary>
public class NodeTriggerEventArgs : EventArgs
{
    public string NodeId;
    public NodeComponent NodeComponent;
}
