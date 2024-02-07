using System;

namespace ZeroLevel.HNSW.PHNSW
{
    internal static class PHNSWMetric
    {
        internal static float CosineDistance(float[] u, float[] v)
        {
            if (u.Length != v.Length)
            {
                throw new ArgumentException("Vectors have non-matching dimensions");
            }

            float dot = 0.0f;
            float nru = 0.0f;
            float nrv = 0.0f;
            for (int i = 0; i < u.Length; ++i)
            {
                dot += u[i] * v[i];
                nru += u[i] * u[i];
                nrv += v[i] * v[i];
            }

            var similarity = dot / (float)(Math.Sqrt(nru) * Math.Sqrt(nrv));
            return 1 - similarity;
        }
    }
}
