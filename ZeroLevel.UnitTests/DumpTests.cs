using System;
using System.Linq;
using Xunit;
using ZeroLevel.Services.Microservices.Dump;
using ZeroLevel.UnitTests.Models;

namespace ZeroLevel.UnitTests
{
    // In developing, not working!
    public class DumpTests
    {
        /*
        [Fact]
        public void DumpStorageTest()
        {
            // Arrange
            var storage = new DumpStorage<TestSerializableDTO>();
            var arr = new TestSerializableDTO[] {
                new TestSerializableDTO { Id = 0, Title = "#1", Timestamp = DateTime.UtcNow.Ticks },
                new TestSerializableDTO { Id = 1, Title = "#2", Timestamp = DateTime.UtcNow.Ticks },
                new TestSerializableDTO { Id = 2, Title = "#3", Timestamp = DateTime.UtcNow.Ticks }
            };

            // Act
            storage.Dump(arr[0]);
            storage.Dump(arr[1]);
            storage.Dump(arr[2]);

            // Assert
            int index = 0;
            foreach (var entry in storage.ReadAndTruncate())
            {
                Assert.True(arr[index].Equals(entry));
                index++;
            }

            Assert.True(0 == storage.ReadAndTruncate().ToArray().Length);
        }

        [Fact]
        public void DumpStorageLongTest()
        {
            // Arrange
            var storage = new DumpStorage<TestSerializableDTO>();
            long index = 0;

            for (int i = 0; i < 1000; i++)
            {
                // Dump
                for (int j = 0; j < 100; j++)
                {
                    storage.Dump(new TestSerializableDTO { Id = i * 1000 + j, Timestamp = DateTime.UtcNow.Ticks, Title = $"#{i * j}" });
                }
                // Clean
                foreach (var entry in storage.ReadAndTruncate())
                {
                    Assert.True(entry.Id == index);
                    index++;
                }
            }
            Assert.True(0 == storage.ReadAndTruncate().ToArray().Length);
        }
        */
    }
}
