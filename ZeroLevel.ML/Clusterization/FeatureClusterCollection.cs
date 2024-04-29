using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ZeroLevel.ML
{
    public class FeatureClusterCollection<T>
    {
        private int _clusterKey = 0;
        private IDictionary<int, FeatureCluster<T>> _clusters = new Dictionary<int, FeatureCluster<T>>();

        public IDictionary<int, FeatureCluster<T>> Clusters => _clusters;

        internal void Add(FeatureCluster<T> cluster)
        {
            _clusters.Add(Interlocked.Increment(ref _clusterKey), cluster);
        }

        public void RemoveByDistance(Func<float[], float[], double> similarityFunction, Func<FeatureCluster<T>, double> winnerValue, double distance)
        {
            bool removed = false;
            do
            {
                removed = false;
                var keys = _clusters.Keys.ToArray();

                var to_remove = new HashSet<int>();

                for (int i = 0; i < keys.Length - 1; i++)
                {
                    for (int j = i + 1; j < keys.Length; j++)
                    {
                        if (to_remove.Contains(j)) continue;
                        if(i == j) continue;
                        var ki = keys[i];
                        var kj = keys[j];

                        var sim = similarityFunction.Invoke(_clusters[ki].CenterOfMass, _clusters[kj].CenterOfMass);
                        if (sim < distance)
                        {
                            var scorei = winnerValue(_clusters[ki]);
                            var scorej = winnerValue(_clusters[kj]);
                            if (scorei < scorej)
                            {
                                to_remove.Add(ki);
                            }
                            else
                            {
                                to_remove.Add(kj);
                            }
                        }
                    }
                }

                if (to_remove.Any())
                {
                    removed = true;
                    foreach (var k in to_remove)
                    {
                        _clusters.Remove(k);
                    }
                }
            } while (removed == true);
        }
    }
}
