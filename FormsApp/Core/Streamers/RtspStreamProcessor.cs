using FormsApp.Core.Interfaces;
using OpenCvSharp;

namespace FormsApp.Core.Streamers {
    /// <summary>
    /// RTSP视频流处理器
    /// 用于处理RTSP流或本地摄像头，支持按指定间隔发送视频帧
    /// </summary>
    public class RtspStreamProcessor : IVideoStreamProcessor {
        /// <summary>
        /// OpenCV视频捕获对象
        /// </summary>
        private VideoCapture _capture;

        /// <summary>
        /// 用于取消视频处理任务的令牌源
        /// </summary>
        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// 视频源URL
        /// </summary>
        private string? _sourceUrl { get; set; }

        /// <summary>
        /// 已读取的总帧数
        /// </summary>
        public int ReadFrameCount { get; private set; } = 0;

        /// <summary>
        /// 帧间隔，每FrameInterval帧处理一次
        /// </summary>
        public int FrameInterval { get; set; } = 5;

        /// <summary>
        /// 视频帧接收事件
        /// </summary>
        public event Action<Mat>? OnFrameReceived;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event Action<string>? OnError;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RtspStreamProcessor() {
            _capture = new VideoCapture();
        }

        /// <summary>
        /// 获取视频帧率
        /// </summary>
        public double Fps => _capture.Fps;

        /// <summary>
        /// 启动视频流处理
        /// </summary>
        /// <param name="sourceUrl">视频源URL或摄像头索引</param>
        public void StartStream(string sourceUrl) {
            _sourceUrl = sourceUrl;
            _cancellationTokenSource = new CancellationTokenSource();
            // 在新线程中处理视频流
            Task.Run(() => ProcessStream(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// 处理视频流
        /// </summary>
        /// <param name="token">取消令牌</param>
        private void ProcessStream(CancellationToken token) {
            bool isOpened = false;

            try {
                // 尝试解析为摄像头索引
                if (int.TryParse(_sourceUrl!, out int cameraIndex)) {
                    isOpened = _capture.Open(cameraIndex);
                } else {
                    // 打开URL流
                    isOpened = _capture.Open(_sourceUrl!);
                }
            } catch (Exception ex) {
                OnError?.Invoke($"打开视频源失败: {ex.Message}");
                return;
            }

            // 检查是否成功打开
            if (!isOpened) {
                OnError?.Invoke($"无法打开视频源: {_sourceUrl}");
                return;
            }

            // 启动异步视频处理任务
            Task.Run(async () => await ProcessVideoStreamAsync(token), token);
        }

        /// <summary>
        /// 异步处理视频流
        /// </summary>
        /// <param name="token">取消令牌</param>
        private async Task ProcessVideoStreamAsync(CancellationToken token) {
            // 创建帧对象
            //using var frame = new Mat();
            var DelayTime = (int)(1000 / _capture.Fps);
            // 循环读取视频帧，直到收到取消请求
            while (!token.IsCancellationRequested) {

                try {
                    //if (!_capture.IsOpened()) {
                    //    OnError?.Invoke($"无法打开视频源: {_sourceUrl}");
                    //    break;
                    //}
                    using var frame = _capture.RetrieveMat();
                    // 读取一帧 如果帧为空，跳过
                    if (/*!_capture.Read(frame) ||*/ frame.Empty()) continue;

                    // 增加已读取帧数计数
                    ReadFrameCount++;

                    // 按指定间隔处理帧
                    if (ReadFrameCount % FrameInterval == 0) {
                        // 克隆帧，避免后续处理影响原帧
                        using var clonedFrame = frame.Clone();
                        // 触发帧接收事件
                        await Task.Run(() => OnFrameReceived?.Invoke(clonedFrame), token);
                        //OnFrameReceived?.Invoke(frame);
                    }
                    //window.ShowImage(frame);
                    // 防止整数溢出
                    if (ReadFrameCount == int.MinValue) ReadFrameCount = 0;
                } finally {
                    //// 短暂延迟，避免CPU占用过高
                    // Cv2.WaitKey(1);
                    //await Task.Delay(DelayTime, token);
                }
            }

            StopStream();
        }

        /// <summary>
        /// 停止视频流处理
        /// </summary>
        public void StopStream() {
            // 取消视频处理任务
            _cancellationTokenSource?.Cancel();
            Thread.Sleep(100);
            //释放视频捕获资源
            _capture?.Release();
            // 重置读取计数
            ReadFrameCount = 0;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() {
            // 停止流处理
            StopStream();
            // 释放视频捕获对象
            _capture?.Dispose();
            // 释放令牌源
            _cancellationTokenSource?.Dispose();
            // 抑制垃圾回收器调用终结器
            GC.SuppressFinalize(this);
        }
    }
}