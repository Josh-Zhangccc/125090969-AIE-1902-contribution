using System.IO;
using UnityEngine;

public static class TextLoader
{
    /// <summary>
    /// 从 StreamingAssets 同步读取文本文件。
    /// 仅适用于 Windows/Mac 本地平台；Android 需要改用 UnityWebRequest 异步读取。
    /// </summary>
    /// <param name="relativePath">相对于 StreamingAssets 的路径，如 "Content/Text/eastGate.zh.md"</param>
    /// <returns>文件内容，若文件不存在则返回 null 并打印错误</returns>
    public static string Load(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return null;

        string fullPath = Path.Combine(Application.streamingAssetsPath, relativePath);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[TextLoader] 文件不存在: {fullPath}");
            return null;
        }

        return File.ReadAllText(fullPath);
    }
}
