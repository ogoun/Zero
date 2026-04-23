using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Serialization
{
    public interface IBinaryWriter
        : IDisposable
    {
        void WriteBoolean(bool val);

        void WriteChar(char val);

        void WriteByte(byte val);

        void WriteSByte(sbyte val);

        void WriteBytes(byte[] val);

        void WriteShort(short number);

        void WriteUShort(ushort number);

        void WriteDouble(double val);

        void WriteFloat(float val);

        void WriteInt32(Int32 number);

        void WriteUInt32(UInt32 number);

        void WriteLong(Int64 number);

        void WriteULong(UInt64 number);

        void WriteString(string line);

        void WriteGuid(Guid guid);

        void WriteDateTime(DateTime? datetime);

        void WriteDateTimeOffset(DateTimeOffset? datetime);

        void WriteDecimal(Decimal number);

        void WriteTimeSpan(TimeSpan period);

        void WriteIP(IPAddress ip);

        void WriteIPEndpoint(IPEndPoint endpoint);

        void WriteUri(Uri uri);

        void WriteVersion(Version version);

        void WriteBitArray(BitArray bits);

        void WriteEnum<T>(T value) where T : struct, Enum;

        #region Nullable primitives
        void WriteBooleanNullable(bool? val);
        void WriteByteNullable(byte? val);
        void WriteSByteNullable(sbyte? val);
        void WriteCharNullable(char? val);
        void WriteShortNullable(short? val);
        void WriteUShortNullable(ushort? val);
        void WriteInt32Nullable(int? val);
        void WriteUInt32Nullable(uint? val);
        void WriteLongNullable(long? val);
        void WriteULongNullable(ulong? val);
        void WriteFloatNullable(float? val);
        void WriteDoubleNullable(double? val);
        void WriteDecimalNullable(decimal? val);
        void WriteTimeSpanNullable(TimeSpan? val);
        void WriteGuidNullable(Guid? val);
        #endregion

        #region Extensions

        #region Arrays
        void WriteArray<T>(T[] array) where T : IBinarySerializable;
        void WriteArray(string[] array);
        void WriteArray(IPAddress[] array);
        void WriteArray(IPEndPoint[] array);
        void WriteArray(Uri[] array);
        void WriteArray(Version[] array);
        void WriteArray(BitArray[] array);
        void WriteArray(Guid[] array);
        void WriteArray(DateTime[] array);
        void WriteArray(DateTime?[] array);
        void WriteArray(DateTimeOffset[] array);
        void WriteArray(DateTimeOffset?[] array);
        void WriteArray(UInt64[] array);
        void WriteArray(UInt32[] array);
        void WriteArray(char[] array);
        void WriteArray(short[] array);
        void WriteArray(ushort[] array);
        void WriteArray(Int64[] array);
        void WriteArray(Int32[] array);
        void WriteArray(float[] array);
        void WriteArray(Double[] array);
        void WriteArray(bool[] array);
        void WriteArray(byte[] array);
        void WriteArray(byte[][] array);
        void WriteArray(sbyte[] array);
        void WriteArray(decimal[] array);
        void WriteArray(TimeSpan[] array);
        #endregion

        #region Collections
        void WriteCollection<T>(IEnumerable<T> collection)
            where T : IBinarySerializable;
        void WriteCollection(IEnumerable<string> collection);
        void WriteCollection(IEnumerable<char> collection);
        void WriteCollection(IEnumerable<Guid> collection);
        void WriteCollection(IEnumerable<DateTime> collection);
        void WriteCollection(IEnumerable<DateTime?> collection);
        void WriteCollection(IEnumerable<DateTimeOffset> collection);
        void WriteCollection(IEnumerable<DateTimeOffset?> collection);
        void WriteCollection(IEnumerable<Int64> collection);
        void WriteCollection(IEnumerable<Int32> collection);
        void WriteCollection(IEnumerable<UInt64> collection);
        void WriteCollection(IEnumerable<UInt32> collection);
        void WriteCollection(IEnumerable<short> collection);
        void WriteCollection(IEnumerable<ushort> collection);
        void WriteCollection(IEnumerable<Double> collection);
        void WriteCollection(IEnumerable<Decimal> collection);
        void WriteCollection(IEnumerable<TimeSpan> collection);
        void WriteCollection(IEnumerable<float> collection);
        void WriteCollection(IEnumerable<bool> collection);
        void WriteCollection(IEnumerable<byte> collection);
        void WriteCollection(IEnumerable<byte[]> collection);
        void WriteCollection(IEnumerable<sbyte> collection);
        void WriteCollection(IEnumerable<IPEndPoint> collection);
        void WriteCollection(IEnumerable<IPAddress> collection);
        void WriteCollection(IEnumerable<Uri> collection);
        void WriteCollection(IEnumerable<Version> collection);
        void WriteCollection(IEnumerable<BitArray> collection);
        #endregion

        void WriteDictionary<TKey, TValue>(IDictionary<TKey, TValue> collection);
        void WriteDictionary<TKey, TValue>(ConcurrentDictionary<TKey, TValue> collection);

        void WriteKeyValuePair<TKey, TValue>(KeyValuePair<TKey, TValue> pair);

        void WriteValueTuple<T1, T2>((T1, T2) value);

        void Write<T>(T item)
            where T : IBinarySerializable;

        void WriteCompatible<T>(T item);

        #endregion Extensions

        Stream Stream { get; }
    }

    public interface IAsyncBinaryWriter
        : IDisposable
    {
        Task WriteCharAsync(char val);

        Task WriteBytesAsync(byte[] val);

        Task WriteSByteAsync(sbyte val);

        Task WriteShortAsync(short number);

        Task WriteUShortAsync(ushort number);

        Task WriteDoubleAsync(double val);

        Task WriteFloatAsync(float val);

        Task WriteInt32Async(Int32 number);

        Task WriteUInt32Async(UInt32 number);

        Task WriteLongAsync(Int64 number);

        Task WriteULongAsync(UInt64 number);

        Task WriteStringAsync(string line);

        Task WriteGuidAsync(Guid guid);

        Task WriteDateTimeAsync(DateTime? datetime);

        Task WriteDateTimeOffsetAsync(DateTimeOffset? datetime);
        Task WriteDecimalAsync(Decimal number);

        Task WriteTimeSpanAsync(TimeSpan period);

        Task WriteIPAsync(IPAddress ip);

        Task WriteIPEndpointAsync(IPEndPoint endpoint);

        Task WriteUriAsync(Uri uri);

        Task WriteVersionAsync(Version version);

        Task WriteBitArrayAsync(BitArray bits);

        Task WriteEnumAsync<T>(T value) where T : struct, Enum;

        #region Nullable primitives
        Task WriteBooleanNullableAsync(bool? val);
        Task WriteByteNullableAsync(byte? val);
        Task WriteSByteNullableAsync(sbyte? val);
        Task WriteCharNullableAsync(char? val);
        Task WriteShortNullableAsync(short? val);
        Task WriteUShortNullableAsync(ushort? val);
        Task WriteInt32NullableAsync(int? val);
        Task WriteUInt32NullableAsync(uint? val);
        Task WriteLongNullableAsync(long? val);
        Task WriteULongNullableAsync(ulong? val);
        Task WriteFloatNullableAsync(float? val);
        Task WriteDoubleNullableAsync(double? val);
        Task WriteDecimalNullableAsync(decimal? val);
        Task WriteTimeSpanNullableAsync(TimeSpan? val);
        Task WriteGuidNullableAsync(Guid? val);
        #endregion

        #region Extensions

        #region Arrays
        Task WriteArrayAsync<T>(T[] array) 
            where T : IAsyncBinarySerializable;
        Task WriteArrayAsync(string[] array);
        Task WriteArrayAsync(IPAddress[] array);
        Task WriteArrayAsync(IPEndPoint[] array);
        Task WriteArrayAsync(Uri[] array);
        Task WriteArrayAsync(Version[] array);
        Task WriteArrayAsync(BitArray[] array);
        Task WriteArrayAsync(Guid[] array);
        Task WriteArrayAsync(DateTime[] array);
        Task WriteArrayAsync(DateTime?[] array);
        Task WriteArrayAsync(UInt64[] array);
        Task WriteArrayAsync(UInt32[] array);
        Task WriteArrayAsync(char[] array);
        Task WriteArrayAsync(short[] array);
        Task WriteArrayAsync(ushort[] array);
        Task WriteArrayAsync(Int64[] array);
        Task WriteArrayAsync(Int32[] array);
        Task WriteArrayAsync(float[] array);
        Task WriteArrayAsync(Double[] array);
        Task WriteArrayAsync(bool[] array);
        Task WriteArrayAsync(byte[] array);
        Task WriteArrayAsync(byte[][] array);
        Task WriteArrayAsync(sbyte[] array);
        Task WriteArrayAsync(decimal[] array);
        Task WriteArrayAsync(TimeSpan[] array);
        #endregion

        #region Collections
        Task WriteCollectionAsync<T>(IEnumerable<T> collection)
            where T : IAsyncBinarySerializable;
        Task WriteCollectionAsync(IEnumerable<string> collection);
        Task WriteCollectionAsync(IEnumerable<char> collection);
        Task WriteCollectionAsync(IEnumerable<Guid> collection);
        Task WriteCollectionAsync(IEnumerable<DateTime> collection);
        Task WriteCollectionAsync(IEnumerable<DateTime?> collection);
        Task WriteCollectionAsync(IEnumerable<Int64> collection);
        Task WriteCollectionAsync(IEnumerable<Int32> collection);
        Task WriteCollectionAsync(IEnumerable<UInt64> collection);
        Task WriteCollectionAsync(IEnumerable<UInt32> collection);
        Task WriteCollectionAsync(IEnumerable<short> collection);
        Task WriteCollectionAsync(IEnumerable<ushort> collection);
        Task WriteCollectionAsync(IEnumerable<Double> collection);
        Task WriteCollectionAsync(IEnumerable<Decimal> collection);
        Task WriteCollectionAsync(IEnumerable<TimeSpan> collection);
        Task WriteCollectionAsync(IEnumerable<float> collection);
        Task WriteCollectionAsync(IEnumerable<bool> collection);
        Task WriteCollectionAsync(IEnumerable<byte> collection);
        Task WriteCollectionAsync(IEnumerable<byte[]> collection);
        Task WriteCollectionAsync(IEnumerable<sbyte> collection);
        Task WriteCollectionAsync(IEnumerable<IPEndPoint> collection);
        Task WriteCollectionAsync(IEnumerable<IPAddress> collection);
        Task WriteCollectionAsync(IEnumerable<Uri> collection);
        Task WriteCollectionAsync(IEnumerable<Version> collection);
        Task WriteCollectionAsync(IEnumerable<BitArray> collection);
        #endregion

        Task WriteDictionaryAsync<TKey, TValue>(IDictionary<TKey, TValue> collection);
        Task WriteDictionaryAsync<TKey, TValue>(ConcurrentDictionary<TKey, TValue> collection);

        Task WriteKeyValuePairAsync<TKey, TValue>(KeyValuePair<TKey, TValue> pair);

        Task WriteValueTupleAsync<T1, T2>((T1, T2) value);

        Task WriteAsync<T>(T item)
            where T : IAsyncBinarySerializable;

        Task WriteCompatibleAsync<T>(T item);

        #endregion Extensions

        Stream Stream { get; }
    }
}