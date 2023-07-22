using System;
using System.Threading.Tasks;
using ZeroLevel.Services.PartitionStorage.Interfaces;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage
{
    public record DeserializeResult<T>(bool Success, T Value);

    public delegate Task<DeserializeResult<T>> TryDeserializeMethod<T>(MemoryStreamReader reader);

    public sealed class StoreSerializers<TKey, TInput, TValue>
        : IStoreSerializer<TKey, TInput, TValue>
    {
        private readonly Func<MemoryStreamWriter, TKey, Task> _keySerializer;
        private readonly Func<MemoryStreamWriter, TInput, Task> _inputSerializer;
        private readonly Func<MemoryStreamWriter, TValue, Task> _valueSerializer;
        private readonly Func<MemoryStreamReader, Task<DeserializeResult<TKey>>> _keyDeserializer;
        private readonly Func<MemoryStreamReader, Task<DeserializeResult<TInput>>> _inputDeserializer;
        private readonly Func<MemoryStreamReader, Task<DeserializeResult<TValue>>> _valueDeserializer;

        public StoreSerializers(Func<MemoryStreamWriter, TKey, Task> keySerializer,
            Func<MemoryStreamWriter, TInput, Task> inputSerializer,
            Func<MemoryStreamWriter, TValue, Task> valueSerializer,
            Func<MemoryStreamReader, Task<DeserializeResult<TKey>>> keyDeserializer,
            Func<MemoryStreamReader, Task<DeserializeResult<TInput>>> inputDeserializer,
            Func<MemoryStreamReader, Task<DeserializeResult<TValue>>> valueDeserializer)
        {
            _keySerializer = keySerializer;
            _inputSerializer = inputSerializer;
            _valueSerializer = valueSerializer;
            _keyDeserializer = keyDeserializer;
            _inputDeserializer = inputDeserializer;
            _valueDeserializer = valueDeserializer;
        }

        public Func<MemoryStreamWriter, TKey, Task> KeySerializer => _keySerializer;

        public Func<MemoryStreamWriter, TInput, Task> InputSerializer => _inputSerializer;

        public Func<MemoryStreamWriter, TValue, Task> ValueSerializer => _valueSerializer;

        public Func<MemoryStreamReader, Task<DeserializeResult<TKey>>> KeyDeserializer => _keyDeserializer;

        public Func<MemoryStreamReader, Task<DeserializeResult<TInput>>> InputDeserializer => _inputDeserializer;

        public Func<MemoryStreamReader, Task<DeserializeResult<TValue>>> ValueDeserializer => _valueDeserializer;
    }
}
