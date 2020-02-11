using System;
using System.Net;
using ZeroLevel.Network;

namespace ZeroLevel
{
    public interface IZeroService
        : IDisposable
    {
        string Name { get; }
        string Key { get; }
        string Version { get; }
        string Group { get; }
        string Type { get; }

        ZeroServiceInfo ServiceInfo { get; }

        bool UseDiscovery();
        bool UseDiscovery(string url);
        bool UseDiscovery(IPEndPoint endpoint);
        
        IRouter UseHost();
        IRouter UseHost(int port);
        IRouter UseHost(IPEndPoint endpoint);

        void ReadServiceInfo();
        void ReadServiceInfo(IConfigurationSet config);

        void WaitForStatus(ZeroServiceStatus status);
        void WaitForStatus(ZeroServiceStatus status, TimeSpan period);
        void WaitWhileStatus(ZeroServiceStatus status);
        void WaitWhileStatus(ZeroServiceStatus status, TimeSpan period);

        ZeroServiceStatus Status { get; }

        void Start();

        void Stop();
    }
}