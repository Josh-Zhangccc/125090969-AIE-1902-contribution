using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Threading.Tasks;

/// <summary>
/// 绑定UI按钮，点击后触发场景加载+传送数据设置
/// 支持加载进度条（可选）+ 加载视频（可选）
/// </summary>
public class TeleportButton : MonoBehaviour
{
    [Header("=== 传送核心设置 ===")]
    [Tooltip("目标场景名称（必须和Build Settings中的完全一致，大小写敏感）")]
    public string targetSceneName;

    [Tooltip("目标场景中的传送位置（世界坐标）")]
    public Vector3 targetPosition;

    [Tooltip("目标场景中的角色旋转（欧拉角）")]
    public Vector3 targetRotation = Vector3.zero;

    [Header("=== 可选扩展：加载进度条 ===")]
    [Tooltip("勾选后启用进度条显示")]
    public bool useProgressBar = false;

    [Tooltip("拖入UI的Slider组件（启用进度条时必填）")]
    public Slider loadProgressSlider;

    [Tooltip("进度条显示文本（可选）")]
    public TMPro.TextMeshProUGUI progressText;

    [Header("=== 可选扩展：加载视频 ===")]
    [Tooltip("勾选后启用加载视频显示")]
    public bool useLoadVideo = false;

    [Tooltip("拖入视频播放的Panel容器（启用视频时必填）")]
    public RectTransform videoPanel;

    [Tooltip("拖入用于显示视频画面的RawImage（启用视频时必填）")]
    public RawImage videoRawImage;

    [Tooltip("加载用的视频资源（可以是本地视频文件或Resources中的视频）")]
    public VideoClip loadVideoClip;

    [Tooltip("是否循环播放视频（建议勾选，防止视频过短）")]
    public bool loopVideo = true;

    [Tooltip("视频音量（0-1）")]
    [Range(0, 1)] public float videoVolume = 0.5f;

    private Button _teleportBtn; // 按钮组件引用
    private VideoPlayer _videoPlayer; // 视频播放器组件
    private AudioSource _videoAudioSource; // 视频音频源

    private void Awake()
    {
        // 获取当前物体上的Button组件
        _teleportBtn = GetComponent<Button>();
        if (_teleportBtn == null)
        {
            Debug.LogError("错误：当前物体没有Button组件！请给按钮添加此脚本", this);
            return;
        }

        // 绑定按钮点击事件
        _teleportBtn.onClick.AddListener(OnTeleportClick);

        // 初始化进度条（隐藏/重置）
        if (useProgressBar)
        {
            if (loadProgressSlider != null)
            {
                loadProgressSlider.gameObject.SetActive(false);
                loadProgressSlider.value = 0;
            }
        }

        // 初始化视频播放组件
        InitVideoPlayer();
    }

    /// <summary>
    /// 初始化视频播放器
    /// </summary>
    private void InitVideoPlayer()
    {
        if (!useLoadVideo) return;

        // 验证视频相关组件是否齐全
        if (videoPanel == null)
        {
            Debug.LogError("错误：启用了加载视频，但未设置Video Panel！", this);
            useLoadVideo = false;
            return;
        }

        if (videoRawImage == null)
        {
            Debug.LogError("错误：启用了加载视频，但未设置Video RawImage！", this);
            useLoadVideo = false;
            return;
        }

        if (loadVideoClip == null)
        {
            Debug.LogWarning("警告：启用了加载视频，但未设置Video Clip！", this);
        }

        // 给Video Panel添加VideoPlayer组件
        _videoPlayer = videoPanel.gameObject.AddComponent<VideoPlayer>();
        // 添加AudioSource用于播放视频音频
        _videoAudioSource = videoPanel.gameObject.AddComponent<AudioSource>();

        // 配置VideoPlayer
        _videoPlayer.playOnAwake = false; // 不自动播放
        _videoPlayer.isLooping = loopVideo; // 设置循环
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture; // 渲染到RenderTexture
        _videoPlayer.targetCameraAlpha = 0; // 不影响相机渲染
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource; // 音频输出到AudioSource
        _videoPlayer.SetTargetAudioSource(0, _videoAudioSource); // 绑定音频源

        // 设置音频音量
        _videoAudioSource.volume = videoVolume;

        // 设置视频源
        if (loadVideoClip != null)
        {
            _videoPlayer.clip = loadVideoClip;
        }

        // 初始化RenderTexture
        CreateVideoRenderTexture();

        // 默认隐藏视频面板
        videoPanel.gameObject.SetActive(false);
    }

    /// <summary>
    /// 创建视频渲染纹理
    /// </summary>
    private void CreateVideoRenderTexture()
    {
        if (_videoPlayer == null || videoRawImage == null) return;

        // 获取屏幕分辨率作为纹理大小（也可以自定义大小）
        int width = Screen.width;
        int height = Screen.height;

        // 创建RenderTexture
        RenderTexture renderTexture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
        renderTexture.filterMode = FilterMode.Bilinear;
        renderTexture.wrapMode = TextureWrapMode.Clamp;

        // 绑定RenderTexture到VideoPlayer和RawImage
        _videoPlayer.targetTexture = renderTexture;
        videoRawImage.texture = renderTexture;
        videoRawImage.uvRect = new Rect(0, 0, 1, 1); // 正常显示，不翻转
    }

    /// <summary>
    /// 按钮点击触发传送
    /// </summary>
    private async void OnTeleportClick()
    {
        // 1. 验证参数合法性
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("错误：请设置目标场景名称！", this);
            return;
        }

        // 2. 确保传送数据管理器存在
        TeleportDataManager dataManager = FindObjectOfType<TeleportDataManager>();
        if (dataManager == null)
        {
            GameObject managerObj = new GameObject("[TeleportDataManager]");
            dataManager = managerObj.AddComponent<TeleportDataManager>();
            Debug.Log("自动创建了传送数据管理器", managerObj);
        }

        // 3. 设置传送目标数据
        dataManager.SetTeleportTarget(
            targetSceneName,
            targetPosition,
            Quaternion.Euler(targetRotation) // 欧拉角转四元数
        );

        // 4. 显示加载界面（视频+进度条）
        ShowLoadingUI();

        // 5. 异步加载目标场景（带进度条和视频）
        if (useProgressBar)
        {
            await LoadSceneWithProgress(targetSceneName);
        }
        else
        {
            // 无进度条：直接加载，视频继续播放直到加载完成
            await LoadSceneWithoutProgress(targetSceneName);
        }

        // 6. 传送角色
        TeleportPlayerToTarget();

        // 7. 隐藏加载界面
        HideLoadingUI();
    }

    /// <summary>
    /// 显示加载UI（视频+进度条）
    /// </summary>
    private void ShowLoadingUI()
    {
        // 显示视频
        if (useLoadVideo && videoPanel != null && _videoPlayer != null)
        {
            videoPanel.gameObject.SetActive(true);

            // 如果有视频资源，开始播放
            if (loadVideoClip != null)
            {
                _videoPlayer.Play();
            }
            else
            {
                Debug.LogWarning("警告：没有设置视频资源，只显示空面板", this);
            }
        }

        // 显示进度条
        if (useProgressBar && loadProgressSlider != null)
        {
            loadProgressSlider.gameObject.SetActive(true);
            loadProgressSlider.value = 0;
            if (progressText != null)
            {
                progressText.text = "0%";
            }
        }

        // 禁用按钮防止重复点击
        if (_teleportBtn != null)
        {
            _teleportBtn.interactable = false;
        }
    }

    /// <summary>
    /// 隐藏加载UI（视频+进度条）
    /// </summary>
    private void HideLoadingUI()
    {
        // 停止视频并隐藏
        if (useLoadVideo && videoPanel != null && _videoPlayer != null)
        {
            _videoPlayer.Stop();
            videoPanel.gameObject.SetActive(false);
        }

        // 隐藏进度条
        if (useProgressBar && loadProgressSlider != null)
        {
            loadProgressSlider.gameObject.SetActive(false);
        }

        // 恢复按钮交互
        if (_teleportBtn != null)
        {
            _teleportBtn.interactable = true;
        }
    }

    /// <summary>
    /// 传送角色到目标位置
    /// </summary>
    private void TeleportPlayerToTarget()
    {
        // 在目标场景加载完成后，传送角色
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == targetSceneName)
        {
            // 找到玩家对象
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                // 设置目标位置和旋转
                player.transform.position = targetPosition;
                player.transform.rotation = Quaternion.Euler(targetRotation);
                Debug.Log($"玩家已传送至场景【{scene.name}】, 位置：{targetPosition}, 旋转：{targetRotation}");
            }

            // 移除监听器，防止多次调用
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    /// <summary>
    /// 带进度条的场景加载（异步）
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    private async Task LoadSceneWithProgress(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; // 先不激活场景，等进度100%

        while (!asyncLoad.isDone)
        {
            // Unity加载进度到0.9时表示资源加载完成，转为100%显示
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // 更新进度条
            if (useProgressBar && loadProgressSlider != null)
            {
                loadProgressSlider.value = progress;

                // 更新进度文本（如果有）
                if (progressText != null)
                {
                    progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
                }
            }

            // 进度满100%后，激活场景
            if (progress >= 1.0f)
            {
                asyncLoad.allowSceneActivation = true;
                Debug.Log($"传送完成：已到达场景【{sceneName}】，目标位置：{targetPosition}");
            }

            await Task.Yield(); // 等待下一帧
        }
    }

    /// <summary>
    /// 无进度条的场景加载（异步）
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    private async Task LoadSceneWithoutProgress(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // 等待加载完成
        while (!asyncLoad.isDone)
        {
            await Task.Yield(); // 等待下一帧
        }

        Debug.Log($"传送完成：已到达场景【{sceneName}】，目标位置：{targetPosition}");
    }
}
