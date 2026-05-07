using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 加载完成时传递的数据包，供 UI 层使用。
/// </summary>
public struct ContentData
{
    public string nodeId;
    public string displayName;
    public string title;
    public string textContent;
    public Sprite[] images;
    public string[] imageDescriptions;
    public AudioClip audioClip;
    public float audioVolume;
    public bool autoPlay;
    public bool isVisible;
    public float autoDismissDuration;
    public CameraOverrideData cameraOverride;
}

/// <summary>
/// 内容系统核心。订阅 ProximityTrigger 事件，加载当前语言对应的文字/图片/音频，
/// 加载完成后触发 OnContentReady / OnContentClear 供 UI 层消费。
/// </summary>
[DefaultExecutionOrder(50)]
public class ContentManager : MonoBehaviour
{
    public static ContentManager Instance { get; private set; }

    [Header("Debug")]
    public bool enableDebugLog = true;

    /// <summary>内容加载完成（携带解析好的 ContentData）</summary>
    public event Action<ContentData> OnContentReady;

    /// <summary>离开节点，UI 应清空显示</summary>
    public event Action OnContentClear;

    /// <summary>手动关闭内容面板（ESC），相机应恢复</summary>
    public event Action OnContentDismissed;

    /// <summary>进入可见节点范围（未触发），UI 应显示提示</summary>
    public event Action<NodeComponent> OnNodeApproached;

    /// <summary>离开可见节点范围（未触发），UI 应隐藏提示</summary>
    public event Action OnApproachCleared;

    /// <summary>当前靠近但未触发的可见节点</summary>
    public NodeComponent PendingNodeComponent { get; private set; }

    /// <summary>是否有待触发的可见节点</summary>
    public bool HasPendingNode => PendingNodeComponent != null;

    /// <summary>音频是否正在播放</summary>
    public bool IsAudioPlaying => audioSource != null && audioSource.isPlaying;

    private ProximityTrigger trigger;
    private AudioSource audioSource;
    private Coroutine loadCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[ContentManager] 场景中存在多个实例，保留第一个");
            Destroy(this);
            return;
        }
        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        trigger = FindFirstObjectByType<ProximityTrigger>();
        if (trigger == null)
        {
            Debug.LogError("[ContentManager] 场景中未找到 ProximityTrigger");
            return;
        }

        trigger.OnNodeEnter += OnNodeEnterHandler;
        trigger.OnNodeExit += OnNodeExitHandler;
        trigger.OnNodeApproached += OnNodeApproachedHandler;
        trigger.OnApproachCleared += OnApproachClearedHandler;

        if (enableDebugLog)
            Debug.Log("[ContentManager] 已订阅 ProximityTrigger 事件");
    }

    private void OnDestroy()
    {
        if (trigger != null)
        {
            trigger.OnNodeEnter -= OnNodeEnterHandler;
            trigger.OnNodeExit -= OnNodeExitHandler;
            trigger.OnNodeApproached -= OnNodeApproachedHandler;
            trigger.OnApproachCleared -= OnApproachClearedHandler;
        }
    }

    private void OnNodeEnterHandler(object sender, NodeTriggerEventArgs e)
    {
        if (e.NodeComponent == null) return;

        if (enableDebugLog)
            Debug.Log($"[ContentManager] 进入节点: {e.NodeId}");

        if (loadCoroutine != null)
            StopCoroutine(loadCoroutine);
        loadCoroutine = StartCoroutine(LoadContentCoroutine(e.NodeComponent));
    }

    private void OnNodeExitHandler(object sender, NodeTriggerEventArgs e)
    {
        if (enableDebugLog)
            Debug.Log($"[ContentManager] 离开节点: {e.NodeId}");

        StopAudio();
        if (loadCoroutine != null)
        {
            StopCoroutine(loadCoroutine);
            loadCoroutine = null;
        }
        PendingNodeComponent = null;
        OnContentClear?.Invoke();
    }

    private void OnNodeApproachedHandler(object sender, NodeTriggerEventArgs e)
    {
        if (e.NodeComponent == null) return;
        PendingNodeComponent = e.NodeComponent;
        OnNodeApproached?.Invoke(e.NodeComponent);
    }

    private void OnApproachClearedHandler()
    {
        PendingNodeComponent = null;
        OnApproachCleared?.Invoke();
    }

    private IEnumerator LoadContentCoroutine(NodeComponent comp)
    {
        var nodeData = comp.ToNodeData();

        // 同步读取文字
        string textContent = null;
        string title = "";
        string displayName = LanguageManager.GetLocalized(nodeData.displayName);

        var locText = LanguageManager.GetLocalizedText(nodeData);
        title = locText.title;
        if (!string.IsNullOrEmpty(locText.file))
        {
            string raw = TextLoader.Load(locText.file);
            textContent = raw != null ? MarkdownParser.ToTmpRichText(raw) : null;
        }

        // 异步加载图片
        Sprite[] sprites = null;
        if (nodeData.images != null && nodeData.images.Count > 0)
        {
            var paths = new string[nodeData.images.Count];
            for (int i = 0; i < nodeData.images.Count; i++)
                paths[i] = nodeData.images[i].file;
            yield return ImageLoader.LoadAll(paths, result => sprites = result);
        }
        else
        {
            sprites = Array.Empty<Sprite>();
        }

        // 收集图片描述（当前语言）
        string[] imgDescs = null;
        if (nodeData.images != null && nodeData.images.Count > 0)
        {
            imgDescs = new string[nodeData.images.Count];
            for (int i = 0; i < nodeData.images.Count; i++)
                imgDescs[i] = LanguageManager.GetLocalized(nodeData.images[i].description);
        }
        else
        {
            imgDescs = Array.Empty<string>();
        }

        // 异步加载音频
        AudioClip audioClip = null;
        var locAudio = LanguageManager.GetLocalizedAudio(nodeData);
        if (!string.IsNullOrEmpty(locAudio.file))
            yield return LoadAudio(locAudio.file, clip => audioClip = clip);

        // 打包数据
        var data = new ContentData
        {
            nodeId = nodeData.id,
            displayName = displayName,
            title = title,
            textContent = textContent,
            images = sprites,
            imageDescriptions = imgDescs,
            audioClip = audioClip,
            audioVolume = locAudio.volume,
            autoPlay = locAudio.autoPlay,
            isVisible = nodeData.isVisible,
            autoDismissDuration = nodeData.autoDismissDuration,
            cameraOverride = nodeData.cameraOverride
        };

        if (enableDebugLog)
            Debug.Log($"[ContentManager] 内容加载完成: {data.nodeId} (文字:{data.textContent != null}, 图片:{data.images.Length}张, 音频:{data.audioClip != null})");

        OnContentReady?.Invoke(data);

        // 自动播放音频
        if (data.autoPlay && data.audioClip != null)
            PlayAudio(data.audioClip, data.audioVolume);
    }

    // ---- 音频加载器（内联） ----

    private IEnumerator LoadAudio(string relativePath, Action<AudioClip> callback)
    {
        string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[ContentManager] 音频文件不存在: {fullPath}");
            callback?.Invoke(null);
            yield break;
        }

        string url = "file://" + fullPath;
        AudioType audioType = GetAudioType(fullPath);

        using var request = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ContentManager] 音频加载失败: {fullPath}\n{request.error}");
            callback?.Invoke(null);
            yield break;
        }

        callback?.Invoke(DownloadHandlerAudioClip.GetContent(request));
    }

    private static AudioType GetAudioType(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".mp3" => AudioType.MPEG,
            ".wav" => AudioType.WAV,
            ".ogg" => AudioType.OGGVORBIS,
            ".aiff" => AudioType.AIFF,
            _ => AudioType.UNKNOWN
        };
    }

    // ---- 音频播放控制 ----

    public void PlayAudio(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
    }

    public void StopAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    /// <summary>手动触发当前 approached 的可见节点（供 UI 按键调用）</summary>
    public void TriggerPendingNode()
    {
        if (trigger != null)
            trigger.TriggerApproachedNode();
    }

    /// <summary>手动播放当前节点音频（供 UI 按钮调用）</summary>
    public void PlayCurrentAudio()
    {
        if (audioSource.clip != null)
            audioSource.Play();
    }

    /// <summary>暂停当前音频</summary>
    public void PauseAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Pause();
    }

    /// <summary>通知外部订阅者面板已手动关闭（供 ContentDisplay.Dismiss 调用）</summary>
    public void NotifyDismissed()
    {
        OnContentDismissed?.Invoke();
    }
}
