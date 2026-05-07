using UnityEngine;

[RequireComponent(typeof(FPSCounter))]
public class FPSAdaptiveResolution : MonoBehaviour
{
    [Header("FPS 阈值设置")]
    [Tooltip("低于此FPS开始降分辨率")]
    public float lowFpsThreshold = 45f;
    
    [Tooltip("高于此FPS尝试升分辨率")]
    public float highFpsThreshold = 55f;

    [Header("分辨率调整步幅")]
    [Tooltip("每次降低/提升的分辨率百分比")]
    [Range(0.1f, 0.5f)]
    public float resolutionStep = 0.2f;

    [Header("最小分辨率限制")]
    public int minWidth = 480;
    public int minHeight = 270;

    // 防抖：避免频繁切换
    private float _checkInterval = 1f;
    private float _checkTimer;
    
    // 当前分辨率缩放系数（1=100%）
    private float _currentScale = 1f;

    private void Start()
    {
        // 初始化：记录初始分辨率
        var res = Screen.currentResolution;
        UpdateResolution(_currentScale);
    }

    private void Update()
    {
        // 每秒检测一次FPS
        _checkTimer += Time.deltaTime;
        if (_checkTimer < _checkInterval) return;
        _checkTimer = 0f;
        
        AdaptiveResolution();
    }

    /// <summary>
    /// 核心自适应逻辑
    /// </summary>
    private void AdaptiveResolution()
    {
        float currentFps = FPSCounter.Instance.CurrentFps;

        // FPS过低 → 降分辨率
        if (currentFps < lowFpsThreshold)
        {
            DecreaseResolution();
        }
        // FPS充足 → 尝试升分辨率
        else if (currentFps > highFpsThreshold)
        {
            IncreaseResolution();
        }
    }

    /// <summary>
    /// 降低分辨率
    /// </summary>
    private void DecreaseResolution()
    {
        _currentScale -= resolutionStep;
        _currentScale = Mathf.Max(_currentScale, 0.5f); // 最低不低于50%
        UpdateResolution(_currentScale);
    }

    /// <summary>
    /// 提升分辨率
    /// </summary>
    private void IncreaseResolution()
    {
        _currentScale += resolutionStep;
        _currentScale = Mathf.Min(_currentScale, 1f); // 最高不超过100%
        UpdateResolution(_currentScale);
    }

    /// <summary>
    /// 应用新分辨率
    /// </summary>
    private void UpdateResolution(float scale)
    {
        int baseWidth = Screen.currentResolution.width;
        int baseHeight = Screen.currentResolution.height;

        int newWidth = Mathf.RoundToInt(baseWidth * scale);
        int newHeight = Mathf.RoundToInt(baseHeight * scale);

        // 限制最小分辨率
        newWidth = Mathf.Max(newWidth, minWidth);
        newHeight = Mathf.Max(newHeight, minHeight);

        // 设置分辨率（全屏模式）
        Screen.SetResolution(newWidth, newHeight, true);
        Debug.Log($"分辨率已调整：{newWidth}x{newHeight} | 当前FPS：{FPSCounter.Instance.CurrentFps:F1}");
    }
}