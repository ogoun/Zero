using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using ZeroInvokingTest.Models;
using ZeroLevel.Services.Invokation;

namespace ZeroInvokingTest
{
    [TestClass]
    public class InvokingTest
    {
        [TestMethod]
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
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetString, new object[] { "hello" }), "hello");
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetNumber, new object[] { 100 }), 100);
            var date = DateTime.Now;
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetDateTime, new object[] { date }), date);
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetHelp), "help");
        }

        [TestMethod]
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
            try
            {
                var obj = invoker.Invoke(new FakeClass(), identityGetString, new object[] { "hello" });
                Assert.Fail("An exception should have been thrown");
            }
            catch { }
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetNumber, new object[] { 100 }), 100);
            var date = DateTime.Now;
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetDateTime, new object[] { date }), date);
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetHelp), "help");
        }

        [TestMethod]
        public void InvokeTypeMethodByFilter()
        {
            // Arrange
            var invoker = InvokeWrapper.Create();
            // Act
            invoker.Configure<FakeClass>(m => m.Name.Equals("GetHelp") || m.Name.Equals("GetNumber"));
            var identityGetString = invoker.GetInvokerIdentity("GetString", new Type[] { typeof(string) });
            var identityGetNumber = invoker.GetInvokerIdentity("GetNumber", new Type[] { typeof(int) });
            var identityGetDateTime = invoker.GetInvokerIdentity("GetDateTime", new Type[] { typeof(DateTime) });
            var identityGetHelp = invoker.GetInvokerIdentity("GetHelp", null);
            // Assert
            try
            {
                var obj = invoker.Invoke(new FakeClass(), identityGetString, new object[] { "hello" });
                Assert.Fail("An exception should have been thrown");
            }
            catch { }
            try
            {
                var date = DateTime.Now;
                Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetDateTime, new object[] { date }), date);
                Assert.Fail("An exception should have been thrown");
            }
            catch { }
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetNumber, new object[] { 100 }), 100);
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetHelp), "help");
        }

        [TestMethod]
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
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetString, new object[] { "hello" }), "hello");
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetNumber, new object[] { 100 }), 100);
            var date = DateTime.Now;
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetDateTime, new object[] { date }), date);
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetHelp), "help");
        }

        [TestMethod]
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
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetString, new object[] { "hello" }), "hello");
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetNumber, new object[] { 100 }), 100);
            var date = DateTime.Now;
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetDateTime, new object[] { date }), date);
            Assert.AreEqual(invoker.Invoke(new FakeClass(), identityGetHelp), "help");
        }

        [TestMethod]
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
            Assert.AreEqual(invoker.InvokeStatic(identityGetString, new object[] { "hello" }), "hello");
            Assert.AreEqual(invoker.InvokeStatic(identityGetNumber, new object[] { 100 }), 100);
            var date = DateTime.Now;
            Assert.AreEqual(invoker.InvokeStatic(identityGetDateTime, new object[] { date }), date);
        }

        [TestMethod]
        public void InvokeByDelegate()
        {
            // Arrange
            var invoker = InvokeWrapper.Create();
            // Act
            var func = new Func<string, bool>(str => str.Length > 0);
            var name = invoker.Configure(func);
            // Assert
            Assert.IsTrue((bool)invoker.Invoke(func.Target, name, new object[] { "hello" }));
        }
    }
}
