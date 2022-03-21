using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;

namespace ZeroLevel.NN
{
    public interface IEncoder
    {
        int InputW { get; }
        int InputH { get; }

        float[] Predict(Image image);
        float[] Predict(Tensor<float> input);
    }
}
