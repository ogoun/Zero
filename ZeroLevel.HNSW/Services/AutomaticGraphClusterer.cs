using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.HNSW.Services
{
    public static class AutomaticGraphClusterer
    {
        private const int HALF_LONG_BITS = 32;

        private class Link
        {
            public int Id1;
            public int Id2;
            public float Distance;
        }

        public static List<HashSet<int>> DetectClusters<T>(SmallWorld<T> world)
        {
            var distance = world.DistanceFunction;
            var links = world.GetLinks().SelectMany(pair => pair.Value.Select(id => new Link { Id1 = pair.Key, Id2 = id, Distance = distance(pair.Key, id) })).ToList();

            // 1. Find R - bound between intra-cluster distances and out-of-cluster distances
            var histogram = new Histogram(HistogramMode.SQRT, links.Select(l => l.Distance));
            int threshold = histogram.CuttOff();
            var min = histogram.Bounds[threshold - 1];
            var max = histogram.Bounds[threshold];
            var R = (max + min) / 2;


            // 2. Get links with distances less than R
            var resultLinks = new List<Link>();
            foreach (var l in links)
            {
                if (l.Distance < R)
                {
                    resultLinks.Add(l);
                }
            }

            // 3. Extract clusters
            List<HashSet<int>> clusters = new List<HashSet<int>>();
            foreach (var l in resultLinks)
            {
                var id1 = l.Id1;
                var id2 = l.Id2;
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
