using Microsoft.ML.OnnxRuntime.Tensors;
using ZeroLevel.NN.Models;

namespace ZeroLevel.NN.Architectures.YoloV5
{
    public class Yolov5Detector
        : SSDNN
    {
        private int INPUT_WIDTH = 640;
        private int INPUT_HEIGHT = 640;
        private int CROP_WIDTH = 1280;
        private int CROP_HEIGHT = 1280;

        public Yolov5Detector(string modelPath, int inputWidth = 640, int inputHeight = 640, bool gpu = false)
            : base(modelPath, gpu)
        {
            INPUT_HEIGHT = inputHeight;
            INPUT_WIDTH = inputWidth;
        }

        public List<YoloPrediction> Predict(Image image, float threshold)
        {
            var input = MakeInput(image,
                new ImagePreprocessorOptions(INPUT_WIDTH, INPUT_HEIGHT, PredictorChannelType.ChannelFirst)
                .ApplyNormilization()
                .ApplyAxeInversion());
            return Predict(input, threshold);
        }

        public List<YoloPrediction> PredictMultiply(Image image, bool withFullResizedImage, float threshold)
        {
            var input = MakeInputBatch(image,
                new ImagePreprocessorOptions(INPUT_WIDTH, INPUT_HEIGHT, PredictorChannelType.ChannelFirst)
                .ApplyNormilization()
                .ApplyAxeInversion()
                .UseCrop(CROP_WIDTH, CROP_HEIGHT, withFullResizedImage, true));
            return PredictMultiply(input, threshold);
        }

        public List<YoloPrediction> Predict(Tensor<float> input, float threshold)
        {
            var result = new List<YoloPrediction>();
            Extract(new Dictionary<string, Tensor<float>> { { "images", input } }, d =>
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
                /*
                var output350 = d["350"];
                var output498 = d["498"];
                var output646 = d["646"];
                */
                if (output != null && output != null)
                {
                    var relative_koef_x = 1.0f / INPUT_WIDTH;
                    var relative_koef_y = 1.0f / INPUT_HEIGHT;
                    for (int box = 0; box < output.Dimensions[1]; box++)
                    {
                        var conf = output[0, box, 4]; // уверенность в наличии любого объекта
                        if (conf > threshold)
                        {
                            var class_confidense = output[0, box, 5]; // уверенность в наличии объекта класса person
                            if (class_confidense > threshold)
                            {
                                // Перевод относительно входа модели в относительные координаты
                                var cx = output[0, box, 0] * relative_koef_x;
                                var cy = output[0, box, 1] * relative_koef_y;
                                var h = output[0, box, 2] * relative_koef_y;
                                var w = output[0, box, 3] * relative_koef_x;
                                result.Add(new YoloPrediction
                                {
                                    Cx = cx,
                                    Cy = cy,
                                    W = w,
                                    H = h,
                                    Class = 0,
                                    Label = "0",
                                    Score = conf
                                });
                            }
                        }
                    }
                }
            });
            return result;
        }

        public List<YoloPrediction> PredictMultiply(ImagePredictionInput[] inputs, float threshold)
        {
            var result = new List<YoloPrediction>();
            var relative_koef_x = 1.0f / (float)INPUT_WIDTH;
            var relative_koef_y = 1.0f / (float)INPUT_HEIGHT;
            foreach (var input in inputs)
            {
                Extract(new Dictionary<string, Tensor<float>> { { "images", input.Tensor } }, d =>
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
                    
                    /*
                    var output350 = d["350"];
                    var output498 = d["498"];
                    var output646 = d["646"];
                    */
                    if (output != null && output != null)
                    {
                        for (int index = 0; index < input.Count; index++)
                        {
                            var real_koef_x = (float)input.Offsets[index].Width / (float)INPUT_WIDTH;
                            var real_koef_y = (float)input.Offsets[index].Height / (float)INPUT_HEIGHT;
                            for (int box = 0; box < output.Dimensions[1]; box++)
                            {
                                var conf = output[index, box, 4]; // уверенность в наличии любого объекта
                                if (conf > threshold)
                                {
                                    var class_confidense = output[index, box, 5]; // уверенность в наличии объекта класса person
                                    if (class_confidense > threshold)
                                    {
                                        // Перевод относительно входа модели в относительные координаты
                                        var cx = output[index, box, 0] * real_koef_x;
                                        var cy = output[index, box, 1] * real_koef_y;
                                        var h = output[index, box, 2] * relative_koef_y;
                                        var w = output[index, box, 3] * relative_koef_x;
                                        // Перевод в координаты отнисительно текущего смещения
                                        cx += input.Offsets[index].X;
                                        cy += input.Offsets[index].Y;
                                        result.Add(new YoloPrediction
                                        {
                                            Cx = cx,
                                            Cy = cy,
                                            W = w,
                                            H = h,
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
            }
            return result;
        }
    }
}
