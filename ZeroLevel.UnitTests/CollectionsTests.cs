using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using ZeroLevel.Services.Collections;
using ZeroLevel.Services.ObjectMapping;
using ZeroLevel.Services.Reflection;

namespace ZeroLevel.CollectionUnitTests
{
    [TestClass]
    public class CollectionsTests
    {
        [TestMethod]
        public void RoundRobinCollectionTest()
        {
            // Arrange
            var collection = new RoundRobinCollection<int>();
            var iter1 = new int[] { 1, 2, 3 };
            var iter2 = new int[] { 2, 3, 1 };
            var iter3 = new int[] { 3, 1, 2 };

            var iter11 = new int[] { 1, 3 };
            var iter12 = new int[] { 3, 1 };
            // Act
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);
            // Assert
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter1));
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter1));
            Assert.IsTrue(collection.MoveNext());
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter2));
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter2));
            Assert.IsTrue(collection.MoveNext());
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter3));
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter3));

            collection.Remove(2);
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter11));
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter11));
            Assert.IsTrue(collection.MoveNext());
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter12));
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter12));
        }

        [TestMethod]
        public void RoundRobinOverCollectionTest()
        {
            var arr = new int[] { 1, 2, 3 };
            // Arrange
            var collection = new RoundRobinOverCollection<int>(arr);
            var iter1 = new int[] { 1, 2, 3 };
            var iter2 = new int[] { 2, 3, 1 };
            var iter3 = new int[] { 3, 1, 2 };
            // Act
            // Assert
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GenerateSeq().ToArray(), iter1));
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GenerateSeq().ToArray(), iter2));
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GenerateSeq().ToArray(), iter3));
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GenerateSeq().ToArray(), iter1));
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GenerateSeq().ToArray(), iter2));
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(collection.GenerateSeq().ToArray(), iter3));
        }

        [TestMethod]
        public void EverythingStorageTest()
        {
            // Arrange
            var storage = EverythingStorage.Create();
            var date = DateTime.Now;
            var typeBuilder = new DTOTypeBuilder("MyType");
            typeBuilder.AppendField<string>("Title");
            typeBuilder.AppendProperty<long>("Id");
            typeBuilder.AppendProperty<DateTime>("Created");
            var type = typeBuilder.Complete();
            var mapper = TypeMapper.Create(type);
            var instance = TypeHelpers.CreateNonInitializedInstance(type);
            mapper.Set(instance, "Title", "This is title");
            mapper.Set(instance, "Id", 1001001);
            mapper.Set(instance, "Created", date);

            // Act
            storage.Add<string>("id", "stringidentity");
            storage.Add<long>("id", 123);
            storage.Add<int>("id", 234);
            storage.Add(type, "rt", instance);

            // Assert
            Assert.IsTrue(storage.ContainsKey<int>("id"));
            Assert.IsTrue(storage.ContainsKey<long>("id"));
            Assert.IsTrue(storage.ContainsKey<string>("id"));
            Assert.IsTrue(storage.ContainsKey(type, "rt"));
            Assert.IsFalse(storage.ContainsKey<int>("somekey"));
            Assert.IsFalse(storage.ContainsKey<Guid>("somekey"));

            Assert.AreEqual(mapper.Get(storage.Get(type, "rt"), "Id"), (long)1001001);
            Assert.AreEqual(mapper.Get(storage.Get(type, "rt"), "Created"), date);
            Assert.AreEqual(mapper.Get(storage.Get(type, "rt"), "Title"), "This is title");
            Assert.AreEqual(storage.Get<long>("id"), (long)123);
            Assert.AreEqual(storage.Get<int>("id"), 234);
            Assert.AreEqual(storage.Get<string>("id"), "stringidentity");
        }

        [TestMethod]
        public void FixSizeQueueTest()
        {
            // Arrange
            var fix = new FixSizeQueue<long>(3);

            // Act
            fix.Push(1);
            fix.Push(2);
            fix.Push(3);
            fix.Push(4);
            fix.Push(5);

            // Assert
            Assert.IsTrue(fix.Count == 3);
            Assert.IsTrue(fix.Contains(3));
            Assert.IsTrue(fix.Contains(4));
            Assert.IsTrue(fix.Contains(5));
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(fix.Dump().ToArray(), new long[] { 3, 4, 5 }));
            Assert.IsTrue(fix.Take() == 3);
            Assert.IsTrue(fix.Count == 2);
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(fix.Dump().ToArray(), new long[] { 4, 5 }));
        }
    }
}
