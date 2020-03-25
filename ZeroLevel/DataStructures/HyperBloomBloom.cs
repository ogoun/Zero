using System;
using System.Collections.Generic;

namespace ZeroLevel.DataStructures
{
    public class HyperBloomBloom
    {
        private BloomFilter _trash;
        private Dictionary<char, BloomFilter> _shardes = new Dictionary<char, BloomFilter>();

        public HyperBloomBloom(int bit_size, bool use_reverse)
        {
            _trash = new BloomFilter(bit_size, use_reverse);
            foreach (var ch in "abcdefghijklmnopqrstuvwxyz0123456789-")
            {
                _shardes.Add(ch, new BloomFilter(bit_size, use_reverse));
            }
        }

        public void Add(string item)
        {
            if (item == null || item.Length == 0) return;
            var k = Char.ToLowerInvariant(item[0]);
            BloomFilter filter;
            if (_shardes.TryGetValue(k, out filter) == false) filter = _trash;
            filter.Add(item);
        }

        public bool Contains(string item)
        {
            if (item == null || item.Length == 0) return true;
            var k = Char.ToLowerInvariant(item[0]);
            BloomFilter filter;
            if (_shardes.TryGetValue(k, out filter) == false) filter = _trash;
            return filter.Contains(item);
        }
        /// <summary>
        /// true if added, false if already exists
        /// </summary>
        public bool TryAdd(string item)
        {
            if (item == null || item.Length == 0) return false;
            var k = Char.ToLowerInvariant(item[0]);
            BloomFilter filter;
            if (_shardes.TryGetValue(k, out filter) == false) filter = _trash;
            return filter.TryAdd(item);
        }
    }
}
