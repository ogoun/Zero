using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Generic;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN.Detectors
{
    public class DamoYoloDetector
        : SSDNN, IObjectDetector
    {
        public float BNorm(float x) => x;// ImageConverter.StandartNormalizator(x);
        public float GNorm(float x) => x;// ImageConverter.StandartNormalizator(x);
        public float RNorm(float x) => x;// ImageConverter.StandartNormalizator(x);

        public DamoYoloDetector(string modelPath, int deviceId = 0)
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
                Tensor<float> scores = d["scores"];
                Tensor<float> boxes = d["boxes"];

                for (int tensorIndex = 0; tensorIndex < inputs.TensorSize; tensorIndex++)
                {
                    var tensor = inputs.GetTensor(tensorIndex);
                    for (int box = 0; box < scores.Dimensions[1]; box++)
                    {
                        var conf = scores[tensorIndex, box, 0]; // уверенность в наличии любого объекта
                        if (conf > threshold)
                        {
                            // Перевод относительно входа модели в относительные координаты
                            var x1 = boxes[tensorIndex, box, 1];
                            var y1 = boxes[tensorIndex, box, 0];
                            var x2 = boxes[tensorIndex, box, 3];
                            var y2 = boxes[tensorIndex, box, 2];

                            var cx = (x1 + x2) / 2;
                            var cy = (y1 + y2) / 2;
                            var w = x2 - x1;
                            var h = y2 - y1;

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
            });
            NMS.Apply(result);
            return result;
        }
    }
}
