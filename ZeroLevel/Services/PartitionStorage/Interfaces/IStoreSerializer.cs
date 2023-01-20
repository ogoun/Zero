using System;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage.Interfaces
{
    public interface IStoreSerializer<TKey, TInput, TValue>
    {
        Action<MemoryStreamWriter, TKey> KeySerializer { get; }
        Action<MemoryStreamWriter, TInput> InputSerializer { get; }
        TryDeserializeMethod<TKey> KeyDeserializer { get; }
        TryDeserializeMethod<TInput> InputDeserializer { get; }
        TryDeserializeMethod<TValue> ValueDeserializer { get; }
    }
}
