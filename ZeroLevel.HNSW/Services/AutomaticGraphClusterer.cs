using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.HNSW.Services
{
    public class Cluster
        : IEnumerable<int>
    {
        private HashSet<int> _elements = new HashSet<int>();

        public int Count => _elements.Count;

        public bool Contains(int id) => _elements.Contains(id);

        public bool Add(int id) => _elements.Add(id);

        public IEnumerator<int> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        public void Merge(Cluster cluster)
        {
            foreach (var e in cluster)
            {
                this._elements.Add(e);
            }
        }

        public float MaxDistance(Func<int, int, float> distance, Cluster other)
        {
            var max = float.MinValue;
            foreach (var e in this._elements)
            {
                foreach (var o in other)
                {
                    var d = distance(e, o);
                    if (d > max)
                    {
                        max = d;
                    }
                }
            }
            return max;
        }

        public float MinDistance(Func<int, int, float> distance, Cluster other)
        {
            var min = float.MaxValue;
            foreach (var e in this._elements)
            {
                foreach (var o in other)
                {
                    var d = distance(e, o);
                    if (d < min)
                    {
                        min = d;
                    }
                }
            }
            return min;
        }

        public float AvgDistance(Func<int, int, float> distance, Cluster other)
        {
            var dist = new List<float>();
            foreach (var e in this._elements)
            {
                foreach (var o in other)
                {
                    dist.Add(distance(e, o));
                }
            }
            return dist.Average();
        }
    }

    public static class AutomaticGraphClusterer
    {
        private class Link
        {
            public int Id1;
            public int Id2;
            public float Distance;
        }

        public static List<Cluster> DetectClusters<T>(SmallWorld<T> world)
        {
            var distance = world.DistanceFunction;
            var links = world.GetLinks().SelectMany(pair => pair.Value.Select(id => new Link { Id1 = pair.Key, Id2 = id, Distance = distance(pair.Key, id) })).ToList();

            // 1. Find R - bound between intra-cluster distances and out-of-cluster distances
            var histogram = new Histogram(HistogramMode.LOG, links.Select(l => l.Distance));
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
            List<Cluster> clusters = new List<Cluster>();
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
                    var c = new Cluster();
                    c.Add(id1);
                    c.Add(id2);
                    clusters.Add(c);
                }
            }
            return clusters;
        }
    }
}
