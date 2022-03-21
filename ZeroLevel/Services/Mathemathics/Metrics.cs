using System;

namespace ZeroLevel.Services.Mathemathics
{
    public enum KnownMetrics
    {
        Cosine, Manhattanm, Euclide, Chebyshev
    }


    public static class Metrics
    {
        public static Func<float[], float[], double> CreateFloat(KnownMetrics metric)
        {
            switch (metric)
            {
                case KnownMetrics.Euclide:
                    return new Func<float[], float[], double>((u, v) => L2EuclideanDistance(u, v));
                case KnownMetrics.Cosine:
                    return new Func<float[], float[], double>((u, v) => CosineDistance(u, v));
                case KnownMetrics.Chebyshev:
                    return new Func<float[], float[], double>((u, v) => ChebyshevDistance(u, v));
                case KnownMetrics.Manhattanm:
                    return new Func<float[], float[], double>((u, v) => L1ManhattanDistance(u, v));
            }
            throw new Exception($"Metric '{metric.ToString()}' not supported for Float type");
        }

        public static Func<byte[], byte[], double> CreateByte(KnownMetrics metric)
        {
            switch (metric)
            {
                case KnownMetrics.Euclide:
                    return new Func<byte[], byte[], double>((u, v) => L2EuclideanDistance(u, v));
                case KnownMetrics.Cosine:
                    return new Func<byte[], byte[], double>((u, v) => CosineDistance(u, v));
                case KnownMetrics.Chebyshev:
                    return new Func<byte[], byte[], double>((u, v) => ChebyshevDistance(u, v));
                case KnownMetrics.Manhattanm:
                    return new Func<byte[], byte[], double>((u, v) => L1ManhattanDistance
                    (u, v));
            }
            throw new Exception($"Metric '{metric.ToString()}' not supported for Byte type");
        }

        public static Func<long[], long[], double> CreateLong(KnownMetrics metric)
        {
            switch (metric)
            {
                case KnownMetrics.Euclide:
                    return new Func<long[], long[], double>((u, v) => L2EuclideanDistance(u, v));
                case KnownMetrics.Cosine:
                    return new Func<long[], long[], double>((u, v) => CosineDistance(u, v));
                case KnownMetrics.Chebyshev:
                    return new Func<long[], long[], double>((u, v) => ChebyshevDistance(u, v));
                case KnownMetrics.Manhattanm:
                    return new Func<long[], long[], double>((u, v) => L1ManhattanDistance(u, v));
            }
            throw new Exception($"Metric '{metric.ToString()}' not supported for Long type");
        }

        public static Func<int[], int[], double> CreateInt(KnownMetrics metric)
        {
            switch (metric)
            {
                case KnownMetrics.Euclide:
                    return new Func<int[], int[], double>((u, v) => L2EuclideanDistance(u, v));
                case KnownMetrics.Cosine:
                    return new Func<int[], int[], double>((u, v) => CosineDistance(u, v));
                case KnownMetrics.Chebyshev:
                    return new Func<int[], int[], double>((u, v) => ChebyshevDistance(u, v));
                case KnownMetrics.Manhattanm:
                    return new Func<int[], int[], double>((u, v) => L1ManhattanDistance(u, v));
            }
            throw new Exception($"Metric '{metric.ToString()}' not supported for Int type");
        }

        /// <summary>
        /// The taxicab metric is also known as rectilinear distance, 
        /// L1 distance or L1 norm, city block distance, Manhattan distance, 
        /// or Manhattan length, with the corresponding variations in the name of the geometry. 
        /// It represents the distance between points in a city road grid. 
        /// It examines the absolute differences between the coordinates of a pair of objects.
        /// </summary>
        public static float L1ManhattanDistance(float[] v1, float[] v2)
        {
            float res = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                float t = v1[i] - v2[i];
                res += t * t;
            }
            return (res);
        }

        public static float L1ManhattanDistance(byte[] v1, byte[] v2)
        {
            float res = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                float t = v1[i] - v2[i];
                res += t * t;
            }
            return (res);
        }

        public static float L1ManhattanDistance(int[] v1, int[] v2)
        {
            float res = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                float t = v1[i] - v2[i];
                res += t * t;
            }
            return (res);
        }

        public static float L1ManhattanDistance(long[] v1, long[] v2)
        {
            float res = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                float t = v1[i] - v2[i];
                res += t * t;
            }
            return (res);
        }

        /// <summary>
        /// Euclidean distance is the most common use of distance. 
        /// Euclidean distance, or simply 'distance', 
        /// examines the root of square differences between the coordinates of a pair of objects. 
        /// This is most generally known as the Pythagorean theorem.
        /// </summary>
        public static float L2EuclideanDistance(float[] v1, float[] v2)
        {
            float res = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                float t = v1[i] - v2[i];
                res += t * t;
            }
            return (float)Math.Sqrt(res);
        }

        public static float L2EuclideanDistance(byte[] v1, byte[] v2)
        {
            float res = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                float t = v1[i] - v2[i];
                res += t * t;
            }
            return (float)Math.Sqrt(res);
        }

        public static float L2EuclideanDistance(int[] v1, int[] v2)
        {
            float res = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                float t = v1[i] - v2[i];
                res += t * t;
            }
            return (float)Math.Sqrt(res);
        }

        public static float L2EuclideanDistance(long[] v1, long[] v2)
        {
            float res = 0;
            for (int i = 0; i < v1.Length; i++)
            {
                float t = v1[i] - v2[i];
                res += t * t;
            }
            return (float)Math.Sqrt(res);
        }

        /// <summary>
        /// The general metric for distance is the Minkowski distance. 
        /// When lambda is equal to 1, it becomes the city block distance (L1), 
        /// and when lambda is equal to 2, it becomes the Euclidean distance (L2). 
        /// The special case is when lambda is equal to infinity (taking a limit), 
        /// where it is considered as the Chebyshev distance.
        /// </summary>
        public static float MinkowskiDistance(float[] v1, float[] v2, int order)
        {
            int count = v1.Length;
            double sum = 0.0;
            for (int i = 0; i < count; i++)
            {
                sum = sum + Math.Pow(Math.Abs(v1[i] - v2[i]), order);
            }
            return (float)Math.Pow(sum, (1 / order));
        }

        public static float MinkowskiDistance(byte[] v1, byte[] v2, int order)
        {
            int count = v1.Length;
            double sum = 0.0;
            for (int i = 0; i < count; i++)
            {
                sum = sum + Math.Pow(Math.Abs(v1[i] - v2[i]), order);
            }
            return (float)Math.Pow(sum, (1 / order));
        }

        public static float MinkowskiDistance(int[] v1, int[] v2, int order)
        {
            int count = v1.Length;
            double sum = 0.0;
            for (int i = 0; i < count; i++)
            {
                sum = sum + Math.Pow(Math.Abs(v1[i] - v2[i]), order);
            }
            return (float)Math.Pow(sum, (1 / order));
        }

        public static float MinkowskiDistance(long[] v1, long[] v2, int order)
        {
            int count = v1.Length;
            double sum = 0.0;
            for (int i = 0; i < count; i++)
            {
                sum = sum + Math.Pow(Math.Abs(v1[i] - v2[i]), order);
            }
            return (float)Math.Pow(sum, (1 / order));
        }

        /// <summary>
        /// Chebyshev distance is also called the Maximum value distance, 
        /// defined on a vector space where the distance between two vectors is 
        /// the greatest of their differences along any coordinate dimension. 
        /// In other words, it examines the absolute magnitude of the differences 
        /// between the coordinates of a pair of objects.
        /// </summary>
        public static double ChebyshevDistance(float[] v1, float[] v2)
        {
            int count = v1.Length;
            float max = float.MinValue;
            float c;
            for (int i = 0; i < count; i++)
            {
                c = Math.Abs(v1[i] - v2[i]);
                if (c > max)
                {
                    max = c;
                }
            }
            return max;
        }

        public static double ChebyshevDistance(byte[] v1, byte[] v2)
        {
            int count = v1.Length;
            float max = float.MinValue;
            float c;
            for (int i = 0; i < count; i++)
            {
                c = Math.Abs(v1[i] - v2[i]);
                if (c > max)
                {
                    max = c;
                }
            }
            return max;
        }

        public static double ChebyshevDistance(int[] v1, int[] v2)
        {
            int count = v1.Length;
            float max = float.MinValue;
            float c;
            for (int i = 0; i < count; i++)
            {
                c = Math.Abs(v1[i] - v2[i]);
                if (c > max)
                {
                    max = c;
                }
            }
            return max;
        }

        public static double ChebyshevDistance(long[] v1, long[] v2)
        {
            int count = v1.Length;
            float max = float.MinValue;
            float c;
            for (int i = 0; i < count; i++)
            {
                c = Math.Abs(v1[i] - v2[i]);
                if (c > max)
                {
                    max = c;
                }
            }
            return max;
        }

        public static float CosineDistance(float[] u, float[] v)
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

        public static float CosineDistance(byte[] u, byte[] v)
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

        public static float CosineDistance(int[] u, int[] v)
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

        public static float CosineDistance(long[] u, long[] v)
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

        public static float CosineClipped(float[] u, float[] v, float min, float max)
        {
            var similarity = CosineDistance(u, v);
            if (min > similarity) similarity = min;
            if (max < similarity) similarity = max;
            return similarity;
        }
    }
}
