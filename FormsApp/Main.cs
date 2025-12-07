
using FormsApp.Core;
using System;
using System.Drawing;

namespace FormsApp {
    public partial class Main : Form {
        private VideoProcessingManager _videoManager;

        public Main() {
            InitializeComponent();
            _videoManager = new VideoProcessingManager();
            _videoManager.OnFrameUpdated += () => Invoke(() => pictureBox1.Refresh());
            _videoManager.OnStatusUpdated += UpdateStatus;
            _videoManager.OnError += ShowError;
            InitializeUI();
        }

        private void InitializeUI() {

            pictureBox1.Paint += (sender, e) => {
                using var bitmap = _videoManager.CurrentBitmap;
                if (bitmap != null) {
                    e.Graphics.DrawImage(bitmap, 0, 0, pictureBox1.Width, pictureBox1.Height);
                }
            };

            textBox1.Text = "rtmp://live-mikudemo.cloudvdn.com/mikudemo/timestamps.m3u8";
            textBox2.Text = _videoManager.FrameInterval.ToString();

            textBox2.TextChanged += (sender, e) => {
                if (int.TryParse(textBox2.Text, out int frameInterval) && frameInterval > 0) {
                    _videoManager.FrameInterval = frameInterval;
                }
            };

            button1.Text = "打开";
            button1.Click += (sender, e) => {
                button1.Enabled = !button1.Enabled;
                if (_videoManager.IsRunning) {
                    _videoManager.Stop();
                    button1.Text = "打开";
                } else {
                    button1.Text = "链接中...";
                    var sourceUrl = textBox1.Text.Trim();
                    _videoManager.Start(sourceUrl);
                    button1.Text = "关闭";
                }
                button1.Enabled = !button1.Enabled;
            };
        }

        private void UpdateStatus(string status) {
            if (InvokeRequired) {
                BeginInvoke((Action<string>)UpdateStatus, status);
                return;
            }
            Text = status;
        }

        private void ShowError(string errorMessage) {
            if (InvokeRequired) {
                BeginInvoke((Action<string>)ShowError, errorMessage);
                return;
            }
            button1.Text = "打开";
            MessageBox.Show(errorMessage, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        protected override void OnFormClosing(FormClosingEventArgs e) {
            _videoManager.Dispose();
            base.OnFormClosing(e);
        }
    }
}
