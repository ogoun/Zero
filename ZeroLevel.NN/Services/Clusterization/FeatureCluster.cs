namespace ZeroLevel.NN.Clusterization
{
    public class FeatureCluster<T>
    {
        private readonly List<T> _features = new List<T>();
        private readonly Func<T, float[]> _vectorExtractor;
        public FeatureCluster(Func<T, float[]> vectorExtractor)
        {
            _vectorExtractor = vectorExtractor;
        }

        public IReadOnlyList<T> Features => _features;

        internal void Append(T face) => _features.Add(face);
        public bool IsNeighbor(T feature, Func<float[], float[], double> similarityFunction, float threshold, float clusterThreshold = 0.5f)
        {
            if (_features.Count == 0) return true;
            if (_features.Count == 1)
            {
                var similarity = similarityFunction(_vectorExtractor(feature), _vectorExtractor(_features[0]));
                return similarity >= threshold;
            }
            var clusterNearestElementsCount = 0;
            foreach (var f in _features)
            {
                var similarity = similarityFunction(_vectorExtractor(feature), _vectorExtractor(f));
                if (similarity >= threshold)
                {
                    clusterNearestElementsCount++;
                }
            }
            var clusterToFaceScore = (float)clusterNearestElementsCount / (float)_features.Count;
            return clusterToFaceScore > clusterThreshold;
        }

        public bool IsNeighborCluster(FeatureCluster<T> cluster, Func<float[], float[], double> similarityFunction, float threshold, float clusterThreshold = 0.5f)
        {
            if (_features.Count == 0) return true;
            if (_features.Count == 1 && cluster.IsNeighbor(_features[0], similarityFunction, threshold, clusterThreshold))
            {
                return true;
            }
            var clusterNearestElementsCount = 0;
            foreach (var f in _features)
            {
                if (cluster.IsNeighbor(f, similarityFunction, threshold, clusterThreshold))
                {
                    clusterNearestElementsCount++;
                }
            }
            var localCount = _features.Count;
            var remoteCount = cluster.Features.Count;
            var localIntersection = (float)clusterNearestElementsCount / (float)localCount;
            var remoteIntersection = (float)clusterNearestElementsCount / (float)remoteCount;
            var score = Math.Max(localIntersection, remoteIntersection);
            return score > clusterThreshold;
        }

        public void Merge(FeatureCluster<T> other)
        {
            this._features.AddRange(other.Features);
        }
    }
}
