using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel.Services.Extensions;

namespace ZeroLevel.Services.Serialization
{
    /// <summary>
    /// Wrapper over memorystream for writing
    /// </summary>
    public partial class MemoryStreamWriter :
        IBinaryWriter
    {
        private const byte ZERO = 0;
        private const byte ONE = 1;
        private const int BATCH_MEMORY_SIZE_LIMIT = 1024 * 1024; // 1Mb

        private long MockCount()
        {
            var savedPos = this._stream.Position;
            WriteInt32(0); // count mock
            return savedPos;
        }

        private void UpdateCount(long savedPos, int count)
        {
            var current_position = this._stream.Position;
            this._stream.Position = savedPos;
            WriteInt32(count);
            this._stream.Position = current_position;
        }

        public Stream Stream => _stream;

        private readonly Stream _stream;

        public MemoryStreamWriter()
        {
            _stream = new MemoryStream();
        }

        public MemoryStreamWriter(Stream stream)
        {
            _stream = stream;
        }

        public MemoryStreamWriter(MemoryStreamWriter writer)
        {
            _stream = writer._stream;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBoolean(bool val) => _stream.WriteByte(val ? ONE : ZERO);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte val) => _stream.WriteByte(val);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSByte(sbyte val) => _stream.WriteByte(unchecked((byte)val));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteShort(short number)
        {
            Span<byte> buf = stackalloc byte[2];
            BinaryPrimitives.WriteInt16LittleEndian(buf, number);
            _stream.Write(buf);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUShort(ushort number)
        {
            Span<byte> buf = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buf, number);
            _stream.Write(buf);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(Int32 number)
        {
            Span<byte> buf = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buf, number);
            _stream.Write(buf);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(UInt32 number)
        {
            Span<byte> buf = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(buf, number);
            _stream.Write(buf);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLong(Int64 number)
        {
            Span<byte> buf = stackalloc byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(buf, number);
            _stream.Write(buf);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteULong(UInt64 number)
        {
            Span<byte> buf = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buf, number);
            _stream.Write(buf);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTimeSpan(TimeSpan period) => WriteLong(period.Ticks);
        public void WriteDecimal(Decimal number)
        {
            Span<byte> buf = stackalloc byte[16];
            var bits = decimal.GetBits(number);
            BinaryPrimitives.WriteInt32LittleEndian(buf.Slice(0, 4), bits[0]);
            BinaryPrimitives.WriteInt32LittleEndian(buf.Slice(4, 4), bits[1]);
            BinaryPrimitives.WriteInt32LittleEndian(buf.Slice(8, 4), bits[2]);
            BinaryPrimitives.WriteInt32LittleEndian(buf.Slice(12, 4), bits[3]);
            _stream.Write(buf);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(double val)
        {
            Span<byte> buf = stackalloc byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(buf, BitConverter.DoubleToInt64Bits(val));
            _stream.Write(buf);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFloat(float val)
        {
            Span<byte> buf = stackalloc byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(buf, BitConverter.SingleToInt32Bits(val));
            _stream.Write(buf);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteGuid(Guid guid)
        {
            Span<byte> buf = stackalloc byte[16];
            guid.TryWriteBytes(buf);
            _stream.Write(buf);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteChar(char val)
        {
            Span<byte> buf = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(buf, val);
            _stream.Write(buf);
        }

        /// <summary>
        /// Write array bytes
        /// </summary>
        /// <param name="val"></param>
        public void WriteBytes(byte[] val)
        {
            if (val == null!)
            {
                WriteInt32(0);
            }
            else
            {
                WriteInt32(val.Length);
                _stream.Write(val, 0, val.Length);
            }
        }
        /// <summary>
        /// Write string (4 bytes long + Length bytes)
        /// </summary>
        public void WriteString(string line)
        {
            if (line == null!)
            {
                WriteInt32(0);
            }
            else
            {
                var buffer = Encoding.UTF8.GetBytes(line);
                WriteInt32(buffer.Length);
                _stream.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Record the datetime
        /// </summary>
        /// <param name="datetime"></param>
        public void WriteDateTime(DateTime? datetime)
        {
            if (datetime == null!)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);
                long serialized = datetime.Value.ToBinary();
                byte[] data = BitConverter.GetBytes(serialized);
                _stream.Write(data, 0, 8);
            }
        }

        public void WriteDateTimeOffset(DateTimeOffset? datetime)
        {
            if (datetime == null!)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);

                byte[] bytes = new byte[16];
                // Записываем тики самого времени (первые 8 байт)
                BitConverter.TryWriteBytes(new Span<byte>(bytes, 0, 8), datetime.Value.Ticks);
                // Записываем тики смещения (следующие 8 байт)
                BitConverter.TryWriteBytes(new Span<byte>(bytes, 8, 8), datetime.Value.Offset.Ticks);

                _stream.Write(bytes, 0, 16);
            }
        }

        public void WriteIP(IPAddress ip)
        {
            if (ip == null!)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);
                WriteBytes(ip.GetAddressBytes());
            }
        }

        public void WriteIPEndpoint(IPEndPoint endpoint)
        {
            if (endpoint == null!)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);
                WriteIP(endpoint.Address);
                WriteInt32(endpoint.Port);
            }
        }

        public void WriteUri(Uri uri)
        {
            // null and empty are indistinguishable on the wire — see WriteString semantics
            WriteString(uri?.OriginalString!);
        }

        public void WriteVersion(Version version)
        {
            if (version == null!)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);
                WriteInt32(version.Major);
                WriteInt32(version.Minor);
                WriteInt32(version.Build);    // -1 if not specified
                WriteInt32(version.Revision); // -1 if not specified
            }
        }

        public void WriteBitArray(BitArray bits)
        {
            if (bits == null!)
            {
                WriteInt32(-1);
                return;
            }
            WriteInt32(bits.Length);
            if (bits.Length == 0) return;
            var bytes = new byte[(bits.Length + 7) >> 3];
            bits.CopyTo(bytes, 0);
            _stream.Write(bytes, 0, bytes.Length);
        }

        public void WriteEnum<T>(T value) where T : struct, Enum
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.Byte: WriteByte(Unsafe.As<T, byte>(ref value)); break;
                case TypeCode.SByte: WriteSByte(Unsafe.As<T, sbyte>(ref value)); break;
                case TypeCode.Int16: WriteShort(Unsafe.As<T, short>(ref value)); break;
                case TypeCode.UInt16: WriteUShort(Unsafe.As<T, ushort>(ref value)); break;
                case TypeCode.Int32: WriteInt32(Unsafe.As<T, int>(ref value)); break;
                case TypeCode.UInt32: WriteUInt32(Unsafe.As<T, uint>(ref value)); break;
                case TypeCode.Int64: WriteLong(Unsafe.As<T, long>(ref value)); break;
                case TypeCode.UInt64: WriteULong(Unsafe.As<T, ulong>(ref value)); break;
                default: throw new NotSupportedException($"Enum {typeof(T).Name} has unsupported underlying type {underlyingType.Name}");
            }
        }

        #region Nullable primitives
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBooleanNullable(bool? val) { if (val.HasValue) { WriteByte(ONE); WriteBoolean(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByteNullable(byte? val) { if (val.HasValue) { WriteByte(ONE); WriteByte(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSByteNullable(sbyte? val) { if (val.HasValue) { WriteByte(ONE); WriteSByte(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteCharNullable(char? val) { if (val.HasValue) { WriteByte(ONE); WriteChar(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteShortNullable(short? val) { if (val.HasValue) { WriteByte(ONE); WriteShort(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUShortNullable(ushort? val) { if (val.HasValue) { WriteByte(ONE); WriteUShort(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32Nullable(int? val) { if (val.HasValue) { WriteByte(ONE); WriteInt32(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32Nullable(uint? val) { if (val.HasValue) { WriteByte(ONE); WriteUInt32(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLongNullable(long? val) { if (val.HasValue) { WriteByte(ONE); WriteLong(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteULongNullable(ulong? val) { if (val.HasValue) { WriteByte(ONE); WriteULong(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFloatNullable(float? val) { if (val.HasValue) { WriteByte(ONE); WriteFloat(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDoubleNullable(double? val) { if (val.HasValue) { WriteByte(ONE); WriteDouble(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDecimalNullable(decimal? val) { if (val.HasValue) { WriteByte(ONE); WriteDecimal(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTimeSpanNullable(TimeSpan? val) { if (val.HasValue) { WriteByte(ONE); WriteTimeSpan(val.Value); } else { WriteByte(ZERO); } }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteGuidNullable(Guid? val) { if (val.HasValue) { WriteByte(ONE); WriteGuid(val.Value); } else { WriteByte(ZERO); } }
        #endregion

        public byte[] Complete()
        {
            return (_stream as MemoryStream)?.ToArray() ?? ReadToEnd(_stream);
        }

        private static byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = 0;
            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }
            try
            {
                byte[] readBuffer = new byte[4096];
                int totalBytesRead = 0;
                int bytesRead;
                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;
                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }
                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        public void Dispose()
        {
            _stream.Flush();
            _stream.Dispose();
            _writeLock?.Dispose();
        }

        #region Extension

        #region Collections
        public void WriteCollection<T>(IEnumerable<T> collection)
            where T : IBinarySerializable
        {
            if (collection != null!)
            {
                var savedPos = MockCount();
                int count = 0;
                foreach (var item in collection)
                {
                    item.Serialize(this);
                    count++;
                }
                UpdateCount(savedPos, count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public void WriteCollection<T>(IEnumerable<T> collection, Action<T> writeAction)
        {
            if (collection != null!)
            {
                var savedPos = MockCount();
                int count = 0;

                foreach (var item in collection)
                {
                    writeAction.Invoke(item);
                    count++;
                }

                UpdateCount(savedPos, count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public void WriteCollection(IEnumerable<string> collection) => WriteCollection(collection, s => WriteString(s));

        public void WriteCollection(IEnumerable<IPAddress> collection) => WriteCollection(collection, s => WriteIP(s));

        public void WriteCollection(IEnumerable<IPEndPoint> collection) => WriteCollection(collection, s => WriteIPEndpoint(s));

        public void WriteCollection(IEnumerable<Uri> collection) => WriteCollection(collection, s => WriteUri(s));

        public void WriteCollection(IEnumerable<Version> collection) => WriteCollection(collection, s => WriteVersion(s));

        public void WriteCollection(IEnumerable<BitArray> collection) => WriteCollection(collection, s => WriteBitArray(s));

        public void WriteCollection(IEnumerable<Guid> collection) => WriteCollection(collection, s => WriteGuid(s));

        public void WriteCollection(IEnumerable<DateTime> collection) => WriteCollection(collection, s => WriteDateTime(s));

        public void WriteCollection(IEnumerable<DateTime?> collection) => WriteCollection(collection, s => WriteDateTime(s));

        public void WriteCollection(IEnumerable<DateTimeOffset> collection) => WriteCollection(collection, s => WriteDateTimeOffset(s));

        public void WriteCollection(IEnumerable<DateTimeOffset?> collection) => WriteCollection(collection, s => WriteDateTimeOffset(s));

        public void WriteCollection(IEnumerable<UInt64> collection) => WriteCollection(collection, s => WriteULong(s));

        public void WriteCollection(IEnumerable<UInt32> collection) => WriteCollection(collection, s => WriteUInt32(s));

        public void WriteCollection(IEnumerable<char> collection) => WriteCollection(collection, s => WriteChar(s));

        public void WriteCollection(IEnumerable<short> collection) => WriteCollection(collection, s => WriteShort(s));

        public void WriteCollection(IEnumerable<ushort> collection) => WriteCollection(collection, s => WriteUShort(s));

        public void WriteCollection(IEnumerable<Int64> collection) => WriteCollection(collection, s => WriteLong(s));

        public void WriteCollection(IEnumerable<Int32> collection) => WriteCollection(collection, s => WriteInt32(s));

        public void WriteCollection(IEnumerable<float> collection) => WriteCollection(collection, s => WriteFloat(s));

        public void WriteCollection(IEnumerable<Double> collection) => WriteCollection(collection, s => WriteDouble(s));

        public void WriteCollection(IEnumerable<bool> collection) => WriteCollection(collection, s => WriteBoolean(s));

        public void WriteCollection(IEnumerable<byte> collection) => WriteCollection(collection, s => WriteByte(s));

        public void WriteCollection(IEnumerable<byte[]> collection) => WriteCollection(collection, s => WriteBytes(s));

        public void WriteCollection(IEnumerable<sbyte> collection) => WriteCollection(collection, s => WriteSByte(s));

        public void WriteCollection(IEnumerable<decimal> collection) => WriteCollection(collection, s => WriteDecimal(s));

        public void WriteCollection(IEnumerable<TimeSpan> collection) => WriteCollection(collection, s => WriteTimeSpan(s));
        #endregion

        #region Arrays
        public void WriteArray<T>(T[] array)
            where T : IBinarySerializable
        {
            if (array != null!)
            {
                WriteInt32(array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    array[i].Serialize(this);
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public void WriteArray<T>(T[] array, Action<T> writeAction)
        {
            if (array != null!)
            {
                WriteInt32(array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    writeAction.Invoke(array[i]);
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public void WriteArray(string[] array) => WriteArray(array, WriteString);

        public void WriteArray(IPAddress[] array) => WriteArray(array, WriteIP);

        public void WriteArray(IPEndPoint[] array) => WriteArray(array, WriteIPEndpoint);

        public void WriteArray(Uri[] array) => WriteArray(array, WriteUri);

        public void WriteArray(Version[] array) => WriteArray(array, WriteVersion);

        public void WriteArray(BitArray[] array) => WriteArray(array, WriteBitArray);

        public void WriteArray(Guid[] array) => WriteArray(array, WriteGuid);

        public void WriteArray(DateTime[] array) => WriteArray(array, dt => WriteDateTime(dt));

        public void WriteArray(DateTime?[] array) => WriteArray(array, WriteDateTime);

        public void WriteArray(DateTimeOffset[] array) => WriteArray(array, dt => WriteDateTimeOffset(dt));

        public void WriteArray(DateTimeOffset?[] array) => WriteArray(array, WriteDateTimeOffset);

        public void WriteArray(UInt64[] array) => WriteArray(array, WriteULong);

        public void WriteArray(UInt32[] array) => WriteArray(array, WriteUInt32);

        public void WriteArray(char[] array) => WriteArray(array, WriteChar);

        public void WriteArray(short[] array) => WriteArray(array, WriteShort);

        public void WriteArray(ushort[] array) => WriteArray(array, WriteUShort);

        public void WriteArray(Int64[] array) => WriteArray(array, WriteLong);

        public void WriteArray(Int32[] array) => WriteArray(array, WriteInt32);

        public void WriteArray(float[] array) => WriteArray(array, WriteFloat);

        public void WriteArray(Double[] array) => WriteArray(array, WriteDouble);

        public void WriteArray(bool[] array) => WriteArray(array, WriteBoolean);

        public void WriteArray(byte[] array) => WriteArray(array, WriteByte);

        public void WriteArray(byte[][] array) => WriteArray(array, WriteBytes);

        public void WriteArray(sbyte[] array) => WriteArray(array, WriteSByte);

        public void WriteArray(decimal[] array) => WriteArray(array, WriteDecimal);

        public void WriteArray(TimeSpan[] array) => WriteArray(array, WriteTimeSpan);
        #endregion

        public void WriteCompatible<T>(T item)
        {
            if (item == null!)
                throw new ArgumentNullException(nameof(item),
                    "WriteCompatible does not support null. Use Write<T> for nullable IBinarySerializable, or wrap nullable values explicitly.");
            var buffer = MessageSerializer.SerializeCompatible(item);
            _stream.Write(buffer, 0, buffer.Length);
        }

        public void Write<T>(T item)
            where T : IBinarySerializable
        {
            if (item != null!)
            {
                WriteByte(1);
                item.Serialize(this);
            }
            else
            {
                WriteByte(0);
            }
        }

        public void WriteKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> pair)
        {
            if (pair.Key == null!)
                throw new ArgumentException("KeyValuePair key cannot be null.", nameof(pair));
            if (pair.Value == null!)
                throw new ArgumentException($"KeyValuePair value for key '{pair.Key}' cannot be null.", nameof(pair));
            var keySer = MessageSerializer.GetSerializer<TKey>();
            var valSer = MessageSerializer.GetSerializer<TValue>();
            keySer(this, pair.Key);
            valSer(this, pair.Value);
        }

        public void WriteValueTuple<T1, T2>((T1, T2) value)
        {
            if (value.Item1 == null!)
                throw new ArgumentException("ValueTuple Item1 cannot be null.", nameof(value));
            if (value.Item2 == null!)
                throw new ArgumentException("ValueTuple Item2 cannot be null.", nameof(value));
            var ser1 = MessageSerializer.GetSerializer<T1>();
            var ser2 = MessageSerializer.GetSerializer<T2>();
            ser1(this, value.Item1);
            ser2(this, value.Item2);
        }

        public void WriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null!)
            {
                foreach (var item in collection)
                {
                    if (item.Key == null!)
                        throw new ArgumentException("Dictionary key cannot be null.", nameof(collection));
                    if (item.Value == null!)
                        throw new ArgumentException($"Dictionary value for key '{item.Key}' cannot be null. WriteDictionary does not support null values.", nameof(collection));
                    WriteCompatible(item.Key);
                    WriteCompatible(item.Value);
                }
            }
        }

        public void WriteDictionary<TKey, TValue>(ConcurrentDictionary<TKey, TValue> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null!)
            {
                foreach (var item in collection)
                {
                    if (item.Key == null!)
                        throw new ArgumentException("Dictionary key cannot be null.", nameof(collection));
                    if (item.Value == null!)
                        throw new ArgumentException($"Dictionary value for key '{item.Key}' cannot be null. WriteDictionary does not support null values.", nameof(collection));
                    WriteCompatible(item.Key);
                    WriteCompatible(item.Value);
                }
            }
        }

        #endregion Extension
    }

    /// <summary>
    /// Async methods
    /// </summary>
    public partial class MemoryStreamWriter :
        IAsyncBinaryWriter, IAsyncDisposable
    {
        private SemaphoreSlim _writeLock = new SemaphoreSlim(1);
        public async Task WaitLockAsync() => await _writeLock.WaitAsync();
        public void Release() => _writeLock.Release();

        /// <summary>
        /// Write char (2 bytes)
        /// </summary>
        public async Task WriteCharAsync(char val)
        {
            var data = BitConverter.GetBytes(val);
            await _stream.WriteAsync(data, 0, 2);
        }

        /// <summary>
        /// Write array bytes
        /// </summary>
        /// <param name="val"></param>
        public async Task WriteBytesAsync(byte[] val)
        {
            if (val == null!)
            {
                await WriteInt32Async(0);
            }
            else
            {
                await WriteInt32Async(val.Length);
                await _stream.WriteAsync(val, 0, val.Length);
            }
        }

        public async Task WriteRawBytesAsyncNoLength(byte[] val)
        {
            if (val == null!)
            {
                throw new ArgumentNullException(nameof(val));
            }
            await _stream.WriteAsync(val, 0, val.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteShortAsync(short number) => await _stream.WriteAsync(BitConverter.GetBytes(number), 0, 2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteUShortAsync(ushort number) => await _stream.WriteAsync(BitConverter.GetBytes(number), 0, 2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteInt32Async(Int32 number) => await _stream.WriteAsync(BitConverter.GetBytes(number), 0, 4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteUInt32Async(UInt32 number) => await _stream.WriteAsync(BitConverter.GetBytes(number), 0, 4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteLongAsync(Int64 number) => await _stream.WriteAsync(BitConverter.GetBytes(number), 0, 8);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteULongAsync(UInt64 number) => await _stream.WriteAsync(BitConverter.GetBytes(number), 0, 8);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteTimeSpanAsync(TimeSpan period) => await WriteLongAsync(period.Ticks);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteDecimalAsync(Decimal number) => await _stream.WriteAsync(BitConverterExt.GetBytes(number), 0, 16);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteDoubleAsync(double val) => await _stream.WriteAsync(BitConverter.GetBytes(val), 0, 8);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteFloatAsync(float val) => await _stream.WriteAsync(BitConverter.GetBytes(val), 0, 4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteGuidAsync(Guid guid) => await _stream.WriteAsync(guid.ToByteArray(), 0, 16);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task WriteSByteAsync(sbyte val)
        {
            _stream.WriteByte(unchecked((byte)val));
            return Task.CompletedTask;
        }

        /// <summary>
        /// Write string (4 bytes long + Length bytes)
        /// </summary>
        public async Task WriteStringAsync(string line)
        {
            if (line == null!)
            {
                await WriteInt32Async(0);
            }
            else
            {
                var buffer = Encoding.UTF8.GetBytes(line);
                await WriteInt32Async(buffer.Length);
                await _stream.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Record the datetime
        /// </summary>
        /// <param name="datetime"></param>
        public async Task WriteDateTimeAsync(DateTime? datetime)
        {
            if (datetime == null!)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);
                long serialized = datetime.Value.ToBinary();
                byte[] data = BitConverter.GetBytes(serialized);
                await _stream.WriteAsync(data, 0, 8);
            }
        }

        public async Task WriteDateTimeOffsetAsync(DateTimeOffset? datetime)
        {
            if (datetime == null!)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);

                byte[] bytes = new byte[16];
                // Записываем тики самого времени (первые 8 байт)
                BitConverter.TryWriteBytes(new Span<byte>(bytes, 0, 8), datetime.Value.Ticks);
                // Записываем тики смещения (следующие 8 байт)
                BitConverter.TryWriteBytes(new Span<byte>(bytes, 8, 8), datetime.Value.Offset.Ticks);

                await _stream.WriteAsync(bytes, 0, 16);
            }
        }

        public async Task WriteIPAsync(IPAddress ip)
        {
            if (ip == null!)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);
                await WriteBytesAsync(ip.GetAddressBytes());
            }
        }

        public async Task WriteIPEndpointAsync(IPEndPoint endpoint)
        {
            if (endpoint == null!)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);
                await WriteIPAsync(endpoint.Address);
                await WriteInt32Async(endpoint.Port);
            }
        }

        public async Task WriteUriAsync(Uri uri)
        {
            await WriteStringAsync(uri?.OriginalString!);
        }

        public async Task WriteVersionAsync(Version version)
        {
            if (version == null!)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);
                await WriteInt32Async(version.Major);
                await WriteInt32Async(version.Minor);
                await WriteInt32Async(version.Build);
                await WriteInt32Async(version.Revision);
            }
        }

        public async Task WriteBitArrayAsync(BitArray bits)
        {
            if (bits == null!)
            {
                await WriteInt32Async(-1);
                return;
            }
            await WriteInt32Async(bits.Length);
            if (bits.Length == 0) return;
            var bytes = new byte[(bits.Length + 7) >> 3];
            bits.CopyTo(bytes, 0);
            await _stream.WriteAsync(bytes, 0, bytes.Length);
        }

        public async Task WriteEnumAsync<T>(T value) where T : struct, Enum
        {
            var underlyingType = Enum.GetUnderlyingType(typeof(T));
            switch (Type.GetTypeCode(underlyingType))
            {
                case TypeCode.Byte: WriteByte(Unsafe.As<T, byte>(ref value)); break;
                case TypeCode.SByte: WriteSByte(Unsafe.As<T, sbyte>(ref value)); break;
                case TypeCode.Int16: await WriteShortAsync(Unsafe.As<T, short>(ref value)); break;
                case TypeCode.UInt16: await WriteUShortAsync(Unsafe.As<T, ushort>(ref value)); break;
                case TypeCode.Int32: await WriteInt32Async(Unsafe.As<T, int>(ref value)); break;
                case TypeCode.UInt32: await WriteUInt32Async(Unsafe.As<T, uint>(ref value)); break;
                case TypeCode.Int64: await WriteLongAsync(Unsafe.As<T, long>(ref value)); break;
                case TypeCode.UInt64: await WriteULongAsync(Unsafe.As<T, ulong>(ref value)); break;
                default: throw new NotSupportedException($"Enum {typeof(T).Name} has unsupported underlying type {underlyingType.Name}");
            }
        }

        #region Nullable primitives (async)
        public async Task WriteBooleanNullableAsync(bool? val) { if (val.HasValue) { WriteByte(ONE); WriteBoolean(val.Value); } else { WriteByte(ZERO); } await Task.CompletedTask; }
        public async Task WriteByteNullableAsync(byte? val) { if (val.HasValue) { WriteByte(ONE); WriteByte(val.Value); } else { WriteByte(ZERO); } await Task.CompletedTask; }
        public async Task WriteSByteNullableAsync(sbyte? val) { if (val.HasValue) { WriteByte(ONE); WriteSByte(val.Value); } else { WriteByte(ZERO); } await Task.CompletedTask; }
        public async Task WriteCharNullableAsync(char? val) { if (val.HasValue) { WriteByte(ONE); await WriteCharAsync(val.Value); } else { WriteByte(ZERO); } }
        public async Task WriteShortNullableAsync(short? val) { if (val.HasValue) { WriteByte(ONE); await WriteShortAsync(val.Value); } else { WriteByte(ZERO); } }
        public async Task WriteUShortNullableAsync(ushort? val) { if (val.HasValue) { WriteByte(ONE); await WriteUShortAsync(val.Value); } else { WriteByte(ZERO); } }
        public async Task WriteInt32NullableAsync(int? val) { if (val.HasValue) { WriteByte(ONE); await WriteInt32Async(val.Value); } else { WriteByte(ZERO); } }
        public async Task WriteUInt32NullableAsync(uint? val) { if (val.HasValue) { WriteByte(ONE); await WriteUInt32Async(val.Value); } else { WriteByte(ZERO); } }
        public async Task WriteLongNullableAsync(long? val) { if (val.HasValue) { WriteByte(ONE); await WriteLongAsync(val.Value); } else { WriteByte(ZERO); } }
        public async Task WriteULongNullableAsync(ulong? val) { if (val.HasValue) { WriteByte(ONE); await WriteULongAsync(val.Value); } else { WriteByte(ZERO); } }
        public async Task WriteFloatNullableAsync(float? val) { if (val.HasValue) { WriteByte(ONE); await WriteFloatAsync(val.Value); } else { WriteByte(ZERO); } }
        public async Task WriteDoubleNullableAsync(double? val) { if (val.HasValue) { WriteByte(ONE); await WriteDoubleAsync(val.Value); } else { WriteByte(ZERO); } }
        public async Task WriteDecimalNullableAsync(decimal? val) { if (val.HasValue) { WriteByte(ONE); await WriteDecimalAsync(val.Value); } else { WriteByte(ZERO); } }
        public async Task WriteTimeSpanNullableAsync(TimeSpan? val) { if (val.HasValue) { WriteByte(ONE); await WriteTimeSpanAsync(val.Value); } else { WriteByte(ZERO); } }
        public async Task WriteGuidNullableAsync(Guid? val) { if (val.HasValue) { WriteByte(ONE); await WriteGuidAsync(val.Value); } else { WriteByte(ZERO); } }
        #endregion

        public async Task<byte[]> CompleteAsync()
        {
            return (_stream as MemoryStream)?.ToArray() ?? (await ReadToEndAsync(_stream));
        }

        private static async Task<byte[]> ReadToEndAsync(System.IO.Stream stream)
        {
            long originalPosition = 0;
            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }
            try
            {
                byte[] readBuffer = new byte[4096];
                int totalBytesRead = 0;
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;
                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }
                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _stream.FlushAsync();
            await _stream.DisposeAsync();
            _writeLock?.Dispose();
        }

        #region Extension

        #region Collections

        /// <summary>
        /// Increase writing by batches
        /// </summary>
        private async Task OptimizedWriteCollectionByChunksAsync<T>(IEnumerable<T> collection, Action<MemoryStreamWriter, T> saveAction, Func<MemoryStreamWriter, T, Task> asyncSaveAction, int chunk_size)
        {
            if (collection != null!)
            {
                if (_stream.CanSeek == false)
                {
                    WriteInt32(collection.Count());
                    foreach (var item in collection)
                    {
                        await asyncSaveAction.Invoke(this, item);
                    }
                }
                else
                {
                    var savedPos = MockCount();
                    int count = 0;
                    if (_stream is MemoryStream)
                    {
                        foreach (var item in collection)
                        {
                            saveAction.Invoke(this, item);
                            count++;
                        }
                    }
                    else
                    {
                        using (var ms = new MemoryStream())
                        {
                            using (var writer = new MemoryStreamWriter(ms))
                            {
                                foreach (var items in collection.Chunkify(chunk_size))
                                {
                                    ms.SetLength(0);
                                    foreach (var item in items)
                                    {
                                        saveAction.Invoke(writer, item);
                                        count++;
                                    }
                                    await WriteRawBytesAsyncNoLength(writer.Complete());
                                }
                            }
                        }
                    }
                    UpdateCount(savedPos, count);
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync<T>(IEnumerable<T> collection)
            where T : IAsyncBinarySerializable
        {
            if (collection != null!)
            {
                var savedPos = MockCount();
                int count = 0;
                foreach (var item in collection)
                {
                    await item.SerializeAsync(this);
                    count++;
                }
                UpdateCount(savedPos, count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<string> collection)
        {
            if (collection != null!)
            {
                var savedPos = MockCount();
                int count = 0;
                foreach (var item in collection)
                {
                    await WriteStringAsync(item);
                    count++;
                }
                UpdateCount(savedPos, count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<IPAddress> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteIP(i), (w, i) => w.WriteIPAsync(i), BATCH_MEMORY_SIZE_LIMIT / 5);

        public async Task WriteCollectionAsync(IEnumerable<IPEndPoint> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteIPEndpoint(i), (w, i) => w.WriteIPEndpointAsync(i), BATCH_MEMORY_SIZE_LIMIT / 9);

        public async Task WriteCollectionAsync(IEnumerable<Uri> collection)
        {
            if (collection != null!)
            {
                var savedPos = MockCount();
                int count = 0;
                foreach (var item in collection)
                {
                    await WriteUriAsync(item);
                    count++;
                }
                UpdateCount(savedPos, count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<Version> collection)
        {
            if (collection != null!)
            {
                var savedPos = MockCount();
                int count = 0;
                foreach (var item in collection)
                {
                    await WriteVersionAsync(item);
                    count++;
                }
                UpdateCount(savedPos, count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<BitArray> collection)
        {
            if (collection != null!)
            {
                var savedPos = MockCount();
                int count = 0;
                foreach (var item in collection)
                {
                    await WriteBitArrayAsync(item);
                    count++;
                }
                UpdateCount(savedPos, count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<Guid> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteGuid(i), (w, i) => w.WriteGuidAsync(i), BATCH_MEMORY_SIZE_LIMIT / 16);

        public async Task WriteCollectionAsync(IEnumerable<DateTime> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteDateTime(i), (w, i) => w.WriteDateTimeAsync(i), BATCH_MEMORY_SIZE_LIMIT / 9);

        public async Task WriteCollectionAsync(IEnumerable<DateTime?> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteDateTime(i), (w, i) => w.WriteDateTimeAsync(i), BATCH_MEMORY_SIZE_LIMIT / 9);

        public async Task WriteCollectionAsync(IEnumerable<DateTimeOffset> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteDateTimeOffset(i), (w, i) => w.WriteDateTimeOffsetAsync(i), BATCH_MEMORY_SIZE_LIMIT / 9);

        public async Task WriteCollectionAsync(IEnumerable<DateTimeOffset?> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteDateTimeOffset(i), (w, i) => w.WriteDateTimeOffsetAsync(i), BATCH_MEMORY_SIZE_LIMIT / 9);

        public async Task WriteCollectionAsync(IEnumerable<UInt64> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteULong(i), (w, i) => w.WriteULongAsync(i), BATCH_MEMORY_SIZE_LIMIT / 8);

        public async Task WriteCollectionAsync(IEnumerable<UInt32> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteUInt32(i), (w, i) => w.WriteUInt32Async(i), BATCH_MEMORY_SIZE_LIMIT / 4);

        public async Task WriteCollectionAsync(IEnumerable<char> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteChar(i), (w, i) => w.WriteCharAsync(i), BATCH_MEMORY_SIZE_LIMIT / 2);

        public async Task WriteCollectionAsync(IEnumerable<short> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteShort(i), (w, i) => w.WriteShortAsync(i), BATCH_MEMORY_SIZE_LIMIT / 2);

        public async Task WriteCollectionAsync(IEnumerable<ushort> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteUShort(i), (w, i) => w.WriteUShortAsync(i), BATCH_MEMORY_SIZE_LIMIT / 2);

        public async Task WriteCollectionAsync(IEnumerable<Int64> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteLong(i), (w, i) => w.WriteLongAsync(i), BATCH_MEMORY_SIZE_LIMIT / 8);

        public async Task WriteCollectionAsync(IEnumerable<Int32> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteInt32(i), (w, i) => w.WriteInt32Async(i), BATCH_MEMORY_SIZE_LIMIT / 4);

        public async Task WriteCollectionAsync(IEnumerable<float> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteFloat(i), (w, i) => w.WriteFloatAsync(i), BATCH_MEMORY_SIZE_LIMIT / 4);

        public async Task WriteCollectionAsync(IEnumerable<Double> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteDouble(i), (w, i) => w.WriteDoubleAsync(i), BATCH_MEMORY_SIZE_LIMIT / 8);

        public async Task WriteCollectionAsync(IEnumerable<bool> collection)
        {
            if (collection != null!)
            {
                if (_stream.CanSeek == false)
                {
                    WriteInt32(collection.Count());
                    foreach (var item in collection)
                    {
                        WriteBoolean(item);
                    }
                }
                else
                {
                    var savedPos = MockCount();

                    int count = 0;
                    if (_stream is MemoryStream)
                    {
                        foreach (var item in collection)
                        {
                            WriteBoolean(item);
                            count++;
                        }
                    }
                    else
                    {
                        var buffer = new byte[BATCH_MEMORY_SIZE_LIMIT];
                        int index = 0;
                        foreach (var b in collection)
                        {
                            buffer[index] = b ? ONE : ZERO;
                            index++;
                            if (index == BATCH_MEMORY_SIZE_LIMIT)
                            {
                                await _stream.WriteAsync(buffer, 0, buffer.Length);
                                index = 0;
                            }
                            count++;
                        }
                        if (index != 0)
                        {
                            _stream.Write(buffer, 0, index);
                        }
                    }

                    UpdateCount(savedPos, count);
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<byte> collection)
        {
            if (collection != null!)
            {
                if (_stream.CanSeek == false)
                {
                    WriteInt32(collection.Count());
                    foreach (var item in collection)
                    {
                        WriteByte(item);
                    }
                }
                else
                {
                    var savedPos = MockCount();
                    int count = 0;
                    if (_stream is MemoryStream)
                    {
                        foreach (var item in collection)
                        {
                            WriteByte(item);
                            count++;
                        }
                    }
                    else
                    {
                        var buffer = new byte[BATCH_MEMORY_SIZE_LIMIT];
                        int index = 0;
                        foreach (var b in collection)
                        {
                            buffer[index] = b;
                            index++;
                            if (index == BATCH_MEMORY_SIZE_LIMIT)
                            {
                                await _stream.WriteAsync(buffer, 0, buffer.Length);
                                index = 0;
                            }
                            count++;
                        }
                        if (index != 0)
                        {
                            _stream.Write(buffer, 0, index);
                        }
                    }

                    UpdateCount(savedPos, count);
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<byte[]> collection)
        {
            if (collection != null!)
            {
                if (_stream.CanSeek == false)
                {
                    WriteInt32(collection.Count());
                    foreach (var item in collection)
                    {
                        WriteBytes(item);
                    }
                }
                else
                {
                    var savedPos = MockCount();

                    int count = 0;
                    if (_stream is MemoryStream)
                    {
                        foreach (var item in collection)
                        {
                            WriteBytes(item);
                            count++;
                        }
                    }
                    else
                    {
                        foreach (var b in collection)
                        {
                            await WriteBytesAsync(b);
                            count++;
                        }
                    }
                    UpdateCount(savedPos, count);
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<sbyte> collection)
        {
            if (collection != null!)
            {
                if (_stream.CanSeek == false)
                {
                    WriteInt32(collection.Count());
                    foreach (var item in collection)
                    {
                        WriteSByte(item);
                    }
                }
                else
                {
                    var savedPos = MockCount();
                    int count = 0;
                    if (_stream is MemoryStream)
                    {
                        foreach (var item in collection)
                        {
                            WriteSByte(item);
                            count++;
                        }
                    }
                    else
                    {
                        var buffer = new byte[BATCH_MEMORY_SIZE_LIMIT];
                        int index = 0;
                        foreach (var b in collection)
                        {
                            buffer[index] = unchecked((byte)b);
                            index++;
                            if (index == BATCH_MEMORY_SIZE_LIMIT)
                            {
                                await _stream.WriteAsync(buffer, 0, buffer.Length);
                                index = 0;
                            }
                            count++;
                        }
                        if (index != 0)
                        {
                            _stream.Write(buffer, 0, index);
                        }
                    }
                    UpdateCount(savedPos, count);
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<decimal> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteDecimal(i), (w, i) => w.WriteDecimalAsync(i), BATCH_MEMORY_SIZE_LIMIT / 16);

        public async Task WriteCollectionAsync(IEnumerable<TimeSpan> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteTimeSpan(i), (w, i) => w.WriteTimeSpanAsync(i), BATCH_MEMORY_SIZE_LIMIT / 16);
        #endregion

        #region Arrays

        /// <summary>
        /// Increase writing by batches
        /// </summary>
        private async Task OptimizedWriteArrayByChunksAsync<T>(T[] array, Action<MemoryStreamWriter, T> saveAction, int chunk_size)
        {
            if (array != null!)
            {
                WriteInt32(array.Length);

                if (_stream is MemoryStream)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        saveAction.Invoke(this, array[i]);
                    }
                }
                else
                {
                    using (var ms = new MemoryStream())
                    {
                        using (var writer = new MemoryStreamWriter(ms))
                        {
                            for (int i = 0; i < array.Length; i += chunk_size)
                            {
                                ms.SetLength(0);
                                for (int j = 0; j < chunk_size && (i + j) < array.Length; j++)
                                {
                                    saveAction.Invoke(writer, array[i + j]);
                                }
                                await WriteRawBytesAsyncNoLength(writer.Complete());
                            }
                        }
                    }
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteArrayAsync<T>(T[] array)
            where T : IAsyncBinarySerializable
        {
            if (array != null!)
            {
                await WriteInt32Async(array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    await array[i].SerializeAsync(this);
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteArrayAsync(string[] array)
        {
            if (array != null!)
            {
                if (_stream is MemoryStream)
                {
                    await WriteInt32Async(array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        WriteString(array[i]);
                    }
                }
                else
                {
                    await WriteInt32Async(array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        await WriteStringAsync(array[i]);
                    }
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteArrayAsync(IPAddress[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteIP(i), BATCH_MEMORY_SIZE_LIMIT / 5);

        public async Task WriteArrayAsync(IPEndPoint[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteIPEndpoint(i), BATCH_MEMORY_SIZE_LIMIT / 9);

        public async Task WriteArrayAsync(Uri[] array)
        {
            if (array != null!)
            {
                if (_stream is MemoryStream)
                {
                    await WriteInt32Async(array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        WriteUri(array[i]);
                    }
                }
                else
                {
                    await WriteInt32Async(array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        await WriteUriAsync(array[i]);
                    }
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteArrayAsync(Version[] array)
        {
            if (array != null!)
            {
                if (_stream is MemoryStream)
                {
                    await WriteInt32Async(array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        WriteVersion(array[i]);
                    }
                }
                else
                {
                    await WriteInt32Async(array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        await WriteVersionAsync(array[i]);
                    }
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteArrayAsync(BitArray[] array)
        {
            if (array != null!)
            {
                if (_stream is MemoryStream)
                {
                    await WriteInt32Async(array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        WriteBitArray(array[i]);
                    }
                }
                else
                {
                    await WriteInt32Async(array.Length);
                    for (int i = 0; i < array.Length; i++)
                    {
                        await WriteBitArrayAsync(array[i]);
                    }
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteArrayAsync(Guid[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteGuid(i), BATCH_MEMORY_SIZE_LIMIT / 16);

        public async Task WriteArrayAsync(DateTime[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteDateTime(i), BATCH_MEMORY_SIZE_LIMIT / 9);

        public async Task WriteArrayAsync(DateTime?[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteDateTime(i), BATCH_MEMORY_SIZE_LIMIT / 9);

        public async Task WriteArrayAsync(UInt64[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteULong(i), BATCH_MEMORY_SIZE_LIMIT / 8);

        public async Task WriteArrayAsync(UInt32[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteUInt32(i), BATCH_MEMORY_SIZE_LIMIT / 4);

        public async Task WriteArrayAsync(char[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteChar(i), BATCH_MEMORY_SIZE_LIMIT / 2);

        public async Task WriteArrayAsync(short[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteShort(i), BATCH_MEMORY_SIZE_LIMIT / 2);

        public async Task WriteArrayAsync(ushort[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteUShort(i), BATCH_MEMORY_SIZE_LIMIT / 2);

        public async Task WriteArrayAsync(Int64[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteLong(i), BATCH_MEMORY_SIZE_LIMIT / 8);

        public async Task WriteArrayAsync(Int32[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteInt32(i), BATCH_MEMORY_SIZE_LIMIT / 4);

        public async Task WriteArrayAsync(float[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteFloat(i), BATCH_MEMORY_SIZE_LIMIT / 4);

        public async Task WriteArrayAsync(Double[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteDouble(i), BATCH_MEMORY_SIZE_LIMIT / 8);

        public async Task WriteArrayAsync(bool[] array)
        {
            if (array != null!)
            {
                WriteInt32(array.Length);

                if (_stream is MemoryStream)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        WriteBoolean(array[i]);
                    }
                }
                else
                {
                    var buffer = new byte[BATCH_MEMORY_SIZE_LIMIT];
                    for (int i = 0; i < array.Length; i += BATCH_MEMORY_SIZE_LIMIT)
                    {
                        int written = 0;
                        for (int j = 0; j < BATCH_MEMORY_SIZE_LIMIT && (i + j) < array.Length; j++)
                        {
                            buffer[j] = array[i + j] ? ONE : ZERO;
                            written++;
                        }
                        await _stream.WriteAsync(buffer, 0, written);
                    }
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteArrayAsync(byte[] array)
        {
            if (array != null!)
            {
                WriteInt32(array.Length);

                if (_stream is MemoryStream)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        WriteByte(array[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < array.Length; i += BATCH_MEMORY_SIZE_LIMIT)
                    {
                        int written = Math.Min(BATCH_MEMORY_SIZE_LIMIT, array.Length - i);
                        await _stream.WriteAsync(array, i, written);
                    }
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteArrayAsync(byte[][] array)
        {
            if (array != null!)
            {
                WriteInt32(array.Length);
                if (_stream is MemoryStream)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        WriteBytes(array[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        await WriteBytesAsync(array[i]);
                    }
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteArrayAsync(sbyte[] array)
        {
            if (array != null!)
            {
                WriteInt32(array.Length);

                if (_stream is MemoryStream)
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        WriteSByte(array[i]);
                    }
                }
                else
                {
                    var buffer = new byte[BATCH_MEMORY_SIZE_LIMIT];
                    for (int i = 0; i < array.Length; i += BATCH_MEMORY_SIZE_LIMIT)
                    {
                        int written = 0;
                        for (int j = 0; j < BATCH_MEMORY_SIZE_LIMIT && (i + j) < array.Length; j++)
                        {
                            buffer[j] = unchecked((byte)array[i + j]);
                            written++;
                        }
                        await _stream.WriteAsync(buffer, 0, written);
                    }
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteArrayAsync(decimal[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteDecimal(i), BATCH_MEMORY_SIZE_LIMIT / 16);

        public async Task WriteArrayAsync(TimeSpan[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteTimeSpan(i), BATCH_MEMORY_SIZE_LIMIT / 8);
        #endregion

        public async Task WriteCompatibleAsync<T>(T item)
        {
            if (item == null!)
                throw new ArgumentNullException(nameof(item),
                    "WriteCompatibleAsync does not support null. Use WriteAsync<T> for nullable IAsyncBinarySerializable, or wrap nullable values explicitly.");
            var buffer = MessageSerializer.SerializeCompatible(item);
            await _stream.WriteAsync(buffer, 0, buffer.Length);
        }

        public async Task WriteAsync<T>(T item)
            where T : IAsyncBinarySerializable
        {
            if (item != null!)
            {
                WriteByte(1);
                await item.SerializeAsync(this);
            }
            else
            {
                WriteByte(0);
            }
        }

        public Task WriteKeyValuePairAsync<TKey, TValue>(KeyValuePair<TKey, TValue> pair)
        {
            if (pair.Key == null!)
                throw new ArgumentException("KeyValuePair key cannot be null.", nameof(pair));
            if (pair.Value == null!)
                throw new ArgumentException($"KeyValuePair value for key '{pair.Key}' cannot be null.", nameof(pair));
            var keySer = MessageSerializer.GetSerializer<TKey>();
            var valSer = MessageSerializer.GetSerializer<TValue>();
            keySer(this, pair.Key);
            valSer(this, pair.Value);
            return Task.CompletedTask;
        }

        public Task WriteValueTupleAsync<T1, T2>((T1, T2) value)
        {
            if (value.Item1 == null!)
                throw new ArgumentException("ValueTuple Item1 cannot be null.", nameof(value));
            if (value.Item2 == null!)
                throw new ArgumentException("ValueTuple Item2 cannot be null.", nameof(value));
            var ser1 = MessageSerializer.GetSerializer<T1>();
            var ser2 = MessageSerializer.GetSerializer<T2>();
            ser1(this, value.Item1);
            ser2(this, value.Item2);
            return Task.CompletedTask;
        }

        public async Task WriteDictionaryAsync<TKey, TValue>(IDictionary<TKey, TValue> collection)
        {
            if (collection != null!)
            {
                WriteInt32(collection.Count);
                foreach (var item in collection)
                {
                    if (item.Key == null!)
                        throw new ArgumentException("Dictionary key cannot be null.", nameof(collection));
                    if (item.Value == null!)
                        throw new ArgumentException($"Dictionary value for key '{item.Key}' cannot be null. WriteDictionaryAsync does not support null values.", nameof(collection));
                    await WriteCompatibleAsync(item.Key);
                    await WriteCompatibleAsync(item.Value);
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteDictionaryAsync<TKey, TValue>(ConcurrentDictionary<TKey, TValue> collection)
        {
            if (collection != null!)
            {
                WriteInt32(collection.Count);
                foreach (var item in collection)
                {
                    if (item.Key == null!)
                        throw new ArgumentException("Dictionary key cannot be null.", nameof(collection));
                    if (item.Value == null!)
                        throw new ArgumentException($"Dictionary value for key '{item.Key}' cannot be null. WriteDictionaryAsync does not support null values.", nameof(collection));
                    await WriteCompatibleAsync(item.Key);
                    await WriteCompatibleAsync(item.Value);
                }
            }
            else
            {
                WriteInt32(0);
            }
        }

        #endregion Extension
    }
}