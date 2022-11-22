using System;

namespace ZeroLevel.Services.PartitionStorage
{
    /// <summary>
    /// File partition, contains the method of forming the path
    /// </summary>
    public class StoreFilePartition<TKey, TMeta>
    {
        /// <summary>
        /// Name of partition, just for info
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// File name generator
        /// </summary>
        public Func<TKey, TMeta, string> FileNameExtractor { get; }

        public StoreFilePartition(string name, Func<TKey, TMeta, string> pathExtractor)
        {
            Name = name;
            FileNameExtractor = pathExtractor;
        }
    }
}
