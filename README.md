## 🤔 项目介绍
ONNX Runtime (OpenVINO)+ YOLOv11 学习


## 🛠️ 技术栈

- **开发语言**：C# (.NET 8/10)
- **界面框架**：Windows Forms
- **视频处理**：OpenCVSharp + YoloSharp
- **深度学习**：ONNX Runtime (OpenVINO)+ YOLOv11 
- **模型**：yolo11n.onnx（轻量级，适合实时检测）

## 📦 项目结构

```
FaceDetection/          # 项目根目录
├── FormsApp/           # Windows Forms应用
│   ├── Core/           # 核心功能模块
│   │   ├── Detectors/  # 目标检测器（YOLO）
│   │   ├── Interfaces/ # 接口定义
│   │   └── Streamers/  # 视频流处理器
│   ├── models/         # 存放YOLO模型
│   ├── Main.cs         # 主界面
│   └── Program.cs      # 程序入口
└── FaceDetection.slnx  # 解决方案文件
```

## 📖 使用说明

### 1. 输入视频源

在界面上方的文本框中输入视频流地址，比如：
- 网络地址：`rtmp://live-mikudemo.cloudvdn.com/mikudemo/timestamps.m3u8`
- 本地视频地址：`rtsp://username:password@ip/Streaming/Channels/101`
- 摄像头地址：`0`
### 3. 调整参数

- **检测间隔**：默认值是1，数值越大检测频率越低，适合性能较弱的电脑

### 4. 开始检测

点击「打开」按钮开始检测，画面会实时显示在下方的预览区域，识别到的物体会用绿色框标注出来。

## 🎯 检测效果

- 支持识别80种常见物体（人脸、动物、交通工具等）
- 检测速度取决于电脑性能，一般在20-60 FPS
- 检测置信度可在代码中调整（默认显示所有检测结果）

## 🔧 自定义设置

### 更换模型

把新的YOLO ONNX模型文件放到 `models` 目录下，然后修改 `VideoProcessingManager.cs` 中的模型路径：

```csharp
_objectDetector = new YoloDetector("models/你的模型文件名.onnx");
```

### 调整检测阈值

在 `VideoProcessingManager.cs` 中的 `ProcessFrame` 方法里，取消注释并调整置信度阈值：

```csharp
//if (result.Confidence < 0.85F) continue;
```

## 📝 注意事项

1. 第一次运行可能会加载较慢，因为需要初始化ONNX Runtime
2. 视频流地址要确保可以正常访问
3. 模型文件（yolo11n.onnx）比较大，确保已经正确下载到models目录
4. 如果程序崩溃，可能是因为模型文件缺失或视频流不可访问


## 📄 许可证

本项目仅供学习和研究使用。

---