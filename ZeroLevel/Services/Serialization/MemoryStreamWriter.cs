using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using ZeroLevel.Services.Extensions;

namespace ZeroLevel.Services.Serialization
{
    /// <summary>
    /// Wrapper over memorystream for writing
    /// </summary>
    public sealed class MemoryStreamWriter :
        IBinaryWriter
    {
        public Stream Stream
        {
            get
            {
                return _stream;
            }
        }

        private readonly Stream _stream;

        public MemoryStreamWriter()
        {
            _stream = new MemoryStream();
        }

        public MemoryStreamWriter(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// Record a boolean value (1 byte)
        /// </summary>
        public void WriteBoolean(bool val)
        {
            _stream.WriteByte(BitConverter.GetBytes(val)[0]);
        }

        /// <summary>
        /// Write byte (1 byte)
        /// </summary>
        public void WriteByte(byte val)
        {
            _stream.WriteByte(val);
        }

        /// <summary>
        /// Write char (2 bytes)
        /// </summary>
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
        /// Record a 32-bit integer (4 bytes)
        /// </summary>
        public void WriteShort(short number)
        {
            _stream.Write(BitConverter.GetBytes(number), 0, 2);
        }

        public void WriteUShort(ushort number)
        {
            _stream.Write(BitConverter.GetBytes(number), 0, 2);
        }

        /// <summary>
        /// Record a 32-bit integer (4 bytes)
        /// </summary>
        public void WriteInt32(Int32 number)
        {
            _stream.Write(BitConverter.GetBytes(number), 0, 4);
        }

        public void WriteUInt32(UInt32 number)
        {
            _stream.Write(BitConverter.GetBytes(number), 0, 4);
        }

        /// <summary>
        /// Record an integer 64-bit number (8 bytes)
        /// </summary>
        public void WriteLong(Int64 number)
        {
            _stream.Write(BitConverter.GetBytes(number), 0, 8);
        }

        public void WriteULong(UInt64 number)
        {
            _stream.Write(BitConverter.GetBytes(number), 0, 8);
        }

        public void WriteTimeSpan(TimeSpan period)
        {
            WriteLong(period.Ticks);
        }

        public void WriteDecimal(Decimal number)
        {
            _stream.Write(BitConverterExt.GetBytes(number), 0, 16);
        }

        public void WriteDouble(double val)
        {
            _stream.Write(BitConverter.GetBytes(val), 0, 8);
        }

        public void WriteFloat(float val)
        {
            _stream.Write(BitConverter.GetBytes(val), 0, 4);
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
        /// GUID record (16 bytes)
        /// </summary>
        public void WriteGuid(Guid guid)
        {
            _stream.Write(guid.ToByteArray(), 0, 16);
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

        public void WriteIP(IPAddress ip)
        {
            WriteBytes(ip.GetAddressBytes());
        }

        public void WriteIPEndpoint(IPEndPoint endpoint)
        {
            WriteIP(endpoint.Address);
            WriteInt32(endpoint.Port);
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

        public void WriteCollection(IEnumerable<string> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteString(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<IPAddress> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteIP(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<IPEndPoint> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteIPEndpoint(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<Guid> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteGuid(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<DateTime> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteDateTime(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<UInt64> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteULong(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<UInt32> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteUInt32(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<char> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteChar(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<short> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteShort(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<ushort> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteUShort(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<Int64> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteLong(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<Int32> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteInt32(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<float> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteFloat(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<Double> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteDouble(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<bool> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteBoolean(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<byte> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteByte(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<byte[]> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteBytes(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<decimal> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteDecimal(item);
                }
            }
        }

        public void WriteCollection(IEnumerable<TimeSpan> collection)
        {
            WriteInt32(collection?.Count() ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteTimeSpan(item);
                }
            }
        }
        #endregion

        #region Arrays
        public void WriteArray<T>(T[] array)
            where T : IBinarySerializable
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i].Serialize(this);
                }
            }
        }

        public void WriteArray(string[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteString(array[i]);
                }
            }
        }

        public void WriteArray(IPAddress[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteIP(array[i]);
                }
            }
        }

        public void WriteArray(IPEndPoint[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteIPEndpoint(array[i]);
                }
            }
        }

        public void WriteArray(Guid[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteGuid(array[i]);
                }
            }
        }

        public void WriteArray(DateTime[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteDateTime(array[i]);
                }
            }
        }

        public void WriteArray(UInt64[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteULong(array[i]);
                }
            }
        }

        public void WriteArray(UInt32[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteUInt32(array[i]);
                }
            }
        }

        public void WriteArray(char[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteChar(array[i]);
                }
            }
        }

        public void WriteArray(short[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteShort(array[i]);
                }
            }
        }

        public void WriteArray(ushort[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteUShort(array[i]);
                }
            }
        }

        public void WriteArray(Int64[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteLong(array[i]);
                }
            }
        }

        public void WriteArray(Int32[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteInt32(array[i]);
                }
            }
        }

        public void WriteArray(float[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteFloat(array[i]);
                }
            }
        }

        public void WriteArray(Double[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteDouble(array[i]);
                }
            }
        }

        public void WriteArray(bool[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteBoolean(array[i]);
                }
            }
        }

        public void WriteArray(byte[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteByte(array[i]);
                }
            }
        }

        public void WriteArray(byte[][] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteBytes(array[i]);
                }
            }
        }

        public void WriteArray(decimal[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteDecimal(array[i]);
                }
            }
        }

        public void WriteArray(TimeSpan[] array)
        {
            WriteInt32(array?.Length ?? 0);
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WriteTimeSpan(array[i]);
                }
            }
        }
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

        public void WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> collection)
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
}