using System.Collections;
using UnityEngine;

/// <summary>
/// 节点触发时，平滑旋转玩家/camera 使其看向 cameraOverride.target。
/// 挂载到 Player 或 Camera GameObject 上。
///
/// 行为分两种模式（由 cameraOverride.stayOnTarget 控制）：
///   stayOnTarget = true  → 转向后停留，玩家可自由转动鼠标脱离
///   stayOnTarget = false → 转向后等待 restoreAfterSeconds 秒，自动恢复原朝向
///
/// ESC 关闭面板 / 离开节点 → 取消待执行的自动恢复（不恢复旋转），交还鼠标控制权
/// </summary>
[DefaultExecutionOrder(200)]
public class CameraController : MonoBehaviour
{
    [Header("Target Transform")]
    [Tooltip("需要旋转的 Transform（通常是 Player）。留空则使用当前 GameObject")]
    public Transform targetTransform;

    [Header("Fallback")]
    [Tooltip("cameraOverride.blendTime 为 0 或未设置时使用的默认过渡时间")]
    [Min(0.1f)]
    public float defaultBlendTime = 1f;

    [Tooltip("恢复视角时使用的过渡时间")]
    [Min(0.1f)]
    public float restoreBlendTime = 0.5f;

    private Quaternion originalRotation;
    private Coroutine lookCoroutine;
    private PlayerMovement playerMovement;
    private bool isOverriding;

    private void Awake()
    {
        if (targetTransform == null)
            targetTransform = transform;

        playerMovement = targetTransform.GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        if (ContentManager.Instance == null) return;

        ContentManager.Instance.OnContentReady += OnContentReady;
        ContentManager.Instance.OnContentClear += OnContentClear;
        ContentManager.Instance.OnContentDismissed += OnDismissed;
    }

    private void OnDestroy()
    {
        if (ContentManager.Instance == null) return;

        ContentManager.Instance.OnContentReady -= OnContentReady;
        ContentManager.Instance.OnContentClear -= OnContentClear;
        ContentManager.Instance.OnContentDismissed -= OnDismissed;
    }

    private void OnContentReady(ContentData data)
    {
        if (!data.cameraOverride.enabled) return;

        if (lookCoroutine != null)
            StopCoroutine(lookCoroutine);

        originalRotation = targetTransform.rotation;

        if (playerMovement != null)
            playerMovement.disableMouseLook = true;

        float blend = data.cameraOverride.blendTime > 0f
            ? data.cameraOverride.blendTime
            : defaultBlendTime;

        isOverriding = true;
        lookCoroutine = StartCoroutine(LookAtCoroutine(
            data.cameraOverride.target,
            blend,
            data.cameraOverride.stayOnTarget,
            data.cameraOverride.restoreAfterSeconds));
    }

    private void OnContentClear()
    {
        CancelOverride();
    }

    private void OnDismissed()
    {
        CancelOverride();
    }

    /// <summary>取消当前 override（不恢复旋转），交还鼠标控制权</summary>
    private void CancelOverride()
    {
        if (!isOverriding) return;

        if (lookCoroutine != null)
        {
            StopCoroutine(lookCoroutine);
            lookCoroutine = null;
        }

        isOverriding = false;

        if (playerMovement != null)
            playerMovement.disableMouseLook = false;
    }

    private IEnumerator LookAtCoroutine(Vector3 target, float blendTime, bool stayOnTarget, float restoreAfterSeconds)
    {
        // Phase 1: smooth rotate to look at target
        Vector3 direction = target - targetTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            isOverriding = false;
            if (playerMovement != null)
                playerMovement.disableMouseLook = false;
            yield break;
        }

        Quaternion startRotation = targetTransform.rotation;
        Quaternion endRotation = Quaternion.LookRotation(direction);
        float elapsed = 0f;

        while (elapsed < blendTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / blendTime);
            targetTransform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
            yield return null;
        }

        targetTransform.rotation = endRotation;

        // 初始转向完成，交还鼠标控制权
        if (playerMovement != null)
            playerMovement.disableMouseLook = false;

        // Phase 2: stay 模式下直接结束；非 stay 模式等待后恢复
        if (stayOnTarget)
        {
            isOverriding = false;
            lookCoroutine = null;
            yield break;
        }

        if (restoreAfterSeconds > 0f)
            yield return new WaitForSeconds(restoreAfterSeconds);

        // 恢复前再次检查是否被取消（ESC / 离开节点）
        if (!isOverriding)
            yield break;

        // Phase 3: smooth restore to original rotation
        if (playerMovement != null)
            playerMovement.disableMouseLook = true;

        startRotation = targetTransform.rotation;
        elapsed = 0f;

        while (elapsed < restoreBlendTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / restoreBlendTime);
            targetTransform.rotation = Quaternion.Slerp(startRotation, originalRotation, t);
            yield return null;
        }

        targetTransform.rotation = originalRotation;

        if (playerMovement != null)
            playerMovement.disableMouseLook = false;

        isOverriding = false;
        lookCoroutine = null;
    }
}
