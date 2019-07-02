using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using ZeroLevel.Network;

namespace ZeroLevel.Services.Applications
{
    public abstract class BaseZeroService
        : IZeroService
    {
        public string Name { get; protected set; }
        public string Key { get; private set; }
        public string Version { get; private set; }
        public string Group { get; private set; }
        public string Type { get; private set; }

        public ZeroServiceStatus Status => _state;
        private ZeroServiceStatus _state;

        protected BaseZeroService()
        {
            Name = GetType().Name;
        }

        protected BaseZeroService(string name)
        {
            Name = name;
        }

        protected abstract void StartAction();
        protected abstract void StopAction();

        #region Config
        private const string DEFAULT_GROUP_NAME = "__default_group__";
        private const string DEFAULT_TYPE_NAME = "__default_type__";

        public void ReadServiceInfo()
        {
            this.Name = ReadName();
            this.Key = ReadKey();
            this.Version = ReadVersion();
            this.Group = ReadServiceGroup();
            this.Type = ReadServiceType();
        }

        public void ReadServiceInfo(IConfigurationSet set)
        {
            this.Name = ReadName(set);
            this.Key = ReadKey(set);
            this.Version = ReadVersion(set);
            this.Group = ReadServiceGroup(set);
            this.Type = ReadServiceType(set);
        }

        private string ReadName(IConfigurationSet set = null)
        {
            return FindInConfig<string>(set, new[] { "ServiceName", "AppName" }, string.Empty, "service")
                ?? this.GetType().Name;
        }

        private string ReadKey(IConfigurationSet set = null)
        {
            return FindInConfig<string>(set, new[] { "ServiceKey", "AppKey" }, string.Empty, "service");
        }

        private string ReadVersion(IConfigurationSet set = null)
        {
            return FindInConfig<string>(set, new[] { "Version", "AppVersion" }, string.Empty, "service")
                ?? "1.0";
        }

        private string ReadServiceGroup(IConfigurationSet set = null)
        {
            return FindInConfig<string>(set, new[] { "DiscoveryGroup", "ServiceGroup" }, string.Empty, "service")
                ?? DEFAULT_GROUP_NAME;
        }

        private string ReadServiceType(IConfigurationSet set = null)
        {
            return FindInConfig<string>(set, new[] { "DiscoveryType", "ServiceType" }, string.Empty, "service")
                ?? DEFAULT_TYPE_NAME;
        }

        protected T FindInConfig<T>(IConfigurationSet set, string[] keys, params string[] sections)
        {
            var configSet = set ?? Configuration.DefaultSet;
            foreach (var section in sections)
            {
                if (string.IsNullOrWhiteSpace(section))
                {
                    foreach (var key in keys)
                    {
                        if (configSet.Default.Contains(key))
                        {
                            return configSet.Default.First<T>(key);
                        }
                    }
                }
                else if (configSet.ContainsSection(section))
                {
                    foreach (var key in keys)
                    {
                        if (configSet[section].Contains(key))
                        {
                            return configSet[section].First<T>(key);
                        }
                    }
                }
            }
            return default(T);
        }
        #endregion Config

        #region Network
        private IRouter _router;
        private static IRouter _null_router = new NullRouter();
        private IDiscoveryClient _discoveryClient;

        public void UseDiscovery()
        {
            var discovery = Configuration.Default.First("discovery");
            _discoveryClient = new DiscoveryClient(GetClient(NetUtils.CreateIPEndPoint(discovery), _null_router, false));
        }

        public void UseDiscovery(string endpoint)
        {
            _discoveryClient = new DiscoveryClient(GetClient(NetUtils.CreateIPEndPoint(endpoint), _null_router, false));
        }

        public void UseDiscovery(IPEndPoint endpoint)
        {
            _discoveryClient = new DiscoveryClient(GetClient(endpoint, _null_router, false));
        }

        public IRouter UseHost()
        {
            if (_state == ZeroServiceStatus.Running)
            {
                return GetServer(new IPEndPoint(IPAddress.Any, NetUtils.GetFreeTcpPort()), new Router()).Router;
            }
            return _null_router;
        }

        public IRouter UseHost(int port)
        {
            if (_state == ZeroServiceStatus.Running)
            {
                return GetServer(new IPEndPoint(IPAddress.Any, port), new Router()).Router;
            }
            return _null_router;
        }

        public IRouter UseHost(IPEndPoint endpoint)
        {
            if (_state == ZeroServiceStatus.Running)
            {
                return GetServer(endpoint, new Router()).Router;
            }
            return _null_router;
        }

        public ExClient ConnectToService(string endpoint)
        {
            if (_state == ZeroServiceStatus.Running)
            {
                return new ExClient(GetClient(NetUtils.CreateIPEndPoint(endpoint), new Router(), true));
            }
            return null;
        }

        public ExClient ConnectToService(IPEndPoint endpoint)
        {
            if (_state == ZeroServiceStatus.Running)
            {
                return new ExClient(GetClient(endpoint, new Router(), true));
            }
            return null;
        }
        #endregion

        #region Service control
        public void Start()
        {
            if (_state == ZeroServiceStatus.Initialized)
            {
                try
                {
                    StartAction();
                    _state = ZeroServiceStatus.Running;
                    Log.Debug($"[{Name}] Service started");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"[{Name}] Failed to start service");
                }
            }
        }

        public void Stop()
        {
            if (_state == ZeroServiceStatus.Running)
            {
                try
                {
                    StopAction();
                    Log.Debug($"[{Name}] Service stopped");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"[{Name}] Failed to stop service");
                }
            }
            _state = ZeroServiceStatus.Stopped;
        }

        public void WaitForStatus(ZeroServiceStatus status)
        {
            var start = DateTime.UtcNow;
            while (this.Status != status)
            {
                Thread.Sleep(150);
            }
        }

        public void WaitForStatus(ZeroServiceStatus status, TimeSpan period)
        {
            var start = DateTime.UtcNow;
            while (this.Status != status && (DateTime.UtcNow - start) < period)
            {
                Thread.Sleep(150);
            }
        }

        public void WaitWhileStatus(ZeroServiceStatus status)
        {
            var start = DateTime.UtcNow;
            while (this.Status == status)
            {
                Thread.Sleep(150);
            }
        }

        public void WaitWhileStatus(ZeroServiceStatus status, TimeSpan period)
        {
            var start = DateTime.UtcNow;
            while (this.Status == status && (DateTime.UtcNow - start) < period)
            {
                Thread.Sleep(150);
            }
        }
        #endregion

        #region Utils
        private static readonly ConcurrentDictionary<string, ISocketClient> _clientInstances = new ConcurrentDictionary<string, ISocketClient>();
        private readonly ConcurrentDictionary<string, SocketServer> _serverInstances = new ConcurrentDictionary<string, SocketServer>();

        private ISocketClient GetClient(IPEndPoint endpoint, IRouter router, bool use_cachee)
        {
            if (use_cachee)
            {
                string key = $"{endpoint.Address}:{endpoint.Port}";
                ISocketClient instance = null;
                if (_clientInstances.ContainsKey(key))
                {
                    instance = _clientInstances[key];
                    if (instance.Status == SocketClientStatus.Working)
                    {
                        return instance;
                    }
                    _clientInstances.TryRemove(key, out instance);
                    instance.Dispose();
                    instance = null;
                }
                instance = new SocketClient(endpoint, router);
                _clientInstances[key] = instance;
                return instance;
            }
            return new SocketClient(endpoint, router);
        }

        private SocketServer GetServer(IPEndPoint endpoint, IRouter router)
        {
            string key = $"{endpoint.Address}:{endpoint.Port}";
            if (_serverInstances.ContainsKey(key))
            {
                return _serverInstances[key];
            }
            var instance = new SocketServer(endpoint, router);
            _serverInstances[key] = instance;
            return instance;
        }

        #endregion

        public void Dispose()
        {
            _state = ZeroServiceStatus.Disposed;

            foreach (var client in _clientInstances)
            {
                try
                {
                    client.Value.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"[BaseZeroService`{Name ?? string.Empty}.Dispose()] Dispose SocketClient to endpoint {client.Key}");
                }
            }

            foreach (var server in _serverInstances)
            {
                try
                {
                    server.Value.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"[BaseZeroService`{Name ?? string.Empty}.Dispose()] Dispose SocketServer with endpoint {server.Key}");
                }
            }
        }
    }
}