using System;
using ZeroLevel.Services.ObjectMapping;
using ZeroMappingTest.Models;
using System.Collections.Generic;
using BusinessApp.DatabaseTest.Model;
using Xunit;

namespace ZeroMappingTest
{
    public class MappingTest
    {
        [Fact]
        public void TestAbstractClassGetInfo()
        {
            // Arrange
            var mapper = TypeMapper.Create<BaseClass>();
            // Act
            var list = new List<string>();
            mapper.TraversalMembers(f => list.Add(f.Name));
            // Assert
            Assert.True(mapper.Exists("Id"));
            Assert.True(mapper.Exists("Title"));
            Assert.True(mapper.Exists("Description"));

            Assert.Contains<string>(list, s => s.Equals("Id", StringComparison.Ordinal));
            Assert.Contains<string>(list, s => s.Equals("Title", StringComparison.Ordinal));
            Assert.Contains<string>(list, s => s.Equals("Description", StringComparison.Ordinal));

            Assert.False(mapper.Exists("Version"));
            Assert.False(mapper.Exists("Created"));

            Assert.DoesNotContain<string>(list, s => s.Equals("Version", StringComparison.Ordinal));
            Assert.DoesNotContain<string>(list, s => s.Equals("Created", StringComparison.Ordinal));

            Assert.Equal(typeof(BaseClass), mapper.EntityType);
        }

        [Fact]
        public void TestInheritedClassGetInfo()
        {
            // Arrange
            var mapper = TypeMapper.Create<ChildClass>();
            // Act
            var list = new List<string>();
            mapper.TraversalMembers(f => list.Add(f.Name));
            // Assert
            Assert.True(mapper.Exists("Id"));
            Assert.True(mapper.Exists("Title"));
            Assert.True(mapper.Exists("Description"));
            Assert.True(mapper.Exists("Number"));
            Assert.True(mapper.Exists("Balance"));
            Assert.True(mapper.Exists("ReadOnlyProperty"));
            Assert.True(mapper.Exists("WriteOnlyProperty"));

            Assert.Contains<string>(list, s => s.Equals("Id", StringComparison.Ordinal));
            Assert.Contains<string>(list, s => s.Equals("Title", StringComparison.Ordinal));
            Assert.Contains<string>(list, s => s.Equals("Description", StringComparison.Ordinal));
            Assert.Contains<string>(list, s => s.Equals("Number", StringComparison.Ordinal));
            Assert.Contains<string>(list, s => s.Equals("Balance", StringComparison.Ordinal));
            Assert.Contains<string>(list, s => s.Equals("ReadOnlyProperty", StringComparison.Ordinal));
            Assert.Contains<string>(list, s => s.Equals("WriteOnlyProperty", StringComparison.Ordinal));

            Assert.False(mapper.Exists("HiddenField"));
            Assert.False(mapper.Exists("Version"));
            Assert.False(mapper.Exists("Created"));

            Assert.DoesNotContain<string>(list, s => s.Equals("HiddenField", StringComparison.Ordinal));
            Assert.DoesNotContain<string>(list, s => s.Equals("Version", StringComparison.Ordinal));
            Assert.DoesNotContain<string>(list, s => s.Equals("Created", StringComparison.Ordinal));

            Assert.Equal(typeof(ChildClass), mapper.EntityType);
        }

        [Fact]
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
            Assert.Equal<Guid>(id, mapper.Get<Guid>(instance, "Id"));
            Assert.Equal(title, mapper.Get<string>(instance, "Title"));
            Assert.Equal(description, mapper.Get<string>(instance, "Description"));

            Assert.Equal(id, mapper.Get(instance, "Id"));
            Assert.Equal(title, mapper.Get(instance, "Title"));
            Assert.Equal(description, mapper.Get(instance, "Description"));

            try
            {
                mapper.Get(instance, "Number");
                Assert.True(false, "Must be inaccessability");
            }
            catch
            {
            }
        }

        [Fact]
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
            Assert.Equal<Guid>(mapper.Get<Guid>(instance, "Id"), id);
            Assert.Equal(mapper.Get<string>(instance, "Title"), title);
            Assert.Equal(mapper.Get<string>(instance, "Description"), description);
            Assert.Equal<int>(mapper.Get<int>(instance, "Number"), number);
            Assert.Equal<int>(mapper.Get<int>(instance, "Balance"), balance);

            Assert.Equal(mapper.Get(instance, "Id"), id);
            Assert.Equal(mapper.Get(instance, "Title"), title);
            Assert.Equal(mapper.Get(instance, "Description"), description);
            Assert.Equal(mapper.Get(instance, "Number"), number);
            Assert.Equal(mapper.Get(instance, "Balance"), balance);

            try
            {
                var test = 1000;
                mapper.Set(instance, "ReadOnlyProperty", test);
                Assert.True(false, "There should be no possibility to set a value.");
            }
            catch
            {

            }

            try
            {
                mapper.Get(instance, "WriteOnlyProperty");
                Assert.True(false, "There should be no possibility to get a value.");
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
                Assert.True(false, "It should be possible to get the default value.");
            }

            try
            {
                mapper.Get(instance, "Number");
            }
            catch (Exception ex)
            {
                Assert.True(false, ex.Message);
            }
        }

        [Fact]
        public void TestMapperscaching()
        {
            // Arrange
            var mapper1 = TypeMapper.Create<ChildClass>();
            var mapper2 = TypeMapper.Create<ChildClass>();
            var mapper3 = TypeMapper.Create<ChildClass>(false);
            // Act
            // Assert
            Assert.Same(mapper1, mapper2);
            Assert.NotSame(mapper1, mapper3);
            Assert.NotSame(mapper3, mapper2);
        }

        [Fact]
        public void PocoFieldMapper()
        {
            // Arrange
            var date = new DateTime(2005, 09, 27);
            var mapper = new TypeMapper(typeof(PocoFields));
            var obj = new PocoFields { Id = 1000, Date = date, Title = "Caption" };

            // Assert
            Assert.Equal(typeof(PocoFields), mapper.EntityType);

            Assert.True(mapper.Exists("Id"));
            Assert.True(mapper.Exists("Date"));
            Assert.True(mapper.Exists("Title"));

            Assert.Equal((long)1000, mapper.Get(obj, "Id"));
            Assert.Equal(date, mapper.Get(obj, "Date"));
            Assert.Equal("Caption", mapper.Get(obj, "Title"));

            mapper.Set(obj, "Id", 1001);
            Assert.Equal(mapper.Get(obj, "Id"), (long)1001);
        }

        [Fact]
        public void PocoPropertiesMapper()
        {
            // Arrange
            var date = new DateTime(2005, 09, 27);
            var mapper = new TypeMapper(typeof(PocoProperties));
            var obj = new PocoProperties { Id = 1000, Date = date, Title = "Caption" };

            // Assert
            Assert.Equal(typeof(PocoProperties), mapper.EntityType);

            Assert.True(mapper.Exists("Id"));
            Assert.True(mapper.Exists("Date"));
            Assert.True(mapper.Exists("Title"));

            Assert.Equal((long)1000, mapper.Get(obj, "Id"));
            Assert.Equal(date, mapper.Get(obj, "Date"));
            Assert.Equal("Caption", mapper.Get(obj, "Title"));

            mapper.Set(obj, "Id", 1001);
            Assert.Equal(mapper.Get(obj, "Id"), (long)1001);
        }
    }
}
