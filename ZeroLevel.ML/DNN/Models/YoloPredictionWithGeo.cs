namespace ZeroLevel.ML.DNN.Models
{
    public sealed class YoloPredictionWithGeo
    {
        private readonly YoloPrediction _yoloPrediction;
        public double Lat { get; private set; }
        public double Lon { get; private set; }

        public void UpdateGeo(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;
        }

        public int Class => _yoloPrediction.Class;
        public float Cx => _yoloPrediction.Cx;
        public float Cy => _yoloPrediction.Cy;
        public float W => _yoloPrediction.W;
        public float H => _yoloPrediction.H;
        public float Score => _yoloPrediction.Score;
        public string Label => _yoloPrediction.Label;

        public float X => _yoloPrediction.X;
        public float Y => _yoloPrediction.Y;
        public float Area => _yoloPrediction.Area;
        public string Description => _yoloPrediction.Description;

        public YoloPredictionWithGeo(YoloPrediction yoloPrediction)
        {
            _yoloPrediction = yoloPrediction;
        }
    }
}
