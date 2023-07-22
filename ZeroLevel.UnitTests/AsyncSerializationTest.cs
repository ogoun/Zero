using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using ZeroLevel.Network;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Serialization
{
    public class AsyncSerializationTest
    {
        private static bool TestOrderingEquals<T>(IEnumerable<T> A, IEnumerable<T> B, Func<T, T, bool> comparer)
        {
            if (A == null && B == null) return true;
            if (A == null || B == null) return false;
            if (A.Count() != B.Count()) return false;
            var enumA = A.GetEnumerator();
            var enumB = B.GetEnumerator();
            while (enumA.MoveNext() && enumB.MoveNext())
            {
                if (enumA.Current == null && enumB.Current == null) continue;
                if (comparer(enumA.Current, enumB.Current) == false) return false;
            }
            return true;
        }
        private async Task MakePrimitiveAsyncTest<T>(T value, Func<MemoryStreamWriter, T, Task> serializer, Func<MemoryStreamReader, Task<T>> deserializer, Func<T, T, bool> comparator = null)
        {
            // Act            
            var writer = new MemoryStreamWriter();
            await serializer.Invoke(writer, value);
            var data = writer.Complete();

            var reader = new MemoryStreamReader(data);
            var clone = await deserializer.Invoke(reader);

            // Assert
            if (comparator == null)
            {
                Assert.Equal<T>(value, clone);
            }
            else
            {
                Assert.True(comparator(value, clone));
            }
        }


        private async Task AsyncMakeCollectionTest<T>(IEnumerable<T> value, Func<MemoryStreamWriter, IEnumerable<T>, Task> serializer, Func<MemoryStreamReader, Task<IEnumerable<T>>> deserializer, Func<T, T, bool> comparator = null)
        {
            // In-Memory
            // Act
            var writer = new MemoryStreamWriter();
            await serializer.Invoke(writer, value);
            var data = writer.Complete();

            var reader = new MemoryStreamReader(data);
            var clone = await deserializer.Invoke(reader);

            // Assert
            if (value == null && clone != null && !clone.Any()) return; // OK
            if (comparator == null)
            {
                Assert.True(CollectionComparsionExtensions.OrderingEquals(value, clone));
            }
            else
            {
                Assert.True(TestOrderingEquals(value, clone, comparator));
            }

            // FS
            var name = FSUtils.FileNameCorrection(typeof(T).Name) + ".bin";
            if (File.Exists(name))
            {
                File.Delete(name);
            }
            using (var fs_writer = new MemoryStreamWriter(File.OpenWrite(name)))
            {
                await serializer.Invoke(fs_writer, value);
            }

            using (var fs_reader = new MemoryStreamReader(File.OpenRead(name)))
            {
                clone = await deserializer.Invoke(fs_reader);

                // Assert
                if (value == null && clone != null && !clone.Any()) return; // OK
                if (comparator == null)
                {
                    Assert.True(CollectionComparsionExtensions.OrderingEquals(value, clone));
                }
                else
                {
                    Assert.True(TestOrderingEquals(value, clone, comparator));
                }
            }
        }

        #region Primitive types tests
        [Fact]
        public async Task AsyncSerializeDateTime()
        {
            await MakePrimitiveAsyncTest<DateTime>(DateTime.Now, async (w, v) => await w.WriteDateTimeAsync(v), async (r) => (await r.ReadDateTimeAsync()).Value);
            await MakePrimitiveAsyncTest<DateTime>(DateTime.UtcNow, async (w, v) => await w.WriteDateTimeAsync(v), async (r) => (await r.ReadDateTimeAsync()).Value);
            await MakePrimitiveAsyncTest<DateTime>(DateTime.Today, async (w, v) => await w.WriteDateTimeAsync(v), async (r) => (await r.ReadDateTimeAsync()).Value);
            await MakePrimitiveAsyncTest<DateTime>(DateTime.Now.AddYears(2000), async (w, v) => await w.WriteDateTimeAsync(v), async (r) => (await r.ReadDateTimeAsync()).Value);
            await MakePrimitiveAsyncTest<DateTime>(DateTime.MinValue, async (w, v) => await w.WriteDateTimeAsync(v), async (r) => (await r.ReadDateTimeAsync()).Value);
            await MakePrimitiveAsyncTest<DateTime>(DateTime.MaxValue, async (w, v) => await w.WriteDateTimeAsync(v), async (r) => (await r.ReadDateTimeAsync()).Value);
        }

        [Fact]
        public async Task AsyncSerializeNullableDateTime()
        {
            await MakePrimitiveAsyncTest<DateTime?>(null, async (w, v) => await w.WriteDateTimeAsync(v), async (r) => await r.ReadDateTimeAsync());
            await MakePrimitiveAsyncTest<DateTime?>(DateTime.UtcNow, async (w, v) => await w.WriteDateTimeAsync(v), async (r) => await r.ReadDateTimeAsync());
            await MakePrimitiveAsyncTest<DateTime?>(DateTime.Today, async (w, v) => await w.WriteDateTimeAsync(v), async (r) => await r.ReadDateTimeAsync());
            await MakePrimitiveAsyncTest<DateTime?>(DateTime.Now.AddYears(2000), async (w, v) => await w.WriteDateTimeAsync(v), async (r) => await r.ReadDateTimeAsync());
            await MakePrimitiveAsyncTest<DateTime?>(DateTime.MinValue, async (w, v) => await w.WriteDateTimeAsync(v), async (r) => await r.ReadDateTimeAsync());
            await MakePrimitiveAsyncTest<DateTime?>(DateTime.MaxValue, async (w, v) => await w.WriteDateTimeAsync(v), async (r) => await r.ReadDateTimeAsync());
        }

        [Fact]
        public async Task AsyncSerializeIPAddress()
        {
            var comparator = new Func<IPAddress, IPAddress, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            await MakePrimitiveAsyncTest<IPAddress>(null, async (w, v) => await w.WriteIPAsync(v), async (r) => await r.ReadIPAsync(), comparator);
            await MakePrimitiveAsyncTest<IPAddress>(IPAddress.Any, async (w, v) => await w.WriteIPAsync(v), async (r) => await r.ReadIPAsync(), comparator);
            await MakePrimitiveAsyncTest<IPAddress>(IPAddress.Broadcast, async (w, v) => await w.WriteIPAsync(v), async (r) => await r.ReadIPAsync(), comparator);
            await MakePrimitiveAsyncTest<IPAddress>(IPAddress.IPv6Any, async (w, v) => await w.WriteIPAsync(v), async (r) => await r.ReadIPAsync(), comparator);
            await MakePrimitiveAsyncTest<IPAddress>(IPAddress.IPv6Loopback, async (w, v) => await w.WriteIPAsync(v), async (r) => await r.ReadIPAsync(), comparator);
            await MakePrimitiveAsyncTest<IPAddress>(IPAddress.IPv6None, async (w, v) => await w.WriteIPAsync(v), async (r) => await r.ReadIPAsync(), comparator);
            await MakePrimitiveAsyncTest<IPAddress>(IPAddress.Loopback, async (w, v) => await w.WriteIPAsync(v), async (r) => await r.ReadIPAsync(), comparator);
            await MakePrimitiveAsyncTest<IPAddress>(IPAddress.None, async (w, v) => await w.WriteIPAsync(v), async (r) => await r.ReadIPAsync(), comparator);
            await MakePrimitiveAsyncTest<IPAddress>(IPAddress.Parse("93.111.16.58"), async (w, v) => await w.WriteIPAsync(v), async (r) => await r.ReadIPAsync(), comparator);
        }

        [Fact]
        public async Task AsyncSerializeIPEndPoint()
        {
            var comparator = new Func<IPEndPoint, IPEndPoint, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            await MakePrimitiveAsyncTest<IPEndPoint>(null, async (w, v) => await w.WriteIPEndpointAsync(v), async (r) => await r.ReadIPEndpointAsync(), comparator);
            await MakePrimitiveAsyncTest<IPEndPoint>(new IPEndPoint(IPAddress.Any, 1), async (w, v) => await w.WriteIPEndpointAsync(v), async (r) => await r.ReadIPEndpointAsync(), comparator);
            await MakePrimitiveAsyncTest<IPEndPoint>(new IPEndPoint(IPAddress.Broadcast, 600), async (w, v) => await w.WriteIPEndpointAsync(v), async (r) => await r.ReadIPEndpointAsync(), comparator);
            await MakePrimitiveAsyncTest<IPEndPoint>(new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MaxPort), async (w, v) => await w.WriteIPEndpointAsync(v), async (r) => await r.ReadIPEndpointAsync(), comparator);
            await MakePrimitiveAsyncTest<IPEndPoint>(new IPEndPoint(IPAddress.IPv6Loopback, 8080), async (w, v) => await w.WriteIPEndpointAsync(v), async (r) => await r.ReadIPEndpointAsync(), comparator);
            await MakePrimitiveAsyncTest<IPEndPoint>(new IPEndPoint(IPAddress.IPv6None, 80), async (w, v) => await w.WriteIPEndpointAsync(v), async (r) => await r.ReadIPEndpointAsync(), comparator);
            await MakePrimitiveAsyncTest<IPEndPoint>(new IPEndPoint(IPAddress.Loopback, 9000), async (w, v) => await w.WriteIPEndpointAsync(v), async (r) => await r.ReadIPEndpointAsync(), comparator);
            await MakePrimitiveAsyncTest<IPEndPoint>(new IPEndPoint(IPAddress.None, 0), async (w, v) => await w.WriteIPEndpointAsync(v), async (r) => await r.ReadIPEndpointAsync(), comparator);
            await MakePrimitiveAsyncTest<IPEndPoint>(new IPEndPoint(IPAddress.Parse("93.111.16.58"), IPEndPoint.MinPort), async (w, v) => await w.WriteIPEndpointAsync(v), async (r) => await r.ReadIPEndpointAsync(), comparator);
        }

        [Fact]
        public async Task AsyncSerializeGuid()
        {
            await MakePrimitiveAsyncTest<Guid>(Guid.Empty, async (w, v) => await w.WriteGuidAsync(v), async (r) => await r.ReadGuidAsync());
            await MakePrimitiveAsyncTest<Guid>(Guid.NewGuid(), async (w, v) => await w.WriteGuidAsync(v), async (r) => await r.ReadGuidAsync());
        }

        [Fact]
        public async Task AsyncSerializeTimeSpan()
        {
            await MakePrimitiveAsyncTest<TimeSpan>(TimeSpan.MaxValue, async (w, v) => await w.WriteTimeSpanAsync(v), async (r) => await r.ReadTimeSpanAsync());
            await MakePrimitiveAsyncTest<TimeSpan>(TimeSpan.MinValue, async (w, v) => await w.WriteTimeSpanAsync(v), async (r) => await r.ReadTimeSpanAsync());
            await MakePrimitiveAsyncTest<TimeSpan>(TimeSpan.Zero, async (w, v) => await w.WriteTimeSpanAsync(v), async (r) => await r.ReadTimeSpanAsync());
            await MakePrimitiveAsyncTest<TimeSpan>(TimeSpan.FromDays(1024), async (w, v) => await w.WriteTimeSpanAsync(v), async (r) => await r.ReadTimeSpanAsync());
            await MakePrimitiveAsyncTest<TimeSpan>(TimeSpan.FromMilliseconds(1), async (w, v) => await w.WriteTimeSpanAsync(v), async (r) => await r.ReadTimeSpanAsync());
            await MakePrimitiveAsyncTest<TimeSpan>(TimeSpan.FromTicks(1), async (w, v) => await w.WriteTimeSpanAsync(v), async (r) => await r.ReadTimeSpanAsync());
            await MakePrimitiveAsyncTest<TimeSpan>(TimeSpan.FromTicks(0), async (w, v) => await w.WriteTimeSpanAsync(v), async (r) => await r.ReadTimeSpanAsync());
        }


        [Fact]
        public async Task AsyncSerializeString()
        {
            var comparator = new Func<string, string, bool>((left, right) =>
                    (left == null && right == null) ||
                    (left == null && right != null && right.Length == 0) ||
                    (left != null && left.Length == 0 && right == null) ||
                    string.Compare(left, right, StringComparison.InvariantCulture) == 0);
            await MakePrimitiveAsyncTest<String>("", async (w, v) => await w.WriteStringAsync(v), async (r) => await r.ReadStringAsync(), comparator);
            await MakePrimitiveAsyncTest<String>(String.Empty, async (w, v) => await w.WriteStringAsync(v), async (r) => await r.ReadStringAsync(), comparator);
            await MakePrimitiveAsyncTest<String>(null, async (w, v) => await w.WriteStringAsync(v), async (r) => await r.ReadStringAsync(), comparator);
            await MakePrimitiveAsyncTest<String>("HELLO!", async (w, v) => await w.WriteStringAsync(v), async (r) => await r.ReadStringAsync(), comparator);
            await MakePrimitiveAsyncTest<String>("𐌼𐌰𐌲 𐌲𐌻𐌴𐍃 𐌹̈𐍄𐌰𐌽, 𐌽𐌹 𐌼𐌹𐍃 𐍅𐌿 𐌽𐌳𐌰𐌽 𐌱𐍂𐌹𐌲𐌲𐌹𐌸", async (w, v) => await w.WriteStringAsync(v), async (r) => await r.ReadStringAsync(), comparator);
        }


        [Fact]
        public async Task AsyncSerializeInt32()
        {
            await MakePrimitiveAsyncTest<Int32>(-0, async (w, v) => await w.WriteInt32Async(v), async (r) => await r.ReadInt32Async());
            await MakePrimitiveAsyncTest<Int32>(0, async (w, v) => await w.WriteInt32Async(v), async (r) => await r.ReadInt32Async());
            await MakePrimitiveAsyncTest<Int32>(-10, async (w, v) => await w.WriteInt32Async(v), async (r) => await r.ReadInt32Async());
            await MakePrimitiveAsyncTest<Int32>(10, async (w, v) => await w.WriteInt32Async(v), async (r) => await r.ReadInt32Async());
            await MakePrimitiveAsyncTest<Int32>(Int32.MinValue, async (w, v) => await w.WriteInt32Async(v), async (r) => await r.ReadInt32Async());
            await MakePrimitiveAsyncTest<Int32>(Int32.MaxValue, async (w, v) => await w.WriteInt32Async(v), async (r) => await r.ReadInt32Async());
        }

        [Fact]
        public async Task AsyncSerializeUInt32()
        {
            await MakePrimitiveAsyncTest<UInt32>(-0, async (w, v) => await w.WriteUInt32Async(v), async (r) => await r.ReadUInt32Async());
            await MakePrimitiveAsyncTest<UInt32>(0, async (w, v) => await w.WriteUInt32Async(v), async (r) => await r.ReadUInt32Async());
            await MakePrimitiveAsyncTest<UInt32>(10, async (w, v) => await w.WriteUInt32Async(v), async (r) => await r.ReadUInt32Async());
            await MakePrimitiveAsyncTest<UInt32>(UInt32.MinValue, async (w, v) => await w.WriteUInt32Async(v), async (r) => await r.ReadUInt32Async());
            await MakePrimitiveAsyncTest<UInt32>(UInt32.MaxValue, async (w, v) => await w.WriteUInt32Async(v), async (r) => await r.ReadUInt32Async());
        }

        [Fact]
        public async Task AsyncSerializeShort()
        {
            await MakePrimitiveAsyncTest<short>(-0, async (w, v) => await w.WriteShortAsync(v), async (r) => await r.ReadShortAsync());
            await MakePrimitiveAsyncTest<short>(0, async (w, v) => await w.WriteShortAsync(v), async (r) => await r.ReadShortAsync());
            await MakePrimitiveAsyncTest<short>(-10, async (w, v) => await w.WriteShortAsync(v), async (r) => await r.ReadShortAsync());
            await MakePrimitiveAsyncTest<short>(10, async (w, v) => await w.WriteShortAsync(v), async (r) => await r.ReadShortAsync());
            await MakePrimitiveAsyncTest<short>(short.MinValue, async (w, v) => await w.WriteShortAsync(v), async (r) => await r.ReadShortAsync());
            await MakePrimitiveAsyncTest<short>(short.MaxValue, async (w, v) => await w.WriteShortAsync(v), async (r) => await r.ReadShortAsync());
        }

        [Fact]
        public async Task AsyncSerializeUShort()
        {
            await MakePrimitiveAsyncTest<ushort>(-0, async (w, v) => await w.WriteUShortAsync(v), async (r) => await r.ReadUShortAsync());
            await MakePrimitiveAsyncTest<ushort>(0, async (w, v) => await w.WriteUShortAsync(v), async (r) => await r.ReadUShortAsync());
            await MakePrimitiveAsyncTest<ushort>(10, async (w, v) => await w.WriteUShortAsync(v), async (r) => await r.ReadUShortAsync());
            await MakePrimitiveAsyncTest<ushort>(ushort.MinValue, async (w, v) => await w.WriteUShortAsync(v), async (r) => await r.ReadUShortAsync());
            await MakePrimitiveAsyncTest<ushort>(ushort.MaxValue, async (w, v) => await w.WriteUShortAsync(v), async (r) => await r.ReadUShortAsync());
        }

        [Fact]
        public async Task AsyncSerializeInt64()
        {
            await MakePrimitiveAsyncTest<Int64>(-0, async (w, v) => await w.WriteLongAsync(v), async (r) => await r.ReadLongAsync());
            await MakePrimitiveAsyncTest<Int64>(0, async (w, v) => await w.WriteLongAsync(v), async (r) => await r.ReadLongAsync());
            await MakePrimitiveAsyncTest<Int64>(-10, async (w, v) => await w.WriteLongAsync(v), async (r) => await r.ReadLongAsync());
            await MakePrimitiveAsyncTest<Int64>(10, async (w, v) => await w.WriteLongAsync(v), async (r) => await r.ReadLongAsync());
            await MakePrimitiveAsyncTest<Int64>(Int64.MinValue, async (w, v) => await w.WriteLongAsync(v), async (r) => await r.ReadLongAsync());
            await MakePrimitiveAsyncTest<Int64>(Int64.MaxValue, async (w, v) => await w.WriteLongAsync(v), async (r) => await r.ReadLongAsync());
            await MakePrimitiveAsyncTest<Int64>(Int64.MinValue / 2, async (w, v) => await w.WriteLongAsync(v), async (r) => await r.ReadLongAsync());
            await MakePrimitiveAsyncTest<Int64>(Int64.MaxValue / 2, async (w, v) => await w.WriteLongAsync(v), async (r) => await r.ReadLongAsync());
        }

        [Fact]
        public async Task AsyncSerializeUInt64()
        {
            await MakePrimitiveAsyncTest<UInt64>(-0, async (w, v) => await w.WriteULongAsync(v), async (r) => await r.ReadULongAsync());
            await MakePrimitiveAsyncTest<UInt64>(0, async (w, v) => await w.WriteULongAsync(v), async (r) => await r.ReadULongAsync());
            await MakePrimitiveAsyncTest<UInt64>(10, async (w, v) => await w.WriteULongAsync(v), async (r) => await r.ReadULongAsync());
            await MakePrimitiveAsyncTest<UInt64>(UInt64.MinValue, async (w, v) => await w.WriteULongAsync(v), async (r) => await r.ReadULongAsync());
            await MakePrimitiveAsyncTest<UInt64>(UInt64.MaxValue, async (w, v) => await w.WriteULongAsync(v), async (r) => await r.ReadULongAsync());
            await MakePrimitiveAsyncTest<UInt64>(UInt64.MinValue / 2, async (w, v) => await w.WriteULongAsync(v), async (r) => await r.ReadULongAsync());
            await MakePrimitiveAsyncTest<UInt64>(UInt64.MaxValue / 2, async (w, v) => await w.WriteULongAsync(v), async (r) => await r.ReadULongAsync());
        }

        [Fact]
        public async Task AsyncSerializeDecimal()
        {
            await MakePrimitiveAsyncTest<Decimal>(-0, async (w, v) => await w.WriteDecimalAsync(v), async (r) => await r.ReadDecimalAsync());
            await MakePrimitiveAsyncTest<Decimal>(0, async (w, v) => await w.WriteDecimalAsync(v), async (r) => await r.ReadDecimalAsync());
            await MakePrimitiveAsyncTest<Decimal>(-10, async (w, v) => await w.WriteDecimalAsync(v), async (r) => await r.ReadDecimalAsync());
            await MakePrimitiveAsyncTest<Decimal>(10, async (w, v) => await w.WriteDecimalAsync(v), async (r) => await r.ReadDecimalAsync());
            await MakePrimitiveAsyncTest<Decimal>(Decimal.MinValue, async (w, v) => await w.WriteDecimalAsync(v), async (r) => await r.ReadDecimalAsync());
            await MakePrimitiveAsyncTest<Decimal>(Decimal.MaxValue, async (w, v) => await w.WriteDecimalAsync(v), async (r) => await r.ReadDecimalAsync());
            await MakePrimitiveAsyncTest<Decimal>(Decimal.MinValue / 2, async (w, v) => await w.WriteDecimalAsync(v), async (r) => await r.ReadDecimalAsync());
            await MakePrimitiveAsyncTest<Decimal>(Decimal.MaxValue / 2, async (w, v) => await w.WriteDecimalAsync(v), async (r) => await r.ReadDecimalAsync());
        }

        [Fact]
        public async Task AsyncSerializeFloat()
        {
            await MakePrimitiveAsyncTest<float>(-0, async (w, v) => await w.WriteFloatAsync(v), async (r) => await r.ReadFloatAsync());
            await MakePrimitiveAsyncTest<float>(0, async (w, v) => await w.WriteFloatAsync(v), async (r) => await r.ReadFloatAsync());
            await MakePrimitiveAsyncTest<float>(-10, async (w, v) => await w.WriteFloatAsync(v), async (r) => await r.ReadFloatAsync());
            await MakePrimitiveAsyncTest<float>(10, async (w, v) => await w.WriteFloatAsync(v), async (r) => await r.ReadFloatAsync());
            await MakePrimitiveAsyncTest<float>(float.MinValue, async (w, v) => await w.WriteFloatAsync(v), async (r) => await r.ReadFloatAsync());
            await MakePrimitiveAsyncTest<float>(float.MaxValue, async (w, v) => await w.WriteFloatAsync(v), async (r) => await r.ReadFloatAsync());
            await MakePrimitiveAsyncTest<float>(float.MinValue / 2, async (w, v) => await w.WriteFloatAsync(v), async (r) => await r.ReadFloatAsync());
            await MakePrimitiveAsyncTest<float>(float.MaxValue / 2, async (w, v) => await w.WriteFloatAsync(v), async (r) => await r.ReadFloatAsync());
        }

        [Fact]
        public async Task AsyncSerializeDouble()
        {
            await MakePrimitiveAsyncTest<Double>(-0, async (w, v) => await w.WriteDoubleAsync(v), async (r) => await r.ReadDoubleAsync());
            await MakePrimitiveAsyncTest<Double>(0, async (w, v) => await w.WriteDoubleAsync(v), async (r) => await r.ReadDoubleAsync());
            await MakePrimitiveAsyncTest<Double>(-10, async (w, v) => await w.WriteDoubleAsync(v), async (r) => await r.ReadDoubleAsync());
            await MakePrimitiveAsyncTest<Double>(10, async (w, v) => await w.WriteDoubleAsync(v), async (r) => await r.ReadDoubleAsync());
            await MakePrimitiveAsyncTest<Double>(Double.MinValue, async (w, v) => await w.WriteDoubleAsync(v), async (r) => await r.ReadDoubleAsync());
            await MakePrimitiveAsyncTest<Double>(Double.MaxValue, async (w, v) => await w.WriteDoubleAsync(v), async (r) => await r.ReadDoubleAsync());
            await MakePrimitiveAsyncTest<Double>(Double.MinValue / 2, async (w, v) => await w.WriteDoubleAsync(v), async (r) => await r.ReadDoubleAsync());
            await MakePrimitiveAsyncTest<Double>(Double.MaxValue / 2, async (w, v) => await w.WriteDoubleAsync(v), async (r) => await r.ReadDoubleAsync());
        }

        [Fact]
        public async Task AsyncSerializeBytes()
        {
            var comparator = new Func<byte[], byte[], bool>((left, right) =>
                (left == null && (right == null || right.Length == 0)) || ArrayExtensions.UnsafeEquals(left, right));
            await MakePrimitiveAsyncTest<Byte[]>(null, async (w, v) => await w.WriteBytesAsync(v), async (r) => await r.ReadBytesAsync(), comparator);
            await MakePrimitiveAsyncTest<Byte[]>(new byte[] { }, async (w, v) => await w.WriteBytesAsync(v), async (r) => await r.ReadBytesAsync(), comparator);
            await MakePrimitiveAsyncTest<Byte[]>(new byte[] { 1 }, async (w, v) => await w.WriteBytesAsync(v), async (r) => await r.ReadBytesAsync(), comparator);
            await MakePrimitiveAsyncTest<Byte[]>(new byte[] { 0, 1, 10, 100, 128, 255 }, async (w, v) => await w.WriteBytesAsync(v), async (r) => await r.ReadBytesAsync(), comparator);
        }
        #endregion

        #region Collection tests
        [Fact]
        public async Task AsyncSerializeCollectionDateTime()
        {
            await AsyncMakeCollectionTest<DateTime?>(null, async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadDateTimeCollectionAsync());
            await AsyncMakeCollectionTest<DateTime?>(new DateTime?[] { }, async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadDateTimeCollectionAsync());
            await AsyncMakeCollectionTest<DateTime?>(new DateTime?[] { DateTime.Now, DateTime.UtcNow, DateTime.Today, DateTime.Now.AddYears(2000), null, DateTime.MinValue, DateTime.MaxValue }, async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadDateTimeCollectionAsync());
        }

        [Fact]
        public async Task AsyncSerializeCollectionIPAddress()
        {
            var comparator = new Func<IPAddress, IPAddress, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            await AsyncMakeCollectionTest<IPAddress>(null, async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadIPCollectionAsync());
            await AsyncMakeCollectionTest<IPAddress>(new IPAddress[] { IPAddress.Any, IPAddress.Broadcast, IPAddress.IPv6Any, IPAddress.IPv6Loopback, IPAddress.IPv6None, IPAddress.Loopback, IPAddress.None, IPAddress.Parse("93.111.16.58") }, 
                async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadIPCollectionAsync(), 
                comparator);
        }

        [Fact]
        public async Task AsyncSerializeCollectionIPEndPoint()
        {
            var comparator = new Func<IPEndPoint, IPEndPoint, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            await AsyncMakeCollectionTest<IPEndPoint>(null, async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadIPEndPointCollectionAsync());
            await AsyncMakeCollectionTest<IPEndPoint>(new IPEndPoint[] { }, async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadIPEndPointCollectionAsync());
            await AsyncMakeCollectionTest<IPEndPoint>(new IPEndPoint[] { new IPEndPoint(IPAddress.Any, 1), new IPEndPoint(IPAddress.Broadcast, 600), new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MaxPort), new IPEndPoint(IPAddress.IPv6Loopback, 8080), new IPEndPoint(IPAddress.IPv6None, 80), new IPEndPoint(IPAddress.Loopback, 9000), new IPEndPoint(IPAddress.None, 0), new IPEndPoint(IPAddress.Parse("93.111.16.58"), IPEndPoint.MinPort) }
            , async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadIPEndPointCollectionAsync()
            , comparator);
        }

        [Fact]
        public async Task AsyncSerializeCollectionGuid()
        {
            await AsyncMakeCollectionTest<Guid>(null, async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadGuidCollectionAsync());
            await AsyncMakeCollectionTest<Guid>(new Guid[] { }, async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadGuidCollectionAsync());
            await AsyncMakeCollectionTest<Guid>(new Guid[] { Guid.Empty, Guid.NewGuid() }, async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadGuidCollectionAsync());
        }

        [Fact]
        public async Task AsyncSerializeCollectionTimeSpan()
        {
            await AsyncMakeCollectionTest<TimeSpan>(new TimeSpan[] { TimeSpan.MaxValue, TimeSpan.MinValue, TimeSpan.Zero, TimeSpan.FromDays(1024), TimeSpan.FromMilliseconds(1), TimeSpan.FromTicks(1), TimeSpan.FromTicks(0) }
                    , async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadTimeSpanCollectionAsync());
        }

        [Fact]
        public async Task AsyncSerializeCollectionString()
        {
            var comparator = new Func<string, string, bool>((left, right) =>
                    (left == null && right == null) ||
                    (left == null && right != null && right.Length == 0) ||
                    (left != null && left.Length == 0 && right == null) ||
                    string.Compare(left, right, StringComparison.InvariantCulture) == 0);
            await AsyncMakeCollectionTest<String>(new string[] { "", String.Empty, null, "HELLO!", "𐌼𐌰𐌲 𐌲𐌻𐌴𐍃 𐌹̈𐍄𐌰𐌽, 𐌽𐌹 𐌼𐌹𐍃 𐍅𐌿 𐌽𐌳𐌰𐌽 𐌱𐍂𐌹𐌲𐌲𐌹𐌸" }
                , async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadStringCollectionAsync()
                , comparator);
        }


        [Fact]
        public async Task AsyncSerializeCollectionInt32()
        {
            await AsyncMakeCollectionTest<Int32>(new int[] { -0, 0, -10, 10, Int32.MinValue, Int32.MaxValue }
                , async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadInt32CollectionAsync());
        }

        [Fact]
        public async Task AsyncSerializeCollectionInt64()
        {
            await AsyncMakeCollectionTest<Int64>(new long[] { -0, 0, -10, 10, Int64.MinValue, Int64.MaxValue, Int64.MinValue / 2, Int64.MaxValue / 2 }
                , async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadInt64CollectionAsync());
        }

        [Fact]
        public async Task AsyncSerializeCollectionDecimal()
        {
            await AsyncMakeCollectionTest<Decimal>(new Decimal[] { -0, 0, -10, 10, Decimal.MinValue, Decimal.MaxValue, Decimal.MinValue / 2, Decimal.MaxValue / 2 }
            , async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadDecimalCollectionAsync());
        }

        [Fact]
        public async Task AsyncSerializeCollectionFloat()
        {
            await AsyncMakeCollectionTest<float>(new float[] { -0, 0, -10, 10, float.MinValue, float.MaxValue, float.MinValue / 2, float.MaxValue / 2 }
            , async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadFloatCollectionAsync());
        }

        [Fact]
        public async Task AsyncSerializeCollectionDouble()
        {
            await AsyncMakeCollectionTest<Double>(new Double[] { -0, 0, -10, 10, Double.MinValue, Double.MaxValue, Double.MinValue / 2, Double.MaxValue / 2 }
            , async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadDoubleCollectionAsync());
        }

        [Fact]
        public async Task AsyncSerializeCollectionBoolean()
        {
            await AsyncMakeCollectionTest<Boolean>(new Boolean[] { true, false, true }
            , async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadBooleanCollectionAsync());
        }

        [Fact]
        public async Task AsyncSerializeCollectionByte()
        {
            await AsyncMakeCollectionTest<Byte>(new byte[] { 0, 3, -0, 1, 10, 128, 255 }
            , async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadByteCollectionAsync());
        }

        [Fact]
        public async Task AsyncSerializeCollectionBytes()
        {
            var comparator = new Func<byte[], byte[], bool>((left, right) =>
                (left == null && (right == null || right.Length == 0)) || ArrayExtensions.UnsafeEquals(left, right));

            await AsyncMakeCollectionTest<Byte[]>(new Byte[][] { null, new byte[] { }, new byte[] { 1 }, new byte[] { 0, 1, 10, 100, 128, 255 } }
            , async (w, v) => await w.WriteCollectionAsync(v), async r => await r.ReadByteArrayCollectionAsync()
            , comparator);
        }

        #endregion
    }
}
