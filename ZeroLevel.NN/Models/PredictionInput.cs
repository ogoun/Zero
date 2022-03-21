using Microsoft.ML.OnnxRuntime.Tensors;

namespace ZeroLevel.NN.Models
{
    public class PredictionInput
    {
        public Tensor<float> Tensor;
        public OffsetBox[] Offsets;
        public int Count;
    }
}
