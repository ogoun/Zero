using System.Collections.Generic;
using System.Net;
using System.Threading;
using Xunit;
using ZeroLevel.Network;

namespace ZeroLevel.NetworkUnitTests
{
    public class ExchangeTests
    {
        [Fact]
        public void HandleMessageTest()
        {
            // Arrange
            var info = new ExServiceInfo
            {
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
            var client = ExchangeTransportFactory.GetClient(IPAddress.Loopback.ToString() + ":6666");
            var ir = client.Send<ExServiceInfo>("register", info);

            locker.WaitOne(1000);

            // Assert
            Assert.True(ir.Success);
            Assert.True(info.Equals(received));

            // Dispose
            locker.Dispose();
            client.Dispose();
            server.Dispose();
        }

        [Fact]
        public void RequestMessageTest()
        {
            // Arrange
            var info1 = new ExServiceInfo
            {
                ServiceGroup = "MyServiceGroup",
                ServiceKey = "MyServiceKey",
                ServiceType = "MyServiceType",
                Version = "1.1.1.1"
            };
            var info2 = new ExServiceInfo
            {
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
            var client = ExchangeTransportFactory.GetClient(IPAddress.Loopback.ToString() + ":6666");
            var ir = client.Request<IEnumerable<ExServiceInfo>>("services", response =>
            {
                received = response;
                locker.Set();
            });

            locker.WaitOne(1000);

            // Assert
            Assert.True(ir.Success);
            Assert.True(CollectionComparsionExtensions.OrderingEquals(new[] { info1, info2 }, received, (a, b) => a.Equals(b)));

            // Dispose
            locker.Dispose();
            client.Dispose();
            server.Dispose();
        }
    }
}
