using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Network.Proxies
{
    public class ProxyBinding
        : IDisposable
    {
        private readonly Socket _left;
        private readonly Socket _right;

        public ProxyBinding(Socket left, Socket right)
        {
            _left = left;
            _right = right;
        }

        public async Task Bind()
        {
            using (var serverStream = new NetworkStream(_left))
            {
                using (var clientStream = new NetworkStream(_right))
                {
                    while (IsConnected(_left) && IsConnected(_right))
                    {
                        if (await Request(clientStream, serverStream) == 0
                            &&
                            await Response(clientStream, serverStream) == 0)
                        {
                            await Task.Delay(50);
                        }
                    }
                }
            }
        }

        private static bool IsConnected(Socket socket)
        {
            try
            {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }

        private static async Task<int> Request(NetworkStream left, NetworkStream right)
        {
            int total = 0;
            if (left.DataAvailable && left.CanRead && right.CanWrite)
            {
                int count;
                byte[] buffer = new byte[32 * 1024];
                while (left.DataAvailable && (count = await left.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await right.WriteAsync(buffer, 0, count);
                    total += count;
                }
            }
            return total;
        }

        private async static Task<int> Response(NetworkStream left, NetworkStream right)
        {
            int total = 0;
            if (right.DataAvailable && right.CanRead && left.CanWrite)
            {
                int count;
                byte[] buffer = new byte[32 * 1024];
                while (right.DataAvailable && (count = await right.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await left.WriteAsync(buffer, 0, count);
                    total += count;
                }
            }
            return total;
        }

        public void Dispose()
        {
            if (_left != null!)
            {
                try
                {
                    if (_left.Connected)
                    {
                        _left.Shutdown(SocketShutdown.Both);
                    }
                    _left.Dispose();
                }
                catch { }
            }
            if (_right != null!)
            {
                try
                {
                    if (_right.Connected)
                    {
                        _right.Shutdown(SocketShutdown.Both);
                    }
                    _right.Dispose();
                }
                catch { }
            }
        }
    }
}
