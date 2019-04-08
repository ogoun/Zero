using System;
using System.Collections.Generic;
using System.Net;

namespace ZeroLevel.Services.Serialization
{
    public interface IBinaryReader : IDisposable
    {
        bool ReadBoolean();

        byte ReadByte();

        byte[] ReadBytes();

        Double ReadDouble();

        float ReadFloat();

        Int32 ReadInt32();

        Int64 ReadLong();

        string ReadString();

        Guid ReadGuid();

        DateTime? ReadDateTime();

        decimal ReadDecimal();

        TimeSpan ReadTimeSpan();

        IPAddress ReadIP();

        IPEndPoint ReadIPEndpoint();

        #region Extensions

        T Read<T>() where T : IBinarySerializable;

        T ReadCompatible<T>();

        List<T> ReadCollection<T>() where T : IBinarySerializable, new();

        List<string> ReadStringCollection();

        List<Guid> ReadGuidCollection();

        List<DateTime> ReadDateTimeCollection();

        List<Int64> ReadInt64Collection();

        List<Int32> ReadInt32Collection();

        List<Double> ReadDoubleCollection();

        List<Decimal> ReadDecimalCollection();

        List<TimeSpan> ReadTimeSpanCollection();

        List<float> ReadFloatCollection();

        List<bool> ReadBooleanCollection();

        List<byte> ReadByteCollection();

        List<byte[]> ReadByteArrayCollection();

        List<IPAddress> ReadIPCollection();

        List<IPEndPoint> ReadIPEndPointCollection();

        #endregion Extensions
    }
}