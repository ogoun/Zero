using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ZeroLevel.Specification;

namespace ZeroSpecificationPatternsTest
{
    [TestClass]
    public class PredicateBuilderTests
    {
        [TestMethod]
        public void PredicateBuilder_FromExpression_AND()
        {
            // Arrange
            var expression = PredicateBuilder.True<string>();
            // Act
            var invoker = expression.
                And(str => str.Length < 255).
                And(str => char.IsLetter(str[0])).
                And(str => char.IsUpper(str[0])).Compile();
            // Assert
            Assert.IsFalse(invoker("test"));
            Assert.IsTrue(invoker("Test"));
            Assert.IsFalse(invoker("1test"));
            Assert.IsFalse(invoker("Test" + new string('_', 260)));
        }

        [TestMethod]
        public void PredicateBuilder_FromFunc_AND()
        {
            // Arrange
            var func = new Func<string, bool>(str => str.Length < 255);
            // Act
            var invoker = func.
                And(str => str.Length < 255).
                And(str => Char.IsLetter(str[0])).
                And(str => Char.IsUpper(str[0])).Compile();
            // Assert
            Assert.IsFalse(invoker("test"));
            Assert.IsTrue(invoker("Test"));
            Assert.IsFalse(invoker("1test"));
            Assert.IsFalse(invoker("Test" + new string('_', 260)));
        }

        [TestMethod]
        public void PredicateBuilder_FromExpression_OR()
        {
            // Arrange
            var expression = PredicateBuilder.
                Create<string>(str => str.Equals("hello", StringComparison.OrdinalIgnoreCase));
            // Act
            var invoker = expression.
                Or(str => str.Equals("world", StringComparison.OrdinalIgnoreCase)).
                Or(str => str.Equals("test", StringComparison.OrdinalIgnoreCase)).
                Or(str => str.Equals("wow", StringComparison.OrdinalIgnoreCase)).
                Compile();
            // Assert
            Assert.IsTrue(invoker("hello"));
            Assert.IsTrue(invoker("world"));
            Assert.IsTrue(invoker("test"));
            Assert.IsTrue(invoker("wow"));
            Assert.IsFalse(invoker("Tests"));
        }

        [TestMethod]
        public void PredicateBuilder_FromFunc_OR()
        {
            // Arrange
            var func = new Func<string, bool>(str => str.Equals("hello", StringComparison.OrdinalIgnoreCase));
            // Act
            var invoker = func.
                Or(str => str.Equals("world", StringComparison.OrdinalIgnoreCase)).
                Or(str => str.Equals("test", StringComparison.OrdinalIgnoreCase)).
                Or(str => str.Equals("wow", StringComparison.OrdinalIgnoreCase)).
                Compile();
            // Assert
            Assert.IsTrue(invoker("hello"));
            Assert.IsTrue(invoker("world"));
            Assert.IsTrue(invoker("test"));
            Assert.IsTrue(invoker("wow"));
            Assert.IsFalse(invoker("Tests"));
        }

        [TestMethod]
        public void PredicateBuilder_FromExpression_NOT()
        {
            // Arrange
            var expression = PredicateBuilder.
                Create<int>(i => i < 100 && i > 0);
            // Act
            var invoker = expression.Not().
                Compile();
            // Assert
            Assert.IsFalse(invoker(1));
            Assert.IsFalse(invoker(50));
            Assert.IsTrue(invoker(100));
            Assert.IsTrue(invoker(0));
        }

        [TestMethod]
        public void PredicateBuilder_FromFunc_NOT()
        {
            // Arrange
            var expression = new Func<int, bool>(i => i < 100 && i > 0);
            // Act
            var invoker = expression.Not().
                Compile();
            // Assert
            Assert.IsFalse(invoker(1));
            Assert.IsFalse(invoker(50));
            Assert.IsTrue(invoker(100));
            Assert.IsTrue(invoker(0));
        }
    }
}
