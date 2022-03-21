using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ZeroLevel.NN.Models;

namespace ZeroLevel.NN
{
    public static class ImagePreprocessor
    {
        private const float NORMALIZATION_SCALE = 1f / 255f;

        private static Func<byte, int, float> PixelToTensorMethod(ImagePreprocessorOptions options)
        {
            if (options.Normalize)
            {
                if (options.Correction)
                {
                    if (options.CorrectionFunc == null)
                    {
                        return new Func<byte, int, float>((b, i) => ((NORMALIZATION_SCALE * (float)b) - options.Mean[i]) / options.Std[i]);
                    }
                    else
                    {
                        return new Func<byte, int, float>((b, i) => options.CorrectionFunc.Invoke(i, NORMALIZATION_SCALE * (float)b));
                    }
                }
                else
                {
                    return new Func<byte, int, float>((b, i) => NORMALIZATION_SCALE * (float)b);
                }
            }
            else if (options.Correction)
            {
                if (options.CorrectionFunc == null)
                {
                    return new Func<byte, int, float>((b, i) => (((float)b) - options.Mean[i]) / options.Std[i]);
                }
                else
                {
                    return new Func<byte, int, float>((b, i) => options.CorrectionFunc.Invoke(i, (float)b));
                }
            }
            return new Func<byte, int, float>((b, _) => (float)b);
        }

        private static int CalculateFragmentsCount(Image image, ImagePreprocessorOptions options)
        {
            int count = 0;
            var xs = options.Crop.Overlap ? (int)(options.Crop.Width * options.Crop.OverlapKoefWidth) : options.Crop.Width;
            var ys = options.Crop.Overlap ? (int)(options.Crop.Height * options.Crop.OverlapKoefHeight) : options.Crop.Height;
            for (var x = 0; x < image.Width - xs; x += xs)
            {
                for (var y = 0; y < image.Height - ys; y += ys)
                {
                    count++;
                }
            }
            return count;
        }
        private static void FillTensor(Tensor<float> tensor, Image image, int index, ImagePreprocessorOptions options, Func<byte, int, float> pixToTensor)
        {
            var append = options.ChannelType == PredictorChannelType.ChannelFirst
                ? new Action<Tensor<float>, float, int, int, int, int>((t, v, ind, c, i, j) => { t[ind, c, i, j] = v; })
                : new Action<Tensor<float>, float, int, int, int, int>((t, v, ind, c, i, j) => { t[ind, i, j, c] = v; });

            ((Image<Rgb24>)image).ProcessPixelRows(pixels =>
            {
                if (options.InvertXY)
                {
                    for (int y = 0; y < pixels.Height; y++)
                    {
                        Span<Rgb24> pixelSpan = pixels.GetRowSpan(y);
                        for (int x = 0; x < pixels.Width; x++)
                        {
                            if (options.BGR)
                            {
                                append(tensor, pixToTensor(pixelSpan[x].B, 0), index, 0, y, x);
                                append(tensor, pixToTensor(pixelSpan[x].G, 1), index, 1, y, x);
                                append(tensor, pixToTensor(pixelSpan[x].R, 2), index, 2, y, x);
                            }
                            else
                            {
                                append(tensor, pixToTensor(pixelSpan[x].R, 0), index, 0, y, x);
                                append(tensor, pixToTensor(pixelSpan[x].G, 1), index, 1, y, x);
                                append(tensor, pixToTensor(pixelSpan[x].B, 2), index, 2, y, x);
                            }
                        }
                    }
                }
                else
                {
                    for (int y = 0; y < pixels.Height; y++)
                    {
                        Span<Rgb24> pixelSpan = pixels.GetRowSpan(y);
                        for (int x = 0; x < pixels.Width; x++)
                        {
                            if (options.BGR)
                            {
                                append(tensor, pixToTensor(pixelSpan[x].B, 0), index, 0, x, y);
                                append(tensor, pixToTensor(pixelSpan[x].G, 1), index, 1, x, y);
                                append(tensor, pixToTensor(pixelSpan[x].R, 2), index, 2, x, y);
                            }
                            else
                            {
                                append(tensor, pixToTensor(pixelSpan[x].R, 0), index, 0, x, y);
                                append(tensor, pixToTensor(pixelSpan[x].G, 1), index, 1, x, y);
                                append(tensor, pixToTensor(pixelSpan[x].B, 2), index, 2, x, y);
                            }
                        }
                    }
                }
            });
        }

        private static Tensor<float> InitInputTensor(ImagePreprocessorOptions options, int batchSize = 1)
        {
            switch (options.ChannelType)
            {
                case PredictorChannelType.ChannelFirst:
                    return options.InvertXY
                        ? new DenseTensor<float>(new[] { batchSize, options.Channels, options.InputHeight, options.InputWidth })
                        : new DenseTensor<float>(new[] { batchSize, options.Channels, options.InputWidth, options.InputHeight });
                default:
                    return options.InvertXY
                        ? new DenseTensor<float>(new[] { batchSize, options.InputHeight, options.InputWidth, options.Channels })
                        : new DenseTensor<float>(new[] { batchSize, options.InputWidth, options.InputHeight, options.Channels });
            }
        }

        public static PredictionInput[] ToTensors(this Image image, ImagePreprocessorOptions options)
        {
            PredictionInput[] result = null;
            var pixToTensor = PixelToTensorMethod(options);
            options.Channels = image.PixelType.BitsPerPixel >> 3;

            if (options.Crop.Enabled)
            {
                var fragments = CalculateFragmentsCount(image, options);
                int count = CalculateFragmentsCount(image, options) + (options.Crop.SaveOriginal ? 1 : 0);
                int offset = count % options.MaxBatchSize;
                int count_tensors = count / options.MaxBatchSize + (offset == 0 ? 0 : 1);
                var tensors = new PredictionInput[count_tensors];
                for (int i = 0; i < count_tensors; i++)
                {
                    if (i < count_tensors - 1)
                    {
                        tensors[i] = new PredictionInput
                        {
                            Tensor = InitInputTensor(options, options.MaxBatchSize),
                            Offsets = new OffsetBox[options.MaxBatchSize],
                            Count = options.MaxBatchSize
                        };
                    }
                    else
                    {
                        tensors[i] = new PredictionInput
                        {
                            Tensor = InitInputTensor(options, offset == 0 ? options.MaxBatchSize : offset),
                            Offsets = new OffsetBox[offset == 0 ? options.MaxBatchSize : offset],
                            Count = offset == 0 ? options.MaxBatchSize : offset
                        };
                    }
                }

                int tensor_index = 0;
                int tensor_part_index = 0;
                var xs = options.Crop.Overlap ? (int)(options.Crop.Width * options.Crop.OverlapKoefWidth) : options.Crop.Width;
                var ys = options.Crop.Overlap ? (int)(options.Crop.Height * options.Crop.OverlapKoefHeight) : options.Crop.Height;

                if (options.Crop.SaveOriginal)
                {
                    using (var copy = image.Clone(img => img.Resize(options.InputWidth, options.InputHeight, KnownResamplers.Bicubic)))
                    {
                        FillTensor(tensors[tensor_index].Tensor, copy, tensor_part_index, options, pixToTensor);
                        tensors[tensor_index].Offsets[tensor_part_index] = new OffsetBox(0, 0, image.Width, image.Height);
                    }
                    tensor_part_index++;
                }
                for (var x = 0; x < image.Width - xs; x += xs)
                {
                    var startx = x;
                    var dx = (x + options.Crop.Width) - image.Width;
                    if (dx > 0)
                    {
                        startx -= dx;
                    }
                    for (var y = 0; y < image.Height - ys; y += ys)
                    {
                        if (tensor_part_index > 0 && tensor_part_index % options.MaxBatchSize == 0)
                        {
                            tensor_index++;
                            tensor_part_index = 0;
                        }
                        var starty = y;
                        var dy = (y + options.Crop.Height) - image.Height;
                        if (dy > 0)
                        {
                            starty -= dy;
                        }
                        using (var copy = image
                            .Clone(img => img
                                .Crop(new Rectangle(startx, starty, options.Crop.Width, options.Crop.Height))
                                .Resize(options.InputWidth, options.InputHeight, KnownResamplers.Bicubic)))
                        {
                            FillTensor(tensors[tensor_index].Tensor, copy, tensor_part_index, options, pixToTensor);
                            tensors[tensor_index].Offsets[tensor_part_index] = new OffsetBox(startx, starty, options.Crop.Width, options.Crop.Height);
                        }
                        tensor_part_index++;
                    }
                }
                return tensors;
            }

            // if resize only
            result = new PredictionInput[1];
            using (var copy = image.Clone(img => img.Resize(options.InputWidth, options.InputHeight, KnownResamplers.Bicubic)))
            {
                Tensor<float> tensor = InitInputTensor(options);
                FillTensor(tensor, copy, 0, options, pixToTensor);
                result[0] = new PredictionInput { Count = 1, Offsets = null, Tensor = tensor };
            }
            return result;
        }

        public static Image Crop(Image source, float x1, float y1, float x2, float y2)
        {
            int left = 0;
            int right = 0;
            int top = 0;
            int bottom = 0;

            int width = (int)(x2 - x1);
            int height = (int)(y2 - y1);

            if (x1 < 0) { left = (int)-x1; x1 = 0; }
            if (x2 > source.Width) { right = (int)(x2 - source.Width); x2 = source.Width - 1; }
            if (y1 < 0) { top = (int)-y1; y1 = 0; }
            if (y2 > source.Height) { bottom = (int)(y2 - source.Height); y2 = source.Height - 1; }

            if (left + right + top + bottom > 0)
            {
                var backgroundImage = new Image<Rgb24>(SixLabors.ImageSharp.Configuration.Default, width, height, new Rgb24(0, 0, 0));
                using (var crop = source.Clone(img => img.Crop(new Rectangle((int)x1, (int)y1, (int)(x2 - x1), (int)(y2 - y1)))))
                {
                    backgroundImage.Mutate(bg => bg.DrawImage(crop, new Point(left, top), 1f));
                }
                return backgroundImage;
            }
            return source.Clone(img => img.Crop(new Rectangle((int)x1, (int)y1, (int)(x2 - x1), (int)(y2 - y1))));
        }
    }
}
