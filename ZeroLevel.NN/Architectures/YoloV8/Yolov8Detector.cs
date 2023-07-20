using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;
using ZeroLevel.NN.Models;

namespace ZeroLevel.NN.Architectures.YoloV5
{
    public sealed class TensorPoolItem
    {
        public int StartX;
        public int StartY;
        public int Width;
        public int Height;
        public int TensorIndex;
        public Tensor<float> Tensor;

        public TensorPoolItem(Tensor<float> tensor, int tensorIndex, int startX, int startY, int width, int height)
        {
            Tensor = tensor;
            TensorIndex = tensorIndex;
            StartX = startX;
            StartY = startY;
            Width = width;
            Height = height;
        }

        public void Set(int x, int y, float valueR, float valueG, float valueB)
        {
            var tx = x - StartX;
            if (tx < 0 || tx >= Width) return;
            var ty = y - StartY;

            this.Tensor[TensorIndex, 0, tx, ty] = valueR;
            this.Tensor[TensorIndex, 1, tx, ty] = valueG;
            this.Tensor[TensorIndex, 2, tx, ty] = valueB;
        }
    }

    public sealed class TensorPool
    {
        public Tensor<float>[] Tensors;
        private const int MAX_BATCH_SIZE = 16;

        public readonly int CropHeight;
        public readonly int CropWidth;
        public readonly int Width;
        public readonly int Height;

        private readonly SortedDictionary<int, TensorPoolItem[]> _pool = new SortedDictionary<int, TensorPoolItem[]>();
        private readonly Dictionary<int, List<TensorPoolItem>> _batchPool = new Dictionary<int, List<TensorPoolItem>>();

        public TensorPool(int fullWidth, int fullHeight, int cropWidth, int cropHeight, bool overlap, float overlapProportion)
        {
            Width = fullWidth;
            Height = fullHeight;
            CropHeight = cropHeight;
            var x_points = SplitRange(fullWidth, cropWidth, overlap ? overlapProportion : 0).ToArray();
            var y_points = SplitRange(fullHeight, cropHeight, overlap ? overlapProportion : 0).ToArray();
            var total = x_points.Length * y_points.Length;
            int offset = total % MAX_BATCH_SIZE;
            int count_tensor_batches = total / MAX_BATCH_SIZE + (offset == 0 ? 0 : 1);

            Tensors = new Tensor<float>[count_tensor_batches];
            for (int batch_index = 0; batch_index < count_tensor_batches; batch_index++)
            {
                if (batch_index < count_tensor_batches - 1)
                {
                    Tensors[batch_index] = new DenseTensor<float>(new[] { MAX_BATCH_SIZE, 3, cropWidth, cropHeight });
                }
                else
                {
                    Tensors[batch_index] = new DenseTensor<float>(new[] { offset == 0 ? MAX_BATCH_SIZE : offset, 3, cropWidth, cropHeight });
                }
            }
            var batchIndex = 0;
            var tensorIndex = 0;
            _batchPool[batchIndex] = new List<TensorPoolItem>(MAX_BATCH_SIZE);
            foreach (var y in y_points)
            {
                var columnPool = new TensorPoolItem[x_points.Length];
                int column = 0;
                foreach (var x in x_points)
                {
                    columnPool[column] = new TensorPoolItem(Tensors[batchIndex], tensorIndex, x, y, cropWidth, cropHeight);
                    _batchPool[batchIndex].Add(columnPool[column]);
                    column++;
                    tensorIndex++;
                    if (tensorIndex >= MAX_BATCH_SIZE)
                    {
                        tensorIndex = 0;
                        batchIndex++;
                        _batchPool[batchIndex] = new List<TensorPoolItem>(MAX_BATCH_SIZE);
                    }
                }
                _pool[y] = columnPool;
            }
            VirtualPool = new List<TensorPoolItem>(x_points.Length * 2);
        }

        public TensorPoolItem GetTensor(int batchIndex, int tensorIndex)
        {
            foreach (var item in _batchPool[batchIndex])
            {
                if (item.TensorIndex == tensorIndex) return item;
            }
            throw new InvalidProgramException();
        }

        public IEnumerable<int> SplitRange(int size, int cropSize, float overlapProportion)
        {
            var stride = (int)(cropSize * (1f - overlapProportion));
            var counter = 0;
            while (true)
            {
                var pt = stride * counter;
                if ((pt + cropSize) > size)
                {
                    if (cropSize == size || pt == size)
                    {
                        break;
                    }
                    yield return size - cropSize;
                    break;
                }
                else
                {
                    yield return pt;
                }
                counter++;
            }
        }

        private readonly List<TensorPoolItem> VirtualPool;
        public List<TensorPoolItem> GetSubpoolForY(int y)
        {
            VirtualPool.Clear();
            foreach (var pair in _pool)
            {
                if (y >= pair.Key && y < (pair.Key + CropHeight))
                {
                    VirtualPool.AddRange(pair.Value);
                }
            }
            return VirtualPool;
        }
    }

    public class Yolov8Detector
        : SSDNN
    {
        private int INPUT_WIDTH = 1280;
        private int INPUT_HEIGHT = 1280;

        public Yolov8Detector(string modelPath, int inputWidth = 1280, int inputHeight = 1280, bool gpu = false)
            : base(modelPath, gpu)
        {
            INPUT_HEIGHT = inputHeight;
            INPUT_WIDTH = inputWidth;
        }
        public List<YoloPrediction> Predict(Image image, float threshold)
        {
            var sw = new Stopwatch();
            sw.Start();
            var input = ImageToTensors(image);
            sw.Stop();
            return Predict(input, threshold);
        }
        public List<YoloPrediction> Predict(TensorPool inputs, float threshold)
        {
            var result = new List<YoloPrediction>();
            var relative_koef_x = 1.0f / (float)inputs.Width;
            var relative_koef_y = 1.0f / (float)inputs.Height;
            int batchIndex = 0;
            foreach (var input in inputs.Tensors)
            {
                Extract(new Dictionary<string, Tensor<float>> { { "images", input } }, d =>
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
                        for (int tensorIndex = 0; tensorIndex < output.Dimensions[0]; tensorIndex++)
                        {
                            var tensor = inputs.GetTensor(batchIndex, tensorIndex);
                            for (int box = 0; box < output.Dimensions[2]; box++)
                            {
                                var conf = output[tensorIndex, 4, box]; // уверенность в наличии любого объекта
                                if (conf > threshold)
                                {
                                    // Перевод относительно входа модели в относительные координаты
                                    var cx = output[tensorIndex, 1, box];
                                    var cy = output[tensorIndex, 0, box];
                                    var w = output[tensorIndex, 3, box];
                                    var h = output[tensorIndex, 2, box];
                                    // Перевод в координаты отнисительно текущего смещения
                                    cx += tensor.StartX;
                                    cy += tensor.StartY;
                                    result.Add(new YoloPrediction
                                    {
                                        Cx = cx * relative_koef_x,
                                        Cy = cy * relative_koef_y,
                                        W = w * relative_koef_x,
                                        H = h * relative_koef_y,
                                        Class = 0,
                                        Label = "0",
                                        Score = conf
                                    });
                                }
                            }
                        }
                    }
                });
                batchIndex++;
            }
            return result;
        }

        private TensorPool ImageToTensors(Image image)
        {
            var pool = new TensorPool(image.Width, image.Height, INPUT_WIDTH, INPUT_HEIGHT, true, 0);
            var koef = 1.0f / 255.0f;
            ((Image<Rgb24>)image).ProcessPixelRows(pixels =>
            {
                for (int y = 0; y < pixels.Height; y++)
                {
                    var subpool = pool.GetSubpoolForY(y);
                    Span<Rgb24> pixelSpan = pixels.GetRowSpan(y);
                    for (int x = 0; x < pixels.Width; x++)
                    {
                        float r = koef * (float)pixelSpan[x].R;
                        float g = koef * (float)pixelSpan[x].G;
                        float b = koef * (float)pixelSpan[x].B;
                        for (int i = 0; i < subpool.Count; i++)
                        {
                            subpool[i].Set(x, y, r, g, b);
                        }
                    }
                }
            });
            return pool;
        }
    }
}
