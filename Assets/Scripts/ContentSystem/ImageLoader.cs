using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class ImageLoader
{
    /// <summary>
    /// 从 StreamingAssets 加载单张图片为 Sprite。
    /// </summary>
    /// <param name="callback">加载完成回调，失败时传入 null</param>
    public static IEnumerator Load(string relativePath, Action<Sprite> callback)
    {
        if (string.IsNullOrEmpty(relativePath))
        {
            callback?.Invoke(null);
            yield break;
        }

        string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[ImageLoader] 文件不存在: {fullPath}");
            callback?.Invoke(null);
            yield break;
        }

        string url = "file://" + fullPath;
        using var request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ImageLoader] 加载失败: {fullPath}\n{request.error}");
            callback?.Invoke(null);
            yield break;
        }

        var texture = DownloadHandlerTexture.GetContent(request);
        var sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
        callback?.Invoke(sprite);
    }

    /// <summary>
    /// 批量加载图片。顺序加载完成后回调，加载失败的项为 null。
    /// </summary>
    public static IEnumerator LoadAll(string[] relativePaths, Action<Sprite[]> callback)
    {
        if (relativePaths == null || relativePaths.Length == 0)
        {
            callback?.Invoke(Array.Empty<Sprite>());
            yield break;
        }

        var sprites = new Sprite[relativePaths.Length];

        for (int i = 0; i < relativePaths.Length; i++)
        {
            Sprite result = null;
            yield return Load(relativePaths[i], s => result = s);
            sprites[i] = result;
        }

        callback?.Invoke(sprites);
    }
}
