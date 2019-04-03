using System;
using System.Text;

namespace ZeroLevel
{
    public static class StringExtensions
    {
        public static bool EndsWith(this StringBuilder sb, string test)
        {
            if (sb == null || sb.Length < test.Length)
                return false;

            string end = sb.ToString(sb.Length - test.Length, test.Length);
            return end.Equals(test);
        }

        public static bool EndsWith(this StringBuilder sb, string test,
            StringComparison comparison)
        {
            if (sb == null || sb.Length < test.Length)
                return false;
            string end = sb.ToString(sb.Length - test.Length, test.Length);
            return end.Equals(test, comparison);
        }
    }
}