using System.Linq;
using Xunit;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.CollectionUnitTests
{
    public class CollectionsTests
    {
        [Fact]
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
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter1));
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter1));
            Assert.True(collection.MoveNext());
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter2));
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter2));
            Assert.True(collection.MoveNext());
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter3));
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter3));

            collection.Remove(2);
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter11));
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter11));
            Assert.True(collection.MoveNext());
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter12));
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter12));
        }

        [Fact]
        public void RoundRobinOverCollectionTest()
        {
            var arr = new int[] { 1, 2, 3 };
            // Arrange
            var collection = new RoundRobinCollection<int>(arr);
            var iter1 = new int[] { 1, 2, 3 };
            var iter2 = new int[] { 2, 3, 1 };
            var iter3 = new int[] { 3, 1, 2 };
            // Act
            // Assert
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter1));
            collection.MoveNext();
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter2));
            collection.MoveNext();
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter3));
            collection.MoveNext();
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter1));
            collection.MoveNext();
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter2));
            collection.MoveNext();
            Assert.True(CollectionComparsionExtensions.OrderingEquals(collection.GetCurrentSeq().ToArray(), iter3));
        }

        [Fact]
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
            Assert.True(fix.Count == 3);
            Assert.True(fix.Contains(3));
            Assert.True(fix.Contains(4));
            Assert.True(fix.Contains(5));
            Assert.True(CollectionComparsionExtensions.OrderingEquals(fix.Dump().ToArray(), new long[] { 3, 4, 5 }));
            Assert.True(fix.Take() == 3);
            Assert.True(fix.Count == 2);
            Assert.True(CollectionComparsionExtensions.OrderingEquals(fix.Dump().ToArray(), new long[] { 4, 5 }));
        }
    }
}
