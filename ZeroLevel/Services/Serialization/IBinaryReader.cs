using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace ZeroLevel.Services.Serialization
{
    public interface IBinaryReader : IDisposable
    {
        bool ReadBoolean();

        char ReadChar();

        byte ReadByte();

        byte[] ReadBytes();

        Double ReadDouble();

        float ReadFloat();

        short ReadShort();

        ushort ReadUShort();

        Int32 ReadInt32();

        UInt32 ReadUInt32();

        Int64 ReadLong();

        UInt64 ReadULong();

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

        Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>();

        ConcurrentDictionary<TKey, TValue> ReadDictionaryAsConcurrent<TKey, TValue>();

        List<string> ReadStringCollection();

        List<Guid> ReadGuidCollection();

        List<DateTime> ReadDateTimeCollection();
        List<char> ReadCharCollection();
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

        List<UInt64> ReadUInt64Collection();

        List<UInt32> ReadUInt32Collection();

        List<short> ReadShortCollection();

        List<ushort> ReadUShortCollection();

        #endregion Extensions

        Stream Stream { get; }
    }
}