using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class NodeSystemValidator
{
    [MenuItem("Tools/Tour System/Validate All Nodes")]
    public static void ValidateAll()
    {
        var allComps = UnityEngine.Object.FindObjectsByType<NodeComponent>(FindObjectsSortMode.None);

        if (allComps.Length == 0)
        {
            Debug.LogWarning("[ValidateAll] 场景中未找到任何 NodeComponent。");
            return;
        }

        int errors = 0;
        int warnings = 0;
        var validIds = new HashSet<string>();
        var sb = new StringBuilder();

        sb.AppendLine("========== NodeSystem 全局校验 ==========");
        sb.AppendLine($"节点总数: {allComps.Length}\n");

        // ── Pass 1: ID ──────────────────────────────────────────
        sb.AppendLine("── ID 检查 ──");
        foreach (var comp in allComps)
        {
            if (string.IsNullOrWhiteSpace(comp.id))
            {
                sb.AppendLine($"  [ERROR] {comp.name}: id 为空");
                errors++;
            }
            else if (!validIds.Add(comp.id))
            {
                sb.AppendLine($"  [ERROR] id \"{comp.id}\" 重复: {comp.name}");
                errors++;
            }
        }
        if (errors == 0) sb.AppendLine("  通过\n");

        // ── Pass 2: Content ─────────────────────────────────────
        sb.AppendLine("── 内容检查 ──");
        int contentWarnings = 0;
        foreach (var comp in allComps)
        {
            if (!comp.isVisible) continue;

            if (string.IsNullOrWhiteSpace(comp.displayName.zh) &&
                string.IsNullOrWhiteSpace(comp.displayName.en))
            {
                sb.AppendLine($"  [WARN] {comp.name} (id={comp.id}): 可见节点 displayName 未填写");
                warnings++;
                contentWarnings++;
            }
            if (string.IsNullOrWhiteSpace(comp.thumbnail))
            {
                sb.AppendLine($"  [WARN] {comp.name} (id={comp.id}): 可见节点 thumbnail 为空");
                warnings++;
                contentWarnings++;
            }
            if (string.IsNullOrWhiteSpace(comp.zhText.title) &&
                string.IsNullOrWhiteSpace(comp.enText.title))
            {
                sb.AppendLine($"  [WARN] {comp.name} (id={comp.id}): 可见节点 text 标题为空");
                warnings++;
                contentWarnings++;
            }
        }
        if (contentWarnings == 0) sb.AppendLine("  通过\n");

        // ── Pass 3: Connections ─────────────────────────────────
        sb.AppendLine("── 连接检查 ──");
        int connWarnings = 0;
        foreach (var comp in allComps)
        {
            if (comp.connectedTo == null || comp.connectedTo.Length == 0) continue;

            foreach (var conn in comp.connectedTo)
            {
                if (string.IsNullOrWhiteSpace(conn.id))
                {
                    sb.AppendLine($"  [WARN] {comp.name} (id={comp.id}): connectedTo 中有空 id");
                    warnings++;
                    connWarnings++;
                }
                else if (conn.id == comp.id)
                {
                    sb.AppendLine($"  [WARN] {comp.name} (id={comp.id}): connectedTo 指向自身");
                    warnings++;
                    connWarnings++;
                }
                else if (!validIds.Contains(conn.id))
                {
                    sb.AppendLine($"  [WARN] {comp.name} (id={comp.id}): 连接的节点 \"{conn.id}\" 不存在");
                    warnings++;
                    connWarnings++;
                }
            }
        }
        if (connWarnings == 0) sb.AppendLine("  通过\n");

        // ── Pass 4: Topology ────────────────────────────────────
        sb.AppendLine("── 拓扑检查 ──");
        var hasIncoming = new HashSet<string>();
        foreach (var comp in allComps)
        {
            if (comp.connectedTo != null)
                foreach (var conn in comp.connectedTo)
                    if (!string.IsNullOrWhiteSpace(conn.id) && validIds.Contains(conn.id))
                        hasIncoming.Add(conn.id);
        }

        int startCount = 0;
        int orphanCount = 0;
        foreach (var comp in allComps)
        {
            string id = comp.id;
            if (string.IsNullOrWhiteSpace(id) || !validIds.Contains(id)) continue;

            if (comp.isStartingNode) startCount++;
            else if (!hasIncoming.Contains(id))
            {
                sb.AppendLine($"  [INFO] {comp.name} (id={comp.id}): 无上游节点且非起点（游离节点）");
                orphanCount++;
            }
        }

        if (startCount == 0)
        {
            sb.AppendLine($"  [WARN] 场景中没有起点节点（isStartingNode = true），UI 将无默认起点");
            warnings++;
        }
        else
        {
            sb.AppendLine($"  起点节点: {startCount} 个");
        }

        if (orphanCount == 0)
            sb.AppendLine("  无游离节点\n");
        else
            sb.AppendLine();

        // ── Summary ──────────────────────────────────────────────
        sb.AppendLine("=========================================");
        if (errors == 0 && warnings == 0)
            sb.AppendLine("  校验通过 — 所有节点正常");
        else
        {
            sb.Append("  校验结果: ");
            if (errors > 0) sb.Append($"{errors} 个错误");
            if (errors > 0 && warnings > 0) sb.Append(", ");
            if (warnings > 0) sb.Append($"{warnings} 个警告");
            sb.AppendLine();
        }
        sb.AppendLine("=========================================");

        Debug.Log(sb.ToString());
    }
}
