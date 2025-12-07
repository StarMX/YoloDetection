using FormsApp.Core.Detectors;
using FormsApp.Core.Interfaces;
using FormsApp.Core.Streamers;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Diagnostics;

namespace FormsApp.Core {
    public class DetectionResult {
        //public int ClassId { get; set; }
        public required string Name { get; set; }
        public float Confidence { get; set; }
        public RectangleF Bounds { get; set; }

        public override string ToString() {
            return $"{Name}: {Confidence:P2} [{Bounds.X}, {Bounds.Y}, {Bounds.Width}, {Bounds.Height}]";
        }
    }



    /// <summary>
    /// 视频处理管理器
    /// 负责协调视频流处理和目标检测
    /// </summary>
    public class VideoProcessingManager : IDisposable {
        private readonly IVideoStreamProcessor _streamProcessor;
        private readonly IObjectDetector _objectDetector;
        private readonly ReaderWriterLockSlim _bitmapLock = new();
        private Bitmap? _currentBitmap;
        private bool _isRunning;

        /// <summary>
        /// 当前处理的位图
        /// </summary>
        public Bitmap? CurrentBitmap {
            get {
                _bitmapLock.EnterReadLock();
                try {
                    return _currentBitmap;
                } finally {
                    _bitmapLock.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 帧间隔，每FrameInterval帧处理一次
        /// </summary>
        public int FrameInterval {
            get => _streamProcessor.FrameInterval;
            set => _streamProcessor.FrameInterval = value;
        }

        /// <summary>
        /// 视频帧率
        /// </summary>
        public double Fps => _streamProcessor.Fps;

        /// <summary>
        /// 已读取的总帧数
        /// </summary>
        public int ReadCount => _streamProcessor.ReadFrameCount;

        /// <summary>
        /// 帧更新事件
        /// </summary>
        public event Action? OnFrameUpdated;

        /// <summary>
        /// 状态更新事件
        /// </summary>
        public event Action<string>? OnStatusUpdated;

        /// <summary>
        /// 错误事件
        /// </summary>
        public event Action<string>? OnError;

        /// <summary>
        /// 构造函数
        /// </summary>
        public VideoProcessingManager() {
            // 创建默认实现
            _streamProcessor = new RtspStreamProcessor();
            //_objectDetector = new YoloSharpDetector("models/yolo11n.onnx");
            _objectDetector = new YoloDotNetDetector(Path.Join("models", "yolo11n.onnx"));

            // 订阅事件
            _streamProcessor.OnFrameReceived += ProcessFrame;
            _streamProcessor.OnError += HandleStreamError;
        }

        /// <summary>
        /// 构造函数（依赖注入）
        /// </summary>
        /// <param name="streamProcessor">视频流处理器</param>
        /// <param name="objectDetector">目标检测器</param>
        public VideoProcessingManager(IVideoStreamProcessor streamProcessor, IObjectDetector objectDetector) {
            _streamProcessor = streamProcessor ?? throw new ArgumentNullException(nameof(streamProcessor));
            _objectDetector = objectDetector ?? throw new ArgumentNullException(nameof(objectDetector));

            // 订阅事件
            _streamProcessor.OnFrameReceived += ProcessFrame;
            _streamProcessor.OnError += HandleStreamError;
        }

        /// <summary>
        /// 开始视频处理
        /// </summary>
        /// <param name="sourceUrl">视频源URL或摄像头索引</param>
        public void Start(string sourceUrl) {
            if (_isRunning)
                return;

            _isRunning = true;
            _streamProcessor.StartStream(sourceUrl);
            OnStatusUpdated?.Invoke("视频处理已启动");
        }

        /// <summary>
        /// 停止视频处理
        /// </summary>
        public void Stop() {
            if (!_isRunning)
                return;

            _streamProcessor.StopStream();
            _isRunning = false;
            ClearCurrentBitmap();
            OnStatusUpdated?.Invoke("视频处理已停止");
        }

        /// <summary>
        /// 处理视频帧
        /// </summary>
        /// <param name="frame">视频帧</param>
        private void ProcessFrame(Mat frame) {
            try {
                Stopwatch stopwatch = Stopwatch.StartNew();
                // 进行目标检测
                var results = _objectDetector.Detect(frame)
                    //.Where(s =>s.Confidence > 0.75F)
                    .ToArray()
                    ;
                foreach (var result in results) {
                    var rect = new Rect((int)result.Bounds.X, (int)result.Bounds.Y, (int)result.Bounds.Width, (int)result.Bounds.Height);
                    Cv2.Rectangle(frame, rect, new Scalar(0, 255, 0), 2);

                    Cv2.PutText(frame,
                        $"{result.Name} ({result.Confidence:P1})",
                        new OpenCvSharp.Point(rect.X, rect.Y - 5),
                        HersheyFonts.HersheySimplex, 0.5, new Scalar(0, 255, 0), 1);
                }

                // 显示FPS和处理帧信息
                Cv2.PutText(frame,
                    $"FPS:{_streamProcessor.Fps:F1} | Processed Frames:{_streamProcessor.ReadFrameCount / _streamProcessor.FrameInterval}",
                    new OpenCvSharp.Point(10, 20), HersheyFonts.HersheySimplex, 0.5, Scalar.MistyRose, 1);

                // 更新当前位图
                UpdateCurrentBitmap(frame);
                stopwatch.Stop();
                // 更新状态
                OnStatusUpdated?.Invoke($"FPS:{_streamProcessor.Fps:F1} | Processed Frames:{_streamProcessor.ReadFrameCount / _streamProcessor.FrameInterval} | Candidates In {stopwatch.ElapsedMilliseconds} ms");

                // 触发帧更新事件
                OnFrameUpdated?.Invoke();
            } catch (Exception ex) {
                OnError?.Invoke($"处理帧时出错: {ex.Message}");
            } finally {
                frame.Dispose();
            }

        }

        /// <summary>
        /// 更新当前位图
        /// </summary>
        /// <param name="frame">视频帧</param>
        private void UpdateCurrentBitmap(Mat frame) {
            _bitmapLock.EnterWriteLock();
            try {
                _currentBitmap?.Dispose();
                _currentBitmap = frame.ToBitmap();
            } finally {
                _bitmapLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 清除当前位图
        /// </summary>
        private void ClearCurrentBitmap() {
            _bitmapLock.EnterWriteLock();
            try {
                _currentBitmap?.Dispose();
                _currentBitmap = null;
            } finally {
                _bitmapLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 处理流错误
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        private void HandleStreamError(string errorMessage) {
            _isRunning = false;
            ClearCurrentBitmap();
            OnError?.Invoke(errorMessage);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() {
            try {
                Stop();
            } catch (Exception ex) {
                OnError?.Invoke($"停止视频处理时出错: {ex.Message}");
            } finally {
                try {
                    _streamProcessor.Dispose();
                } catch (Exception ex) {
                    OnError?.Invoke($"释放视频流处理器时出错: {ex.Message}");
                }

                try {
                    _objectDetector.Dispose();
                } catch (Exception ex) {
                    OnError?.Invoke($"释放目标检测器时出错: {ex.Message}");
                }

                ClearCurrentBitmap();
                _bitmapLock.Dispose();
            }
        }
    }
}