using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.NN.Architectures.YoloV5;
using ZeroLevel.NN.Models;
using ZeroLevel.NN.Services;

namespace TestApp
{
    public class PersonDetector
    {
        private const string MODEL_PATH = @"nnmodels/Yolo5S/yolov5s327e.onnx";
        private readonly Yolov5Detector _detector;
        private float _threshold = 0.17f;
        public PersonDetector()
        {
            _detector = new Yolov5Detector(MODEL_PATH, gpu: false);
        }

        public IEnumerable<YoloPrediction> Detect(string imagePath)
        {
            using (Image<Rgb24> image = Image.Load<Rgb24>(imagePath))
            {
                var t_predictions = _detector.PredictMultiply(image, true, _threshold);
                t_predictions.Apply(p =>
                {
                    p.Cx /= image.Width;
                    p.Cy /= image.Height;
                });
                if (t_predictions != null)
                {
                    t_predictions.RemoveAll(p => (p.W * image.Width) < 10.0f || (p.H * image.Height) < 10.0f);
                }
                if (t_predictions.Count > 0)
                {
                    NMS.Apply(t_predictions);
                    return t_predictions;
                }
            }
            return Enumerable.Empty<YoloPrediction>();
        }
    }
}
