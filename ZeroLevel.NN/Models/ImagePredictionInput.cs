using Microsoft.ML.OnnxRuntime.Tensors;

namespace ZeroLevel.NN.Models
{
    public class ImagePredictionInput
    {
        public Tensor<float> Tensor;
        public OffsetBox[] Offsets;
        public int Count;
    }
}
