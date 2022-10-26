using System;

namespace ZeroLevel.Services.Storages.PartitionFileSystemStorage
{
    /// <summary>
    /// Make part of full file path
    /// </summary>
    public class Partition<TKey>
    {
        public Partition(string name, Func<TKey, string> pathExtractor)
        {
            Name = name;
            PathExtractor = pathExtractor;
        }
        public Func<TKey, string> PathExtractor { get; }
        public string Name { get; }
    }
}
