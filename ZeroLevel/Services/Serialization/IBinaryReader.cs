using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

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
        DateTime?[] ReadDateTimeArray();
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
        List<DateTime?> ReadDateTimeCollection();
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

        #region Collections lazy
        IEnumerable<T> ReadCollectionLazy<T>()
            where T : IBinarySerializable, new();
        IEnumerable<string> ReadStringCollectionLazy();
        IEnumerable<IPAddress> ReadIPCollectionLazy();
        IEnumerable<IPEndPoint> ReadIPEndPointCollectionLazy();
        IEnumerable<Guid> ReadGuidCollectionLazy();
        IEnumerable<DateTime?> ReadDateTimeCollectionLazy();
        IEnumerable<Int64> ReadInt64CollectionLazy();
        IEnumerable<Int32> ReadInt32CollectionLazy();
        IEnumerable<UInt64> ReadUInt64CollectionLazy();
        IEnumerable<UInt32> ReadUInt32CollectionLazy();
        IEnumerable<char> ReadCharCollectionLazy();
        IEnumerable<short> ReadShortCollectionLazy();
        IEnumerable<ushort> ReadUShortCollectionLazy();
        IEnumerable<float> ReadFloatCollectionLazy();
        IEnumerable<Double> ReadDoubleCollectionLazy();
        IEnumerable<bool> ReadBooleanCollectionLazy();
        IEnumerable<byte> ReadByteCollectionLazy();
        IEnumerable<byte[]> ReadByteArrayCollectionLazy();
        IEnumerable<decimal> ReadDecimalCollectionLazy();
        IEnumerable<TimeSpan> ReadTimeSpanCollectionLazy();
        #endregion

        T Read<T>() where T : IBinarySerializable;
        T Read<T>(object arg) where T : IBinarySerializable;

        T ReadCompatible<T>();

        Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>();

        ConcurrentDictionary<TKey, TValue> ReadDictionaryAsConcurrent<TKey, TValue>();

        #endregion Extensions

        void SetPosition(long position);
    }

    public interface IAsyncBinaryReader 
        : IDisposable
    {
        Task<bool> ReadBooleanAsync();
        Task<char> ReadCharAsync();
        Task<byte> ReadByteAsync();
        Task<byte[]> ReadBytesAsync();
        Task<Double> ReadDoubleAsync();
        Task<float> ReadFloatAsync();
        Task<short> ReadShortAsync();
        Task<ushort> ReadUShortAsync();
        Task<Int32> ReadInt32Async();
        Task<UInt32> ReadUInt32Async();
        Task<Int64> ReadLongAsync();
        Task<UInt64> ReadULongAsync();
        Task<string> ReadStringAsync();
        Task<Guid> ReadGuidAsync();
        Task<DateTime?> ReadDateTimeAsync();
        Task<decimal> ReadDecimalAsync();
        Task<TimeSpan> ReadTimeSpanAsync();
        Task<IPAddress> ReadIPAsync();
        Task<IPEndPoint> ReadIPEndpointAsync();

        #region Extensions

        #region Arrays
        Task<T[]> ReadArrayAsync<T>() where T : IAsyncBinarySerializable, new();
        Task<string[]> ReadStringArrayAsync();
        Task<IPAddress[]> ReadIPArrayAsync();
        Task<IPEndPoint[]> ReadIPEndPointArrayAsync();
        Task<Guid[]> ReadGuidArrayAsync();
        Task<DateTime?[]> ReadDateTimeArrayAsync();
        Task<Int64[]> ReadInt64ArrayAsync();
        Task<Int32[]> ReadInt32ArrayAsync();
        Task<UInt64[]> ReadUInt64ArrayAsync();
        Task<UInt32[]> ReadUInt32ArrayAsync();
        Task<char[]> ReadCharArrayAsync();
        Task<short[]> ReadShortArrayAsync();
        Task<ushort[]> ReadUShortArrayAsync();
        Task<float[]> ReadFloatArrayAsync();
        Task<Double[]> ReadDoubleArrayAsync();
        Task<bool[]> ReadBooleanArrayAsync();
        Task<byte[]> ReadByteArrayAsync();
        Task<byte[][]> ReadByteArrayArrayAsync();
        Task<decimal[]> ReadDecimalArrayAsync();
        Task<TimeSpan[]> ReadTimeSpanArrayAsync();
        #endregion

        #region Collections
        Task<List<T>> ReadCollectionAsync<T>() where T : IAsyncBinarySerializable, new();
        Task<List<string>> ReadStringCollectionAsync();
        Task<List<Guid>> ReadGuidCollectionAsync();
        Task<List<DateTime?>> ReadDateTimeCollectionAsync();
        Task<List<char>> ReadCharCollectionAsync();
        Task<List<Int64>> ReadInt64CollectionAsync();
        Task<List<Int32>> ReadInt32CollectionAsync();
        Task<List<Double>> ReadDoubleCollectionAsync();
        Task<List<Decimal>> ReadDecimalCollectionAsync();
        Task<List<TimeSpan>> ReadTimeSpanCollectionAsync();
        Task<List<float>> ReadFloatCollectionAsync();
        Task<List<bool>> ReadBooleanCollectionAsync();
        Task<List<byte>> ReadByteCollectionAsync();
        Task<List<byte[]>> ReadByteArrayCollectionAsync();
        Task<List<IPAddress>> ReadIPCollectionAsync();
        Task<List<IPEndPoint>> ReadIPEndPointCollectionAsync();
        Task<List<UInt64>> ReadUInt64CollectionAsync();
        Task<List<UInt32>> ReadUInt32CollectionAsync();
        Task<List<short>> ReadShortCollectionAsync();
        Task<List<ushort>> ReadUShortCollectionAsync();
        #endregion

        Task<T> ReadAsync<T>() where T : IAsyncBinarySerializable;
        Task<T> ReadAsync<T>(object arg) where T : IAsyncBinarySerializable;
        Task<T> ReadCompatibleAsync<T>();
        Task<Dictionary<TKey, TValue>> ReadDictionaryAsync<TKey, TValue>();
        Task<ConcurrentDictionary<TKey, TValue>> ReadDictionaryAsConcurrentAsync<TKey, TValue>();

        #endregion Extensions
    }
}