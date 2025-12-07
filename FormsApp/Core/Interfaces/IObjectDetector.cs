using Compunet.YoloSharp;
using Compunet.YoloSharp.Data;
using OpenCvSharp;

namespace FormsApp.Core.Interfaces {
    /// <summary>
    /// 目标检测器接口
    /// 定义目标检测的基本操作
    /// </summary>
    public interface IObjectDetector : IDisposable {
        /// <summary>
        /// 检测图像中的目标
        /// </summary>
        /// <param name="frame">输入图像帧</param>
        /// <returns>检测结果</returns>
        YoloResult<Detection> Detect(Mat frame);

    }
}
