using System;

namespace ImMonitorUploader.Models
{
    public class LogEntry
    {
        public DateTime LogDate { get; set; }  // 日志时间
        public string Message { get; set; }    // 日志内容
        public LogLevel Level { get; set; }    // 日志级别（Info, Warning, Error）

        public LogEntry(string message, LogLevel level = LogLevel.Info)
        {
            LogDate = DateTime.Now;
            Message = message;
            Level = level;
        }

        public override string ToString()
        {
            return $"[{LogDate:yyyy-MM-dd HH:mm:ss}] [{Level}] {Message}";
        }
    }

    // 定义日志级别
    public enum LogLevel
    {
        Info,      // 普通信息
        Warning,   // 警告信息
        Error      // 错误信息
    }
}
