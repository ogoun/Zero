namespace ZeroLevel.ML.Models
{
    public enum PointType { Lat, Lon }
    public class DmsMsPoint
    {
        public int Degrees { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public int Milliseconds { get; set; }
        public PointType Type { get; set; }

        public override string ToString()
        {
            var letter = Type == PointType.Lat
                    ? Degrees < 0 ? "S" : "N"
                    : Degrees < 0 ? "W" : "E";
            return $"{Degrees}°{Minutes}'{Seconds}.{Milliseconds.ToString("D3")}\"{letter}";
        }
    }

    public sealed class DmsMsLocation
    {
        private DmsMsLocation() 
        {
            Latitude = null!;
            Longitude = null!;
        }
        public DmsMsPoint Latitude { get; set; }
        public DmsMsPoint Longitude { get; set; }

        public override string ToString()
        {
            return string.Format("{0}, {1}",
                Latitude, Longitude);
        }

        public static DmsMsLocation CreateFromDouble(double latitude, double longitude)
        {
            return new DmsMsLocation
            {
                Latitude = Extract(latitude, PointType.Lat),
                Longitude = Extract(longitude, PointType.Lon)
            };
        }

        static DmsMsPoint Extract(double value, PointType type)
        {
            var d = (int)value;
            var tmp = (value - d) * 3600;
            var m = (int)(tmp / 60);
            var st = tmp % 60;
            var s = (int)st;
            var ms = (int)((st - s) * 1000);
            return new DmsMsPoint { Type = type, Degrees = d, Minutes = m, Seconds = s, Milliseconds = ms };
        }

        public static DmsMsLocation CreateFromDouble(GeoPoint decimalLocation)
        {
            if (decimalLocation == null)
            {
                return null!;
            }
            return CreateFromDouble(decimalLocation.Latitude, decimalLocation.Longitude);
        }
    }
}