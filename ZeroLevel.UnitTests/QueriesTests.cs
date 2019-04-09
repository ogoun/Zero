using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.Patterns.Queries;
using ZeroSpecificationPatternsTest.Models;

namespace ZeroSpecificationPatternsTest
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class QueriesTests
    {
        private static bool TestDTOEqual(TestDTO left, TestDTO right)
        {
            if (left == null && right == null) return true;
            if (left == null || right == null) return false;
            if (left.IsFlag != right.IsFlag) return false;
            if (left.LongNumber != right.LongNumber) return false;
            if (left.Number != right.Number) return false;
            if (left.Real != right.Real) return false;
            if (string.Compare(left.Summary, right.Summary, StringComparison.Ordinal) != 0) return false;
            if (string.Compare(left.Title, right.Title, StringComparison.Ordinal) != 0) return false;
            return true;
        }

        [TestMethod]
        public void MemoryStoragePostTest()
        {
            var storage = new MemoryStorage<TestDTO>();
            var a1 = storage.Post(new TestDTO
            {
                IsFlag = false,
                LongNumber = 100,
                Number = 1000,
                Real = 1000.0001,
                Summary = "Summary #1",
                Title = "Title #1"
            });
            var a2 = storage.Post(new TestDTO
            {
                IsFlag = true,
                LongNumber = 0,
                Number = -500,
                Real = 500.005,
                Summary = "Summary #2",
                Title = "Title #2"
            });
            var a3 = storage.Post(new TestDTO
            {
                IsFlag = false,
                LongNumber = -1,
                Number = 500,
                Real = -0.0001,
                Summary = "Summary #3",
                Title = "Title #3"
            });
            Assert.IsTrue(a1.Success);
            Assert.AreEqual<long>(a1.Count, 1);
            Assert.IsTrue(a2.Success);
            Assert.AreEqual<long>(a2.Count, 1);
            Assert.IsTrue(a3.Success);
            Assert.AreEqual<long>(a3.Count, 1);
        }

        [TestMethod]
        public void MemoryStorageGetAllTest()
        {
            var set = new List<TestDTO>();
            set.Add(new TestDTO
            {
                IsFlag = false,
                LongNumber = 100,
                Number = 1000,
                Real = 1000.0001,
                Summary = "Summary #1",
                Title = "Title #1"
            });
            set.Add(new TestDTO
            {
                IsFlag = true,
                LongNumber = 0,
                Number = -500,
                Real = 500.005,
                Summary = "Summary #2",
                Title = "Title #2"
            });
            set.Add(new TestDTO
            {
                IsFlag = false,
                LongNumber = -1,
                Number = 500,
                Real = -0.0001,
                Summary = "Summary #3",
                Title = "Title #3"
            });
            var storage = new MemoryStorage<TestDTO>();
            foreach (var i in set)
            {
                var ar = storage.Post(i);
                Assert.IsTrue(ar.Success);
                Assert.AreEqual<long>(ar.Count, 1);
            }
            // Test equals set and storage data
            foreach (var i in storage.Get())
            {
                Assert.IsTrue(set.Exists(dto => TestDTOEqual(i, dto)));
            }
            // Modify originals
            foreach (var i in set)
            {
                i.Title = "###";
            }
            // Test independences storage from original
            foreach (var i in storage.Get())
            {
                Assert.IsFalse(set.Exists(dto => TestDTOEqual(i, dto)));
            }
        }


        [TestMethod]
        public void MemoryStorageGetByTest()
        {
            var set = new List<TestDTO>();
            set.Add(new TestDTO
            {
                IsFlag = false,
                LongNumber = 100,
                Number = 1000,
                Real = 1000.0001,
                Summary = "Summary #1",
                Title = "Title #1"
            });
            set.Add(new TestDTO
            {
                IsFlag = true,
                LongNumber = 0,
                Number = -500,
                Real = 500.005,
                Summary = "Summary #2",
                Title = "Title #2"
            });
            set.Add(new TestDTO
            {
                IsFlag = false,
                LongNumber = -1,
                Number = 500,
                Real = -0.0001,
                Summary = "Summary #3",
                Title = "Title #3"
            });
            var storage = new MemoryStorage<TestDTO>();
            foreach (var i in set)
            {
                var ar = storage.Post(i);
                Assert.IsTrue(ar.Success);
                Assert.AreEqual<long>(ar.Count, 1);
            }
            // Test equals set and storage data
            foreach (var i in storage.Get())
            {
                Assert.IsTrue(set.Exists(dto => TestDTOEqual(i, dto)));
            }
            foreach (var i in storage.Get(Query.ALL()))
            {
                Assert.IsTrue(set.Exists(dto => TestDTOEqual(i, dto)));
            }
            var result_eq = storage.Get(Query.EQ("Title", "Title #1"));
            Assert.AreEqual<int>(result_eq.Count(), 1);
            Assert.IsTrue(TestDTOEqual(set[0], result_eq.First()));

            var result_neq = storage.Get(Query.NEQ("Title", "Title #1"));
            Assert.AreEqual<int>(result_neq.Count(), 2);
            Assert.IsTrue(TestDTOEqual(set[1], result_neq.First()));
            Assert.IsTrue(TestDTOEqual(set[2], result_neq.Skip(1).First()));

            var result_gt = storage.Get(Query.GT("Number", 1));
            Assert.AreEqual<int>(result_gt.Count(), 2);
            Assert.IsTrue(TestDTOEqual(set[0], result_gt.First()));
            Assert.IsTrue(TestDTOEqual(set[2], result_gt.Skip(1).First()));

            var result_lt = storage.Get(Query.LT("Number", 1));
            Assert.AreEqual<int>(result_lt.Count(), 1);
            Assert.IsTrue(TestDTOEqual(set[1], result_lt.First()));
        }
    }
}
