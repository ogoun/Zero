namespace ZeroLevel.NN.Clusterization
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
    }
}
