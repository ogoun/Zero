using System;
using ZeroLevel.Services.PartitionStorage.Interfaces;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage
{
    internal sealed class StoreStandartSerializer<TKey, TInput, TValue>
        : IStoreSerializer<TKey, TInput, TValue>
    {
        private readonly Action<MemoryStreamWriter, TKey> _keySerializer;
        private readonly Action<MemoryStreamWriter, TInput> _inputSerializer;
        private readonly Func<MemoryStreamReader, TKey> _keyDeserializer;
        private readonly Func<MemoryStreamReader, TInput> _inputDeserializer;
        private readonly Func<MemoryStreamReader, TValue> _valueDeserializer;

        public StoreStandartSerializer()
        {
            _keySerializer = MessageSerializer.GetSerializer<TKey>();
            _inputSerializer = MessageSerializer.GetSerializer<TInput>();
            _keyDeserializer = MessageSerializer.GetDeserializer<TKey>();
            _inputDeserializer = MessageSerializer.GetDeserializer<TInput>();
            _valueDeserializer = MessageSerializer.GetDeserializer<TValue>();
        }

        public Action<MemoryStreamWriter, TKey> KeySerializer => _keySerializer;

        public Action<MemoryStreamWriter, TInput> InputSerializer => _inputSerializer;

        public Func<MemoryStreamReader, TKey> KeyDeserializer => _keyDeserializer;

        public Func<MemoryStreamReader, TInput> InputDeserializer => _inputDeserializer;

        public Func<MemoryStreamReader, TValue> ValueDeserializer => _valueDeserializer;
    }
}
