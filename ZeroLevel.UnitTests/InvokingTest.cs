using System;
using System.Reflection;
using Xunit;
using ZeroInvokingTest.Models;
using ZeroLevel.Services.Invokation;

namespace ZeroInvokingTest
{
    public class InvokingTest
    {
        [Fact]
        public void InvokeTypeAllMethod()
        {
            // Arrange            
            var invoker = InvokeWrapper.Create();
            // Act
            invoker.Configure<FakeClass>();
            var identityGetString = invoker.GetInvokerIdentity("GetString", new Type[] { typeof(string) });
            var identityGetNumber = invoker.GetInvokerIdentity("GetNumber", new Type[] { typeof(int) });
            var identityGetDateTime = invoker.GetInvokerIdentity("GetDateTime", new Type[] { typeof(DateTime) });
            var identityGetHelp = invoker.GetInvokerIdentity("GetHelp", null);
            // Assert
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetString, new object[] { "hello" }), "hello");
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetNumber, new object[] { 100 }), 100);
            var date = DateTime.Now;
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetDateTime, new object[] { date }), date);
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetHelp), "help");
        }

        [Fact]
        public void InvokeTypeMethodByName()
        {
            // Arrange
            var invoker = InvokeWrapper.Create();
            // Act
            //invoker.Configure<FakeClass>("GetString");
            invoker.Configure<FakeClass>("GetHelp");
            invoker.Configure<FakeClass>("GetNumber");
            invoker.Configure<FakeClass>("GetDateTime");
            var identityGetString = invoker.GetInvokerIdentity("GetString", new Type[] { typeof(string) });
            var identityGetNumber = invoker.GetInvokerIdentity("GetNumber", new Type[] { typeof(int) });
            var identityGetDateTime = invoker.GetInvokerIdentity("GetDateTime", new Type[] { typeof(DateTime) });
            var identityGetHelp = invoker.GetInvokerIdentity("GetHelp", null);
            // Assert
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetNumber, new object[] { 100 }), 100);
            var date = DateTime.Now;
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetDateTime, new object[] { date }), date);
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetHelp), "help");
        }

        [Fact]
        public void InvokeTypeMethodByFilter()
        {
            // Arrange
            var invoker = InvokeWrapper.Create();
            // Act
            invoker.Configure<FakeClass>(m => m.Name.Equals("GetHelp") || m.Name.Equals("GetNumber") || m.Name.Equals("GetDateTime"));
            var identityGetString = invoker.GetInvokerIdentity("GetString", new Type[] { typeof(string) });
            var identityGetNumber = invoker.GetInvokerIdentity("GetNumber", new Type[] { typeof(int) });
            var identityGetDateTime = invoker.GetInvokerIdentity("GetDateTime", new Type[] { typeof(DateTime) });
            var identityGetHelp = invoker.GetInvokerIdentity("GetHelp", null);
            // Assert
            var date = DateTime.Now;
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetDateTime, new object[] { date }), date);
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetNumber, new object[] { 100 }), 100);
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetHelp), "help");
        }

        [Fact]
        public void InvokeByMethodsList()
        {
            // Arrange
            var invoker = InvokeWrapper.Create();
            // Act
            invoker.Configure(new MethodInfo[]
            {
                typeof(FakeClass).GetMethod("GetHelp", BindingFlags.Public | BindingFlags.FlattenHierarchy| BindingFlags.Instance),
                typeof(FakeClass).GetMethod("GetNumber", BindingFlags.NonPublic| BindingFlags.Instance),
                typeof(FakeClass).GetMethod("GetDateTime", BindingFlags.NonPublic| BindingFlags.Instance),
                typeof(FakeClass).GetMethod("GetString")
            });
            var identityGetString = invoker.GetInvokerIdentity("GetString", new Type[] { typeof(string) });
            var identityGetNumber = invoker.GetInvokerIdentity("GetNumber", new Type[] { typeof(int) });
            var identityGetDateTime = invoker.GetInvokerIdentity("GetDateTime", new Type[] { typeof(DateTime) });
            var identityGetHelp = invoker.GetInvokerIdentity("GetHelp", null);
            // Assert
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetString, new object[] { "hello" }), "hello");
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetNumber, new object[] { 100 }), 100);
            var date = DateTime.Now;
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetDateTime, new object[] { date }), date);
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetHelp), "help");
        }

        [Fact]
        public void InvokeByMethods()
        {
            // Arrange
            var invoker = InvokeWrapper.Create();
            // Act
            invoker.Configure(typeof(FakeClass).GetMethod("GetHelp", BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance));
            invoker.Configure(typeof(FakeClass).GetMethod("GetNumber", BindingFlags.NonPublic | BindingFlags.Instance));
            invoker.Configure(typeof(FakeClass).GetMethod("GetDateTime", BindingFlags.NonPublic | BindingFlags.Instance));
            invoker.Configure(typeof(FakeClass).GetMethod("GetString"));
            var identityGetString = invoker.GetInvokerIdentity("GetString", new Type[] { typeof(string) });
            var identityGetNumber = invoker.GetInvokerIdentity("GetNumber", new Type[] { typeof(int) });
            var identityGetDateTime = invoker.GetInvokerIdentity("GetDateTime", new Type[] { typeof(DateTime) });
            var identityGetHelp = invoker.GetInvokerIdentity("GetHelp", null);
            // Assert
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetString, new object[] { "hello" }), "hello");
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetNumber, new object[] { 100 }), 100);
            var date = DateTime.Now;
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetDateTime, new object[] { date }), date);
            Assert.Equal(invoker.Invoke(new FakeClass(), identityGetHelp), "help");
        }

        [Fact]
        public void InvokeStaticByMethods()
        {
            // Arrange
            var invoker = InvokeWrapper.Create();
            // Act
            invoker.Configure(typeof(StaticFakeClass).GetMethod("GetNumber", BindingFlags.NonPublic | BindingFlags.Static));
            invoker.Configure(typeof(StaticFakeClass).GetMethod("GetDateTime", BindingFlags.NonPublic | BindingFlags.Static));
            invoker.Configure(typeof(StaticFakeClass).GetMethod("GetString"));
            var identityGetString = invoker.GetInvokerIdentity("GetString", new Type[] { typeof(string) });
            var identityGetNumber = invoker.GetInvokerIdentity("GetNumber", new Type[] { typeof(int) });
            var identityGetDateTime = invoker.GetInvokerIdentity("GetDateTime", new Type[] { typeof(DateTime) });
            // Assert
            Assert.Equal(invoker.InvokeStatic(identityGetString, new object[] { "hello" }), "hello");
            Assert.Equal(invoker.InvokeStatic(identityGetNumber, new object[] { 100 }), 100);
            var date = DateTime.Now;
            Assert.Equal(invoker.InvokeStatic(identityGetDateTime, new object[] { date }), date);
        }

        [Fact]
        public void InvokeByDelegate()
        {
            // Arrange
            var invoker = InvokeWrapper.Create();
            // Act
            var func = new Func<string, bool>(str => str.Length > 0);
            var name = invoker.Configure(func);
            // Assert
            Assert.True((bool)invoker.Invoke(func.Target, name, new object[] { "hello" }));
        }
    }
}
