using Xunit;
using ZeroLevel.Services.Semantic;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Tries
{
    public class TrieTests
    {
        [Fact]
        public void MainTest()
        {
            // Arrange            
            var tree = new Trie();
            // Act
            tree.Append("коллекция");
            tree.Append("коллектор");
            tree.Append("колл-центр");
            tree.Append("коллектив");
            tree.Append("коллегия");
            tree.Append("метро");
            tree.Append("метрополитен");
            tree.Append("метрополит");
            // Assert
            Assert.True(tree.Key("коллекция") == 1);
            Assert.True(tree.Key("коллектор") == 2);
            Assert.True(tree.Key("колл-центр") == 3);
            Assert.True(tree.Key("коллектив") == 4);
            Assert.True(tree.Key("коллегия") == 5);
            Assert.True(tree.Key("метро") == 6);
            Assert.True(tree.Key("метрополитен") == 7);
            Assert.True(tree.Key("метрополит") == 8);

            Assert.True(tree.Key("колл") == null);
            Assert.True(tree.Key("центр") == null);

            Assert.True(tree.Contains("коллекция"));
            Assert.True(tree.Contains("коллектор"));
            Assert.True(tree.Contains("колл-центр"));
            Assert.True(tree.Contains("коллектив"));
            Assert.True(tree.Contains("коллегия"));
            Assert.True(tree.Contains("метро"));
            Assert.True(tree.Contains("метрополитен"));
            Assert.True(tree.Contains("метрополит"));

            Assert.False(tree.Contains("колл"));
            Assert.False(tree.Contains("коллег"));
        }

        [Fact]
        public void SerializationTest()
        {
            // Arrange            
            var tree_original = new Trie();
            // Act
            tree_original.Append("коллекция");
            tree_original.Append("коллектор");
            tree_original.Append("колл-центр");
            tree_original.Append("коллектив");
            tree_original.Append("коллегия");
            tree_original.Append("метро");
            tree_original.Append("метрополитен");
            tree_original.Append("метрополит");

            var data = MessageSerializer.Serialize(tree_original);
            var tree = MessageSerializer.Deserialize<Trie>(data);

            // Assert
            Assert.True(tree.Key("коллекция") == 1);
            Assert.True(tree.Key("коллектор") == 2);
            Assert.True(tree.Key("колл-центр") == 3);
            Assert.True(tree.Key("коллектив") == 4);
            Assert.True(tree.Key("коллегия") == 5);
            Assert.True(tree.Key("метро") == 6);
            Assert.True(tree.Key("метрополитен") == 7);
            Assert.True(tree.Key("метрополит") == 8);

            Assert.True(tree.Key("колл") == null);
            Assert.True(tree.Key("центр") == null);

            Assert.True(tree.Contains("коллекция"));
            Assert.True(tree.Contains("коллектор"));
            Assert.True(tree.Contains("колл-центр"));
            Assert.True(tree.Contains("коллектив"));
            Assert.True(tree.Contains("коллегия"));
            Assert.True(tree.Contains("метро"));
            Assert.True(tree.Contains("метрополитен"));
            Assert.True(tree.Contains("метрополит"));

            Assert.False(tree.Contains("колл"));
            Assert.False(tree.Contains("коллег"));
        }
    }
}
