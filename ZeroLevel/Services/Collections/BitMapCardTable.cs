using System.Collections.Generic;

namespace ZeroLevel.Services.Collections
{
    public sealed class BitMapCardTable
    {
        private readonly long[] _bitmap;
        public BitMapCardTable(int N)
        {
            var count = N / 64 + (N % 64 == 0 ? 0 : 1);
            _bitmap = new long[count];
        }

        public bool this[int index]
        {
            get
            {
                var i = index / 64;
                var n = _bitmap[i];
                var bit_index = index % 64;
                return (n & (1L << bit_index)) != 0;
            }
            set
            {
                var i = index / 64;
                var bit_index = index % 64;
                if (value)
                {
                    _bitmap[i] = _bitmap[i] | (1L << bit_index);
                }
                else
                {
                    _bitmap[i] = _bitmap[i] & ~(1L << bit_index);
                }
            }
        }

        public void ResetTo(bool value)
        {
            if (value)
            {
                for (int i = 0; i < _bitmap.Length; i++)
                {
                    _bitmap[i] = long.MaxValue;
                    _bitmap[i] |= 1L << 63;
                }
            }
            else
            {
                for (int i = 0; i < _bitmap.Length; i++)
                {
                    _bitmap[i] = 0;
                }
            }
        }

        public IEnumerable<int> GetSetIndexes()
        {
            for (int i = 0; i < _bitmap.Length; i++)
            {
                if (_bitmap[i] != 0)
                {
                    var start = i * 64;
                    for (int offset = 0; offset < 64; offset++)
                    {
                        if ((_bitmap[i] & (1L << offset)) != 0)
                        {
                            yield return start + offset;
                        }
                    }
                }
            }
        }
    }
}
