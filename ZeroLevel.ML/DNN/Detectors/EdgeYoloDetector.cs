using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Generic;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN.Detectors
{
    public class EdgeYoloDetector
        : SSDNN, IObjectDetector
    {
        public EdgeYoloDetector(string modelPath, int deviceId) 
            : base(modelPath, deviceId)
        {
        }

        public float BNorm(float x) => ImageConverter.StandartNormalizator(x);
        public float GNorm(float x) => ImageConverter.StandartNormalizator(x);
        public float RNorm(float x) => ImageConverter.StandartNormalizator(x);

        public List<YoloPrediction> Predict(FastTensorPool inputs, float threshold)
        {
            var result = new List<YoloPrediction>();
            var relative_koef_x = 1.0f / inputs.Width;
            var relative_koef_y = 1.0f / inputs.Height;
            Extract(new Dictionary<string, Tensor<float>> { { "images", inputs.Tensor } }, d =>
            {
                Tensor<float> output = d["output"];
                if (output != null)
                {
                    for (int tensorIndex = 0; tensorIndex < inputs.TensorSize; tensorIndex++)
                    {
                        var tensor = inputs.GetTensor(tensorIndex);
                        for (int box = 0; box < output.Dimensions[1]; box++)
                        {
                            var conf = output[tensorIndex, box, 4]; // уверенность в наличии любого объекта
                            if (conf > threshold)
                            {
                                var class_score = output[tensorIndex, box, 5];
                                if (class_score > threshold)
                                {
                                    // Перевод относительно входа модели в относительные координаты
                                    var cx = output[tensorIndex, box, 1];
                                    var cy = output[tensorIndex, box, 0];
                                    var w = output[tensorIndex, box, 3];
                                    var h = output[tensorIndex, box, 2];
                                    // Перевод в координаты отнисительно текущего смещения
                                    cx += tensor.StartX;
                                    cy += tensor.StartY;
                                    result.Add(new YoloPrediction
                                    {
                                        Cx = cx * relative_koef_x,
                                        Cy = cy * relative_koef_y,
                                        W = w * relative_koef_x,
                                        H = h * relative_koef_y,
                                        Class = 0,
                                        Label = "0",
                                        Score = conf
                                    });
                                }
                            }
                        }
                    }
                }
            });
            NMS.Apply(result);
            return result;
        }
    }
}
