using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using ZeroLevel.Services.HashFunctions;

namespace ZeroLevel.DataStructures
{
    /// <summary>
    /// Bloom filter implementation, 128 bit
    /// </summary>
    public class BloomFilter
    {
        #region Private
        private struct HIND
        {
            public ulong PrimiryDirect;
            public uint SecondDirect;
            public uint ThirdDirect;
            public ulong PrimiryReverse;
            public uint SecondReverse;
            public uint ThirdReverse;
        }

        private readonly BitArray _primary;
        private readonly BitArray _second;
        private readonly BitArray _third;

        private readonly BitArray _r_primary;
        private readonly BitArray _r_second;
        private readonly BitArray _r_third;

        private readonly bool _use_reverse = false;
        #endregion

        public BloomFilter(int bit_size, bool use_reverse)
        {
            _use_reverse = use_reverse;

            _primary = new BitArray(bit_size);
            _second = new BitArray(bit_size);
            _third = new BitArray(bit_size);

            if (_use_reverse)
            {
                _r_primary = new BitArray(bit_size);
                _r_second = new BitArray(bit_size);
                _r_third = new BitArray(bit_size);
            }
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

        private HIND Compute(string line)
        {
            var hind = new HIND
            {
                PrimiryDirect = HashUL(line),
                SecondDirect = HashXX(line),
                ThirdDirect = HashMM(line)
            };
            if(_use_reverse)
            { 
                var r = Reverse(line);
                hind.PrimiryReverse = HashUL(r);
                hind.SecondReverse = HashXX(r);
                hind.ThirdReverse = HashMM(r);
            }
            return hind;
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
            int pi = (int)(hind.PrimiryDirect % (ulong)_primary.Length);
            _primary[pi] = true;

            int si = (int)(hind.SecondDirect % (uint)_second.Length);
            _second[si] = true;

            int ti = (int)(hind.ThirdDirect % (uint)_third.Length);
            _third[ti] = true;

            if (_use_reverse)
            {
                int rpi = (int)(hind.PrimiryReverse % (ulong)_primary.Length);
                _r_primary[rpi] = true;

                int rsi = (int)(hind.SecondReverse % (uint)_second.Length);
                _r_second[rsi] = true;

                int rti = (int)(hind.ThirdReverse % (uint)_third.Length);
                _r_third[rti] = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Contains(HIND hind)
        {
            int pi = (int)(hind.PrimiryDirect % (ulong)_primary.Length);
            if (!_primary[pi]) return false;

            int si = (int)(hind.SecondDirect % (uint)_second.Length);
            if (!_second[si]) return false;

            int ti = (int)(hind.ThirdDirect % (uint)_third.Length);
            if (!_third[ti]) return false;

            if (_use_reverse)
            {
                int rpi = (int)(hind.PrimiryReverse % (ulong)_primary.Length);
                if (!_r_primary[rpi]) return false;

                int rsi = (int)(hind.SecondReverse % (uint)_second.Length);
                if (!_r_second[rsi]) return false;

                int rti = (int)(hind.ThirdReverse % (uint)_third.Length);
                if (!_r_third[rti]) return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong HashUL(string line)
        {
            return XXH3_64.Hash(line);
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
    }
}
