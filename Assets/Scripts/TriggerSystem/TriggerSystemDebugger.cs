using UnityEngine;

/// <summary>
/// 订阅 TourSessionManager 事件，在 Console 和屏幕输出触发调试信息。
/// </summary>
[DefaultExecutionOrder(150)]
public class TriggerSystemDebugger : MonoBehaviour
{
    public bool showOverlay;

    private string lastEventText = "";
    private string statsText = "";

    private void Start()
    {
        var session = TourSessionManager.Instance;
        if (session == null)
        {
            Debug.LogError("[TriggerSystemDebugger] 场景中未找到 TourSessionManager.Instance");
            return;
        }

        session.OnInvalidated += OnInvalidatedHandler;
        session.OnReset += OnResetHandler;
        UpdateStatsText(session);
    }

    private void OnInvalidatedHandler(string nodeId)
    {
        var session = TourSessionManager.Instance;
        Debug.Log($"[TriggerDebug] Invalidated: {nodeId}  ({session.InvalidatedCount}/{session.TotalVisibleCount} visible)");
        UpdateStatsText(session);
    }

    private void OnResetHandler()
    {
        Debug.Log("[TriggerDebug] Tour reset — 0 nodes visited");
        UpdateStatsText(TourSessionManager.Instance);
    }

    private void UpdateStatsText(TourSessionManager session)
    {
        statsText = $"Visited: {session.InvalidatedCount} / {session.TotalVisibleCount}";
    }

    private void OnGUI()
    {
        if (!showOverlay) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            normal = { textColor = Color.white }
        };

        var rect = new Rect(10, 10, 400, 30);
        var bgRect = new Rect(5, 5, 410, 35);
        GUI.Box(bgRect, "");
        GUI.Label(rect, statsText, style);
    }
}
