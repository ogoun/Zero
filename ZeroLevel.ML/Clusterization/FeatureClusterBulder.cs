using System;
using System.Collections.Generic;

namespace ZeroLevel.ML
{
    public class FeatureClusterBulder
    {
        public FeatureClusterCollection<T> Build<T>(IEnumerable<T> items, Func<T, float[]> vectorExtractor, Func<float[], float[], double> similarityFunction, float threshold)
        {
            var collection = new FeatureClusterCollection<T>();
            foreach (var item in items)
            {
                bool isAdded = false;
                foreach (var cluster in collection.Clusters)
                {
                    if (cluster.Value.IsNeighbor(item, similarityFunction, threshold))
                    {
                        cluster.Value.Append(item);
                        isAdded = true;
                        break;
                    }
                }
                if (false == isAdded)
                {
                    var cluster = new FeatureCluster<T>(vectorExtractor);
                    cluster.Append(item);
                    collection.Add(cluster);
                }
            }
            return collection;
        }
    }
}
