using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using ZeroLevel.Network;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Applications
{
    public abstract class BaseZeroService
        : IZeroService
    {
        private readonly ZeroServiceInfo _serviceInfo = new ZeroServiceInfo();

        public string Name { get { return _serviceInfo.Name; } private set { _serviceInfo.Name = value; } }
        public string Key { get { return _serviceInfo.ServiceKey; } private set { _serviceInfo.ServiceKey = value; } }
        public string Version { get { return _serviceInfo.Version; } private set { _serviceInfo.Version = value; } }
        public string Group { get { return _serviceInfo.ServiceGroup; } private set { _serviceInfo.ServiceGroup = value; } }
        public string Type { get { return _serviceInfo.ServiceType; } private set { _serviceInfo.ServiceType = value; } }

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
        private static readonly IRouter _null_router = new NullRouter();
        private IDiscoveryClient _discoveryClient = null; // Feature расширить до нескольких discovery
        private long _update_discovery_table_task = -1;
        private long _register_in_discovery_table_task = -1;
        private readonly AliasSet<IPEndPoint> _aliases = new AliasSet<IPEndPoint>();
        private static TimeSpan _update_discovery_table_period = TimeSpan.FromSeconds(15);
        private static TimeSpan _register_in_discovery_table_period = TimeSpan.FromSeconds(15);
        private static readonly ConcurrentDictionary<string, ExClient> _clientInstances = new ConcurrentDictionary<string, ExClient>();
        private readonly ConcurrentDictionary<string, SocketServer> _serverInstances = new ConcurrentDictionary<string, SocketServer>();

        private void RestartDiscoveryTasks()
        {
            if (_update_discovery_table_task != -1)
            {
                Sheduller.Remove(_update_discovery_table_task);
            }
            if (_register_in_discovery_table_task != -1)
            {
                Sheduller.Remove(_register_in_discovery_table_task);
            }
            _update_discovery_table_task = Sheduller.RemindEvery(_update_discovery_table_period, RegisterServicesInDiscovery);
            _register_in_discovery_table_task = Sheduller.RemindEvery(_register_in_discovery_table_period, () => { });
        }

        private void RegisterServicesInDiscovery()
        {
            var services = _serverInstances.
                Values.
                Select(s =>
                {
                    var info = MessageSerializer.Copy(this._serviceInfo);
                    info.Port = s.LocalEndpoint.Port;
                    return info;
                }).
                ToList();
            foreach (var service in services)
            {
                _discoveryClient.Register(service);
            }
        }

        public void UseDiscovery()
        {
            if (_state == ZeroServiceStatus.Running
               || _state == ZeroServiceStatus.Initialized)
            {
                if (_discoveryClient != null)
                {
                    _discoveryClient.Dispose();
                    _discoveryClient = null;
                }
                var discovery = Configuration.Default.First("discovery");
                _discoveryClient = new DiscoveryClient(GetClient(NetUtils.CreateIPEndPoint(discovery), _null_router, false));
                RestartDiscoveryTasks();
            }
        }

        public void UseDiscovery(string endpoint)
        {
            if (_state == ZeroServiceStatus.Running
               || _state == ZeroServiceStatus.Initialized)
            {
                if (_discoveryClient != null)
                {
                    _discoveryClient.Dispose();
                    _discoveryClient = null;
                }
                _discoveryClient = new DiscoveryClient(GetClient(NetUtils.CreateIPEndPoint(endpoint), _null_router, false));
                RestartDiscoveryTasks();
            }
        }

        public void UseDiscovery(IPEndPoint endpoint)
        {
            if (_state == ZeroServiceStatus.Running
               || _state == ZeroServiceStatus.Initialized)
            {
                if (_discoveryClient != null)
                {
                    _discoveryClient.Dispose();
                    _discoveryClient = null;
                }
                _discoveryClient = new DiscoveryClient(GetClient(endpoint, _null_router, false));
                RestartDiscoveryTasks();
            }
        }

        public IRouter UseHost()
        {
            if (_state == ZeroServiceStatus.Running
                || _state == ZeroServiceStatus.Initialized)
            {
                return GetServer(new IPEndPoint(IPAddress.Any, NetUtils.GetFreeTcpPort()), new Router()).Router;
            }
            return _null_router;
        }

        public IRouter UseHost(int port)
        {
            if (_state == ZeroServiceStatus.Running
                || _state == ZeroServiceStatus.Initialized)
            {
                return GetServer(new IPEndPoint(IPAddress.Any, port), new Router()).Router;
            }
            return _null_router;
        }

        public IRouter UseHost(IPEndPoint endpoint)
        {
            if (_state == ZeroServiceStatus.Running
                || _state == ZeroServiceStatus.Initialized)
            {
                return GetServer(endpoint, new Router()).Router;
            }
            return _null_router;
        }

        public ExClient ConnectToService(string endpoint)
        {
            if (_state == ZeroServiceStatus.Running
                || _state == ZeroServiceStatus.Initialized)
            {
                return GetClient(NetUtils.CreateIPEndPoint(endpoint), new Router(), true);
            }
            return null;
        }

        public ExClient ConnectToService(IPEndPoint endpoint)
        {
            if (_state == ZeroServiceStatus.Running
                || _state == ZeroServiceStatus.Initialized)
            {
                return GetClient(endpoint, new Router(), true);
            }
            return null;
        }

        #region Autoregistration inboxes
        private static Delegate CreateDelegate(Type delegateType, MethodInfo methodInfo, object target)
        {
            Func<Type[], Type> getType;
            var isAction = methodInfo.ReturnType.Equals((typeof(void)));
            if (isAction)
            {
                getType = Expression.GetActionType;
            }
            else
            {
                getType = Expression.GetFuncType;
            }
            if (methodInfo.IsStatic)
            {
                return Delegate.CreateDelegate(delegateType, methodInfo);
            }
            return Delegate.CreateDelegate(delegateType, target, methodInfo.Name);
        }

        public void AutoregisterInboxes(IServer server)
        {
            var type = server.GetType();
            // Search router registerinbox methods with inbox name
            var register_methods = type.GetMethods(BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.FlattenHierarchy)?
                .Where(mi => mi.Name.Equals("RegisterInbox", StringComparison.Ordinal) &&
                mi.GetParameters().First().ParameterType == typeof(string));

            var register_message_handler = register_methods.First(mi => mi.IsGenericMethod == false);
            var register_message_handler_with_msg = register_methods.First(mi =>
            {
                if (mi.IsGenericMethod)
                {
                    var paremeters = mi.GetParameters().ToArray();
                    if (paremeters.Length == 2 && paremeters[1].ParameterType.IsAssignableToGenericType(typeof(MessageHandler<>)))
                    {
                        return true;
                    }
                }
                return false;
            });
            var register_request_handler_without_msg = register_methods.First(mi =>
            {
                if (mi.IsGenericMethod)
                {
                    var paremeters = mi.GetParameters().ToArray();
                    if (paremeters.Length == 2 && paremeters[1].ParameterType.IsAssignableToGenericType(typeof(RequestHandler<>)))
                    {
                        return true;
                    }
                }
                return false;
            });
            var register_request_handler = register_methods.First(mi =>
            {
                if (mi.IsGenericMethod)
                {
                    var paremeters = mi.GetParameters().ToArray();
                    if (paremeters.Length == 2 && paremeters[1].ParameterType.IsAssignableToGenericType(typeof(RequestHandler<,>)))
                    {
                        return true;
                    }
                }
                return false;
            });

            MethodInfo[] methods = this.
                GetType().
                GetMethods(BindingFlags.NonPublic
                | BindingFlags.Public
                | BindingFlags.Instance
                | BindingFlags.FlattenHierarchy
                | BindingFlags.Instance);

            foreach (MethodInfo mi in methods)
            {
                try
                {
                    foreach (Attribute attr in Attribute.GetCustomAttributes(mi, typeof(ExchangeAttribute)))
                    {
                        var args = mi.GetParameters().ToArray();
                        if (attr.GetType() == typeof(ExchangeMainHandlerAttribute))
                        {
                            if (args.Length == 1)
                            {
                                var handler = CreateDelegate(typeof(MessageHandler), mi, this);
                                register_message_handler.Invoke(server, new object[] { BaseSocket.DEFAULT_MESSAGE_INBOX, handler });
                            }
                            else
                            {
                                var handler = CreateDelegate(typeof(MessageHandler<>).MakeGenericType(args[1].ParameterType), mi, this);
                                MethodInfo genericMethod = register_message_handler_with_msg.MakeGenericMethod(args[1].ParameterType);
                                genericMethod.Invoke(server, new object[] { BaseSocket.DEFAULT_MESSAGE_INBOX, handler });
                            }
                        }
                        else if (attr.GetType() == typeof(ExchangeHandlerAttribute))
                        {
                            if (args.Length == 1)
                            {
                                var handler = CreateDelegate(typeof(MessageHandler), mi, this);
                                register_message_handler.Invoke(server, new object[] { (attr as ExchangeHandlerAttribute).Inbox, handler });
                            }
                            else
                            {
                                var handler = CreateDelegate(typeof(MessageHandler<>).MakeGenericType(args[1].ParameterType), mi, this);
                                MethodInfo genericMethod = register_message_handler_with_msg.MakeGenericMethod(args[1].ParameterType);
                                genericMethod.Invoke(server, new object[] { (attr as ExchangeHandlerAttribute).Inbox, handler });
                            }
                        }

                        else if (attr.GetType() == typeof(ExchangeMainReplierAttribute))
                        {
                            var returnType = mi.ReturnType;
                            var genArgType = args[1].ParameterType;
                            MethodInfo genericMethod = register_request_handler.MakeGenericMethod(genArgType, returnType);
                            var requestHandler = CreateDelegate(typeof(RequestHandler<,>).MakeGenericType(args[1].ParameterType, returnType), mi, this);
                            genericMethod.Invoke(server, new object[] { BaseSocket.DEFAULT_REQUEST_INBOX, requestHandler });
                        }
                        else if (attr.GetType() == typeof(ExchangeReplierAttribute))
                        {
                            var returnType = mi.ReturnType;
                            var genArgType = args[1].ParameterType;
                            MethodInfo genericMethod = register_request_handler.MakeGenericMethod(genArgType, returnType);
                            var requestHandler = CreateDelegate(typeof(RequestHandler<,>).MakeGenericType(args[1].ParameterType, returnType), mi, this);
                            genericMethod.Invoke(server, new object[] { (attr as ExchangeReplierAttribute).Inbox, requestHandler });
                        }

                        else if (attr.GetType() == typeof(ExchangeMainReplierWithoutArgAttribute))
                        {
                            var returnType = mi.ReturnType;
                            MethodInfo genericMethod = register_request_handler_without_msg.MakeGenericMethod(returnType);
                            var requestHandler = CreateDelegate(typeof(RequestHandler<>).MakeGenericType(returnType), mi, this);
                            genericMethod.Invoke(server, new object[] { BaseSocket.DEFAULT_REQUEST_WITHOUT_ARGS_INBOX, requestHandler });
                        }
                        else if (attr.GetType() == typeof(ExchangeReplierWithoutArgAttribute))
                        {
                            var returnType = mi.ReturnType;
                            MethodInfo genericMethod = register_request_handler_without_msg.MakeGenericMethod(returnType);
                            var requestHandler = CreateDelegate(typeof(RequestHandler<>).MakeGenericType(returnType), mi, this);
                            genericMethod.Invoke(server, new object[] { (attr as ExchangeReplierWithoutArgAttribute).Inbox, requestHandler });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"[ZExchange] Can't register method {mi.Name} as inbox handler. {ex}");
                }
            }
        }
        #endregion


        public void StoreConnection(string endpoint)
        {
            if (_state == ZeroServiceStatus.Running ||
                _state == ZeroServiceStatus.Initialized)
            {
                _aliases.Set(endpoint, NetUtils.CreateIPEndPoint(endpoint));
            }
        }
        public void StoreConnection(string alias, string endpoint)
        {
            if (_state == ZeroServiceStatus.Running ||
                _state == ZeroServiceStatus.Initialized)
            {
                _aliases.Set(alias, NetUtils.CreateIPEndPoint(endpoint));
            }
        }
        public void StoreConnection(IPEndPoint endpoint)
        {
            if (_state == ZeroServiceStatus.Running ||
                _state == ZeroServiceStatus.Initialized)
            {
                _aliases.Set($"{endpoint.Address}:{endpoint.Port}", endpoint);
            }
        }
        public void StoreConnection(string alias, IPEndPoint endpoint)
        {
            if (_state == ZeroServiceStatus.Running ||
                _state == ZeroServiceStatus.Initialized)
            {
                _aliases.Set(alias, endpoint);
            }
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

        private ExClient GetClient(IPEndPoint endpoint, IRouter router, bool use_cachee)
        {
            if (use_cachee)
            {
                string key = $"{endpoint.Address}:{endpoint.Port}";
                ExClient instance = null;
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
                instance = new ExClient(new SocketClient(endpoint, router));
                _clientInstances[key] = instance;
                return instance;
            }
            return new ExClient(new SocketClient(endpoint, router));
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
            if (_state != ZeroServiceStatus.Disposed)
            {
                _state = ZeroServiceStatus.Disposed;

                if (_update_discovery_table_task != -1)
                {
                    Sheduller.Remove(_update_discovery_table_task);
                }

                if (_register_in_discovery_table_task != -1)
                {
                    Sheduller.Remove(_register_in_discovery_table_task);
                }

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
}