using System;
using System.Threading;
using Xunit;
using ZeroLevel.Services.Applications;

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
