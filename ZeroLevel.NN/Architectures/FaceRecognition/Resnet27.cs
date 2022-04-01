using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using ZeroLevel.NN.Models;

namespace ZeroLevel.NN
{
    public class Resnet27
        : SSDNN, IEncoder
    {
        private const int INPUT_WIDTH = 128;
        private const int INPUT_HEIGHT = 128;
        public Resnet27(string modelPath)
            : base(modelPath)
        {
        }

        public int InputW => INPUT_WIDTH;
        public int InputH => INPUT_HEIGHT;

        public float[] Predict(Image image)
        {
            var input = MakeInput(image,
                new ImagePreprocessorOptions(INPUT_WIDTH, INPUT_HEIGHT, PredictorChannelType.ChannelFirst)
                .ApplyCorrection((c, px) => (px - 127.5f) / 128f)
                .ApplyAxeInversion());
            return Predict(input);
        }

        public float[] Predict(Tensor<float> input)
        {
            float[] embedding = null;
            Extract(new Dictionary<string, Tensor<float>> { { "input.1", input } }, d =>
            {
                embedding = d.First().Value.ToArray();
            });
            Norm(embedding);
            return embedding;
        }
    }
}
