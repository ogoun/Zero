using System;
using System.Collections.Generic;
using ZeroLevel.Services.Reflection;

namespace ZeroLevel.Services.Serialization
{
    public static class MessageSerializer
    {
        private readonly static Type _wgt = typeof(SerializedObjectWrapper<>);

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


        public static bool TrySerialize<T>(T obj, out byte[] data)
        {
            if (null == obj)
            {
                data = null;
                return false;
            }
            try
            {
                var direct_seriazlizable = (obj as IBinarySerializable);
                if (direct_seriazlizable != null)
                {
                    using (var writer = new MemoryStreamWriter())
                    {
                        direct_seriazlizable.Serialize(writer);
                        data = writer.Complete();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"[MessageSerializer] Fault direct serialization object.\r\n{ex.ToString()}");
                data = null;
                return false;
            }
            try
            {
                var wrapper = new SerializedObjectWrapper<T>(obj);
                using (var writer = new MemoryStreamWriter())
                {
                    wrapper.Serialize(writer);
                    data = writer.Complete();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"[MessageSerializer] Can't serialize object. {ex.Message}");
            }
            data = null;
            return false;
        }

        public static bool TryDeserialize<T>(byte[] data, out T result)
        {
            if (data == null || data.Length == 0)
            {
                result = default(T);
                return false;
            }
            try
            {
                if (typeof(IBinarySerializable).IsAssignableFrom(typeof(T)))
                {
                    using (var reader = new MemoryStreamReader(data))
                    {
                        var direct = (IBinarySerializable)Activator.CreateInstance<T>();
                        direct.Deserialize(reader);
                        result = (T)direct;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"[MessageSerializer] Fault direct deserialization object.\r\n{ex.ToString()}");
                result = default(T);
                return false;
            }
            try
            {
                var wrapper = new SerializedObjectWrapper<T>();
                using (var reader = new MemoryStreamReader(data))
                {
                    wrapper.Deserialize(reader);
                    result = wrapper.Value;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"[MessageSerializer] Can't deserialize object. {ex.Message}");
            }
            result = default(T);
            return false;
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
            var rt = _wgt.MakeGenericType(obj.GetType());
            var instance = (IBinarySerializable)Activator.CreateInstance(rt, new object[] { obj });
            using (var writer = new MemoryStreamWriter())
            {
                instance.Serialize(writer);
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
            var wrapper = new SerializedObjectWrapper<T>(obj);
            using (var writer = new MemoryStreamWriter())
            {
                wrapper.Serialize(writer);
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
            var wrapper = new SerializedObjectWrapper<T>();
            using (var reader = new MemoryStreamReader(data))
            {
                wrapper.Deserialize(reader);
                return wrapper.Value;
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
            var wrapper = new SerializedObjectWrapper<T>();
            wrapper.Deserialize(reader);
            return wrapper.Value;
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
            var rt = _wgt.MakeGenericType(type);
            var instance = (IBinarySerializable)Activator.CreateInstance(rt);
            using (var reader = new MemoryStreamReader(data))
            {
                instance.Deserialize(reader);
            }
            return TypeFastAccessMethodBuilder.BuildGetter(rt.GetProperty("Value"))(instance);
        }
    }
}
