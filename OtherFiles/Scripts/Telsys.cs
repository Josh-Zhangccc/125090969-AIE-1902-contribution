using UnityEngine;

public class TeleportSystem : MonoBehaviour
{
    [Header("玩家引用")]
    public Transform playerTransform; // 拖拽你的玩家对象Transform到这里

    [Header("传送效果（可选）")]
    public GameObject teleportEffect; // 传送粒子效果（可选）

    private void Awake()
    {
        // 如果没手动指定玩家，自动查找标签为"Player"的对象（根据你的玩家标签修改）
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    // 传送玩家到目标位置
    public void TeleportPlayer(Vector3 targetPosition)
    {
        if (playerTransform == null)
        {
            Debug.LogError("未找到玩家，请在TeleportSystem中指定playerTransform！");
            return;
        }

        // 播放传送效果（可选）
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, playerTransform.position, Quaternion.identity);
        }

        // 执行传送（核心）
        playerTransform.position = targetPosition;

        // 目标位置播放效果（可选）
        if (teleportEffect != null)
        {
            Instantiate(teleportEffect, targetPosition, Quaternion.identity);
        }
    }
}