using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Generic;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN.Detectors
{
    public sealed class MMYoloDetector
       : SSDNN, IObjectDetector
    {
        public MMYoloDetector(string modelPath, int deviceId = 0)
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
                Tensor<float> boxes = d["boxes"];
                Tensor<float> scores = d["scores"];

                if (boxes != null && scores != null)
                {
                    for (int tensorIndex = 0; tensorIndex < inputs.TensorSize; tensorIndex++)
                    {
                        var tensor = inputs.GetTensor(tensorIndex);
                        for (int box = 0; box < scores.Dimensions[1]; box++)
                        {
                            var conf = scores[tensorIndex, box]; // уверенность в наличии любого объекта
                            if (conf > threshold)
                            {
                                // Перевод относительно входа модели в относительные координаты
                                var tlx = boxes[tensorIndex, box, 1];
                                var tly = boxes[tensorIndex, box, 0];
                                var brx = boxes[tensorIndex, box, 3];
                                var bry = boxes[tensorIndex, box, 2];

                                var cx = (tlx + brx) * 0.5f;
                                var cy = (tly + bry) * 0.5f;
                                var w = brx - tlx;
                                var h = bry - tly;

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
