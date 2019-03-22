using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel.Microservices.Contracts;
using ZeroLevel.Microservices.Model;
using ZeroLevel.Network.Microservices;
using ZeroLevel.Services.Network;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Microservices
{
    /// <summary>
    /// Обеспечивает обмен данными между сервисами
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
        #endregion

        /// <summary>
        ///  Регистрация сервиса
        /// </summary>
        public IExService RegisterService(IExchangeService service)
        {
            return _host.RegisterService(service);
        }

        public IExService RegisterService(MicroserviceInfo service)
        {
            return _host.RegisterService(service);
        }

        #region Balanced send
        /// <summary>
        /// Отправка сообщения сервису
        /// </summary>
        /// <param name="serviceKey">Ключ сервиса</param>
        /// <param name="inbox">Имя точки приема сообщений</param>
        /// <param name="data">Сообщение</param>
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
        #endregion

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
        #endregion

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
        #endregion

        #region Broadcast
        /// <summary>
        /// Отправка сообщения всем сервисам с указанным ключом в указанный обработчик
        /// </summary>
        /// <typeparam name="T">Тип сообщения</typeparam>
        /// <param name="serviceKey">Ключ сервиса</param>
        /// <param name="inbox">Имя обработчика</param>
        /// <param name="data">Сообщение</param>
        /// <returns>true - при успешной отправке</returns>
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
        /// Отправка сообщения всем сервисам с указанным ключом, в обработчик по умолчанию
        /// </summary>
        /// <typeparam name="T">Тип сообщения</typeparam>
        /// <param name="serviceKey">Ключ сервиса</param>
        /// <param name="data">Сообщение</param>
        /// <returns>true - при успешной отправке</returns>
        public bool SendBroadcast<T>(string serviceKey, T data) => SendBroadcast(serviceKey, ZBaseNetwork.DEFAULT_MESSAGE_INBOX, data);
        /// <summary>
        /// Отправка сообщения всем сервисам конкретного типа в указанный обработчик
        /// </summary>
        /// <typeparam name="T">Тип сообщения</typeparam>
        /// <param name="serviceType">Тип сервиса</param>
        /// <param name="inbox">Имя обработчика</param>
        /// <param name="data">Сообщение</param>
        /// <returns>true - при успешной отправке</returns>
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
        /// Отправка сообщения всем сервисам конкретного типа, в обработчик по умолчанию
        /// </summary>
        /// <typeparam name="T">Тип сообщения</typeparam>
        /// <param name="serviceType">Тип сервиса</param>
        /// <param name="data">Сообщение</param>
        /// <returns>true - при успешной отправке</returns>
        public bool SendBroadcastByType<T>(string serviceType, T data) =>
            SendBroadcastByType(serviceType, ZBaseNetwork.DEFAULT_MESSAGE_INBOX, data);
        /// <summary>
        /// Отправка сообщения всем сервисам конкретной группы в указанный обработчик
        /// </summary>
        /// <typeparam name="T">Тип сообщения</typeparam>
        /// <param name="serviceGroup">Группа сервиса</param>
        /// <param name="inbox">Имя обработчика</param>
        /// <param name="data">Сообщение</param>
        /// <returns>true - при успешной отправке</returns>
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
        /// Отправка сообщения всем сервисам конкретной группы, в обработчик по умолчанию
        /// </summary>
        /// <typeparam name="T">Тип сообщения</typeparam>
        /// <param name="serviceGroup">Группа сервиса</param>
        /// <param name="data">Сообщение</param>
        /// <returns>true - при успешной отправке</returns>
        public bool SendBroadcastByGroup<T>(string serviceGroup, T data) =>
            SendBroadcastByGroup(serviceGroup, ZBaseNetwork.DEFAULT_MESSAGE_INBOX, data);
        /// <summary>
        /// Широковещательный опрос сервисов по ключу
        /// </summary>
        /// <typeparam name="Treq">Тип запроса</typeparam>
        /// <typeparam name="Tresp">Тип ответа</typeparam>
        /// <param name="serviceKey">Ключ сервиса</param>
        /// <param name="inbox">Имя обработчика</param>
        /// <param name="data">Запрос</param>
        /// <param name="responseHandler">Обработчик ответа</param>
        /// <returns>true - в случае успешной рассылки</returns>
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
        /// Широковещательный опрос сервисов по ключу, без сообщеня запроса
        /// </summary>
        /// <typeparam name="Tresp">Тип ответа</typeparam>
        /// <param name="serviceKey">Ключ сервиса</param>
        /// <param name="inbox">Имя обработчика</param>
        /// <param name="responseHandler">Обработчик ответа</param>
        /// <returns>true - в случае успешной рассылки</returns>
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
        /// Широковещательный опрос сервисов по ключу, в обработчик по умолчанию
        /// </summary>
        /// <typeparam name="Treq">Тип запроса</typeparam>
        /// <typeparam name="Tresp">Тип ответа</typeparam>
        /// <param name="serviceKey">Ключ сервиса</param>
        /// <param name="data">Запрос</param>
        /// <param name="responseHandler">Обработчик ответа</param>
        /// <returns>true - в случае успешной рассылки</returns>
        public IEnumerable<Tresp> RequestBroadcast<Treq, Tresp>(string serviceKey, Treq data) =>
            RequestBroadcast<Treq, Tresp>(serviceKey, ZBaseNetwork.DEFAULT_REQUEST_INBOX, data);
        /// <summary>
        /// Широковещательный опрос сервисов по ключу, без сообщеня запроса, в обработчик по умолчанию
        /// </summary>
        /// <typeparam name="Tresp">Тип ответа</typeparam>
        /// <param name="serviceKey">Ключ сервиса</param>
        /// <param name="responseHandler">Обработчик ответа</param>
        /// <returns>true - в случае успешной рассылки</returns>
        public IEnumerable<Tresp> RequestBroadcast<Tresp>(string serviceKey) =>
            RequestBroadcast<Tresp>(serviceKey, ZBaseNetwork.DEFAULT_REQUEST_INBOX);
        /// <summary>
        /// Широковещательный опрос сервисов по типу сервису
        /// </summary>
        /// <typeparam name="Treq">Тип запроса</typeparam>
        /// <typeparam name="Tresp">Тип ответа</typeparam>
        /// <param name="serviceType">Тип сервиса</param>
        /// <param name="inbox">Имя обработчика</param>
        /// <param name="data">Запрос</param>
        /// <param name="responseHandler">Обработчик ответа</param>
        /// <returns>true - в случае успешной рассылки</returns>
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
        /// Широковещательный опрос сервисов по типу сервису, без сообщеня запроса
        /// </summary>
        /// <typeparam name="Tresp">Тип ответа</typeparam>
        /// <param name="serviceType">Тип сервиса</param>
        /// <param name="inbox">Имя обработчика</param>
        /// <param name="responseHandler">Обработчик ответа</param>
        /// <returns>true - в случае успешной рассылки</returns>
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
        /// Широковещательный опрос сервисов по типу сервису, в обработчик по умолчанию
        /// </summary>
        /// <typeparam name="Treq">Тип запроса</typeparam>
        /// <typeparam name="Tresp">Тип ответа</typeparam>
        /// <param name="serviceType">Тип сервиса</param>
        /// <param name="data">Запрос</param>
        /// <param name="responseHandler">Обработчик ответа</param>
        /// <returns>true - в случае успешной рассылки</returns>
        public IEnumerable<Tresp> RequestBroadcastByType<Treq, Tresp>(string serviceType, Treq data) =>
            RequestBroadcastByType<Treq, Tresp>(serviceType, ZBaseNetwork.DEFAULT_REQUEST_INBOX, data);
        /// <summary>
        /// Широковещательный опрос сервисов по типу, без сообщеня запроса, в обработчик по умолчанию
        /// </summary>
        /// <typeparam name="Tresp">Тип ответа</typeparam>
        /// <param name="serviceType">Тип сервиса</param>
        /// <param name="responseHandler">Обработчик ответа</param>
        /// <returns>true - в случае успешной рассылки</returns>
        public IEnumerable<Tresp> RequestBroadcastByType<Tresp>(string serviceType) =>
            RequestBroadcastByType<Tresp>(serviceType, ZBaseNetwork.DEFAULT_REQUEST_INBOX);
        /// <summary>
        /// Широковещательный опрос сервисов по группе сервисов
        /// </summary>
        /// <typeparam name="Treq">Тип запроса</typeparam>
        /// <typeparam name="Tresp">Тип ответа</typeparam>
        /// <param name="serviceGroup">Группа сервиса</param>
        /// <param name="inbox">Имя обработчика</param>
        /// <param name="data">Запрос</param>
        /// <param name="responseHandler">Обработчик ответа</param>
        /// <returns>true - в случае успешной рассылки</returns>
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
        /// Широковещательный опрос сервисов по группе сервисов, без сообщения запроса
        /// </summary>
        /// <typeparam name="Tresp">Тип ответа</typeparam>
        /// <param name="serviceGroup">Группа сервиса</param>
        /// <param name="inbox">Имя обработчика</param>
        /// <param name="responseHandler">Обработчик ответа</param>
        /// <returns>true - в случае успешной рассылки</returns>
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
        /// Широковещательный опрос сервисов по группе сервисов в обработчик по умолчанию
        /// </summary>
        /// <typeparam name="Treq">Тип запроса</typeparam>
        /// <typeparam name="Tresp">Тип ответа</typeparam>
        /// <param name="serviceGroup">Группа сервиса</param>
        /// <param name="data">Запрос</param>
        /// <param name="responseHandler">Обработчик ответа</param>
        /// <returns>true - в случае успешной рассылки</returns>
        public IEnumerable<Tresp> RequestBroadcastByGroup<Treq, Tresp>(string serviceGroup, Treq data) =>
            RequestBroadcastByGroup<Treq, Tresp>(serviceGroup, ZBaseNetwork.DEFAULT_REQUEST_INBOX, data);
        /// <summary>
        /// Широковещательный опрос сервисов по группе сервисов, без сообщения запроса, в обработчик по умолчанию
        /// </summary>
        /// <typeparam name="Tresp">Тип ответа</typeparam>
        /// <param name="serviceGroup">Группа сервиса</param>
        /// <param name="responseHandler">Обработчик ответа</param>
        /// <returns>true - в случае успешной рассылки</returns>
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
        #endregion

        #endregion

        public void Dispose()
        {
            this._host.Dispose();
        }
    }
}
