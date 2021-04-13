using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Xunit;
using ZeroLevel.DocumentObjectModel;
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
            MakeCollectionTest<DateTime>(null);
            MakeCollectionTest<DateTime>(new DateTime[] { });
            MakeCollectionTest<DateTime>(new DateTime[] { DateTime.Now, DateTime.UtcNow, DateTime.Today, DateTime.Now.AddYears(2000), DateTime.MinValue, DateTime.MaxValue });
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
