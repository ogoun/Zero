using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using ZeroLevel.Services.Extensions;

namespace ZeroLevel.Services.Serialization
{
    /// <summary>
    /// A wrapper over a MemoryStream for reading, with a check for overflow
    /// </summary>
    public sealed class MemoryStreamReader
        : IBinaryReader
    {
        private readonly Stream _stream;
        private bool _reverseByteOrder = false;

        public void ReverseByteOrder(bool use_reverse_byte_order)
        {
            _reverseByteOrder = use_reverse_byte_order;
        }

        /// <summary>
        /// End of stream
        /// </summary>
        public bool EOS => _stream.Position >= _stream.Length;

        public MemoryStreamReader(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            _stream = new MemoryStream(data);
        }

        public MemoryStreamReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            _stream = stream;
        }

        /// <summary>
        /// Flag reading
        /// </summary>
        public bool ReadBoolean()
        {
            if (CheckOutOfRange(1))
                throw new OutOfMemoryException("Array index out of bounds");
            return BitConverter.ToBoolean(new byte[1] { ReadByte() }, 0);
        }

        /// <summary>
        /// Reading byte
        /// </summary>
        public byte ReadByte()
        {
            if (CheckOutOfRange(1))
                throw new OutOfMemoryException("Array index out of bounds");
            return (byte)_stream.ReadByte();
        }

        public char ReadChar()
        {
            if (CheckOutOfRange(2))
                throw new OutOfMemoryException("Array index out of bounds");
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
            var p1 = ReadInt32();
            var p2 = ReadInt32();
            var p3 = ReadInt32();
            var p4 = ReadInt32();
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
            if (count == 0) return null;
            if (CheckOutOfRange(count))
                throw new OutOfMemoryException("Array index out of bounds");
            var buffer = new byte[count];
            var readedCount = _stream.Read(buffer, 0, count);
            if (count != readedCount)
                throw new InvalidOperationException($"The stream returned less data ({count} bytes) than expected ({readedCount} bytes)");
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
        public bool CheckOutOfRange(int offset)
        {
            return (_stream.Position + offset) > _stream.Length;
        }        

        #region Extensions

        #region Collections
        public List<T> ReadCollection<T>()
            where T : IBinarySerializable, new()
        {
            int count = ReadInt32();
            var collection = new List<T>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var item = new T();
                    item.Deserialize(this);
                    collection.Add(item);
                }
            }
            return collection;
        }

        public List<string> ReadStringCollection()
        {
            int count = ReadInt32();
            var collection = new List<string>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadString());
                }
            }
            return collection;
        }

        public List<IPAddress> ReadIPCollection()
        {
            int count = ReadInt32();
            var collection = new List<IPAddress>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadIP());
                }
            }
            return collection;
        }

        public List<IPEndPoint> ReadIPEndPointCollection()
        {
            int count = ReadInt32();
            var collection = new List<IPEndPoint>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadIPEndpoint());
                }
            }
            return collection;
        }

        public List<Guid> ReadGuidCollection()
        {
            int count = ReadInt32();
            var collection = new List<Guid>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadGuid());
                }
            }
            return collection;
        }

        public List<DateTime> ReadDateTimeCollection()
        {
            int count = ReadInt32();
            var collection = new List<DateTime>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadDateTime() ?? DateTime.MinValue);
                }
            }
            return collection;
        }

        public List<Int64> ReadInt64Collection()
        {
            int count = ReadInt32();
            var collection = new List<Int64>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadLong());
                }
            }
            return collection;
        }

        public List<Int32> ReadInt32Collection()
        {
            int count = ReadInt32();
            var collection = new List<Int32>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadInt32());
                }
            }
            return collection;
        }

        public List<UInt64> ReadUInt64Collection()
        {
            int count = ReadInt32();
            var collection = new List<UInt64>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadULong());
                }
            }
            return collection;
        }

        public List<UInt32> ReadUInt32Collection()
        {
            int count = ReadInt32();
            var collection = new List<UInt32>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadUInt32());
                }
            }
            return collection;
        }

        public List<char> ReadCharCollection()
        {
            int count = ReadInt32();
            var collection = new List<char>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadChar());
                }
            }
            return collection;
        }

        public List<short> ReadShortCollection()
        {
            int count = ReadInt32();
            var collection = new List<short>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadShort());
                }
            }
            return collection;
        }

        public List<ushort> ReadUShortCollection()
        {
            int count = ReadInt32();
            var collection = new List<ushort>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadUShort());
                }
            }
            return collection;
        }

        public List<float> ReadFloatCollection()
        {
            int count = ReadInt32();
            var collection = new List<float>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadFloat());
                }
            }
            return collection;
        }

        public List<Double> ReadDoubleCollection()
        {
            int count = ReadInt32();
            var collection = new List<Double>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadDouble());
                }
            }
            return collection;
        }

        public List<bool> ReadBooleanCollection()
        {
            int count = ReadInt32();
            var collection = new List<bool>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadBoolean());
                }
            }
            return collection;
        }

        public List<byte> ReadByteCollection()
        {
            int count = ReadInt32();
            var collection = new List<byte>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadByte());
                }
            }
            return collection;
        }

        public List<byte[]> ReadByteArrayCollection()
        {
            int count = ReadInt32();
            var collection = new List<byte[]>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadBytes());
                }
            }
            return collection;
        }

        public List<decimal> ReadDecimalCollection()
        {
            int count = ReadInt32();
            var collection = new List<decimal>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadDecimal());
                }
            }
            return collection;
        }

        public List<TimeSpan> ReadTimeSpanCollection()
        {
            int count = ReadInt32();
            var collection = new List<TimeSpan>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadTimeSpan());
                }
            }
            return collection;
        }
        #endregion

        #region Arrays
        public T[] ReadArray<T>()
            where T : IBinarySerializable, new()
        {
            int count = ReadInt32();
            var array = new T[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var item = new T();
                    item.Deserialize(this);
                    array[i] = item;
                }
            }
            return array;
        }

        public string[] ReadStringArray()
        {
            int count = ReadInt32();
            var array = new string[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadString();
                }
            }
            return array;
        }

        public IPAddress[] ReadIPArray()
        {
            int count = ReadInt32();
            var array = new IPAddress[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadIP();
                }
            }
            return array;
        }

        public IPEndPoint[] ReadIPEndPointArray()
        {
            int count = ReadInt32();
            var array = new IPEndPoint[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadIPEndpoint();
                }
            }
            return array;
        }

        public Guid[] ReadGuidArray()
        {
            int count = ReadInt32();
            var array = new Guid[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadGuid();
                }
            }
            return array;
        }

        public DateTime[] ReadDateTimeArray()
        {
            int count = ReadInt32();
            var array = new DateTime[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = (ReadDateTime() ?? DateTime.MinValue);
                }
            }
            return array;
        }

        public Int64[] ReadInt64Array()
        {
            int count = ReadInt32();
            var array = new Int64[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadLong();
                }
            }
            return array;
        }

        public Int32[] ReadInt32Array()
        {
            int count = ReadInt32();
            var array = new Int32[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadInt32();
                }
            }
            return array;
        }

        public UInt64[] ReadUInt64Array()
        {
            int count = ReadInt32();
            var array = new UInt64[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadULong();
                }
            }
            return array;
        }

        public UInt32[] ReadUInt32Array()
        {
            int count = ReadInt32();
            var array = new UInt32[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadUInt32();
                }
            }
            return array;
        }

        public char[] ReadCharArray()
        {
            int count = ReadInt32();
            var array = new char[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadChar();
                }
            }
            return array;
        }

        public short[] ReadShortArray()
        {
            int count = ReadInt32();
            var array = new short[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadShort();
                }
            }
            return array;
        }

        public ushort[] ReadUShortArray()
        {
            int count = ReadInt32();
            var array = new ushort[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadUShort();
                }
            }
            return array;
        }

        public float[] ReadFloatArray()
        {
            int count = ReadInt32();
            var array = new float[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadFloat();
                }
            }
            return array;
        }

        public Double[] ReadDoubleArray()
        {
            int count = ReadInt32();
            var array = new Double[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadDouble();
                }
            }
            return array;
        }

        public bool[] ReadBooleanArray()
        {
            int count = ReadInt32();
            var array = new bool[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadBoolean();
                }
            }
            return array;
        }

        public byte[] ReadByteArray()
        {
            return ReadBytes();
        }

        public byte[][] ReadByteArrayArray()
        {
            int count = ReadInt32();
            var array = new byte[count][];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadBytes();
                }
            }
            return array;
        }

        public decimal[] ReadDecimalArray()
        {
            int count = ReadInt32();
            var array = new decimal[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadDecimal();
                }
            }
            return array;
        }

        public TimeSpan[] ReadTimeSpanArray()
        {
            int count = ReadInt32();
            var array = new TimeSpan[count];
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    array[i] = ReadTimeSpan();
                }
            }
            return array;
        }
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
            _stream.Dispose();
        }

        public Stream Stream => _stream;
    }
}