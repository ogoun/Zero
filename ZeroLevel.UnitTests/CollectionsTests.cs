using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.UnitTests
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
    }
}
