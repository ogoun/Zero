using System;
using System.Threading;
using Xunit;
using ZeroLevel.Services.Applications;
using ZeroLevel.Services.Network.Proxies;

namespace ZeroLevel.Network
{
    public class NetworkTest
        : BaseZeroService
    {
        [Fact]
        public void ClientServerTest()
        {
            // Arrange
            var server = UseHost(8181);
            Exchange.RoutesStorage.Set("test", NetUtils.CreateIPEndPoint("127.0.0.1:8181"));

            bool got_message_no_request = false;
            bool got_message_with_request = false;
            bool got_response_message_no_request = false;
            bool got_response_message_with_request = false;

            server.RegisterInbox("empty", (_) =>
            {
                got_message_no_request = true;
            });
            server.RegisterInbox<string>((_, ___) =>
            {
                got_message_with_request = true;
            });
            server.RegisterInbox<string>("get_response", (_) => "Hello");
            server.RegisterInbox<int, string>("convert", (__, num) => num.ToString());

            Thread.Sleep(200);

            Assert.True(Exchange.Peek("test", "empty"));
            Assert.True(Exchange.Send<string>("test", "hello"));

            int repeat = 10;
            while (!got_message_no_request || !got_message_with_request)
            {
                Thread.Sleep(200);
                repeat--;
                if (repeat == 0) break;
            }

            // Assert
            Assert.True(got_message_no_request, "No signal for no request default inbox");
            Assert.True(got_message_with_request, "No signal for default inbox");

            for (int i = 0; i < 100; i++)
            {
                got_response_message_no_request =
                    Exchange.Request<string>("test", "get_response")
                    .Equals("Hello", StringComparison.Ordinal);

                got_response_message_with_request =
                    Exchange.Request<int, string>("test", "convert", 10)
                    .Equals("10", StringComparison.Ordinal);

                Assert.True(got_response_message_no_request, "No response without request");
                Assert.True(got_response_message_with_request, "No response with request");
            }
        }

        [Fact]
        public void ProxyTest()
        {
            bool got_message_no_request = false;
            bool got_message_with_request = false;

            using (var proxy = new Proxy(NetUtils.CreateIPEndPoint("127.0.0.1:92")))
            {
                proxy.AppendServer(NetUtils.CreateIPEndPoint("127.0.0.1:93"));
                proxy.Run();
                var server = Exchange.UseHost(NetUtils.CreateIPEndPoint("127.0.0.1:93"));
                server.RegisterInbox("empty", (_) =>
                {
                    got_message_no_request = true;
                });
                server.RegisterInbox<bool>((_, ___) =>
                {
                    got_message_with_request = true;
                });

                var client = Exchange.GetConnection(NetUtils.CreateIPEndPoint("127.0.0.1:92"));
                int repeat = 10;

                Assert.True(client.Send("empty"));
                Assert.True(client.Send<bool>(true));
                while (!got_message_no_request || !got_message_with_request)
                {
                    Thread.Sleep(200);
                    repeat--;
                    if (repeat == 0) break;
                }                
            }
            Assert.True(got_message_no_request, "No signal for no request default inbox");
            Assert.True(got_message_with_request, "No signal for default inbox");
        }

        protected override void StartAction()
        {
            throw new NotImplementedException();
        }

        protected override void StopAction()
        {
            throw new NotImplementedException();
        }
    }
}
