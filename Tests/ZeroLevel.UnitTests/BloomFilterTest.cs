using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using ZeroLevel.DataStructures;

namespace ZeroLevel.UnitTests
{
    public class BloomFilterTest
    {
        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        [Fact]
        public void SimpleBloomFilterTest()
        {
            // Arrange
            var size = 100000;
            var lines = new HashSet<string>(size);
            var lines_another = new HashSet<string>(size);
            for (int i = 0; i < size; i++)
            {
                lines.Add(RandomString(i % 9 + 5));
                lines_another.Add(RandomString(i % 9 + 5));
            }
            var bloom = new BloomFilter(16536 * 2048);
            // Act
            var sw = new Stopwatch();
            sw.Start();
            foreach (var line in lines)
            {
                bloom.Add(line);
            }
            sw.Stop();
            Debug.Print($"BloomFilter. Append {lines.Count} items. {sw.ElapsedMilliseconds} ms");

            // Assert
            foreach (var line in lines)
            {
                Assert.True(bloom.Contains(line));
            }

            int collision_count = 0;

            foreach (var line in lines_another)
            {
                if (bloom.Contains(line))
                {
                    if (false == lines.Contains(line))
                    {
                        collision_count++;
                    }
                }
            }

            Debug.WriteLine($"Collision for string: {collision_count}.");
        }

        [Fact]
        public void HyperBloomBloomFilterTest()
        {
            // Arrange
            var size = 1000000;
            var lines = new HashSet<string>(size);
            var lines_another = new HashSet<string>(size);
            for (int i = 0; i < size; i++)
            {
                lines.Add(RandomString(i % 9 + 5));
                lines_another.Add(RandomString(i % 9 + 5));
            }
            var bloom = new HyperBloomBloom(16, 16536 * 4096);
            // Act
            var sw = new Stopwatch();
            sw.Start();
            foreach (var line in lines)
            {
                bloom.Add(line);
            }
            sw.Stop();
            Debug.Print($"BloomFilter. Append {lines.Count} items. {sw.ElapsedMilliseconds} ms");

            // Assert
            foreach (var line in lines)
            {
                Assert.True(bloom.Contains(line));
            }

            int collision_count = 0;

            foreach (var line in lines_another)
            {
                if (bloom.Contains(line))
                {
                    if (false == lines.Contains(line))
                    {
                        collision_count++;
                    }
                }
            }

            Debug.WriteLine($"Collision for string: {collision_count}.");
        }
    }
}
