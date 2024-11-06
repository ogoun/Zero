using System;

namespace ZeroLevel.Services.Utils
{
    public static class Utility
    {
        /// <summary>
        /// Parse size in string notation into long.
        /// Examples: 4k, 4K, 4KB, 4 KB, 8m, 8MB, 12g, 12 GB, 16t, 16 TB, 32p, 32 PB.
        /// </summary>
        /// <param name="value">String version of number</param>
        /// <returns>The number</returns>
        public static long ParseSize(string value)
        {
            char[] suffix = ['k', 'm', 'g', 't', 'p'];
            long result = 0;
            foreach (char c in value)
            {
                if (char.IsDigit(c))
                {
                    result = result * 10 + (byte)c - '0';
                }
                else
                {
                    for (int i = 0; i < suffix.Length; i++)
                    {
                        if (char.ToLower(c) == suffix[i])
                        {
                            result *= (long)Math.Pow(1024, i + 1);
                            return result;
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Pretty print value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static string PrettySize(long value)
        {
            char[] suffix = ['K', 'M', 'G', 'T', 'P'];
            double v = value;
            int exp = 0;
            while (v - Math.Floor(v) > 0)
            {
                if (exp >= 18)
                    break;
                exp += 3;
                v *= 1024;
                v = Math.Round(v, 12);
            }

            while (Math.Floor(v).ToString().Length > 3)
            {
                if (exp <= -18)
                    break;
                exp -= 3;
                v /= 1024;
                v = Math.Round(v, 12);
            }
            if (exp > 0)
                return v.ToString() + suffix[exp / 3 - 1] + "B";
            else if (exp < 0)
                return v.ToString() + suffix[-exp / 3 - 1] + "B";
            return v.ToString() + "B";
        }
    }
}
