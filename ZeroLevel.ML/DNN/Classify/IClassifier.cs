using System;
using System.Collections.Generic;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN.Classify
{
    public interface IClassifier
        : IDisposable
    {
        float RNorm(float x);
        float GNorm(float x);
        float BNorm(float x);
        int InputSize { get; }
        List<float[]> Predict(FastTensorPool inputs);
        List<(int, float)> DetectClass(FastTensorPool inputs);
    }
}
