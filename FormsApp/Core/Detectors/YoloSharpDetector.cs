using Compunet.YoloSharp;
using FormsApp.Core.Interfaces;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;

namespace FormsApp.Core.Detectors {
    /// <summary>
    /// YOLO目标检测器
    /// 使用YoloSharp库实现目标检测
    /// </summary>
    public class YoloSharpDetector : IObjectDetector {
        private YoloPredictor? _predictor;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="modelPath">模型文件路径</param>
        public YoloSharpDetector(string modelPath) {
            if (string.IsNullOrEmpty(modelPath))
                throw new ArgumentNullException(nameof(modelPath), "模型路径不能为空");

            if (!File.Exists(modelPath))
                throw new FileNotFoundException("模型文件未找到", modelPath);
            var session = new SessionOptions();
            session.AppendExecutionProvider_OpenVINO("AUTO:GPU,CPU");
            _predictor = new YoloPredictor(modelPath, new YoloPredictorOptions() {
                UseCuda = false,
                SessionOptions = session
            });
        }
        public string Name => nameof(YoloSharpDetector);

        /// <summary>
        /// 检测图像中的目标
        /// </summary>
        /// <param name="frame">输入图像帧</param>
        /// <returns>检测结果</returns>
        public IEnumerable<DetectionResult> Detect(Mat frame) {
            if (_predictor == null)
                throw new ObjectDisposedException(nameof(YoloSharpDetector), "检测器已被释放");

            // 直接返回检测结果，不做类型转换
            //return _predictor.Detect(frame.ToBytes());
            try {
                var results = _predictor.Detect(frame.ToBytes());
                //内存泄漏，没搞定直接强制回收吧
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return [.. results.Select(e => new DetectionResult {
                    Name = e.Name.ToString(),
                    Confidence = e.Confidence,
                    Bounds = new RectangleF(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height)
                })];
            } catch {
                return Array.Empty<DetectionResult>();
            }

        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() {
            _predictor?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
