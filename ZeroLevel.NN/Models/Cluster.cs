namespace ZeroLevel.NN.Models
{
    public class Cluster<T>
    {
        private int _key;
        private readonly List<T> _points = new List<T>();

        public T this[int index] => _points[index];
        public IReadOnlyList<T> Points => _points;
        public int Key { get { return _key; } set { _key = value; } }
        public Cluster()
        {
        }

        public Cluster(T point)
        {
            _points.Add(point);
        }

        public Cluster(IEnumerable<T> points)
        {
            _points.AddRange(points);
        }

        public void Add(T point)
        {
            _points.Add(point);
        }

        public void Remove(T point)
        {
            _points.Remove(point);
        }
        /*
        public bool IsNeighbor(T feature,
            Func<T, float[]> embeddingFunction,
            Func<float[], float[], double> similarityFunction,
            float threshold,
            float clusterThreshold)
        {
            if (_points.Count == 0) return true;
            if (_points.Count == 1)
            {
                var similarity = similarityFunction(embeddingFunction(feature), embeddingFunction(_points[0]));
                return similarity >= threshold;
            }
            var clusterNearestElementsCount = 0;
            foreach (var f in _points)
            {
                var similarity = similarityFunction(embeddingFunction(feature), embeddingFunction(f));
                if (similarity >= threshold)
                {
                    clusterNearestElementsCount++;
                }
            }
            var clusterToFaceScore = (float)clusterNearestElementsCount / (float)_points.Count;
            return clusterToFaceScore > clusterThreshold;
        }
        */
        public bool IsNearest(T feature,
            Func<T, T, double> distanceFunction,
            double maxDistance)
        {
            if (_points.Count == 0) return true;
            if (_points.Count == 1)
            {
                var distance = distanceFunction(feature, _points[0]);
                return distance <= maxDistance;
            }
            foreach (var f in _points)
            {
                var distance = distanceFunction(feature, f);
                if (distance > maxDistance)
                {
                    return false;
                }
            }
            return true;
        }

        public double MinimalDistance(T feature,
            Func<T, T, double> distanceFunction)
        {
            if (_points.Count == 0) return int.MaxValue;
            var min = distanceFunction(feature, _points[0]);
            if (_points.Count == 1)
            {
                return min;
            }
            for (int i = 0; i<_points.Count; i++)
            {
                var distance = distanceFunction(feature, _points[i]);
                if (distance < min)
                {
                    min = distance;
                }
            }
            return min;
        }
        /*
        public bool IsNeighborCluster(Cluster<T> cluster,
            Func<T, float[]> embeddingFunction,
            Func<float[], float[], double> similarityFunction,
            float threshold,
            float clusterThreshold)
        {
            if (_points.Count == 0) return true;
            if (_points.Count == 1 && cluster.IsNeighbor(_points[0], embeddingFunction, similarityFunction, threshold, clusterThreshold))
            {
                return true;
            }
            var clusterNearestElementsCount = 0;
            foreach (var f in _points)
            {
                if (cluster.IsNeighbor(f, embeddingFunction, similarityFunction, threshold, clusterThreshold))
                {
                    clusterNearestElementsCount++;
                }
            }
            var localCount = _points.Count;
            var remoteCount = cluster._points.Count;
            var localIntersection = (float)clusterNearestElementsCount / (float)localCount;
            var remoteIntersection = (float)clusterNearestElementsCount / (float)remoteCount;
            var score = Math.Max(localIntersection, remoteIntersection);
            return score > clusterThreshold;
        }
        */
        public void Merge(Cluster<T> other)
        {
            this._points.AddRange(other.Points);
        }
    }
}
