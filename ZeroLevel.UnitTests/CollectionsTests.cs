using System;
using System.IO;
using System.Linq;
using System.Net;
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

        [Fact]
        public void ChunkifyTest()
        {
            // Arrange
            var arr = new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var empty_arr = new long[0];

            // Act
            var empty_chunks = empty_arr.Chunkify(3).ToArray();
            var chunks_2 = arr.Chunkify(2).ToArray();
            var chunks_3 = arr.Chunkify(3).ToArray();

            // Assert
            Assert.True(empty_chunks.Length == 0);
            Assert.True(chunks_2.Length == 5);
            Assert.True(chunks_3.Length == 3);

            Assert.True(CollectionComparsionExtensions.OrderingEquals(chunks_3[0], new long[] { 1, 2, 3 }));
            Assert.True(CollectionComparsionExtensions.OrderingEquals(chunks_3[1], new long[] { 4, 5, 6 }));
            Assert.True(CollectionComparsionExtensions.OrderingEquals(chunks_3[2], new long[] { 7, 8, 9 }));

            Assert.True(CollectionComparsionExtensions.OrderingEquals(chunks_2[0], new long[] { 1, 2 }));
            Assert.True(CollectionComparsionExtensions.OrderingEquals(chunks_2[1], new long[] { 3, 4 }));
            Assert.True(CollectionComparsionExtensions.OrderingEquals(chunks_2[2], new long[] { 5, 6 }));
            Assert.True(CollectionComparsionExtensions.OrderingEquals(chunks_2[3], new long[] { 7, 8 }));
            Assert.True(CollectionComparsionExtensions.OrderingEquals(chunks_2[4], new long[] { 9 }));
        }

        [Fact]
        public void EverythingStorageTest()
        {
            // Arrange
            var storage = EverythingStorage.Create();

            // Act
            storage.Add<int>("int_a", 101);
            storage.Add<int>("int_b", 255);
            storage.Add<int>("int_c", 1024);

            storage.Add<TimeSpan>("ts_a", TimeSpan.FromSeconds(6546));
            storage.Add<TimeSpan>("ts_b", TimeSpan.FromSeconds(777));

            storage.Add<ulong>("l_max", ulong.MaxValue);

            storage.Add<IPAddress>("ip", IPAddress.Loopback);

            // Assert
            Assert.Equal<int>(101, storage.Get<int>("int_a"));
            Assert.Equal<int>(255, storage.Get<int>("int_b"));
            Assert.Equal<int>(1024, storage.Get<int>("int_c"));

            Assert.Equal<TimeSpan>(TimeSpan.FromSeconds(6546), storage.Get<TimeSpan>("ts_a"));
            Assert.Equal<TimeSpan>(TimeSpan.FromSeconds(777), storage.Get<TimeSpan>("ts_b"));

            Assert.Equal<ulong>(ulong.MaxValue, storage.Get<ulong>("l_max"));

            Assert.Equal<IPAddress>(IPAddress.Loopback, storage.Get<IPAddress>("ip"));
        }

        [Fact]
        public void EverythingStorageDumpTest()
        {
            // Arrange
            var storage = EverythingStorage.Create();

            // Act
            storage.Add<int>("int_a", 101);
            storage.Add<int>("int_b", 255);
            storage.Add<int>("int_c", 1024);

            storage.Add<TimeSpan>("ts_a", TimeSpan.FromSeconds(6546));
            storage.Add<TimeSpan>("ts_b", TimeSpan.FromSeconds(777));

            storage.Add<ulong>("l_max", ulong.MaxValue);

            var restored = EverythingStorage.Create();

            var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            storage.Save(file);
            restored.Load(file);

            File.Delete(file);

            // Assert
            Assert.Equal<int>(101, restored.Get<int>("int_a"));
            Assert.Equal<int>(255, restored.Get<int>("int_b"));
            Assert.Equal<int>(1024, restored.Get<int>("int_c"));

            Assert.Equal<TimeSpan>(TimeSpan.FromSeconds(6546), restored.Get<TimeSpan>("ts_a"));
            Assert.Equal<TimeSpan>(TimeSpan.FromSeconds(777), restored.Get<TimeSpan>("ts_b"));

            Assert.Equal<ulong>(ulong.MaxValue, restored.Get<ulong>("l_max"));
        }
    }
}
