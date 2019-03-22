using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using ZeroLevel.Services.Invokation;

namespace ZeroLevel.Services.Serialization
{
    public class SerializedObjectWrapper<T>
        : IBinarySerializable
    {
        #region Cachee
        private class Wrapper
        {
            public string ReadId;
            public string WriteId;
            public IInvokeWrapper Invoker;

            public T Read(IBinaryReader reader)
            {
                return (T)Invoker.Invoke(reader, ReadId);
            }

            public void Write(IBinaryWriter writer, T value)
            {
                Invoker.Invoke(writer, WriteId, new object[] { value });
            }
        }

        private readonly static object _creation_lock = new object();
        private readonly static Dictionary<Type, Wrapper> _cachee =
            new Dictionary<Type, Wrapper>();
        private static Wrapper Create<Tw>()
        {
            var type = typeof(Tw);
            if (_cachee.ContainsKey(type) == false)
            {
                lock (_creation_lock)
                {
                    if (_cachee.ContainsKey(type) == false)
                    {
                        var wrapper = new Wrapper { Invoker = InvokeWrapper.Create() };
                        if (type == typeof(Int32))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadInt32").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteInt32").First();
                        }
                        else if (typeof(T) == typeof(Boolean))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadBoolean").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteBoolean").First();
                        }
                        else if (typeof(T) == typeof(Byte))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadByte").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteByte").First();
                        }
                        else if (typeof(T) == typeof(Byte[]))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadBytes").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteBytes").First();
                        }
                        else if (typeof(T) == typeof(DateTime))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDateTime").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteDateTime").First();
                        }
                        else if (typeof(T) == typeof(Decimal))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDecimal").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteDecimal").First();
                        }
                        else if (typeof(T) == typeof(Double))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDouble").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteDouble").First();
                        }
                        else if (typeof(T) == typeof(Guid))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadGuid").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteGuid").First();
                        }
                        else if (typeof(T) == typeof(IPAddress))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadIP").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteIP").First();
                        }
                        else if (typeof(T) == typeof(IPEndPoint))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadIPEndpoint").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteIPEndpoint").First();
                        }
                        else if (typeof(T) == typeof(Int64))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadLong").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteLong").First();
                        }

                        else if (typeof(T) == typeof(String))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadString").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteString").First();
                        }
                        else if (typeof(T) == typeof(TimeSpan))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadTimeSpan").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteTimeSpan").First();
                        }
                        //
                        //  Collections
                        //
                        else if (typeof(T) == typeof(IEnumerable<Int32>))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadInt32Collection").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreatePredicate()).First();
                        }
                        else if (typeof(T) == typeof(IEnumerable<Boolean>))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadBooleanCollection").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreatePredicate()).First();
                        }
                        else if (typeof(T) == typeof(IEnumerable<Byte>))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadByteCollection").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreatePredicate()).First();
                        }
                        else if (typeof(T) == typeof(IEnumerable<Byte[]>))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadByteArrayCollection").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreatePredicate()).First();
                        }
                        else if (typeof(T) == typeof(IEnumerable<DateTime>))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDateTimeCollection").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreatePredicate()).First();
                        }
                        else if (typeof(T) == typeof(IEnumerable<Double>))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDoubleCollection").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreatePredicate()).First();
                        }
                        else if (typeof(T) == typeof(IEnumerable<Guid>))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadGuidCollection").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreatePredicate()).First();
                        }
                        else if (typeof(T) == typeof(IEnumerable<IPAddress>))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadIPCollection").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreatePredicate()).First();
                        }
                        else if (typeof(T) == typeof(IEnumerable<IPEndPoint>))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadIPEndPointCollection").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreatePredicate()).First();
                        }
                        else if (typeof(T) == typeof(IEnumerable<Int64>))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadInt64Collection").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreatePredicate()).First();
                        }

                        else if (typeof(T) == typeof(IEnumerable<String>))
                        {
                            wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadStringCollection").First();
                            wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreatePredicate()).First();
                        }
                        //
                        //  Generic collection
                        //
                        else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(T)) &&
                            typeof(IBinarySerializable).IsAssignableFrom(typeof(T).GetGenericArguments().FirstOrDefault()))
                        {
                            var typeArg = typeof(T).GetGenericArguments().First();
                            wrapper.ReadId = wrapper.Invoker.ConfigureGeneric(typeof(MemoryStreamReader), typeArg, "ReadCollection").First();
                            wrapper.WriteId = wrapper.Invoker.ConfigureGeneric(typeof(MemoryStreamWriter), typeArg,
                                mi => mi.Name.Equals("WriteCollection") && mi.IsGenericMethod).First();
                        }
                        //
                        //  Not supported
                        //
                        else
                        {
                            throw new NotSupportedException($"Type {typeof(T).Name} not supported");
                        }
                        _cachee.Add(type, wrapper);
                    }
                }
            }
            return _cachee[type];
        }
        private Wrapper _wrapper;
        #endregion

        public SerializedObjectWrapper()
        {
            _wrapper = Create<T>();
        }

        public SerializedObjectWrapper(T obj) : this()
        {
            Value = obj;
        }

        public T Value { get; set; }

        private static Func<MethodInfo, bool> CreatePredicate()
        {
            return mi => mi.Name.Equals("WriteCollection", StringComparison.Ordinal) &&
            mi.GetParameters().First().ParameterType.GetGenericArguments().First() == typeof(T);
        }

        public void Deserialize(IBinaryReader reader)
        {
            this.Value = _wrapper.Read(reader);
        }

        public void Serialize(IBinaryWriter writer)
        {
            _wrapper.Write(writer, this.Value);
        }
    }
}
