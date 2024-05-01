using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Xmp;
using System;
using System.IO;
using System.Linq;
using System.Text;
using XmpCore;
using ZeroLevel.ML.LocationMath;

namespace ZeroLevel.ML
{
    public sealed class DewarpData
    {
        /*
For the parameter values conversion I used the following formulas (from Luhmann et al., 2019):
k1 pixel units = k1 focal units / focal length^2
k2 pixel units = k2 focal units / focal length^4
k3 pixel units = k3 focal units / focal length^6
p1 pixel units = p1 focal units / focal length
p2 pixel units = -p2 focal units / focal length
         */

        public string Date = null!;
        /// <summary>
        /// Focal length
        /// </summary>
        public float Fx;
        /// <summary>
        /// Focal length
        /// </summary>
        public float Fy;
        /// <summary>
        /// Principal points
        /// </summary>
        public float Cx;
        /// <summary>
        /// Principal points
        /// </summary>
        public float Cy;
        /// <summary>
        /// Lens distortion
        /// </summary>
        public float K1;
        /// <summary>
        /// Lens distortion
        /// </summary>
        public float K2;
        /// <summary>
        /// Lens distortion
        /// </summary>
        public float P1;
        /// <summary>
        /// Lens distortion
        /// </summary>
        public float P2;
        /// <summary>
        /// Lens distortion
        /// </summary>
        public float K3;

        public DewarpData(string data)
        {
            if (!string.IsNullOrWhiteSpace(data))
            {
                var parts = data.Split(';');
                if (parts.Length == 2)
                {
                    Date = parts[0];
                    var par_parts = parts[1].Split(",");

                    if (par_parts.Length >= 9)
                    {
                        Fx = ToFloat(par_parts[0]);
                        Fy = ToFloat(par_parts[1]);
                        Cx = ToFloat(par_parts[2]);
                        Cy = ToFloat(par_parts[3]);
                        K1 = ToFloat(par_parts[4]);
                        K2 = ToFloat(par_parts[5]);
                        P1 = ToFloat(par_parts[6]);
                        P2 = ToFloat(par_parts[7]);
                        K3 = ToFloat(par_parts[8]);
                    }
                }
            }
        }

        private static float ToFloat(string s)
        {
            var ns = ImageMetainfo.GetNumber(s);
            if (float.TryParse(ns, out var f))
            {
                return f;
            }
            return 0f;
        }

        public float CalculateDistortedX(float x)
        {
            return x * (1 + K1);
        }

    }

    public sealed class ImageMetainfo
    {
        private CameraMath _camera = null!;
        public CameraMath CreateCamera()
        {
            if (_camera == null)
            {
                _camera = new CameraMath(CameraPixelSizes.GetPixelSizeByModel(this.Model), this.FocalLength, this.ImageWidth, this.ImageHeight);
            }
            return _camera;
        }

        public DateTime Created { get; set; }
        public string Make { get; set; } = null!;
        public string Model { get; set; } = null!;
        public string SerialNumber { get; set; } = null!;

        public double FocalLength { get; set; }
        public double ImageWidth { get; set; }
        public double ImageHeight { get; set; }

        public double GPSLatitude { get; set; }
        public double GPSLongitude { get; set; }
        public double GPSAltitude { get; set; }

        public double DJILatitude { get; set; }
        public double DJILongitude { get; set; }
        public double DJIAltitude { get; set; }

        private double GimbalYawDegree { get; set; }
        private double FlightYawDegree { get; set; }

        private static double Diff(double a, double b)
        {
            var a1 = Math.Abs(a - b);
            var a2 = 360 - Math.Max(a, b) + Math.Min(a, b);
            return Math.Min(a1, a2);
        }

        public double Yaw
        {
            get
            {
                var fn = (FlightYawDegree + 360) % 360;
                var gn = (GimbalYawDegree + 360) % 360;
                var gni = (GimbalYawDegree + 540) % 360;

                var gy = GimbalYawDegree;
                if (gy < 0)
                {
                    gy = 180 + GimbalYawDegree;
                }
                else
                {
                    gy = -180 + GimbalYawDegree;
                }

                var d1 = Diff(fn, gn);
                var d2 = Diff(fn, gni);

                if (d2 < d1)
                {
                    return gy;
                }
                return GimbalYawDegree;
            }
        }

        public double CalibratedFocalLength { get; set; }

        public DewarpData Dewarp { get; set; } = null!;

        public double Latitude => Math.Max(DJILatitude, GPSLatitude);
        public double Longitude => Math.Max(DJILongitude, GPSLongitude);
        public double Altitude => DJIAltitude;

        internal static string GetNumber(string line)
        {
            var ns = new StringBuilder();
            var sep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            bool started = false;
            bool separated = false;
            foreach (var ch in line)
            {
                if (char.IsDigit(ch))
                {
                    started = true;
                    ns.Append(ch);
                }
                else if (ch == '.' || ch == ',' && started)
                {
                    if (separated)
                    {
                        break;
                    }
                    separated = true;
                    ns.Append(sep);
                }
                else if (ch == '-' && !started)
                {
                    started = true;
                    ns.Append(ch);
                }
                else if (started)
                {
                    break;
                }
            }
            return ns.ToString();
        }

        public void ReadMetadata(Stream stream)
        {
            double dBuf;
            try
            {
                var metadata = ImageMetadataReader.ReadMetadata(stream);
                var exif0 = metadata.OfType<ExifIfd0Directory>().FirstOrDefault();
                if (exif0 != null)
                {
                    foreach (var t in exif0.Tags)
                    {
                        if (t.Name.Equals("Make"))
                        {
                            this.Make = t.Description!;
                        }
                        else if (t.Name.Equals("Model"))
                        {
                            this.Model = t.Description!;
                        }
                    }
                }

                var exif = metadata.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (exif != null)
                {
                    foreach (var t in exif.Tags)
                    {
                        if (t.Name.Equals("Focal Length"))
                        {
                            if (double.TryParse(GetNumber(t.Description ?? string.Empty), out dBuf))
                            {
                                this.FocalLength = dBuf;
                            }
                        }
                        else if (t.Name.Equals("Exif Image Width"))
                        {
                            if (double.TryParse(GetNumber(t.Description ?? string.Empty), out dBuf))
                            {
                                this.ImageWidth = dBuf;
                            }
                        }
                        else if (t.Name.Equals("Exif Image Height"))
                        {
                            if (double.TryParse(GetNumber(t.Description ?? string.Empty), out dBuf))
                            {
                                this.ImageHeight = dBuf;
                            }
                        }
                        else if (t.Name.Equals("Body Serial Number"))
                        {
                            this.SerialNumber = t.Description!;
                        }
                        else if (t.Name.Equals("CreateDate"))
                        {
                            this.Created = DateTime.Parse(t.Description ?? string.Empty);
                        }
                    }
                }

                var gps = metadata.OfType<GpsDirectory>().FirstOrDefault();
                if (gps != null)
                {
                    if (gps.TryGetGeoLocation(out var location))
                    {
                        this.GPSLatitude = location.Latitude;
                        this.GPSLongitude = location.Longitude;
                        double alt = 0.0d;
                        if (gps.TryGetDouble(GpsDirectory.TagAltitude, out alt))
                        {
                            this.GPSAltitude = alt;
                        }
                    }
                }


                var xmp = metadata.OfType<XmpDirectory>().FirstOrDefault();
                if (xmp != null)
                {
                    foreach (var p in xmp.XmpMeta?.Properties ?? Enumerable.Empty<IXmpPropertyInfo>())
                    {
                        if (!string.IsNullOrEmpty(p.Path))
                        {
                            if (p.Path.IndexOf("RelativeAltitude") >= 0)
                            {
                                if (double.TryParse(GetNumber(p.Value), out dBuf))
                                {
                                    this.DJIAltitude = dBuf;
                                }
                            }
                            else if (p.Path.IndexOf("GpsLatitude") >= 0)
                            {
                                if (double.TryParse(GetNumber(p.Value), out dBuf))
                                {
                                    this.DJILatitude = dBuf;
                                }
                            }
                            else if (p.Path.IndexOf("GpsLongitude") >= 0)
                            {
                                if (double.TryParse(GetNumber(p.Value), out dBuf))
                                {
                                    this.DJILongitude = dBuf;
                                }
                            }
                            else if (p.Path.IndexOf("GimbalYawDegree") >= 0)
                            {
                                if (double.TryParse(GetNumber(p.Value), out dBuf))
                                {
                                    this.GimbalYawDegree = dBuf;
                                }
                            }
                            else if (p.Path.IndexOf("FlightYawDegree") >= 0)
                            {
                                if (double.TryParse(GetNumber(p.Value), out dBuf))
                                {
                                    this.FlightYawDegree = dBuf;
                                }
                            }
                            else if (p.Path.IndexOf("CalibratedFocalLength") >= 0)
                            {
                                if (double.TryParse(GetNumber(p.Value), out dBuf))
                                {
                                    this.CalibratedFocalLength = dBuf;
                                }
                            }
                            else if (p.Path.IndexOf("DewarpData") >= 0)
                            {
                                this.Dewarp = new DewarpData(p.Value);
                            }
                        }
                    }
                }
            }
            catch
            {
                stream.Close();
                throw;
            }
        }
    }
}
