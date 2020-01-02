using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace ZeroLevel.WPF
{
    public static class BitmapSourceHelper
    {
        public static BitmapSource LoadBitmap(Bitmap bmp)
        {
            if (ImageFormat.Jpeg.Equals(bmp.RawFormat))
            {
                return LoadBitmap(bmp, ImageFormat.Jpeg);
            }
            else if (ImageFormat.Png.Equals(bmp.RawFormat))
            {
                return LoadBitmap(bmp, ImageFormat.Png);
            }
            else if (ImageFormat.Gif.Equals(bmp.RawFormat))
            {
                return LoadBitmap(bmp, ImageFormat.Gif);
            }
            else if (ImageFormat.Bmp.Equals(bmp.RawFormat) || ImageFormat.MemoryBmp.Equals(bmp.RawFormat))
            {
                return LoadBitmap(bmp, ImageFormat.Bmp);
            }
            else if (ImageFormat.Emf.Equals(bmp.RawFormat))
            {
                return LoadBitmap(bmp, ImageFormat.Emf);
            }
            else if (ImageFormat.Exif.Equals(bmp.RawFormat))
            {
                return LoadBitmap(bmp, ImageFormat.Exif);
            }
            else if (ImageFormat.Icon.Equals(bmp.RawFormat))
            {
                return LoadBitmap(bmp, ImageFormat.Icon);
            }
            else if (ImageFormat.Tiff.Equals(bmp.RawFormat))
            {
                return LoadBitmap(bmp, ImageFormat.Tiff);
            }
            else if (ImageFormat.Wmf.Equals(bmp.RawFormat))
            {
                return LoadBitmap(bmp, ImageFormat.Wmf);
            }
            return LoadBitmap(bmp, ImageFormat.Bmp);
        }

        public static BitmapSource LoadBitmap(Bitmap bmp, ImageFormat format)
        {
            var bi = new BitmapImage();
            using (var ms = new System.IO.MemoryStream())
            {
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bmp.Save(ms, format);
                bi.EndInit();
                bi.Freeze();
            }
            return bi;
        }
    }
}
