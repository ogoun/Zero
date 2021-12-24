using System;
using System.Drawing;
using System.Linq;
using ZeroLevel.HNSW;

namespace HNSWDemo.Tests
{
    public class HistogramTest
        : ITest
    {
        private static int Count = 3000;
        private static int Dimensionality = 128;
        private static int Width = 3000;
        private static int Height = 3000;

        public void Run()
        {
            var vectors = VectorUtils.RandomVectors(Dimensionality, Count);
            var world = SmallWorld.CreateWorld<float[]>(NSWOptions<float[]>.Create(8, 16, 200, 200, Metrics.L2Euclidean));
            world.AddItems(vectors);

            var distance = new Func<int, int, float>((id1, id2) => Metrics.L2Euclidean(world.GetVector(id1), world.GetVector(id2)));
            var weights = world.GetLinks().SelectMany(pair => pair.Value.Select(id => distance(pair.Key, id)));
            var histogram = new Histogram(HistogramMode.SQRT, weights);
            histogram.Smooth();

            int threshold = histogram.OTSU();
            var min = histogram.Bounds[threshold - 1];
            var max = histogram.Bounds[threshold];
            var R = (max + min) / 2;

            DrawHistogram(histogram, @"D:\hist.jpg");
        }

        static void DrawHistogram(Histogram histogram, string filename)
        {
            var wb = Width / histogram.Values.Length;
            var k = ((float)Height) / (float)histogram.Values.Max();

            var maxes = histogram.GetMaximums().ToDictionary(m => m.Index, m => m);
            int threshold = histogram.OTSU();

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
