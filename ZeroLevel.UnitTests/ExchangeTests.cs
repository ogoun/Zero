using System.Collections.Generic;
using System.Net;
using System.Threading;
using Xunit;
using ZeroLevel.Services.Applications;

namespace ZeroLevel.NetworkUnitTests
{
    public class ExchangeTests
        : BaseZeroService
    {
        [Fact]
        public void HandleMessageTest()
        {
            // Arrange
            var info = new ZeroServiceInfo
            {
                ServiceGroup = "MyServiceGroup",
                ServiceKey = "MyServiceKey",
                ServiceType = "MyServiceType",
                Version = "1.1.1.1"
            };
            var locker = new ManualResetEvent(false);
            var server = UseHost(6666);
            ZeroServiceInfo received = null;

            server.RegisterInbox<ZeroServiceInfo>("register", (c, i) =>
            {
                received = i;
                locker.Set();
            });

            // Act
            var client = Exchange.GetConnection(IPAddress.Loopback.ToString() + ":6666");
            var ir = client.Send<ZeroServiceInfo>("register", info);

            locker.WaitOne(1000);

            // Assert
            Assert.True(ir.Success);
            Assert.True(info.Equals(received));

            // Dispose
            locker.Dispose();
            client.Dispose();
        }

        [Fact]
        public void RequestMessageTest()
        {
            // Arrange
            var info1 = new ZeroServiceInfo
            {
                ServiceGroup = "MyServiceGroup",
                ServiceKey = "MyServiceKey",
                ServiceType = "MyServiceType",
                Version = "1.1.1.1"
            };
            var info2 = new ZeroServiceInfo
            {
                ServiceGroup = "MyServiceGroup",
                ServiceKey = "MyServiceKey2",
                ServiceType = "MyServiceType",
                Version = "1.1.0.1"
            };
            var locker = new ManualResetEvent(false);
            var server = UseHost(6667);
            IEnumerable<ZeroServiceInfo> received = null;

            server.RegisterInbox<IEnumerable<ZeroServiceInfo>>("services", (_) => new[] { info1, info2 });

            // Act
            var client = Exchange.GetConnection(IPAddress.Loopback.ToString() + ":6667");
            var ir = client.Request<IEnumerable<ZeroServiceInfo>>("services", response =>
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
        }

        protected override void StartAction()
        {
        }

        protected override void StopAction()
        {
        }
    }
}
