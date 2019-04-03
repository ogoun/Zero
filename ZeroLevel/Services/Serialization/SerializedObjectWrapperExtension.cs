using System;
using System.Collections.Generic;
using System.Net;

namespace ZeroLevel.Services.Serialization
{
    public static class SerializedObjectWrapperExtension
    {
        public static SerializedObjectWrapper<Int32> WrapToSerialized(this Int32 value) => new SerializedObjectWrapper<Int32> { Value = value };

        public static SerializedObjectWrapper<Boolean> WrapToSerialized(this Boolean value) => new SerializedObjectWrapper<Boolean> { Value = value };

        public static SerializedObjectWrapper<Byte> WrapToSerialized(this Byte value) => new SerializedObjectWrapper<Byte> { Value = value };

        public static SerializedObjectWrapper<Byte[]> WrapToSerialized(this Byte[] value) => new SerializedObjectWrapper<Byte[]> { Value = value };

        public static SerializedObjectWrapper<DateTime> WrapToSerialized(this DateTime value) => new SerializedObjectWrapper<DateTime> { Value = value };

        public static SerializedObjectWrapper<Decimal> WrapToSerialized(this Decimal value) => new SerializedObjectWrapper<Decimal> { Value = value };

        public static SerializedObjectWrapper<Double> WrapToSerialized(this Double value) => new SerializedObjectWrapper<Double> { Value = value };

        public static SerializedObjectWrapper<Guid> WrapToSerialized(this Guid value) => new SerializedObjectWrapper<Guid> { Value = value };

        public static SerializedObjectWrapper<IPAddress> WrapToSerialized(this IPAddress value) => new SerializedObjectWrapper<IPAddress> { Value = value };

        public static SerializedObjectWrapper<IPEndPoint> WrapToSerialized(this IPEndPoint value) => new SerializedObjectWrapper<IPEndPoint> { Value = value };

        public static SerializedObjectWrapper<Int64> WrapToSerialized(this Int64 value) => new SerializedObjectWrapper<Int64> { Value = value };

        public static SerializedObjectWrapper<String> WrapToSerialized(this String value) => new SerializedObjectWrapper<String> { Value = value };

        public static SerializedObjectWrapper<TimeSpan> WrapToSerialized(this TimeSpan value) => new SerializedObjectWrapper<TimeSpan> { Value = value };

        public static SerializedObjectWrapper<IEnumerable<Int32>> WrapToSerialized(this IEnumerable<Int32> value) => new SerializedObjectWrapper<IEnumerable<Int32>> { Value = value };

        public static SerializedObjectWrapper<IEnumerable<Boolean>> WrapToSerialized(this IEnumerable<Boolean> value) => new SerializedObjectWrapper<IEnumerable<Boolean>> { Value = value };

        public static SerializedObjectWrapper<IEnumerable<Byte>> WrapToSerialized(this IEnumerable<Byte> value) => new SerializedObjectWrapper<IEnumerable<Byte>> { Value = value };

        public static SerializedObjectWrapper<IEnumerable<Byte[]>> WrapToSerialized(this IEnumerable<Byte[]> value) => new SerializedObjectWrapper<IEnumerable<Byte[]>> { Value = value };

        public static SerializedObjectWrapper<IEnumerable<DateTime>> WrapToSerialized(this IEnumerable<DateTime> value) => new SerializedObjectWrapper<IEnumerable<DateTime>> { Value = value };

        public static SerializedObjectWrapper<IEnumerable<Double>> WrapToSerialized(this IEnumerable<Double> value) => new SerializedObjectWrapper<IEnumerable<Double>> { Value = value };

        public static SerializedObjectWrapper<IEnumerable<Guid>> WrapToSerialized(this IEnumerable<Guid> value) => new SerializedObjectWrapper<IEnumerable<Guid>> { Value = value };

        public static SerializedObjectWrapper<IEnumerable<IPAddress>> WrapToSerialized(this IEnumerable<IPAddress> value) => new SerializedObjectWrapper<IEnumerable<IPAddress>> { Value = value };

        public static SerializedObjectWrapper<IEnumerable<IPEndPoint>> WrapToSerialized(this IEnumerable<IPEndPoint> value) => new SerializedObjectWrapper<IEnumerable<IPEndPoint>> { Value = value };

        public static SerializedObjectWrapper<IEnumerable<Int64>> WrapToSerialized(this IEnumerable<Int64> value) => new SerializedObjectWrapper<IEnumerable<Int64>> { Value = value };

        public static SerializedObjectWrapper<IEnumerable<String>> WrapToSerialized(this IEnumerable<String> value) => new SerializedObjectWrapper<IEnumerable<String>> { Value = value };

        public static SerializedObjectWrapper<IEnumerable<T>> WrapToSerialized<T>(this IEnumerable<T> value) where T : IBinarySerializable
            => new SerializedObjectWrapper<IEnumerable<T>> { Value = value };
    }
}