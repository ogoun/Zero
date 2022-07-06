using System;
using System.Buffers;
using System.Collections.Generic;

namespace ZeroLevel.Services.Semantic
{
    public static class WordTokenizer
    {
        const int ARRAY_SIZE = 2048;
        static ArrayPool<char> _pool = ArrayPool<char>.Create();

        public static IEnumerable<string> Tokenize(string text)
        {
            int index = 0;
            bool first = true;
            var buffer = _pool.Rent(ARRAY_SIZE);
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
                _pool.Return(buffer);
            }
        }

        public static IEnumerable<string> TokenizeCaseSensitive(string text)
        {
            int index = 0;
            bool first = true;
            var buffer = _pool.Rent(ARRAY_SIZE);
            try
            {
                for (int i = 0; i < text?.Length; i++)
                {
                    if (first && Char.IsLetter(text[i]))
                    {
                        first = false;
                        buffer[index++] = text[i];
                    }
                    else if (first == false && Char.IsLetterOrDigit(text[i]))
                    {
                        buffer[index++] = text[i];
                    }
                    else if (index > 0)
                    {
                        yield return new string(buffer, 0, index);
                        index = 0;
                        first = true;
                    }
                }
                if (index > 0)
                {
                    yield return new string(buffer, 0, index);
                }
            }
            finally
            {
                _pool.Return(buffer);
            }
        }
    }
}
