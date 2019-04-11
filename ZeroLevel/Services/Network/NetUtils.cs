using System;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ZeroLevel.Network
{
    public static class NetUtils
    {
        public static bool TestConnection(IPEndPoint endpoint)
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, false);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoChecksum, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 100);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 100);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, false);
                try
                {
                    socket.Connect(endpoint);
                    socket.Close();
                    return true;
                }
                catch
                {
                }
            }
            return false;
        }

        public static int Compare(this IPEndPoint x, IPEndPoint y)
        {
            var xx = x.Address.ToString();
            var yy = y.Address.ToString();
            var result = string.CompareOrdinal(xx, yy);
            return result == 0 ? x.Port.CompareTo(y.Port) : result;
        }

        public static int Compare(this IPAddress x, IPAddress y)
        {
            var xx = x.ToString();
            var yy = y.ToString();
            return string.CompareOrdinal(xx, yy);
        }

        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length < 2) throw new FormatException("Invalid endpoint format");
            IPAddress ip;
            if (ep.Length > 2)
            {
                if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                {
                    throw new FormatException("Invalid ip-adress");
                }
            }
            else
            {
                if (!IPAddress.TryParse(ep[0], out ip))
                {
                    throw new FormatException("Invalid ip-adress");
                }
            }
            int port;
            if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }

        public static int GetFreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            l = null;
            return port;
        }

        public static IPAddress GetNonLoopbackAddress(bool ignore_virtual_devices = true, bool ignore_docker_devices = true)
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (adapter.OperationalStatus == OperationalStatus.Up)
                    {
                        if (ignore_virtual_devices && (adapter.Description.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0 || adapter.Name.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0))
                            continue;
                        if (ignore_docker_devices && (adapter.Description.IndexOf("Docker", StringComparison.OrdinalIgnoreCase) >= 0 || adapter.Name.IndexOf("Docker", StringComparison.OrdinalIgnoreCase) >= 0))
                            continue;
                        if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
                            continue;
                        foreach (UnicastIPAddressInformation address in adapter.GetIPProperties().UnicastAddresses)
                        {
                            if (address.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                if (!IPAddress.IsLoopback(address.Address))
                                {
                                    return address.Address;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}