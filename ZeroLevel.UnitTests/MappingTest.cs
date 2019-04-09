using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroLevel.Services.ObjectMapping;
using ZeroMappingTest.Models;
using System.Collections.Generic;
using BusinessApp.DatabaseTest.Model;

namespace ZeroMappingTest
{
    [TestClass]
    public class MappingTest
    {
        [TestMethod]
        public void TestAbstractClassGetInfo()
        {
            // Arrange
            var mapper = TypeMapper.Create<BaseClass>();
            // Act
            var list = new List<string>();
            mapper.TraversalMembers(f => list.Add(f.Name));
            // Assert
            Assert.IsTrue(mapper.Exists("Id"));
            Assert.IsTrue(mapper.Exists("Title"));
            Assert.IsTrue(mapper.Exists("Description"));

            Assert.IsTrue(list.Contains("Id"));
            Assert.IsTrue(list.Contains("Title"));
            Assert.IsTrue(list.Contains("Description"));

            Assert.IsFalse(mapper.Exists("Version"));
            Assert.IsFalse(mapper.Exists("Created"));

            Assert.IsFalse(list.Contains("Version"));
            Assert.IsFalse(list.Contains("Created"));

            Assert.AreEqual(mapper.EntityType, typeof(BaseClass));
        }

        [TestMethod]
        public void TestInheritedClassGetInfo()
        {
            // Arrange
            var mapper = TypeMapper.Create<ChildClass>();
            // Act
            var list = new List<string>();
            mapper.TraversalMembers(f => list.Add(f.Name));
            // Assert
            Assert.IsTrue(mapper.Exists("Id"));
            Assert.IsTrue(mapper.Exists("Title"));
            Assert.IsTrue(mapper.Exists("Description"));
            Assert.IsTrue(mapper.Exists("Number"));
            Assert.IsTrue(mapper.Exists("Balance"));
            Assert.IsTrue(mapper.Exists("ReadOnlyProperty"));
            Assert.IsTrue(mapper.Exists("WriteOnlyProperty"));

            Assert.IsTrue(list.Contains("Id"));
            Assert.IsTrue(list.Contains("Title"));
            Assert.IsTrue(list.Contains("Description"));
            Assert.IsTrue(list.Contains("Number"));
            Assert.IsTrue(list.Contains("Balance"));
            Assert.IsTrue(list.Contains("ReadOnlyProperty"));
            Assert.IsTrue(list.Contains("WriteOnlyProperty"));

            Assert.IsFalse(mapper.Exists("HiddenField"));
            Assert.IsFalse(mapper.Exists("Version"));
            Assert.IsFalse(mapper.Exists("Created"));

            Assert.IsFalse(list.Contains("HiddenField"));
            Assert.IsFalse(list.Contains("Version"));
            Assert.IsFalse(list.Contains("Created"));

            Assert.AreEqual(mapper.EntityType, typeof(ChildClass));
        }
        
        [TestMethod]
        public void TestAbstractClassMapping()
        {
            // Arrange
            var instance = new ChildClass
            {
                Id = Guid.Empty,
                Title = "title",
                Description = "description",
                WriteOnlyProperty = 100,
                Balance = 100,
                Number = 100
            };
            var mapper = TypeMapper.Create<BaseClass>();
            // Act
            var id = Guid.NewGuid();
            var title = "New title";
            var description = "New description";
            mapper.Set(instance, "Id", id);
            mapper.Set(instance, "Title", title);
            mapper.Set(instance, "Description", description);
            // Assert
            Assert.AreEqual<Guid>(mapper.Get<Guid>(instance, "Id"), id);
            Assert.AreEqual<string>(mapper.Get<string>(instance, "Title"), title);
            Assert.AreEqual<string>(mapper.Get<string>(instance, "Description"), description);

            Assert.AreEqual(mapper.Get(instance, "Id"), id);
            Assert.AreEqual(mapper.Get(instance, "Title"), title);
            Assert.AreEqual(mapper.Get(instance, "Description"), description);
            try
            {
                mapper.Get(instance, "Number");
                Assert.Fail("Must be inaccessability");
            }
            catch
            {
            }
        }
        
        [TestMethod]
        public void TestInheritedClassMapping()
        {
            // Arrange
            var instance = new ChildClass
            {
                Id = Guid.Empty,
                Title = "title",
                Description = "description",
                WriteOnlyProperty = 100,
                Balance = 100,
                Number = 100
            };
            var mapper = TypeMapper.Create<ChildClass>();
            // Act
            var id = Guid.NewGuid();
            var title = "New title";
            var description = "New description";
            var number = 5465;
            var balance = 5555;


            mapper.Set(instance, "Id", id);
            mapper.Set(instance, "Title", title);
            mapper.Set(instance, "Description", description);
            mapper.Set(instance, "Number", number);
            mapper.Set(instance, "Balance", balance);
            // Assert
            Assert.AreEqual<Guid>(mapper.Get<Guid>(instance, "Id"), id);
            Assert.AreEqual<string>(mapper.Get<string>(instance, "Title"), title);
            Assert.AreEqual<string>(mapper.Get<string>(instance, "Description"), description);
            Assert.AreEqual<int>(mapper.Get<int>(instance, "Number"), number);
            Assert.AreEqual<int>(mapper.Get<int>(instance, "Balance"), balance);

            Assert.AreEqual(mapper.Get(instance, "Id"), id);
            Assert.AreEqual(mapper.Get(instance, "Title"), title);
            Assert.AreEqual(mapper.Get(instance, "Description"), description);
            Assert.AreEqual(mapper.Get(instance, "Number"), number);
            Assert.AreEqual(mapper.Get(instance, "Balance"), balance);

            try
            {
                var test = 1000;
                mapper.Set(instance, "ReadOnlyProperty", test);
                Assert.Fail("There should be no possibility to set a value.");
            }
            catch
            {

            }

            try
            {
                mapper.Get(instance, "WriteOnlyProperty");
                Assert.Fail("There should be no possibility to get a value.");
            }
            catch
            {

            }

            try
            {
                mapper.GetOrDefault(instance, "WriteOnlyProperty", null);
            }
            catch
            {
                Assert.Fail("It should be possible to get the default value.");
            }

            try
            {
                mapper.Get(instance, "Number");                
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void TestMapperscaching()
        {
            // Arrange
            var mapper1 = TypeMapper.Create<ChildClass>();
            var mapper2 = TypeMapper.Create<ChildClass>();
            var mapper3 = TypeMapper.Create<ChildClass>(false);
            // Act
            // Assert
            Assert.AreSame(mapper1, mapper2);
            Assert.AreNotSame(mapper1, mapper3);
            Assert.AreNotSame(mapper3, mapper2);
        }

        [TestMethod]
        public void PocoFieldMapper()
        {
            // Arrange
            var date = new DateTime(2005, 09, 27);
            var mapper = new TypeMapper(typeof(PocoFields));
            var obj = new PocoFields { Id = 1000, Date = date, Title = "Caption" };

            // Assert
            Assert.AreEqual(mapper.EntityType, typeof(PocoFields));

            Assert.IsTrue(mapper.Exists("Id"));
            Assert.IsTrue(mapper.Exists("Date"));
            Assert.IsTrue(mapper.Exists("Title"));

            Assert.AreEqual(mapper.Get(obj, "Id"), (long)1000);
            Assert.AreEqual(mapper.Get(obj, "Date"), date);
            Assert.AreEqual(mapper.Get(obj, "Title"), "Caption");

            mapper.Set(obj, "Id", 1001);
            Assert.AreEqual(mapper.Get(obj, "Id"), (long)1001);
        }

        [TestMethod]
        public void PocoPropertiesMapper()
        {
            // Arrange
            var date = new DateTime(2005, 09, 27);
            var mapper = new TypeMapper(typeof(PocoProperties));
            var obj = new PocoProperties { Id = 1000, Date = date, Title = "Caption" };

            // Assert
            Assert.AreEqual(mapper.EntityType, typeof(PocoProperties));

            Assert.IsTrue(mapper.Exists("Id"));
            Assert.IsTrue(mapper.Exists("Date"));
            Assert.IsTrue(mapper.Exists("Title"));

            Assert.AreEqual(mapper.Get(obj, "Id"), (long)1000);
            Assert.AreEqual(mapper.Get(obj, "Date"), date);
            Assert.AreEqual(mapper.Get(obj, "Title"), "Caption");

            mapper.Set(obj, "Id", 1001);
            Assert.AreEqual(mapper.Get(obj, "Id"), (long)1001);
        }
    }
}
