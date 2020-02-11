using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Threading;
using ZeroLevel.Network;

namespace ZeroLevel.Services.Applications
{
    public abstract class BaseZeroService
        : IZeroService
    {
        private readonly ZeroServiceInfo _serviceInfo = new ZeroServiceInfo();
        private readonly IExchange _exhange;
        protected IExchange Exchange => _exhange;
        public ZeroServiceInfo ServiceInfo => _serviceInfo;

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
            _exhange = new Exchange(this);
        }

        protected BaseZeroService(string name)
        {
            Name = name;
            _exhange = new Exchange(this);
        }

        protected abstract void StartAction();
        protected abstract void StopAction();

        #region Config
        private const string DEFAULT_GROUP_NAME = "__default_group__";
        private const string DEFAULT_TYPE_NAME = "__default_type__";

        public void ReadServiceInfo()
        {
            if (string.IsNullOrWhiteSpace(this.Name))
                this.Name = ReadName();
            if (string.IsNullOrWhiteSpace(this.Key))
                this.Key = ReadKey();
            if (string.IsNullOrWhiteSpace(this.Version))
                this.Version = ReadVersion();
            if (string.IsNullOrWhiteSpace(this.Group))
                this.Group = ReadServiceGroup();
            if (string.IsNullOrWhiteSpace(this.Type))
                this.Type = ReadServiceType();
        }

        public void ReadServiceInfo(IConfigurationSet set)
        {
            if (string.IsNullOrWhiteSpace(this.Name))
                this.Name = ReadName(set);
            if (string.IsNullOrWhiteSpace(this.Key))
                this.Key = ReadKey(set);
            if (string.IsNullOrWhiteSpace(this.Version))
                this.Version = ReadVersion(set);
            if (string.IsNullOrWhiteSpace(this.Group))
                this.Group = ReadServiceGroup(set);
            if (string.IsNullOrWhiteSpace(this.Type))
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

        public bool UseDiscovery()
        {
            if (_state == ZeroServiceStatus.Running
               || _state == ZeroServiceStatus.Initialized)
            {
                ReadServiceInfo();
                return _exhange.UseDiscovery();
            }
            return false;
        }

        public bool UseDiscovery(string endpoint)
        {
            if (_state == ZeroServiceStatus.Running
               || _state == ZeroServiceStatus.Initialized)
            {
                ReadServiceInfo();
                return _exhange.UseDiscovery(endpoint);
            }
            return false;
        }

        public bool UseDiscovery(IPEndPoint endpoint)
        {
            if (_state == ZeroServiceStatus.Running
               || _state == ZeroServiceStatus.Initialized)
            {
                ReadServiceInfo();
                return _exhange.UseDiscovery(endpoint);
            }
            return false;
        }

        public IRouter UseHost()
        {
            if (_state == ZeroServiceStatus.Running
                || _state == ZeroServiceStatus.Initialized)
            {
                return _exhange.UseHost();
            }
            return BaseSocket.NullRouter;
        }

        public IRouter UseHost(int port)
        {
            if (_state == ZeroServiceStatus.Running
                || _state == ZeroServiceStatus.Initialized)
            {
                return _exhange.UseHost(port);
            }
            return BaseSocket.NullRouter;
        }

        public IRouter UseHost(IPEndPoint endpoint)
        {
            if (_state == ZeroServiceStatus.Running
                || _state == ZeroServiceStatus.Initialized)
            {
                return _exhange.UseHost(endpoint);
            }
            return BaseSocket.NullRouter;
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

        #endregion

        #region Service control
        public void Start()
        {
            if (_state == ZeroServiceStatus.Initialized)
            {
                try
                {
                    _state = ZeroServiceStatus.Running;
                    StartAction();
                    Log.Debug($"[{Name}] Service started");
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"[{Name}] Failed to start service");
                    Stop();
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
                Thread.Sleep(1500);
            }
        }

        public void WaitForStatus(ZeroServiceStatus status, TimeSpan period)
        {
            var start = DateTime.UtcNow;
            while (this.Status != status && (DateTime.UtcNow - start) < period)
            {
                Thread.Sleep(1500);
            }
        }

        public void WaitWhileStatus(ZeroServiceStatus status)
        {
            var start = DateTime.UtcNow;
            while (this.Status == status)
            {
                Thread.Sleep(1500);
            }
        }

        public void WaitWhileStatus(ZeroServiceStatus status, TimeSpan period)
        {
            var start = DateTime.UtcNow;
            while (this.Status == status && (DateTime.UtcNow - start) < period)
            {
                Thread.Sleep(1500);
            }
        }
        #endregion

        public void Dispose()
        {
            if (_state != ZeroServiceStatus.Disposed)
            {
                _state = ZeroServiceStatus.Disposed;
                _exhange.Dispose();
            }
        }
    }
}