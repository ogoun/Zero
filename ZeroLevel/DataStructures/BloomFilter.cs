using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using ZeroLevel.Services.HashFunctions;

namespace ZeroLevel.DataStructures
{
    public class BloomFilter
    {
        #region Private
        private struct HIND
        {
            public int IndexFirst;
            public int IndexSecond;
            public int IndexThird;
            public int IndexReverse;
        }

        private BitArray _primary;
        private BitArray _second;
        private BitArray _third;
        private BitArray _reverse;
        #endregion

        public BloomFilter(int bit_size)
        {
            var diff = bit_size % 8;
            if (diff != 0)
            {
                bit_size += diff;
            }
            _primary = new BitArray(bit_size);
            _second = new BitArray(bit_size);
            _third = new BitArray(bit_size);
            _reverse = new BitArray(bit_size);
        }

        private HIND Compute(string line)
        {
            var r = Reverse(line);
            var first = HashMM(line) ^ StringHash.DotNetFullHash(line);
            var second = HashXX(line) ^ StringHash.DotNetFullHash(r);
            var third = HashMM(r) ^ StringHash.CustomHash(line);
            var reverse = HashXX(r) ^ StringHash.CustomHash2(r);

            var hind = new HIND
            {
                IndexFirst = (int)(first % _primary.Length),
                IndexSecond = (int)(second % _second.Length),
                IndexThird = (int)(third % _third.Length),
                IndexReverse = (int)(reverse % _reverse.Length)
            };
            return hind;
        }

        private BloomFilter()
        {
        }

        public void Add(string item)
        {
            if (item == null || item.Length == 0) return;
            var hind = Compute(item);
            Add(hind);
        }

        public bool Contains(string item)
        {
            if (item == null || item.Length == 0) return true;
            var hind = Compute(item);
            return Contains(hind);
        }
        /// <summary>
        /// true if added, false if already exists
        /// </summary>
        public bool TryAdd(string item)
        {
            if (item == null || item.Length == 0) return false;
            var hind = Compute(item);
            if (Contains(hind))
            {
                return false;
            }
            Add(hind);
            return true;
        }

        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Add(HIND hind)
        {
            _primary[hind.IndexFirst] = true;
            _second[hind.IndexSecond] = true;
            _third[hind.IndexThird] = true;
            _reverse[hind.IndexReverse] = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Contains(HIND hind)
        {
            if (!_primary[hind.IndexFirst]) return false;
            if (!_second[hind.IndexSecond]) return false;
            if (!_third[hind.IndexThird]) return false;
            if (!_reverse[hind.IndexReverse]) return false;

            return true;
        }

        private readonly XXHashUnsafe _hash_xx_32 = new XXHashUnsafe();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint HashXX(string line)
        {
            return _hash_xx_32.Hash(line);
        }

        private readonly Murmur3Unsafe _hash_mm_32 = new Murmur3Unsafe();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint HashMM(string line)
        {
            return _hash_mm_32.Hash(line);
        }

        public bool IsEqual(BloomFilter other)
        {
            if (Equals(this._primary, other._primary) == false) return false;
            if (Equals(this._second, other._second) == false) return false;
            if (Equals(this._third, other._third) == false) return false;
            if (Equals(this._reverse, other._reverse) == false) return false;
            return true;
        }

        public bool Equals(BitArray first, BitArray second)
        {
            if (first.Length != second.Length)
            {
                return false;
            }
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }
            return true;
        }

        public byte[] Dump()
        {
            var add = new Action<MemoryStream, BitArray>((ms, arr) =>
            {
                int tc = arr.Length / 8;
                ms.Write(BitConverter.GetBytes(tc), 0, 4);
                byte[] t = new byte[tc];
                arr.CopyTo(t, 0);
                ms.Write(t, 0, tc);
            });
            using (var ms = new MemoryStream())
            {
                add(ms, _primary);
                add(ms, _second);
                add(ms, _third);
                add(ms, _reverse);
                return ms.ToArray();
            }
        }

        public static BloomFilter Load(byte[] data)
        {
            var bf = new BloomFilter();
            byte[] sizeArr = new byte[4];
            var readArray = new Func<MemoryStream, BitArray>(stream =>
            {
                stream.Read(sizeArr, 0, 4);
                int count = BitConverter.ToInt32(sizeArr, 0);
                byte[] bfData = new byte[count];
                stream.Read(bfData, 0, count);
                return new BitArray(bfData);
            });
            using (var ms = new MemoryStream(data))
            {
                bf._primary = readArray(ms);
                bf._second = readArray(ms);
                bf._third = readArray(ms);
                bf._reverse = readArray(ms);
            }
            return bf;
        }
    }
}
