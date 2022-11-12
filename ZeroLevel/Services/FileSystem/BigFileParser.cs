using System;
using System.Collections.Generic;
using System.IO;

namespace ZeroLevel.Services.FileSystem
{
    public class BigFileParser<T>
    {
        private readonly string _filePath;
        private readonly Func<string, T> _parser;
        private readonly int _bufferSize;

        public BigFileParser(string filePath, Func<string, T> parser, int bufferSize = 1024 * 1024 * 32)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }
            _filePath = filePath;
            _parser = parser;
            _bufferSize = bufferSize;

        }

        public IEnumerable<T[]> ReadBatches(int batchSize, bool skipNull = false)
        {
            T[] buffer;
            var buffer_index = 0;
            using (FileStream fs = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (BufferedStream bs = new BufferedStream(fs, _bufferSize))
                {
                    using (StreamReader sr = new StreamReader(bs))
                    {
                        string line;
                        buffer = new T[batchSize];
                        while ((line = sr.ReadLine()) != null)
                        {
                            var value = _parser.Invoke(line);
                            if (skipNull && value == null) continue;
                            buffer[buffer_index] = value;
                            buffer_index++;
                            if (buffer_index >= batchSize)
                            {
                                yield return buffer;
                                buffer_index = 0;
                            }
                        }
                    }
                }
            }
            if (buffer_index > 0)
            {
                if (buffer_index < batchSize)
                {
                    var bias = new T[buffer_index];
                    Array.Copy(buffer, 0, bias, 0, buffer_index);
                    yield return bias;
                }
            }
        }

        public IEnumerable<T> Read()
        {
            using (FileStream fs = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (BufferedStream bs = new BufferedStream(fs, _bufferSize))
                {
                    using (StreamReader sr = new StreamReader(bs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            yield return _parser.Invoke(line);
                        }
                    }
                }
            }
        }
    }
}
