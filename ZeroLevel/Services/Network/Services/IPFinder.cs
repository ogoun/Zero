using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ZeroLevel.Services.Network
{
    public static class IPFinder
    {
        public static int GetFreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            l = null;
            return port;
        }

        public static IPAddress GetNonLoopbackAddress()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (adapter.Description.IndexOf("VirtualBox", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet)
                        continue;
                    foreach (UnicastIPAddressInformation address in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            if (!IPAddress.IsLoopback(address.Address))
                            {
                                return address.Address;
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
