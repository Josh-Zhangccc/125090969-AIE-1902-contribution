using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// UI 桥接脚本。挂载到 Canvas 上，在 Inspector 中拖拽绑定各 UI 组件。
/// 订阅 ContentManager.OnContentReady / OnContentClear 自动更新显示。
///
/// 无需手写 UI 创建代码——在 Unity Editor 中创建好 Canvas/Panel/Text/Image，
/// 拖入下方槽位即可。
/// </summary>
[DefaultExecutionOrder(100)]
public class ContentDisplay : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("内容面板根节点 GameObject，进入节点时显示，离开时隐藏")]
    public GameObject contentPanel;

    [Header("Text")]
    [Tooltip("标题文字（TextMeshPro）")]
    public TextMeshProUGUI titleText;
    [Tooltip("正文内容（TextMeshPro），可显示多行")]
    public TextMeshProUGUI bodyText;

    [Header("Image")]
    [Tooltip("显示当前图片的 Image 组件")]
    public Image mainImage;
    [Tooltip("图片描述文字")]
    public TextMeshProUGUI imageDescText;
    [Tooltip("页码指示，如 \"1 / 3\"")]
    public TextMeshProUGUI pageIndicator;

    [Header("Buttons")]
    [Tooltip("上一张图片按钮")]
    public Button prevImageButton;
    [Tooltip("下一张图片按钮")]
    public Button nextImageButton;

    [Header("Audio Buttons (可选)")]
    [Tooltip("音频播放按钮")]
    public Button playAudioButton;
    [Tooltip("音频暂停按钮")]
    public Button pauseAudioButton;

    [Header("Dismiss")]
    [Tooltip("关闭面板的按钮（可选）")]
    public Button closeButton;
    [Tooltip("关闭面板的按键（Escape / Enter / Space 等）")]
    public Key dismissKey = Key.Escape;
    [Tooltip("是否在离开节点时自动关闭 UI（默认关闭，即手动关闭模式）")]
    public bool autoHideOnExit = false;

    [Header("Trigger")]
    [Tooltip("按下此键触发已靠近的可见节点")]
    public Key triggerKey = Key.F;

    [Header("Prompt")]
    [Tooltip("靠近节点时的提示文字（如\"按 F 查看\"）")]
    public TextMeshProUGUI promptText;

    [Header("Invisible Node Panel (底部字幕)")]
    [Tooltip("隐形节点专属面板根节点")]
    public GameObject invisibleContentPanel;
    [Tooltip("隐形节点标题")]
    public TextMeshProUGUI invisibleTitleText;
    [Tooltip("隐形节点正文")]
    public TextMeshProUGUI invisibleBodyText;

    [Header("Play/Mute")]
    [Tooltip("播放/静音切换按钮（仅隐形节点面板使用）")]
    public Button playMuteButton;
    [Tooltip("播放/静音按钮文字标签")]
    public TextMeshProUGUI playMuteButtonLabel;

    private ContentData currentData;
    private int currentImageIndex;
    private static bool cjkWarningLogged;
    private bool panelVisible;
    private InputAction dismissAction;
    private InputAction triggerAction;
    private Coroutine autoDismissCoroutine;

    private void Awake()
    {
        // 尝试加载 CJK fallback 字体（由 Tools > Content System > Setup CJK Fallback Font 创建）
        TrySetupCJKFallback();

        if (ContentManager.Instance == null)
        {
            Debug.LogError("[ContentDisplay] 场景中未找到 ContentManager.Instance");
            return;
        }

        ContentManager.Instance.OnContentReady += OnContentReady;
        ContentManager.Instance.OnContentClear += OnContentClear;
        ContentManager.Instance.OnNodeApproached += OnNodeApproached;
        ContentManager.Instance.OnApproachCleared += OnApproachCleared;

        if (prevImageButton != null)
            prevImageButton.onClick.AddListener(ShowPrevImage);
        if (nextImageButton != null)
            nextImageButton.onClick.AddListener(ShowNextImage);
        if (playAudioButton != null)
            playAudioButton.onClick.AddListener(OnPlayAudioClicked);
        if (pauseAudioButton != null)
            pauseAudioButton.onClick.AddListener(OnPauseAudioClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(Dismiss);
        if (playMuteButton != null)
            playMuteButton.onClick.AddListener(OnPlayMuteClicked);

        if (contentPanel != null)
            contentPanel.SetActive(false);
        if (invisibleContentPanel != null)
            invisibleContentPanel.SetActive(false);

        SetImageNavButtonsVisible(false);

        dismissAction = new InputAction(name: "Dismiss", binding: $"<Keyboard>/{dismissKey.ToString().ToLower()}");
        dismissAction.performed += OnDismissAction;

        triggerAction = new InputAction(name: "Trigger", binding: $"<Keyboard>/{triggerKey.ToString().ToLower()}");
        triggerAction.performed += OnTriggerAction;

        Debug.Log("[ContentDisplay] 初始化完成，已订阅 ContentManager 事件");
    }

    private void OnEnable()
    {
        dismissAction?.Enable();
        triggerAction?.Enable();
    }

    private void OnDisable()
    {
        dismissAction?.Disable();
        triggerAction?.Disable();
    }

    private void OnDismissAction(InputAction.CallbackContext ctx)
    {
        if (panelVisible)
            Dismiss();
    }

    private void OnTriggerAction(InputAction.CallbackContext ctx)
    {
        if (ContentManager.Instance != null
            && ContentManager.Instance.HasPendingNode
            && !panelVisible)
        {
            ContentManager.Instance.TriggerPendingNode();
        }
    }

    private void OnNodeApproached(NodeComponent comp)
    {
        if (promptText != null)
        {
            promptText.text = $"按 {triggerKey} 查看";
            promptText.gameObject.SetActive(true);
        }
    }

    private void OnApproachCleared()
    {
        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void OnPlayMuteClicked()
    {
        if (ContentManager.Instance == null) return;

        if (ContentManager.Instance.IsAudioPlaying)
        {
            ContentManager.Instance.PauseAudio();
            if (playMuteButtonLabel != null)
                playMuteButtonLabel.text = "▶ 播放";
        }
        else
        {
            ContentManager.Instance.PlayCurrentAudio();
            if (playMuteButtonLabel != null)
                playMuteButtonLabel.text = "🔇 静音";
        }
    }

    private static void TrySetupCJKFallback()
    {
        var cjkFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/CJKFallback SDF");
        if (cjkFont == null) return;

        // Validate font asset integrity — a broken atlas texture will crash TMPro
        if (cjkFont.atlasTextures == null || cjkFont.atlasTextures.Length == 0 || cjkFont.atlasTextures[0] == null)
        {
            Debug.LogWarning("[ContentDisplay] CJK fallback font asset exists but has corrupted textures. "
                + "Run Tools > Content System > Setup CJK Fallback Font to recreate it.");
            return;
        }

        var fallbackList = TMP_Settings.fallbackFontAssets;
        if (!fallbackList.Contains(cjkFont))
        {
            fallbackList.Add(cjkFont);
            Debug.Log("[ContentDisplay] 已加载 CJK fallback 字体");
        }
    }

    private void OnDestroy()
    {
        if (ContentManager.Instance != null)
        {
            ContentManager.Instance.OnContentReady -= OnContentReady;
            ContentManager.Instance.OnContentClear -= OnContentClear;
            ContentManager.Instance.OnNodeApproached -= OnNodeApproached;
            ContentManager.Instance.OnApproachCleared -= OnApproachCleared;
        }

        if (prevImageButton != null)
            prevImageButton.onClick.RemoveListener(ShowPrevImage);
        if (nextImageButton != null)
            nextImageButton.onClick.RemoveListener(ShowNextImage);
        if (playAudioButton != null)
            playAudioButton.onClick.RemoveListener(OnPlayAudioClicked);
        if (pauseAudioButton != null)
            pauseAudioButton.onClick.RemoveListener(OnPauseAudioClicked);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Dismiss);
        if (playMuteButton != null)
            playMuteButton.onClick.RemoveListener(OnPlayMuteClicked);

        if (dismissAction != null)
        {
            dismissAction.performed -= OnDismissAction;
            dismissAction.Dispose();
            dismissAction = null;
        }

        if (triggerAction != null)
        {
            triggerAction.performed -= OnTriggerAction;
            triggerAction.Dispose();
            triggerAction = null;
        }
    }

    private void OnContentReady(ContentData data)
    {
        currentData = data;
        currentImageIndex = 0;

        // 内容就绪，隐藏靠近提示
        if (promptText != null)
            promptText.gameObject.SetActive(false);

        if (data.isVisible)
        {
            // 可见节点：使用主内容面板
            if (contentPanel != null)
                contentPanel.SetActive(true);
            if (invisibleContentPanel != null)
                invisibleContentPanel.SetActive(false);
            panelVisible = true;

            if (titleText != null)
                titleText.text = data.title;

            if (bodyText != null)
                bodyText.text = data.textContent ?? "";

            CheckCJKWarning(data.title, data.textContent);

            if (data.images != null && data.images.Length > 0)
            {
                ShowImageAtIndex(0);
                SetImageNavButtonsVisible(data.images.Length > 1);
            }
            else
            {
                if (mainImage != null)
                    mainImage.sprite = null;
                if (imageDescText != null)
                    imageDescText.text = "";
                if (pageIndicator != null)
                    pageIndicator.text = "";
                SetImageNavButtonsVisible(false);
            }
        }
        else
        {
            // 不可见节点：无文本内容则不显示字幕面板
            if (string.IsNullOrWhiteSpace(data.textContent))
            {
                Debug.LogWarning($"[ContentDisplay] 不可见节点 \"{data.nodeId}\" 无文本内容，跳过字幕面板");

                if (contentPanel != null)
                    contentPanel.SetActive(false);
                if (invisibleContentPanel != null)
                    invisibleContentPanel.SetActive(false);
                return;
            }

            // 不可见节点：使用底部字幕面板
            if (contentPanel != null)
                contentPanel.SetActive(false);
            if (invisibleContentPanel != null)
                invisibleContentPanel.SetActive(true);
            panelVisible = true;

            if (invisibleTitleText != null)
                invisibleTitleText.text = data.title;

            if (invisibleBodyText != null)
                invisibleBodyText.text = data.textContent;

            if (playMuteButtonLabel != null)
                playMuteButtonLabel.text = "🔇 静音";
        }

        // 自动关闭计时器
        if (data.autoDismissDuration > 0f)
        {
            if (autoDismissCoroutine != null)
                StopCoroutine(autoDismissCoroutine);
            autoDismissCoroutine = StartCoroutine(AutoDismissCoroutine(data.autoDismissDuration));
        }
    }

    private System.Collections.IEnumerator AutoDismissCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Dismiss();
    }

    private void OnContentClear()
    {
        if (autoHideOnExit)
        {
            if (invisibleContentPanel != null)
                invisibleContentPanel.SetActive(false);
            Dismiss();
        }
    }

    /// <summary>手动关闭内容面板（可绑定到关闭按钮或通过 Esc 键调用）</summary>
    public void Dismiss()
    {
        if (autoDismissCoroutine != null)
        {
            StopCoroutine(autoDismissCoroutine);
            autoDismissCoroutine = null;
        }

        if (contentPanel != null)
            contentPanel.SetActive(false);
        if (invisibleContentPanel != null)
            invisibleContentPanel.SetActive(false);
        if (promptText != null)
            promptText.gameObject.SetActive(false);
        panelVisible = false;

        if (ContentManager.Instance != null)
        {
            ContentManager.Instance.StopAudio();
            ContentManager.Instance.NotifyDismissed();
        }
    }

    private void ShowImageAtIndex(int index)
    {
        if (currentData.images == null || currentData.images.Length == 0) return;
        if (index < 0 || index >= currentData.images.Length) return;

        currentImageIndex = index;

        if (mainImage != null)
            mainImage.sprite = currentData.images[index];

        if (imageDescText != null && currentData.imageDescriptions != null)
            imageDescText.text = currentData.imageDescriptions[index];

        if (pageIndicator != null)
            pageIndicator.text = $"{index + 1} / {currentData.images.Length}";
    }

    public void ShowNextImage()
    {
        if (currentData.images == null || currentData.images.Length == 0) return;
        int next = (currentImageIndex + 1) % currentData.images.Length;
        ShowImageAtIndex(next);
    }

    public void ShowPrevImage()
    {
        if (currentData.images == null || currentData.images.Length == 0) return;
        int prev = (currentImageIndex - 1 + currentData.images.Length) % currentData.images.Length;
        ShowImageAtIndex(prev);
    }

    private void SetImageNavButtonsVisible(bool visible)
    {
        if (prevImageButton != null)
            prevImageButton.gameObject.SetActive(visible);
        if (nextImageButton != null)
            nextImageButton.gameObject.SetActive(visible);
    }

    private static void CheckCJKWarning(string title, string body)
    {
        if (cjkWarningLogged) return;

        string combined = (title ?? "") + (body ?? "");
        if (ContainsCJK(combined) && !HasCJKFallback())
        {
            cjkWarningLogged = true;
            Debug.LogWarning(
                "[ContentDisplay] 中文内容已加载，但 TMPro 没有 CJK 后备字体，将导致重复的 "
                + "\"character was not found\" 警告。\n"
                + "修复: Unity Editor → Tools → Content System → Setup CJK Fallback Font");
        }
    }

    private static bool ContainsCJK(string text)
    {
        foreach (char c in text)
        {
            if ((c >= 0x4E00 && c <= 0x9FFF)  // CJK Unified Ideographs
                || (c >= 0x3400 && c <= 0x4DBF)   // CJK Extension A
                || (c >= 0x3000 && c <= 0x303F)   // CJK Punctuation
                || (c >= 0xFF00 && c <= 0xFFEF)   // Fullwidth Forms
                || (c >= 0x2000 && c <= 0x206F))  // General Punctuation (includes ‘ “ etc.)
                return true;
        }
        return false;
    }

    private static bool HasCJKFallback()
    {
        foreach (var fb in TMP_Settings.fallbackFontAssets)
        {
            if (fb != null && fb.name.Contains("CJK")) return true;
        }
        return false;
    }

    private void OnPlayAudioClicked()
    {
        ContentManager.Instance?.PlayCurrentAudio();
    }

    private void OnPauseAudioClicked()
    {
        ContentManager.Instance?.PauseAudio();
    }
}
