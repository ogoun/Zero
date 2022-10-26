using System;
using System.Collections.Generic;

namespace ZeroLevel.Services.Storages.PartitionFileSystemStorage
{
    public class PartitionFileSystemStorageOptions<TKey, TRecord>
    {
        public string RootFolder { get; set; }
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount / 2;
        public bool MergeFiles { get; set; } = false;
        public int MergeFrequencyInMinutes { get; set; } = 180;
        public bool UseCompression { get; set; } = false;
        public IPartitionDataConverter<TRecord> DataConverter { get; set; }
        public List<Partition<TKey>> Partitions { get; set; } = new List<Partition<TKey>>();
    }
}
