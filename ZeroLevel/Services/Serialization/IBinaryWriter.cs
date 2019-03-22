using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace ZeroLevel.Services.Serialization
{
    public interface IBinaryWriter 
        : IDisposable
    {
        void WriteBoolean(bool val);
        void WriteByte(byte val);
        void WriteBytes(byte[] val);
        void WriteDouble(double val);
        void WriteInt32(Int32 number);
        void WriteLong(Int64 number);
        void WriteString(string line);
        void WriteGuid(Guid guid);
        void WriteDateTime(DateTime? datetime);
        void WriteDecimal(Decimal number);
        void WriteTimeSpan(TimeSpan period);
        void WriteIP(IPAddress ip);
        void WriteIPEndpoint(IPEndPoint endpoint);

        #region Extensions
        void WriteCollection<T>(IEnumerable<T> collection)
            where T : IBinarySerializable;
        void WriteCollection(IEnumerable<string> collection);
        void WriteCollection(IEnumerable<Guid> collection);
        void WriteCollection(IEnumerable<DateTime> collection);
        void WriteCollection(IEnumerable<Int64> collection);
        void WriteCollection(IEnumerable<Int32> collection);
        void WriteCollection(IEnumerable<Double> collection);
        void WriteCollection(IEnumerable<bool> collection);
        void WriteCollection(IEnumerable<byte> collection);
        void WriteCollection(IEnumerable<byte[]> collection);
        void WriteCollection(IEnumerable<IPEndPoint> collection);
        void WriteCollection(IEnumerable<IPAddress> collection);
        void Write<T>(T item)
            where T : IBinarySerializable;

        void WriteCompatible<T>(T item);
        #endregion

        Stream Stream { get; }
    }
}
