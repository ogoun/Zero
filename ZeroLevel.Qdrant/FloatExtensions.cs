using System.Globalization;

namespace ZeroLevel.Qdrant
{
    public static class NumericExtensions
    {
        private static NumberFormatInfo nfi = new NumberFormatInfo() { NumberDecimalSeparator = "." };
        public static string ConvertToString(this float num)
        {
            return num.ToString(nfi);
        }

        public static string ConvertToString(this double num)
        {
            return num.ToString(nfi);
        }
    }
}
