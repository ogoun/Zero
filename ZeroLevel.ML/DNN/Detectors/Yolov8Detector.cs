using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN.Detectors
{
    public class Yolov8Detector
        : SSDNN, IObjectDetector
    {
        public float BNorm(float x) => ImageConverter.StandartNormalizator(x);
        public float GNorm(float x) => ImageConverter.StandartNormalizator(x);
        public float RNorm(float x) => ImageConverter.StandartNormalizator(x);

        public Yolov8Detector(string modelPath, int deviceId = 0)
            : base(modelPath, deviceId)
        {
        }

        public List<YoloPrediction> Predict(FastTensorPool inputs, float threshold)
        {
            var result = new List<YoloPrediction>();
            var relative_koef_x = 1.0f / inputs.Width;
            var relative_koef_y = 1.0f / inputs.Height;
            Extract(new Dictionary<string, Tensor<float>> { { "images", inputs.Tensor } }, d =>
            {
                Tensor<float> output;
                if (d.ContainsKey("output0"))
                {
                    output = d["output0"];
                }
                else
                {
                    output = d.First().Value;
                }
                if (output != null)
                {
                    for (int tensorIndex = 0; tensorIndex < inputs.TensorSize; tensorIndex++)
                    {
                        var tensor = inputs.GetTensor(tensorIndex);
                        for (int box = 0; box < output.Dimensions[2]; box++)
                        {
                            var conf = output[tensorIndex, 4, box]; // уверенность в наличии любого объекта
                            if (conf > threshold)
                            {
                                // Перевод относительно входа модели в относительные координаты
                                var cx = output[tensorIndex, 1, box];
                                var cy = output[tensorIndex, 0, box];
                                var w = output[tensorIndex, 3, box];
                                var h = output[tensorIndex, 2, box];
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
            });
            NMS.Apply(result);
            return result;
        }
    }
}
