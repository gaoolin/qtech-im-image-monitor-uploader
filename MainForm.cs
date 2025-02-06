using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImMonitorUploader
{
    public partial class MainForm : Form
    {
        private NotifyIcon notifyIcon;
        private ContextMenu notifyMenu;

        // 静态属性，用于在其他地方访问 MainForm 实例
        public static MainForm Instance { get; private set; }



        public MainForm()
        {
            InitializeComponent();
            // 将当前实例赋值给静态属性
            Instance = this;
            // 初始化窗体
            this.Text = "画胶图像采集";
            this.Size = new Size(400, 300);

            // 当窗体加载时自动隐藏
            this.Load += (sender, e) =>
            {
                this.Hide();
                this.ShowInTaskbar = false;
            };

            // 初始化托盘菜单和托盘图标
            notifyMenu = new ContextMenu();
            notifyMenu.MenuItems.Add("显示窗口", (s, e) => ShowWindow());
            notifyMenu.MenuItems.Add("退出程序", (s, e) => ExitApplication());

            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information, // 使用系统默认图标
                ContextMenu = notifyMenu,
                Visible = true,
                Text = "画胶图像上传程序"
            };

            // 双击托盘图标时显示窗口
            notifyIcon.DoubleClick += (s, e) => ShowWindow();

            // 拦截窗体关闭事件，隐藏窗口而不是退出
            this.FormClosing += MainForm_FormClosing;

            // 启动文件监控逻辑
            this.Load += (s, e) =>
            {
                // 启动文件监控逻辑
                ImagesMonitor.Start();
            };

            Logger.Log("画胶图像采集程序已启动！");
        }


        // 拦截关闭事件：点击关闭按钮时隐藏窗口而非退出程序
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 如果是用户点击关闭按钮，则隐藏窗体，并取消退出操作
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                this.ShowInTaskbar = false;
            }
        }

        // 显示窗口方法
        private void ShowWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
        }

        // 退出程序方法
        private void ExitApplication()
        {
            notifyIcon.Dispose();
            Application.Exit();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
        
        private void richTextBoxLog_TextChanged(object sender, EventArgs e)
        {

        }

        // 追加日志到 RichTextBox 的方法
        public void AppendLog(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendLog), message);
            }
            else
            {
                // 将时间戳和消息追加到日志框中
                richTextBoxLog.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                richTextBoxLog.ScrollToCaret();
            }
        }

        // 如果需要在窗体中添加其他初始化工作（例如监控逻辑），可以在这里添加
    }
}
