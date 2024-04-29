using System.Collections.Generic;

namespace ZeroLevel.ML
{
    public static class CameraPixelSizes
    {
        /// <summary>
        ///  В микрометрах 
        /// </summary>
        private static Dictionary<string, double> _pixels = new Dictionary<string, double>
        {
            { "ZenmuseP1",  4.4d  },
            { "M3E",        3.35821d  },
            { "L1D-20c",    2.41d },
            { "F230",       1.55d },
            { "FC3411",     2.4d  },
            { "XT702",      2.4d  },
            { "FC7303",     1.334d},
        };

        public static double GetPixelSizeByModel(string model)
        {
            if (_pixels.ContainsKey(model))
            {
                return _pixels[model];
            }
            return 3.3d;
        }
    }
}
