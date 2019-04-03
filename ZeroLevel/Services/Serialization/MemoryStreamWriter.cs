using System;
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

        private readonly MemoryStream _stream;

        public MemoryStreamWriter()
        {
            _stream = new MemoryStream();
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
        public void WriteInt32(Int32 number)
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
            WriteLong(ip.Address);
        }

        public void WriteIPEndpoint(IPEndPoint endpoint)
        {
            WriteLong(endpoint.Address.Address);
            WriteInt32(endpoint.Port);
        }

        public byte[] Complete()
        {
            return _stream.ToArray();
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        #region Extension
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

        public void WriteCompatible<T>(T item)
        {
            WriteBytes(MessageSerializer.SerializeCompatible(item));
        }
        #endregion
    }
}
