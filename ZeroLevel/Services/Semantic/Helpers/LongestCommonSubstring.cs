namespace ZeroLevel.Semantic
{
    public static class LongestCommonSubstring
    {
        public static string LCS(string s1, string s2)
        {
            if (s1 == null || s2 == null || s1.Length == 0 || s2.Length == 0) return string.Empty;
            var a = new int[s1.Length + 1, s2.Length + 1];
            int u = 0, v = 0;

            for (var i = 0; i < s1.Length; i++)
                for (var j = 0; j < s2.Length; j++)
                    if (s1[i] == s2[j])
                    {
                        a[i + 1, j + 1] = a[i, j] + 1;
                        if (a[i + 1, j + 1] > a[u, v])
                        {
                            u = i + 1;
                            v = j + 1;
                        }
                    }

            return s1.Substring(u - a[u, v], a[u, v]);
        }

        public static string LCSIgnoreCase(string s1, string s2)
        {
            if (s1 == null || s2 == null || s1.Length == 0 || s2.Length == 0) return string.Empty;
            var a = new int[s1.Length + 1, s2.Length + 1];
            int u = 0, v = 0;

            for (var i = 0; i < s1.Length; i++)
                for (var j = 0; j < s2.Length; j++)
                    if (char.ToUpperInvariant(s1[i]) == char.ToUpperInvariant(s2[j]))
                    {
                        a[i + 1, j + 1] = a[i, j] + 1;
                        if (a[i + 1, j + 1] > a[u, v])
                        {
                            u = i + 1;
                            v = j + 1;
                        }
                    }

            return s1.Substring(u - a[u, v], a[u, v]);
        }
    }
}
