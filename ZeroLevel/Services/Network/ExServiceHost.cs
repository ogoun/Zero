using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ZeroLevel.Network
{
    public sealed class ExServiceHost
        : IDisposable
    {
        private class MetaService
        {
            public ExServiceInfo ServiceInfo { get; set; }
            public IExService Server { get; set; }
        }

        private bool _disposed = false;
        private readonly long _registerTaskKey = -1;
        private readonly IDiscoveryClient _discoveryClient;

        private readonly ConcurrentDictionary<string, MetaService> _services
            = new ConcurrentDictionary<string, MetaService>();

        public ExServiceHost(IDiscoveryClient client)
        {
            _discoveryClient = client;
            _registerTaskKey = Sheduller.RemindEvery(TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(55), RegisterServicesInDiscovery);
        }

        public IExService RegisterService(IExchangeService service)
        {
            try
            {
                if (_disposed) throw new ObjectDisposedException("ExServiceHost");
                if (service == null) throw new ArgumentNullException(nameof(service));
                ValidateService(service);
                if (_services.ContainsKey(service.Key))
                {
                    throw new Exception($"[ExServiceHost] Service {service.Key} already registered");
                }
                var server = ExchangeTransportFactory.GetServer();
                if (false == _services.TryAdd(service.Key, new MetaService
                {
                    Server = server,
                    ServiceInfo = new ExServiceInfo
                    {
                        Port = server.Endpoint.Port,
                        ServiceKey = service.Key,
                        Version = service.Version,
                        ServiceGroup = service.Group,
                        ServiceType = service.Type
                    }
                }))
                {
                    server.Dispose();
                    return null;
                }

                RegisterServiceInboxes(service);

                return server;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[ExServiceHost] Fault register service");
                return null;
            }
        }

        public IExService RegisterService(ExServiceInfo serviceInfo)
        {
            try
            {
                if (_disposed) throw new ObjectDisposedException("ExServiceHost");
                if (serviceInfo == null) throw new ArgumentNullException(nameof(serviceInfo));
                ValidateService(serviceInfo);

                if (_services.ContainsKey(serviceInfo.ServiceKey))
                {
                    throw new Exception($"[ExServiceHost] Service {serviceInfo.ServiceKey} already registered");
                }

                var server = ExchangeTransportFactory.GetServer();
                if (false == _services.TryAdd(serviceInfo.ServiceKey, new MetaService
                {
                    Server = server,
                    ServiceInfo = new ExServiceInfo
                    {
                        Port = server.Endpoint.Port,
                        ServiceKey = serviceInfo.ServiceKey,
                        Version = serviceInfo.Version,
                        ServiceGroup = serviceInfo.ServiceGroup,
                        ServiceType = serviceInfo.ServiceType
                    }
                }))
                {
                    server.Dispose();
                    return null;
                }
                return server;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[ExServiceHost] Fault register service");
                return null;
            }
        }

        #region Private methods

        private void ValidateService(IExchangeService service)
        {
            if (string.IsNullOrWhiteSpace(service.Key))
            {
                throw new ArgumentNullException("Service.Key");
            }
        }

        private void ValidateService(ExServiceInfo service)
        {
            if (string.IsNullOrWhiteSpace(service.ServiceKey))
            {
                throw new ArgumentNullException("ServiceKey");
            }
        }

        private void RegisterServiceInboxes(IExchangeService service)
        {
            MethodInfo[] methods = service.
                GetType().
                GetMethods(BindingFlags.NonPublic | BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.FlattenHierarchy |
                BindingFlags.Instance);

            var registerHandler = this.GetType().GetMethod("RegisterHandler");
            var registerReplier = this.GetType().GetMethod("RegisterReplier");
            var registerReplierWithNoRequestBody = this.GetType().GetMethod("RegisterReplierWithNoRequestBody");

            foreach (MethodInfo mi in methods)
            {
                try
                {
                    foreach (Attribute attr in Attribute.GetCustomAttributes(mi, typeof(ExchangeAttribute)))
                    {
                        if (attr.GetType() == typeof(ExchangeMainHandlerAttribute))
                        {
                            var firstArgType = mi.GetParameters().First().ParameterType;
                            MethodInfo genericMethod = registerHandler.MakeGenericMethod(firstArgType);
                            genericMethod.Invoke(this, new object[] { ZBaseNetwork.DEFAULT_MESSAGE_INBOX, CreateDelegate(mi, service) });
                        }
                        else if (attr.GetType() == typeof(ExchangeHandlerAttribute))
                        {
                            var firstArgType = mi.GetParameters().First().ParameterType;
                            MethodInfo genericMethod = registerHandler.MakeGenericMethod(firstArgType);
                            genericMethod.Invoke(this, new object[] { (attr as ExchangeHandlerAttribute).Inbox, CreateDelegate(mi, service) });
                        }
                        else if (attr.GetType() == typeof(ExchangeMainReplierAttribute))
                        {
                            var returnType = mi.ReturnType;
                            var firstArgType = mi.GetParameters().First().ParameterType;
                            MethodInfo genericMethod = registerReplier.MakeGenericMethod(firstArgType, returnType);
                            genericMethod.Invoke(this, new object[] { ZBaseNetwork.DEFAULT_REQUEST_INBOX, CreateDelegate(mi, service) });
                        }
                        else if (attr.GetType() == typeof(ExchangeReplierAttribute))
                        {
                            var returnType = mi.ReturnType;
                            var firstArgType = mi.GetParameters().First().ParameterType;
                            MethodInfo genericMethod = registerReplier.MakeGenericMethod(firstArgType, returnType);
                            genericMethod.Invoke(this, new object[] { (attr as ExchangeReplierAttribute).Inbox, CreateDelegate(mi, service) });
                        }
                        else if (attr.GetType() == typeof(ExchangeMainReplierWithoutArgAttribute))
                        {
                            var returnType = mi.ReturnType;
                            var firstArgType = mi.GetParameters().First().ParameterType;
                            MethodInfo genericMethod = registerReplierWithNoRequestBody.MakeGenericMethod(returnType);
                            genericMethod.Invoke(this, new object[] { ZBaseNetwork.DEFAULT_REQUEST_INBOX, CreateDelegate(mi, service) });
                        }
                        else if (attr.GetType() == typeof(ExchangeReplierWithoutArgAttribute))
                        {
                            var returnType = mi.ReturnType;
                            var firstArgType = mi.GetParameters().First().ParameterType;
                            MethodInfo genericMethod = registerReplierWithNoRequestBody.MakeGenericMethod(returnType);
                            genericMethod.Invoke(this, new object[] { (attr as ExchangeReplierAttribute).Inbox, CreateDelegate(mi, service) });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug($"[ZExchange] Can't register method {mi.Name} as inbox handler. {ex}");
                }
            }
        }

        private void RegisterServicesInDiscovery()
        {
            var services = _services.
                Values.
                Select(s => s.ServiceInfo).
                ToList();
            foreach (var service in services)
            {
                _discoveryClient.Register(service);
            }
        }

        #endregion Private methods

        #region Utils

        private static Delegate CreateDelegate(MethodInfo methodInfo, object target)
        {
            Func<Type[], Type> getType;
            var isAction = methodInfo.ReturnType.Equals((typeof(void)));
            var types = methodInfo.GetParameters().Select(p => p.ParameterType);
            if (isAction)
            {
                getType = Expression.GetActionType;
            }
            else
            {
                getType = Expression.GetFuncType;
                types = types.Concat(new[] { methodInfo.ReturnType });
            }
            if (methodInfo.IsStatic)
            {
                return Delegate.CreateDelegate(getType(types.ToArray()), methodInfo);
            }
            return Delegate.CreateDelegate(getType(types.ToArray()), target, methodInfo.Name);
        }

        #endregion Utils

        #region Inboxes

        /// <summary>
        /// Registering an Inbox Handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="protocol">Protocol</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="handler">Handler</param>
        private void RegisterHandler<T>(MetaService meta, string inbox, Action<T, long, IZBackward> handler)
        {
            if (_disposed) return;
            try
            {
                meta.Server.RegisterInbox(inbox, handler);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Register inbox handler error. Inbox '{inbox}'. Service '{meta.ServiceInfo.ServiceKey}'");
            }
        }

        /// <summary>
        /// Registration method responding to an incoming request
        /// </summary>
        /// <typeparam name="Treq">Request message type</typeparam>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="protocol">Protocol</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="replier">Handler</param>
        private void RegisterReplier<Treq, Tresp>(MetaService meta, string inbox, Func<Treq, long, IZBackward, Tresp> handler)
        {
            if (_disposed) return;
            try
            {
                meta.Server.RegisterInbox(inbox, handler);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Register inbox replier error. Inbox '{inbox}'. Service '{meta.ServiceInfo.ServiceKey}'");
            }
        }

        /// <summary>
        /// Registration of the method of responding to the incoming request, not receiving incoming data
        /// </summary>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="protocol">Protocol</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="replier">Handler</param>
        private void RegisterReplierWithNoRequestBody<Tresp>(MetaService meta, string inbox, Func<long, IZBackward, Tresp> handler)
        {
            if (_disposed) return;
            try
            {
                meta.Server.RegisterInbox(inbox, handler);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Register inbox replier error. Inbox '{inbox}'. Service '{meta.ServiceInfo.ServiceKey}'");
            }
        }

        #endregion Inboxes

        #region Transport helpers

        /// <summary>
        /// Call service with round-robin balancing
        /// </summary>
        /// <param name="serviceKey">Service key</param>
        /// <param name="callHandler">Service call code</param>
        /// <returns>true - service called succesfully</returns>
        internal bool CallService(string serviceKey, Func<string, IExClient, bool> callHandler)
        {
            if (_disposed) return false;
            List<ServiceEndpointInfo> candidates;
            try
            {
                candidates = _discoveryClient.GetServiceEndpoints(serviceKey)?.ToList();
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[ExServiceHost] Error when trying get endpoints for service key '{serviceKey}'");
                return false;
            }
            if (candidates == null || candidates.Any() == false)
            {
                Log.Debug($"[ExServiceHost] Not found endpoints for service key '{serviceKey}'");
                return false;
            }
            var success = false;
            foreach (var service in candidates)
            {
                IExClient transport;
                try
                {
                    transport = ExchangeTransportFactory.GetClientWithCache(service.Endpoint);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[ExServiceHost] Can't get transport for service '{serviceKey}'");
                    continue;
                }
                try
                {
                    success = callHandler(service.Endpoint, transport);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[ExServiceHost] Error send/request data in service '{serviceKey}'. Endpoint '{service.Endpoint}'");
                    success = false;
                }
                if (success)
                {
                    break;
                }
            }
            return success;
        }

        internal bool CallServiceDirect(string endpoint, string serviceKey, Func<IExClient, bool> callHandler)
        {
            if (_disposed) return false;
            ServiceEndpointInfo candidate = null;
            try
            {
                candidate = _discoveryClient.GetService(serviceKey, endpoint);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[ExServiceHost] Error when trying get service info by key '{serviceKey}' and endpoint '{endpoint}'");
                return false;
            }
            if (candidate == null)
            {
                Log.Debug($"[ExServiceHost] Not found service info for key '{serviceKey}' and endpoint '{endpoint}'");
                return false;
            }
            IExClient transport;
            try
            {
                transport = ExchangeTransportFactory.GetClientWithCache(candidate.Endpoint);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[ExServiceHost] Can't get transport for service '{serviceKey}'");
                return false;
            }
            return callHandler(transport);
        }

        internal IEnumerable<IExClient> GetClientEnumerator(string serviceKey)
        {
            if (!_disposed)
            {
                List<ServiceEndpointInfo> candidates;
                try
                {
                    candidates = _discoveryClient.GetServiceEndpoints(serviceKey)?.ToList();
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Exchange] Error when trying get endpoints for service key '{serviceKey}'");
                    candidates = null;
                }
                if (candidates != null && candidates.Any())
                {
                    foreach (var service in candidates)
                    {
                        IExClient transport;
                        try
                        {
                            transport = ExchangeTransportFactory.GetClientWithCache(service.Endpoint);
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[Exchange] Can't get transport for endpoint '{service.Endpoint}'");
                            continue;
                        }
                        yield return transport;
                    }
                }
                else
                {
                    Log.Debug($"[Exchange] Not found endpoints for service key '{serviceKey}'");
                }
            }
        }

        internal IEnumerable<IExClient> GetClientEnumeratorByType(string serviceType)
        {
            if (!_disposed)
            {
                List<ServiceEndpointInfo> candidates;
                try
                {
                    candidates = _discoveryClient.GetServiceEndpointsByType(serviceType)?.ToList();
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Exchange] Error when trying get endpoints for service type '{serviceType}'");
                    candidates = null;
                }
                if (candidates != null && candidates.Any())
                {
                    foreach (var service in candidates)
                    {
                        IExClient transport;
                        try
                        {
                            transport = ExchangeTransportFactory.GetClientWithCache(service.Endpoint);
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[Exchange] Can't get transport for endpoint '{service.Endpoint}'");
                            continue;
                        }
                        yield return transport;
                    }
                }
                else
                {
                    Log.Debug($"[Exchange] Not found endpoints for service type '{serviceType}'");
                }
            }
        }

        internal IEnumerable<IExClient> GetClientEnumeratorByGroup(string serviceGroup)
        {
            if (!_disposed)
            {
                List<ServiceEndpointInfo> candidates;
                try
                {
                    candidates = _discoveryClient.GetServiceEndpointsByGroup(serviceGroup)?.ToList();
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Exchange] Error when trying get endpoints for service group '{serviceGroup}'");
                    candidates = null;
                }
                if (candidates != null && candidates.Any())
                {
                    foreach (var service in candidates)
                    {
                        IExClient transport;
                        try
                        {
                            transport = ExchangeTransportFactory.GetClientWithCache(service.Endpoint);
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[Exchange] Can't get transport for endpoint '{service.Endpoint}'");
                            continue;
                        }
                        yield return transport;
                    }
                }
                else
                {
                    Log.Debug($"[Exchange] Not found endpoints for service group '{serviceGroup}'");
                }
            }
        }

        #endregion Transport helpers

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Sheduller.Remove(_registerTaskKey);
            foreach (var s in _services)
            {
                s.Value.Server.Dispose();
            }
        }
    }
}