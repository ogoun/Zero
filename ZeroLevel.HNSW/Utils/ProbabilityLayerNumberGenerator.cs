using System;

namespace ZeroLevel.HNSW.Services
{
    internal sealed class ProbabilityLayerNumberGenerator
    {
        private const float DIVIDER = 3.361f;
        private readonly float[] _probabilities;
        private float _mL;

        internal ProbabilityLayerNumberGenerator(int maxLayers, int M)
        {
            _mL = maxLayers;
            _probabilities = new float[maxLayers];
            var probability = 1.0f / DIVIDER;
            for (int i = 0; i < maxLayers; i++)
            {
                _probabilities[i] = probability;
                probability /= DIVIDER;
            }
        }

        internal int GetRandomLayer()
        {
            var probability = DefaultRandomGenerator.Instance.NextFloat();
            for (int i = 0; i < _probabilities.Length; i++)
            {
                if (probability > _probabilities[i])
                    return i;
            }
            return 0;
        }
    }
}
