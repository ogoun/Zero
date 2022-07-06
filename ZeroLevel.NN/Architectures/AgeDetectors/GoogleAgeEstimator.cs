using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using ZeroLevel.NN.Models;

namespace ZeroLevel.NN
{
    public enum Age
    {
        From0To2,
        From4To6,
        From8To12,
        From15To20,
        From25To32,
        From38To43,
        From48To53,
        From60To100
    }

    /// <summary>
    /// Input tensor is 1 x 3 x height x width with mean values 104, 117, 123. Input image have to be previously resized to 224 x 224 pixels and converted to BGR format. 
    /// </summary>
    public class GoogleAgeEstimator
        : SSDNN
    {
        private const int INPUT_WIDTH = 224;
        private const int INPUT_HEIGHT = 224;
        private static float[] MEAN = new[] { 104f, 117f, 123f };

        private Age[] _ageList = new[] { Age.From0To2, Age.From4To6, Age.From8To12, Age.From15To20, Age.From25To32, Age.From38To43, Age.From48To53, Age.From60To100 };

        public GoogleAgeEstimator(string modelPath, bool gpu = false) : base(modelPath, gpu)
        {
        }

        public Age Predict(Image image)
        {
            var input = MakeInput(image,
                new ImagePreprocessorOptions(INPUT_WIDTH, INPUT_HEIGHT, PredictorChannelType.ChannelFirst)
                .ApplyCorrection((c, px) => px - MEAN[c])
                .ApplyAxeInversion());
            return Predict(input);
        }

        public Age Predict(Tensor<float> input)
        {
            float[] variances = null;
            Extract(new Dictionary<string, Tensor<float>> { { "input", input } }, d =>
            {
                variances = d.First().Value.ToArray();
            });
            var index = Argmax(variances);
            return _ageList[index];
        }
    }
}
