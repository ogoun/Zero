extern alias CoreDrawing;

using System;
using System.Collections.Generic;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN.Detectors
{
    public interface IDetector
        : IDisposable
    {
        FastTensorPool CreateInput(string filePath);
        FastTensorPool CreateInput(CoreDrawing.System.Drawing.Bitmap image, string filePath = null!);
        List<YoloPrediction> Detect(FastTensorPool input);
        List<YoloPrediction> Detect(string filePath);
        List<YoloPrediction> Detect(CoreDrawing.System.Drawing.Bitmap image, string filePath = null!);
    }
}
