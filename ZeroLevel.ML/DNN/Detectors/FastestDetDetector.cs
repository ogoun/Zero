using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN.Detectors
{
    public class FastestDetDetector
        : SSDNN, IObjectDetector
    {
        private const float SIZE = 640;

        public FastestDetDetector(string modelPath, int deviceId)
            : base(modelPath, deviceId)
        {
        }

        public float RNorm(float x) => ImageConverter.StandartNormalizator(x);
        public float BNorm(float x) => ImageConverter.StandartNormalizator(x);
        public float GNorm(float x) => ImageConverter.StandartNormalizator(x);

        private static double sigmoid(double x)
        {
            return 1d / (1d + Math.Exp(-x));
        }

        private static double tanh(double x)
        {
            return 2d / (1d + Math.Exp(-2d * x)) - 1d;
        }


        public List<YoloPrediction> Predict(FastTensorPool inputs, float threshold)
        {
            var result = new List<YoloPrediction>();
            var relative_koef_x = 1.0f / inputs.Width;
            var relative_koef_y = 1.0f / inputs.Height;
            Extract(new Dictionary<string, Tensor<float>> { { "images", inputs.Tensor } }, d =>
            {
                var output = d.First().Value;
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

            });
            NMS.Apply(result);
            return result;
        }


    }
}
