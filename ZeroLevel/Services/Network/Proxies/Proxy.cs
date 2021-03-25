using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Network.Proxies
{
    public class Proxy
    {
        private readonly ProxyBalancer _balancer = new ProxyBalancer();

        public void AppendServer(IPEndPoint ep) => _balancer.AddEndpoint(ep);

        private Socket _incomingSocket;

        public Proxy(IPEndPoint listenEndpoint)
        {
            _incomingSocket = new Socket(listenEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _incomingSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            _incomingSocket.Bind(listenEndpoint);
            _incomingSocket.Listen(100);
        }

        public async Task Run()
        {
            while (true)
            {
                var socket = await _incomingSocket.AcceptAsync();
                // no await!
                CreateProxyConnection(socket);
            }
        }

        public async Task CreateProxyConnection(Socket connection)
        {
            var endpoint = _balancer.GetServerProxy();
            var server = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            server.Bind(endpoint);
            using (var bind = new ProxyBinding(connection, server))
            {
                await bind.Bind();
            }
        }
    }
}
