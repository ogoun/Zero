extern alias CoreDrawing;

using System;
using System.Runtime.CompilerServices;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.DNN
{
    public interface IImageConverter
    {
        FastTensorPool ImageToFastTensorsV2(string imagePath);
        FastTensorPool ImageToFastTensorsV2Inverted(string imagePath);
        FastTensorPool ImageToFastTensorsV2(CoreDrawing.System.Drawing.Bitmap image);
        FastTensorPool ImageToFastTensorsV2Inverted(CoreDrawing.System.Drawing.Bitmap image);
    }

    public sealed class ImageToTensorConversionOptions
    {
        /// <summary>
        /// Размер кропа который хранится в одном срезе тензора
        /// [batchSize, TensorCropSize, TensorCropSize, 3]
        /// или
        /// [batchSize, 3, TensorCropSize, TensorCropSize]
        /// </summary>
        public int TensorCropSize { get; set; } = 640;

        /// <summary>
        /// Множитель размера изображения в препроцессинге, например, мы хотим разрезать изображения на части размером 960*960
        /// которые затем отресайзить на вход сети к 640 на 640, в этом случае производительнее сначала сделать ресайз изображения
        /// на множитель side = side * (640 / 960) = side * 0.666, и затем разрезать сразу на части 640*640
        /// </summary>
        public float Multiplier { get; set; } = 1.0f;

        /// <summary>
        /// Размер батча для сетей с фиксированным размером, при -1 батч будет равен количеству кропов
        /// </summary>
        public int BatchSize { get; set; } = -1;

        /// <summary>
        /// Использовать BRG порядок пикселей вместо RGB
        /// </summary>
        public bool UseBRG { get; set; } = false;

        /// <summary>
        /// Кратность входного размера
        /// </summary>
        public int SizeMultiplicity { get; set; } = 64;
    }

    public sealed class ImageConverter
        : IImageConverter
    {

        static float standart_normalizator_koef = 1.0f / 255.0f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float StandartNormalizator(float x) => x * standart_normalizator_koef;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MeanStdNormilize(float x, float mean, float std) => (standart_normalizator_koef * x - mean) / std;


        private readonly ImageToTensorConversionOptions _options;

        private readonly bool _noPreresize;
        public ImageConverter(ImageToTensorConversionOptions options)
        {
            if (options.Multiplier <= float.Epsilon)
            {
                throw new ArgumentException("Multiplier must be positive number");
            }

            _options = options;
            _noPreresize = Math.Abs(1.0f - options.Multiplier) <= float.Epsilon;
        }

        private CoreDrawing.System.Drawing.Bitmap PrepareBitmap(string imagePath)
        {
            CoreDrawing.System.Drawing.Bitmap image;
            image = new CoreDrawing.System.Drawing.Bitmap(imagePath);
            return PrepareBitmap(image);
        }

        private CoreDrawing.System.Drawing.Bitmap PrepareBitmap(CoreDrawing.System.Drawing.Bitmap image)
        {
            int sourceWidth = image.Width, sourceHeight = image.Height;

            if (_noPreresize == false)
            {
                sourceWidth = (int)(sourceWidth * _options.Multiplier);
                sourceHeight = (int)(sourceHeight * _options.Multiplier);
            }
            if (_options.SizeMultiplicity > 1)
            {
                //Сделать размеры изображения кратным числу
                sourceWidth = ((int)Math.Ceiling((double)(sourceWidth / _options.SizeMultiplicity))) * _options.SizeMultiplicity;
                sourceHeight = ((int)Math.Ceiling((double)(sourceHeight / _options.SizeMultiplicity))) * _options.SizeMultiplicity;
            }
            if (sourceWidth != image.Width || sourceHeight != image.Height)
            {
                using (var buf = image)
                {
                    image = new CoreDrawing.System.Drawing.Bitmap(buf, new System.Drawing.Size(sourceWidth, sourceHeight));
                }
            }
            return image;
        }

        public FastTensorPool ImageToFastTensorsV2(string imagePath)
        {
            var image = PrepareBitmap(imagePath);
            return Convert(image, image.Width, image.Height, _options.TensorCropSize, _options.BatchSize, _options.UseBRG);
        }

        public FastTensorPool ImageToFastTensorsV2Inverted(string imagePath)
        {
            var image = PrepareBitmap(imagePath);
            return ConvertInverted(image, image.Width, image.Height, _options.TensorCropSize, _options.BatchSize, _options.UseBRG);
        }
        public FastTensorPool ImageToFastTensorsV2(CoreDrawing.System.Drawing.Bitmap image)
        {
            image = PrepareBitmap(image);
            return Convert(image, image.Width, image.Height, _options.TensorCropSize, _options.BatchSize, _options.UseBRG);
        }

        public FastTensorPool ImageToFastTensorsV2Inverted(CoreDrawing.System.Drawing.Bitmap image)
        {
            image = PrepareBitmap(image);
            return ConvertInverted(image, image.Width, image.Height, _options.TensorCropSize, _options.BatchSize, _options.UseBRG);
        }

        public static FastTensorPool Convert(CoreDrawing.System.Drawing.Bitmap image, int sourceWidth, int sourceHeight, int cropSize, int batchSize, bool bgr)
        {
            var pool = new FastTensorPool(sourceWidth, sourceHeight, image.Width, image.Height, cropSize);
            pool.BatchSize = batchSize;
            if (bgr)
            {
                pool.FillFromImageBGR(image);
            }
            else
            {
                pool.FillFromImage(image);
            }
            return pool;
        }

        public static FastTensorPool ConvertInverted(CoreDrawing.System.Drawing.Bitmap image, int sourceWidth, int sourceHeight, int cropSize, int batchSize, bool bgr)
        {
            var pool = new FastTensorPool(sourceWidth, sourceHeight, image.Width, image.Height, cropSize);
            pool.BatchSize = batchSize;
            if(bgr)
            {
                pool.FillFromImageInvertAxeBGR(image);
            }
            else
            {
                pool.FillFromImageInvertAxe(image);
            }
            return pool;
        }
    }
}
