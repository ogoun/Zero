namespace ZeroLevel.ML.DNN.Models
{
    public sealed class ImagePrediction
    {
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;
        public int Width { get; set; }
        public int Height { get; set; }
        public YoloPredictionWithGeo[] Predictions { get; set; } = null!;
    }
}
