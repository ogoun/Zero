using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using ZeroLevel.NN.Models;

namespace ZeroLevel.NN
{
    public class GenderAgeEstimator
        : SSDNN
    {
        private const int INPUT_WIDTH = 64;
        private const int INPUT_HEIGHT = 64;

        public GenderAgeEstimator(string modelPath, bool gpu = false) : base(modelPath, gpu)
        {
        }

        public (Gender, int) Predict(Image image)
        {
            var input = MakeInput(image,
                new ImagePreprocessorOptions(INPUT_WIDTH, INPUT_HEIGHT, PredictorChannelType.ChannelFirst)
                .ApplyNormilization()
                .ApplyAxeInversion());
            return Predict(input);
        }

        public (Gender, int) Predict(Tensor<float> input)
        {
            float[] variances = null;
            Extract(new Dictionary<string, Tensor<float>> { { "input", input } }, d =>
            {
                variances = d.First().Value.ToArray();
            });
            var gender = Argmax(variances[0..2]) == 0 ? Gender.Male : Gender.Feemale;
            return (gender, (int)variances[2]);
        }
    }
}
