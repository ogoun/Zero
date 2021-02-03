using System;
using System.IO;
using ZeroLevel.Services.HashFunctions;

namespace ZeroLevel.DataStructures
{
    public class HyperBloomBloom
    {
        private BloomFilter[] _shardes;

        public HyperBloomBloom(int shardes_size, int bit_size)
        {
            _shardes = new BloomFilter[shardes_size];
            for (int i = 0; i < shardes_size; i++)
            {
                _shardes[i] = new BloomFilter(bit_size);
            }
        }

        private HyperBloomBloom()
        {
        }

        public void Add(string item)
        {
            if (item == null || item.Length == 0) return;
            var index = GetIndex(item);
            _shardes[index].Add(item);
        }

        public bool Contains(string item)
        {
            if (item == null || item.Length == 0) return true;
            var index = GetIndex(item);
            return _shardes[index].Contains(item);
        }
        /// <summary>
        /// true if added, false if already exists
        /// </summary>
        public bool TryAdd(string item)
        {
            if (item == null || item.Length == 0) return false;
            var index = GetIndex(item);
            return _shardes[index].TryAdd(item);
        }

        private uint GetIndex(string line)
        {
            var hash = StringHash.DotNetFullHash(line);
            return (uint)(hash % _shardes.Length);
        }

        public byte[] Dump()
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(BitConverter.GetBytes(_shardes.Length), 0, 4);
                foreach (var shard in _shardes)
                {
                    var arr = shard.Dump();
                    stream.Write(BitConverter.GetBytes(arr.Length), 0, 4);
                    stream.Write(arr, 0, arr.Length);
                }
                return stream.ToArray();
            }
        }

        public static HyperBloomBloom Load(byte[] data)
        {
            var hbb = new HyperBloomBloom();
            byte[] sizeArr = new byte[4];
            using (var stream = new MemoryStream(data))
            {
                stream.Read(sizeArr, 0, 4);
                var count = BitConverter.ToInt32(sizeArr, 0);
                hbb._shardes = new BloomFilter[count];
                for (int i = 0; i < count; i++)
                {
                    stream.Read(sizeArr, 0, 4);
                    var length = BitConverter.ToInt32(sizeArr, 0);
                    var bloomData = new byte[length];
                    stream.Read(bloomData, 0, length);
                    hbb._shardes[i] = BloomFilter.Load(bloomData);
                }
            }
            return hbb;
        }

        public bool IsEqual(HyperBloomBloom other)
        {
            if (this._shardes.Length == other._shardes.Length)
            {
                for (int i = 0; i < this._shardes.Length; i++)
                {
                    if (false == this._shardes[i].IsEqual(other._shardes[i]))
                        return false;
                }
                return true;
            }
            return false;
        }
    }
}
