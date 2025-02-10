using ImMonitorUploader.Models;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ImMonitorUploader.Services
{
    public class LogService
    {
        private readonly string _logUrl;
        private readonly HttpClient _httpClient;

        public LogService()
        {
            _logUrl = ConfigurationManager.AppSettings["LogUrl"];
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// 发送日志到远程服务器
        /// </summary>
        public async Task<bool> SendLogAsync(LogEntry logEntry)
        {
            try
            {
                if (string.IsNullOrEmpty(_logUrl))
                    throw new InvalidOperationException("日志 API 地址未配置");

                // 格式化日期时间，确保符合后端要求
                var logObject = new
                {
                    logDate = logEntry.LogDate.ToString("yyyy-MM-dd HH:mm:ss"),  // 格式化日期
                    message = logEntry.Message,
                    logLevel = logEntry.Level
                };

                string jsonPayload = JsonConvert.SerializeObject(logObject, new JsonSerializerSettings
                {
                    DateFormatString = "yyyy-MM-dd HH:mm:ss"  // 额外指定全局日期格式
                });

                var jsonContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PutAsync(_logUrl, jsonContent);
                string responseContent = await response.Content.ReadAsStringAsync();

                var apiResponse = ApiResponse<bool>.FromJson(responseContent);
                return apiResponse.Code == 200 && apiResponse.Data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("日志上传失败: " + ex.Message);
                return false;
            }
        }
    }
}
