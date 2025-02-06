using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImMonitorUploader
{
    public static class Logger
    {
        /// <summary>
        /// 记录日志信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void Log(string message)
        {
            // 如果 MainForm 已经创建，则调用 AppendLog 方法
            if (MainForm.Instance != null)
            {
                MainForm.Instance.AppendLog(message);
            }
        }
    }
}
