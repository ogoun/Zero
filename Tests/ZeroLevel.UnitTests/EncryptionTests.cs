using System;
using System.Linq;
using Xunit;
using ZeroLevel.DocumentObjectModel;
using ZeroLevel.Services.Encryption;
using ZeroLevel.Services.Serialization;
using ZeroLevel.UnitTests.Models;

namespace ZeroLevel.EncryptionUnitTests
{
    public class EncryptionTests
    {
        private static double CalculateDeviation(byte[] data)
        {
            double average = data.Average(b => (double)b);
            double sumOfSquaresOfDifferences = (double)data.Select(val => (val - average) * (val - average)).Sum();
            return Math.Sqrt(sumOfSquaresOfDifferences / data.Length);
        }

        [Fact]
        public void FastObfuscatorTest()
        {
            // Arrange
            var comparator = new Func<Document, Document, bool>((left, right) =>
            {
                var l_bin = MessageSerializer.Serialize(left);
                var r_bin = MessageSerializer.Serialize(right);
                return ArrayExtensions.UnsafeEquals(l_bin, r_bin);
            });
            var instance = CompositeInstanceFactory.MakeDocument();
            var data = MessageSerializer.Serialize(instance);

            // Act
            var obfscator = new FastObfuscator("mypassword");
            var deviation = CalculateDeviation(data);
            obfscator.HashData(data);
            var obf_deviation = CalculateDeviation(data);

            new FastObfuscator("mypassword").DeHashData(data);
            var deobf_deviation = CalculateDeviation(data);
            var clone = MessageSerializer.Deserialize<Document>(data);

            // Assert
            Assert.Equal(deviation, deobf_deviation);
            Assert.NotEqual(deviation, obf_deviation);
            Assert.True(obf_deviation >= deviation);
            Assert.True(comparator(instance, clone));
        }
    }
}
