using Compunet.YoloSharp;
using Compunet.YoloSharp.Data;
using FormsApp.Core.Interfaces;
using Microsoft.ML.OnnxRuntime;
using OpenCvSharp;

namespace FormsApp.Core.Detectors {
    /// <summary>
    /// YOLO目标检测器
    /// 使用YoloSharp库实现目标检测
    /// </summary>
    public class YoloDetector : IObjectDetector {
        private YoloPredictor? _yoloPredictor;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="modelPath">模型文件路径</param>
        public YoloDetector(string modelPath) {
            if (string.IsNullOrEmpty(modelPath))
                throw new ArgumentNullException(nameof(modelPath), "模型路径不能为空");

            if (!File.Exists(modelPath))
                throw new FileNotFoundException("模型文件未找到", modelPath);
            var session = new SessionOptions();
            session.AppendExecutionProvider_OpenVINO("AUTO:GPU,CPU");
            _yoloPredictor = new YoloPredictor(modelPath, new YoloPredictorOptions() {
                UseCuda = false,
                SessionOptions = session
            });
        }


        /// <summary>
        /// 检测图像中的目标
        /// </summary>
        /// <param name="frame">输入图像帧</param>
        /// <returns>检测结果</returns>
        public YoloResult<Detection> Detect(Mat frame) {
            if (_yoloPredictor == null)
                throw new ObjectDisposedException(nameof(YoloDetector), "检测器已被释放");

            //if (frame == null || frame.Empty())
            //    throw new ArgumentException("输入帧不能为空", nameof(frame));

            try {
                // 直接返回检测结果，不做类型转换
                return _yoloPredictor.Detect(frame.ToBytes());
            } catch (Exception ex) {
                throw new InvalidOperationException($"目标检测失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose() {
            _yoloPredictor?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
