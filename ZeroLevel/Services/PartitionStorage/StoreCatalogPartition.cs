using System;

namespace ZeroLevel.Services.PartitionStorage
{
    public class StoreCatalogPartition<TMeta>
    {
        public string Name { get; }
        public Func<TMeta, string> PathExtractor { get; }

        public StoreCatalogPartition(string name, Func<TMeta, string> pathExtractor)
        {
            Name = name;
            PathExtractor = pathExtractor;
        }
    }
}
