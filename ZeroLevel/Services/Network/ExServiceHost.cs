using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ZeroLevel.Network
{
    /*
    public sealed class ExServiceHost
        : IDisposable
    {
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
    */
}