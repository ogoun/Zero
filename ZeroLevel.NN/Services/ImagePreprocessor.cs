using Aurigma.GraphicsMill;
using Microsoft.ML.OnnxRuntime.Tensors;
using ZeroLevel.NN.Models;

namespace ZeroLevel.NN
{
    public static class ImagePreprocessor
    {
        private static Action<Tensor<float>, float, int, int, int, int> _precompiledChannelFirstAction = new Action<Tensor<float>, float, int, int, int, int>((t, v, ind, c, i, j) => { t[ind, c, i, j] = v; });
        private static Action<Tensor<float>, float, int, int, int, int> _precompiledChannelLastAction = new Action<Tensor<float>, float, int, int, int, int>((t, v, ind, c, i, j) => { t[ind, i, j, c] = v; });
        private static Func<byte, int, float> PixelToTensorMethod(ImagePreprocessorOptions options)
        {
            if (options.Normalize)
            {
                if (options.Correction)
                {
                    if (options.CorrectionFunc == null)
                    {
                        return new Func<byte, int, float>((b, i) => ((options.NormalizationMultiplier * (float)b) - options.Mean[i]) / options.Std[i]);
                    }
                    else
                    {
                        return new Func<byte, int, float>((b, i) => options.CorrectionFunc.Invoke(i, options.NormalizationMultiplier * (float)b));
                    }
                }
                else
                {
                    return new Func<byte, int, float>((b, i) => options.NormalizationMultiplier * (float)b);
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

        //private static int CalculateFragmentsCount(Image image, ImagePreprocessorOptions options)
        //{
        //    int count = (options.Crop.SaveOriginal ? 1 : 0);
        //    var Sw = image.Width;           // ширина оригинала
        //    var Sh = image.Height;          // высота оригинала

        //    var CRw = options.Crop.Width;   // ширина кропа
        //    var CRh = options.Crop.Height;  // высота кропа

        //    var Dx = options.Crop.Overlap ? (int)(options.Crop.OverlapKoefWidth * CRw) : CRw;   // сдвиг по OX к следующему кропу
        //    var Dy = options.Crop.Overlap ? (int)(options.Crop.OverlapKoefHeight * CRh) : CRh;  // сдвиг по OY к следующему кропу

        //    for (int x = 0; x < Sw; x += Dx)
        //    {
        //        for (int y = 0; y < Sh; y += Dy)
        //        {
        //            count++;
        //        }
        //    }
        //    return count;
        //}
        private static int CalculateFragmentsCount(Image image, ImagePreprocessorOptions options)
        {
            int count = (options.Crop.SaveOriginal ? 1 : 0);
            var Sw = image.Width;           // ширина оригинала
            var Sh = image.Height;          // высота оригинала

            var CRw = options.InputWidth;   // ширина кропа (равна ширине входа, т.к. изображение отресайзено подобающим образом)
            var CRh = options.InputHeight;  // высота кропа (равна высоте входа, т.к. изображение отресайзено подобающим образом)

            var Dx = options.Crop.Overlap ? (int)(options.Crop.OverlapKoefWidth * CRw) : CRw;   // сдвиг по OX к следующему кропу
            var Dy = options.Crop.Overlap ? (int)(options.Crop.OverlapKoefHeight * CRh) : CRh;  // сдвиг по OY к следующему кропу

            for (int x = 0; x < Sw; x += Dx)
            {
                for (int y = 0; y < Sh; y += Dy)
                {
                    count++;
                }
            }
            return count;
        }
        private static void FillTensor(Tensor<float> tensor, Image image, int index, ImagePreprocessorOptions options, Func<byte, int, float> pixToTensor)
        {
            var append = options.ChannelType == PredictorChannelType.ChannelFirst ? _precompiledChannelFirstAction : _precompiledChannelLastAction;

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

        private static void FillTensor(Tensor<float> tensor, Image image, int startX, int startY, int w, int h, int index, ImagePreprocessorOptions options, Func<byte, int, float> pixToTensor)
        {
            var append = options.ChannelType == PredictorChannelType.ChannelFirst ? _precompiledChannelFirstAction : _precompiledChannelLastAction;

            if (image.PixelType.BitsPerPixel != 24)
            {
                var i = image;
                image = i.CloneAs<Rgb24>();
                i.Dispose();
            }

            ((Image<Rgb24>)image).ProcessPixelRows(pixels =>
            {
                if (options.InvertXY)
                {
                    for (int y = startY; y < h; y++)
                    {
                        Span<Rgb24> pixelSpan = pixels.GetRowSpan(y);
                        for (int x = startX; x < w; x++)
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
                    for (int y = startY; y < h; y++)
                    {
                        Span<Rgb24> pixelSpan = pixels.GetRowSpan(y);
                        for (int x = startX; x < w; x++)
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


        public static ImagePredictionInput[] ToTensors(this Image image, ImagePreprocessorOptions options)
        {
            ImagePredictionInput[] result = null;
            var pixToTensor = PixelToTensorMethod(options);
            options.Channels = image.PixelType.BitsPerPixel >> 3;
            if (options.Crop.Enabled)
            {
                // Размеры оригинального изображения
                var Sw = image.Width;
                var Sh = image.Height;

                // Создание ресайза для целочисленного прохода кропами шириной CRw и высотой CRh
                var resizedForCropWidthKoef = options.InputWidth / (double)options.Crop.Width;
                var resizedForCropHeightKoef = options.InputHeight / (double)options.Crop.Height;

                // Размеры для ресайза изображения к размеру по которому удобно идти кропами
                var resizedForCropWidth = (int)Math.Round(Sw * resizedForCropWidthKoef, MidpointRounding.ToEven);
                var resizedForCropHeight = (int)Math.Round(Sh * resizedForCropHeightKoef, MidpointRounding.ToEven);

                // Размеры кропа, равны входу сети, а не (options.Crop.Width, options.Crop.Height), т.к. для оптимизации изображение будет предварительно отресайзено
                var CRw = options.InputWidth;
                var CRh = options.InputHeight;

                // Расчет сдвигов между кропами
                var Dx = options.Crop.Overlap ? (int)(options.Crop.OverlapKoefWidth * CRw) : CRw;
                var Dy = options.Crop.Overlap ? (int)(options.Crop.OverlapKoefHeight * CRh) : CRh;

                using (var source = image.Clone(img => img.Resize(resizedForCropWidth, resizedForCropHeight, KnownResamplers.Bicubic)))
                {
                    // Количество тензоров всего, во всех батчах суммарно
                    var count = CalculateFragmentsCount(source, options);

                    // Проверка, укладывается ли количество тензоров поровну в батчи
                    int offset = count % options.MaxBatchSize;

                    // Количество батчей
                    int count_tensor_batches = count / options.MaxBatchSize + (offset == 0 ? 0 : 1);

                    // Батчи
                    var tensors = new ImagePredictionInput[count_tensor_batches];

                    // Инициализация батчей
                    Parallel.For(0, count_tensor_batches, batch_index =>
                    {
                        if (batch_index < count_tensor_batches - 1)
                        {
                            tensors[batch_index] = new ImagePredictionInput
                            {
                                Tensor = InitInputTensor(options, options.MaxBatchSize),
                                Offsets = new OffsetBox[options.MaxBatchSize],
                                Count = options.MaxBatchSize
                            };
                        }
                        else
                        {
                            tensors[batch_index] = new ImagePredictionInput
                            {
                                Tensor = InitInputTensor(options, offset == 0 ? options.MaxBatchSize : offset),
                                Offsets = new OffsetBox[offset == 0 ? options.MaxBatchSize : offset],
                                Count = offset == 0 ? options.MaxBatchSize : offset
                            };
                        }
                    });

                    // Заполнение батчей
                    int tensor_index = 0;

                    // Если используется ресайз оригинала кроме кропов, пишется в первый батч в первый тензор
                    if (options.Crop.SaveOriginal)
                    {
                        using (var copy = source.Clone(img => img.Resize(options.InputWidth, options.InputHeight, KnownResamplers.Bicubic)))
                        {
                            FillTensor(tensors[0].Tensor, copy, 0, options, pixToTensor);
                            tensors[tensor_index].Offsets[0] = new OffsetBox(0, 0, image.Width, image.Height);
                        }
                        tensor_index++;
                    }
                    tensor_index--;
                    Parallel.ForEach(SteppedIterator(0, source.Width, Dx), x =>
                    {
                        // Можно запараллелить и тут, но выигрыш дает малоощутимый
                        for (int y = 0; y < source.Height; y += Dy)
                        {
                            var current_index = Interlocked.Increment(ref tensor_index);
                            // Индекс тензора внутри батча
                            var b_index = current_index % options.MaxBatchSize;
                            // Индекс батча
                            var p_index = (int)Math.Round((double)current_index / (double)options.MaxBatchSize, MidpointRounding.ToNegativeInfinity);
                            int w = CRw;
                            if ((x + CRw) > source.Width)
                            {
                                w = source.Width - x;
                            }
                            int h = CRh;
                            if ((y + CRh) > source.Height)
                            {
                                h = source.Height - y;
                            }
                            // Заполнение b_index тензора в p_index батче
                            FillTensor(tensors[p_index].Tensor, source, x, y, w, h, b_index, options, pixToTensor);
                            // Указание смещений для данного тензора
                            tensors[p_index].Offsets[b_index] = new OffsetBox(x, y, options.Crop.Width, options.Crop.Height);
                        }
                    });
                    return tensors;
                }
            }
            // if resize only
            result = new ImagePredictionInput[1];
            using (var copy = image.Clone(img => img.Resize(options.InputWidth, options.InputHeight, KnownResamplers.Bicubic)))
            {
                Tensor<float> tensor = InitInputTensor(options);
                FillTensor(tensor, copy, 0, options, pixToTensor);
                result[0] = new ImagePredictionInput
                {
                    Count = 1,
                    Offsets = null,
                    Tensor = tensor
                };
            }
            return result;
        }


        private static IEnumerable<int> SteppedIterator(int startIndex, int endIndex, int stepSize)
        {
            for (int i = startIndex; i < endIndex; i += stepSize)
            {
                yield return i;
            }
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
