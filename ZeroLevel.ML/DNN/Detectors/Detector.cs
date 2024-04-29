extern alias CoreDrawing;

using System.Collections.Generic;
using System.IO;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN.Detectors
{
    internal sealed class Detector
        : IDetector
    {
        private readonly IImageConverter _imageConverter;
        private readonly IObjectDetector _model;
        private readonly float _threshold;
        private readonly bool _invertAxes = false;
        internal Detector(IObjectDetector model, 
            float threshold, 
            ImageToTensorConversionOptions imageConverterOptions, 
            bool invertAxes = false)
        {
            _imageConverter = new ImageConverter(imageConverterOptions);
            _model = model;
            _threshold = threshold;
            _invertAxes = invertAxes;
        }

        public FastTensorPool CreateInput(string filePath)
        {
            FastTensorPool input;
            if (_invertAxes)
                input = _imageConverter.ImageToFastTensorsV2Inverted(filePath);
            else
                input = _imageConverter.ImageToFastTensorsV2(filePath);
            input.Name = Path.GetFileNameWithoutExtension(filePath);
            input.Path = filePath;
            return input;
        }

        public FastTensorPool CreateInput(CoreDrawing.System.Drawing.Bitmap image, string filePath = null!)
        {
            var input = _imageConverter.ImageToFastTensorsV2(image);
            if (string.IsNullOrWhiteSpace(filePath) == false)
            {
                input.Name = Path.GetFileNameWithoutExtension(filePath);
                input.Path = filePath;
            }
            return input;
        }

        public List<YoloPrediction> Detect(string filePath)
        {
            var input = _imageConverter.ImageToFastTensorsV2(filePath);
            input.Name = Path.GetFileNameWithoutExtension(filePath);
            input.Path = filePath;
            return _model.Predict(input, _threshold);
        }

        public List<YoloPrediction> Detect(CoreDrawing.System.Drawing.Bitmap image, string filePath = null!)
        {
            var input = _imageConverter.ImageToFastTensorsV2(image);
            if (string.IsNullOrWhiteSpace(filePath) == false)
            {
                input.Name = Path.GetFileNameWithoutExtension(filePath);
                input.Path = filePath;
            }
            return _model.Predict(input, _threshold);
        }

        public List<YoloPrediction> Detect(FastTensorPool input) => _model.Predict(input, _threshold);

        public void Dispose()
        {
            _model.Dispose();
        }
    }
}
