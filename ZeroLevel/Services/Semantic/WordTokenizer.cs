using System;
using System.Collections.Generic;
using ZeroLevel.Services.Pools;

namespace ZeroLevel.Services.Semantic
{
    public static class WordTokenizer
    {
        static Pool<char[]> _pool = new Pool<char[]>(64 ,(p) => new char[2048]);

        public static IEnumerable<string> Tokenize(string text)
        {
            int index = 0;
            bool first = true;
            var buffer = _pool.Acquire();
            try
            {
                for (int i = 0; i < text?.Length; i++)
                {
                    if (first && Char.IsLetter(text[i]))
                    {
                        first = false;
                        buffer[index++] = text[i];
                    }
                    else if (first == false &&  Char.IsLetterOrDigit(text[i]))
                    {
                        buffer[index++] = text[i];
                    }
                    else if (index > 0)
                    {
                        yield return new string(buffer, 0, index).ToLowerInvariant();
                        index = 0;
                        first = true;
                    }
                }
                if (index > 0)
                {
                    yield return new string(buffer, 0, index).ToLowerInvariant();
                }
            }
            finally
            {
                _pool.Release(buffer);
            }
        }
    }
}
