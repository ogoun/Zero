using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel.Models;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Network
{
    /// <summary>
    /// Provides data exchange between services
    /// </summary>
    public sealed class Exchange :
        IClientSet,
        IDisposable
    {
        private IDiscoveryClient _discoveryClient = null; // Feature расширить до нескольких discovery        
        private readonly ServiceRouteStorage _aliases = new ServiceRouteStorage();
        private readonly ExClientServerCachee _cachee = new ExClientServerCachee();

        #region Ctor

        public Exchange()
        {
        }

        #endregion Ctor

        #region IMultiClient      

        /// <summary>
        /// Sending a message to the service
        /// </summary>
        /// <param name="alias">Service key or url</param>
        /// <param name="data">Message</param>
        /// <returns></returns>
        public bool Send<T>(string alias, T data)
        {
            return CallService(alias, (transport) => transport.Send<T>(BaseSocket.DEFAULT_MESSAGE_INBOX, data).Success);
        }

        /// <summary>
        /// Sending a message to the service
        /// </summary>
        /// <param name="alias">Service key or url</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Message</param>
        /// <returns></returns>
        public bool Send<T>(string alias, string inbox, T data)
        {
            return CallService(alias, (transport) => transport.Send<T>(inbox, data).Success);
        }

        /// <summary>
        /// Sending a message to all services with the specified key, to the default handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="serviceKey">Service key</param>
        /// <param name="data">Message</param>
        /// <returns>true - on successful submission</returns>
        public bool SendBroadcast<T>(string serviceKey, T data) => SendBroadcast(serviceKey, BaseSocket.DEFAULT_MESSAGE_INBOX, data);

        /// <summary>
        /// Sending a message to all services with the specified key to the specified handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="alias">Service key</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Message</param>
        /// <returns>true - on successful submission</returns>
        public bool SendBroadcast<T>(string alias, string inbox, T data)
        {
            try
            {
                foreach (var client in GetClientEnumerator(alias))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            client.Send(inbox, data);
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[Exchange.SendBroadcast] Error broadcast send data to services '{alias}'. Inbox '{inbox}'");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.SendBroadcast] Error broadcast send data in service '{alias}'. Inbox '{inbox}'");
            }
            return false;
        }

        /// <summary>
        /// Sending a message to all services of a specific type to the specified handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="type">Service type</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Message</param>
        /// <returns>true - on successful submission</returns>
        public bool SendBroadcastByType<T>(string type, string inbox, T data)
        {
            try
            {
                foreach (var client in GetClientEnumeratorByType(type))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            client.Send(inbox, data);
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[Exchange.SendBroadcastByType] Error broadcast send data to services with type '{type}'. Inbox '{inbox}'");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.SendBroadcastByType] Error broadcast send data to services with type '{type}'. Inbox '{inbox}'");
            }
            return false;
        }

        /// <summary>
        /// Sending a message to all services of a particular type, to the default handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="type">Service type</param>
        /// <param name="data">Message</param>
        /// <returns>true - on successful submission</returns>
        public bool SendBroadcastByType<T>(string type, T data) =>
            SendBroadcastByType(type, BaseSocket.DEFAULT_MESSAGE_INBOX, data);

        /// <summary>
        /// Sending a message to all services of a specific group to the specified handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="group">Service group</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Message</param>
        /// <returns>true - on successful submission</returns>
        public bool SendBroadcastByGroup<T>(string group, string inbox, T data)
        {
            try
            {
                foreach (var client in GetClientEnumeratorByGroup(group))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            client.Send(inbox, data);
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[Exchange.SendBroadcastByGroup] Error broadcast send data to services with type '{group}'. Inbox '{inbox}'");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.SendBroadcastByGroup] Error broadcast send data to services with type '{group}'. Inbox '{inbox}'");
            }
            return false;
        }

        /// <summary>
        /// Sending a message to all services of a specific group in the default handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="serviceGroup">Service group</param>
        /// <param name="data">Messsage</param>
        /// <returns>true - on successful submission</returns>
        public bool SendBroadcastByGroup<T>(string serviceGroup, T data) =>
            SendBroadcastByGroup(serviceGroup, BaseSocket.DEFAULT_MESSAGE_INBOX, data);

        public bool Request<Tresponse>(string alias, Action<Tresponse> callback) =>
            Request(alias, BaseSocket.DEFAULT_REQUEST_WITHOUT_ARGS_INBOX, callback);

        public bool Request<Tresponse>(string alias, string inbox, Action<Tresponse> callback)
        {
            bool success = false;
            Tresponse response = default(Tresponse);
            try
            {
                if (false == CallService(alias, (transport) =>
                {
                    try
                    {
                        using (var waiter = new ManualResetEventSlim(false))
                        {
                            if (false == transport.Request<Tresponse>(inbox, resp =>
                            {
                                response = resp;
                                success = true;
                                waiter.Set();
                            }).Success)
                            {
                                return false;
                            }
                            if (false == waiter.Wait(BaseSocket.MAX_REQUEST_TIME_MS))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[Exchange.Request] Error request to service '{alias}'. Inbox '{inbox}'");
                    }
                    return false;
                }))
                {
                    Log.SystemWarning($"[Exchange.Request] No responce on request. Service key '{alias}'. Inbox '{inbox}'");
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.Request] Error request to service '{alias}'. Inbox '{inbox}'");
            }
            callback(response);
            return success;
        }

        public bool Request<Trequest, Tresponse>(string alias, Trequest request, Action<Tresponse> callback)
            => Request(alias, BaseSocket.DEFAULT_REQUEST_INBOX, callback);

        public bool Request<Trequest, Tresponse>(string alias, string inbox, Trequest request, Action<Tresponse> callback)
        {
            bool success = false;
            Tresponse response = default(Tresponse);
            try
            {
                if (false == CallService(alias, (transport) =>
                {
                    try
                    {
                        using (var waiter = new ManualResetEventSlim(false))
                        {
                            if (false == transport.Request<Trequest, Tresponse>(inbox, request, resp =>
                            {
                                response = resp;
                                success = true;
                                waiter.Set();
                            }).Success)
                            {
                                return false;
                            }
                            if (false == waiter.Wait(BaseSocket.MAX_REQUEST_TIME_MS))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[Exchange.Request] Error request to service '{alias}'. Inbox '{inbox}'");
                    }
                    return false;
                }))
                {
                    Log.SystemWarning($"[Exchange.Request] No responce on request. Service key '{alias}'. Inbox '{inbox}'");
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.Request] Error request to service '{alias}'. Inbox '{inbox}'");
            }
            callback(response);
            return success;
        }

        /// <summary>
        /// Broadcast polling of services by key, without message of request, to default handler
        /// </summary>
        /// <typeparam name="Tresponse">Response message type</typeparam>
        /// <param name="alias">Service key</param>
        /// <param name="callback">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public bool RequestBroadcast<Tresponse>(string alias, Action<IEnumerable<Tresponse>> callback) =>
            RequestBroadcast(alias, BaseSocket.DEFAULT_REQUEST_WITHOUT_ARGS_INBOX, callback);

        /// <summary>
        /// Broadcast polling services by key
        /// </summary>
        /// <typeparam name="Tresponse">Response message type</typeparam>
        /// <param name="alias">Service key</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Request message</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public bool RequestBroadcast<Tresponse>(string alias, string inbox, Action<IEnumerable<Tresponse>> callback)
        {
            try
            {
                var clients = GetClientEnumerator(alias).ToList();
                callback(_RequestBroadcast<Tresponse>(clients, inbox));
                return true;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.RequestBroadcast] Error broadcast request to service '{alias}'. Inbox '{inbox}'");
            }
            return false;
        }

        public bool RequestBroadcast<Trequest, Tresponse>(string alias, Trequest data, Action<IEnumerable<Tresponse>> callback)
            => RequestBroadcast(alias, BaseSocket.DEFAULT_REQUEST_INBOX, data, callback);

        public bool RequestBroadcast<Trequest, Tresponse>(string alias, string inbox, Trequest data
            , Action<IEnumerable<Tresponse>> callback)
        {
            try
            {
                var clients = GetClientEnumerator(alias).ToList();
                callback(_RequestBroadcast<Trequest, Tresponse>(clients, inbox, data));
                return true;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.RequestBroadcast] Error broadcast request to service '{alias}'. Inbox '{inbox}'");
            }
            return false;
        }

        public bool RequestBroadcastByGroup<Tresponse>(string serviceGroup, Action<IEnumerable<Tresponse>> callback)
            => RequestBroadcastByGroup(serviceGroup, BaseSocket.DEFAULT_REQUEST_INBOX, callback);

        public bool RequestBroadcastByGroup<Tresponse>(string serviceGroup, string inbox, Action<IEnumerable<Tresponse>> callback)
        {
            try
            {
                var clients = GetClientEnumeratorByGroup(serviceGroup).ToList();
                callback(_RequestBroadcast<Tresponse>(clients, inbox));
                return true;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast request to service by group '{serviceGroup}'. Inbox '{inbox}'");
            }
            return false;
        }

        public bool RequestBroadcastByGroup<Trequest, Tresponse>(string serviceGroup, Trequest data, Action<IEnumerable<Tresponse>> callback)
            => RequestBroadcastByGroup(serviceGroup, BaseSocket.DEFAULT_REQUEST_INBOX, data, callback);

        public bool RequestBroadcastByGroup<Trequest, Tresponse>(string serviceGroup, string inbox, Trequest data
            , Action<IEnumerable<Tresponse>> callback)
        {
            try
            {
                var clients = GetClientEnumeratorByGroup(serviceGroup).ToList();
                callback(_RequestBroadcast<Trequest, Tresponse>(clients, inbox, data));
                return true;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast request to service by group '{serviceGroup}'. Inbox '{inbox}'");
            }
            return false;
        }

        public bool RequestBroadcastByType<Tresponse>(string serviceType, Action<IEnumerable<Tresponse>> callback)
            => RequestBroadcastByType(serviceType, BaseSocket.DEFAULT_REQUEST_WITHOUT_ARGS_INBOX, callback);

        public bool RequestBroadcastByType<Tresponse>(string serviceType, string inbox, Action<IEnumerable<Tresponse>> callback)
        {
            try
            {
                var clients = GetClientEnumeratorByType(serviceType).ToList();
                callback(_RequestBroadcast<Tresponse>(clients, inbox));
                return true;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast request to service by type '{serviceType}'. Inbox '{inbox}'");
            }
            return false;
        }

        /// <summary>
        /// Broadcast polling services by type of service, to default handler
        /// </summary>
        /// <typeparam name="Treq">Request message type</typeparam>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceType">Service type</param>
        /// <param name="data">Request message</param>
        /// <param name="callback">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public bool RequestBroadcastByType<Trequest, Tresponse>(string serviceType, Trequest data
            , Action<IEnumerable<Tresponse>> callback) =>
        RequestBroadcastByType(serviceType, BaseSocket.DEFAULT_REQUEST_INBOX, data, callback);

        /// <summary>
        /// Broadcast polling services by type of service
        /// </summary>
        /// <typeparam name="Treq">Request message type</typeparam>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceType">Service type</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Request message</param>
        /// <param name="callback">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public bool RequestBroadcastByType<Trequest, Tresponse>(string serviceType, string inbox, Trequest data
            , Action<IEnumerable<Tresponse>> callback)
        {
            try
            {
                var clients = GetClientEnumeratorByType(serviceType).ToList();
                callback(_RequestBroadcast<Trequest, Tresponse>(clients, inbox, data));
                return true;
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast request to service by type '{serviceType}'. Inbox '{inbox}'");
            }
            return false;
        }
        #endregion

        #region Discovery
        private long _update_discovery_table_task = -1;
        private long _register_in_discovery_table_task = -1;
        private static TimeSpan _update_discovery_table_period = TimeSpan.FromSeconds(15);
        private static TimeSpan _register_in_discovery_table_period = TimeSpan.FromSeconds(15);

        public void UseDiscovery()
        {
            if (_discoveryClient != null)
            {
                _discoveryClient.Dispose();
                _discoveryClient = null;
            }
            var discovery = Configuration.Default.First("discovery");
            _discoveryClient = new DiscoveryClient(_cachee.GetClient(NetUtils.CreateIPEndPoint(discovery), false, BaseSocket.NullRouter));
            RestartDiscoveryTasks();
        }

        public void UseDiscovery(string endpoint)
        {
            if (_discoveryClient != null)
            {
                _discoveryClient.Dispose();
                _discoveryClient = null;
            }
            _discoveryClient = new DiscoveryClient(_cachee.GetClient(NetUtils.CreateIPEndPoint(endpoint), false, BaseSocket.NullRouter));
            RestartDiscoveryTasks();
        }

        public void UseDiscovery(IPEndPoint endpoint)
        {
            if (_discoveryClient != null)
            {
                _discoveryClient.Dispose();
                _discoveryClient = null;
            }
            _discoveryClient = new DiscoveryClient(_cachee.GetClient(endpoint, false, BaseSocket.NullRouter));
            RestartDiscoveryTasks();
        }

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
            RegisterServicesInDiscovery();
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
        #endregion

        #region Host service
        public IRouter UseHost()
        {
            return _cachee.GetServer(new IPEndPoint(IPAddress.Any, NetUtils.GetFreeTcpPort()), new Router()).Router;
        }

        public IRouter UseHost(int port)
        {
            return _cachee.GetServer(new IPEndPoint(IPAddress.Any, port), new Router()).Router;
        }

        public IRouter UseHost(IPEndPoint endpoint)
        {
            return _cachee.GetServer(endpoint, new Router()).Router;
        }
        #endregion

        #region Private
        internal IEnumerable<ExClient> GetClientEnumerator(string serviceKey)
        {
            InvokeResult<IEnumerable<IPEndPoint>> candidates;
            try
            {
                candidates = _aliases.GetAll(serviceKey);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.GetClientEnumerator] Error when trying get endpoints for service key '{serviceKey}'");
                candidates = null;
            }
            if (candidates != null && candidates.Success && candidates.Value.Any())
            {
                foreach (var endpoint in candidates.Value)
                {
                    ExClient transport;
                    try
                    {
                        transport = _cachee.GetClient(endpoint, true);
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[Exchange.GetClientEnumerator] Can't get transport for endpoint '{endpoint}'");
                        continue;
                    }
                    yield return transport;
                }
            }
            else
            {
                Log.Debug($"[Exchange.GetClientEnumerator] Not found endpoints for service key '{serviceKey}'");
            }
        }

        internal IEnumerable<ExClient> GetClientEnumeratorByType(string serviceType)
        {
            InvokeResult<IEnumerable<IPEndPoint>> candidates;
            try
            {
                candidates = _aliases.GetAllByType(serviceType);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.GetClientEnumeratorByType] Error when trying get endpoints for service type '{serviceType}'");
                candidates = null;
            }
            if (candidates != null && candidates.Success && candidates.Value.Any())
            {
                foreach (var endpoint in candidates.Value)
                {
                    ExClient transport;
                    try
                    {
                        transport = _cachee.GetClient(endpoint, true);
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[Exchange.GetClientEnumeratorByType] Can't get transport for endpoint '{endpoint}'");
                        continue;
                    }
                    yield return transport;
                }
            }
            else
            {
                Log.Debug($"[Exchange.GetClientEnumeratorByType] Not found endpoints for service type '{serviceType}'");
            }
        }

        internal IEnumerable<ExClient> GetClientEnumeratorByGroup(string serviceGroup)
        {
            InvokeResult<IEnumerable<IPEndPoint>> candidates;
            try
            {
                candidates = _aliases.GetAllByGroup(serviceGroup);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.GetClientEnumeratorByGroup] Error when trying get endpoints for service group '{serviceGroup}'");
                candidates = null;
            }
            if (candidates != null && candidates.Success && candidates.Value.Any())
            {
                foreach (var service in candidates.Value)
                {
                    ExClient transport;
                    try
                    {
                        transport = _cachee.GetClient(service, true);
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[Exchange.GetClientEnumeratorByGroup] Can't get transport for endpoint '{service}'");
                        continue;
                    }
                    yield return transport;
                }
            }
            else
            {
                Log.Debug($"[Exchange.GetClientEnumeratorByGroup] Not found endpoints for service group '{serviceGroup}'");
            }
        }
        /// <summary>
        /// Call service with round-robin balancing
        /// </summary>
        /// <param name="serviceKey">Service key</param>
        /// <param name="callHandler">Service call code</param>
        /// <returns>true - service called succesfully</returns>
        internal bool CallService(string serviceKey, Func<ExClient, bool> callHandler)
        {
            InvokeResult<IEnumerable<IPEndPoint>> candidates;
            try
            {
                candidates = _aliases.GetAll(serviceKey);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.CallService] Error when trying get endpoints for service key '{serviceKey}'");
                return false;
            }
            if (candidates == null || !candidates.Success || candidates.Value.Any() == false)
            {
                Log.Debug($"[Exchange.CallService] Not found endpoints for service key '{serviceKey}'");
                return false;
            }
            var success = false;
            foreach (var endpoint in candidates.Value)
            {
                ExClient transport;
                try
                {
                    transport = _cachee.GetClient(endpoint, true);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Exchange.CallService] Can't get transport for service '{serviceKey}'");
                    continue;
                }
                try
                {
                    success = callHandler(transport);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Exchange.CallService] Error send/request data in service '{serviceKey}'. Endpoint '{endpoint}'");
                    success = false;
                }
                if (success)
                {
                    break;
                }
            }
            return success;
        }

        internal InvokeResult CallServiceDirect(string endpoint, Func<ExClient, InvokeResult> callHandler)
        {
            ExClient transport;
            try
            {
                transport = _cachee.GetClient(NetUtils.CreateIPEndPoint(endpoint), true);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.CallServiceDirect] Can't get transport for endpoint '{endpoint}'");
                return InvokeResult.Fault(ex.Message);
            }
            return callHandler(transport);
        }

        private IEnumerable<Tresp> _RequestBroadcast<Treq, Tresp>(List<ExClient> clients, string inbox, Treq data)
        {
            var response = new List<Tresp>();
            using (var waiter = new CountdownEvent(clients.Count))
            {
                foreach (var client in clients)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            if (false == client.Request<Treq, Tresp>(inbox, data, resp => { waiter.Signal(); response.Add(resp); }).Success)
                            {
                                waiter.Signal();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[ExClientSet._RequestBroadcast] Error direct request to service '{client.EndPoint}' in broadcast request. Inbox '{inbox}'");
                            waiter.Signal();
                        }
                    });
                }
                waiter.Wait(BaseSocket.MAX_REQUEST_TIME_MS);
            }
            return response;
        }

        private IEnumerable<Tresp> _RequestBroadcast<Tresp>(List<ExClient> clients, string inbox)
        {
            var response = new List<Tresp>();
            using (var waiter = new CountdownEvent(clients.Count))
            {
                foreach (var client in clients)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            if (false == client.Request<Tresp>(inbox, resp => { waiter.Signal(); response.Add(resp); }).Success)
                            {
                                waiter.Signal();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[ExClientSet._RequestBroadcast] Error direct request to service '{client.EndPoint}' in broadcast request. Inbox '{inbox}'");
                            waiter.Signal();
                        }
                    });
                }
                waiter.Wait(BaseSocket.MAX_REQUEST_TIME_MS);
            }
            return response;
        }
        #endregion

        public void Dispose()
        {
            if (_update_discovery_table_task != -1)
            {
                Sheduller.Remove(_update_discovery_table_task);
            }
            if (_register_in_discovery_table_task != -1)
            {
                Sheduller.Remove(_register_in_discovery_table_task);
            }
            _cachee.Dispose();
        }
    }
}