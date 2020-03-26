using ZeroLevel.Services.HashFunctions;

namespace ZeroLevel.DataStructures
{
    public class HyperBloomBloom
    {
        private IHash _shardHash = new XXHashUnsafe();
        private BloomFilter[] _shardes;

        public HyperBloomBloom(int shardes_size, int bit_size, bool use_reverse)
        {
            _shardes = new BloomFilter[shardes_size];
            for (int i = 0; i < shardes_size; i++)
            {
                _shardes[i] = new BloomFilter(bit_size, use_reverse);
            }
        }

        public void Add(string item)
        {
            if (item == null || item.Length == 0) return;
            int index = (int)(_shardHash.Hash(item) % _shardes.Length);
            _shardes[index].Add(item);
        }

        public bool Contains(string item)
        {
            if (item == null || item.Length == 0) return true;
            int index = (int)(_shardHash.Hash(item) % _shardes.Length);
            return _shardes[index].Contains(item);
        }
        /// <summary>
        /// true if added, false if already exists
        /// </summary>
        public bool TryAdd(string item)
        {
            if (item == null || item.Length == 0) return false;
            int index = (int)(_shardHash.Hash(item) % _shardes.Length);
            return _shardes[index].TryAdd(item);
        }
    }
}
