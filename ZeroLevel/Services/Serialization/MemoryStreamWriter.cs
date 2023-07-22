using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
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
        private long _saved_stream_position = -1;
        private const int BATCH_MEMORY_SIZE_LIMIT = 1024 * 1024; // 1Mb
        private void MockCount()
        {
            _saved_stream_position = this._stream.Position;
            WriteInt32(0); // count mock
        }

        private void UpdateCount(int count)
        {
            if (_saved_stream_position != -1)
            {
                var current_position = this._stream.Position;
                this._stream.Position = _saved_stream_position;
                WriteInt32(count);
                this._stream.Position = current_position;
                _saved_stream_position = -1;
            }
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
        public void WriteShort(short number) => _stream.Write(BitConverter.GetBytes(number), 0, 2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUShort(ushort number) => _stream.Write(BitConverter.GetBytes(number), 0, 2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(Int32 number) => _stream.Write(BitConverter.GetBytes(number), 0, 4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(UInt32 number) => _stream.Write(BitConverter.GetBytes(number), 0, 4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLong(Int64 number) => _stream.Write(BitConverter.GetBytes(number), 0, 8);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteULong(UInt64 number) => _stream.Write(BitConverter.GetBytes(number), 0, 8);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteTimeSpan(TimeSpan period) => WriteLong(period.Ticks);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDecimal(Decimal number) => _stream.Write(BitConverterExt.GetBytes(number), 0, 16);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(double val) => _stream.Write(BitConverter.GetBytes(val), 0, 8);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFloat(float val) => _stream.Write(BitConverter.GetBytes(val), 0, 4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteGuid(Guid guid) => _stream.Write(guid.ToByteArray(), 0, 16);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteChar(char val)
        {
            var data = BitConverter.GetBytes(val);
            _stream.Write(data, 0, 2);
        }

        /// <summary>
        /// Write array bytes
        /// </summary>
        /// <param name="val"></param>
        public void WriteBytes(byte[] val)
        {
            if (val == null)
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
            if (line == null)
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
            if (datetime == null)
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

        public void WriteTime(TimeOnly? time)
        {
            if (time == null)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);
                var ts = time.Value.ToTimeSpan();
                WriteTimeSpan(ts);
            }
        }

        public void WriteDate(DateOnly? date)
        {
            if (date == null)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);
                var days = date.Value.DayNumber;
                WriteInt32(days);
            }
        }

        public void WriteIP(IPAddress ip)
        {
            if (ip == null)
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
            if (endpoint == null)
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
        }

        #region Extension

        #region Collections
        public void WriteCollection<T>(IEnumerable<T> collection)
            where T : IBinarySerializable
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    item.Serialize(this);
                }
            }
        }

        public void WriteCollection<T>(IEnumerable<T> collection, Action<T> writeAction)
        {
            if (collection != null)
            {
                MockCount();
                int count = 0;

                foreach (var item in collection)
                {
                    writeAction.Invoke(item);
                    count++;
                }

                UpdateCount(count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public void WriteCollection(IEnumerable<string> collection) => WriteCollection(collection, s => WriteString(s));

        public void WriteCollection(IEnumerable<IPAddress> collection) => WriteCollection(collection, s => WriteIP(s));

        public void WriteCollection(IEnumerable<IPEndPoint> collection) => WriteCollection(collection, s => WriteIPEndpoint(s));

        public void WriteCollection(IEnumerable<Guid> collection) => WriteCollection(collection, s => WriteGuid(s));

        public void WriteCollection(IEnumerable<DateTime> collection) => WriteCollection(collection, s => WriteDateTime(s));

        public void WriteCollection(IEnumerable<DateTime?> collection) => WriteCollection(collection, s => WriteDateTime(s));

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

        public void WriteCollection(IEnumerable<decimal> collection) => WriteCollection(collection, s => WriteDecimal(s));

        public void WriteCollection(IEnumerable<TimeSpan> collection) => WriteCollection(collection, s => WriteTimeSpan(s));
        #endregion

        #region Arrays
        public void WriteArray<T>(T[] array)
            where T : IBinarySerializable
        {
            if (array != null)
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
            if (array != null)
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

        public void WriteArray(Guid[] array) => WriteArray(array, WriteGuid);

        public void WriteArray(DateTime[] array) => WriteArray(array, dt => WriteDateTime(dt));

        public void WriteArray(DateTime?[] array) => WriteArray(array, WriteDateTime);

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

        public void WriteArray(decimal[] array) => WriteArray(array, WriteDecimal);

        public void WriteArray(TimeSpan[] array) => WriteArray(array, WriteTimeSpan);
        #endregion

        public void WriteCompatible<T>(T item)
        {
            var buffer = MessageSerializer.SerializeCompatible(item);
            _stream.Write(buffer, 0, buffer.Length);
        }

        public void Write<T>(T item)
            where T : IBinarySerializable
        {
            if (item != null)
            {
                WriteByte(1);
                item.Serialize(this);
            }
            else
            {
                WriteByte(0);
            }
        }

        public void WriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteCompatible(item.Key);
                    WriteCompatible(item.Value);
                }
            }
        }

        public void WriteDictionary<TKey, TValue>(ConcurrentDictionary<TKey, TValue> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
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
        IAsyncBinaryWriter
    {
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
            if (val == null)
            {
                await WriteInt32Async(0);
            }
            else
            {
                await WriteInt32Async(val.Length);
                await _stream.WriteAsync(val, 0, val.Length);
            }
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
        public async Task WriteDoubleAsync(double val)=> await _stream.WriteAsync(BitConverter.GetBytes(val), 0, 8);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteFloatAsync(float val) => await _stream.WriteAsync(BitConverter.GetBytes(val), 0, 4);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task WriteGuidAsync(Guid guid) => await _stream.WriteAsync(guid.ToByteArray(), 0, 16);

        /// <summary>
        /// Write string (4 bytes long + Length bytes)
        /// </summary>
        public async Task WriteStringAsync(string line)
        {
            if (line == null)
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
            if (datetime == null)
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

        public async Task WriteTimeAsync(TimeOnly? time)
        {
            if (time == null)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);
                var ts = time.Value.ToTimeSpan();
                await WriteTimeSpanAsync(ts);
            }
        }

        public async Task WriteDateAsync(DateOnly? date)
        {
            if (date == null)
            {
                WriteByte(0);
            }
            else
            {
                WriteByte(1);
                var days = date.Value.DayNumber;
                await WriteInt32Async(days);
            }
        }

        public async Task WriteIPAsync(IPAddress ip)
        {
            if (ip == null)
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
            if (endpoint == null)
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

        public async Task DisposeAsync()
        {
            await _stream.FlushAsync();
            await _stream.DisposeAsync();
        }

        #region Extension

        #region Collections

        /// <summary>
        /// Increase writing by batches
        /// </summary>
        private async Task OptimizedWriteCollectionByChunksAsync<T>(IEnumerable<T> collection, Action<MemoryStreamWriter, T> saveAction, int chunk_size)
        {
            if (collection != null)
            {
                MockCount();
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
                                foreach (var item in items)
                                {
                                    saveAction.Invoke(writer, item);
                                    count++;
                                }
                                await WriteBytesAsync(writer.Complete());
                                writer.Stream.Position = 0;
                            }
                        }
                    }
                }
                UpdateCount(count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync<T>(IEnumerable<T> collection)
            where T : IAsyncBinarySerializable
        {
            if (collection != null)
            {
                MockCount();
                int count = 0;
                foreach (var item in collection)
                {
                    await item.SerializeAsync(this);
                    count++;
                }
                UpdateCount(count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<string> collection)
        {
            if (collection != null)
            {
                MockCount();
                int count = 0;
                if (collection != null)
                {
                    foreach (var item in collection)
                    {
                        await WriteStringAsync(item);
                        count++;
                    }
                }
                UpdateCount(count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<IPAddress> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteIP(i), BATCH_MEMORY_SIZE_LIMIT / 5);

        public async Task WriteCollectionAsync(IEnumerable<IPEndPoint> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteIPEndpoint(i), BATCH_MEMORY_SIZE_LIMIT / 9);

        public async Task WriteCollectionAsync(IEnumerable<Guid> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteGuid(i), BATCH_MEMORY_SIZE_LIMIT / 16);

        public async Task WriteCollectionAsync(IEnumerable<DateTime> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteDateTime(i), BATCH_MEMORY_SIZE_LIMIT / 9);

        public async Task WriteCollectionAsync(IEnumerable<DateTime?> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteDateTime(i), BATCH_MEMORY_SIZE_LIMIT / 9);

        public async Task WriteCollectionAsync(IEnumerable<UInt64> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteULong(i), BATCH_MEMORY_SIZE_LIMIT / 8);

        public async Task WriteCollectionAsync(IEnumerable<UInt32> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteUInt32(i), BATCH_MEMORY_SIZE_LIMIT / 4);

        public async Task WriteCollectionAsync(IEnumerable<char> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteChar(i), BATCH_MEMORY_SIZE_LIMIT / 2);

        public async Task WriteCollectionAsync(IEnumerable<short> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteShort(i), BATCH_MEMORY_SIZE_LIMIT / 2);

        public async Task WriteCollectionAsync(IEnumerable<ushort> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteUShort(i), BATCH_MEMORY_SIZE_LIMIT / 2);

        public async Task WriteCollectionAsync(IEnumerable<Int64> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteLong(i), BATCH_MEMORY_SIZE_LIMIT / 8);

        public async Task WriteCollectionAsync(IEnumerable<Int32> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteInt32(i), BATCH_MEMORY_SIZE_LIMIT / 4);

        public async Task WriteCollectionAsync(IEnumerable<float> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteFloat(i), BATCH_MEMORY_SIZE_LIMIT / 4);

        public async Task WriteCollectionAsync(IEnumerable<Double> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteDouble(i), BATCH_MEMORY_SIZE_LIMIT / 8);

        public async Task WriteCollectionAsync(IEnumerable<bool> collection)
        {
            if (collection != null)
            {
                MockCount();

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

                UpdateCount(count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<byte> collection)
        {
            if (collection != null)
            {
                MockCount();

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

                UpdateCount(count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<byte[]> collection)
        {
            if (collection != null)
            {
                MockCount();

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
                UpdateCount(count);
            }
            else
            {
                WriteInt32(0);
            }
        }

        public async Task WriteCollectionAsync(IEnumerable<decimal> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteDecimal(i), BATCH_MEMORY_SIZE_LIMIT / 16);

        public async Task WriteCollectionAsync(IEnumerable<TimeSpan> collection) => await OptimizedWriteCollectionByChunksAsync(collection, (w, i) => w.WriteTimeSpan(i), BATCH_MEMORY_SIZE_LIMIT / 16);
        #endregion

        #region Arrays

        /// <summary>
        /// Increase writing by batches
        /// </summary>
        private async Task OptimizedWriteArrayByChunksAsync<T>(T[] array, Action<MemoryStreamWriter, T> saveAction, int chunk_size)
        {
            if (array != null)
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
                                for (int j = 0; j < chunk_size && (i + j) < array.Length; j++)
                                {
                                    saveAction.Invoke(writer, array[i + j]);
                                }
                                await WriteBytesAsync(writer.Complete());
                                writer.Stream.Position = 0;
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
            if (array != null)
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
            if (array != null)
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
            if (array != null)
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
                    using (var ms = new MemoryStream())
                    {
                        using (var writer = new MemoryStreamWriter(ms))
                        {
                            for (int i = 0; i < array.Length; i += BATCH_MEMORY_SIZE_LIMIT)
                            {
                                for (int j = 0; j < BATCH_MEMORY_SIZE_LIMIT && (i + j) < array.Length; j++)
                                {
                                    buffer[j] = array[i + j] ? ONE : ZERO;
                                }
                                await WriteBytesAsync(writer.Complete());
                                writer.Stream.Position = 0;
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

        public async Task WriteArrayAsync(byte[] array)
        {
            if (array != null)
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
                    var buffer = new byte[BATCH_MEMORY_SIZE_LIMIT];
                    using (var ms = new MemoryStream())
                    {
                        using (var writer = new MemoryStreamWriter(ms))
                        {
                            for (int i = 0; i < array.Length; i += BATCH_MEMORY_SIZE_LIMIT)
                            {
                                for (int j = 0; j < BATCH_MEMORY_SIZE_LIMIT && (i + j) < array.Length; j++)
                                {
                                    buffer[j] = array[i + j];
                                }
                                await WriteBytesAsync(writer.Complete());
                                writer.Stream.Position = 0;
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

        public async Task WriteArrayAsync(byte[][] array)
        {
            if (array != null)
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

        public async Task WriteArrayAsync(decimal[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteDecimal(i), BATCH_MEMORY_SIZE_LIMIT / 16);

        public async Task WriteArrayAsync(TimeSpan[] array) => await OptimizedWriteArrayByChunksAsync(array, (w, i) => w.WriteTimeSpan(i), BATCH_MEMORY_SIZE_LIMIT / 8);
        #endregion

        public async Task WriteCompatibleAsync<T>(T item)
        {
            var buffer = MessageSerializer.SerializeCompatible(item);
            await _stream.WriteAsync(buffer, 0, buffer.Length);
        }

        public async Task WriteAsync<T>(T item)
            where T : IAsyncBinarySerializable
        {
            if (item != null)
            {
                WriteByte(1);
                await item.SerializeAsync(this);
            }
            else
            {
                WriteByte(0);
            }
        }

        public async Task WriteDictionaryAsync<TKey, TValue>(IDictionary<TKey, TValue> collection)
        {
            if (collection != null)
            {
                WriteInt32(collection.Count);
                foreach (var item in collection)
                {
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
            if (collection != null)
            {
                WriteInt32(collection.Count);
                foreach (var item in collection)
                {
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