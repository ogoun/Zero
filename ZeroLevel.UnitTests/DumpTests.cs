using System;
using System.Linq;
using Xunit;
using ZeroLevel.Services.Microservices.Dump;
using ZeroLevel.UnitTests.Models;

namespace ZeroLevel.UnitTests
{
    public class DumpTests
    {
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
    }
}
