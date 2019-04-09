using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroLevel.Network;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.UnitTests
{
    [TestClass]
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
                Assert.AreEqual<T>(value, clone);
            }
            else
            {
                Assert.IsTrue(comparator(value, clone));
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
                Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(value, clone));
            }
            else
            {
                Assert.IsTrue(TestOrderingEquals(value, clone, comparator));
            }
        }

        [TestMethod]
        public void SerializeDateTime()
        {
            MakePrimitiveTest<DateTime>(DateTime.Now);
            MakePrimitiveTest<DateTime>(DateTime.UtcNow);
            MakePrimitiveTest<DateTime>(DateTime.Today);
            MakePrimitiveTest<DateTime>(DateTime.Now.AddYears(2000));
            MakePrimitiveTest<DateTime>(DateTime.MinValue);
            MakePrimitiveTest<DateTime>(DateTime.MaxValue);
        }

        [TestMethod]
        public void SerializeIPAddress()
        {
            var comparator = new Func<IPAddress, IPAddress, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            MakePrimitiveTest<IPAddress>(IPAddress.Any, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.Broadcast, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.IPv6Any, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.IPv6Loopback, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.IPv6None, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.Loopback, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.None, comparator);
            MakePrimitiveTest<IPAddress>(IPAddress.Parse("93.111.16.58"), comparator);
        }

        [TestMethod]
        public void SerializeIPEndPoint()
        {
            var comparator = new Func<IPEndPoint, IPEndPoint, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.Any, 1), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.Broadcast, 600), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MaxPort), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.IPv6Loopback, 8080), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.IPv6None, 80), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.Loopback, 9000), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.None, 0), comparator);
            MakePrimitiveTest<IPEndPoint>(new IPEndPoint(IPAddress.Parse("93.111.16.58"), IPEndPoint.MinPort), comparator);
        }

        [TestMethod]
        public void SerializeGuid()
        {
            MakePrimitiveTest<Guid>(Guid.Empty);
            MakePrimitiveTest<Guid>(Guid.NewGuid());
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public void SerializeInt32()
        {
            MakePrimitiveTest<Int32>(-0);
            MakePrimitiveTest<Int32>(0);
            MakePrimitiveTest<Int32>(-10);
            MakePrimitiveTest<Int32>(10);
            MakePrimitiveTest<Int32>(Int32.MinValue);
            MakePrimitiveTest<Int32>(Int32.MaxValue);
        }

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
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

        [TestMethod]
        public void SerializeBoolean()
        {
            MakePrimitiveTest<Boolean>(true);
            MakePrimitiveTest<Boolean>(false);
        }

        [TestMethod]
        public void SerializeByte()
        {
            MakePrimitiveTest<Byte>(0);
            MakePrimitiveTest<Byte>(-0);
            MakePrimitiveTest<Byte>(1);
            MakePrimitiveTest<Byte>(10);
            MakePrimitiveTest<Byte>(128);
            MakePrimitiveTest<Byte>(255);
        }

        [TestMethod]
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

        [TestMethod]
        public void SerializeCollectionDateTime()
        {
            MakeCollectionTest<DateTime>(null);
            MakeCollectionTest<DateTime>(new DateTime[] { });
            MakeCollectionTest<DateTime>(new DateTime[] { DateTime.Now, DateTime.UtcNow, DateTime.Today, DateTime.Now.AddYears(2000), DateTime.MinValue, DateTime.MaxValue });
        }

        [TestMethod]
        public void SerializeCollectionIPAddress()
        {
            var comparator = new Func<IPAddress, IPAddress, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            MakeCollectionTest<IPAddress>(null);
            MakeCollectionTest<IPAddress>(new IPAddress[] { IPAddress.Any, IPAddress.Broadcast, IPAddress.IPv6Any, IPAddress.IPv6Loopback, IPAddress.IPv6None, IPAddress.Loopback, IPAddress.None, IPAddress.Parse("93.111.16.58") }, comparator);
        }

        [TestMethod]
        public void SerializeCollectionIPEndPoint()
        {
            var comparator = new Func<IPEndPoint, IPEndPoint, bool>((left, right) => NetUtils.Compare(left, right) == 0);
            MakeCollectionTest<IPEndPoint>(null);
            MakeCollectionTest<IPEndPoint>(new IPEndPoint[] { });
            MakeCollectionTest<IPEndPoint>(new IPEndPoint[] { new IPEndPoint(IPAddress.Any, 1), new IPEndPoint(IPAddress.Broadcast, 600), new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MaxPort), new IPEndPoint(IPAddress.IPv6Loopback, 8080), new IPEndPoint(IPAddress.IPv6None, 80), new IPEndPoint(IPAddress.Loopback, 9000), new IPEndPoint(IPAddress.None, 0), new IPEndPoint(IPAddress.Parse("93.111.16.58"), IPEndPoint.MinPort) }, comparator);
        }

        [TestMethod]
        public void SerializeCollectionGuid()
        {
            MakeCollectionTest<Guid>(null);
            MakeCollectionTest<Guid>(new Guid[] { });
            MakeCollectionTest<Guid>(new Guid[] { Guid.Empty, Guid.NewGuid() });
        }

        [TestMethod]
        public void SerializeCollectionTimeSpan()
        {
            MakeCollectionTest<TimeSpan>(new TimeSpan[] { TimeSpan.MaxValue, TimeSpan.MinValue, TimeSpan.Zero, TimeSpan.FromDays(1024), TimeSpan.FromMilliseconds(1), TimeSpan.FromTicks(1), TimeSpan.FromTicks(0) });
        }

        [TestMethod]
        public void SerializeCollectionString()
        {
            var comparator = new Func<string, string, bool>((left, right) =>
                    (left == null && right == null) ||
                    (left == null && right != null && right.Length == 0) ||
                    (left != null && left.Length == 0 && right == null) ||
                    string.Compare(left, right, StringComparison.InvariantCulture) == 0);
            MakeCollectionTest<String>(new string[] { "", String.Empty, null, "HELLO!", "𐌼𐌰𐌲 𐌲𐌻𐌴𐍃 𐌹̈𐍄𐌰𐌽, 𐌽𐌹 𐌼𐌹𐍃 𐍅𐌿 𐌽𐌳𐌰𐌽 𐌱𐍂𐌹𐌲𐌲𐌹𐌸" }, comparator);
        }


        [TestMethod]
        public void SerializeCollectionInt32()
        {
            MakeCollectionTest<Int32>(new int[] { -0, 0, -10, 10, Int32.MinValue, Int32.MaxValue });
        }

        [TestMethod]
        public void SerializeCollectionInt64()
        {
            MakeCollectionTest<Int64>(new long[] { -0, 0, -10, 10, Int64.MinValue, Int64.MaxValue, Int64.MinValue / 2, Int64.MaxValue / 2 });
        }

        [TestMethod]
        public void SerializeCollectionDecimal()
        {
            MakeCollectionTest<Decimal>(new Decimal[] { -0, 0, -10, 10, Decimal.MinValue, Decimal.MaxValue, Decimal.MinValue / 2, Decimal.MaxValue / 2 });
        }

        [TestMethod]
        public void SerializeCollectionFloat()
        {
            MakeCollectionTest<float>(new float[] { -0, 0, -10, 10, float.MinValue, float.MaxValue, float.MinValue / 2, float.MaxValue / 2 });
        }

        [TestMethod]
        public void SerializeCollectionDouble()
        {
            MakeCollectionTest<Double>(new Double[] { -0, 0, -10, 10, Double.MinValue, Double.MaxValue, Double.MinValue / 2, Double.MaxValue / 2 });
        }

        [TestMethod]
        public void SerializeCollectionBoolean()
        {
            MakeCollectionTest<Boolean>(new Boolean[] { true, false, true });
        }

        [TestMethod]
        public void SerializeCollectionByte()
        {
            MakeCollectionTest<Byte>(new byte[] { 0, 3, -0, 1, 10, 128, 255 });
        }

        [TestMethod]
        public void SerializeCollectionBytes()
        {
            var comparator = new Func<byte[], byte[], bool>((left, right) =>
                (left == null && (right == null || right.Length == 0)) || ArrayExtensions.UnsafeEquals(left, right));

            MakeCollectionTest<Byte[]>(new Byte[][] { null, new byte[] { }, new byte[] { 1 }, new byte[] { 0, 1, 10, 100, 128, 255 } }, comparator);
        }
    }
}
