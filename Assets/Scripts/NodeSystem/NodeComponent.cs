using UnityEngine;

[DisallowMultipleComponent]
public class NodeComponent : MonoBehaviour
{
    [Header("Identity")]
    public string id;
    public LocalizedString displayName;
    public LocalizedString shortDescription;
    public string campusLocation = "lower";
    public bool isStartingNode;
    public bool isVisible = true;

    [Header("Trigger")]
    [Min(0.1f)]
    public float radius = 5f;
    [Tooltip("隐形节点 UI 自动消失的秒数，0 表示手动关闭")]
    public float autoDismissDuration = 0f;

    [Header("Navigation")]
    [Tooltip("下游连接节点")]
    public ConnectedNode[] connectedTo;

    [Header("Media")]
    public string thumbnail;
    public LocalizedText zhText;
    public LocalizedText enText;
    public NodeImage[] images;
    public LocalizedAudio zhAudio;
    public LocalizedAudio enAudio;

    [Header("Camera Override")]
    public CameraOverrideData cameraOverride;

    /// <summary>运行时节点位置（由 Transform 同步）</summary>
    public Vector3 Position => transform.position;

    /// <summary>运行时节点朝向（由 Transform 同步）</summary>
    public Vector3 Rotation => transform.eulerAngles;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
            Debug.LogWarning($"[NodeComponent] {name}: id 为空", this);
        if (string.IsNullOrWhiteSpace(displayName.zh) && string.IsNullOrWhiteSpace(displayName.en))
            Debug.LogWarning($"[NodeComponent] {name}: displayName 未填写", this);
        if (!isVisible)
        {
            radius = Mathf.Min(radius, 3f);
            if (autoDismissDuration <= 0f)
                Debug.LogWarning($"[NodeComponent] {name}: 隐形节点建议设置 autoDismissDuration > 0", this);
        }
    }

    private void Start()
    {
        if (NodeRegistry.Instance == null)
        {
            Debug.LogError("[NodeComponent] 场景中未找到 NodeRegistry，请先添加 NodeRegistry 组件");
            return;
        }
        NodeRegistry.Instance.Register(this);
    }

    private void OnDestroy()
    {
        if (NodeRegistry.Instance != null)
            NodeRegistry.Instance.Unregister(this);
    }

    /// <summary>导出为纯数据对象</summary>
    public NodeData ToNodeData()
    {
        return new NodeData
        {
            id = id,
            displayName = displayName,
            shortDescription = shortDescription,
            campusLocation = campusLocation,
            isStartingNode = isStartingNode,
            isVisible = isVisible,
            position = transform.position,
            rotation = transform.eulerAngles,
            connectedNodes = new ConnectedNodesData { to = connectedTo != null
                ? new System.Collections.Generic.List<ConnectedNode>(connectedTo)
                : new System.Collections.Generic.List<ConnectedNode>() },
            radius = radius,
            thumbnail = thumbnail,
            zhText = zhText,
            enText = enText,
            images = images != null
                ? new System.Collections.Generic.List<NodeImage>(images)
                : new System.Collections.Generic.List<NodeImage>(),
            zhAudio = zhAudio,
            enAudio = enAudio,
            cameraOverride = cameraOverride,
            autoDismissDuration = autoDismissDuration
        };
    }

    private void OnDrawGizmos()
    {
        // trigger radius
        Gizmos.color = isVisible
            ? new Color(0, 1, 0, 0.15f)
            : new Color(0, 0.5f, 1, 0.15f);
        Gizmos.DrawSphere(transform.position, radius);
        Gizmos.color = isVisible ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void OnDrawGizmosSelected()
    {
        if (connectedTo == null || connectedTo.Length == 0) return;

        var allComps = FindObjectsByType<NodeComponent>(FindObjectsSortMode.None);
        Gizmos.color = Color.yellow;
        foreach (var conn in connectedTo)
        {
            foreach (var other in allComps)
            {
                if (other.id == conn.id)
                {
                    Gizmos.DrawLine(transform.position, other.transform.position);

                    // draw arrowhead
                    var dir = (other.transform.position - transform.position).normalized;
                    var mid = transform.position + dir * (Vector3.Distance(transform.position, other.transform.position) * 0.5f);
                    Gizmos.color = conn.required ? Color.yellow : new Color(1f, 0.8f, 0.2f);
                    Gizmos.DrawRay(mid, Quaternion.Euler(0, 45, 0) * dir * 0.5f);
                    Gizmos.DrawRay(mid, Quaternion.Euler(0, -45, 0) * dir * 0.5f);
                    break;
                }
            }
        }
    }
}
