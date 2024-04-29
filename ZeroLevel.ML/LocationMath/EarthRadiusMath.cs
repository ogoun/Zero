using System;
using ZeroLevel.ML.Models;

namespace ZeroLevel.ML.LocationMath
{
    public static class EarthRadiusMath
    {
        private const double a = 6378137.0d;
        private const double b = 6356752.3142d;

        private const double a2 = a * a;
        private const double b2 = b * b;

        public static double CalculateEarthRadius(GeoPoint location) => CalculateEarthRadius(location.Longitude);

        public static double CalculateEarthRadius(double longitudeDegrees)
        {
            var radLongitude = GeoMath.toRadians(longitudeDegrees);
            var sinB = Math.Sin(radLongitude);
            var cosB = Math.Cos(radLongitude);
            var top = a2 * cosB * a2 * cosB + b2 * sinB * b2 * sinB;
            var bottom = a * cosB * a * cosB + b * sinB * b * sinB;
            var R = Math.Sqrt(top / bottom);
            return R;
        }
    }
}




