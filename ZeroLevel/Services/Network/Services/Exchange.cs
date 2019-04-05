using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroLevel.Network
{
    /// <summary>
    /// Provides data exchange between services
    /// </summary>
    public sealed class Exchange :
        IDisposable
    {
        private readonly IDiscoveryClient _discoveryClient;
        private readonly ExServiceHost _host;

        #region Ctor

        public Exchange(IDiscoveryClient discoveryClient)
        {
            this._discoveryClient = discoveryClient ?? throw new ArgumentNullException(nameof(discoveryClient));
            this._host = new ExServiceHost(this._discoveryClient);
        }

        #endregion Ctor

        /// <summary>
        ///  Registration service
        /// </summary>
        public IExService RegisterService(IExchangeService service)
        {
            return _host.RegisterService(service);
        }

        public IExService RegisterService(ExServiceInfo service)
        {
            return _host.RegisterService(service);
        }

        #region Balanced send

        /// <summary>
        /// Sending a message to the service
        /// </summary>
        /// <param name="serviceKey">Service key</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Message</param>
        /// <returns></returns>
        public bool Send<T>(string serviceKey, string inbox, T data)
        {
            try
            {
                return _host.CallService(serviceKey, (endpoint, transport) => transport.Send<T>(inbox, data).Success);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error send data in service '{serviceKey}'. Inbox '{inbox}'");
            }
            return false;
        }

        public bool Send<T>(string serviceKey, T data) => Send(serviceKey, ZBaseNetwork.DEFAULT_MESSAGE_INBOX, data);

        #endregion Balanced send

        #region Balanced request

        public Tresp Request<Treq, Tresp>(string serviceKey, string inbox, Treq data)
        {
            Tresp response = default(Tresp);
            try
            {
                if (false == _host.CallService(serviceKey, (endpoint, transport) =>
                  {
                      try
                      {
                          using (var waiter = new ManualResetEventSlim(false))
                          {
                              if (false == transport.Request<Treq, Tresp>(inbox, data, resp =>
                               {
                                   response = resp;
                                   waiter.Set();
                               }).Success)
                              {
                                  return false;
                              }
                              if (false == waiter.Wait(ZBaseNetwork.MAX_REQUEST_TIME_MS))
                              {
                                  return false;
                              }
                          }
                          return true;
                      }
                      catch (Exception ex)
                      {
                          Log.SystemError(ex, $"[Exchange] Error request to service '{serviceKey}'. Inbox '{inbox}'");
                      }
                      return false;
                  }))
                {
                    Log.SystemWarning($"[Exchange] No responce on request. Service key '{serviceKey}'. Inbox '{inbox}'");
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error request to service '{serviceKey}'. Inbox '{inbox}'");
            }
            return response;
        }

        public Tresp Request<Tresp>(string serviceKey, string inbox)
        {
            Tresp response = default(Tresp);
            try
            {
                if (false == _host.CallService(serviceKey, (endpoint, transport) =>
                 {
                     try
                     {
                         using (var waiter = new ManualResetEventSlim(false))
                         {
                             if (false == transport.Request<Tresp>(inbox, resp =>
                              {
                                  response = resp;
                                  waiter.Set();
                              }).Success)
                             {
                                 return false;
                             }
                             if (false == waiter.Wait(ZBaseNetwork.MAX_REQUEST_TIME_MS))
                             {
                                 return false;
                             }
                         }
                         return true;
                     }
                     catch (Exception ex)
                     {
                         Log.SystemError(ex, $"[Exchange] Error request to service '{serviceKey}'. Inbox '{inbox}'");
                     }
                     return false;
                 }))
                {
                    Log.SystemWarning($"[Exchange] No responce on request. Service key '{serviceKey}'. Inbox '{inbox}'");
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error request to service '{serviceKey}'. Inbox '{inbox}'");
            }
            return response;
        }

        public Tresp Request<Treq, Tresp>(string serviceKey, Treq data) =>
            Request<Treq, Tresp>(serviceKey, ZBaseNetwork.DEFAULT_REQUEST_INBOX, data);

        public Tresp Request<Tresp>(string serviceKey) =>
            Request<Tresp>(serviceKey, ZBaseNetwork.DEFAULT_REQUEST_INBOX);

        #endregion Balanced request

        #region Direct request

        public Tresp RequestDirect<Treq, Tresp>(string endpoint, string serviceKey, string inbox, Treq data)
        {
            Tresp response = default(Tresp);
            try
            {
                if (false == _host.CallServiceDirect(endpoint, serviceKey, (transport) =>
                {
                    try
                    {
                        using (var waiter = new ManualResetEventSlim(false))
                        {
                            if (false == transport.Request<Treq, Tresp>(inbox, data, resp =>
                             {
                                 response = resp;
                                 waiter.Set();
                             }).Success)
                            {
                                return false;
                            }
                            if (false == waiter.Wait(ZBaseNetwork.MAX_REQUEST_TIME_MS))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[Exchange] Error direct request to '{endpoint}'. Service key '{serviceKey}'. Inbox '{inbox}'");
                    }
                    return false;
                }))
                {
                    Log.SystemWarning($"[Exchange] No responce on direct request to '{endpoint}'. Service key '{serviceKey}'. Inbox '{inbox}'");
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error direct request to '{endpoint}'. Service key '{serviceKey}'. Inbox '{inbox}'");
            }
            return response;
        }

        public Tresp RequestDirect<Tresp>(string endpoint, string serviceKey, string inbox)
        {
            Tresp response = default(Tresp);
            try
            {
                if (false == _host.CallServiceDirect(endpoint, serviceKey, (transport) =>
                {
                    try
                    {
                        using (var waiter = new ManualResetEventSlim(false))
                        {
                            if (false == transport.Request<Tresp>(inbox, resp =>
                            {
                                response = resp;
                                waiter.Set();
                            }).Success)
                            {
                                return false;
                            }
                            if (false == waiter.Wait(ZBaseNetwork.MAX_REQUEST_TIME_MS))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log.SystemError(ex, $"[Exchange] Error direct request to '{endpoint}'. Service key '{serviceKey}'. Inbox '{inbox}'");
                    }
                    return false;
                }))
                {
                    Log.SystemWarning($"[Exchange] No responce on direct request to '{endpoint}'. Service key '{serviceKey}'. Inbox '{inbox}'");
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error direct request to service '{serviceKey}'. Inbox '{inbox}'");
            }
            return response;
        }

        public Tresp RequestDirect<Treq, Tresp>(string endpoint, string serviceKey, Treq data) =>
            RequestDirect<Treq, Tresp>(endpoint, serviceKey, ZBaseNetwork.DEFAULT_REQUEST_INBOX, data);

        public Tresp RequestDirect<Tresp>(string endpoint, string serviceKey) =>
            RequestDirect<Tresp>(endpoint, serviceKey, ZBaseNetwork.DEFAULT_REQUEST_INBOX);

        #endregion Direct request

        #region Broadcast

        /// <summary>
        /// Sending a message to all services with the specified key to the specified handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="serviceKey">Service key</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Message</param>
        /// <returns>true - on successful submission</returns>
        public bool SendBroadcast<T>(string serviceKey, string inbox, T data)
        {
            try
            {
                foreach (var client in _host.GetClientEnumerator(serviceKey))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            client.Send(inbox, data);
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[Exchange] Error broadcast send data to services '{serviceKey}'. Inbox '{inbox}'");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast send data in service '{serviceKey}'. Inbox '{inbox}'");
            }
            return false;
        }

        /// <summary>
        /// Sending a message to all services with the specified key, to the default handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="serviceKey">Service key</param>
        /// <param name="data">Message</param>
        /// <returns>true - on successful submission</returns>
        public bool SendBroadcast<T>(string serviceKey, T data) => SendBroadcast(serviceKey, ZBaseNetwork.DEFAULT_MESSAGE_INBOX, data);

        /// <summary>
        /// Sending a message to all services of a specific type to the specified handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="serviceType">Service type</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Message</param>
        /// <returns>true - on successful submission</returns>
        public bool SendBroadcastByType<T>(string serviceType, string inbox, T data)
        {
            try
            {
                foreach (var client in _host.GetClientEnumeratorByType(serviceType))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            client.Send(inbox, data);
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[Exchange] Error broadcast send data to services with type '{serviceType}'. Inbox '{inbox}'");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast send data to services with type '{serviceType}'. Inbox '{inbox}'");
            }
            return false;
        }

        /// <summary>
        /// Sending a message to all services of a particular type, to the default handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="serviceType">Service type</param>
        /// <param name="data">Message</param>
        /// <returns>true - on successful submission</returns>
        public bool SendBroadcastByType<T>(string serviceType, T data) =>
            SendBroadcastByType(serviceType, ZBaseNetwork.DEFAULT_MESSAGE_INBOX, data);

        /// <summary>
        /// Sending a message to all services of a specific group to the specified handler
        /// </summary>
        /// <typeparam name="T">Message type</typeparam>
        /// <param name="serviceGroup">Service group</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Message</param>
        /// <returns>true - on successful submission</returns>
        public bool SendBroadcastByGroup<T>(string serviceGroup, string inbox, T data)
        {
            try
            {
                foreach (var client in _host.GetClientEnumeratorByGroup(serviceGroup))
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            client.Send(inbox, data);
                        }
                        catch (Exception ex)
                        {
                            Log.SystemError(ex, $"[Exchange] Error broadcast send data to services with type '{serviceGroup}'. Inbox '{inbox}'");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast send data to services with type '{serviceGroup}'. Inbox '{inbox}'");
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
            SendBroadcastByGroup(serviceGroup, ZBaseNetwork.DEFAULT_MESSAGE_INBOX, data);

        /// <summary>
        /// Broadcast polling services by key
        /// </summary>
        /// <typeparam name="Treq">Request message type</typeparam>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceKey">Service key</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Request message</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public IEnumerable<Tresp> RequestBroadcast<Treq, Tresp>(string serviceKey, string inbox, Treq data)
        {
            try
            {
                var clients = _host.GetClientEnumerator(serviceKey).ToList();
                return _RequestBroadcast<Treq, Tresp>(clients, inbox, data);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast request to service '{serviceKey}'. Inbox '{inbox}'");
            }
            return Enumerable.Empty<Tresp>();
        }

        /// <summary>
        /// Broadcast polling services by key, without message request
        /// </summary>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceKey">Service key</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public IEnumerable<Tresp> RequestBroadcast<Tresp>(string serviceKey, string inbox)
        {
            try
            {
                var clients = _host.GetClientEnumerator(serviceKey).ToList();
                return _RequestBroadcast<Tresp>(clients, inbox);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast request to service '{serviceKey}'. Inbox '{inbox}'");
            }
            return Enumerable.Empty<Tresp>();
        }

        /// <summary>
        /// Broadcast polling services by key, to default handler
        /// </summary>
        /// <typeparam name="Treq">Request message type</typeparam>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceKey">Service key</param>
        /// <param name="data">Request message</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public IEnumerable<Tresp> RequestBroadcast<Treq, Tresp>(string serviceKey, Treq data) =>
            RequestBroadcast<Treq, Tresp>(serviceKey, ZBaseNetwork.DEFAULT_REQUEST_INBOX, data);

        /// <summary>
        /// Broadcast polling of services by key, without message of request, to default handler
        /// </summary>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceKey">Service key</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public IEnumerable<Tresp> RequestBroadcast<Tresp>(string serviceKey) =>
            RequestBroadcast<Tresp>(serviceKey, ZBaseNetwork.DEFAULT_REQUEST_INBOX);

        /// <summary>
        /// Broadcast polling services by type of service
        /// </summary>
        /// <typeparam name="Treq">Request message type</typeparam>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceType">Service type</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Request message</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public IEnumerable<Tresp> RequestBroadcastByType<Treq, Tresp>(string serviceType, string inbox, Treq data)
        {
            try
            {
                var clients = _host.GetClientEnumeratorByType(serviceType).ToList();
                return _RequestBroadcast<Treq, Tresp>(clients, inbox, data);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast request to service by type '{serviceType}'. Inbox '{inbox}'");
            }
            return Enumerable.Empty<Tresp>();
        }

        /// <summary>
        /// Broadcast polling of services by type of service, without a request message
        /// </summary>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceType">Service type</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public IEnumerable<Tresp> RequestBroadcastByType<Tresp>(string serviceType, string inbox)
        {
            try
            {
                var clients = _host.GetClientEnumeratorByType(serviceType).ToList();
                return _RequestBroadcast<Tresp>(clients, inbox);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast request to service by type '{serviceType}'. Inbox '{inbox}'");
            }
            return Enumerable.Empty<Tresp>();
        }

        /// <summary>
        /// Broadcast polling services by type of service, in the default handler
        /// </summary>
        /// <typeparam name="Treq">Request message type</typeparam>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceType">Service type</param>
        /// <param name="data">Request message</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public IEnumerable<Tresp> RequestBroadcastByType<Treq, Tresp>(string serviceType, Treq data) =>
            RequestBroadcastByType<Treq, Tresp>(serviceType, ZBaseNetwork.DEFAULT_REQUEST_INBOX, data);

        /// <summary>
        /// Broadcast polling services by type, without message request, in the default handler
        /// </summary>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceType">Service type</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public IEnumerable<Tresp> RequestBroadcastByType<Tresp>(string serviceType) =>
            RequestBroadcastByType<Tresp>(serviceType, ZBaseNetwork.DEFAULT_REQUEST_INBOX);

        /// <summary>
        /// Broadcast polling services for a group of services
        /// </summary>
        /// <typeparam name="Treq">Request message type</typeparam>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceGroup">Service group</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="data">Request message</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public IEnumerable<Tresp> RequestBroadcastByGroup<Treq, Tresp>(string serviceGroup, string inbox, Treq data)
        {
            try
            {
                var clients = _host.GetClientEnumeratorByGroup(serviceGroup).ToList();
                return _RequestBroadcast<Treq, Tresp>(clients, inbox, data);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast request to service by type '{serviceGroup}'. Inbox '{inbox}'");
            }
            return Enumerable.Empty<Tresp>();
        }

        /// <summary>
        /// Broadcast polling services for a group of services, without prompting
        /// </summary>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceGroup">Service group</param>
        /// <param name="inbox">Inbox name</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public IEnumerable<Tresp> RequestBroadcastByGroup<Tresp>(string serviceGroup, string inbox)
        {
            try
            {
                var clients = _host.GetClientEnumeratorByGroup(serviceGroup).ToList();
                return _RequestBroadcast<Tresp>(clients, inbox);
            }
            catch (Exception ex)
            {
                Log.SystemError(ex, $"[Exchange] Error broadcast request to service by type '{serviceGroup}'. Inbox '{inbox}'");
            }
            return Enumerable.Empty<Tresp>();
        }

        /// <summary>
        /// Broadcast polling services by service group to default handler
        /// </summary>
        /// <typeparam name="Treq">Request message type</typeparam>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceGroup">Service group</param>
        /// <param name="data">Request message</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public IEnumerable<Tresp> RequestBroadcastByGroup<Treq, Tresp>(string serviceGroup, Treq data) =>
            RequestBroadcastByGroup<Treq, Tresp>(serviceGroup, ZBaseNetwork.DEFAULT_REQUEST_INBOX, data);

        /// <summary>
        ///Broadcast polling services for a group of services, without sending a request, to the default handler
        /// </summary>
        /// <typeparam name="Tresp">Response message type</typeparam>
        /// <param name="serviceGroup">Service group</param>
        /// <param name="responseHandler">Response handler</param>
        /// <returns>true - in case of successful mailing</returns>
        public IEnumerable<Tresp> RequestBroadcastByGroup<Tresp>(string serviceGroup) =>
            RequestBroadcastByGroup<Tresp>(serviceGroup, ZBaseNetwork.DEFAULT_REQUEST_INBOX);

        #region Private

        private IEnumerable<Tresp> _RequestBroadcast<Treq, Tresp>(List<IExClient> clients, string inbox, Treq data)
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
                            Log.SystemError(ex, $"[Exchange] Error direct request to service '{client.Endpoint}' in broadcast request. Inbox '{inbox}'");
                            waiter.Signal();
                        }
                    });
                }
                waiter.Wait(ZBaseNetwork.MAX_REQUEST_TIME_MS);
            }
            return response;
        }

        private IEnumerable<Tresp> _RequestBroadcast<Tresp>(List<IExClient> clients, string inbox)
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
                            Log.SystemError(ex, $"[Exchange] Error direct request to service '{client.Endpoint}' in broadcast request. Inbox '{inbox}'");
                            waiter.Signal();
                        }
                    });
                }
                waiter.Wait(ZBaseNetwork.MAX_REQUEST_TIME_MS);
            }
            return response;
        }

        #endregion Private

        #endregion Broadcast

        public void Dispose()
        {
            this._host.Dispose();
        }
    }
}