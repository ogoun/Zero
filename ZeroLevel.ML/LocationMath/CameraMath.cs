using System;
using System.Runtime.CompilerServices;

namespace ZeroLevel.ML.LocationMath
{
    public sealed class CameraMath
    {
        public string Id { get; set; }

        public readonly double ImageWidth;
        public readonly double ImageHeight;
        public readonly double FocalLength;
        public readonly double PixelSize;
        /*
        public readonly double AOVhorizontal;
        public readonly double AOVvertical;
        public readonly double SensorWidth;
        public readonly double SensorHeight;
        */
        internal readonly double VerticalToDiagonalAngleOffset;

        private static bool useCustomAltitude = false;
        private static double customAltitude;
        public static void UseCustomAltitude(double altitude)
        {
            useCustomAltitude = true;
            customAltitude = altitude;
        }

        public double Altitude(double altitude) => useCustomAltitude ? customAltitude : altitude;

        public CameraMath(double pixelSize, double focalLength, double imageWidth, double imageHeight)
        {
            PixelSize = pixelSize;
            FocalLength = focalLength;

            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            /*
            SensorWidth = pixelSize * 0.001 * imageWidth;
            SensorHeight = pixelSize * 0.001 * imageHeight;

            AOVhorizontal = 2d * Math.Atan(SensorWidth / (2d * focalLength));
            AOVvertical = 2d * Math.Atan(SensorHeight / (2d * focalLength));
            */
            VerticalToDiagonalAngleOffset = Math.Atan((ImageWidth * .5) / (ImageHeight * 0.5)) * 180d / Math.PI;
        }
        /*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CaclulateImageWidth(double altitude) => 2d * Altitude(altitude) * Math.Tan(0.5d * AOVhorizontal);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CaclulateImageHeight(double altitude) => 2d * Altitude(altitude) * Math.Tan(0.5d * AOVvertical);

        public double WidthPixelToMeters(double pixels, double altitude)
        {
            var Kw = pixels / ImageWidth;
            var Kh = pixels / ImageHeight;

            var fieldWidth = CaclulateImageWidth(Altitude(altitude));
            var fieldHeight = CaclulateImageHeight(Altitude(altitude));

            var w = fieldWidth * Kw;
            var h = fieldHeight * Kh;

            var dist = Math.Sqrt(w * w + h * h);
            return dist;
        }

        public double HeightPixelToMeters(double pixels, double altitude)
        {
            var Kw = pixels / ImageWidth;
            var Kh = pixels / ImageHeight;

            var fieldWidth = CaclulateImageWidth(Altitude(altitude));
            var fieldHeight = CaclulateImageHeight(Altitude(altitude));

            var w = fieldWidth * Kw;
            var h = fieldHeight * Kh;

            var dist = Math.Sqrt(w * w + h * h);
            return dist;
        }
        */
        // H = h * (D - f) / f
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double PixToMillimeters(double altitude, double pixels) => (altitude * 1000d - FocalLength) * pixels * PixelSize / (1000d * FocalLength);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double PixToMeters(double altitude, double pixels) => PixToMillimeters(altitude, pixels) / 1000;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CaclulateImageWidth(double altitude) => PixToMeters(altitude, ImageWidth);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CaclulateImageHeight(double altitude) => PixToMeters(altitude, ImageHeight);
    }
}
