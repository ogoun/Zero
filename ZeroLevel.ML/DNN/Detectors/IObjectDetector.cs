using System;
using System.Collections.Generic;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN.Detectors
{
    public interface IObjectDetector
        : IDisposable
    {
        float RNorm(float x);
        float GNorm(float x);
        float BNorm(float x);
        List<YoloPrediction> Predict(FastTensorPool inputs, float threshold);
    }
}
