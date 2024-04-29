using System;
using System.Runtime.CompilerServices;

namespace ZeroLevel.ML.LocationMath
{
    public static class GeoMath
    {
        /// <summary>
        /// Conversion factor degrees to radians
        /// </summary>
        public const double DegToRad = Math.PI / 180d; //0.01745329252; // Convert Degrees to Radians


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double toRadians(double v) => v * DegToRad;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double toDegree(double v) => v * 180 / Math.PI;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double wrap90(double degrees)
        {
            if (-90 <= degrees && degrees <= 90) return degrees; // avoid rounding due to arithmetic ops if within range

            // latitude wrapping requires a triangle wave function; a general triangle wave is
            //     f(x) = 4a/p ⋅ | (x-p/4)%p - p/2 | - a
            // where a = amplitude, p = period, % = modulo; however, JavaScript '%' is a remainder operator
            // not a modulo operator - for modulo, replace 'x%n' with '((x%n)+n)%n'
            double x = degrees, a = 90, p = 360;
            return 4 * a / p * Math.Abs(((x - p / 4) % p + p) % p - p / 2) - a;
        }
        /// <summary>
        /// Constrain degrees to range -180..+180 (for longitude); e.g. -181 => 179, 181 => -179.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double wrap180(double degrees)
        {
            if (-180 <= degrees && degrees <= 180) return degrees; // avoid rounding due to arithmetic ops if within range

            // longitude wrapping requires a sawtooth wave function; a general sawtooth wave is
            //     f(x) = (2ax/p - p/2) % p - a
            // where a = amplitude, p = period, % = modulo; however, JavaScript '%' is a remainder operator
            // not a modulo operator - for modulo, replace 'x%n' with '((x%n)+n)%n'
            double x = degrees, a = 180, p = 360;
            return ((2 * a * x / p - p / 2) % p + p) % p - a;
        }
        /// <summary>
        /// Constrain degrees to range 0..360 (for bearings); e.g. -1 => 359, 361 => 1.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double wrap360(double degrees)
        {
            if (0 <= degrees && degrees < 360) return degrees; // avoid rounding due to arithmetic ops if within range

            // bearing wrapping requires a sawtooth wave function with a vertical offset equal to the
            // amplitude and a corresponding phase shift; this changes the general sawtooth wave function from
            //     f(x) = (2ax/p - p/2) % p - a
            // to
            //     f(x) = (2ax/p) % p
            // where a = amplitude, p = period, % = modulo; however, JavaScript '%' is a remainder operator
            // not a modulo operator - for modulo, replace 'x%n' with '((x%n)+n)%n'
            double x = degrees, a = 180, p = 360;
            return (2 * a * x / p % p + p) % p;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double YawToBearing(double a)
        {
            return (a + 360) % 360;
        }

        /// <summary>
        /// Calculate the difference between two longitudal values constrained 0 - 180 deg
        /// </summary>
        /// <param name="lon1">The first longitue value in degrees</param>
        /// <param name="lon2">The second longitue value in degrees</param>
        /// <returns>The distance in degrees</returns>
        public static double DiffLongitude(double lon1, double lon2)
        {
            double diff;

            if (lon1 > 180.0)
                lon1 = 360.0 - lon1;
            if (lon2 > 180.0)
                lon2 = 360.0 - lon2;

            if (lon1 >= 0.0 && lon2 >= 0.0)
                diff = lon2 - lon1;
            else if (lon1 < 0.0 && lon2 < 0.0)
                diff = lon2 - lon1;
            else
            {
                // different hemispheres
                if (lon1 < 0)
                    lon1 = -1 * lon1;
                if (lon2 < 0)
                    lon2 = -1 * lon2;
                diff = lon1 + lon2;
                if (diff > 180.0)
                    diff = 360.0 - diff;
            }
            return diff;
        }
    }
}
