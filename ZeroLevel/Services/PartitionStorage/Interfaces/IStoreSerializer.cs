using System;
using System.Threading.Tasks;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage.Interfaces
{
    public interface IStoreSerializer<TKey, TInput, TValue>
    {
        Func<MemoryStreamWriter, TKey, Task> KeySerializer { get; }

        Func<MemoryStreamWriter, TInput, Task> InputSerializer { get; }

        Func<MemoryStreamWriter, TValue, Task> ValueSerializer { get; }

        Func<MemoryStreamReader, Task<DeserializeResult<TKey>>> KeyDeserializer { get; }

        Func<MemoryStreamReader, Task<DeserializeResult<TInput>>> InputDeserializer { get; }

        Func<MemoryStreamReader, Task<DeserializeResult<TValue>>> ValueDeserializer { get; }
    }
}
