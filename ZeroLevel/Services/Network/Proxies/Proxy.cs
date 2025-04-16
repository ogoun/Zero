using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Network.Proxies
{
    public class Proxy
        : IDisposable
    {
        private readonly ProxyBalancer _balancer = new();

        public void AppendServer(IPEndPoint ep) => _balancer.AddEndpoint(ep);

        private readonly Socket _incomingSocket;

        public Proxy(IPEndPoint listenEndpoint)
        {
            _incomingSocket = new Socket(listenEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _incomingSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            _incomingSocket.Bind(listenEndpoint);
        }

        public void Run()
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    _incomingSocket.Listen(100);
                    while (true)
                    {
                        var socket = await _incomingSocket.AcceptAsync();
                        // no await!
                        await Task.Run(async () =>
                        {
                            await CreateProxyConnection(socket);
                        }).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[Proxy.Run]");
                }
            });
        }

        private async Task CreateProxyConnection(Socket connection)
        {
            try
            {
                var endpoint = _balancer.GetServerProxy();
                var server = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
                server.Connect(endpoint);
                using var bind = new ProxyBinding(connection, server);
                await bind.Bind();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[Proxy.CreateProxyConnection]");
            }
        }

        public void Dispose()
        {
            try
            {
                _incomingSocket.Shutdown(SocketShutdown.Both);
                _incomingSocket.Dispose();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[Proxy.Dispose]");
            }
        }
    }
}
