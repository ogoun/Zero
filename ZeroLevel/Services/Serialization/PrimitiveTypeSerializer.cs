using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using ZeroLevel.Services.Invokation;
using ZeroLevel.Services.Reflection;

namespace ZeroLevel.Services.Serialization
{
    public static class PrimitiveTypeSerializer
    {
        static PrimitiveTypeSerializer()
        {
            PreloadCachee();
        }

        #region Cachee

        private class Wrapper
        {
            public string ReadId;
            public string WriteId;
            public IInvokeWrapper Invoker;

            public T Read<T>(IBinaryReader reader)
            {
                return (T)Invoker.Invoke(reader, ReadId);
            }

            public object ReadObject(IBinaryReader reader)
            {
                return Invoker.Invoke(reader, ReadId);
            }

            public void Write<T>(IBinaryWriter writer, T value)
            {
                Invoker.Invoke(writer, WriteId, new object[] { value });
            }

            public void WriteObject(IBinaryWriter writer, object value)
            {
                Invoker.Invoke(writer, WriteId, new object[] { value });
            }
        }
        private readonly static Dictionary<Type, Wrapper> _cachee = new Dictionary<Type, Wrapper>();
        private readonly static Dictionary<Type, Type> _enumTypesCachee = new Dictionary<Type, Type>();
        private readonly static Dictionary<Type, Type> _arrayTypesCachee = new Dictionary<Type, Type>();

        private static void PreloadCachee()
        {
            _cachee.Add(typeof(char), Create<char>());
            _cachee.Add(typeof(Boolean), Create<Boolean>());
            _cachee.Add(typeof(Byte), Create<Byte>());
            _cachee.Add(typeof(Byte[]), Create<Byte[]>());
            _cachee.Add(typeof(Int32), Create<Int32>());
            _cachee.Add(typeof(UInt32), Create<UInt32>());
            _cachee.Add(typeof(Int64), Create<Int64>());
            _cachee.Add(typeof(UInt64), Create<UInt64>());
            _cachee.Add(typeof(Double), Create<Double>());
            _cachee.Add(typeof(float), Create<float>());
            _cachee.Add(typeof(short), Create<short>());
            _cachee.Add(typeof(ushort), Create<ushort>());
            _cachee.Add(typeof(Decimal), Create<Decimal>());
            _cachee.Add(typeof(DateTime), Create<DateTime>());
            _cachee.Add(typeof(Guid), Create<Guid>());
            _cachee.Add(typeof(String), Create<String>());
            _cachee.Add(typeof(TimeSpan), Create<TimeSpan>());
            _cachee.Add(typeof(IPEndPoint), Create<IPEndPoint>());
            _cachee.Add(typeof(IPAddress), Create<IPAddress>());

            _cachee.Add(typeof(char[]), Create<char[]>());
            _cachee.Add(typeof(Boolean[]), Create<Boolean[]>());
            _cachee.Add(typeof(Byte[][]), Create<Byte[][]>());
            _cachee.Add(typeof(Int32[]), Create<Int32[]>());
            _cachee.Add(typeof(UInt32[]), Create<UInt32[]>());
            _cachee.Add(typeof(Int64[]), Create<Int64[]>());
            _cachee.Add(typeof(UInt64[]), Create<UInt64[]>());
            _cachee.Add(typeof(Double[]), Create<Double[]>());
            _cachee.Add(typeof(float[]), Create<float[]>());
            _cachee.Add(typeof(short[]), Create<short[]>());
            _cachee.Add(typeof(ushort[]), Create<ushort[]>());
            _cachee.Add(typeof(Decimal[]), Create<Decimal[]>());
            _cachee.Add(typeof(DateTime[]), Create<DateTime[]>());
            _cachee.Add(typeof(Guid[]), Create<Guid[]>());
            _cachee.Add(typeof(String[]), Create<String[]>());
            _cachee.Add(typeof(TimeSpan[]), Create<TimeSpan[]>());
            _cachee.Add(typeof(IPEndPoint[]), Create<IPEndPoint[]>());
            _cachee.Add(typeof(IPAddress[]), Create<IPAddress[]>());

            _cachee.Add(typeof(IEnumerable<char>), Create<IEnumerable<char>>());
            _cachee.Add(typeof(IEnumerable<Boolean>), Create<IEnumerable<Boolean>>());
            _cachee.Add(typeof(IEnumerable<Byte>), Create<IEnumerable<Byte>>());
            _cachee.Add(typeof(IEnumerable<Byte[]>), Create<IEnumerable<Byte[]>>());
            _cachee.Add(typeof(IEnumerable<Int32>), Create<IEnumerable<Int32>>());
            _cachee.Add(typeof(IEnumerable<UInt32>), Create<IEnumerable<UInt32>>());
            _cachee.Add(typeof(IEnumerable<Int64>), Create<IEnumerable<Int64>>());
            _cachee.Add(typeof(IEnumerable<UInt64>), Create<IEnumerable<UInt64>>());
            _cachee.Add(typeof(IEnumerable<Double>), Create<IEnumerable<Double>>());
            _cachee.Add(typeof(IEnumerable<float>), Create<IEnumerable<float>>());
            _cachee.Add(typeof(IEnumerable<short>), Create<IEnumerable<short>>());
            _cachee.Add(typeof(IEnumerable<ushort>), Create<IEnumerable<ushort>>());
            _cachee.Add(typeof(IEnumerable<Decimal>), Create<IEnumerable<Decimal>>());
            _cachee.Add(typeof(IEnumerable<DateTime>), Create<IEnumerable<DateTime>>());
            _cachee.Add(typeof(IEnumerable<Guid>), Create<IEnumerable<Guid>>());
            _cachee.Add(typeof(IEnumerable<String>), Create<IEnumerable<String>>());
            _cachee.Add(typeof(IEnumerable<TimeSpan>), Create<IEnumerable<TimeSpan>>());
            _cachee.Add(typeof(IEnumerable<IPEndPoint>), Create<IEnumerable<IPEndPoint>>());
            _cachee.Add(typeof(IEnumerable<IPAddress>), Create<IEnumerable<IPAddress>>());

            _arrayTypesCachee.Add(typeof(char), typeof(char[]));
            _arrayTypesCachee.Add(typeof(Boolean), typeof(Boolean[]));
            _arrayTypesCachee.Add(typeof(Byte[]), typeof(Byte[][]));
            _arrayTypesCachee.Add(typeof(Int32), typeof(Int32[]));
            _arrayTypesCachee.Add(typeof(UInt32), typeof(UInt32[]));
            _arrayTypesCachee.Add(typeof(Int64), typeof(Int64[]));
            _arrayTypesCachee.Add(typeof(UInt64), typeof(UInt64[]));
            _arrayTypesCachee.Add(typeof(Double), typeof(Double[]));
            _arrayTypesCachee.Add(typeof(float), typeof(float[]));
            _arrayTypesCachee.Add(typeof(short), typeof(short[]));
            _arrayTypesCachee.Add(typeof(ushort), typeof(ushort[]));
            _arrayTypesCachee.Add(typeof(Decimal), typeof(Decimal[]));
            _arrayTypesCachee.Add(typeof(DateTime), typeof(DateTime[]));
            _arrayTypesCachee.Add(typeof(Guid), typeof(Guid[]));
            _arrayTypesCachee.Add(typeof(String), typeof(String[]));
            _arrayTypesCachee.Add(typeof(TimeSpan), typeof(TimeSpan[]));
            _arrayTypesCachee.Add(typeof(IPEndPoint), typeof(IPEndPoint[]));
            _arrayTypesCachee.Add(typeof(IPAddress), typeof(IPAddress[]));

            _enumTypesCachee.Add(typeof(char), typeof(IEnumerable<char>));
            _enumTypesCachee.Add(typeof(Boolean), typeof(IEnumerable<Boolean>));
            _enumTypesCachee.Add(typeof(Byte), typeof(IEnumerable<Byte>));
            _enumTypesCachee.Add(typeof(Byte[]), typeof(IEnumerable<Byte[]>));
            _enumTypesCachee.Add(typeof(Int32), typeof(IEnumerable<Int32>));
            _enumTypesCachee.Add(typeof(UInt32), typeof(IEnumerable<UInt32>));
            _enumTypesCachee.Add(typeof(Int64), typeof(IEnumerable<Int64>));
            _enumTypesCachee.Add(typeof(UInt64), typeof(IEnumerable<UInt64>));
            _enumTypesCachee.Add(typeof(Double), typeof(IEnumerable<Double>));
            _enumTypesCachee.Add(typeof(float), typeof(IEnumerable<float>));
            _enumTypesCachee.Add(typeof(short), typeof(IEnumerable<short>));
            _enumTypesCachee.Add(typeof(ushort), typeof(IEnumerable<ushort>));
            _enumTypesCachee.Add(typeof(Decimal), typeof(IEnumerable<Decimal>));
            _enumTypesCachee.Add(typeof(DateTime), typeof(IEnumerable<DateTime>));
            _enumTypesCachee.Add(typeof(Guid), typeof(IEnumerable<Guid>));
            _enumTypesCachee.Add(typeof(String), typeof(IEnumerable<String>));
            _enumTypesCachee.Add(typeof(TimeSpan), typeof(IEnumerable<TimeSpan>));
            _enumTypesCachee.Add(typeof(IPEndPoint), typeof(IEnumerable<IPEndPoint>));
            _enumTypesCachee.Add(typeof(IPAddress), typeof(IEnumerable<IPAddress>));
        }

        private static Wrapper Create<Tw>()
        {
            var type = typeof(Tw);
            var wrapper = new Wrapper { Invoker = InvokeWrapper.Create() };
            if (type == typeof(Int32))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadInt32").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteInt32").First();
            }
            else if (type == typeof(UInt32))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadUInt32").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteUInt32").First();
            }
            else if (type == typeof(char))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadChar").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteChar").First();
            }
            else if (type == typeof(Boolean))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadBoolean").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteBoolean").First();
            }
            else if (type == typeof(Byte))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadByte").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteByte").First();
            }
            else if (type == typeof(Byte[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadBytes").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteBytes").First();
            }
            else if (type == typeof(DateTime))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDateTime").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteDateTime").First();
            }
            else if (type == typeof(Decimal))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDecimal").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteDecimal").First();
            }
            else if (type == typeof(Double))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDouble").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteDouble").First();
            }
            else if (type == typeof(float))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadFloat").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteFloat").First();
            }
            else if (type == typeof(Int16))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadShort").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteShort").First();
            }
            else if (type == typeof(UInt16))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadUShort").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteUShort").First();
            }
            else if (type == typeof(Guid))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadGuid").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteGuid").First();
            }
            else if (type == typeof(IPAddress))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadIP").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteIP").First();
            }
            else if (type == typeof(IPEndPoint))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadIPEndpoint").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteIPEndpoint").First();
            }
            else if (type == typeof(Int64))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadLong").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteLong").First();
            }
            else if (type == typeof(UInt64))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadULong").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteULong").First();
            }
            else if (type == typeof(String))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadString").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteString").First();
            }
            else if (type == typeof(TimeSpan))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadTimeSpan").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), "WriteTimeSpan").First();
            }
            //
            // Arrays
            //
            else if (type == typeof(Int32[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadInt32Array").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(char[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadCharArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(UInt32[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadUInt32Array").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(Boolean[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadBooleanArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(Byte[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadByteArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(Byte[][]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadByteArrayArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(DateTime[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDateTimeArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(Double[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDoubleArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(float[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadFloatArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(Guid[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadGuidArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(IPAddress[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadIPArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(IPEndPoint[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadIPEndPointArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(Int64[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadInt64Array").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(UInt64[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadUInt64Array").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(Int16[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadShortArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(UInt16[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadUShortArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(String[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadStringArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(Decimal[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDecimalArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            else if (type == typeof(TimeSpan[]))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadTimeSpanArray").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateArrayPredicate<Tw>()).First();
            }
            //
            //  Collections
            //
            else if (type == typeof(IEnumerable<Int32>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadInt32Collection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<char>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadCharCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<UInt32>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadUInt32Collection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<Boolean>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadBooleanCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<Byte>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadByteCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<Byte[]>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadByteArrayCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<DateTime>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDateTimeCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<Double>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDoubleCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<float>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadFloatCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<Guid>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadGuidCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<IPAddress>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadIPCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<IPEndPoint>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadIPEndPointCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<Int64>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadInt64Collection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<UInt64>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadUInt64Collection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<Int16>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadShortCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<UInt16>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadUShortCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<String>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadStringCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<Decimal>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadDecimalCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            else if (type == typeof(IEnumerable<TimeSpan>))
            {
                wrapper.ReadId = wrapper.Invoker.Configure(typeof(MemoryStreamReader), "ReadTimeSpanCollection").First();
                wrapper.WriteId = wrapper.Invoker.Configure(typeof(MemoryStreamWriter), CreateCollectionPredicate<Tw>()).First();
            }
            //
            //  Not supported
            //
            else
            {
                throw new NotSupportedException($"Type {type.Name} not supported");
            }
            return wrapper;
        }

        private static Func<MethodInfo, bool> CreateCollectionPredicate<T>()
        {
            var typeArg = typeof(T).GetGenericArguments().First();
            return mi => mi.Name.Equals("WriteCollection", StringComparison.Ordinal) &&
            mi.GetParameters().First().ParameterType.GetGenericArguments().First().IsAssignableFrom(typeArg);
        }

        private static Func<MethodInfo, bool> CreateArrayPredicate<T>()
        {
            var typeArg = typeof(T).GetElementType();
            return mi => mi.Name.Equals("WriteArray", StringComparison.Ordinal) &&
                mi.GetParameters().First().ParameterType.GetElementType().IsAssignableFrom(typeArg);
        }


        private readonly static Dictionary<Type, Wrapper> _concrete_type_cachee = new Dictionary<Type, Wrapper>();
        private readonly static object _concrete_type_cachee_locker = new object();

        private static Wrapper Find<T>()
        {
            return Find(typeof(T));
        }

        private static Wrapper Find(Type type)
        {
            if (_concrete_type_cachee.ContainsKey(type) == false)
            {
                lock (_concrete_type_cachee_locker)
                {
                    if (_concrete_type_cachee.ContainsKey(type) == false)
                    {
                        if (_cachee.ContainsKey(type))
                        {
                            _concrete_type_cachee[type] = _cachee[type];
                        }
                        else if (TypeHelpers.IsAssignableToGenericType(type, typeof(IEnumerable<>)))
                        {
                            Type elementType;
                            var dict = _enumTypesCachee;
                            var writeName = "WriteCollection";
                            var readName = "ReadCollection";
                            if (TypeHelpers.IsArray(type))
                            {
                                elementType = type.GetElementType();
                                dict = _arrayTypesCachee;
                                writeName = "WriteArray";
                                readName = "ReadArray";
                            }
                            else
                            {
                                elementType = type.GetGenericArguments().First();
                            }
                            if (dict.ContainsKey(elementType))
                            {
                                _concrete_type_cachee[type] = _cachee[dict[elementType]];
                            }
                            else if (typeof(IBinarySerializable).IsAssignableFrom(elementType))
                            {
                                var wrapper = new Wrapper { Invoker = InvokeWrapper.Create() };

                                wrapper.ReadId = wrapper.Invoker.ConfigureGeneric(typeof(MemoryStreamReader), elementType, readName).First();
                                wrapper.WriteId = wrapper.Invoker.ConfigureGeneric(typeof(MemoryStreamWriter), elementType,
                                        mi => mi.Name.Equals(writeName) && mi.IsGenericMethod).First();
                                _concrete_type_cachee[type] = wrapper;
                            }
                        }
                    }
                }
            }
            if (_concrete_type_cachee.ContainsKey(type) == false)
            {
                throw new NotSupportedException($"Type {type.Name} not supported");
            }
            return _concrete_type_cachee[type];
        }

        #endregion Cachee

        public static T Deserialize<T>(IBinaryReader reader)
        {
            var wrapper = Find<T>();
            return wrapper.Read<T>(reader);
        }

        public static void Serialize<T>(IBinaryWriter writer, T value)
        {
            var wrapper = Find<T>();
            wrapper.Write<T>(writer, value);
        }

        public static object Deserialize(IBinaryReader reader, Type type)
        {
            var wrapper = Find(type);
            return wrapper.ReadObject(reader);
        }

        public static void Serialize(IBinaryWriter writer, object value)
        {
            var wrapper = Find(value.GetType());
            wrapper.WriteObject(writer, value);
        }
    }
}