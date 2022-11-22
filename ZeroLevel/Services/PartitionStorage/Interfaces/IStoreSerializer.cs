using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage.Interfaces
{
    public interface IStoreSerializer<TKey, TInput, TValue>
    {
        Action<MemoryStreamWriter, TKey> KeySerializer { get; }
        Action<MemoryStreamWriter, TInput> InputSerializer { get; }
        Func<MemoryStreamReader, TKey> KeyDeserializer { get; }
        Func<MemoryStreamReader, TInput> InputDeserializer { get; }
        Func<MemoryStreamReader, TValue> ValueDeserializer { get; }
    }
}
