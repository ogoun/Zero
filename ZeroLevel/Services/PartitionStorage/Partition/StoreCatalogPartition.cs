using System;

namespace ZeroLevel.Services.PartitionStorage
{
    /// <summary>
    /// Partition, contains the method of forming the path
    /// </summary>
    public class StoreCatalogPartition<TMeta>
    {
        /// <summary>
        /// Name of partition, just for info
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Path generator
        /// </summary>
        public Func<TMeta, string> PathExtractor { get; }

        public StoreCatalogPartition(string name, Func<TMeta, string> pathExtractor)
        {
            Name = name;
            PathExtractor = pathExtractor;
        }
    }
}
