using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;

namespace ZeroLevel.ML.DNN
{
    public abstract class SSDNN
        : IDisposable
    {
        protected readonly InferenceSession _session;

        public SSDNN(string modelPath, int deviceId)
        {
            var so = SessionOptions.MakeSessionOptionWithCudaProvider(deviceId);
            so.RegisterOrtExtensions();
            so.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_FATAL;
            so.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            so.ExecutionMode = ExecutionMode.ORT_PARALLEL;
            _session = new InferenceSession(modelPath, so);
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
        public void Dispose()
        {
            _session?.Dispose();
        }
    }
}
