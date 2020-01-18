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

        #region Arrays
        T[] ReadArray<T>() where T : IBinarySerializable, new();
        string[] ReadStringArray();
        IPAddress[] ReadIPArray();
        IPEndPoint[] ReadIPEndPointArray();
        Guid[] ReadGuidArray();
        DateTime[] ReadDateTimeArray();
        Int64[] ReadInt64Array();
        Int32[] ReadInt32Array();
        UInt64[] ReadUInt64Array();
        UInt32[] ReadUInt32Array();
        char[] ReadCharArray();
        short[] ReadShortArray();
        ushort[] ReadUShortArray();
        float[] ReadFloatArray();
        Double[] ReadDoubleArray();
        bool[] ReadBooleanArray();
        byte[] ReadByteArray();
        byte[][] ReadByteArrayArray();
        decimal[] ReadDecimalArray();
        TimeSpan[] ReadTimeSpanArray();
        #endregion

        #region Collections
        List<T> ReadCollection<T>() where T : IBinarySerializable, new();
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
        #endregion

        T Read<T>() where T : IBinarySerializable;

        T ReadCompatible<T>();

        Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>();

        ConcurrentDictionary<TKey, TValue> ReadDictionaryAsConcurrent<TKey, TValue>();
        
        #endregion Extensions

        Stream Stream { get; }
    }
}