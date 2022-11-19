using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace ZeroLevel.Services.Serialization
{
    public static class MessageSerializer
    {
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

        public static Action<MemoryStreamWriter, T> GetSerializer<T>()
        {
            var t = typeof(T);
            if (t.IsAssignableTo(typeof(IBinarySerializable)))
            {
                return (w, o) => ((IBinarySerializable)o).Serialize(w);
            }
            return (w, o) => PrimitiveTypeSerializer.Serialize<T>(w, o);
        }

        public static Func<IBinaryReader, T> GetDeserializer<T>()
        {
            if (typeof(IBinarySerializable).IsAssignableFrom(typeof(T)))
            {
                return (r) =>
                {
                    var o = (IBinarySerializable)FormatterServices.GetUninitializedObject(typeof(T));
                    o.Deserialize(r);
                    return (T)o;
                };
            }
            return (r) => PrimitiveTypeSerializer.Deserialize<T>(r);
        }

        public static byte[] SerializeCompatible(object obj)
        {
            if (null == obj)
            {
                return null;
            }
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

        public static void SerializeCompatible(this MemoryStreamWriter writer, object obj)
        {
            var direct_seriazlizable = (obj as IBinarySerializable);
            if (direct_seriazlizable != null)
            {
                direct_seriazlizable.Serialize(writer);
            }
            else
            {
                PrimitiveTypeSerializer.Serialize(writer, obj);
            }
        }

        public static byte[] SerializeCompatible<T>(T obj)
        {
            if (null == obj)
            {
                return null;
            }
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

        public static IEnumerable<T> DeserializeCollectionLazy<T>(byte[] data)
           where T : IBinarySerializable
        {
            if (data != null && data.Length > 0)
            {
                using (var reader = new MemoryStreamReader(data))
                {
                    int count = reader.ReadInt32();
                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            var item = Activator.CreateInstance<T>();
                            item.Deserialize(reader);
                            yield return item;
                        }
                    }
                }
            }
        }

        public static T DeserializeCompatible<T>(byte[] data)
        {
            if (data == null || data.Length == 0) return default(T);
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

        public static T Copy<T>(T value)
            where T : IBinarySerializable
        {
            if (null == value) return default;
            using (var writer = new MemoryStreamWriter())
            {
                value.Serialize(writer);
                using (var reader = new MemoryStreamReader(writer.Complete()))
                {
                    var direct = (IBinarySerializable)Activator.CreateInstance<T>();
                    direct.Deserialize(reader);
                    return (T)direct;
                }
            }
        }

        public static T CopyCompatible<T>(T value)
        {
            if (typeof(IBinarySerializable).IsAssignableFrom(typeof(T)))
            {
                using (var writer = new MemoryStreamWriter())
                {
                    ((IBinarySerializable)value).Serialize(writer);
                    using (var reader = new MemoryStreamReader(writer.Complete()))
                    {
                        var direct = (IBinarySerializable)Activator.CreateInstance<T>();
                        direct.Deserialize(reader);
                        return (T)direct;
                    }
                }
            }
            using (var writer = new MemoryStreamWriter())
            {
                PrimitiveTypeSerializer.Serialize<T>(writer, value);
                using (var reader = new MemoryStreamReader(writer.Complete()))
                {
                    return PrimitiveTypeSerializer.Deserialize<T>(reader);
                }
            }
        }

        #region Stream
        public static void Serialize<T>(Stream stream, T obj)
            where T : IBinarySerializable
        {
            if (obj == null) return;
            using (var writer = new MemoryStreamWriter(stream))
            {
                obj.Serialize(writer);
            }
        }

        public static void Serialize<T>(Stream stream, IEnumerable<T> items)
            where T : IBinarySerializable
        {
            if (items == null) return;
            using (var writer = new MemoryStreamWriter(stream))
            {
                writer.WriteCollection<T>(items);
            }
        }

        public static void SerializeCompatible(Stream stream, object obj)
        {
            if (null == obj)
            {
                return;
            }
            var direct_seriazlizable = (obj as IBinarySerializable);
            if (direct_seriazlizable != null)
            {
                using (var writer = new MemoryStreamWriter(stream))
                {
                    direct_seriazlizable.Serialize(writer);
                }
            }
            else
            {
                using (var writer = new MemoryStreamWriter(stream))
                {
                    PrimitiveTypeSerializer.Serialize(writer, obj);
                }
            }
        }

        public static void SerializeCompatible<T>(Stream stream, T obj)
        {
            if (null == obj)
            {
                return;
            }
            var direct_seriazlizable = (obj as IBinarySerializable);
            if (direct_seriazlizable != null)
            {
                using (var writer = new MemoryStreamWriter(stream))
                {
                    direct_seriazlizable.Serialize(writer);
                }
            }
            else
            {
                using (var writer = new MemoryStreamWriter(stream))
                {
                    PrimitiveTypeSerializer.Serialize<T>(writer, obj);
                }
            }
        }

        public static T Deserialize<T>(Stream stream)
            where T : IBinarySerializable
        {
            if (stream == null) return default(T);
            using (var reader = new MemoryStreamReader(stream))
            {
                var result = Activator.CreateInstance<T>();
                result.Deserialize(reader);
                return result;
            }
        }

        public static object Deserialize(Type type, Stream stream)
        {
            if (stream == null) return null;
            using (var reader = new MemoryStreamReader(stream))
            {
                var result = (IBinarySerializable)Activator.CreateInstance(type);
                result.Deserialize(reader);
                return result;
            }
        }

        public static List<T> DeserializeCollection<T>(Stream stream)
           where T : IBinarySerializable
        {
            List<T> collection = null;
            if (stream != null)
            {
                using (var reader = new MemoryStreamReader(stream))
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

        public static IEnumerable<T> DeserializeCollectionLazy<T>(Stream stream)
           where T : IBinarySerializable
        {
            if (stream != null)
            {
                using (var reader = new MemoryStreamReader(stream))
                {
                    int count = reader.ReadInt32();
                    if (count > 0)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            var item = Activator.CreateInstance<T>();
                            item.Deserialize(reader);
                            yield return item;
                        }
                    }
                }
            }
        }

        public static T DeserializeCompatible<T>(Stream stream)
        {
            if (stream == null) return default(T);
            if (typeof(IBinarySerializable).IsAssignableFrom(typeof(T)))
            {
                using (var reader = new MemoryStreamReader(stream))
                {
                    var direct = (IBinarySerializable)Activator.CreateInstance<T>();
                    direct.Deserialize(reader);
                    return (T)direct;
                }
            }
            using (var reader = new MemoryStreamReader(stream))
            {
                return PrimitiveTypeSerializer.Deserialize<T>(reader);
            }
        }

        public static object DeserializeCompatible(Type type, Stream stream)
        {
            if (stream == null) return null;
            if (typeof(IBinarySerializable).IsAssignableFrom(type))
            {
                using (var reader = new MemoryStreamReader(stream))
                {
                    var direct = (IBinarySerializable)Activator.CreateInstance(type);
                    direct.Deserialize(reader);
                    return direct;
                }
            }
            using (var reader = new MemoryStreamReader(stream))
            {
                return PrimitiveTypeSerializer.Deserialize(reader, type);
            }
        }
        #endregion
    }
}