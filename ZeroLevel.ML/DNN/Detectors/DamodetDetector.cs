using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN.Detectors
{
    /// <summary>
    /// DamoYolo and FastestDet models combination
    /// </summary>
    public class DamodetDetector
     : SSDNN, IObjectDetector
    {
        private const float SIZE = 640;

        public DamodetDetector(string modelPath, int deviceId)
            : base(modelPath, deviceId)
        {
        }

        public float RNorm(float x) => x;
        public float BNorm(float x) => x;
        public float GNorm(float x) => x;

        #region FastestDet
        private static double sigmoid(double x)
        {
            return 1d / (1d + Math.Exp(-x));
        }

        private static double tanh(double x)
        {
            return 2d / (1d + Math.Exp(-2d * x)) - 1d;
        }

        private void FastestDetPostprocess(FastTensorPool inputs, Tensor<float> output, List<YoloPrediction> result, float threshold)
        {
            var relative_koef_x = 1.0f / inputs.Width;
            var relative_koef_y = 1.0f / inputs.Height;
            var feature_map_height = output.Dimensions[2];
            var feature_map_width = output.Dimensions[3];
            for (int tensorIndex = 0; tensorIndex < inputs.TensorSize; tensorIndex++)
            {
                var tensor = inputs.GetTensor(tensorIndex);

                for (int h = 0; h < feature_map_height; h++)
                {
                    for (int w = 0; w < feature_map_width; w++)
                    {
                        var obj_score = output[tensorIndex, 0, h, w];
                        var cls_score = output[tensorIndex, 5, h, w];
                        var score = Math.Pow(obj_score, 0.6) * Math.Pow(cls_score, 0.4);
                        if (score > threshold)
                        {
                            var x_offset = tanh(output[tensorIndex, 1, h, w]);
                            var y_offset = tanh(output[tensorIndex, 2, h, w]);

                            var box_width = sigmoid(output[tensorIndex, 3, h, w]) * SIZE;
                            var box_height = sigmoid(output[tensorIndex, 4, h, w]) * SIZE;

                            var box_cx = ((w + x_offset) / feature_map_width) * SIZE + tensor.StartX;
                            var box_cy = ((h + y_offset) / feature_map_height) * SIZE + tensor.StartY;

                            result.Add(new YoloPrediction
                            {
                                Cx = (float)box_cx * relative_koef_x,
                                Cy = (float)box_cy * relative_koef_y,
                                W = (float)box_width * relative_koef_x,
                                H = (float)box_height * relative_koef_y,
                                Class = 0,
                                Score = (float)score
                            });
                        }
                    }
                }
            }
        }
        #endregion

        #region DamoYolo
        private void DamoYoloPostprocess(FastTensorPool inputs, Tensor<float> scores, Tensor<float> boxes, List<YoloPrediction> result, float threshold)
        {
            var relative_koef_x = 1.0f / inputs.Width;
            var relative_koef_y = 1.0f / inputs.Height;
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
        }
        #endregion

        private static float _fastest_threshold = 0.932f;
        public List<YoloPrediction> Predict(FastTensorPool inputs, float threshold)
        {
            var result = new List<YoloPrediction>();
            var relative_koef_x = 1.0f / inputs.Width;
            var relative_koef_y = 1.0f / inputs.Height;
            Extract(new Dictionary<string, Tensor<float>> { { "images", inputs.Tensor } }, d =>
            {
                Tensor<float> damo_scores = d["scores"];
                Tensor<float> damo_boxes = d["boxes"];
                Tensor<float> fastest_output = d["output"];

                DamoYoloPostprocess(inputs, damo_scores, damo_boxes, result, threshold);
                FastestDetPostprocess(inputs, fastest_output, result, _fastest_threshold);
            });
            NMS.Apply(result);
            return result;
        }
    }
}
