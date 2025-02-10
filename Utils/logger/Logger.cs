using ImMonitorUploader.Models;
using ImMonitorUploader.Repositories;
using ImMonitorUploader.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImMonitorUploader
{
    public static class Logger
    {
        private static readonly string logFilePath = "log.txt"; // 日志文件路径
        private static readonly TimeSpan retentionPeriod = TimeSpan.FromHours(1); // UI 日志保留时间
        private static readonly bool WriteToFile = false; // 是否写入本地文件

        private static List<string> logQueue = new List<string>(); // 日志缓存
        private static LogRepository _logRepository = new LogRepository();
        private static readonly LogService _logService = new LogService();

        /// <summary>
        /// 记录日志，并同时输出到 UI 和数据库
        /// </summary>
        /// <param name="message">日志内容</param>
        public static async Task LogAsync(string message, LogLevel level = LogLevel.Info)
        {
            LogEntry logEntry = new LogEntry(message, level);
            // 1. 使用 Serilog 记录日志
            switch (level)
            {
                case LogLevel.Info:
                    Log.Information(logEntry.ToString());
                    break;
                case LogLevel.Warning:
                    Log.Warning(logEntry.ToString());
                    break;
                case LogLevel.Error:
                    Log.Error(logEntry.ToString());
                    break;
                default:
                    Log.Information(logEntry.ToString());
                    break;
            }
            // 1. 追加日志到 UI
            AppendLogToUI(logEntry.ToString());

            // 2. 写入日志到 Oracle
            //_logRepository.InsertLog(logEntry); // 记录数据库

            bool success = await _logService.SendLogAsync(logEntry);
            if (!success)
            {
                AppendLogToUI(new LogEntry("日志上传失败，请检查网络！", LogLevel.Error).ToString());
            }

            // 3. 可选：写入本地文件
            if (WriteToFile)
            {
                AppendLogToFile(logEntry.ToString());
            }
        }

        /// <summary>
        /// 在 UI 界面上追加日志（如果 MainForm 存在）
        /// </summary>
        /// <param name="logEntry">日志内容</param>

        public static void AppendLogToUI(string logEntry)
        {
            // UI 未初始化，先缓存日志
            if (MainForm.Instance == null || !MainForm.Instance.IsHandleCreated)
            {
                logQueue.Add(logEntry);
                return;
            }

            // UI 可用时，刷新缓存日志
            if (logQueue.Count > 0)
            {
                foreach (var log in logQueue)
                {
                    MainForm.Instance.richTextBoxLog.AppendText(log + Environment.NewLine);
                }
                logQueue.Clear(); // 清空缓存
            }

            // 线程安全调用 UI 更新
            if (MainForm.Instance.InvokeRequired)
            {
                MainForm.Instance.BeginInvoke((MethodInvoker)(() =>
                {
                    MainForm.Instance.richTextBoxLog.AppendText(logEntry + Environment.NewLine);
                    MainForm.Instance.richTextBoxLog.ScrollToCaret();
                    PurgeOldLogsFromUI();
                }));
            }
            else
            {
                MainForm.Instance.richTextBoxLog.AppendText(logEntry + Environment.NewLine);
                MainForm.Instance.richTextBoxLog.ScrollToCaret();
                PurgeOldLogsFromUI();
            }
        }

        /// <summary>
        /// 仅保留 24 小时内的日志（UI `RichTextBox`）
        /// </summary>
        public static void PurgeOldLogsFromUI()
        {
            if (MainForm.Instance != null && MainForm.Instance.richTextBoxLog != null)
            {
                string[] lines = MainForm.Instance.richTextBoxLog.Lines;
                var newLines = new List<string>();

                foreach (string line in lines)
                {
                    if (line.StartsWith("[") && line.Contains("]"))
                    {
                        int endIndex = line.IndexOf("]");
                        string timestampStr = line.Substring(1, endIndex - 1);
                        if (DateTime.TryParseExact(timestampStr, "yyyy-MM-dd HH:mm:ss",
                                                   System.Globalization.CultureInfo.InvariantCulture,
                                                   System.Globalization.DateTimeStyles.None, out DateTime timestamp))
                        {
                            if (DateTime.Now - timestamp < retentionPeriod)
                            {
                                newLines.Add(line);
                            }
                        }
                        else
                        {
                            newLines.Add(line);
                        }
                    }
                    else
                    {
                        newLines.Add(line);
                    }
                }

                MainForm.Instance.richTextBoxLog.Lines = newLines.ToArray();
            }
        }

        /// <summary>
        /// 追加日志到本地文件
        /// </summary>
        /// <param name="logEntry">日志内容</param>
        private static void AppendLogToFile(string logEntry)
        {
            try
            {
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("日志写入本地文件失败：" + ex.Message);
            }
        }
    }
}
