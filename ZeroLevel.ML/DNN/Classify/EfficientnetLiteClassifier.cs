using Microsoft.ML.OnnxRuntime.Tensors;
using ZeroLevel.ML.DNN.Models;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ZeroLevel.ML.DNN.Classify
{
    public sealed class EfficientnetLiteClassifier
        : SSDNN, IClassifier
    {
        public int InputSize => 224;
        public float[] MEAN_RGB = new float[3] { 0.498f, 0.498f, 0.498f };
        public float[] STDDEV_RGB = new float[3] { 0.502f, 0.502f, 0.502f };

        public float RNorm(float x) => ImageConverter.MeanStdNormilize(x, MEAN_RGB[0], STDDEV_RGB[0]);
        public float GNorm(float x) => ImageConverter.MeanStdNormilize(x, MEAN_RGB[1], STDDEV_RGB[1]);
        public float BNorm(float x) => ImageConverter.MeanStdNormilize(x, MEAN_RGB[2], STDDEV_RGB[2]);

        public EfficientnetLiteClassifier(string modelPath, int deviceId = 0)
            : base(modelPath, deviceId)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe float[] Softmax(float[] input)
        {
            var sum = 0f;
            var dst = new float[input.Length];
            for (var i = 0; i < input.Length; ++i)
            {
                var e = (float)Math.Exp(input[i]);
                dst[i] = e;
                sum += e;
            }
            var sumInv = 1f / sum;
            for (var i = 0; i < input.Length; ++i)
                dst[i] *= sumInv;

            return dst;
        }

        public List<float[]> Predict(FastTensorPool inputs)
        {
            var result = new List<float[]>();
            Extract(new Dictionary<string, Tensor<float>> { { "input", inputs.Tensor } }, d =>
            {
                Tensor<float> output;
                if (d.ContainsKey("output"))
                {
                    output = d["output"];
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
                        var probs = Softmax(scores);
                        result.Add(probs);
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
            return classes.OrderByDescending(x => x.Item2).ToList();
        }
    }
}
