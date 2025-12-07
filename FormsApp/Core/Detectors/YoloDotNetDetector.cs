using FormsApp.Core.Interfaces;
using OpenCvSharp;
using SkiaSharp;
using YoloDotNet;
using YoloDotNet.Core;
using YoloDotNet.Enums;
using YoloDotNet.Models;

namespace FormsApp.Core.Detectors {
    public class YoloDotNetDetector : IObjectDetector {

        private Yolo? _predictor;
        public YoloDotNetDetector(string modelPath) {
            if (string.IsNullOrEmpty(modelPath))
                throw new ArgumentNullException(nameof(modelPath), "模型路径不能为空");

            if (!File.Exists(modelPath))
                throw new FileNotFoundException("模型文件未找到", modelPath);


            _predictor = new Yolo(new YoloOptions {
                OnnxModel = modelPath,
                ExecutionProvider = new OpenVINOExecutionProvider(),
                //ExecutionProvider = new CudaExecutionProvider(GpuId: 0, PrimeGpu: true),
                // 推理前应用的缩放模式。Proportional（等比缩放）会保持宽高比（必要时加填充）；Stretch（拉伸缩放）则直接将图片resize到目标尺寸，不保持宽高比。该参数会直接影响推理结果。
                ImageResize = ImageResize.Proportional,

                // 缩放时可用的采样选项；会影响推理速度与质量。
                // 其他采样选项的对比示例，参见基准测试：
                SamplingOptions = new(SKFilterMode.Nearest, SKMipmapMode.None)
            });
        }

        public string Name => nameof(YoloDotNetDetector);

        public IEnumerable<DetectionResult> Detect(Mat frame) {
            if (_predictor == null)
                throw new ObjectDisposedException(nameof(YoloSharpDetector), "检测器已被释放");
            try {
                using var image = SKBitmap.Decode(frame.ToBytes());
                var results = _predictor.RunObjectDetection(image);
                //内存泄漏，没搞定直接强制回收吧
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return [.. results.Select(r => new DetectionResult {
                    Name = r.Label.Name,
                    Confidence = (float)r.Confidence,
                    Bounds =  new RectangleF(r.BoundingBox.Left, r.BoundingBox.Top, r.BoundingBox.Width, r.BoundingBox.Height)
                })];
            } catch {
                return Array.Empty<DetectionResult>();
            }

        }

        public void Dispose() {
            _predictor?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
