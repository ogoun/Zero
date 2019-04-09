using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ZeroLevel;

namespace ZeroArrayExtensionsTest
{
    [TestClass]
    public class ArrayExtensionsTest
    {
        internal class EQDTO
        {
            public byte[] Arr;
            public DateTime Date;
            public string Title;
            public long Num;

            public override bool Equals(object obj)
            {
                return this.Equals(obj as EQDTO);
            }

            public bool Equals(EQDTO other)
            {
                if (other == null) return false;

                if (ArrayExtensions.UnsafeEquals(this.Arr, other.Arr) == false) return false;
                if (DateTime.Compare(this.Date, other.Date) != 0) return false;
                if (string.Compare(this.Title, other.Title, StringComparison.OrdinalIgnoreCase) != 0) return false;
                return this.Num == other.Num;
            }
        }

        [TestMethod]
        public void ByteArrayEqualTest()
        {
            // Arrange
            var a1 = new byte[] { 3, 1, 4, 1, 5, 9, 2, 6, 5, 3, 5, 8, 9, 7, 9, 7 };
            var a2 = new byte[] { 3, 1, 4, 1, 5, 9, 2, 6, 5, 3, 5, 8, 9, 7, 9, 7 };
            var a3 = new byte[] { 3, 1, 4, 1, 5, 9, 2, 6, 5, 3, 5, 8, 9, 7, 9 };
            var a4 = new byte[] { 1, 4, 1, 5, 9, 2, 6, 5, 3, 5, 8, 9, 7, 9, 7 };
            byte[] a5 = null;
            byte[] a6 = null;
            var a7 = new byte[0];
            var a8 = new byte[0];

            // Assert
            Assert.IsTrue(ArrayExtensions.UnsafeEquals(a1, a2));
            Assert.IsTrue(ArrayExtensions.UnsafeEquals(a5, a6));
            Assert.IsTrue(ArrayExtensions.UnsafeEquals(a7, a8));

            Assert.IsFalse(ArrayExtensions.UnsafeEquals(a1, a3));
            Assert.IsFalse(ArrayExtensions.UnsafeEquals(a1, a4));

            Assert.IsFalse(ArrayExtensions.UnsafeEquals(a1, a5));
            Assert.IsFalse(ArrayExtensions.UnsafeEquals(a1, a7));

            Assert.IsFalse(ArrayExtensions.UnsafeEquals(a5, a7));
        }

        [TestMethod]
        public void ArrayEqualTest()
        {
            // Arrange
            var date = DateTime.Now;
            var arr1 = new EQDTO[]
            {
                new EQDTO { Arr = new byte[] { 1,2,3}, Date = date, Num = 10, Title = "t1" },
                new EQDTO { Arr = new byte[] { 1,2,4}, Date = date, Num = 20, Title = "t2" },
                new EQDTO { Arr = new byte[] { 1,2,5}, Date = date, Num = 30, Title = "t3" },
                new EQDTO { Arr = new byte[] { 1,2,6}, Date = date, Num = 40, Title = "t4" },
                new EQDTO { Arr = new byte[] { 1,2,7}, Date = date, Num = 50, Title = "t5" }
            };
            var arr2 = new EQDTO[]
            {
                new EQDTO { Arr = new byte[] { 1,2,6}, Date = date, Num = 40, Title = "t4" },
                new EQDTO { Arr = new byte[] { 1,2,7}, Date = date, Num = 50, Title = "t5" }
            };
            var arr3 = new EQDTO[]
            {
                new EQDTO { Arr = new byte[] { 1,2,6}, Date = date, Num = 40, Title = "t4" },
                new EQDTO { Arr = new byte[] { 1,2,7}, Date = date, Num = 50, Title = "t5" },
                new EQDTO { Arr = new byte[] { 1,2,7}, Date = date, Num = 50, Title = "t6" },
            };

            //Assert
            Assert.IsTrue(ArrayExtensions.Contains(arr1, arr2));
            Assert.IsFalse(ArrayExtensions.Contains(arr1, arr3));
        }
    }
}
