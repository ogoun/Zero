using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel.Network;

namespace ZeroLevel.NetworkUnitTests
{
    [TestClass]
    public class ExchangeTests
    {
        [TestMethod]
        public void HandleMessageTest()
        {
            // Arrange
            var info = new ExServiceInfo
            {
                Endpoint = "192.168.1.11:7755",
                ServiceGroup = "MyServiceGroup",
                ServiceKey = "MyServiceKey",
                ServiceType = "MyServiceType",
                Version = "1.1.1.1"
            };
            var locker = new ManualResetEvent(false);
            var server = ExchangeTransportFactory.GetServer(6666);
            ExServiceInfo received = null;

            server.RegisterInbox<ExServiceInfo>("register", (i, _, __) =>
            {
                received = i;
                locker.Set();
            });

            // Act
            var client = ExchangeTransportFactory.GetClient(server.Endpoint.Address.ToString() + ":6666");
            var ir = client.Send<ExServiceInfo>("register", info);

            locker.WaitOne(1000);

            // Assert
            Assert.IsTrue(ir.Success);
            Assert.IsTrue(info.Equals(received));

            // Dispose
            locker.Dispose();
            client.Dispose();
            server.Dispose();
        }

        [TestMethod]
        public void RequestMessageTest()
        {
            // Arrange
            var info1 = new ExServiceInfo
            {
                Endpoint = "192.168.1.11:7755",
                ServiceGroup = "MyServiceGroup",
                ServiceKey = "MyServiceKey",
                ServiceType = "MyServiceType",
                Version = "1.1.1.1"
            };
            var info2 = new ExServiceInfo
            {
                Endpoint = "192.168.41.11:4564",
                ServiceGroup = "MyServiceGroup",
                ServiceKey = "MyServiceKey2",
                ServiceType = "MyServiceType",
                Version = "1.1.0.1"
            };
            var locker = new ManualResetEvent(false);
            var server = ExchangeTransportFactory.GetServer(6666);
            IEnumerable<ExServiceInfo> received = null;

            server.RegisterInbox<IEnumerable<ExServiceInfo>>("services", (_, __) => new[] { info1, info2 });

            // Act
            var client = ExchangeTransportFactory.GetClient(server.Endpoint.Address.ToString() + ":6666");
            var ir = client.Request<IEnumerable<ExServiceInfo>>("services", response =>
            {
                received = response;
                locker.Set();
            });

            locker.WaitOne(1000);

            // Assert
            Assert.IsTrue(ir.Success);
            Assert.IsTrue(CollectionComparsionExtensions.OrderingEquals(new[] { info1, info2 }, received, (a, b) => a.Equals(b)));

            // Dispose
            locker.Dispose();
            client.Dispose();
            server.Dispose();
        }
    }
}
