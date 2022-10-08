namespace ZeroLevel.NN.Services.KNN
{
    public class FPoint
    {
        public float[] Values { get; set; }
    }

    public interface IMetric
    {
        float Calculate(FPoint a, FPoint b);
    }
}
