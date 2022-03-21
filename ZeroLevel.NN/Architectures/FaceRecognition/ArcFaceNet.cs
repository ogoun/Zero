using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using ZeroLevel.NN.Models;

/*
INPUT

Image, name: data, shape: 1, 3, 112, 112, format: B, C, H, W, where:
B - batch size
C - channel
H - height
W - width
Channel order is BGR.

OUTPUT

Face embeddings, name: fc1, shape: 1, 512, output data format: B, C, where:
B - batch size
C - row-vector of 512 floating points values, face embeddings

INPUT NORMALIZATION
img -= 127.5
img /= 128

OUTPUT NORMALIZATION
NORM - vector length = 1
 */

namespace ZeroLevel.NN
{
    public sealed class ArcFaceNet
        : SSDNN, IEncoder
    {
        private const int INPUT_WIDTH = 112;
        private const int INPUT_HEIGHT = 112;
        public ArcFaceNet(string modelPath)
            : base(modelPath)
        {
        }

        public int InputW => INPUT_WIDTH;

        public int InputH => INPUT_HEIGHT;

        public float[] Predict(Image image)
        {
            var input = MakeInput(image,
                new ImagePreprocessorOptions(INPUT_WIDTH, INPUT_HEIGHT, PredictorChannelType.ChannelFirst)
                .ApplyAxeInversion());
            return Predict(input);
        }

        public float[] Predict(Tensor<float> input)
        {
            float[] embedding = null;
            Extract(new Dictionary<string, Tensor<float>> { { "data", input } }, d =>
            {
                embedding = d.First().Value.ToArray();
            });
            Norm(embedding);
            return embedding;
        }
    }
}
