using Microsoft.ML.OnnxRuntime.Tensors;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN.Classify
{
    public class Yolov8Classifier
        : SSDNN, IClassifier
    {
        public int InputSize => 224;
        public float BNorm(float x) => ImageConverter.StandartNormalizator(x);
        public float GNorm(float x) => ImageConverter.StandartNormalizator(x);
        public float RNorm(float x) => ImageConverter.StandartNormalizator(x);

        public Yolov8Classifier(string modelPath, int deviceId = 0)
            : base(modelPath, deviceId)
        {
        }

        public List<float[]> Predict(FastTensorPool inputs)
        {
            var result = new List<float[]>();
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
                if (output != null && output != null)
                {
                    for (int tensorIndex = 0; tensorIndex < inputs.TensorSize; tensorIndex++)
                    {
                        var scores = new float[output.Dimensions[1]];
                        for (int objclass = 0; objclass < output.Dimensions[1]; objclass++)
                        {
                            scores[objclass] = output[tensorIndex, objclass];
                        }
                        result.Add(scores);
                    }
                }
            });
            return result;
        }

        public List<(int, float)> DetectClass(FastTensorPool inputs)
        {
            var classes = new List<(int, float)>();
            var scores = Predict(inputs);
            foreach (var score in scores)
            {
                if (score.Length > 0)
                {
                    int index = 0;
                    float max = score[0];
                    for (int i = 1; i < score.Length; i++)
                    {
                        if (score[i] > max)
                        {
                            max = score[i];
                            index = i;
                        }
                    }
                    classes.Add((index, max));
                }
                else
                {
                    classes.Add((-1, 0f));
                }
            }
            return classes;
        }
    }
}
