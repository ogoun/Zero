using Xunit;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.UnitTests
{
    public class PriorityQueueTests
    {
        class TestItem
        {
            public string Value;
        }

        [Fact]
        public void AllItemsCorrectHandleTest()
        {
            // Arrange
            var q = new ZPriorityQueue<string>(s => new PriorityQueueObjectHandleResult { IsCompleted = true, CanBeSkipped = false });
            q.Append("s0", 0);
            q.Append("s1", 10);
            q.Append("s2", 4);
            q.Append("s3", 1);
            q.Append("s4", 6);
            q.Append("s5", 2);
            q.Append("s6", 7);

            // Assert
            Assert.Equal("s0", q.HandleCurrentItem());
            Assert.Equal("s3", q.HandleCurrentItem());
            Assert.Equal("s5", q.HandleCurrentItem());
            Assert.Equal("s2", q.HandleCurrentItem());
            Assert.Equal("s4", q.HandleCurrentItem());
            Assert.Equal("s6", q.HandleCurrentItem());
            Assert.Equal("s1", q.HandleCurrentItem());
        }

        [Fact]
        public void SkipItemsHandleTest()
        {
            bool skipped = false;
            // Arrange
            var q = new ZPriorityQueue<string>(s =>
            {
                if (s.Equals("s5") && !skipped)
                { 
                    skipped = true;
                    return new PriorityQueueObjectHandleResult { IsCompleted = false, CanBeSkipped = true };
                }
                return new PriorityQueueObjectHandleResult { IsCompleted = true, CanBeSkipped = false };
            });
            q.Append("s0", 0);
            q.Append("s1", 10);
            q.Append("s2", 4);
            q.Append("s3", 1);
            q.Append("s4", 6);
            q.Append("s5", 2);
            q.Append("s6", 7);

            // Assert
            Assert.Equal("s0", q.HandleCurrentItem());
            Assert.Equal("s3", q.HandleCurrentItem());            
            
            Assert.Equal("s2", q.HandleCurrentItem());
            Assert.Equal("s5", q.HandleCurrentItem());

            Assert.Equal("s4", q.HandleCurrentItem());
            Assert.Equal("s6", q.HandleCurrentItem());
            Assert.Equal("s1", q.HandleCurrentItem());
        }

        [Fact]
        public void NoSkipItemsIncorrectHandleTest()
        {
            bool skipped = false;
            // Arrange
            var q = new ZPriorityQueue<string>(s =>
            {
                if (s.Equals("s5") && !skipped)
                {
                    skipped = true;
                    return new PriorityQueueObjectHandleResult { IsCompleted = false, CanBeSkipped = false };
                }
                return new PriorityQueueObjectHandleResult { IsCompleted = true, CanBeSkipped = false };
            });
            q.Append("s0", 0);
            q.Append("s1", 10);
            q.Append("s2", 4);
            q.Append("s3", 1);
            q.Append("s4", 6);
            q.Append("s5", 2);
            q.Append("s6", 7);

            // Assert
            Assert.Equal("s0", q.HandleCurrentItem());
            Assert.Equal("s3", q.HandleCurrentItem());

            Assert.Equal(null, q.HandleCurrentItem());
            Assert.Equal("s5", q.HandleCurrentItem());
            Assert.Equal("s2", q.HandleCurrentItem());            

            Assert.Equal("s4", q.HandleCurrentItem());
            Assert.Equal("s6", q.HandleCurrentItem());
            Assert.Equal("s1", q.HandleCurrentItem());
        }
    }
}
