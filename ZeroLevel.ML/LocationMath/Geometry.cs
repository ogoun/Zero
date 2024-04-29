using System;
using ZeroLevel.ML.Models;

namespace ZeroLevel.ML.LocationMath
{
    public static class Geometry
    {
        public static Point2D GeoPointCoordinateInArea(GeoPoint point, GeoPoint[] corners)
        {
            var distanceToTLTR = Math.Abs(point.crossTrackDistanceTo(corners[0], corners[1]));
            var distanceToTLBL = Math.Abs(point.crossTrackDistanceTo(corners[0], corners[3]));

            var distanceTLTR = Math.Abs(corners[0].DistanceToPoint(corners[1]));
            var distanceTLBL = Math.Abs(corners[0].DistanceToPoint(corners[3]));

            return new Point2D(distanceToTLBL / distanceTLTR, distanceToTLTR / distanceTLBL);
        }


        public static bool IsGeoPointInArea(GeoPoint point, GeoPoint[] corners)
        {
            var toTop = point.crossTrackDistanceTo(corners[0], corners[1]);
            var toBottom = point.crossTrackDistanceTo(corners[3], corners[1]);

            if (toTop < 0 && toBottom < 0) return false;
            if (toTop > 0 && toBottom > 0) return false;


            var toLeft = point.crossTrackDistanceTo(corners[3], corners[0]);
            var toRight = point.crossTrackDistanceTo(corners[2], corners[1]);

            if (toLeft < 0 && toRight < 0) return false;
            if (toLeft > 0 && toRight > 0) return false;

            return true;
        }

        public static bool IsPointInPolygon(GeoPoint[] poly, GeoPoint point)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if ((((poly[i].Latitude <= point.Latitude) && (point.Latitude < poly[j].Latitude))
                        || ((poly[j].Latitude <= point.Latitude) && (point.Latitude < poly[i].Latitude)))
                        && (point.Longitude < (poly[j].Longitude - poly[i].Longitude) * (point.Latitude - poly[i].Latitude)
                            / (poly[j].Latitude - poly[i].Latitude) + poly[i].Longitude))
                {
                    c = !c;
                }
            }
            return c;
        }
    }
}
