using System;
using ZeroLevel.Services.PartitionStorage.Interfaces;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.PartitionStorage
{
    

    // TODO INTERNAL
    public sealed class StoreStandartSerializer<TKey, TInput, TValue>
        : IStoreSerializer<TKey, TInput, TValue>
    {
        private readonly Action<MemoryStreamWriter, TKey> _keySerializer;
        private readonly Action<MemoryStreamWriter, TInput> _inputSerializer;
        private readonly TryDeserializeMethod<TKey> _keyDeserializer;
        private readonly TryDeserializeMethod<TInput> _inputDeserializer;
        private readonly TryDeserializeMethod<TValue> _valueDeserializer;

        public StoreStandartSerializer()
        {
            _keySerializer = MessageSerializer.GetSerializer<TKey>();
            _inputSerializer = MessageSerializer.GetSerializer<TInput>();

            _keyDeserializer = MessageSerializer.GetSafetyDeserializer<TKey>();
            _inputDeserializer = MessageSerializer.GetSafetyDeserializer<TInput>();
            _valueDeserializer = MessageSerializer.GetSafetyDeserializer<TValue>();
        }

        public Action<MemoryStreamWriter, TKey> KeySerializer => _keySerializer;

        public Action<MemoryStreamWriter, TInput> InputSerializer => _inputSerializer;

        public TryDeserializeMethod<TKey> KeyDeserializer => _keyDeserializer;

        public TryDeserializeMethod<TInput> InputDeserializer => _inputDeserializer;

        public TryDeserializeMethod<TValue> ValueDeserializer => _valueDeserializer;
    }
}
