extern alias CoreDrawing;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeroLevel.ML.Services;

namespace ZeroLevel.ML.DNN.Models
{
    public sealed class FastTensorPool
        : IDisposable
    {
        public Tensor<float> Tensor;
        public string Name = null!;
        public string Path = null!;
        public int CropSize;
        public int Width;
        public int Height;
        public int SourceWidth;
        public int SourceHeight;
        public int BatchSize = -1; // -1 dynamic
        public int TensorSize { get; private set; }

        private Dictionary<int, TensorPoolItem> _index;

        public FastTensorPool()
        {
        }

        public FastTensorPool(int sourceWidth, int sourceHeight, int fullWidth, int fullHeight, int cropSize)
        {
            SourceWidth = sourceWidth;
            SourceHeight = sourceHeight;
            Width = fullWidth;
            Height = fullHeight;
            CropSize = cropSize;
        }

        public TensorPoolItem GetTensor(int tensorIndex) => _index[tensorIndex];

        public void FillFromImage(CoreDrawing.System.Drawing.Bitmap image)
        {
            using (var scanner = new ImageScanner(image, CropSize))
            {
                _index = new Dictionary<int, TensorPoolItem>(scanner.TotalRegions);
                TensorSize = scanner.TotalRegions;
                var diff = BatchSize - scanner.TotalRegions;
                var tensorSize = BatchSize == -1 ? scanner.TotalRegions : BatchSize;
                Tensor = new DenseTensor<float>(new[] { tensorSize, 3, scanner.CropSizeX, scanner.CropSizeY });
                var tasks = new Task[scanner.TotalRegions];
                foreach (var regionReader in scanner.ScanByRegions())
                {
                    var tensor = new TensorPoolItem(Tensor, regionReader.TensorIndex, regionReader.X, regionReader.Y, scanner.CropSizeX, scanner.CropSizeY);
                    _index[regionReader.TensorIndex] = tensor;
                    tasks[regionReader.TensorIndex] = Task.Factory.StartNew((_reader) =>
                    {
                        var reader = (ImageRegionReader)_reader;
                        reader.FillTensor(_index[reader.TensorIndex]);
                    }, regionReader);
                }
                Task.WaitAll(tasks);
            }
        }

        public void FillFromImageInvertAxe(CoreDrawing.System.Drawing.Bitmap image)
        {
            using (var scanner = new ImageScanner(image, CropSize))
            {
                _index = new Dictionary<int, TensorPoolItem>(scanner.TotalRegions);
                TensorSize = scanner.TotalRegions;
                var diff = BatchSize - scanner.TotalRegions;
                var tensorSize = BatchSize == -1 ? scanner.TotalRegions : BatchSize;
                Tensor = new DenseTensor<float>(new[] { tensorSize, 3, scanner.CropSizeX, scanner.CropSizeY });
                var tasks = new Task[scanner.TotalRegions];
                foreach (var regionReader in scanner.ScanByRegions())
                {
                    var tensor = new TensorPoolItem(Tensor, regionReader.TensorIndex, regionReader.X, regionReader.Y, scanner.CropSizeX, scanner.CropSizeY);
                    _index[regionReader.TensorIndex] = tensor;
                    tasks[regionReader.TensorIndex] = Task.Factory.StartNew((_reader) =>
                    {
                        var reader = (ImageRegionReader)_reader;
                        reader.Read((x, y, r, g, b) =>
                        {
                            _index[reader.TensorIndex].FastSet(y, x, r, g, b);
                        });
                    }, regionReader);
                }
                Task.WaitAll(tasks);
            }
        }

        public void FillFromImageBGR(CoreDrawing.System.Drawing.Bitmap image)
        {
            using (var scanner = new ImageScanner(image, CropSize))
            {
                _index = new Dictionary<int, TensorPoolItem>(scanner.TotalRegions);
                TensorSize = scanner.TotalRegions;
                var diff = BatchSize - scanner.TotalRegions;
                var tensorSize = BatchSize == -1 ? scanner.TotalRegions : BatchSize;
                Tensor = new DenseTensor<float>(new[] { tensorSize, 3, scanner.CropSizeX, scanner.CropSizeY });
                var tasks = new Task[scanner.TotalRegions];
                foreach (var regionReader in scanner.ScanByRegions())
                {
                    var tensor = new TensorPoolItem(Tensor, regionReader.TensorIndex, regionReader.X, regionReader.Y, scanner.CropSizeX, scanner.CropSizeY);
                    _index[regionReader.TensorIndex] = tensor;
                    tasks[regionReader.TensorIndex] = Task.Factory.StartNew((_reader) =>
                    {
                        var reader = (ImageRegionReader)_reader;
                        reader.Read((x, y, r, g, b) =>
                        {
                            _index[reader.TensorIndex].FastSet(x, y, b, g, r);
                        });
                    }, regionReader);
                }
                Task.WaitAll(tasks);
            }
        }

        public void FillFromImageInvertAxeBGR(CoreDrawing.System.Drawing.Bitmap image)
        {
            using (var scanner = new ImageScanner(image, CropSize))
            {
                _index = new Dictionary<int, TensorPoolItem>(scanner.TotalRegions);
                TensorSize = scanner.TotalRegions;
                var diff = BatchSize - scanner.TotalRegions;
                var tensorSize = BatchSize == -1 ? scanner.TotalRegions : BatchSize;
                Tensor = new DenseTensor<float>(new[] { tensorSize, 3, scanner.CropSizeX, scanner.CropSizeY });
                var tasks = new Task[scanner.TotalRegions];
                foreach (var regionReader in scanner.ScanByRegions())
                {
                    var tensor = new TensorPoolItem(Tensor, regionReader.TensorIndex, regionReader.X, regionReader.Y, scanner.CropSizeX, scanner.CropSizeY);
                    _index[regionReader.TensorIndex] = tensor;
                    tasks[regionReader.TensorIndex] = Task.Factory.StartNew((_reader) =>
                    {
                        var reader = (ImageRegionReader)_reader;
                        reader.Read((x, y, r, g, b) =>
                        {
                            _index[reader.TensorIndex].FastSet(y, x, b, g, r);
                        });
                    }, regionReader);
                }
                Task.WaitAll(tasks);
            }
        }

        public void Dispose()
        {
            Tensor = null!;
        }
    }
}
