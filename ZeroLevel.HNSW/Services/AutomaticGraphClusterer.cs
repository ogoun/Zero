using System.Collections.Generic;

namespace ZeroLevel.HNSW.Services
{
    public static class AutomaticGraphClusterer
    {
        private const int HALF_LONG_BITS = 32;

        public static List<HashSet<int>> DetectClusters<T>(SmallWorld<T> world)
        {
            var links = world.GetNSWLinks();
            // 1. Find R - bound between intra-cluster distances and out-of-cluster distances
            var histogram = new Histogram(HistogramMode.SQRT, links.Values);
            int threshold = histogram.OTSU();
            var min = histogram.Bounds[threshold - 1];
            var max = histogram.Bounds[threshold];
            var R = (max + min) / 2;


            // 2. Get links with distances less than R
            var resultLinks = new SortedList<long, float>();
            foreach (var pair in links)
            {
                if (pair.Value < R)
                {
                    resultLinks.Add(pair.Key, pair.Value);
                }
            }

            // 3. Extract clusters
            List<HashSet<int>> clusters = new List<HashSet<int>>();
            foreach (var pair in resultLinks)
            {
                var k = pair.Key;
                var id1 = (int)(k >> HALF_LONG_BITS);
                var id2 = (int)(k - (((long)id1) << HALF_LONG_BITS));

                bool found = false;
                foreach (var c in clusters)
                {
                    if (c.Contains(id1))
                    {
                        c.Add(id2);
                        found = true;
                        break;
                    }
                    else if (c.Contains(id2))
                    {
                        c.Add(id1);
                        found = true;
                        break;
                    }
                }
                if (found == false)
                {
                    var c = new HashSet<int>();
                    c.Add(id1);
                    c.Add(id2);
                    clusters.Add(c);
                }
            }
            return clusters;
        }
    }
}
