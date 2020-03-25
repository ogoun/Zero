namespace ZeroLevel.Services.Semantic.Helpers
{
    public static class TextDistance
    {
        private static int MinOf3(int a, int b, int c)
        {
            if (a < b)
            {
                if (b < c) return a;
                if (c < a) return c;
                else return a;
            }
            if (c < b) return c;
            else return b;
        }

        /// <summary>
        /// Computes the Levenshtein distance between two strings.
        /// </summary>
        /// <param name="s1">The first <see cref="string"/>.</param>
        /// <param name="s2">The second <see cref="string"/>.</param>
        /// <returns>The edit distiance between the given <see cref="string"/> objets.</returns>
        public static int LevenshteinDistance(string s1, string s2)
        {
            // Null or empty checks
            if (string.IsNullOrEmpty(s1))
            {
                if (string.IsNullOrEmpty(s2))
                    return 0;
                else
                    return s2.Length;
            }
            if (string.IsNullOrEmpty(s2)) return s1.Length;

            // Faster access
            int s1Length = s1.Length;
            int s2Length = s2.Length;

            // Create two rows for computation. We don't need reconstruction so a full matrix isn't needed
            var rows = new int[2][];
            rows[0] = new int[s2Length + 1];
            rows[1] = new int[s2Length + 1];

            // Initialize first row
            for (int i = 0; i <= s2Length; i++)
                rows[0][i] = i;
            // Row for computation
            int curRow = 1;
            for (int i = 0; i < s1Length; i++)
            {
                // Calculate first index in current row for computation
                rows[curRow][0] = i + 1;
                int prevRow = curRow ^ 1;
                // Calculate rest of the row
                for (int j = 1; j <= s2Length; j++)
                {
                    int cost = s1[i] == s2[j - 1] ? 0 : 1;
                    rows[curRow][j] = MinOf3(
                                    rows[prevRow][j] + 1, // deletion
                                    rows[curRow][j - 1] + 1, // insertion
                                    rows[prevRow][j - 1] + cost); // substitution
                }
                // Change row for computation to the next.
                curRow = i & 1;
            }
            return rows[curRow ^ 1][s2Length];
        }

        /// <summary>
        /// Computes the Damerau-Levenshtein distance between two strings.
        /// </summary>
        /// <param name="s1">The first <see cref="string"/>.</param>
        /// <param name="s2">The second <see cref="string"/>.</param>
        /// <returns>The edit distiance between the given <see cref="string"/> objets.</returns>
        public static int DamerauLevenshteinDistance(string s1, string s2)
        {
            // Null or empty checks
            if (string.IsNullOrEmpty(s1))
            {
                if (string.IsNullOrEmpty(s2))
                    return 0;
                else
                    return s2.Length;
            }
            if (string.IsNullOrEmpty(s2)) return s1.Length;

            // Faster access
            int s1Length = s1.Length;
            int s2Length = s2.Length;

            // Create three rows for computation. We don't need reconstruction so a full matrix isn't needed
            var rows = new int[3][];
            rows[0] = new int[s2Length + 1];
            rows[1] = new int[s2Length + 1];
            rows[2] = new int[s2Length + 1];

            // Initialize first row
            for (int i = 0; i <= s2Length; i++)
                rows[0][i] = i;

            // Define rows
            int transRow = -1;
            int prevRow = 0;
            int curRow = 1;

            for (int i = 1; i <= s1Length; i++)
            {
                // Calculate first index in current row for computation
                rows[curRow][0] = i;

                // Calculate rest of the row
                for (int j = 1; j <= s2Length; j++)
                {
                    int cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    rows[curRow][j] = MinOf3(
                                    rows[prevRow][j] + 1, // deletion
                                    rows[curRow][j - 1] + 1, // insertion
                                    rows[prevRow][j - 1] + cost); // substitution

                    if (i > 1 && j > 1 && s1[i - 1] == s2[j - 2] && s1[i - 2] == s2[j - 1])
                    {
                        // Transposition
                        int curVal = rows[curRow][j];
                        int transVal = rows[transRow][j - 2] + cost;
                        rows[curRow][j] = curVal < transVal ? curVal : transVal;
                    }
                }

                // Update rows
                switch (curRow)
                {
                    case 0:
                        curRow = 1;
                        prevRow = 0;
                        transRow = 2;
                        break;
                    case 1:
                        curRow = 2;
                        prevRow = 1;
                        transRow = 0;
                        break;
                    case 2:
                        curRow = 0;
                        prevRow = 2;
                        transRow = 1;
                        break;
                    default:
                        break;
                }
            }

            return rows[prevRow][s2Length];
        }
    }
}
