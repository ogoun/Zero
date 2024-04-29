extern alias CoreDrawing;

using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroLevel.ML.Services
{
    internal sealed class ImageScanner
            : IDisposable
    {
        private readonly CoreDrawing.System.Drawing.Bitmap _image;
        private readonly CoreDrawing.System.Drawing.Imaging.BitmapData _bitmapData;
        private readonly int _stride;
        private readonly int _bytesPerPixel;
        private readonly int _cropSizeX;
        private readonly int _cropSizeY;
        private readonly int _total;
        private readonly int[] _x_points;
        private readonly int[] _y_points;

        public int CropSizeX => _cropSizeX;
        public int CropSizeY => _cropSizeY;
        public int TotalRegions => _total;
        public ImageScanner(CoreDrawing.System.Drawing.Bitmap image, int cropSize)
        {
            _image = image;
            _bitmapData = _image.LockBits(new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                CoreDrawing.System.Drawing.Imaging.ImageLockMode.ReadOnly,
                CoreDrawing.System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            _stride = Math.Abs(_bitmapData.Stride);
            _bytesPerPixel = _stride / image.Width;
            _cropSizeX = cropSize > 0 ? cropSize : _image.Width;
            _cropSizeY = cropSize > 0 ? cropSize : _image.Height;
            _x_points = SplitRange(_image.Width, _cropSizeX, 0).ToArray();
            _y_points = SplitRange(_image.Height, _cropSizeY, 0).ToArray();
            _total = _x_points.Length * _y_points.Length;
        }

        public IEnumerable<ImageRegionReader> ScanByRegions()
        {
            var tensorIndex = 0;
            foreach (var x in _x_points)
            {
                foreach (var y in _y_points)
                {
                    var region = new RegionInfo { Index = tensorIndex, X = x, Y = y, CropSizeX = _cropSizeX, CropSizeY = _cropSizeY, Stride = _stride, BytesPerPixel = _bytesPerPixel };
                    yield return new ImageRegionReader(_bitmapData, region);
                    tensorIndex++;
                }
            }
        }

        private IEnumerable<int> SplitRange(int size, int cropSize, float overlapProportion)
        {
            var stride = (int)(cropSize * (1f - overlapProportion));
            var counter = 0;
            while (true)
            {
                var pt = stride * counter;
                if (pt + cropSize > size)
                {
                    if (cropSize == size || pt == size)
                    {
                        break;
                    }
                    yield return size - cropSize;
                    break;
                }
                else
                {
                    yield return pt;
                }
                counter++;
            }
        }

        public void Dispose()
        {
            _image.UnlockBits(_bitmapData);
            _image.Dispose();
        }
    }
}
