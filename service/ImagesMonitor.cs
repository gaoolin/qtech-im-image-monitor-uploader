using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using ImMonitorUploader;

public class ImagesMonitor
{
    private static HttpClient client = new HttpClient();
    private static string uploadUrl = "http://10.170.6.40:31555/s3/files/upload/bytes";
    private static string bucketName = "pic-epoxy-inspection";
    private static string directoryToWatch = @"D:\EpoxyInsp";

    private static string[] imageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

    private static string logUrl = "http://10.170.6.40/im/aa/pic/log";  // 替换为实际的日志接收接口

    private static FileSystemWatcher watcher;

    // 启动文件监控
    public static void Start()
    {
        watcher = new FileSystemWatcher(directoryToWatch)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };

        watcher.Created += (sender, e) => OnFileCreated(e);
        watcher.EnableRaisingEvents = true;
    }

    private static void OnFileCreated(FileSystemEventArgs e)
    {
        if (IsImageFile(e.FullPath))
        {
            Logger.Log($"检测到图片：{Path.GetFileName(e.FullPath)}");

            Task.Run(async () =>
            {
                await Task.Delay(1000);
                await UploadFileAsync(e.FullPath);
            });
        }
    }

    private static bool IsImageFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        foreach (var ext in imageExtensions)
        {
            if (extension == ext)
            {
                return true;
            }
        }
        return false;
    }

    private static async Task UploadFileAsync(string filePath)
    {
        try
        {
            string fileName = Path.GetFileName(filePath);
            string encodedBucketName = Uri.EscapeDataString(bucketName);
            string encodedFileName = Uri.EscapeDataString(fileName);

            string uploadUrlWithParams = $"{uploadUrl}?bucketName={encodedBucketName}&fileName={encodedFileName}";

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var content = new StreamContent(fs))
                {
                    content.Headers.Add("Content-Type", "application/octet-stream");

                    var response = await client.PostAsync(uploadUrlWithParams, content);

                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Log($"文件上传成功: {Path.GetFileName(filePath)}");
                    }
                    else
                    {
                        Logger.Log($"文件上传失败: {Path.GetFileName(filePath)}, 状态码: {response.StatusCode}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"上传文件时出错: {ex.Message}");
        }
    }

    // 释放 HttpClient 资源的方法
    public static void Dispose()
    {
        if (client != null)
        {
            client.Dispose();
            client = null;
        }

        if (watcher != null)
        {
            watcher.Dispose();
            watcher = null;
        }
    }
}
