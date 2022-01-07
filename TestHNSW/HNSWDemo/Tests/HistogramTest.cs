using System;
using System.Drawing;
using System.IO;
using System.Linq;
using ZeroLevel.HNSW;

namespace HNSWDemo.Tests
{
    public class HistogramTest
        : ITest
    {
        private static int Count = 3000;
        private static int Dimensionality = 128;
        private static int Width = 2440;
        private static int Height = 1920;

        public void Run()
        {
            Create(Dimensionality, @"D:\hist");
            // Process.Start("explorer", $"D:\\hist{Dimensionality.ToString("D3")}.jpg");

            /* for (int i = 12; i < 512; i++)
             {
                 Create(i, @"D:\hist");
             }*/
        }

        private void Create(int dim, string output)
        {
            var vectors = VectorUtils.RandomVectors(dim, Count);
            var world = SmallWorld.CreateWorld<float[]>(NSWOptions<float[]>.Create(8, 16, 200, 200, Metrics.L2Euclidean));
            world.AddItems(vectors);

            var distance = new Func<int, int, float>((id1, id2) => Metrics.L2Euclidean(world.GetVector(id1), world.GetVector(id2)));
            var weights = world.GetLinks().SelectMany(pair => pair.Value.Select(id => distance(pair.Key, id)));
            var histogram = new Histogram(HistogramMode.SQRT, weights);
            histogram.Smooth();

            int threshold = histogram.CuttOff();
            var min = histogram.Bounds[threshold - 1];
            var max = histogram.Bounds[threshold];
            var R = (max + min) / 2;

            DrawHistogram(histogram, Path.Combine(output, $"hist{dim.ToString("D3")}.jpg"));
        }

        static void DrawHistogram(Histogram histogram, string filename)
        {
            var wb = Width / histogram.Values.Length;
            var k = ((float)Height) / (float)histogram.Values.Max();

            var maxes = histogram.GetMaximums().ToDictionary(m => m.Index, m => m);
            int threshold = histogram.CuttOff();

            using (var bmp = new Bitmap(Width, Height))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    for (int i = 0; i < histogram.Values.Length; i++)
                    {
                        var height = (int)(histogram.Values[i] * k);
                        if (maxes.ContainsKey(i))
                        {
                            g.DrawRectangle(Pens.Red, i * wb, bmp.Height - height, wb, height);
                            g.DrawRectangle(Pens.Red, i * wb + 1, bmp.Height - height, wb - 1, height);
                        }
                        else
                        {
                            g.DrawRectangle(Pens.Blue, i * wb, bmp.Height - height, wb, height);
                        }
                        if (i == threshold)
                        {
                            g.DrawLine(Pens.Green, i * wb + wb / 2, 0, i * wb + wb / 2, bmp.Height);
                        }
                    }
                }
                bmp.Save(filename);
            }
        }
    }
}
