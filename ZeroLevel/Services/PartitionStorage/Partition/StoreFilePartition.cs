using System;

namespace ZeroLevel.Services.PartitionStorage
{
    public class StoreFilePartition<TKey, TMeta>
    {
        public string Name { get; }
        public Func<TKey, TMeta, string> PathExtractor { get; }

        public StoreFilePartition(string name, Func<TKey, TMeta, string> pathExtractor)
        {
            Name = name;
            PathExtractor = pathExtractor;
        }
    }
}
