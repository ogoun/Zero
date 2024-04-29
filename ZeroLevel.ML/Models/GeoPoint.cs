using System;
using ZeroLevel.ML.LocationMath;

namespace ZeroLevel.ML.Models
{
    public sealed class GeoPoint
    {
        /// <summary>
        /// latitude in degrees
        /// </summary>
        public double Latitude { get; private set; }
        /// <summary>
        /// longitude in degrees
        /// </summary>
        public double Longitude { get; private set; }
        /// <summary>
        /// altitude in meters
        /// </summary>
        public double Altditude { get; private set; }

        public GeoPoint(double lat, double lon)
        {
            Latitude = GeoMath.wrap90(lat);
            Longitude = GeoMath.wrap180(lon);
            Altditude = 0;
        }

        public GeoPoint(double lat, double lon, double alt)
        {
            Latitude = GeoMath.wrap90(lat);
            Longitude = GeoMath.wrap180(lon);
            Altditude = alt;
        }

        public double RadLatitude => GeoMath.toRadians(Latitude);
        public double RadLongitude => GeoMath.toRadians(Longitude);

        public GeoPoint CalculateDestinationPoint(double distance, double heading)
        {
            distance /= EarthRadiusMath.CalculateEarthRadius(Longitude);
            heading = GeoMath.toRadians(heading);
            // http://williams.best.vwh.net/avform.htm#LL
            double fromLat = RadLatitude;
            double fromLng = RadLongitude;
            double cosDistance = Math.Cos(distance);
            double sinDistance = Math.Sin(distance);
            double sinFromLat = Math.Sin(fromLat);
            double cosFromLat = Math.Cos(fromLat);
            double sinLat = cosDistance * sinFromLat + sinDistance * cosFromLat * Math.Cos(heading);
            double dLng = Math.Atan2(sinDistance * cosFromLat * Math.Sin(heading), cosDistance - sinFromLat * sinLat);
            return new GeoPoint(GeoMath.toDegree(Math.Asin(sinLat)), GeoMath.toDegree(fromLng + dLng));
        }

        public double DistanceToPoint(GeoPoint target)
        {
            var earthRadius = EarthRadiusMath.CalculateEarthRadius(target.Longitude);
            var dLat = GeoMath.toRadians(target.Latitude - Latitude);
            var dLon = GeoMath.toRadians(target.Longitude - Longitude);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(RadLatitude) * Math.Cos(target.RadLatitude) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
            var d = earthRadius * c;
            return d;
        }

        public GeoPoint ShortDistanceMidpointTo(GeoPoint point)
        {
            var lat = 0.5 * (Latitude + point.Latitude);
            var lon = 0.5 * (Longitude + point.Longitude);
            var alt = 0.5 * (Altditude + point.Altditude);
            return new GeoPoint(lat, lon, alt);
        }

        public GeoPoint MidpointTo(GeoPoint point)
        {
            var φ1 = RadLatitude;
            var λ1 = RadLongitude;
            var φ2 = point.RadLatitude;
            var Δλ = GeoMath.toRadians(GeoMath.DiffLongitude(point.Longitude, Longitude));


            var A = new Point3D(x: Math.Cos(φ1), y: 0, z: Math.Sin(φ1));
            var B = new Point3D(x: Math.Cos(φ2) * Math.Cos(Δλ), y: Math.Cos(φ2) * Math.Sin(Δλ), z: Math.Sin(φ2));
            var C = new Point3D(x: A.x + B.x, y: A.y + B.y, z: A.z + B.z);

            var φm = Math.Atan2(C.z, Math.Sqrt(C.x * C.x + C.y * C.y));
            var λm = λ1 + Math.Atan2(C.y, C.x);

            var alt = 0.5 * (Altditude + point.Altditude);

            return new GeoPoint(GeoMath.toDegree(φm), GeoMath.toDegree(λm), alt);
        }

        public double GetBearing(GeoPoint p2)
        {
            var latitude1 = RadLatitude;
            var latitude2 = p2.RadLatitude;
            var longitudeDifference = GeoMath.toRadians(p2.Longitude - Longitude);
            var y = Math.Sin(longitudeDifference) * Math.Cos(latitude2);
            var x = Math.Cos(latitude1) * Math.Sin(latitude2) - Math.Sin(latitude1) * Math.Cos(latitude2) * Math.Cos(longitudeDifference);
            return (GeoMath.toDegree(Math.Atan2(y, x)) + 360) % 360;
        }

        public double crossTrackDistanceTo(GeoPoint pathStart, GeoPoint pathEnd)
        {
            var R = EarthRadiusMath.CalculateEarthRadius(this);
            var δ13 = pathStart.DistanceToPoint(this) / R;
            var θ13 = GeoMath.toRadians(pathStart.GetBearing(this));
            var θ12 = GeoMath.toRadians(pathStart.GetBearing(pathEnd));
            var δxt = Math.Asin(Math.Sin(δ13) * Math.Sin(θ13 - θ12));
            return δxt * R;
        }
    }
}
