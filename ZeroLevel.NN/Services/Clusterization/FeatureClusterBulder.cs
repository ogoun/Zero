namespace ZeroLevel.NN.Clusterization
{
    public class FeatureClusterBulder
    {
        public FeatureClusterCollection<T> Build<T>(IEnumerable<T> items, Func<T, float[]> vectorExtractor, Func<float[], float[], double> similarityFunction, float threshold, float clusterThreshold = 0.5f)
        {
            var collection = new FeatureClusterCollection<T>();
            foreach (var item in items)
            {
                bool isAdded = false;
                foreach (var cluster in collection.Clusters)
                {
                    if (cluster.Value.IsNeighbor(item, similarityFunction, threshold, clusterThreshold))
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
            MergeClusters(collection, similarityFunction, threshold, clusterThreshold);
            return collection;
        }

        private void MergeClusters<T>(FeatureClusterCollection<T> collection, Func<float[], float[], double> similarityFunction, float threshold, float clusterThreshold = 0.5f)
        {
            int lastCount = collection.Clusters.Count;
            var removed = new Queue<int>();
            do
            {
                var ids = collection.Clusters.Keys.ToList();
                for (var i = 0; i < ids.Count - 1; i++)
                {
                    for (var j = i + 1; j < ids.Count; j++)
                    {
                        var c1 = collection.Clusters[ids[i]];
                        var c2 = collection.Clusters[ids[j]];
                        if (c1.IsNeighborCluster(c2, similarityFunction, threshold, clusterThreshold))
                        {
                            c1.Merge(c2);
                            removed.Enqueue(ids[j]);
                            ids.RemoveAt(j);
                            j--;
                        }
                    }
                }
                while (removed.Count > 0)
                {
                    collection.Clusters.Remove(removed.Dequeue());
                }
                if (lastCount == collection.Clusters.Count)
                {
                    break;
                }
                lastCount = collection.Clusters.Count;
            } while (true);
        }
    }
}
