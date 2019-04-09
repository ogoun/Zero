using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ZeroLevel.Services.ObjectMapping;
using ZeroLevel.Services.Reflection;

namespace ZeroLevel.ReflectionUnitTests
{
    [TestClass]
    public class ReflectionTests
    {
        [TestMethod]
        public void TestDTORuntymeBuildedTypes()
        {
            // Arrange
            var date = DateTime.Now;
            var typeBuilder = new DTOTypeBuilder("MyType");
            typeBuilder.AppendField<string>("Title");
            typeBuilder.AppendProperty<long>("Id");
            typeBuilder.AppendProperty<DateTime>("Created");

            var type = typeBuilder.Complete();

            // Act
            var mapper = TypeMapper.Create(type);
            var instance = TypeHelpers.CreateNonInitializedInstance(type);
            mapper.Set(instance, "Title", "This is title");
            mapper.Set(instance, "Id", 1001001);
            mapper.Set(instance, "Created", date);

            // Assert
            Assert.IsTrue(mapper.Exists("Title"));
            Assert.IsTrue(mapper.Exists("Id"));
            Assert.IsTrue(mapper.Exists("Created"));
            Assert.AreEqual(mapper.Get(instance, "Id"), (long)1001001);
            Assert.AreEqual(mapper.Get(instance, "Created"), date);
            Assert.AreEqual(mapper.Get(instance, "Title"), "This is title");
        }
    }
}
