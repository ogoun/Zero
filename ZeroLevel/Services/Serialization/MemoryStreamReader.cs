using System;
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
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            _accessor = new StreamVewAccessor(new MemoryStream(data));
        }

        public MemoryStreamReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            _accessor = new StreamVewAccessor(stream);
        }

        public MemoryStreamReader(MemoryStreamReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            _accessor = reader._accessor;
        }

        public MemoryStreamReader(IViewAccessor accessor)
        {
            if (accessor == null)
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
            return BitConverter.ToBoolean(new byte[1] { ReadByte() }, 0);
        }

        /// <summary>
        /// Reading byte
        /// </summary>
        public byte ReadByte()
        {
            var buffer = ReadBuffer(1);
            return buffer[0];
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
            if (length == 0) return null;
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
            var buffer = _accessor.ReadBuffer(count).Result;
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
                buffer = null;
                return false;
            }
            try
            {
                buffer = _accessor.ReadBuffer(count).Result;
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
                buffer = null;
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
            if (is_null == 0) return null;
            var buffer = ReadBuffer(8);
            long deserialized = BitConverter.ToInt64(buffer, 0);
            return DateTime.FromBinary(deserialized);
        }
        public TimeOnly? ReadTime()
        {
            var is_null = ReadByte();
            if (is_null == 0) return null;
            var ts = ReadTimeSpan();
            return TimeOnly.FromTimeSpan(ts);
        }

        public DateOnly? ReadDate()
        {
            var is_null = ReadByte();
            if (is_null == 0) return null;
            var days = ReadInt32();
            return DateOnly.FromDayNumber(days);
        }
        public IPAddress ReadIP()
        {
            var exists = ReadByte();
            if (exists == 1)
            {
                var addr = ReadBytes();
                return new IPAddress(addr);
            }
            return null;
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
            return null;
        }

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

        public List<Guid> ReadGuidCollection() => ReadList(ReadGuid);

        public List<DateTime?> ReadDateTimeCollection() => ReadList(ReadDateTime);

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

        public IEnumerable<Guid> ReadGuidCollectionLazy() => ReadEnumerable(ReadGuid);

        public IEnumerable<DateTime?> ReadDateTimeCollectionLazy() => ReadEnumerable(ReadDateTime);

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

        public Guid[] ReadGuidArray() => ReadArray(ReadGuid);

        public DateTime?[] ReadDateTimeArray() => ReadArray(ReadDateTime);

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

        public decimal[] ReadDecimalArray() => ReadArray(ReadDecimal);

        public TimeSpan[] ReadTimeSpanArray() => ReadArray(ReadTimeSpan);
        #endregion

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
            if (type == 0) return default(T);
            var item = (T)Activator.CreateInstance<T>();
            item.Deserialize(this);
            return item;
        }

        public bool TryReadByte(out byte b)
        {
            if (TryReadBuffer(1, out var buffer))
            {
                b = buffer[0];
                return true;
            }
            b = default;
            return false;
        }

        public bool TryRead<T>(out T item) where T : IBinarySerializable
        {
            if (TryReadByte(out var type))
            {
                if (type == 0)
                {
                    item = default(T);
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
            item = default;
            return false;
        }

        public T Read<T>(object arg) where T : IBinarySerializable
        {
            byte type = ReadByte();
            if (type == 0) return default(T);
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
                buffer = null;
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
                buffer = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Flag reading
        /// </summary>
        public async Task<bool> ReadBooleanAsync()
        {
            return BitConverter.ToBoolean(new byte[1] { await ReadByteAsync() }, 0);
        }

        /// <summary>
        /// Reading byte
        /// </summary>
        public async Task<byte> ReadByteAsync()
        {
            var buffer = await ReadBufferAsync(1);
            return buffer[0];
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
            if (length == 0) return null;
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
            if (is_null == 0) return null;
            var buffer = await ReadBufferAsync(8);
            long deserialized = BitConverter.ToInt64(buffer, 0);
            return DateTime.FromBinary(deserialized);
        }
        public async Task<TimeOnly?> ReadTimeAsync()
        {
            var is_null = await ReadByteAsync();
            if (is_null == 0) return null;
            var ts = await ReadTimeSpanAsync();
            return TimeOnly.FromTimeSpan(ts);
        }

        public async Task<DateOnly?> ReadDateAsync()
        {
            var is_null = await ReadByteAsync();
            if (is_null == 0) return null;
            var days = await ReadInt32Async();
            return DateOnly.FromDayNumber(days);
        }
        public async Task<IPAddress> ReadIPAsync()
        {
            var exists = await ReadByteAsync();
            if (exists == 1)
            {
                var addr = await ReadBytesAsync();
                return new IPAddress(addr);
            }
            return null;
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
            return null;
        }

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

        public async Task<List<Guid>> ReadGuidCollectionAsync() => await ReadListAsync(ReadGuidAsync, ReadGuid);

        public async Task<List<DateTime?>> ReadDateTimeCollectionAsync() => await ReadListAsync(ReadDateTimeAsync, ReadDateTime);

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

        public async Task<Guid[]> ReadGuidArrayAsync() => await ReadArrayAsync(ReadGuidAsync, ReadGuid);

        public async Task<DateTime?[]> ReadDateTimeArrayAsync() => await ReadArrayAsync(ReadDateTimeAsync, ReadDateTime);

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

        public async Task<decimal[]> ReadDecimalArrayAsync() => await ReadArrayAsync(ReadDecimalAsync, ReadDecimal);

        public async Task<TimeSpan[]> ReadTimeSpanArrayAsync() => await ReadArrayAsync(ReadTimeSpanAsync, ReadTimeSpan);

        #endregion

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
            if (type == 0) return default(T);
            var item = (T)Activator.CreateInstance<T>();
            await item.DeserializeAsync(this);
            return item;
        }

        public async Task<T> ReadAsync<T>(object arg) where T : IAsyncBinarySerializable
        {
            byte type = ReadByte();
            if (type == 0) return default(T);
            var item = (T)Activator.CreateInstance(typeof(T), arg);
            await item.DeserializeAsync(this);
            return item;
        }
        #endregion Extensions
    }

}