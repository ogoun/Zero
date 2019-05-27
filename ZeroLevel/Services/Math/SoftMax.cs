using System;

namespace ZeroLevel.Services.Mathematic
{
    public static class SoftMax
    {
        public static double[] Compute(double[] vector)
        {
            double sum = 0;
            for (int i = 0; i < vector.Length; i++)
            {
                sum += Math.Exp(vector[i]);
            }
            double[] result = new double[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                result[i] = Math.Exp(vector[i]) / sum;
            }
            return result;
        }
    }
}
