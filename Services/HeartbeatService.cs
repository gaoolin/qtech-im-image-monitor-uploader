using Confluent.Kafka;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace ImMonitorUploader
{
    public class HeartbeatService
    {
        private Timer heartbeatTimer;

        /// <summary>
        /// 启动心跳检测
        /// </summary>
        public void Start()
        {
            int interval = int.TryParse(ConfigurationManager.AppSettings["HeartbeatInterval"], out int result) ? result : 60000;
            heartbeatTimer = new Timer(async state => await SendHeartbeatAsync(), null, 0, interval);
            Log.Information("心跳服务已启动，间隔：" + interval + " 毫秒");
        }

        /// <summary>
        /// 发送心跳信息到 Kafka
        /// </summary>
        private async Task SendHeartbeatAsync()
        {
            try
            {
                var heartBeatData = new
                {
                    Timestamp = DateTime.UtcNow.ToString("o"),
                    InstanceId = Environment.MachineName + "-" + System.Diagnostics.Process.GetCurrentProcess().Id,
                    ApplicationName = "ImMonitorUploader",
                    ApplicationVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                    MachineName = Environment.MachineName,
                    OSVersion = Environment.OSVersion.ToString(),
                    Uptime = TimeSpan.FromMilliseconds(Environment.TickCount).ToString(),
                    CpuUsage = GetCpuUsage(),
                    MemoryUsage = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024 + " MB",
                    DiskUsage = GetDiskUsage(),
                    IpAddress = GetLocalIPAddress(),
                    KafkaStatus = await CheckKafkaConnection()
                };

                string jsonPayload = JsonConvert.SerializeObject(heartBeatData);
                string kafkaTopic = ConfigurationManager.AppSettings["KafkaTopicHeartbeat"];

                if (string.IsNullOrEmpty(kafkaTopic))
                {
                    Log.Error("Kafka 未配置！");
                    return;
                }

                await KafkaProducerSingleton.Producer.ProduceAsync(kafkaTopic, new Message<Null, string> { Value = jsonPayload });
                Log.Information($"心跳已发送到 Kafka");
            }
            catch (Exception ex)
            {
                Log.Error($"发送心跳时发生异常：{ex.Message}");
            }
        }

        public void Stop()
        {
            heartbeatTimer?.Change(Timeout.Infinite, 0);
            Log.Warning("心跳服务已停止。");
        }

        private static string GetLocalIPAddress()
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "Unknown";
        }

        private static double GetCpuUsage()
        {
            var cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
            cpuCounter.NextValue();
            System.Threading.Thread.Sleep(500);
            return Math.Round(cpuCounter.NextValue(), 2);
        }

        private static long GetDiskUsage()
        {
            var drive = new System.IO.DriveInfo("C");
            return drive.AvailableFreeSpace / (1024 * 1024 * 1024);
        }

        private static async Task<bool> CheckKafkaConnection()
        {
            try
            {
                var testMessage = new Message<Null, string> { Value = "Kafka connection test" };
                await KafkaProducerSingleton.Producer.ProduceAsync(ConfigurationManager.AppSettings["KafkaTopicHeartbeat"], testMessage);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Kafka 连接检查失败: {ex.Message}");
                return false;
            }
        }
    }
}
