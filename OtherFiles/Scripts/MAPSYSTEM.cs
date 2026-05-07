using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

public class MapSystem : MonoBehaviour
{
    [Header("地图UI设置")]
    public GameObject mapWindow; // 地图窗口面板（需在Inspector赋值）
    public RectTransform mapContainer; // 地图背景容器（RectTransform，承载传送点按钮）
    public GameObject locationButtonPrefab; // 传送点按钮预制体（需含Button和Image组件）
    public Sprite defaultLocationIcon; // 默认传送点图标（可选）

    [Header("屏蔽设置UI")]
    public GameObject settingsUI; // 需要屏蔽的设置UI面板（传送时隐藏）

    [Header("加载界面设置")]
    public GameObject loadingScreen; // 加载界面（全屏遮挡，需手动创建）
    public Image loadingProgressBar; // 加载进度条（可选）
    public float loadingDuration = 1.5f; // 加载动画时长（1-2秒为宜）

    [Header("视频播放设置")]
    public VideoPlayer videoPlayer; // 视频播放器（确保已经在Inspector里赋值）
    public RawImage videoRawImage; // 显示视频的RawImage UI（也需要在Inspector里赋值）
    public float teleportationDuration = 2f; // 传送界面显示时间（以秒为单位）

    [Header("传送音效设置")]
    public AudioClip teleportSound; // 拖入你的传送音效文件（WAV/MP3）
    [Range(0.1f, 1f)]
    public float teleportSoundVolume = 0.8f; // 传送音效音量
    private AudioSource teleportAudioSource; // 音效播放组件（自动创建）

    [Header("核心组件引用")]
    public PlayerMovement playerMovement; // 玩家移动脚本（需含Teleport方法）
    public CameraFollow cameraFollow; // 相机跟随组件（自动查找或手动赋值）
    public List<MapLocation> mapLocations = new List<MapLocation>(); // 传送点数据列表

    private List<Button> locationButtons = new List<Button>(); // 缓存生成的传送点按钮

    private void Start()
    {
        // 初始化UI状态（默认隐藏地图、加载界面，显示设置UI）
        mapWindow?.SetActive(false);
        settingsUI?.SetActive(true);
        loadingScreen?.SetActive(false);

        // 初始化传送音效组件
        InitTeleportAudioSource();

        // 自动查找缺失的核心组件
        AutoFindCoreComponents();

        // 生成地图上的传送点按钮
        CreateLocationButtons();
    }

    /// <summary>
    /// 初始化传送音效AudioSource（自动创建，无需手动添加）
    /// </summary>
    private void InitTeleportAudioSource()
    {
        // 查找现有AudioSource，无则创建
        teleportAudioSource = GetComponent<AudioSource>();
        if (teleportAudioSource == null)
        {
            teleportAudioSource = gameObject.AddComponent<AudioSource>();
        }

        // 音效组件基础设置（确保音效正常播放）
        teleportAudioSource.loop = false; // 仅播放一次
        teleportAudioSource.volume = teleportSoundVolume;
        teleportAudioSource.spatialBlend = 0f; // 2D音效（全场景可听）
        teleportAudioSource.playOnAwake = false; // 不自动播放
        teleportAudioSource.mute = false; // 关闭静音
    }

    /// <summary>
    /// 自动查找玩家和相机跟随组件（减少手动赋值工作量）
    /// </summary>
    private void AutoFindCoreComponents()
    {
        // 查找玩家移动组件
        if (playerMovement == null)
        {
            playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement == null)
                Debug.LogError("MapSystem：未找到 PlayerMovement 组件！请确保玩家身上挂载了该脚本");
        }

        // 查找相机跟随组件
        if (cameraFollow == null)
        {
            // 优先从主相机查找
            if (Camera.main != null)
                cameraFollow = Camera.main.GetComponent<CameraFollow>();

            // 全局查找所有CameraFollow
            if (cameraFollow == null)
                cameraFollow = FindObjectOfType<CameraFollow>();

            if (cameraFollow == null)
                Debug.LogWarning("MapSystem：未找到 CameraFollow 组件！传送后相机可能无法正确定位");
        }
    }

    /// <summary>
    /// 生成所有传送点按钮（动态创建，支持编辑mapLocations后自动更新）
    /// </summary>
    private void CreateLocationButtons()
    {
        // 清除旧按钮，避免重复
        foreach (var btn in locationButtons)
        {
            if (btn != null)
                Destroy(btn.gameObject);
        }
        locationButtons.Clear();

        // 校验必要组件（缺失则报错并终止）
        if (mapContainer == null)
        {
            Debug.LogError("MapSystem：未设置 mapContainer（地图容器）！");
            return;
        }
        if (locationButtonPrefab == null)
        {
            Debug.LogError("MapSystem：未设置 locationButtonPrefab（传送点预制体）！");
            return;
        }

        // 遍历传送点列表，生成按钮
        foreach (var location in mapLocations)
        {
            GameObject btnObj = Instantiate(locationButtonPrefab, mapContainer);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            Button btn = btnObj.GetComponent<Button>();
            Image btnImg = btnObj.GetComponent<Image>();

            // 设置按钮在地图上的位置（基于UI锚点坐标）
            if (btnRect != null)
                btnRect.anchoredPosition = location.uiPosition;

            // 设置按钮图标（优先使用自定义图标，无则用默认）
            if (btnImg != null)
                btnImg.sprite = location.locationIcon ?? defaultLocationIcon;

            // 绑定点击事件（传送到对应位置）
            if (btn != null)
            {
                int locationIndex = mapLocations.IndexOf(location);
                btn.onClick.AddListener(() => OnLocationButtonClick(locationIndex));
                locationButtons.Add(btn);
            }
            else
            {
                Debug.LogWarning($"MapSystem：传送点预制体缺少 Button 组件！已销毁无效对象");
                Destroy(btnObj);
            }
        }
    }

    /// <summary>
    /// 传送点按钮点击事件（播放传送音效）
    /// </summary>
    private void OnLocationButtonClick(int locationIndex)
    {
        // 校验索引有效性
        if (locationIndex < 0 || locationIndex >= mapLocations.Count)
        {
            Debug.LogError("MapSystem：无效的传送点索引！");
            return;
        }

        // 校验玩家组件（防止空引用）
        if (playerMovement == null)
        {
            Debug.LogError("MapSystem：未找到 PlayerMovement 组件，无法传送！");
            return;
        }

        // 播放传送音效（不受Time.timeScale影响）
        PlayTeleportSound();

        // 无加载界面则直接传送，有则启动加载+传送协程
        if (loadingScreen == null)
        {
            ExecuteTeleport(locationIndex);
            // 直接传送时也重置音效
            ResetPlayerStepSound();
            HideMap(); // 传送后隐藏地图
            return;
        }

        // 启动加载动画+后台传送协程
        StartCoroutine(LoadingAndTeleportCoroutine(locationIndex));
    }

    /// <summary>
    /// 播放传送音效（关键：使用PlayOneShot，不受Time.timeScale暂停影响）
    /// </summary>
    private void PlayTeleportSound()
    {
        if (teleportSound == null)
        {
            Debug.LogWarning("MapSystem：未配置传送音效文件！");
            return;
        }

        if (teleportAudioSource == null)
        {
            InitTeleportAudioSource(); // 容错：重新初始化音效组件
        }

        // PlayOneShot：不受Time.timeScale影响，且不会中断其他音效
        teleportAudioSource.PlayOneShot(teleportSound, teleportSoundVolume);
        Debug.Log("MapSystem：播放传送音效");
    }

    /// <summary>
    /// 加载动画+后台传送协程（核心优化：加载期间完成传送，无中间画面）
    /// </summary>
    private IEnumerator LoadingAndTeleportCoroutine(int locationIndex)
    {
        // 1. 显示加载界面，隐藏地图和设置UI（遮挡屏幕，防止泄露传送过程）
        loadingScreen.SetActive(true);
        mapWindow.SetActive(false);
        settingsUI.SetActive(false);

        // 2. 播放视频（确保视频播放器和RawImage已经设置好）
        if (videoPlayer != null && videoRawImage != null)
        {
            // 设置视频RawImage显示
            videoRawImage.gameObject.SetActive(true);
            videoPlayer.Play(); // 播放视频

            // 等待视频播放的指定时长，或者直到视频播放完成
            float elapsedTime = 0f;
            while (elapsedTime < teleportationDuration && videoPlayer.isPlaying)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            // 如果视频播放结束前传送完成，确保进度条满格
            if (!videoPlayer.isPlaying)
            {
                elapsedTime = teleportationDuration; // 确保加载界面保持时间一致
            }
        }

        // 3. 暂停玩家移动（避免加载期间误操作）
        playerMovement.isMovementPaused = true;
        Time.timeScale = 0; // 暂停游戏时间（不影响加载动画）
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // 修复物理时间缩放

        // 4. 关键步骤：后台执行传送（玩家+相机瞬间到位，加载界面遮挡不可见）
        ExecuteTeleport(locationIndex);

        // 5. 播放加载动画（仅视觉过渡，实际传送已完成）
        if (loadingProgressBar != null)
        {
            // 进度条动画（从0到1平滑填充）
            float progress = 0f;
            while (progress < 1f)
            {
                progress += Time.unscaledDeltaTime / teleportationDuration;
                loadingProgressBar.fillAmount = Mathf.Clamp01(progress);
                yield return null;
            }
            loadingProgressBar.fillAmount = 1f; // 确保进度条满格
        }
        else
        {
            // 无进度条时，直接等待指定时长
            yield return new WaitForSecondsRealtime(teleportationDuration);
        }

        // 6. 加载结束：恢复UI和游戏状态（新增强制刷新）
        loadingScreen.SetActive(false);
        settingsUI.SetActive(true);

        // 强制恢复移动状态（核心修复：避免状态未同步）
        playerMovement.isMovementPaused = false;
        Time.timeScale = 1;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // 修复物理时间缩放

        // 隐藏视频播放
        if (videoRawImage != null)
        {
            videoRawImage.gameObject.SetActive(false);
        }

        // 核心修复：传送完成后重置玩家脚步音效
        ResetPlayerStepSound();
    }

    /// <summary>
    /// 执行实际传送逻辑（玩家瞬移+相机强制定位）
    /// </summary>
    private void ExecuteTeleport(int locationIndex)
    {
        MapLocation targetLocation = mapLocations[locationIndex];

        // 1. 玩家瞬间传送至目标世界坐标
        playerMovement.Teleport(targetLocation.worldPosition);
        Debug.Log($"MapSystem：已传送至 {targetLocation.locationName}（坐标：{targetLocation.worldPosition}）");

        // 2. 相机强制定位至玩家身边（跳过所有移动动画）
        if (cameraFollow != null)
        {
            // 调用CameraFollow的SetTarget，触发瞬间定位
            cameraFollow.SetTarget(playerMovement.transform);

            // 额外保险：直接计算相机最终位置并赋值（避免CameraFollow内部延迟）
            Vector3 targetCameraPos = playerMovement.transform.position + cameraFollow.followOffset;

            // 应用CameraFollow的轴锁定和范围限制（保持配置一致性）
            if (cameraFollow.lockXAxis) targetCameraPos.x = cameraFollow.transform.position.x;
            if (cameraFollow.lockYAxis) targetCameraPos.y = cameraFollow.transform.position.y;
            if (cameraFollow.lockZAxis) targetCameraPos.z = cameraFollow.transform.position.z;
            if (cameraFollow.enableRangeLimit)
            {
                targetCameraPos.x = Mathf.Clamp(targetCameraPos.x, cameraFollow.minX, cameraFollow.maxX);
                targetCameraPos.y = Mathf.Clamp(targetCameraPos.y, cameraFollow.minY, cameraFollow.maxY);
                targetCameraPos.z = Mathf.Clamp(targetCameraPos.z, cameraFollow.minZ, cameraFollow.maxZ);
            }

            // 相机瞬间到位
            cameraFollow.transform.position = targetCameraPos;
        }
        else
        {
            // 无CameraFollow时的备用方案：直接移动主相机
            if (Camera.main != null)
            {
                Camera.main.transform.position = playerMovement.transform.position + new Vector3(0, 5, -10);
            }
        }
    }

    /// <summary>
    /// 重置玩家脚步音效状态（核心修复方法）
    /// </summary>
    private void ResetPlayerStepSound()
    {
        if (playerMovement.TryGetComponent<PlayerStepSound>(out var stepSound))
        {
            stepSound.ResetStepSoundState();
            Debug.Log("MapSystem：传送完成，已重置玩家脚步音效状态");
        }
        else
        {
            Debug.LogWarning("MapSystem：玩家对象上未找到PlayerStepSound脚本，无法重置音效！");
        }
    }

    /// <summary>
    /// 显示地图（调用此方法打开地图，如绑定按键）
    /// </summary>
    public void ShowMap()
    {
        mapWindow?.SetActive(true);
        settingsUI?.SetActive(false);
    }

    /// <summary>
    /// 隐藏地图（调用此方法关闭地图，如绑定按键）
    /// </summary>
    public void HideMap()
    {
        mapWindow?.SetActive(false);
        settingsUI?.SetActive(true);
    }

    /// <summary>
    /// 动态添加传送点（支持代码动态扩展传送点）
    /// </summary>
    public void AddMapLocation(string locationName, Vector3 worldPos, Vector2 uiPos, Sprite icon = null)
    {
        mapLocations.Add(new MapLocation
        {
            locationName = locationName,
            worldPosition = worldPos,
            uiPosition = uiPos,
            locationIcon = icon
        });
        CreateLocationButtons(); // 重新生成按钮，更新地图
    }
}

/// <summary>
/// 传送点数据结构（序列化到Inspector，可直观编辑）
/// </summary>
[System.Serializable]
public class MapLocation
{
    public string locationName; // 地点名称（用于调试和显示）
    public Vector3 worldPosition; // 实际传送的世界坐标
    public Vector2 uiPosition; // 地图UI上的按钮位置（锚点坐标）
    public Sprite locationIcon; // 传送点自定义图标（可选）
}