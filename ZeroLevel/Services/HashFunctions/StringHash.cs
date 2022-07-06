namespace ZeroLevel.Services.HashFunctions
{
    /// <summary>
    /// In .net core string.GetHashCode not deterministic more
    /// https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/#a-deterministic-gethashcode-implementation
    /// </summary>
    public static class StringHash
    {
        public static int DotNetFullHash(string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;
                if (str != null)
                {
                    for (int i = 0; i < str.Length; i += 2)
                    {
                        hash1 = ((hash1 << 5) + hash1) ^ str[i];
                        if (i == str.Length - 1)
                            break;
                        hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                    }
                }
                return (hash1 + (hash2 * 1566083941)) & 0x7FFFFFFF;
            }
        }

        const long seed = 57;
        public static long CustomHash(string str)
        {
            long result = 1;
            foreach (var ch in str)
            {
                result = (seed * result + (int)ch) & 0xFFFFFFFF;
            }
            return result & 0x7FFFFFFF;
        }

        public static int CustomHash2(string s)
        {
            int hash = 0;
            for (int i = 0; i < s.Length; i++)
            {
                hash += s[i];
                hash += (hash << 10);
                hash ^= (hash >> 6);
            }
            hash += (hash << 3);
            hash ^= (hash >> 11);
            hash += (hash << 15);
            return hash & 0x7FFFFFFF;
        }
    }
}
