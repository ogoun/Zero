using System;
using ZeroLevel.ML.Models;

namespace ZeroLevel.ML.LocationMath
{
    public static class ViewMath
    {
        public static GeoPoint[] CalculateCornerGeopoints(CameraMath camera, GeoPoint center, double flightYaw)
        {
            // True North direction
            var bearing = GeoMath.YawToBearing(flightYaw);

            // Distances in meters
            var imageWidthInMeters = camera.CaclulateImageWidth(center.Altditude);
            var imageHeightInMeters = camera.CaclulateImageHeight(center.Altditude);
            var diagonalInMeters = Math.Sqrt(imageWidthInMeters * imageWidthInMeters + imageHeightInMeters * imageHeightInMeters);
            var distanceInMeters = diagonalInMeters * 0.5d;

            // Directions to corners
            var topLeftDirection = GeoMath.YawToBearing(bearing - camera.VerticalToDiagonalAngleOffset);
            var topRightDirection = GeoMath.YawToBearing(bearing + camera.VerticalToDiagonalAngleOffset);
            var bottomLeftDirection = GeoMath.YawToBearing(bearing + 180d + camera.VerticalToDiagonalAngleOffset);
            var bottomRightDirection = GeoMath.YawToBearing(bearing + 180d - camera.VerticalToDiagonalAngleOffset);

            // Corners locations
            var topLeft = center.CalculateDestinationPoint(distanceInMeters, topLeftDirection);
            var topRight = center.CalculateDestinationPoint(distanceInMeters, topRightDirection);
            var bottomRight = center.CalculateDestinationPoint(distanceInMeters, bottomRightDirection);
            var bottomLeft = center.CalculateDestinationPoint(distanceInMeters, bottomLeftDirection);

            return new[] { topLeft, topRight, bottomRight, bottomLeft };
        }

        public static GeoPoint CalculateBoxGeopoint(CameraMath camera, GeoPoint center, double flightYaw, Point2D point)
        {
            var dx = Math.Abs(camera.ImageWidth * 0.5 - point.x);
            var dy = Math.Abs(camera.ImageHeight * 0.5 - point.y);
            // Если точка находится вблизи центра снимка, нет смысла считать смещение
            if (dx < 10f && dy < 10f)
            {
                return center;
            }

            // Переход в 0; 0
            var xn = point.x - camera.ImageWidth * 0.5;
            if (Math.Abs(xn) < double.Epsilon)
            {
                xn = 0.000001;
            }
            var yn = camera.ImageHeight * 0.5 - point.y;
            var distanceToPointInPixels = Math.Sqrt(dx * dx + dy * dy);

            // Точка на окружности вертикально вверх
            var x0 = 0;
            var y0 = distanceToPointInPixels;

            var angle = GetAngle(x0, y0, xn, yn);
            var bearing = GeoMath.YawToBearing(flightYaw);
            var bearingToPoint = GeoMath.YawToBearing(bearing + angle);

            // рассчитать расстояние и пеленг бокса относительно центральной точки
            var distanceInMeters = camera.PixToMeters(center.Altditude, distanceToPointInPixels);

            return center.CalculateDestinationPoint(distanceInMeters, bearingToPoint);
        }

        /// <summary>
        /// Угол между двумя отрезками, с общей точкой в 0;0
        /// </summary>
        private static double GetAngle(double x0, double y0, double x2, double y2)
        {
            var x1 = 0;
            var y1 = 0;
            var a1 = -(Math.Atan2(y0 - y1, x0 - x1) * 180 / Math.PI - 90);
            var a2 = -(Math.Atan2(y2 - y1, x2 - x1) * 180 / Math.PI - 90);
            return (a2 - a1);
        }
    }
}
