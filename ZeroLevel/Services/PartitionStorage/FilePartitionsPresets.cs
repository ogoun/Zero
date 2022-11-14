using System;
using ZeroLevel.Services.HashFunctions;

namespace ZeroLevel.Services.PartitionStorage
{
    public static class FilePartitionsPresets
    {
        public static StoreFilePartition<string, TMeta> StringDivideIntoParts<TMeta>(string name, int parts)
        {
            Func<string, TMeta, string> extractor = (key, _) => ((int)Math.Abs(StringHash.DotNetFullHash(key) % parts)).ToString();
            return new StoreFilePartition<string, TMeta>(name, extractor);
        }

        public static StoreFilePartition<ulong, TMeta> ULongDivideIntoParts<TMeta>(string name, int parts)
        {
            Func<ulong, TMeta, string> extractor = (key, _) => (key % (ulong)parts).ToString();
            return new StoreFilePartition<ulong, TMeta>(name, extractor);
        }

        public static StoreFilePartition<long, TMeta> LongDivideIntoParts<TMeta>(string name, int parts)
        {
            Func<long, TMeta, string> extractor = (key, _) => (key % (long)parts).ToString();
            return new StoreFilePartition<long, TMeta>(name, extractor);
        }

        public static StoreFilePartition<uint, TMeta> UIntDivideIntoParts<TMeta>(string name, int parts)
        {
            Func<uint, TMeta, string> extractor = (key, _) => (key % (uint)parts).ToString();
            return new StoreFilePartition<uint, TMeta>(name, extractor);
        }

        public static StoreFilePartition<int, TMeta> IntDivideIntoParts<TMeta>(string name, int parts)
        {
            Func<int, TMeta, string> extractor = (key, _) => (key % parts).ToString();
            return new StoreFilePartition<int, TMeta>(name, extractor);
        }
    }
}