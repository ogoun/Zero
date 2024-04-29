extern alias CoreDrawing;

using System;
using ZeroLevel.ML.DNN.Models;

namespace ZeroLevel.ML.Services
{
    internal sealed class ImageRegionReader
    {
        private readonly CoreDrawing.System.Drawing.Imaging.BitmapData _bitmapData;
        private readonly RegionInfo _regionInfo;

        public int TensorIndex => _regionInfo.Index;
        public int X => _regionInfo.X;
        public int Y => _regionInfo.Y;

        public ImageRegionReader(CoreDrawing.System.Drawing.Imaging.BitmapData bitmapData, RegionInfo regionInfo)
        {
            _bitmapData = bitmapData;
            _regionInfo = regionInfo;
        }

        /// <summary>
        /// dx, dy, r, g, b
        /// </summary>
        /// <param name="handler"></param>
        public void Read(Action<int, int, int, int, int> handler)
        {
            throw new NotImplementedException();
            unsafe
            {
                var cropSizeX = _regionInfo.CropSizeX;
                var cropSizeY = _regionInfo.CropSizeY;
                var right = _regionInfo.X + _regionInfo.CropSizeX;
                if (right > _bitmapData.Width)
                {
                    cropSizeX = _bitmapData.Width - _regionInfo.X;
                }
                var bottom = _regionInfo.Y + _regionInfo.CropSizeY;
                if (bottom > _bitmapData.Height)
                {
                    cropSizeY = _bitmapData.Height - _regionInfo.Y;
                }
                byte* scan0 = (byte*)_bitmapData.Scan0.ToPointer();
                for (var dy = 0; dy < cropSizeY; dy++)
                {
                    byte* row = scan0 + (_regionInfo.Y + dy + 0) * _regionInfo.Stride;
                    for (var dx = 0; dx < cropSizeX; dx++)
                    {
                        int bIndex = (dx + _regionInfo.X) * _regionInfo.BytesPerPixel;
                        int gIndex = bIndex + 1;
                        int rIndex = bIndex + 2;
                        int b = row[rIndex];
                        int g = row[gIndex];
                        int r = row[bIndex];
                        handler.Invoke(dx, dy, r, g, b);
                    }
                }
            }
        }


        public void FillTensor(TensorPoolItem tensor)
        {
            
            unsafe
            {
                var cropSizeX = _regionInfo.CropSizeX;
                var cropSizeY = _regionInfo.CropSizeY;
                var right = _regionInfo.X + _regionInfo.CropSizeX;
                if (right > _bitmapData.Width)
                {
                    cropSizeX = _bitmapData.Width - _regionInfo.X;
                }
                var bottom = _regionInfo.Y + _regionInfo.CropSizeY;
                if (bottom > _bitmapData.Height)
                {
                    cropSizeY = _bitmapData.Height - _regionInfo.Y;
                }
                byte* scan0 = (byte*)_bitmapData.Scan0.ToPointer();

                var x_l = ((int)Math.Ceiling((double)(cropSizeX / 8))) * 8;
                var y_l = ((int)Math.Ceiling((double)(cropSizeY / 8))) * 8;
                int idx;

                for (var dy = 0; dy < cropSizeY; dy++)
                {
                    byte* row = scan0 + (_regionInfo.Y + dy + 0) * _regionInfo.Stride;

                    for (var dx = 0; dx < x_l; dx += 8)
                    {
                        idx = (0 + dx + _regionInfo.X) * _regionInfo.BytesPerPixel;
                        tensor.Tensor[tensor.TensorIndex, 0, dx + 0, dy] = row[idx + 0];
                        tensor.Tensor[tensor.TensorIndex, 1, dx + 0, dy] = row[idx + 1];
                        tensor.Tensor[tensor.TensorIndex, 2, dx + 0, dy] = row[idx + 2];

                        idx = (1 + dx + _regionInfo.X) * _regionInfo.BytesPerPixel;
                        tensor.Tensor[tensor.TensorIndex, 0, dx + 1, dy] = row[idx + 0];
                        tensor.Tensor[tensor.TensorIndex, 1, dx + 1, dy] = row[idx + 1];
                        tensor.Tensor[tensor.TensorIndex, 2, dx + 1, dy] = row[idx + 2];

                        idx = (2 + dx + _regionInfo.X) * _regionInfo.BytesPerPixel;
                        tensor.Tensor[tensor.TensorIndex, 0, dx + 2, dy] = row[idx + 0];
                        tensor.Tensor[tensor.TensorIndex, 1, dx + 2, dy] = row[idx + 1];
                        tensor.Tensor[tensor.TensorIndex, 2, dx + 2, dy] = row[idx + 2];

                        idx = (3 + dx + _regionInfo.X) * _regionInfo.BytesPerPixel;
                        tensor.Tensor[tensor.TensorIndex, 0, dx + 3, dy] = row[idx + 0];
                        tensor.Tensor[tensor.TensorIndex, 1, dx + 3, dy] = row[idx + 1];
                        tensor.Tensor[tensor.TensorIndex, 2, dx + 3, dy] = row[idx + 2];

                        idx = (4 + dx + _regionInfo.X) * _regionInfo.BytesPerPixel;
                        tensor.Tensor[tensor.TensorIndex, 0, dx + 4, dy] = row[idx + 0];
                        tensor.Tensor[tensor.TensorIndex, 1, dx + 4, dy] = row[idx + 1];
                        tensor.Tensor[tensor.TensorIndex, 2, dx + 4, dy] = row[idx + 2];

                        idx = (5 + dx + _regionInfo.X) * _regionInfo.BytesPerPixel;
                        tensor.Tensor[tensor.TensorIndex, 0, dx + 5, dy] = row[idx + 0];
                        tensor.Tensor[tensor.TensorIndex, 1, dx + 5, dy] = row[idx + 1];
                        tensor.Tensor[tensor.TensorIndex, 2, dx + 5, dy] = row[idx + 2];

                        idx = (6 + dx + _regionInfo.X) * _regionInfo.BytesPerPixel;
                        tensor.Tensor[tensor.TensorIndex, 0, dx + 6, dy] = row[idx + 0];
                        tensor.Tensor[tensor.TensorIndex, 1, dx + 6, dy] = row[idx + 1];
                        tensor.Tensor[tensor.TensorIndex, 2, dx + 6, dy] = row[idx + 2];

                        idx = (7 + dx + _regionInfo.X) * _regionInfo.BytesPerPixel;
                        tensor.Tensor[tensor.TensorIndex, 0, dx + 7, dy] = row[idx + 0];
                        tensor.Tensor[tensor.TensorIndex, 1, dx + 7, dy] = row[idx + 1];
                        tensor.Tensor[tensor.TensorIndex, 2, dx + 7, dy] = row[idx + 2];
                    }

                    if (x_l < cropSizeX)
                    {
                        for(var dx = x_l; dx <= cropSizeX; dx++) 
                        {
                            idx = (dx + _regionInfo.X) * _regionInfo.BytesPerPixel;
                            tensor.Tensor[tensor.TensorIndex, 0, dx, dy] = row[idx + 0];
                            tensor.Tensor[tensor.TensorIndex, 1, dx, dy] = row[idx + 1];
                            tensor.Tensor[tensor.TensorIndex, 2, dx, dy] = row[idx + 2];
                        }
                    }
                }
            }
        }
    }
}
