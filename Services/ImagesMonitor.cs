using Confluent.Kafka;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

public class ImagesMonitor
{
    private static HttpClient client = new HttpClient();
    private static readonly string uploadUrl = ConfigurationManager.AppSettings["UploadUrl"];
    private static readonly string bucketName = ConfigurationManager.AppSettings["BucketName"];
    private static readonly string directoryToWatch = ConfigurationManager.AppSettings["DirectoryToWatch"];
    private static readonly string kafkaServers = ConfigurationManager.AppSettings["KafkaServers"];
    private static readonly string kafkaTopic = ConfigurationManager.AppSettings["KafkaTopicAaGlueLogs"];

    private static readonly string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

    private static FileSystemWatcher watcher;

    // 启动前检查配置是否完整
    private static void ValidateConfig()
    {
        if (string.IsNullOrWhiteSpace(uploadUrl) ||
            string.IsNullOrWhiteSpace(bucketName) ||
            string.IsNullOrWhiteSpace(directoryToWatch) ||
            string.IsNullOrWhiteSpace(kafkaServers) ||
            string.IsNullOrWhiteSpace(kafkaTopic))
        {
            throw new NotSupportedException("应用程序配置缺失，请检查 app.config");
        }
    }

    // 启动文件监控
    public static void Start()
    {
        ValidateConfig();

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
            Log.Information($"检测到图片：{Path.GetFileName(e.FullPath)}");
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
        return Array.Exists(imageExtensions, ext => ext == extension);
    }

    private static async Task UploadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Log.Error($"文件未找到: {filePath}");
            return;
        }

        if (!await WaitForFileReady(filePath))
        {
            await LogToKafka("文件长时间未就绪，放弃上传", filePath);
            return;
        }

        int retryCount = 0;
        const int maxRetries = 3;
        bool success = false;

        while (!success && retryCount < maxRetries)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                string encodedBucketName = Uri.EscapeDataString(bucketName);
                string encodedFileName = Uri.EscapeDataString(fileName);
                string uploadUrlWithParams = $"{uploadUrl}?bucketName={encodedBucketName}&fileName={encodedFileName}";

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var content = new StreamContent(fs))
                {
                    content.Headers.Add("Content-Type", "application/octet-stream");
                    var response = await client.PostAsync(uploadUrlWithParams, content);

                    if (response.IsSuccessStatusCode)
                    {
                        await LogToKafka("文件上传成功", filePath);
                        success = true;
                        Log.Information($"文件上传成功: {Path.GetFileName(filePath)}");
                    }
                    else
                    {
                        await LogToKafka($"文件上传失败，状态码: {response.StatusCode}", filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                await LogToKafka($"上传文件时出错: {ex.Message}", filePath);
            }

            retryCount++;
            if (!success)
            {
                await Task.Delay(1000); // 重试前延时
            }
        }

        if (!success)
        {
            await LogToKafka("文件上传失败，达到最大重试次数", filePath);
        }
    }

    // Kafka 日志发送
    private static async Task LogToKafka(string message, string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Log.Error($"文件未找到: {filePath}");
                return;
            }

            var fileInfo = new FileInfo(filePath);
            var logMessage = new
            {
                Timestamp = DateTime.UtcNow.ToString("o"),
                Message = message,
                FileName = fileInfo.Name,
                FilePath = fileInfo.FullName,
                FileSize = fileInfo.Length,
                FileType = fileInfo.Extension,
                UploadTime = DateTime.UtcNow.ToString("o")
            };

            string jsonLog = JsonConvert.SerializeObject(logMessage);
            await KafkaProducerSingleton.Producer.ProduceAsync(kafkaTopic, new Message<Null, string> { Value = jsonLog });
        }
        catch (Exception ex)
        {
            Log.Error($"Kafka 日志发送失败: {ex.Message}");
        }
    }

    // 等待文件可用
    private static async Task<bool> WaitForFileReady(string filePath, int maxRetries = 5, int delayMilliseconds = 500)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                await Task.Delay(delayMilliseconds);
            }
        }
        return false;
    }

    // 释放资源
    public static void Dispose()
    {
        client?.Dispose();
        client = null;

        watcher?.Dispose();
        watcher = null;

        if (producerInstance.IsValueCreated)
        {
            KafkaProducer?.Flush(TimeSpan.FromSeconds(5));
            KafkaProducer?.Dispose();
        }
    }
}