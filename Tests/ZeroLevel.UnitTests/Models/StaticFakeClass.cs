using System;

namespace ZeroInvokingTest.Models
{
    public static class StaticFakeClass
    {
        public static string GetString(string line) => line;
        internal static int GetNumber(int number) => number;
        private static DateTime GetDateTime(DateTime date) => date;
    }
}
