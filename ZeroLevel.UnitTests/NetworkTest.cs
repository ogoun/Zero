using System;
using System.Threading;
using Xunit;
using ZeroLevel.Network;

namespace ZeroLevel.UnitTests
{
    public class NetworkTest
    {
        [Fact]
        public void ClientServerTest()
        {
            // Arrange
            var server = ExchangeTransportFactory.GetServer(8181);
            var client = ExchangeTransportFactory.GetClient("127.0.0.1:8181");

            bool got_message_no_request = false;
            bool got_message_with_request = false;
            bool got_response_message_no_request = false;
            bool got_response_message_with_request = false;

            using (var signal = new ManualResetEvent(false))
            {
                server.RegisterInbox("empty", (_, __) => { signal.Set(); got_message_no_request = true; });
                server.RegisterInbox<string>((_, __, ___) => { signal.Set(); got_message_with_request = true; });
                server.RegisterInbox<string>("get_response", (_, __) => { return "Hello"; });
                server.RegisterInbox<int, string>("convert", (num, _, __) => { return num.ToString(); });

                // Act
                signal.Reset();
                client.Send("empty");
                signal.WaitOne(1000);

                signal.Reset();
                client.Send<string>("hello");
                signal.WaitOne(100);

                signal.Reset();
                client.Request<string>("get_response", (s) => { signal.Set(); got_response_message_no_request = s.Equals("Hello", StringComparison.Ordinal); });
                signal.WaitOne(1000);

                signal.Reset();
                client.Request<int, string>("convert", 10, (s) => { signal.Set(); got_response_message_with_request = s.Equals("10", StringComparison.Ordinal); });
                signal.WaitOne(1000);
            }



            // Assert
            Assert.True(got_message_no_request, "No signal for no request default inbox");
            Assert.True(got_message_with_request, "No signal for default inbox");
            Assert.True(got_response_message_no_request, "No response without request");
            Assert.True(got_response_message_with_request, "No response with request");
        }
    }
}
