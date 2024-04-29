using System;

namespace ZeroLevel.ML.DNN.Detectors
{
    public static class DetectorFactory
    {
        public static IDetector Create(IObjectDetector model, float threshold, ImageToTensorConversionOptions imageConverterOptions)
        {
            return new Detector(model, threshold, imageConverterOptions);
        }

        public static ObjectDetectionModels GetDetectorModel(string depectorPath)
        {
            var detectorType = ObjectDetectionModels.YoloV7;
            if (depectorPath.Contains("nanodet", StringComparison.OrdinalIgnoreCase))
            {
                detectorType = ObjectDetectionModels.Nanodet;
            }
            else if (depectorPath.Contains("yolov8", StringComparison.OrdinalIgnoreCase))
            {
                detectorType = ObjectDetectionModels.YoloV8;
            }
            else if (depectorPath.Contains("yolov6", StringComparison.OrdinalIgnoreCase))
            {
                detectorType = ObjectDetectionModels.YoloV6;
            }
            else if (depectorPath.Contains("yolov5", StringComparison.OrdinalIgnoreCase))
            {
                detectorType = ObjectDetectionModels.YoloV5;
            }
            else if (depectorPath.Contains("mmyolo", StringComparison.OrdinalIgnoreCase))
            {
                detectorType = ObjectDetectionModels.MMYolo;
            }
            else if (depectorPath.Contains("damoyolo", StringComparison.OrdinalIgnoreCase))
            {
                detectorType = ObjectDetectionModels.DamoYolo;
            }
            else if (depectorPath.Contains("edgeyolo", StringComparison.OrdinalIgnoreCase))
            {
                detectorType = ObjectDetectionModels.EdgeYolo;
            }
            else if (depectorPath.Contains("fastestdet", StringComparison.OrdinalIgnoreCase))
            {
                detectorType = ObjectDetectionModels.FastestDet;
            }
            else if (depectorPath.Contains("damodet", StringComparison.OrdinalIgnoreCase))
            {
                detectorType = ObjectDetectionModels.DamoDet;
            }
            
            return detectorType;
        }

        public static IDetector Create(ObjectDetectionModels modelType, float threshold, string modelPath, ImageToTensorConversionOptions imageConverterOptions, int deviceId = 0)
        {
            IObjectDetector model;
            bool invertAxes = false;
            switch (modelType)
            {
                case ObjectDetectionModels.YoloV5: { model = new Yolov5Detector(modelPath, deviceId); break; }
                case ObjectDetectionModels.YoloV6: { model = new Yolov6Detector(modelPath, deviceId); break; }
                case ObjectDetectionModels.YoloV7: { model = new Yolov7Detector(modelPath, deviceId); break; }
                case ObjectDetectionModels.YoloV8: { model = new Yolov8Detector(modelPath, deviceId); break; }
                case ObjectDetectionModels.MMYolo: { model = new MMYoloDetector(modelPath, deviceId); break; }
                case ObjectDetectionModels.Nanodet: { model = new NanodetDetector(modelPath, deviceId); break; }
                case ObjectDetectionModels.DamoYolo: { model = new DamoYoloDetector(modelPath, deviceId); break; }
                case ObjectDetectionModels.EdgeYolo: { model = new EdgeYoloDetector(modelPath, deviceId); break; }
                case ObjectDetectionModels.DamoDet: { model = new DamodetDetector(modelPath, deviceId); break; }
                case ObjectDetectionModels.FastestDet: { model = new FastestDetDetector(modelPath, deviceId); invertAxes = modelPath.Contains("modified", StringComparison.OrdinalIgnoreCase) == false; break; }
                default:
                    throw new Exception($"Model type '{modelType}' not implemented yet");
            }
            return new Detector(model, threshold, imageConverterOptions, invertAxes);

        }
    }
}
