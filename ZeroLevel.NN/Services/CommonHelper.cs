namespace Zero.NN.Services
{
    internal static class CommonHelper
    {
        public static float Sigmoid(float x)
        {
            if (x >= 0)
            {
                return 1.0f / (1.0f + (float)Math.Exp(-x));
            }
            else
            {
                return (float)(Math.Exp(x) / (1.0f + Math.Exp(x)));
            }
        }

        public static float Logit(float x)
        {
            if (x == 0)
            {
                return (float)(int.MinValue);
            }
            else if (x == 1)
            {
                return (float)(int.MaxValue);
            }
            else
            {
                return (float)Math.Log(x / (1.0f - x));
            }
        }
    }
}
