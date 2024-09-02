using System;
using Xunit;

namespace ZeroLevel.UnitTests
{
    public class BytesEncodingTests
    {
        public static void DeHashData(byte[] data, byte initialMask)
        {
            int i;
            for (i = data.Length - 1; i > 9; i -= 8)
            {
                data[i - 0] ^= data[i - 1];
                data[i - 1] ^= data[i - 2];
                data[i - 2] ^= data[i - 3];
                data[i - 3] ^= data[i - 4];
                data[i - 4] ^= data[i - 5];
                data[i - 5] ^= data[i - 6];
                data[i - 6] ^= data[i - 7];
                data[i - 7] ^= data[i - 8];
            }
            for (; i >= 1; i--)
            {
                data[i] ^= data[i - 1];
            }
            data[0] ^= initialMask;
        }

        public static void HashData(byte[] data, byte initialmask)
        {
            if (data == null || data.Length == 0) return;
            int i = 1;
            data[0] ^= initialmask;
            for (; i < (data.Length - 8); i += 8)
            {
                data[i + 0] ^= data[i - 1];
                data[i + 1] ^= data[i + 0];
                data[i + 2] ^= data[i + 1];
                data[i + 3] ^= data[i + 2];
                data[i + 4] ^= data[i + 3];
                data[i + 5] ^= data[i + 4];
                data[i + 6] ^= data[i + 5];
                data[i + 7] ^= data[i + 6];
            }
            for (; i < data.Length; i++)
            {
                data[i] ^= data[i - 1];
            }
        }

        [Fact]
        public void SpecialTest()
        {
            var arr = new byte[] { 244, 135 }; //, 125, 160, 109, 144, 187, 109, 88, 78, 175, 115, 83, 174, 165, 246, 253, 112 };
            var copy = new byte[arr.Length];
            Array.Copy(arr, copy, arr.Length);

            byte initial = (byte)(arr.Length % 255);
            HashData(arr, initial);
            Assert.False(ArrayExtensions.UnsafeEquals(arr, copy));

            DeHashData(arr, initial);
            Assert.True(ArrayExtensions.UnsafeEquals(arr, copy));
        }

        [Fact]
        public void BytesEncodingTest()
        {
            // Arrange
            var r = new Random((int)Environment.TickCount);

            for (int i = 1; i < 20; i ++)
            {
                // Act
                var arr = new byte[i];
                var copy = new byte[i];
                r.NextBytes(arr);
                Array.Copy(arr, copy, arr.Length);

                // Assert

                var initial = arr[0];
                HashData(arr, initial);
                Assert.False(ArrayExtensions.UnsafeEquals(arr, copy));

                DeHashData(arr, initial);
                Assert.True(ArrayExtensions.UnsafeEquals(arr, copy));
            }

            for (int i = 1; i < 200000; i += 17)
            {
                // Act
                var arr = new byte[i];
                var copy = new byte[i];
                r.NextBytes(arr);
                Array.Copy(arr, copy, arr.Length);

                // Assert

                var initial = arr[0];
                HashData(arr, initial);
                Assert.False(ArrayExtensions.UnsafeEquals(arr, copy));

                DeHashData(arr, initial);
                Assert.True(ArrayExtensions.UnsafeEquals(arr, copy));
            }
        }

    }
}
