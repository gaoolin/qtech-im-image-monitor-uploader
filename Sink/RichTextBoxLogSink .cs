using Serilog.Core;
using Serilog.Events;
using System;
using System.Windows.Forms;

namespace ImMonitorUploader.Sinks
{
    public class RichTextBoxLogSink : ILogEventSink
    {
        private readonly RichTextBox _richTextBox;

        public RichTextBoxLogSink(RichTextBox richTextBox)
        {
            _richTextBox = richTextBox ?? throw new ArgumentNullException(nameof(richTextBox));
        }

        public void Emit(LogEvent logEvent)
        {
            // 渲染日志消息
            var logMessage = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss") + " [" + logEvent.Level + "] " + logEvent.RenderMessage();

            // 确保线程安全地更新 UI
            if (_richTextBox.InvokeRequired)
            {
                _richTextBox.BeginInvoke((MethodInvoker)(() =>
                {
                    _richTextBox.AppendText(logMessage + Environment.NewLine);
                    _richTextBox.ScrollToCaret();
                }));
            }
            else
            {
                _richTextBox.AppendText(logMessage + Environment.NewLine);
                _richTextBox.ScrollToCaret();
                Logger.PurgeOldLogsFromUI();
            }
        }
    }
}