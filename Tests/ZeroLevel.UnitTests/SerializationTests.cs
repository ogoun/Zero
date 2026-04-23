using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Xunit;
using ZeroLevel.DocumentObjectModel;
using ZeroLevel.Models;
using ZeroLevel.Network;
using ZeroLevel.Services.Serialization;
using ZeroLevel.UnitTests.Models;

namespace ZeroLevel.Serialization
{
    public class SerializationTests
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

        private void MakePrimitiveTest<T>(T value, Func<T, T, bool> comparator = null)
        {
            // Act
            var data = MessageSerializer.SerializeCompatible<T>(value);
            var clone = MessageSerializer.DeserializeCompatible<T>(data);

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


        private void MakeCollectionTest<T>(IEnumerable<T> value, Func<T, T, bool> comparator = null)
        {
            // Act
            var data = MessageSerializer.SerializeCompatible<IEnumerable<T>>(value);
            var clone = MessageSerializer.DeserializeCompatible<IEnumerable<T>>(data);

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

        private void MakeDictionaryTest<TKey, TValue>(Dictionary<TKey, TValue> value
            , Func<TKey, TKey, bool> keyComparator = null
            , Func<TValue, TValue, bool> valueComparator = null)
        {
            byte[] data;
            Dictionary<TKey, TValue> clone;
            // Act
            using (var writer = new MemoryStreamWriter())
            {
                writer.WriteDictionary<TKey, TValue>(value);
                data = writer.Complete();
            }
            using (var reader = new MemoryStreamReader(data))
            {
                clone = reader.ReadDictionary<TKey, TValue>();
            }

            // Assert
            if (value == null && clone != null && !clone.Any()) return; // OK

            if (value != null && clone == null) throw new Exception("Fail");
            var original_keys = value.Keys.ToArray();
            var clone_keys = clone.Keys.ToArray();

            if (keyComparator == null)
            {
                Assert.True(CollectionComparsionExtensions.NoOrderingEquals(original_keys, clone_keys));
            }
            else
            {
                Assert.True(CollectionComparsionExtensions.NoOrderingEquals(original_keys, clone_keys, keyComparator));
            }
            foreach (var key in original_keys)
            {
                if (valueComparator == null)
                {
                    Assert.Equal(value[key], clone[key]);
                }
                else
                {
                    Assert.True(valueComparator(value[key], clone[key]));
                }
            }
        }

        [Fact]
        public void SerializeDateTime()
        {
            MakePrimitiveTest<DateTime>(DateTime.Now);
            MakePrimitiveTest<DateTime>(DateTime.UtcNow);
            MakePrimitiveTest<DateTime>(DateTime.Today);
            MakePrimitiveTest<DateTime>(DateTime.Now.AddYears(2000));
            MakePrimitiveTest<DateTime>(DateTime.MinValue);
            MakePrimitiveTest<DateTime>(DateTime.MaxValue);

            MakePrimitiveTest<DateTimeOffset>(DateTimeOffset.Now);
            MakePrimitiveTest<DateTimeOffset>(DateTimeOffset.UtcNow);
            MakePrimitiveTest<DateTimeOffset>(DateTimeOffset.Now.AddYears(2000));
            MakePrimitiveTest<DateTimeOffset>(DateTimeOffset.MinValue);
            MakePrimitiveTest<DateTimeOffset>(DateTimeOffset.MaxValue);

            var testData = new DataRequest
            {
                Data = null,
                Symbol = "BTC",
                Start = new DateTimeOffset(2025, 10, 10, 14, 00, 00, TimeSpan.Zero),
                End = new DateTimeOffset(2025, 10, 10, 16, 00, 00, TimeSpan.Zero)
            };
            var bytes = MessageSerializer.Serialize(testData);
            var restored = MessageSerializer.Deserialize<DataRequest>(bytes);

            Assert.Equal(testData.Start, restored.Start);
            Assert.Equal(testData.End, restored.End);
            Assert.Equal(testData.Symbol, restored.Symbol);
        }

        [Fact]
        public void SerializeIPAddress()
        {
            var comparator = new Func<IPAddress, IPAddress, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            MakePrimitiveTest<IPAddress>(null, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.Any, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.Broadcast, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.IPv6Any, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.IPv6Loopback, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.IPv6None, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.Loopback, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.None, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.Parse("93.111.16.58"), comparator);
        }

        [Fact]
        public void SerializeIPEndPoint()
        {
            var comparator = new Func<IPEndPoint, IPEndPoint, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            MakePrimitiveTest<IPEndPoint>(null, comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.Any, 1), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.Broadcast, 600), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MaxPort), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.IPv6Loopback, 8080), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.IPv6None, 80), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.Loopback, 9000), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.None, 0), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.Parse("93.111.16.58"), IPEndPoint.MinPort), comparator);
        }

        [Fact]
        public void SerializeGuid()
        {
            MakePrimitiveTest<Guid>(Guid.Empty);
            MakePrimitiveTest<Guid>(Guid.NewGuid());
        }

        [Fact]
        public void SerializeTimeSpan()
        {
            MakePrimitiveTest<TimeSpan>(TimeSpan.MaxValue);
            MakePrimitiveTest<TimeSpan>(TimeSpan.MinValue);
            MakePrimitiveTest<TimeSpan>(TimeSpan.Zero);
            MakePrimitiveTest<TimeSpan>(TimeSpan.FromDays(1024));
            MakePrimitiveTest<TimeSpan>(TimeSpan.FromMilliseconds(1));
            MakePrimitiveTest<TimeSpan>(TimeSpan.FromTicks(1));
            MakePrimitiveTest<TimeSpan>(TimeSpan.FromTicks(0));
        }

        [Fact]
        public void SerializeString()
        {
            var comparator = new Func<string, string, bool>((left, right) =>
                    (left == null && right == null) ||
                    (left == null && right != null && right.Length == 0) ||
                    (left != null && left.Length == 0 && right == null) ||
                    string.Compare(left, right, StringComparison.InvariantCulture) == 0);
            MakePrimitiveTest<String>("", comparator);
            MakePrimitiveTest<String>(String.Empty, comparator);
            MakePrimitiveTest<String>(null, comparator);
            MakePrimitiveTest<String>("HELLO!", comparator);
            MakePrimitiveTest<String>("𐌼𐌰𐌲 𐌲𐌻𐌴𐍃 𐌹̈𐍄𐌰𐌽, 𐌽𐌹 𐌼𐌹𐍃 𐍅𐌿 𐌽𐌳𐌰𐌽 𐌱𐍂𐌹𐌲𐌲𐌹𐌸", comparator);
        }
        [Fact]
        public void SerizlizeCharText()
        {
            // Arrange
            var line = "abcxyzABCZА-Яа-яёЁйЙ123";

            // Act
            var bytes = line.Select(ch => MessageSerializer.SerializeCompatible<char>(ch));

            // Assert
            var testLine = new string(bytes.Select(ba => MessageSerializer.DeserializeCompatible<char>(ba)).ToArray());

            Assert.Equal(line, testLine);
        }
        [Fact]
        public void SerializeInt32()
        {
            MakePrimitiveTest<Int32>(-0);
            MakePrimitiveTest<Int32>(0);
            MakePrimitiveTest<Int32>(-10);
            MakePrimitiveTest<Int32>(10);
            MakePrimitiveTest<Int32>(Int32.MinValue);
            MakePrimitiveTest<Int32>(Int32.MaxValue);
        }

        [Fact]
        public void SerializeUInt32()
        {
            MakePrimitiveTest<UInt32>(-0);
            MakePrimitiveTest<UInt32>(0);
            MakePrimitiveTest<UInt32>(10);
            MakePrimitiveTest<UInt32>(UInt32.MinValue);
            MakePrimitiveTest<UInt32>(UInt32.MaxValue);
        }

        [Fact]
        public void SerializeShort()
        {
            MakePrimitiveTest<short>(-0);
            MakePrimitiveTest<short>(0);
            MakePrimitiveTest<short>(-10);
            MakePrimitiveTest<short>(10);
            MakePrimitiveTest<short>(short.MinValue);
            MakePrimitiveTest<short>(short.MaxValue);
        }

        [Fact]
        public void SerializeUShort()
        {
            MakePrimitiveTest<ushort>(-0);
            MakePrimitiveTest<ushort>(0);
            MakePrimitiveTest<ushort>(10);
            MakePrimitiveTest<ushort>(ushort.MinValue);
            MakePrimitiveTest<ushort>(ushort.MaxValue);
        }

        [Fact]
        public void SerializeInt64()
        {
            MakePrimitiveTest<Int64>(-0);
            MakePrimitiveTest<Int64>(0);
            MakePrimitiveTest<Int64>(-10);
            MakePrimitiveTest<Int64>(10);
            MakePrimitiveTest<Int64>(Int64.MinValue);
            MakePrimitiveTest<Int64>(Int64.MaxValue);
            MakePrimitiveTest<Int64>(Int64.MinValue / 2);
            MakePrimitiveTest<Int64>(Int64.MaxValue / 2);
        }

        [Fact]
        public void SerializeUInt64()
        {
            MakePrimitiveTest<UInt64>(-0);
            MakePrimitiveTest<UInt64>(0);
            MakePrimitiveTest<UInt64>(10);
            MakePrimitiveTest<UInt64>(UInt64.MinValue);
            MakePrimitiveTest<UInt64>(UInt64.MaxValue);
            MakePrimitiveTest<UInt64>(UInt64.MinValue / 2);
            MakePrimitiveTest<UInt64>(UInt64.MaxValue / 2);
        }

        [Fact]
        public void SerializeDecimal()
        {
            MakePrimitiveTest<Decimal>(-0);
            MakePrimitiveTest<Decimal>(0);
            MakePrimitiveTest<Decimal>(-10);
            MakePrimitiveTest<Decimal>(10);
            MakePrimitiveTest<Decimal>(Decimal.MinValue);
            MakePrimitiveTest<Decimal>(Decimal.MaxValue);
            MakePrimitiveTest<Decimal>(Decimal.MinValue / 2);
            MakePrimitiveTest<Decimal>(Decimal.MaxValue / 2);
        }

        [Fact]
        public void SerializeFloat()
        {
            MakePrimitiveTest<float>(-0);
            MakePrimitiveTest<float>(0);
            MakePrimitiveTest<float>(-10);
            MakePrimitiveTest<float>(10);
            MakePrimitiveTest<float>(float.MinValue);
            MakePrimitiveTest<float>(float.MaxValue);
            MakePrimitiveTest<float>(float.MinValue / 2);
            MakePrimitiveTest<float>(float.MaxValue / 2);
        }

        [Fact]
        public void SerializeDouble()
        {
            MakePrimitiveTest<Double>(-0);
            MakePrimitiveTest<Double>(0);
            MakePrimitiveTest<Double>(-10);
            MakePrimitiveTest<Double>(10);
            MakePrimitiveTest<Double>(Double.MinValue);
            MakePrimitiveTest<Double>(Double.MaxValue);
            MakePrimitiveTest<Double>(Double.MinValue / 2);
            MakePrimitiveTest<Double>(Double.MaxValue / 2);
        }

        [Fact]
        public void SerializeBoolean()
        {
            MakePrimitiveTest<Boolean>(true);
            MakePrimitiveTest<Boolean>(false);
        }

        [Fact]
        public void SerializeByte()
        {
            MakePrimitiveTest<Byte>(0);
            MakePrimitiveTest<Byte>(-0);
            MakePrimitiveTest<Byte>(1);
            MakePrimitiveTest<Byte>(10);
            MakePrimitiveTest<Byte>(128);
            MakePrimitiveTest<Byte>(255);
        }

        [Fact]
        public void SerializeSByte()
        {
            MakePrimitiveTest<sbyte>(0);
            MakePrimitiveTest<sbyte>(1);
            MakePrimitiveTest<sbyte>(-1);
            MakePrimitiveTest<sbyte>(10);
            MakePrimitiveTest<sbyte>(-10);
            MakePrimitiveTest<sbyte>(sbyte.MinValue);
            MakePrimitiveTest<sbyte>(sbyte.MaxValue);
        }

        [Fact]
        public void SerializeCollectionSByte()
        {
            MakeCollectionTest<sbyte>(new sbyte[] { 0, 1, -1, 10, -10, sbyte.MinValue, sbyte.MaxValue });
        }

        public enum EnumByte : byte { Zero = 0, One = 1, Max = byte.MaxValue }
        public enum EnumSByte : sbyte { Min = sbyte.MinValue, Zero = 0, Max = sbyte.MaxValue }
        public enum EnumShort : short { Min = short.MinValue, Zero = 0, Max = short.MaxValue }
        public enum EnumUShort : ushort { Zero = 0, Max = ushort.MaxValue }
        public enum EnumInt32 { Min = int.MinValue, Zero = 0, A = 100, Max = int.MaxValue }
        public enum EnumUInt32 : uint { Zero = 0, Max = uint.MaxValue }
        public enum EnumInt64 : long { Min = long.MinValue, Zero = 0, Max = long.MaxValue }
        public enum EnumUInt64 : ulong { Zero = 0, Max = ulong.MaxValue }
        [Flags] public enum EnumFlags { None = 0, A = 1, B = 2, C = 4, AB = A | B, ABC = A | B | C }

        [Fact]
        public void SerializeEnums()
        {
            // Each underlying type
            MakePrimitiveTest<EnumByte>(EnumByte.Zero);
            MakePrimitiveTest<EnumByte>(EnumByte.Max);
            MakePrimitiveTest<EnumSByte>(EnumSByte.Min);
            MakePrimitiveTest<EnumSByte>(EnumSByte.Max);
            MakePrimitiveTest<EnumShort>(EnumShort.Min);
            MakePrimitiveTest<EnumShort>(EnumShort.Max);
            MakePrimitiveTest<EnumUShort>(EnumUShort.Max);
            MakePrimitiveTest<EnumInt32>(EnumInt32.Min);
            MakePrimitiveTest<EnumInt32>(EnumInt32.A);
            MakePrimitiveTest<EnumInt32>(EnumInt32.Max);
            MakePrimitiveTest<EnumUInt32>(EnumUInt32.Max);
            MakePrimitiveTest<EnumInt64>(EnumInt64.Min);
            MakePrimitiveTest<EnumInt64>(EnumInt64.Max);
            MakePrimitiveTest<EnumUInt64>(EnumUInt64.Max);
            // Flags
            MakePrimitiveTest<EnumFlags>(EnumFlags.None);
            MakePrimitiveTest<EnumFlags>(EnumFlags.AB);
            MakePrimitiveTest<EnumFlags>(EnumFlags.ABC);
            MakePrimitiveTest<EnumFlags>((EnumFlags)5); // unnamed combination

            // Direct WriteEnum/ReadEnum on the writer/reader
            using (var w = new MemoryStreamWriter())
            {
                w.WriteEnum(EnumInt32.A);
                w.WriteEnum(EnumByte.Max);
                w.WriteEnum(EnumInt64.Min);
                using (var r = new MemoryStreamReader(w.Complete()))
                {
                    Assert.Equal(EnumInt32.A, r.ReadEnum<EnumInt32>());
                    Assert.Equal(EnumByte.Max, r.ReadEnum<EnumByte>());
                    Assert.Equal(EnumInt64.Min, r.ReadEnum<EnumInt64>());
                }
            }
        }

        [Fact]
        public void SerializeValueTuple()
        {
            using (var w = new MemoryStreamWriter())
            {
                w.WriteValueTuple<int, string>((42, "answer"));
                w.WriteValueTuple<Guid, double>((Guid.NewGuid(), Math.PI));
                w.WriteValueTuple<string, long>(("key", long.MaxValue));
                using (var r = new MemoryStreamReader(w.Complete()))
                {
                    var t1 = r.ReadValueTuple<int, string>();
                    Assert.Equal(42, t1.Item1);
                    Assert.Equal("answer", t1.Item2);

                    var t2 = r.ReadValueTuple<Guid, double>();
                    Assert.NotEqual(Guid.Empty, t2.Item1);
                    Assert.Equal(Math.PI, t2.Item2);

                    var t3 = r.ReadValueTuple<string, long>();
                    Assert.Equal("key", t3.Item1);
                    Assert.Equal(long.MaxValue, t3.Item2);
                }
            }
        }

        [Fact]
        public void SerializeKeyValuePair()
        {
            using (var w = new MemoryStreamWriter())
            {
                w.WriteKeyValuePair<int, string>(new KeyValuePair<int, string>(42, "hello"));
                w.WriteKeyValuePair<Guid, long>(new KeyValuePair<Guid, long>(Guid.NewGuid(), long.MaxValue));
                w.WriteKeyValuePair<string, int>(new KeyValuePair<string, int>("key", 100));
                using (var r = new MemoryStreamReader(w.Complete()))
                {
                    var p1 = r.ReadKeyValuePair<int, string>();
                    Assert.Equal(42, p1.Key);
                    Assert.Equal("hello", p1.Value);

                    var p2 = r.ReadKeyValuePair<Guid, long>();
                    Assert.NotEqual(Guid.Empty, p2.Key);
                    Assert.Equal(long.MaxValue, p2.Value);

                    var p3 = r.ReadKeyValuePair<string, int>();
                    Assert.Equal("key", p3.Key);
                    Assert.Equal(100, p3.Value);
                }
            }

            // null key/value rejected
            using (var w = new MemoryStreamWriter())
            {
                Assert.Throws<ArgumentException>(() =>
                    w.WriteKeyValuePair<string, int>(new KeyValuePair<string, int>(null!, 1)));
                Assert.Throws<ArgumentException>(() =>
                    w.WriteKeyValuePair<int, string>(new KeyValuePair<int, string>(1, null!)));
            }
        }

        [Fact]
        public void SerializeHashSet()
        {
            // primitive element types via SerializeCompatible
            var setInt = new HashSet<int> { 1, 2, 3, 7, 11 };
            var bytesInt = MessageSerializer.SerializeCompatible<HashSet<int>>(setInt);
            var restoredInt = MessageSerializer.DeserializeCompatible<HashSet<int>>(bytesInt);
            Assert.Equal(setInt.OrderBy(x => x).ToArray(), restoredInt.OrderBy(x => x).ToArray());

            var setStr = new HashSet<string> { "a", "bb", "ccc", "" };
            var bytesStr = MessageSerializer.SerializeCompatible<HashSet<string>>(setStr);
            var restoredStr = MessageSerializer.DeserializeCompatible<HashSet<string>>(bytesStr);
            // null and empty are indistinguishable per WriteString — empty set member round-trips to null
            Assert.Equal(setStr.Count, restoredStr.Count);

            var setGuid = new HashSet<Guid> { Guid.Empty, Guid.NewGuid(), Guid.NewGuid() };
            var bytesGuid = MessageSerializer.SerializeCompatible<HashSet<Guid>>(setGuid);
            var restoredGuid = MessageSerializer.DeserializeCompatible<HashSet<Guid>>(bytesGuid);
            Assert.True(setGuid.SetEquals(restoredGuid));

            // direct ReadHashSet<T> for IBinarySerializable element type
            using (var w = new MemoryStreamWriter())
            {
                var docs = new HashSet<Document>();
                docs.Add(CompositeInstanceFactory.MakeDocument());
                w.WriteCollection<Document>(docs);
                using (var r = new MemoryStreamReader(w.Complete()))
                {
                    var read = r.ReadHashSet<Document>();
                    Assert.Equal(docs.Count, read.Count);
                }
            }
        }

        [Fact]
        public void SerializeBitArray()
        {
            var comparator = new Func<BitArray, BitArray, bool>((l, r) =>
            {
                if (l == null && r == null) return true;
                if (l == null || r == null) return false;
                if (l.Length != r.Length) return false;
                for (int i = 0; i < l.Length; i++)
                    if (l[i] != r[i]) return false;
                return true;
            });

            MakePrimitiveTest<BitArray>(null, comparator);
            MakePrimitiveTest<BitArray>(new BitArray(0), comparator);
            MakePrimitiveTest<BitArray>(new BitArray(1, true), comparator);
            MakePrimitiveTest<BitArray>(new BitArray(8, false), comparator);
            MakePrimitiveTest<BitArray>(new BitArray(new[] { true, false, true, true, false, false, true }), comparator);

            // length not divisible by 8 — verify trailing bits do not leak
            var b13 = new BitArray(13);
            for (int i = 0; i < 13; i++) b13[i] = (i % 3 == 0);
            MakePrimitiveTest<BitArray>(b13, comparator);

            // larger set
            var big = new BitArray(1024);
            for (int i = 0; i < 1024; i++) big[i] = (i % 7 == 3);
            MakePrimitiveTest<BitArray>(big, comparator);
        }

        [Fact]
        public void SerializeVersion()
        {
            var comparator = new Func<Version, Version, bool>((l, r) =>
                (l == null && r == null) || (l != null && r != null && l.Equals(r)));
            MakePrimitiveTest<Version>(null, comparator);
            MakePrimitiveTest<Version>(new Version(1, 0), comparator);              // build, revision = -1
            MakePrimitiveTest<Version>(new Version(1, 2, 3), comparator);            // revision = -1
            MakePrimitiveTest<Version>(new Version(1, 2, 3, 4), comparator);
            MakePrimitiveTest<Version>(new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue), comparator);
            MakePrimitiveTest<Version>(new Version(0, 0, 0, 0), comparator);
        }

        [Fact]
        public void SerializeCollectionVersion()
        {
            var comparator = new Func<Version, Version, bool>((l, r) =>
                (l == null && r == null) || (l != null && r != null && l.Equals(r)));
            MakeCollectionTest<Version>(new Version[]
            {
                new Version(1, 0),
                new Version(2, 5, 1),
                new Version(3, 4, 5, 6)
            }, comparator);
        }

        [Fact]
        public void SerializeUri()
        {
            var comparator = new Func<Uri, Uri, bool>((left, right) =>
                (left == null && right == null) ||
                (left != null && right != null && string.Equals(left.OriginalString, right.OriginalString, StringComparison.Ordinal)));
            MakePrimitiveTest<Uri>(null, comparator);
            MakePrimitiveTest<Uri>(new Uri("https://example.com/path?query=1#frag"), comparator);
            MakePrimitiveTest<Uri>(new Uri("http://пример.рф/путь"), comparator);
            MakePrimitiveTest<Uri>(new Uri("file:///C:/temp/x.txt"), comparator);
            MakePrimitiveTest<Uri>(new Uri("/relative/path", UriKind.Relative), comparator);
        }

        [Fact]
        public void SerializeCollectionUri()
        {
            var comparator = new Func<Uri, Uri, bool>((left, right) =>
                (left == null && right == null) ||
                (left != null && right != null && string.Equals(left.OriginalString, right.OriginalString, StringComparison.Ordinal)));
            MakeCollectionTest<Uri>(new Uri[]
            {
                new Uri("https://a.com"),
                new Uri("https://b.com/path"),
                new Uri("/rel", UriKind.Relative)
            }, comparator);
        }

        [Fact]
        public void SerializeNullablePrimitives()
        {
            // null round-trips to null
            MakePrimitiveTest<bool?>(null);
            MakePrimitiveTest<byte?>(null);
            MakePrimitiveTest<sbyte?>(null);
            MakePrimitiveTest<char?>(null);
            MakePrimitiveTest<short?>(null);
            MakePrimitiveTest<ushort?>(null);
            MakePrimitiveTest<int?>(null);
            MakePrimitiveTest<uint?>(null);
            MakePrimitiveTest<long?>(null);
            MakePrimitiveTest<ulong?>(null);
            MakePrimitiveTest<float?>(null);
            MakePrimitiveTest<double?>(null);
            MakePrimitiveTest<decimal?>(null);
            MakePrimitiveTest<TimeSpan?>(null);
            MakePrimitiveTest<Guid?>(null);
            MakePrimitiveTest<DateTime?>(null);
            MakePrimitiveTest<DateTimeOffset?>(null);

            // values
            MakePrimitiveTest<bool?>(true);
            MakePrimitiveTest<bool?>(false);
            MakePrimitiveTest<byte?>(0);
            MakePrimitiveTest<byte?>(255);
            MakePrimitiveTest<sbyte?>(sbyte.MinValue);
            MakePrimitiveTest<sbyte?>(sbyte.MaxValue);
            MakePrimitiveTest<char?>('Я');
            MakePrimitiveTest<short?>(short.MinValue);
            MakePrimitiveTest<short?>(short.MaxValue);
            MakePrimitiveTest<ushort?>(ushort.MaxValue);
            MakePrimitiveTest<int?>(int.MinValue);
            MakePrimitiveTest<int?>(int.MaxValue);
            MakePrimitiveTest<uint?>(uint.MaxValue);
            MakePrimitiveTest<long?>(long.MinValue);
            MakePrimitiveTest<long?>(long.MaxValue);
            MakePrimitiveTest<ulong?>(ulong.MaxValue);
            MakePrimitiveTest<float?>(float.MinValue);
            MakePrimitiveTest<float?>(float.MaxValue);
            MakePrimitiveTest<double?>(double.MinValue);
            MakePrimitiveTest<double?>(double.MaxValue);
            MakePrimitiveTest<decimal?>(decimal.MinValue);
            MakePrimitiveTest<decimal?>(decimal.MaxValue);
            MakePrimitiveTest<TimeSpan?>(TimeSpan.Zero);
            MakePrimitiveTest<TimeSpan?>(TimeSpan.MaxValue);
            MakePrimitiveTest<Guid?>(Guid.Empty);
            MakePrimitiveTest<Guid?>(Guid.NewGuid());
            MakePrimitiveTest<DateTime?>(DateTime.Now);
            MakePrimitiveTest<DateTimeOffset?>(DateTimeOffset.Now);
        }

        [Fact]
        public void SerializeBytes()
        {
            var comparator = new Func<byte[], byte[], bool>((left, right) =>
                (left == null && (right == null || right.Length == 0)) || ArrayExtensions.UnsafeEquals(left, right));
            MakePrimitiveTest<Byte[]>(null, comparator);
            MakePrimitiveTest<Byte[]>(new byte[] { }, comparator);
            MakePrimitiveTest<Byte[]>(new byte[] { 1 }, comparator);
            MakePrimitiveTest<Byte[]>(new byte[] { 0, 1, 10, 100, 128, 255 }, comparator);
        }

        /*
         COLLECTIONS
         */

        [Fact]
        public void SerializeCollectionDateTime()
        {
            MakeCollectionTest<DateTime?>(null);
            MakeCollectionTest<DateTime?>(new DateTime?[] { });
            MakeCollectionTest<DateTime?>(new DateTime?[] { DateTime.Now, DateTime.UtcNow, DateTime.Today, DateTime.Now.AddYears(2000), null, DateTime.MinValue, DateTime.MaxValue });


            MakeCollectionTest<DateTimeOffset?>(null);
            MakeCollectionTest<DateTimeOffset?>(new DateTimeOffset?[] { });
            MakeCollectionTest<DateTimeOffset?>(new DateTimeOffset?[] { DateTimeOffset.Now, DateTimeOffset.UtcNow, DateTimeOffset.Now.AddYears(2000), null, DateTimeOffset.MinValue, DateTimeOffset.MaxValue });
        }

        [Fact]
        public void SerializeCollectionIPAddress()
        {
            var comparator = new Func<IPAddress, IPAddress, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            MakeCollectionTest<IPAddress>(null);
            MakeCollectionTest<IPAddress>(new IPAddress[] { IPAddress.Any, IPAddress.Broadcast, IPAddress.IPv6Any, IPAddress.IPv6Loopback, IPAddress.IPv6None, IPAddress.Loopback, IPAddress.None, IPAddress.Parse("93.111.16.58") }, comparator);
        }

        [Fact]
        public void SerializeCollectionIPEndPoint()
        {
            var comparator = new Func<IPEndPoint, IPEndPoint, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            MakeCollectionTest<IPEndPoint>(null);
            MakeCollectionTest<IPEndPoint>(new IPEndPoint[] { });
            MakeCollectionTest<IPEndPoint>(new IPEndPoint[] { new IPEndPoint(IPAddress.Any, 1), new IPEndPoint(IPAddress.Broadcast, 600), new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MaxPort), new IPEndPoint(IPAddress.IPv6Loopback, 8080), new IPEndPoint(IPAddress.IPv6None, 80), new IPEndPoint(IPAddress.Loopback, 9000), new IPEndPoint(IPAddress.None, 0), new IPEndPoint(IPAddress.Parse("93.111.16.58"), IPEndPoint.MinPort) }, comparator);
        }

        [Fact]
        public void SerializeCollectionGuid()
        {
            MakeCollectionTest<Guid>(null);
            MakeCollectionTest<Guid>(new Guid[] { });
            MakeCollectionTest<Guid>(new Guid[] { Guid.Empty, Guid.NewGuid() });
        }

        [Fact]
        public void SerializeCollectionTimeSpan()
        {
            MakeCollectionTest<TimeSpan>(new TimeSpan[] { TimeSpan.MaxValue, TimeSpan.MinValue, TimeSpan.Zero, TimeSpan.FromDays(1024), TimeSpan.FromMilliseconds(1), TimeSpan.FromTicks(1), TimeSpan.FromTicks(0) });
        }

        [Fact]
        public void SerializeCollectionString()
        {
            var comparator = new Func<string, string, bool>((left, right) =>
                    (left == null && right == null) ||
                    (left == null && right != null && right.Length == 0) ||
                    (left != null && left.Length == 0 && right == null) ||
                    string.Compare(left, right, StringComparison.InvariantCulture) == 0);
            MakeCollectionTest<String>(new string[] { "", String.Empty, null, "HELLO!", "𐌼𐌰𐌲 𐌲𐌻𐌴𐍃 𐌹̈𐍄𐌰𐌽, 𐌽𐌹 𐌼𐌹𐍃 𐍅𐌿 𐌽𐌳𐌰𐌽 𐌱𐍂𐌹𐌲𐌲𐌹𐌸" }, comparator);
        }

        [Fact]
        public void SerizlizeCollectionChar()
        {
            // Arrange
            var line = "abcxyzABCZА-Яа-яёЁйЙ123";

            // Act
            var bytes_string = MessageSerializer.SerializeCompatible<string>(line);
            var bytes_charenum = MessageSerializer.SerializeCompatible<IEnumerable<char>>(line);

            // Assert
            var test_line1 = MessageSerializer.DeserializeCompatible<string>(bytes_string);
            var test_line2 = new string(MessageSerializer.DeserializeCompatible<IEnumerable<char>>(bytes_charenum).ToArray());

            Assert.Equal(line, test_line1);
            Assert.Equal(line, test_line2);
            Assert.NotEqual(bytes_string, bytes_charenum);
        }

        [Fact]
        public void EOSTest()
        {
            var data = new Dictionary<long, string>
            {
                { 0,  "asd"},
                { 1,  "sdf"},
                { 2,  "dfg"},
                { 3,  "fgh"},
                { 4,  "ghj"}
            };
            var num_data = long.MaxValue >> 1;
            var date_data = DateTime.UtcNow;
            byte[] serialized;
            using (var writer = new MemoryStreamWriter())
            {
                writer.WriteDateTime(date_data);
                foreach (var key in data.Keys.OrderBy(k => k))
                {
                    writer.WriteLong(key);
                    writer.WriteString(data[key]);
                }
                writer.WriteLong(num_data);
                serialized = writer.Complete();
            }
            using (var reader = new MemoryStreamReader(serialized))
            {
                Assert.False(reader.EOS);
                var date = reader.ReadDateTime();
                Assert.Equal(date, date_data);
                Assert.False(reader.EOS);
                for (long i = 0; i < 5; i++)
                {
                    Assert.Equal(i, reader.ReadLong());
                    Assert.False(reader.EOS);
                    Assert.Equal(data[i], reader.ReadString());
                    Assert.False(reader.EOS);
                }
                var num = reader.ReadLong();
                Assert.Equal(num, num_data);
                Assert.True(reader.EOS);
            }
        }

        [Fact]
        public void SerializeCollectionInt32()
        {
            MakeCollectionTest<Int32>(new int[] { -0, 0, -10, 10, Int32.MinValue, Int32.MaxValue });
        }

        [Fact]
        public void SerializeCollectionInt64()
        {
            MakeCollectionTest<Int64>(new long[] { -0, 0, -10, 10, Int64.MinValue, Int64.MaxValue, Int64.MinValue / 2, Int64.MaxValue / 2 });
        }

        [Fact]
        public void SerializeCollectionDecimal()
        {
            MakeCollectionTest<Decimal>(new Decimal[] { -0, 0, -10, 10, Decimal.MinValue, Decimal.MaxValue, Decimal.MinValue / 2, Decimal.MaxValue / 2 });
        }

        [Fact]
        public void SerializeCollectionFloat()
        {
            MakeCollectionTest<float>(new float[] { -0, 0, -10, 10, float.MinValue, float.MaxValue, float.MinValue / 2, float.MaxValue / 2 });
        }

        [Fact]
        public void SerializeCollectionDouble()
        {
            MakeCollectionTest<Double>(new Double[] { -0, 0, -10, 10, Double.MinValue, Double.MaxValue, Double.MinValue / 2, Double.MaxValue / 2 });
        }

        [Fact]
        public void SerializeCollectionBoolean()
        {
            MakeCollectionTest<Boolean>(new Boolean[] { true, false, true });
        }

        [Fact]
        public void SerializeCollectionByte()
        {
            MakeCollectionTest<Byte>(new byte[] { 0, 3, -0, 1, 10, 128, 255 });
        }

        [Fact]
        public void SerializeCollectionBytes()
        {
            var comparator = new Func<byte[], byte[], bool>((left, right) =>
                (left == null && (right == null || right.Length == 0)) || ArrayExtensions.UnsafeEquals(left, right));

            MakeCollectionTest<Byte[]>(new Byte[][] { null, new byte[] { }, new byte[] { 1 }, new byte[] { 0, 1, 10, 100, 128, 255 } }, comparator);
        }

        [Fact]
        public void SerializeCompositeObject()
        {
            var comparator = new Func<Document, Document, bool>((left, right) =>
            {
                var l_bin = MessageSerializer.Serialize(left);
                var r_bin = MessageSerializer.Serialize(right);
                return ArrayExtensions.UnsafeEquals(l_bin, r_bin);
            });

            MakePrimitiveTest<Document>(CompositeInstanceFactory.MakeDocument(), comparator);
        }

        [Fact]
        public void SerializeCompositeOnjectCollection()
        {
            var comparator = new Func<Document, Document, bool>((left, right) =>
            {
                var l_bin = MessageSerializer.Serialize(left);
                var r_bin = MessageSerializer.Serialize(right);
                return ArrayExtensions.UnsafeEquals(l_bin, r_bin);
            });
            var collection = new Document[] { CompositeInstanceFactory.MakeDocument(), CompositeInstanceFactory.MakeDocument(), CompositeInstanceFactory.MakeDocument() };

            var data = MessageSerializer.Serialize<Document>(collection);
            var restored = MessageSerializer.DeserializeCollection<Document>(data);
            var restored_lazy = MessageSerializer.DeserializeCollectionLazy<Document>(data);

            // Assert
            Assert.True(CollectionComparsionExtensions.OrderingEquals<Document>(collection, restored, comparator));
            Assert.True(CollectionComparsionExtensions.OrderingEquals<Document>(collection, restored_lazy, comparator));
        }

        [Fact]
        public void ReverseByteOrderTest()
        {
            var data = new byte[4] { 0, 0, 8, 1 };
            using (var reader = new MemoryStreamReader(data))
            {
                Assert.Equal(17301504, reader.ReadInt32());
            }
            using (var reader = new MemoryStreamReader(data))
            {
                reader.ReverseByteOrder(true);
                Assert.Equal(2049, reader.ReadInt32());
            }
        }

        [Fact]
        public void SerializeDictionaryTest()
        {
            var dict = new Dictionary<int, string>
            {
                {0, "Dear" },
                {1, "Chaisy" },
                {2, "Lain" }
            };
            MakeDictionaryTest(dict);
        }

        [Fact]
        public void SerializeDictionaryWithComposedObjectTest()
        {
            var dict = new Dictionary<int, Document>
            {
                {0, CompositeInstanceFactory.MakeDocument() },
                {1, CompositeInstanceFactory.MakeDocument() },
                {2, CompositeInstanceFactory.MakeDocument() },
                {3, CompositeInstanceFactory.MakeDocument() }
            };
            var comparator = new Func<Document, Document, bool>((left, right) =>
            {
                var l_bin = MessageSerializer.Serialize(left);
                var r_bin = MessageSerializer.Serialize(right);
                return ArrayExtensions.UnsafeEquals(l_bin, r_bin);
            });
            MakeDictionaryTest(dict, valueComparator: comparator);
        }
    }
}
