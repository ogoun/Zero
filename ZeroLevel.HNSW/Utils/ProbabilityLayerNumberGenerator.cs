using System;

namespace ZeroLevel.HNSW.Services
{
    internal sealed class ProbabilityLayerNumberGenerator
    {
        private readonly float[] _probabilities;

        internal ProbabilityLayerNumberGenerator(int maxLayers, int M)
        {
            _mL = maxLayers;
            _probabilities = new float[maxLayers];
            var m_L = 1.0f / Math.Log(M);
            for (int i = 0; i < maxLayers; i++)
            {
                _probabilities[i] = (float)(Math.Exp(-i / m_L) * (1 - Math.Exp(-1 / m_L)));
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
