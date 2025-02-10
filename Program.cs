using ImMonitorUploader.Sinks;
using Serilog;
using System;
using System.Threading;
using System.Windows.Forms;

namespace ImMonitorUploader
{
    static class Program
    {
        private const string MutexName = "ImMonitorUploader_SingleInstanceMutex";

        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("程序已经在运行中！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 启动主窗体
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // 启动时等待 MainForm 实例化完毕
                var mainForm = new MainForm();

                // 配置日志
                ConfigureLogging(mainForm);

                // 启动窗体
                Application.Run(mainForm);
            }
        }

        private static void ConfigureLogging(MainForm mainForm)
        {
            // 获取 MainForm 的 RichTextBox 控件
            var richTextBoxLog = mainForm.richTextBoxLog;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()  // 配置最低日志级别
                .WriteTo.Console()  // 将日志输出到控制台
                // .WriteTo.Sink(new KafkaLogSink(kafkaServers, kafkaTopic))  // 使用自定义 Kafka Sink
                .WriteTo.Sink(new RichTextBoxLogSink(richTextBoxLog))  // 使用自定义 Sink 输出到 RichTextBox
                .CreateLogger();
        }
    }
}
