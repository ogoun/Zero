using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel.Models;
using ZeroLevel.Network.SDL;

namespace ZeroLevel.Network
{
    /// <summary>
    /// Provides data exchange between services
    /// </summary>
    internal sealed class Exchange :
        IExchange
    {
        private readonly ServiceRouteStorage _dicovery_aliases = new ServiceRouteStorage();
        private readonly ServiceRouteStorage _user_aliases = new ServiceRouteStorage();
        private readonly ExClientServerCachee _cachee = new ExClientServerCachee();

        public IServiceRoutesStorage RoutesStorage => _user_aliases;
        public IServiceRoutesStorage DiscoveryStorage => _dicovery_aliases;
        private readonly IZeroService _owner;

        #region Ctor        

        public Exchange(IZeroService owner)
        {
            _owner = owner;
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
                if (clients.Count > 0)
                {
                    callback(_RequestBroadcast<Tresponse>(clients, inbox));
                    return true;
                }
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
                if (clients.Count > 0)
                {
                    callback(_RequestBroadcast<Trequest, Tresponse>(clients, inbox, data));
                    return true;
                }
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
                if (clients.Count > 0)
                {
                    callback(_RequestBroadcast<Tresponse>(clients, inbox));
                    return true;
                }
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
                if (clients.Count > 0)
                {
                    callback(_RequestBroadcast<Trequest, Tresponse>(clients, inbox, data));
                    return true;
                }
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
                if (clients.Count > 0)
                {
                    callback(_RequestBroadcast<Tresponse>(clients, inbox));
                    return true;
                }
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
                if (clients.Count > 0)
                {
                    callback(_RequestBroadcast<Trequest, Tresponse>(clients, inbox, data));
                    return true;
                }
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
            try
            {
                var discoveryEndpoint = Configuration.Default.First("discovery");
                _user_aliases.Set(BaseSocket.DISCOVERY_ALIAS, NetUtils.CreateIPEndPoint(discoveryEndpoint));
                RestartDiscoveryTasks();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Exchange.UseDiscovery]");
            }
        }

        public void UseDiscovery(string discoveryEndpoint)
        {
            try
            {
                _user_aliases.Set(BaseSocket.DISCOVERY_ALIAS, NetUtils.CreateIPEndPoint(discoveryEndpoint));
                RestartDiscoveryTasks();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Exchange.UseDiscovery]");
            }
        }

        public void UseDiscovery(IPEndPoint discoveryEndpoint)
        {
            try
            {
                _user_aliases.Set(BaseSocket.DISCOVERY_ALIAS, discoveryEndpoint);
                RestartDiscoveryTasks();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[Exchange.UseDiscovery]");
            }
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
            UpdateServiceListFromDiscovery();
            _register_in_discovery_table_task = Sheduller.RemindEvery(TimeSpan.FromMilliseconds(500), _update_discovery_table_period, RegisterServicesInDiscovery);
            _update_discovery_table_task = Sheduller.RemindEvery(TimeSpan.FromMilliseconds(750), _register_in_discovery_table_period, UpdateServiceListFromDiscovery);
        }

        private void RegisterServicesInDiscovery()
        {
            var discovery_endpoint = _user_aliases.Get(BaseSocket.DISCOVERY_ALIAS);
            if (discovery_endpoint.Success)
            {
                var discoveryClient = _cachee.GetClient(discovery_endpoint.Value, true);
                if (discoveryClient != null)
                {
                    foreach (var service in _cachee.ServerList)
                    {
                        var request = discoveryClient.Request<ServiceRegisterInfo, InvokeResult>("register"
                            , new ServiceRegisterInfo
                            {
                                Port = service.LocalEndpoint.Port,
                                ServiceInfo = _owner.ServiceInfo
                            }
                            , r =>
                            {
                                if (!r.Success)
                                {
                                    Log.SystemWarning($"[Exchange.RegisterServicesInDiscovery] Register canceled. {r.Comment}");
                                }
                            });
                        if (request.Success == false)
                        {
                            Log.SystemWarning($"[Exchange.RegisterServicesInDiscovery] Register canceled.{request.Comment}");
                        }
                    }
                }
            }
        }

        private void UpdateServiceListFromDiscovery()
        {
            var discovery_endpoint = _user_aliases.Get(BaseSocket.DISCOVERY_ALIAS);
            if (discovery_endpoint.Success)
            {
                var discoveryClient = _cachee.GetClient(discovery_endpoint.Value, true);
                if (discoveryClient != null)
                {
                    try
                    {
                        var ir = discoveryClient.Request<IEnumerable<ServiceEndpointInfo>>("services", records =>
                        {
                            if (records == null)
                            {
                                Log.SystemWarning("[Exchange.UpdateServiceListFromDiscovery] UpdateServiceListInfo. Discrovery response is empty");
                                return;
                            }
                            _dicovery_aliases.BeginUpdate();
                            try
                            {
                                foreach (var service in records)
                                {
                                    _dicovery_aliases.Set(service.ServiceInfo.ServiceKey
                                        , service.ServiceInfo.ServiceType
                                        , service.ServiceInfo.ServiceGroup
                                        , NetUtils.CreateIPEndPoint(service.Endpoint));
                                }
                                _dicovery_aliases.Commit();
                            }
                            catch
                            {
                                _dicovery_aliases.Rollback();
                            }
                        });
                        if (!ir.Success)
                        {
                            Log.SystemWarning($"[Exchange.UpdateServiceListFromDiscovery] Error request to inbox 'services'. {ir.Comment}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, "[Exchange.UpdateServiceListFromDiscovery] Discovery service response is absent");
                    }
                }
            }
        }
        #endregion

        public IClient GetConnection(string alias)
        {
            if (_update_discovery_table_task != -1)
            {
                var address = _dicovery_aliases.Get(alias);
                if (address.Success)
                {
                    return _cachee.GetClient(address.Value, true);
                }
            }
            else
            {
                var address = _user_aliases.Get(alias);
                if (address.Success)
                {
                    return _cachee.GetClient(address.Value, true);
                }
                try
                {
                    var endpoint = NetUtils.CreateIPEndPoint(alias);
                    return _cachee.GetClient(endpoint, true);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, "[Exchange.GetConnection]");
                }
            }
            return null;
        }

        public IClient GetConnection(IPEndPoint endpoint)
        {
            try
            {
                return _cachee.GetClient(endpoint, true);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[Exchange.GetConnection]");
            }
            return null;
        }

        public IClient GetConnection(ISocketClient client)
        {
            try
            {
                return new ExClient(client);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, "[Exchange.GetConnection]");
            }
            return null;
        }

        #region Host service
        public IRouter UseHost()
        {
            return MakeHost(new IPEndPoint(IPAddress.Any, NetUtils.GetFreeTcpPort()));
        }

        public IRouter UseHost(int port)
        {
            return MakeHost(new IPEndPoint(IPAddress.Any, port));
        }

        public IRouter UseHost(IPEndPoint endpoint)
        {
            return MakeHost(endpoint);
        }

        private IRouter MakeHost(IPEndPoint endpoint)
        {
            var server = _cachee.GetServer(endpoint, new Router());
            server.RegisterInbox<ServiceDescription>("__service_description__", _ => CollectServiceDescription());
            return server;
        }
        #endregion

        #region Private
        private IEnumerable<IPEndPoint> GetAllAddresses(string serviceKey)
        {
            if (_update_discovery_table_task != -1)
            {
                var dr = _dicovery_aliases.GetAll(serviceKey);
                var ur = _user_aliases.GetAll(serviceKey);
                if (dr.Success && ur.Success)
                {
                    return Enumerable.Union<IPEndPoint>(dr.Value, ur.Value);
                }
                else if (dr.Success)
                {
                    return dr.Value;
                }
                else if (ur.Success)
                {
                    return ur.Value;
                }
            }
            else
            {
                var result = _user_aliases.GetAll(serviceKey);
                if (result.Success)
                {
                    return result.Value;
                }
            }
            return null;
        }

        private IEnumerable<IPEndPoint> GetAllAddressesByType(string serviceType)
        {
            if (_update_discovery_table_task != -1)
            {
                var dr = _dicovery_aliases.GetAllByType(serviceType);
                var ur = _user_aliases.GetAllByType(serviceType);
                if (dr.Success && ur.Success)
                {
                    return Enumerable.Union<IPEndPoint>(dr.Value, ur.Value);
                }
                else if (dr.Success)
                {
                    return dr.Value;
                }
                else if (ur.Success)
                {
                    return ur.Value;
                }
            }
            else
            {
                var result = _user_aliases.GetAllByType(serviceType);
                if (result.Success)
                {
                    return result.Value;
                }
            }
            return null;
        }

        private IEnumerable<IPEndPoint> GetAllAddressesByGroup(string serviceGroup)
        {
            if (_update_discovery_table_task != -1)
            {
                var dr = _dicovery_aliases.GetAllByGroup(serviceGroup);
                var ur = _user_aliases.GetAllByGroup(serviceGroup);
                if (dr.Success && ur.Success)
                {
                    return Enumerable.Union<IPEndPoint>(dr.Value, ur.Value);
                }
                else if (dr.Success)
                {
                    return dr.Value;
                }
                else if (ur.Success)
                {
                    return ur.Value;
                }
            }
            else
            {
                var result = _user_aliases.GetAllByGroup(serviceGroup);
                if (result.Success)
                {
                    return result.Value;
                }
            }
            return null;
        }

        private IEnumerable<IClient> GetClientEnumerator(string serviceKey)
        {
            IEnumerable<IPEndPoint> candidates;
            try
            {
                candidates = GetAllAddresses(serviceKey);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.GetClientEnumerator] Error when trying get endpoints for service key '{serviceKey}'");
                candidates = null;
            }
            if (candidates != null && candidates.Any())
            {
                foreach (var endpoint in candidates)
                {
                    IClient transport;
                    try
                    {
                        transport = _cachee.GetClient(endpoint, true);
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[Exchange.GetClientEnumerator] Can't get transport for endpoint '{endpoint}'");
                        continue;
                    }
                    if (transport == null) continue;
                    yield return transport;
                }
            }
            else
            {
                Log.Debug($"[Exchange.GetClientEnumerator] Not found endpoints for service key '{serviceKey}'");
            }
        }

        private IEnumerable<IClient> GetClientEnumeratorByType(string serviceType)
        {
            IEnumerable<IPEndPoint> candidates;
            try
            {
                candidates = GetAllAddressesByType(serviceType);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.GetClientEnumeratorByType] Error when trying get endpoints for service type '{serviceType}'");
                candidates = null;
            }
            if (candidates != null && candidates.Any())
            {
                foreach (var endpoint in candidates)
                {
                    IClient transport;
                    try
                    {
                        transport = _cachee.GetClient(endpoint, true);
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[Exchange.GetClientEnumeratorByType] Can't get transport for endpoint '{endpoint}'");
                        continue;
                    }
                    if (transport == null) continue;
                    yield return transport;
                }
            }
            else
            {
                Log.Debug($"[Exchange.GetClientEnumeratorByType] Not found endpoints for service type '{serviceType}'");
            }
        }

        private IEnumerable<IClient> GetClientEnumeratorByGroup(string serviceGroup)
        {
            IEnumerable<IPEndPoint> candidates;
            try
            {
                candidates = GetAllAddressesByGroup(serviceGroup);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.GetClientEnumeratorByGroup] Error when trying get endpoints for service group '{serviceGroup}'");
                candidates = null;
            }
            if (candidates != null && candidates.Any())
            {
                foreach (var service in candidates)
                {
                    IClient transport;
                    try
                    {
                        transport = _cachee.GetClient(service, true);
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[Exchange.GetClientEnumeratorByGroup] Can't get transport for endpoint '{service}'");
                        continue;
                    }
                    if (transport == null) continue;
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
        private bool CallService(string serviceKey, Func<IClient, bool> callHandler)
        {
            IEnumerable<IPEndPoint> candidates;
            try
            {
                candidates = GetAllAddresses(serviceKey);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange.CallService] Error when trying get endpoints for service key '{serviceKey}'");
                return false;
            }
            if (candidates == null || candidates.Any() == false)
            {
                Log.Debug($"[Exchange.CallService] Not found endpoints for service key '{serviceKey}'");
                return false;
            }
            var success = false;
            foreach (var endpoint in candidates)
            {
                IClient transport;
                try
                {
                    transport = _cachee.GetClient(endpoint, true);
                }
                catch (Exception ex)
                {
                    Log.SystemError(ex, $"[Exchange.CallService] Can't get transport for service '{serviceKey}'");
                    continue;
                }
                if (transport == null) continue;
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

        private IEnumerable<Tresp> _RequestBroadcast<Treq, Tresp>(List<IClient> clients, string inbox, Treq data)
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
                            if (false == client.Request<Treq, Tresp>(inbox, data, resp => { response.Add(resp); waiter.Signal(); }).Success)
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

        private IEnumerable<Tresp> _RequestBroadcast<Tresp>(List<IClient> clients, string inbox)
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
                            if (false == client.Request<Tresp>(inbox, resp => { response.Add(resp); waiter.Signal(); }).Success)
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

        private ServiceDescription CollectServiceDescription()
        {
            return new ServiceDescription
            {
                ServiceInfo = this._owner?.ServiceInfo,
                Inboxes = _cachee.ServerList
                    .SelectMany(se => se
                        .CollectInboxInfo()
                        .Select(i =>
                        {
                            i.Port = se.LocalEndpoint.Port;
                            return i;
                        })).ToList()
            };
        }

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