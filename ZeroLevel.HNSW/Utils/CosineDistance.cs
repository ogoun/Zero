using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ZeroLevel.HNSW
{
    /// <summary>
    /// Calculates cosine similarity.
    /// </summary>
    /// <remarks>
    /// Intuition behind selecting float as a carrier.
    ///
    /// 1. In practice we work with vectors of dimensionality 100 and each component has value in range [-1; 1]
    ///    There certainly is a possibility of underflow.
    ///    But we assume that such cases are rare and we can rely on such underflow losses.
    ///
    /// 2. According to the article http://www.ti3.tuhh.de/paper/rump/JeaRu13.pdf
    ///    the floating point rounding error is less then 100 * 2^-24 * sqrt(100) * sqrt(100) &lt; 0.0005960
    ///    We deem such precision is satisfactory for out needs.
    /// </remarks>
    public static class CosineDistance
    {
        /// <summary>
        /// Calculates cosine distance without making any optimizations.
        /// </summary>
        /// <param name="u">Left vector.</param>
        /// <param name="v">Right vector.</param>
        /// <returns>Cosine distance between u and v.</returns>
        public static float NonOptimized(float[] u, float[] v)
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

        public static float NonOptimized(byte[] u, byte[] v)
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
                dot += (float)(u[i] * v[i]);
                nru += (float)(u[i] * u[i]);
                nrv += (float)(v[i] * v[i]);
            }

            var similarity = dot / (float)(Math.Sqrt(nru) * Math.Sqrt(nrv));
            return 1 - similarity;
        }

        public static float NonOptimized(int[] u, int[] v)
        {
            if (u.Length != v.Length)
            {
                throw new ArgumentException("Vectors have non-matching dimensions");
            }

            float dot = 0.0f;
            float nru = 0.0f;
            float nrv = 0.0f;
            byte[] bu;
            byte[] bv;

            for (int i = 0; i < u.Length; ++i)
            {
                bu = BitConverter.GetBytes(u[i]);
                bv = BitConverter.GetBytes(v[i]);

                dot += (float)(bu[0] * bv[0]);
                nru += (float)(bu[0] * bu[0]);
                nrv += (float)(bv[0] * bv[0]);

                dot += (float)(bu[1] * bv[1]);
                nru += (float)(bu[1] * bu[1]);
                nrv += (float)(bv[1] * bv[1]);

                dot += (float)(bu[2] * bv[2]);
                nru += (float)(bu[2] * bu[2]);
                nrv += (float)(bv[2] * bv[2]);

                dot += (float)(bu[3] * bv[3]);
                nru += (float)(bu[3] * bu[3]);
                nrv += (float)(bv[3] * bv[3]);
            }

            var similarity = dot / (float)(Math.Sqrt(nru) * Math.Sqrt(nrv));
            return 1 - similarity;
        }

        public static float NonOptimized(long[] u, long[] v)
        {
            if (u.Length != v.Length)
            {
                throw new ArgumentException("Vectors have non-matching dimensions");
            }

            float dot = 0.0f;
            float nru = 0.0f;
            float nrv = 0.0f;
            byte[] bu;
            byte[] bv;

            for (int i = 0; i < u.Length; ++i)
            {
                bu = BitConverter.GetBytes(u[i]);
                bv = BitConverter.GetBytes(v[i]);

                dot += (float)(bu[0] * bv[0]);
                nru += (float)(bu[0] * bu[0]);
                nrv += (float)(bv[0] * bv[0]);

                dot += (float)(bu[1] * bv[1]);
                nru += (float)(bu[1] * bu[1]);
                nrv += (float)(bv[1] * bv[1]);

                dot += (float)(bu[2] * bv[2]);
                nru += (float)(bu[2] * bu[2]);
                nrv += (float)(bv[2] * bv[2]);

                dot += (float)(bu[3] * bv[3]);
                nru += (float)(bu[3] * bu[3]);
                nrv += (float)(bv[3] * bv[3]);

                dot += (float)(bu[4] * bv[4]);
                nru += (float)(bu[4] * bu[4]);
                nrv += (float)(bv[4] * bv[4]);

                dot += (float)(bu[5] * bv[5]);
                nru += (float)(bu[5] * bu[5]);
                nrv += (float)(bv[5] * bv[5]);

                dot += (float)(bu[6] * bv[6]);
                nru += (float)(bu[6] * bu[6]);
                nrv += (float)(bv[6] * bv[6]);

                dot += (float)(bu[7] * bv[7]);
                nru += (float)(bu[7] * bu[7]);
                nrv += (float)(bv[7] * bv[7]);
            }

            var similarity = dot / (float)(Math.Sqrt(nru) * Math.Sqrt(nrv));
            return 1 - similarity;
        }

        /// <summary>
        /// Calculates cosine distance with assumption that u and v are unit vectors.
        /// </summary>
        /// <param name="u">Left vector.</param>
        /// <param name="v">Right vector.</param>
        /// <returns>Cosine distance between u and v.</returns>
        public static float ForUnits(float[] u, float[] v)
        {
            if (u.Length != v.Length)
            {
                throw new ArgumentException("Vectors have non-matching dimensions");
            }

            float dot = 0;
            for (int i = 0; i < u.Length; ++i)
            {
                dot += u[i] * v[i];
            }

            return 1 - dot;
        }

        /// <summary>
        /// Calculates cosine distance optimized using SIMD instructions.
        /// </summary>
        /// <param name="u">Left vector.</param>
        /// <param name="v">Right vector.</param>
        /// <returns>Cosine distance between u and v.</returns>
        public static float SIMD(float[] u, float[] v)
        {
            if (!Vector.IsHardwareAccelerated)
            {
                throw new NotSupportedException($"SIMD version of {nameof(CosineDistance)} is not supported");
            }

            if (u.Length != v.Length)
            {
                throw new ArgumentException("Vectors have non-matching dimensions");
            }

            float dot = 0;
            var norm = default(Vector2);
            int step = Vector<float>.Count;

            int i, to = u.Length - step;
            for (i = 0; i <= to; i += step)
            {
                var ui = new Vector<float>(u, i);
                var vi = new Vector<float>(v, i);
                dot += Vector.Dot(ui, vi);
                norm.X += Vector.Dot(ui, ui);
                norm.Y += Vector.Dot(vi, vi);
            }

            for (; i < u.Length; ++i)
            {
                dot += u[i] * v[i];
                norm.X += u[i] * u[i];
                norm.Y += v[i] * v[i];
            }

            norm = Vector2.SquareRoot(norm);
            float n = (norm.X * norm.Y);

            if (n == 0)
            {
                return 1f;
            }

            var similarity = dot / n;
            return 1f - similarity;
        }

        /// <summary>
        /// Calculates cosine distance with assumption that u and v are unit vectors using SIMD instructions.
        /// </summary>
        /// <param name="u">Left vector.</param>
        /// <param name="v">Right vector.</param>
        /// <returns>Cosine distance between u and v.</returns>
        public static float SIMDForUnits(float[] u, float[] v)
        {
            return 1f - DotProduct(ref u, ref v);
        }

        private static readonly int _vs1 = Vector<float>.Count;
        private static readonly int _vs2 = 2 * Vector<float>.Count;
        private static readonly int _vs3 = 3 * Vector<float>.Count;
        private static readonly int _vs4 = 4 * Vector<float>.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float DotProduct(ref float[] lhs, ref float[] rhs)
        {
            float result = 0f;

            var count = lhs.Length;
            var offset = 0;

            while (count >= _vs4)
            {
                result += Vector.Dot(new Vector<float>(lhs, offset), new Vector<float>(rhs, offset));
                result += Vector.Dot(new Vector<float>(lhs, offset + _vs1), new Vector<float>(rhs, offset + _vs1));
                result += Vector.Dot(new Vector<float>(lhs, offset + _vs2), new Vector<float>(rhs, offset + _vs2));
                result += Vector.Dot(new Vector<float>(lhs, offset + _vs3), new Vector<float>(rhs, offset + _vs3));
                if (count == _vs4) return result;
                count -= _vs4;
                offset += _vs4;
            }

            if (count >= _vs2)
            {
                result += Vector.Dot(new Vector<float>(lhs, offset), new Vector<float>(rhs, offset));
                result += Vector.Dot(new Vector<float>(lhs, offset + _vs1), new Vector<float>(rhs, offset + _vs1));
                if (count == _vs2) return result;
                count -= _vs2;
                offset += _vs2;
            }
            if (count >= _vs1)
            {
                result += Vector.Dot(new Vector<float>(lhs, offset), new Vector<float>(rhs, offset));
                if (count == _vs1) return result;
                count -= _vs1;
                offset += _vs1;
            }
            if (count > 0)
            {
                while (count > 0)
                {
                    result += lhs[offset] * rhs[offset];
                    offset++; count--;
                }
            }
            return result;
        }
    }
}
