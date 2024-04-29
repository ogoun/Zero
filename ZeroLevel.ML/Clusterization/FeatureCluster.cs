using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.ML
{
    public class FeatureCluster<T>
    {
        private readonly List<T> _features = new List<T>();
        private readonly Func<T, float[]> _vectorExtractor;

        public float[] CenterOfMass => _centerOfMass;

        private float[] _centerOfMass;

        public FeatureCluster(Func<T, float[]> vectorExtractor)
        {
            _vectorExtractor = vectorExtractor;
        }

        public IList<T> Features => _features;

        internal void Append(T item)
        {
            _features.Add(item);
            _centerOfMass = _vectorExtractor.Invoke(_features[0]);
            if (_features.Count > 1)
            {
                foreach (var f in _features.Skip(1))
                {
                    var f_vector = _vectorExtractor(f);
                    for (int i = 0; i < f_vector.Length; i++)
                    {
                        _centerOfMass[i] += f_vector[i];
                    }
                }
                for (int i = 0; i < _centerOfMass.Length; i++)
                {
                    _centerOfMass[i] /= _features.Count;
                }
            }
        }
        public bool IsNeighbor(T feature, Func<float[], float[], double> similarityFunction, float threshold)
        {
            if (_features.Count == 0) return true;
            var similarity = similarityFunction(_vectorExtractor(feature), _centerOfMass);
            return similarity <= threshold;
        }
    }
}
