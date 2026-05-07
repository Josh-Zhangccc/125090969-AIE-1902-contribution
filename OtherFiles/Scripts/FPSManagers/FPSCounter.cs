using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public static FPSCounter Instance;

    [Header("FPS 刷新间隔")]
    public float updateInterval = 1f;

    public float CurrentFps { get; private set; }

    private float _timer;
    private int _frameCount;

    private void Awake()
    {
        // 单例
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        _frameCount++;
        _timer += Time.deltaTime;

        if (_timer >= updateInterval)
        {
            CurrentFps = _frameCount / _timer;
            _frameCount = 0;
            _timer = 0f;
        }
    }
}