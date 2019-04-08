using System;
using System.Collections.Generic;

namespace ZeroLevel.Services.Serialization
{
    public static class MessageSerializer
    {
        public static T Deserialize<T>(byte[] data)
            where T : IBinarySerializable
        {
            if (data == null || data.Length == 0) return default(T);
            using (var reader = new MemoryStreamReader(data))
            {
                var result = Activator.CreateInstance<T>();
                result.Deserialize(reader);
                return result;
            }
        }

        public static object Deserialize(Type type, byte[] data)
        {
            if (data == null || data.Length == 0) return null;
            using (var reader = new MemoryStreamReader(data))
            {
                var result = (IBinarySerializable)Activator.CreateInstance(type);
                result.Deserialize(reader);
                return result;
            }
        }

        public static List<T> DeserializeCollection<T>(byte[] data)
           where T : IBinarySerializable
        {
            List<T> collection = null;
            if (data != null && data.Length > 0)
            {
                using (var reader = new MemoryStreamReader(data))
                {
                    int count = reader.ReadInt32();
                    collection = new List<T>(count);
                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            var item = Activator.CreateInstance<T>();
                            item.Deserialize(reader);
                            collection.Add(item);
                        }
                    }
                }
            }
            return collection;
        }

        public static byte[] Serialize<T>(T obj)
            where T : IBinarySerializable
        {
            if (obj == null) return null;
            using (var writer = new MemoryStreamWriter())
            {
                obj.Serialize(writer);
                return writer.Complete();
            }
        }

        public static byte[] Serialize<T>(IEnumerable<T> items)
            where T : IBinarySerializable
        {
            if (items == null) return null;
            using (var writer = new MemoryStreamWriter())
            {
                writer.WriteCollection<T>(items);
                return writer.Complete();
            }
        }

        public static byte[] SerializeCompatible(object obj)
        {
            var direct_seriazlizable = (obj as IBinarySerializable);
            if (direct_seriazlizable != null)
            {
                using (var writer = new MemoryStreamWriter())
                {
                    direct_seriazlizable.Serialize(writer);
                    return writer.Complete();
                }
            }
            using (var writer = new MemoryStreamWriter())
            {
                PrimitiveTypeSerializer.Serialize(writer, obj);
                return writer.Complete();
            }
        }

        public static byte[] SerializeCompatible<T>(T obj)
        {
            var direct_seriazlizable = (obj as IBinarySerializable);
            if (direct_seriazlizable != null)
            {
                using (var writer = new MemoryStreamWriter())
                {
                    direct_seriazlizable.Serialize(writer);
                    return writer.Complete();
                }
            }
            using (var writer = new MemoryStreamWriter())
            {
                PrimitiveTypeSerializer.Serialize<T>(writer, obj);
                return writer.Complete();
            }
        }

        public static T DeserializeCompatible<T>(byte[] data)
        {
            if (typeof(IBinarySerializable).IsAssignableFrom(typeof(T)))
            {
                using (var reader = new MemoryStreamReader(data))
                {
                    var direct = (IBinarySerializable)Activator.CreateInstance<T>();
                    direct.Deserialize(reader);
                    return (T)direct;
                }
            }
            using (var reader = new MemoryStreamReader(data))
            {
                return PrimitiveTypeSerializer.Deserialize<T>(reader);
            }
        }

        public static T DeserializeCompatible<T>(IBinaryReader reader)
        {
            if (typeof(IBinarySerializable).IsAssignableFrom(typeof(T)))
            {
                var direct = (IBinarySerializable)Activator.CreateInstance<T>();
                direct.Deserialize(reader);
                return (T)direct;
            }
            return PrimitiveTypeSerializer.Deserialize<T>(reader);
        }

        public static object DeserializeCompatible(Type type, byte[] data)
        {
            if (data == null || data.Length == 0) return null;
            if (typeof(IBinarySerializable).IsAssignableFrom(type))
            {
                using (var reader = new MemoryStreamReader(data))
                {
                    var direct = (IBinarySerializable)Activator.CreateInstance(type);
                    direct.Deserialize(reader);
                    return direct;
                }
            }
            using (var reader = new MemoryStreamReader(data))
            {
                return PrimitiveTypeSerializer.Deserialize(reader, type);
            }
        }
    }
}