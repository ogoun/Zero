using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using ZeroLevel.Services.Extensions;
using ZeroLevel.Services.Memory;

namespace ZeroLevel.Services.Serialization
{
    /// <summary>
    /// A wrapper over a MemoryStream for reading, with a check for overflow
    /// </summary>
    public partial class MemoryStreamReader
        : IBinaryReader
    {
        private readonly IViewAccessor _accessor;
        private bool _reverseByteOrder = false;

        public void ReverseByteOrder(bool use_reverse_byte_order)
        {
            _reverseByteOrder = use_reverse_byte_order;
        }

        /// <summary>
        /// End of stream
        /// </summary>
        public bool EOS => _accessor.EOV;

        public MemoryStreamReader(byte[] data)
        {
            if (data == null!)
                throw new ArgumentNullException(nameof(data));
            _accessor = new StreamVewAccessor(new MemoryStream(data));
        }

        public MemoryStreamReader(Stream stream)
        {
            if (stream == null!)
                throw new ArgumentNullException(nameof(stream));
            _accessor = new StreamVewAccessor(stream);
        }

        public MemoryStreamReader(MemoryStreamReader reader)
        {
            if (reader == null!)
                throw new ArgumentNullException(nameof(reader));
            _accessor = reader._accessor;
        }

        public MemoryStreamReader(IViewAccessor accessor)
        {
            if (accessor == null!)
                throw new ArgumentNullException(nameof(accessor));
            _accessor = accessor;
        }

        public long Position => _accessor.Position;

        public void SetPosition(long position) => _accessor.Seek(position);

        /// <summary>
        /// Flag reading
        /// </summary>
        public bool ReadBoolean()
        {
            return ReadByte() != 0;
        }

        /// <summary>
        /// Reading byte
        /// </summary>
        public byte ReadByte()
        {
            var buffer = ReadBuffer(1);
            return buffer[0];
        }

        /// <summary>
        /// Reading sbyte
        /// </summary>
        public sbyte ReadSByte()
        {
            return unchecked((sbyte)ReadByte());
        }

        public char ReadChar()
        {
            var buffer = ReadBuffer(2);
            return BitConverter.ToChar(buffer, 0);
        }

        /// <summary>
        /// Reading bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ReadBytes()
        {
            var length = BitConverter.ToInt32(ReadBuffer(4), 0);
            if (length == 0) return new byte[0];
            return ReadBuffer(length);
        }

        public short ReadShort()
        {
            var buffer = ReadBuffer(2);
            return BitConverter.ToInt16(buffer, 0);
        }

        public ushort ReadUShort()
        {
            var buffer = ReadBuffer(2);
            return BitConverter.ToUInt16(buffer, 0);
        }

        /// <summary>
        /// Read 32-bit integer (4 bytes)
        /// </summary>
        public Int32 ReadInt32()
        {
            var buffer = ReadBuffer(4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public UInt32 ReadUInt32()
        {
            var buffer = ReadBuffer(4);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public decimal ReadDecimal()
        {
            var arr = ReadBuffer(16);
            var p1 = BitConverter.ToInt32(arr, 0);
            var p2 = BitConverter.ToInt32(arr, 4);
            var p3 = BitConverter.ToInt32(arr, 8);
            var p4 = BitConverter.ToInt32(arr, 12);
            return BitConverterExt.ToDecimal(new int[] { p1, p2, p3, p4 });
        }

        /// <summary>
        /// Read integer 64-bit number (8 bytes)
        /// </summary>
        public Int64 ReadLong()
        {
            var buffer = ReadBuffer(8);
            return BitConverter.ToInt64(buffer, 0);
        }

        public UInt64 ReadULong()
        {
            var buffer = ReadBuffer(8);
            return BitConverter.ToUInt64(buffer, 0);
        }

        public TimeSpan ReadTimeSpan()
        {
            return new TimeSpan(ReadLong());
        }

        public float ReadFloat()
        {
            var buffer = ReadBuffer(4);
            return BitConverter.ToSingle(buffer, 0);
        }

        public double ReadDouble()
        {
            var buffer = ReadBuffer(8);
            return BitConverter.ToDouble(buffer, 0);
        }

        /// <summary>
        /// Read string (4 bytes per length + Length bytes)
        /// </summary>
        public string ReadString()
        {
            var length = BitConverter.ToInt32(ReadBuffer(4), 0);
            if (length == 0) return null!;
            var buffer = ReadBuffer(length);
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Read GUID (16 bytes)
        /// </summary>
        public Guid ReadGuid()
        {
            var buffer = ReadBuffer(16);
            return new Guid(buffer);
        }

        /// <summary>
        ///  Reading byte-package (read the size of the specified number of bytes, and then the packet itself read size)
        /// </summary>
        public byte[] ReadBuffer(int count)
        {
            if (CheckOutOfRange(count))
                throw new OutOfMemoryException("Array index out of bounds");
            var buffer = _accessor.ReadBufferSync(count);
            if (_reverseByteOrder && count > 1)
            {
                byte b;
                for (int i = 0; i < (count >> 1); i++)
                {
                    b = buffer[i];
                    buffer[i] = buffer[count - i - 1];
                    buffer[count - i - 1] = b;
                }
            }
            return buffer;
        }

        public bool TryReadBuffer(int count, out byte[] buffer)
        {
            if (CheckOutOfRange(count))
            {
                buffer = null!;
                return false;
            }
            try
            {
                buffer = _accessor.ReadBufferSync(count);
                if (_reverseByteOrder && count > 1)
                {
                    byte b;
                    for (int i = 0; i < (count >> 1); i++)
                    {
                        b = buffer[i];
                        buffer[i] = buffer[count - i - 1];
                        buffer[count - i - 1] = b;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[MemoryStreamReader.TryReadBuffer] Fault read {count} bytes");
                buffer = null!;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Reading the datetime
        /// </summary>
        /// <returns></returns>
        public DateTime? ReadDateTime()
        {
            var is_null = ReadByte();
            if (is_null == 0) return null!;
            var buffer = ReadBuffer(8);
            long deserialized = BitConverter.ToInt64(buffer, 0);
            return DateTime.FromBinary(deserialized);
        }

        public DateTimeOffset? ReadDateTimeOffset()
        {
            var is_null = ReadByte();
            if (is_null == 0) return null!;
            var buffer = ReadBuffer(16);
            long ticks = BitConverter.ToInt64(buffer, 0);
            long offsetTicks = BitConverter.ToInt64(buffer, 8);
            return new DateTimeOffset(ticks, new TimeSpan(offsetTicks));
        }

        public IPAddress ReadIP()
        {
            var exists = ReadByte();
            if (exists == 1)
            {
                var addr = ReadBytes();
                return new IPAddress(addr);
            }
            return null!;
        }

        public IPEndPoint ReadIPEndpoint()
        {
            var exists = ReadByte();
            if (exists == 1)
            {
                var addr = ReadIP();
                var port = ReadInt32();
                return new IPEndPoint(addr, port);
            }
            return null!;
        }

        public Uri ReadUri()
        {
            var s = ReadString();
            if (s == null!) return null!;
            return new Uri(s, UriKind.RelativeOrAbsolute);
        }

        public Version ReadVersion()
        {
            if (ReadByte() == 0) return null!;
            int major = ReadInt32();
            int minor = ReadInt32();
            int build = ReadInt32();
            int revision = ReadInt32();
            if (build < 0) return new Version(major, minor);
            if (revision < 0) return new Version(major, minor, build);
            return new Version(major, minor, build, revision);
        }

        public BitArray ReadBitArray()
        {
            int length = ReadInt32();
            if (length == -1) return null!;
            if (length == 0) return new BitArray(0);
            var bytes = ReadBuffer((length + 7) >> 3);
            var bits = new BitArray(bytes);
            bits.Length = length; // trim padding bits in the last byte
            return bits;
        }

        public T ReadEnum<T>() where T : struct, Enum
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.Byte: { var v = ReadByte(); return Unsafe.As<byte, T>(ref v); }
                case TypeCode.SByte: { var v = ReadSByte(); return Unsafe.As<sbyte, T>(ref v); }
                case TypeCode.Int16: { var v = ReadShort(); return Unsafe.As<short, T>(ref v); }
                case TypeCode.UInt16: { var v = ReadUShort(); return Unsafe.As<ushort, T>(ref v); }
                case TypeCode.Int32: { var v = ReadInt32(); return Unsafe.As<int, T>(ref v); }
                case TypeCode.UInt32: { var v = ReadUInt32(); return Unsafe.As<uint, T>(ref v); }
                case TypeCode.Int64: { var v = ReadLong(); return Unsafe.As<long, T>(ref v); }
                case TypeCode.UInt64: { var v = ReadULong(); return Unsafe.As<ulong, T>(ref v); }
                default: throw new NotSupportedException($"Enum {typeof(T).Name} has unsupported underlying type {underlyingType.Name}");
            }
        }

        #region Nullable primitives
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool? ReadBooleanNullable() => ReadByte() == 0 ? (bool?)null : ReadBoolean();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte? ReadByteNullable() => ReadByte() == 0 ? (byte?)null : ReadByte();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte? ReadSByteNullable() => ReadByte() == 0 ? (sbyte?)null : ReadSByte();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char? ReadCharNullable() => ReadByte() == 0 ? (char?)null : ReadChar();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short? ReadShortNullable() => ReadByte() == 0 ? (short?)null : ReadShort();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort? ReadUShortNullable() => ReadByte() == 0 ? (ushort?)null : ReadUShort();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int? ReadInt32Nullable() => ReadByte() == 0 ? (int?)null : ReadInt32();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint? ReadUInt32Nullable() => ReadByte() == 0 ? (uint?)null : ReadUInt32();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long? ReadLongNullable() => ReadByte() == 0 ? (long?)null : ReadLong();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong? ReadULongNullable() => ReadByte() == 0 ? (ulong?)null : ReadULong();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float? ReadFloatNullable() => ReadByte() == 0 ? (float?)null : ReadFloat();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double? ReadDoubleNullable() => ReadByte() == 0 ? (double?)null : ReadDouble();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal? ReadDecimalNullable() => ReadByte() == 0 ? (decimal?)null : ReadDecimal();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSpan? ReadTimeSpanNullable() => ReadByte() == 0 ? (TimeSpan?)null : ReadTimeSpan();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Guid? ReadGuidNullable() => ReadByte() == 0 ? (Guid?)null : ReadGuid();
        #endregion

        /// <summary>
        /// Check if data reading is outside the stream
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckOutOfRange(int offset) => _accessor.CheckOutOfRange(offset);

        #region Extensions

        #region Collections
        private List<T> ReadList<T>(Func<T> read)
        {
            int count = ReadInt32();
            var collection = new List<T>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(read.Invoke());
                }
            }
            return collection;
        }

        public List<T> ReadCollection<T>()
            where T : IBinarySerializable, new()
        =>
            ReadList(() =>
            {
                var item = new T();
                item.Deserialize(this);
                return item;
            });

        public List<string> ReadStringCollection() => ReadList(ReadString);

        public List<IPAddress> ReadIPCollection() => ReadList(ReadIP);

        public List<IPEndPoint> ReadIPEndPointCollection() => ReadList(ReadIPEndpoint);

        public List<Uri> ReadUriCollection() => ReadList(ReadUri);

        public List<Version> ReadVersionCollection() => ReadList(ReadVersion);

        public List<BitArray> ReadBitArrayCollection() => ReadList(ReadBitArray);

        public List<Guid> ReadGuidCollection() => ReadList(ReadGuid);

        public List<DateTime?> ReadDateTimeCollection() => ReadList(ReadDateTime);

        public List<DateTimeOffset?> ReadDateTimeOffsetCollection() => ReadList(ReadDateTimeOffset);

        public List<Int64> ReadInt64Collection() => ReadList(ReadLong);

        public List<Int32> ReadInt32Collection() => ReadList(ReadInt32);

        public List<UInt64> ReadUInt64Collection() => ReadList(ReadULong);

        public List<UInt32> ReadUInt32Collection() => ReadList(ReadUInt32);

        public List<char> ReadCharCollection() => ReadList(ReadChar);

        public List<short> ReadShortCollection() => ReadList(ReadShort);

        public List<ushort> ReadUShortCollection() => ReadList(ReadUShort);

        public List<float> ReadFloatCollection() => ReadList(ReadFloat);

        public List<Double> ReadDoubleCollection() => ReadList(ReadDouble);

        public List<bool> ReadBooleanCollection() => ReadList(ReadBoolean);

        public List<byte> ReadByteCollection() => ReadList(ReadByte);

        public List<byte[]> ReadByteArrayCollection() => ReadList(ReadBytes);

        public List<sbyte> ReadSByteCollection() => ReadList(ReadSByte);

        public List<decimal> ReadDecimalCollection() => ReadList(ReadDecimal);

        public List<TimeSpan> ReadTimeSpanCollection() => ReadList(ReadTimeSpan);
        #endregion

        #region Collections lazy

        private IEnumerable<T> ReadEnumerable<T>(Func<T> read)
        {
            int count = ReadInt32();
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    yield return read.Invoke();
                }
            }
        }

        public IEnumerable<T> ReadCollectionLazy<T>()
            where T : IBinarySerializable, new()
            =>
            ReadEnumerable<T>(() =>
            {
                var item = new T();
                item.Deserialize(this);
                return item;
            });

        public IEnumerable<string> ReadStringCollectionLazy() => ReadEnumerable(ReadString);

        public IEnumerable<IPAddress> ReadIPCollectionLazy() => ReadEnumerable(ReadIP);
        public IEnumerable<IPEndPoint> ReadIPEndPointCollectionLazy() => ReadEnumerable(ReadIPEndpoint);

        public IEnumerable<Uri> ReadUriCollectionLazy() => ReadEnumerable(ReadUri);

        public IEnumerable<Version> ReadVersionCollectionLazy() => ReadEnumerable(ReadVersion);

        public IEnumerable<BitArray> ReadBitArrayCollectionLazy() => ReadEnumerable(ReadBitArray);

        public IEnumerable<Guid> ReadGuidCollectionLazy() => ReadEnumerable(ReadGuid);

        public IEnumerable<DateTime?> ReadDateTimeCollectionLazy() => ReadEnumerable(ReadDateTime);

        public IEnumerable<DateTimeOffset?> ReadDateTimeOffsetCollectionLazy() => ReadEnumerable(ReadDateTimeOffset);

        public IEnumerable<Int64> ReadInt64CollectionLazy() => ReadEnumerable(ReadLong);

        public IEnumerable<Int32> ReadInt32CollectionLazy() => ReadEnumerable(ReadInt32);

        public IEnumerable<UInt64> ReadUInt64CollectionLazy() => ReadEnumerable(ReadULong);

        public IEnumerable<UInt32> ReadUInt32CollectionLazy() => ReadEnumerable(ReadUInt32);

        public IEnumerable<char> ReadCharCollectionLazy() => ReadEnumerable(ReadChar);

        public IEnumerable<short> ReadShortCollectionLazy() => ReadEnumerable(ReadShort);

        public IEnumerable<ushort> ReadUShortCollectionLazy() => ReadEnumerable(ReadUShort);

        public IEnumerable<float> ReadFloatCollectionLazy() => ReadEnumerable(ReadFloat);

        public IEnumerable<Double> ReadDoubleCollectionLazy() => ReadEnumerable(ReadDouble);

        public IEnumerable<bool> ReadBooleanCollectionLazy() => ReadEnumerable(ReadBoolean);

        public IEnumerable<byte> ReadByteCollectionLazy() => ReadEnumerable(ReadByte);

        public IEnumerable<byte[]> ReadByteArrayCollectionLazy() => ReadEnumerable(ReadBytes);

        public IEnumerable<sbyte> ReadSByteCollectionLazy() => ReadEnumerable(ReadSByte);

        public IEnumerable<decimal> ReadDecimalCollectionLazy() => ReadEnumerable(ReadDecimal);

        public IEnumerable<TimeSpan> ReadTimeSpanCollectionLazy() => ReadEnumerable(ReadTimeSpan);
        #endregion

        #region Arrays
        private T[] ReadArray<T>(Func<T> read)
        {
            int count = ReadInt32();
            var array = new T[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = read.Invoke();
                }
            }
            return array;
        }


        public T[] ReadArray<T>()
            where T : IBinarySerializable, new()
            =>
            ReadArray(() =>
            {
                var item = new T();
                item.Deserialize(this);
                return item;
            });

        public string[] ReadStringArray() => ReadArray(ReadString);

        public IPAddress[] ReadIPArray() => ReadArray(ReadIP);

        public IPEndPoint[] ReadIPEndPointArray() => ReadArray(ReadIPEndpoint);

        public Uri[] ReadUriArray() => ReadArray(ReadUri);

        public Version[] ReadVersionArray() => ReadArray(ReadVersion);

        public BitArray[] ReadBitArrayArray() => ReadArray(ReadBitArray);

        public Guid[] ReadGuidArray() => ReadArray(ReadGuid);

        public DateTime?[] ReadDateTimeArray() => ReadArray(ReadDateTime);

        public DateTimeOffset?[] ReadDateTimeOffsetArray() => ReadArray(ReadDateTimeOffset);

        public Int64[] ReadInt64Array() => ReadArray(ReadLong);

        public Int32[] ReadInt32Array() => ReadArray(ReadInt32);

        public UInt64[] ReadUInt64Array() => ReadArray(ReadULong);

        public UInt32[] ReadUInt32Array() => ReadArray(ReadUInt32);

        public char[] ReadCharArray() => ReadArray(ReadChar);

        public short[] ReadShortArray() => ReadArray(ReadShort);

        public ushort[] ReadUShortArray() => ReadArray(ReadUShort);

        public float[] ReadFloatArray() => ReadArray(ReadFloat);

        public Double[] ReadDoubleArray() => ReadArray(ReadDouble);

        public bool[] ReadBooleanArray() => ReadArray(ReadBoolean);

        public byte[] ReadByteArray() => ReadBytes();

        public byte[][] ReadByteArrayArray() => ReadArray(ReadBytes);

        public sbyte[] ReadSByteArray() => ReadArray(ReadSByte);

        public decimal[] ReadDecimalArray() => ReadArray(ReadDecimal);

        public TimeSpan[] ReadTimeSpanArray() => ReadArray(ReadTimeSpan);
        #endregion

        public KeyValuePair<TKey, TValue> ReadKeyValuePair<TKey, TValue>()
        {
            var keyDeser = MessageSerializer.GetDeserializer<TKey>();
            var valDeser = MessageSerializer.GetDeserializer<TValue>();
            var key = keyDeser(this);
            var val = valDeser(this);
            return new KeyValuePair<TKey, TValue>(key, val);
        }

        public (T1, T2) ReadValueTuple<T1, T2>()
        {
            var d1 = MessageSerializer.GetDeserializer<T1>();
            var d2 = MessageSerializer.GetDeserializer<T2>();
            var i1 = d1(this);
            var i2 = d2(this);
            return (i1, i2);
        }

        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>()
        {
            int count = ReadInt32();
            var collection = new Dictionary<TKey, TValue>(count);
            if (count > 0)
            {
                TKey key;
                TValue value;
                for (int i = 0; i < count; i++)
                {
                    key = ReadCompatible<TKey>();
                    value = ReadCompatible<TValue>();
                    collection.Add(key, value);
                }
            }
            return collection;
        }

        public ConcurrentDictionary<TKey, TValue> ReadDictionaryAsConcurrent<TKey, TValue>()
        {
            int count = ReadInt32();
            var collection = new ConcurrentDictionary<TKey, TValue>();
            if (count > 0)
            {
                TKey key;
                TValue value;
                for (int i = 0; i < count; i++)
                {
                    key = ReadCompatible<TKey>();
                    value = ReadCompatible<TValue>();
                    collection.TryAdd(key, value);
                }
            }
            return collection;
        }

        public T ReadCompatible<T>()
        {
            return MessageSerializer.DeserializeCompatible<T>(this);
        }

        public T Read<T>() where T : IBinarySerializable
        {
            byte type = ReadByte();
            if (type == 0) return default(T)!;
            var item = (T)Activator.CreateInstance<T>();
            item.Deserialize(this);
            return item;
        }

        public HashSet<T> ReadHashSet<T>() where T : IBinarySerializable, new()
        {
            int count = ReadInt32();
            var set = new HashSet<T>(count);
            for (int i = 0; i < count; i++)
            {
                var item = new T();
                item.Deserialize(this);
                set.Add(item);
            }
            return set;
        }

        public bool TryReadByte(out byte b)
        {
            if (TryReadBuffer(1, out var buffer))
            {
                b = buffer[0];
                return true;
            }
            b = default!;
            return false;
        }

        public bool TryRead<T>(out T item) where T : IBinarySerializable
        {
            if (TryReadByte(out var type))
            {
                if (type == 0)
                {
                    item = default(T)!;
                    return true;
                }
                try
                {
                    var o = (IBinarySerializable)FormatterServices.GetUninitializedObject(typeof(T));
                    o.Deserialize(this);
                    item = (T)o;
                    return true;
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[MemoryStreamReader.TryRead]");
                }
            }
            item = default!;
            return false;
        }

        public T Read<T>(object arg) where T : IBinarySerializable
        {
            byte type = ReadByte();
            if (type == 0) return default(T)!;
            var item = (T)Activator.CreateInstance(typeof(T), arg);
            item.Deserialize(this);
            return item;
        }
        #endregion Extensions

        public void Dispose()
        {
            _accessor.Dispose();
        }
    }

    public partial class MemoryStreamReader
     : IAsyncBinaryReader
    {
        /// <summary>
        ///  Reading byte-package (read the size of the specified number of bytes, and then the packet itself read size)
        /// </summary>
        public async Task<byte[]> ReadBufferAsync(int count)
        {
            if (CheckOutOfRange(count))
                throw new OutOfMemoryException("Array index out of bounds");
            var buffer = await _accessor.ReadBuffer(count);
            if (_reverseByteOrder && count > 1)
            {
                byte b;
                for (int i = 0; i < (count >> 1); i++)
                {
                    b = buffer[i];
                    buffer[i] = buffer[count - i - 1];
                    buffer[count - i - 1] = b;
                }
            }
            return buffer;
        }

        public async Task<bool> TryReadBufferAsync(int count, byte[] buffer)
        {
            if (CheckOutOfRange(count))
            {
                buffer = null!;
                return false;
            }
            try
            {
                buffer = await _accessor.ReadBuffer(count);
                if (_reverseByteOrder && count > 1)
                {
                    byte b;
                    for (int i = 0; i < (count >> 1); i++)
                    {
                        b = buffer[i];
                        buffer[i] = buffer[count - i - 1];
                        buffer[count - i - 1] = b;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[MemoryStreamReader.TryReadBufferAsync] Fault read {count} bytes");
                buffer = null!;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Flag reading
        /// </summary>
        public async Task<bool> ReadBooleanAsync()
        {
            return await ReadByteAsync() != 0;
        }

        /// <summary>
        /// Reading byte
        /// </summary>
        public async Task<byte> ReadByteAsync()
        {
            var buffer = await ReadBufferAsync(1);
            return buffer[0];
        }

        /// <summary>
        /// Reading sbyte
        /// </summary>
        public async Task<sbyte> ReadSByteAsync()
        {
            return unchecked((sbyte)(await ReadByteAsync()));
        }

        public async Task<char> ReadCharAsync()
        {
            var buffer = await ReadBufferAsync(2);
            return BitConverter.ToChar(buffer, 0);
        }

        /// <summary>
        /// Reading bytes
        /// </summary>
        /// <returns></returns>
        public async Task<byte[]> ReadBytesAsync()
        {
            var length = BitConverter.ToInt32(await ReadBufferAsync(4), 0);
            if (length == 0) return new byte[0];
            return ReadBuffer(length);
        }

        public async Task<short> ReadShortAsync()
        {
            var buffer = await ReadBufferAsync(2);
            return BitConverter.ToInt16(buffer, 0);
        }

        public async Task<ushort> ReadUShortAsync()
        {
            var buffer = await ReadBufferAsync(2);
            return BitConverter.ToUInt16(buffer, 0);
        }

        /// <summary>
        /// Read 32-bit integer (4 bytes)
        /// </summary>
        public async Task<Int32> ReadInt32Async()
        {
            var buffer = await ReadBufferAsync(4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public async Task<UInt32> ReadUInt32Async()
        {
            var buffer = await ReadBufferAsync(4);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public async Task<decimal> ReadDecimalAsync()
        {
            var arr = await ReadBufferAsync(16);
            var p1 = BitConverter.ToInt32(arr, 0);
            var p2 = BitConverter.ToInt32(arr, 4);
            var p3 = BitConverter.ToInt32(arr, 8);
            var p4 = BitConverter.ToInt32(arr, 12);
            return BitConverterExt.ToDecimal(new int[] { p1, p2, p3, p4 });
        }

        /// <summary>
        /// Read integer 64-bit number (8 bytes)
        /// </summary>
        public async Task<Int64> ReadLongAsync()
        {
            var buffer = await ReadBufferAsync(8);
            return BitConverter.ToInt64(buffer, 0);
        }

        public async Task<UInt64> ReadULongAsync()
        {
            var buffer = await ReadBufferAsync(8);
            return BitConverter.ToUInt64(buffer, 0);
        }

        public async Task<TimeSpan> ReadTimeSpanAsync()
        {
            return new TimeSpan(await ReadLongAsync());
        }

        public async Task<float> ReadFloatAsync()
        {
            var buffer = await ReadBufferAsync(4);
            return BitConverter.ToSingle(buffer, 0);
        }

        public async Task<double> ReadDoubleAsync()
        {
            var buffer = await ReadBufferAsync(8);
            return BitConverter.ToDouble(buffer, 0);
        }

        /// <summary>
        /// Read string (4 bytes per length + Length bytes)
        /// </summary>
        public async Task<string> ReadStringAsync()
        {
            var length = BitConverter.ToInt32(await ReadBufferAsync(4), 0);
            if (length == 0) return null!;
            var buffer = await ReadBufferAsync(length);
            return Encoding.UTF8.GetString(buffer);
        }

        /// <summary>
        /// Read GUID (16 bytes)
        /// </summary>
        public async Task<Guid> ReadGuidAsync()
        {
            var buffer = await ReadBufferAsync(16);
            return new Guid(buffer);
        }

        /// <summary>
        /// Reading the datetime
        /// </summary>
        /// <returns></returns>
        public async Task<DateTime?> ReadDateTimeAsync()
        {
            var is_null = ReadByte();
            if (is_null == 0) return null!;
            var buffer = await ReadBufferAsync(8);
            long deserialized = BitConverter.ToInt64(buffer, 0);
            return DateTime.FromBinary(deserialized);
        }

        public async Task<DateTimeOffset?> ReadDateTimeOffsetAsync()
        {
            var is_null = ReadByte();
            if (is_null == 0) return null!;
            var buffer = await ReadBufferAsync(16);
            long ticks = BitConverter.ToInt64(buffer, 0);
            long offsetTicks = BitConverter.ToInt64(buffer, 8);
            return new DateTimeOffset(ticks, new TimeSpan(offsetTicks));
        }

        public async Task<IPAddress> ReadIPAsync()
        {
            var exists = await ReadByteAsync();
            if (exists == 1)
            {
                var addr = await ReadBytesAsync();
                return new IPAddress(addr);
            }
            return null!;
        }

        public async Task<IPEndPoint> ReadIPEndpointAsync()
        {
            var exists = await ReadByteAsync();
            if (exists == 1)
            {
                var addr = await ReadIPAsync();
                var port = await ReadInt32Async();
                return new IPEndPoint(addr, port);
            }
            return null!;
        }

        public async Task<Uri> ReadUriAsync()
        {
            var s = await ReadStringAsync();
            if (s == null!) return null!;
            return new Uri(s, UriKind.RelativeOrAbsolute);
        }

        public async Task<Version> ReadVersionAsync()
        {
            if (await ReadByteAsync() == 0) return null!;
            int major = await ReadInt32Async();
            int minor = await ReadInt32Async();
            int build = await ReadInt32Async();
            int revision = await ReadInt32Async();
            if (build < 0) return new Version(major, minor);
            if (revision < 0) return new Version(major, minor, build);
            return new Version(major, minor, build, revision);
        }

        public async Task<BitArray> ReadBitArrayAsync()
        {
            int length = await ReadInt32Async();
            if (length == -1) return null!;
            if (length == 0) return new BitArray(0);
            var bytes = await ReadBufferAsync((length + 7) >> 3);
            var bits = new BitArray(bytes);
            bits.Length = length;
            return bits;
        }

        public async Task<T> ReadEnumAsync<T>() where T : struct, Enum
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.Byte: { var v = await ReadByteAsync(); return Unsafe.As<byte, T>(ref v); }
                case TypeCode.SByte: { var v = await ReadSByteAsync(); return Unsafe.As<sbyte, T>(ref v); }
                case TypeCode.Int16: { var v = await ReadShortAsync(); return Unsafe.As<short, T>(ref v); }
                case TypeCode.UInt16: { var v = await ReadUShortAsync(); return Unsafe.As<ushort, T>(ref v); }
                case TypeCode.Int32: { var v = await ReadInt32Async(); return Unsafe.As<int, T>(ref v); }
                case TypeCode.UInt32: { var v = await ReadUInt32Async(); return Unsafe.As<uint, T>(ref v); }
                case TypeCode.Int64: { var v = await ReadLongAsync(); return Unsafe.As<long, T>(ref v); }
                case TypeCode.UInt64: { var v = await ReadULongAsync(); return Unsafe.As<ulong, T>(ref v); }
                default: throw new NotSupportedException($"Enum {typeof(T).Name} has unsupported underlying type {underlyingType.Name}");
            }
        }

        #region Nullable primitives (async)
        public async Task<bool?> ReadBooleanNullableAsync() => await ReadByteAsync() == 0 ? (bool?)null : await ReadBooleanAsync();
        public async Task<byte?> ReadByteNullableAsync() => await ReadByteAsync() == 0 ? (byte?)null : await ReadByteAsync();
        public async Task<sbyte?> ReadSByteNullableAsync() => await ReadByteAsync() == 0 ? (sbyte?)null : await ReadSByteAsync();
        public async Task<char?> ReadCharNullableAsync() => await ReadByteAsync() == 0 ? (char?)null : await ReadCharAsync();
        public async Task<short?> ReadShortNullableAsync() => await ReadByteAsync() == 0 ? (short?)null : await ReadShortAsync();
        public async Task<ushort?> ReadUShortNullableAsync() => await ReadByteAsync() == 0 ? (ushort?)null : await ReadUShortAsync();
        public async Task<int?> ReadInt32NullableAsync() => await ReadByteAsync() == 0 ? (int?)null : await ReadInt32Async();
        public async Task<uint?> ReadUInt32NullableAsync() => await ReadByteAsync() == 0 ? (uint?)null : await ReadUInt32Async();
        public async Task<long?> ReadLongNullableAsync() => await ReadByteAsync() == 0 ? (long?)null : await ReadLongAsync();
        public async Task<ulong?> ReadULongNullableAsync() => await ReadByteAsync() == 0 ? (ulong?)null : await ReadULongAsync();
        public async Task<float?> ReadFloatNullableAsync() => await ReadByteAsync() == 0 ? (float?)null : await ReadFloatAsync();
        public async Task<double?> ReadDoubleNullableAsync() => await ReadByteAsync() == 0 ? (double?)null : await ReadDoubleAsync();
        public async Task<decimal?> ReadDecimalNullableAsync() => await ReadByteAsync() == 0 ? (decimal?)null : await ReadDecimalAsync();
        public async Task<TimeSpan?> ReadTimeSpanNullableAsync() => await ReadByteAsync() == 0 ? (TimeSpan?)null : await ReadTimeSpanAsync();
        public async Task<Guid?> ReadGuidNullableAsync() => await ReadByteAsync() == 0 ? (Guid?)null : await ReadGuidAsync();
        #endregion

        #region Extensions

        #region Collections
        private async Task<List<T>> ReadListAsync<T>(Func<Task<T>> readAsync, Func<T> read)
        {
            int count = await ReadInt32Async();
            var collection = new List<T>(count);
            if (count > 0)
            {
                if (_accessor.IsMemoryStream)
                {
                    for (int i = 0; i < count; i++)
                    {
                        collection.Add(read.Invoke());
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        collection.Add(await readAsync.Invoke());
                    }
                }
            }
            return collection;
        }

        public async Task<List<T>> ReadCollectionAsync<T>()
            where T : IAsyncBinarySerializable, new()
        {
            int count = await ReadInt32Async();
            var collection = new List<T>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var item = new T();
                    await item.DeserializeAsync(this);
                    collection.Add(item);
                }
            }
            return collection;
        }

        public async Task<List<string>> ReadStringCollectionAsync() => await ReadListAsync(ReadStringAsync, ReadString);

        public async Task<List<IPAddress>> ReadIPCollectionAsync() => await ReadListAsync(ReadIPAsync, ReadIP);

        public async Task<List<IPEndPoint>> ReadIPEndPointCollectionAsync() => await ReadListAsync(ReadIPEndpointAsync, ReadIPEndpoint);

        public async Task<List<Uri>> ReadUriCollectionAsync() => await ReadListAsync(ReadUriAsync, ReadUri);

        public async Task<List<Version>> ReadVersionCollectionAsync() => await ReadListAsync(ReadVersionAsync, ReadVersion);

        public async Task<List<BitArray>> ReadBitArrayCollectionAsync() => await ReadListAsync(ReadBitArrayAsync, ReadBitArray);

        public async Task<List<Guid>> ReadGuidCollectionAsync() => await ReadListAsync(ReadGuidAsync, ReadGuid);

        public async Task<List<DateTime?>> ReadDateTimeCollectionAsync() => await ReadListAsync(ReadDateTimeAsync, ReadDateTime);

        public async Task<List<DateTimeOffset?>> ReadDateTimeOffsetCollectionAsync() => await ReadListAsync(ReadDateTimeOffsetAsync, ReadDateTimeOffset);

        public async Task<List<Int64>> ReadInt64CollectionAsync() => await ReadListAsync(ReadLongAsync, ReadLong);

        public async Task<List<Int32>> ReadInt32CollectionAsync() => await ReadListAsync(ReadInt32Async, ReadInt32);

        public async Task<List<UInt64>> ReadUInt64CollectionAsync() => await ReadListAsync(ReadULongAsync, ReadULong);

        public async Task<List<UInt32>> ReadUInt32CollectionAsync() => await ReadListAsync(ReadUInt32Async, ReadUInt32);

        public async Task<List<char>> ReadCharCollectionAsync() => await ReadListAsync(ReadCharAsync, ReadChar);

        public async Task<List<short>> ReadShortCollectionAsync() => await ReadListAsync(ReadShortAsync, ReadShort);

        public async Task<List<ushort>> ReadUShortCollectionAsync() => await ReadListAsync(ReadUShortAsync, ReadUShort);

        public async Task<List<float>> ReadFloatCollectionAsync() => await ReadListAsync(ReadFloatAsync, ReadFloat);

        public async Task<List<Double>> ReadDoubleCollectionAsync() => await ReadListAsync(ReadDoubleAsync, ReadDouble);

        public async Task<List<bool>> ReadBooleanCollectionAsync() => await ReadListAsync(ReadBooleanAsync, ReadBoolean);

        public async Task<List<byte>> ReadByteCollectionAsync() => await ReadListAsync(ReadByteAsync, ReadByte);

        public async Task<List<byte[]>> ReadByteArrayCollectionAsync() => await ReadListAsync(ReadBytesAsync, ReadBytes);

        public async Task<List<sbyte>> ReadSByteCollectionAsync() => await ReadListAsync(ReadSByteAsync, ReadSByte);

        public async Task<List<decimal>> ReadDecimalCollectionAsync() => await ReadListAsync(ReadDecimalAsync, ReadDecimal);

        public async Task<List<TimeSpan>> ReadTimeSpanCollectionAsync() => await ReadListAsync(ReadTimeSpanAsync, ReadTimeSpan);

        #endregion

        #region Arrays
        private async Task<T[]> ReadArrayAsync<T>(Func<Task<T>> readAsync, Func<T> read)
        {
            int count = await ReadInt32Async();
            var array = new T[count];
            if (count > 0)
            {
                if (_accessor.IsMemoryStream)
                {
                    for (int i = 0; i < count; i++)
                    {
                        array[i] = read.Invoke();
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        array[i] = await readAsync.Invoke();
                    }
                }
            }
            return array;
        }
        public async Task<T[]> ReadArrayAsync<T>()
            where T : IAsyncBinarySerializable, new()
        {
            int count = ReadInt32();
            var array = new T[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var item = new T();
                    await item.DeserializeAsync(this);
                    array[i] = item;
                }
            }
            return array;
        }

        public async Task<string[]> ReadStringArrayAsync() => await ReadArrayAsync(ReadStringAsync, ReadString);

        public async Task<IPAddress[]> ReadIPArrayAsync() => await ReadArrayAsync(ReadIPAsync, ReadIP);

        public async Task<IPEndPoint[]> ReadIPEndPointArrayAsync() => await ReadArrayAsync(ReadIPEndpointAsync, ReadIPEndpoint);

        public async Task<Uri[]> ReadUriArrayAsync() => await ReadArrayAsync(ReadUriAsync, ReadUri);

        public async Task<Version[]> ReadVersionArrayAsync() => await ReadArrayAsync(ReadVersionAsync, ReadVersion);

        public async Task<BitArray[]> ReadBitArrayArrayAsync() => await ReadArrayAsync(ReadBitArrayAsync, ReadBitArray);

        public async Task<Guid[]> ReadGuidArrayAsync() => await ReadArrayAsync(ReadGuidAsync, ReadGuid);

        public async Task<DateTime?[]> ReadDateTimeArrayAsync() => await ReadArrayAsync(ReadDateTimeAsync, ReadDateTime);

        public async Task<DateTimeOffset?[]> ReadDateTimeArrayOffsetAsync() => await ReadArrayAsync(ReadDateTimeOffsetAsync, ReadDateTimeOffset);

        public async Task<Int64[]> ReadInt64ArrayAsync() => await ReadArrayAsync(ReadLongAsync, ReadLong);

        public async Task<Int32[]> ReadInt32ArrayAsync() => await ReadArrayAsync(ReadInt32Async, ReadInt32);

        public async Task<UInt64[]> ReadUInt64ArrayAsync() => await ReadArrayAsync(ReadULongAsync, ReadULong);

        public async Task<UInt32[]> ReadUInt32ArrayAsync() => await ReadArrayAsync(ReadUInt32Async, ReadUInt32);

        public async Task<char[]> ReadCharArrayAsync() => await ReadArrayAsync(ReadCharAsync, ReadChar);

        public async Task<short[]> ReadShortArrayAsync() => await ReadArrayAsync(ReadShortAsync, ReadShort);

        public async Task<ushort[]> ReadUShortArrayAsync() => await ReadArrayAsync(ReadUShortAsync, ReadUShort);

        public async Task<float[]> ReadFloatArrayAsync() => await ReadArrayAsync(ReadFloatAsync, ReadFloat);

        public async Task<Double[]> ReadDoubleArrayAsync() => await ReadArrayAsync(ReadDoubleAsync, ReadDouble);

        public async Task<bool[]> ReadBooleanArrayAsync() => await ReadArrayAsync(ReadBooleanAsync, ReadBoolean);

        public async Task<byte[]> ReadByteArrayAsync()
        {
            if (_accessor.IsMemoryStream)
            {
                return ReadBytes();
            }
            return await ReadBytesAsync();
        }

        public async Task<byte[][]> ReadByteArrayArrayAsync() => await ReadArrayAsync(ReadBytesAsync, ReadBytes);

        public async Task<sbyte[]> ReadSByteArrayAsync() => await ReadArrayAsync(ReadSByteAsync, ReadSByte);

        public async Task<decimal[]> ReadDecimalArrayAsync() => await ReadArrayAsync(ReadDecimalAsync, ReadDecimal);

        public async Task<TimeSpan[]> ReadTimeSpanArrayAsync() => await ReadArrayAsync(ReadTimeSpanAsync, ReadTimeSpan);

        #endregion

        public Task<KeyValuePair<TKey, TValue>> ReadKeyValuePairAsync<TKey, TValue>()
        {
            var keyDeser = MessageSerializer.GetDeserializer<TKey>();
            var valDeser = MessageSerializer.GetDeserializer<TValue>();
            var key = keyDeser(this);
            var val = valDeser(this);
            return Task.FromResult(new KeyValuePair<TKey, TValue>(key, val));
        }

        public Task<(T1, T2)> ReadValueTupleAsync<T1, T2>()
        {
            var d1 = MessageSerializer.GetDeserializer<T1>();
            var d2 = MessageSerializer.GetDeserializer<T2>();
            var i1 = d1(this);
            var i2 = d2(this);
            return Task.FromResult((i1, i2));
        }

        public async Task<Dictionary<TKey, TValue>> ReadDictionaryAsync<TKey, TValue>()
        {
            int count = ReadInt32();
            var collection = new Dictionary<TKey, TValue>(count);
            if (count > 0)
            {
                TKey key;
                TValue value;
                for (int i = 0; i < count; i++)
                {
                    key = await ReadCompatibleAsync<TKey>();
                    value = await ReadCompatibleAsync<TValue>();
                    collection.Add(key, value);
                }
            }
            return collection;
        }

        public async Task<ConcurrentDictionary<TKey, TValue>> ReadDictionaryAsConcurrentAsync<TKey, TValue>()
        {
            int count = ReadInt32();
            var collection = new ConcurrentDictionary<TKey, TValue>();
            if (count > 0)
            {
                TKey key;
                TValue value;
                for (int i = 0; i < count; i++)
                {
                    key = await ReadCompatibleAsync<TKey>();
                    value = await ReadCompatibleAsync<TValue>();
                    collection.TryAdd(key, value);
                }
            }
            return collection;
        }

        public async Task<T> ReadCompatibleAsync<T>()
        {
            return await MessageSerializer.DeserializeCompatibleAsync<T>(this);
        }

        public async Task<T> ReadAsync<T>() where T : IAsyncBinarySerializable
        {
            byte type = await ReadByteAsync();
            if (type == 0) return default(T)!;
            var item = (T)Activator.CreateInstance<T>();
            await item.DeserializeAsync(this);
            return item;
        }

        public async Task<HashSet<T>> ReadHashSetAsync<T>() where T : IAsyncBinarySerializable, new()
        {
            int count = await ReadInt32Async();
            var set = new HashSet<T>(count);
            for (int i = 0; i < count; i++)
            {
                var item = new T();
                await item.DeserializeAsync(this);
                set.Add(item);
            }
            return set;
        }

        public async Task<T> ReadAsync<T>(object arg) where T : IAsyncBinarySerializable
        {
            byte type = ReadByte();
            if (type == 0) return default(T)!;
            var item = (T)Activator.CreateInstance(typeof(T), arg);
            await item.DeserializeAsync(this);
            return item;
        }
        #endregion Extensions
    }

}