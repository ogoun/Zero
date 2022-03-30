using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZeroLevel.NN.Models;

namespace ZeroLevel.NN
{
    public abstract class SSDNN
        : IDisposable
    {
        private readonly InferenceSession _session;

        public SSDNN(string modelPath, bool gpu = false)
        {
            if (gpu)
            {
                try
                {
                    var so = SessionOptions.MakeSessionOptionWithCudaProvider(0);
                    so.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                    _session = new InferenceSession(modelPath, so);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Fault create InferenceSession with CUDA");
                    _session = new InferenceSession(modelPath);
                }
            }
            else
            {
                _session = new InferenceSession(modelPath);
            }
        }

        protected void Extract(IDictionary<string, Tensor<float>> input, Action<IDictionary<string, Tensor<float>>> inputHandler)
        {
            var container = new List<NamedOnnxValue>();
            foreach (var pair in input)
            {
                container.Add(NamedOnnxValue.CreateFromTensor<float>(pair.Key, pair.Value));
            }
            using (var output = _session.Run(container))
            {
                var result = new Dictionary<string, Tensor<float>>();
                foreach (var o in output)
                {
                    result.Add(o.Name, o.AsTensor<float>());
                }
                inputHandler.Invoke(result);
            }
        }

        /// <summary>
        /// Scale input vectors individually to unit norm (vector length).
        /// </summary>
        protected void Norm(float[] vector)
        {
            var totalSum = vector.Sum(v => v * v);
            var length = (float)Math.Sqrt(totalSum);
            var inverseLength = 1.0f / length;
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] *= inverseLength;
            }
        }
        protected PredictionInput[] MakeInputBatch(Image<Rgb24> image, ImagePreprocessorOptions options)
        {
            return ImagePreprocessor.ToTensors(image, options);
        }

        protected Tensor<float> MakeInput(Image image, ImagePreprocessorOptions options)
        {
            var input =  ImagePreprocessor.ToTensors(image, options);
            return input[0].Tensor;
        }

        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
