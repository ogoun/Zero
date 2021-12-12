using System;

namespace ZeroLevel.HNSW
{
    public static class Metrics
    {
        /// <summary>
        /// The taxicab metric is also known as rectilinear distance, 
        /// L1 distance or L1 norm, city block distance, Manhattan distance, 
        /// or Manhattan length, with the corresponding variations in the name of the geometry. 
        /// It represents the distance between points in a city road grid. 
        /// It examines the absolute differences between the coordinates of a pair of objects.
        /// </summary>
        public static float L1Manhattan(float[] v1, float[] v2)
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
        public static float L2Euclidean(float[] v1, float[] v2)
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
    }
}
