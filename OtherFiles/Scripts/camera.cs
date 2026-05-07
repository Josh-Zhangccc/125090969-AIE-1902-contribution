using UnityEngine;

/// <summary>
/// 相机跟随组件（优化传送瞬移逻辑）
/// 目标瞬移时相机直接定位，正常移动时平滑跟随
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("基础跟随设置")]
    [Tooltip("默认跟随目标（可在 Inspector 直接指定）")]
    public Transform defaultTarget;
    [Tooltip("相机与目标的偏移量（世界坐标）")]
    public Vector3 followOffset = new Vector3(0, 5, -10);
    [Tooltip("跟随平滑速度（值越大越灵敏，建议 0.1-1 之间）")]
    public float smoothSpeed = 0.2f;

    [Header("瞬移检测设置（关键优化）")]
    [Tooltip("目标位置突变阈值（超过此距离判定为瞬移，单位：米）")]
    public float teleportThreshold = 10f; // 可根据场景大小调整，默认10米
    [Tooltip("瞬移时相机是否直接定位（勾选=直接瞬移，取消=保留平滑）")]
    public bool teleportCameraInstantly = true;

    [Header("轴锁定设置")]
    [Tooltip("是否锁定X轴（相机X坐标不跟随目标）")]
    public bool lockXAxis = false;
    [Tooltip("是否锁定Y轴（相机Y坐标不跟随目标）")]
    public bool lockYAxis = false;
    [Tooltip("是否锁定Z轴（相机Z坐标不跟随目标）")]
    public bool lockZAxis = false;

    [Header("跟随范围限制（可选）")]
    [Tooltip("是否启用范围限制")]
    public bool enableRangeLimit = false;
    [Tooltip("相机最小X坐标（启用范围限制时生效）")]
    public float minX = -50f;
    [Tooltip("相机最大X坐标（启用范围限制时生效）")]
    public float maxX = 50f;
    [Tooltip("相机最小Y坐标（启用范围限制时生效）")]
    public float minY = 2f;
    [Tooltip("相机最大Y坐标（启用范围限制时生效）")]
    public float maxY = 15f;
    [Tooltip("相机最小Z坐标（启用范围限制时生效）")]
    public float minZ = -50f;
    [Tooltip("相机最大Z坐标（启用范围限制时生效）")]
    public float maxZ = 0f;

    [Header("目标丢失处理")]
    [Tooltip("目标丢失后是否自动查找默认目标")]
    public bool autoFindDefaultTarget = true;
    [Tooltip("目标丢失后的等待时间（秒）")]
    public float targetLostWaitTime = 2f;

    private Transform currentTarget; // 当前跟随目标
    private Vector3 lastTargetPosition; // 上一帧目标位置（用于检测瞬移）
    private float targetLostTimer; // 目标丢失计时器

    private void Start()  //轉化使用方式
    {
        // 初始化跟随目标
        if (defaultTarget != null)
        {
            currentTarget = defaultTarget;
            lastTargetPosition = currentTarget.position; // 记录初始位置
            Debug.Log($"CameraFollow：初始跟随目标 -> {defaultTarget.gameObject.name}");
        }
        else if (autoFindDefaultTarget)
        {
            // 自动查找玩家（标签为 "Player" 的物体）
            FindPlayerTarget();
        }

        // 初始化相机位置（如果有目标，直接定位到目标对应的相机位置）
        if (currentTarget != null)
        {
            Vector3 initialCameraPos = CalculateTargetCameraPosition(currentTarget.position);
            transform.position = initialCameraPos;
            lastTargetPosition = currentTarget.position;
        }
    }

    private void LateUpdate()
    {
        // 处理目标跟随逻辑（LateUpdate 确保在玩家移动后执行，避免相机抖动）
        HandleTargetFollow();
    }

    /// <summary>
    /// 处理相机跟随逻辑（核心优化：检测瞬移）
    /// </summary>
    private void HandleTargetFollow()
    {
        // 目标存在时正常跟随
        if (currentTarget != null)
        {
            targetLostTimer = 0f; // 重置丢失计时器

            // 计算目标当前应对应的相机位置
            Vector3 targetCameraPos = CalculateTargetCameraPosition(currentTarget.position);

            // 检测目标是否瞬移（当前位置与上一帧位置距离超过阈值）
            bool isTargetTeleported = Vector3.Distance(currentTarget.position, lastTargetPosition) > teleportThreshold;

            // 根据是否瞬移，选择相机移动方式
            if (isTargetTeleported && teleportCameraInstantly)
            {
                // 目标瞬移：相机直接定位到目标位置（无平滑）
                transform.position = targetCameraPos;
                Debug.Log($"CameraFollow：检测到目标瞬移，相机直接定位到新位置");
            }
            else
            {
                // 目标正常移动：平滑跟随
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetCameraPos, smoothSpeed);
                transform.position = smoothedPosition;
            }

            // 更新上一帧目标位置（用于下次检测）
            lastTargetPosition = currentTarget.position;
        }
        else
        {
            // 目标丢失时的处理
            HandleTargetLost();
        }
    }

    /// <summary>
    /// 计算目标对应的相机位置（应用偏移、轴锁定、范围限制）
    /// </summary>
    private Vector3 CalculateTargetCameraPosition(Vector3 targetWorldPos)
    {
        // 基础位置 = 目标世界位置 + 相机偏移
        Vector3 cameraPos = targetWorldPos + followOffset;

        // 应用轴锁定（过滤不需要跟随的轴）
        if (lockXAxis) cameraPos.x = transform.position.x;
        if (lockYAxis) cameraPos.y = transform.position.y;
        if (lockZAxis) cameraPos.z = transform.position.z;

        // 应用范围限制
        if (enableRangeLimit)
        {
            cameraPos.x = Mathf.Clamp(cameraPos.x, minX, maxX);
            cameraPos.y = Mathf.Clamp(cameraPos.y, minY, maxY);
            cameraPos.z = Mathf.Clamp(cameraPos.z, minZ, maxZ);
        }

        return cameraPos;
    }

    /// <summary>
    /// 目标丢失后的处理逻辑
    /// </summary>
    private void HandleTargetLost()
    {
        targetLostTimer += Time.deltaTime;

        // 超过等待时间后尝试自动查找目标
        if (autoFindDefaultTarget && targetLostTimer >= targetLostWaitTime)
        {
            FindPlayerTarget();
            targetLostTimer = 0f;
        }
    }

    /// <summary>
    /// 自动查找标签为 "Player" 的目标
    /// </summary>
    private void FindPlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            currentTarget = player.transform;
            lastTargetPosition = currentTarget.position; // 初始化上一帧位置
            // 自动定位到玩家对应的相机位置
            transform.position = CalculateTargetCameraPosition(currentTarget.position);
            Debug.Log($"CameraFollow：自动找到玩家目标 -> {player.name}，相机已定位");
        }
        else
        {
            Debug.LogWarning("CameraFollow：未找到标签为 'Player' 的目标，请检查玩家标签或手动指定 defaultTarget");
        }
    }

    /// <summary>
    /// 设置跟随目标（兼容 MapSystem 的调用）
    /// </summary>
    /// <param name="newTarget">新的跟随目标</param>
    public void SetTarget(Transform newTarget)
    {
        if (newTarget != null)
        {
            currentTarget = newTarget;
            lastTargetPosition = currentTarget.position; // 重置上一帧位置（避免误判瞬移）
            // 切换目标时直接定位（比如传送后切换目标）
            Vector3 newCameraPos = CalculateTargetCameraPosition(currentTarget.position);
            transform.position = newCameraPos;
            Debug.Log($"CameraFollow：跟随目标已更新为 -> {newTarget.gameObject.name}，相机直接定位");
        }
        else
        {
            Debug.LogWarning("CameraFollow：尝试设置空目标，忽略该操作");
        }
    }

    /// <summary>
    /// 动态修改相机偏移量（支持代码调用）
    /// </summary>
    /// <param name="newOffset">新的偏移量</param>
    public void SetFollowOffset(Vector3 newOffset)
    {
        followOffset = newOffset;
        Debug.Log($"CameraFollow：相机偏移量已更新为 -> {newOffset}");
    }

    /// <summary>
    /// 动态修改平滑速度（支持代码调用）
    /// </summary>
    /// <param name="newSmoothSpeed">新的平滑速度</param>
    public void SetSmoothSpeed(float newSmoothSpeed)
    {
        smoothSpeed = Mathf.Clamp(newSmoothSpeed, 0.01f, 5f); // 限制速度范围，避免异常
        Debug.Log($"CameraFollow：平滑速度已更新为 -> {smoothSpeed}");
    }

    /// <summary>
    /// 启用/禁用范围限制（支持代码调用）
    /// </summary>
    public void ToggleRangeLimit(bool isEnabled)
    {
        enableRangeLimit = isEnabled;
        Debug.Log($"CameraFollow：范围限制已{(isEnabled ? "启用" : "禁用")}");
    }
}