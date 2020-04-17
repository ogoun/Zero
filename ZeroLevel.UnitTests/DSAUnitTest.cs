using System;
using System.Linq;
using Xunit;
using ZeroLevel.Services.Serialization;
using ZeroLevel.Services.Trees;

namespace ZeroLevel.UnitTests
{
    public class DSAUnitTest
    {
        [Fact]
        public void DSATest()
        {
            // Arrange
            var words_in = new[] { "физика", "атомного", "ядра", "и", "элементарных", "частиц" };
            var words_out = new[] { "полгода", "после", "переезда" };
            var dsa = new DSA();

            // Act&Assert
            foreach (var w in words_in)
            {
                Assert.True(dsa.AppendWord(w));
            }

            //Count
            Assert.Equal(dsa.Count, words_in.Length);

            // Contains
            foreach (var w in words_in)
            {
                Assert.True(dsa.Contains(w));
            }
            foreach (var w in words_out)
            {
                Assert.False(dsa.Contains(w));
            }
            // Iterator
            var saved = dsa.Iterator().ToList();
            Assert.Equal(saved.Count, words_in.Length);
            foreach (var w in words_in)
            {
                Assert.True(saved.IndexOf(w) >= 0);
            }
            foreach (var w in words_out)
            {
                Assert.True(saved.IndexOf(w) == -1);
            }

            // Serialization
            var bf = MessageSerializer.Serialize(dsa);
            var restored = MessageSerializer.Deserialize<DSA>(bf);

            //Count
            Assert.Equal(restored.Count, words_in.Length);

            // Contains
            foreach (var w in words_in)
            {
                Assert.True(restored.Contains(w));
            }
            foreach (var w in words_out)
            {
                Assert.False(restored.Contains(w));
            }
            // Iterator
            saved = restored.Iterator().ToList();
            Assert.Equal(saved.Count, words_in.Length);
            foreach (var w in words_in)
            {
                Assert.True(saved.IndexOf(w) >= 0);
            }
            foreach (var w in words_out)
            {
                Assert.True(saved.IndexOf(w) == -1);
            }
        }
    }
}
