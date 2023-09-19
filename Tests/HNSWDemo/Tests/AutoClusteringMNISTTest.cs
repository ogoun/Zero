using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ZeroLevel.HNSW;
using ZeroLevel.HNSW.Services;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.Mathemathics;

namespace HNSWDemo.Tests
{
    public class AutoClusteringMNISTTest
        : ITest
    {
        private static int Width = 3000;
        private static int Height = 3000;

        private static byte[] PadLines(byte[] bytes, int rows, int columns)
        {
            int currentStride = columns; // 3
            int newStride = columns;  // 4
            byte[] newBytes = new byte[newStride * rows];
            for (int i = 0; i < rows; i++)
                Buffer.BlockCopy(bytes, currentStride * i, newBytes, newStride * i, currentStride);
            return newBytes;
        }

        public void Run()
        {
            var folder = @"D:\Mnist";
            int columns = 28;
            int rows = 28;
            int imageCount, rowCount, colCount;
            var buf = new byte[4];
            var image = new byte[rows * columns];
            var vectors = new List<byte[]>();
            using (var fs = new FileStream("t10k-images.idx3-ubyte", FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // first 4 bytes is a magic number
                fs.Read(buf, 0, 4);
                // second 4 bytes is the number of images
                fs.Read(buf, 0, 4);
                imageCount = BitConverter.ToInt32(buf.Reverse().ToArray(), 0);
                // third 4 bytes is the row count
                fs.Read(buf, 0, 4);
                rowCount = BitConverter.ToInt32(buf.Reverse().ToArray(), 0);
                // fourth 4 bytes is the column count
                fs.Read(buf, 0, 4);
                colCount = BitConverter.ToInt32(buf.Reverse().ToArray(), 0);

                for (int i = 0; i < imageCount; i++)
                {
                    fs.Read(image, 0, image.Length);
                    var v = new byte[image.Length];
                    Array.Copy(image, v, image.Length);
                    vectors.Add(v);
                }
            }
            var options = NSWOptions<byte[]>.Create(8, 16, 200, 200, Metrics.L2EuclideanDistance);
            SmallWorld<byte[]> world;
            if (File.Exists("graph_mnist.bin"))
            {
                using (var fs = new FileStream("graph_mnist.bin", FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    world = SmallWorld.CreateWorldFrom<byte[]>(options, fs);
                }
            }
            else
            {
                world = SmallWorld.CreateWorld<byte[]>(options);
                world.AddItems(vectors);
                using (var fs = new FileStream("graph_mnist.bin", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    world.Serialize(fs);
                }
            }

            var distance = new Func<int, int, float>((id1, id2) => Metrics.L2EuclideanDistance(world.GetVector(id1), world.GetVector(id2)));
            var links = world.GetLinks().SelectMany(pair => pair.Value.Select(p=> distance(pair.Key, p))).ToList();
            var exists = links.Where(n => n > 0).ToArray();
            
            var histogram = new Histogram(HistogramMode.LOG, links);
            DrawHistogram(histogram, @"D:\histogram.jpg");

            var clusters = AutomaticGraphClusterer.DetectClusters(world);
            Console.WriteLine($"Found {clusters.Count} clusters");

            while (clusters.Count > 10)
            {
                var last = clusters[clusters.Count - 1];
                var testDistance = clusters[0].MinDistance(distance, last);
                var index = 0;
                for (int i = 1; i < clusters.Count - 1; i++)
                { 
                    var d = clusters[i].MinDistance(distance, last);
                    if (d < testDistance)
                    {
                        testDistance = d;
                        index = i;
                    }
                }
                clusters[index].Merge(last);
                clusters.RemoveAt(clusters.Count - 1);
            }

            for (int i = 0; i < clusters.Count; i++)
            {
                var ouput = Path.Combine(folder, i.ToString("D3"));
                FSUtils.CleanAndTestFolder(ouput);
                foreach (var v in clusters[i])
                {                    
                    int stride = columns;
                    byte[] newbytes = PadLines(world.GetVector(v), rows, columns);
                    using (var im = new Bitmap(columns, rows, stride, PixelFormat.Format8bppIndexed, Marshal.UnsafeAddrOfPinnedArrayElement(newbytes, 0)))
                    {
                        im.Save(Path.Combine(ouput, $"{v}.bmp"));
                    }
                }
                Console.WriteLine($"Cluster {i + 1} countains {clusters[i].Count} items");
            }
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
