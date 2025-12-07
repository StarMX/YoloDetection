using OpenCvSharp;

namespace FormsApp.Core.Interfaces {
    /// <summary>
    /// 视频流处理器接口
    /// 定义视频流处理的基本操作
    /// </summary>
    public interface IVideoStreamProcessor : IDisposable {
        /// <summary>
        /// 已读取的总帧数
        /// </summary>
        int ReadFrameCount { get; }

        /// <summary>
        /// 帧间隔，每FrameCount帧处理一次
        /// </summary>
        int FrameInterval { get; set; }

        /// <summary>
        /// 视频帧率
        /// </summary>
        double Fps { get; }

        /// <summary>
        /// 视频帧接收事件
        /// </summary>
        event Action<Mat> OnFrameReceived;

        /// <summary>
        /// 错误事件
        /// </summary>
        event Action<string> OnError;

        /// <summary>
        /// 启动视频流处理
        /// </summary>
        /// <param name="sourceUrl">视频源URL或摄像头索引</param>
        void StartStream(string sourceUrl);

        /// <summary>
        /// 停止视频流处理
        /// </summary>
        void StopStream();
    }
}
