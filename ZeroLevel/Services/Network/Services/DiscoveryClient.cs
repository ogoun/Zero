using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZeroLevel.Models;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.Network
{
    internal sealed class DCRouter
    {
        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private IEnumerable<ServiceEndpointInfo> _empty = Enumerable.Empty<ServiceEndpointInfo>();
        private List<ServiceEndpointInfo> _services = new List<ServiceEndpointInfo>();

        private Dictionary<string, RoundRobinOverCollection<ServiceEndpointInfo>> _tableByKey;
        private Dictionary<string, RoundRobinOverCollection<ServiceEndpointInfo>> _tableByGroups;
        private Dictionary<string, RoundRobinOverCollection<ServiceEndpointInfo>> _tableByTypes;

        internal void Update(IEnumerable<ServiceEndpointsInfo> records)
        {
            if (records == null)
            {
                Log.Warning("[DiscoveryClient] UpdateServiceListInfo. Discrovery response is empty");
                return;
            }
            var services = new List<ServiceEndpointInfo>();
            foreach (var service in records)
            {
                var key = service.ServiceKey.ToUpperInvariant();
                var type = service.ServiceType.ToUpperInvariant();
                var group = service.ServiceGroup.ToUpperInvariant();
                services.AddRange(service.Endpoints.Select(e => new ServiceEndpointInfo { Endpoint = e, Group = group, Key = key, Type = type }));
            }
            _lock.EnterWriteLock();
            try
            {
                _services = services;
                _tableByKey = _services.GroupBy(r => r.Key).ToDictionary(g => g.Key, g => new RoundRobinOverCollection<ServiceEndpointInfo>(g));
                _tableByTypes = _services.GroupBy(r => r.Type).ToDictionary(g => g.Key, g => new RoundRobinOverCollection<ServiceEndpointInfo>(g));
                _tableByGroups = _services.GroupBy(r => r.Group).ToDictionary(g => g.Key, g => new RoundRobinOverCollection<ServiceEndpointInfo>(g));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[DiscoveryClient] UpdateServiceListInfo. Update local routing table error.");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public ServiceEndpointInfo GetService(string serviceKey, string endpoint)
        {
            var key = serviceKey.ToUpperInvariant();
            _lock.EnterReadLock();
            try
            {
                if (_tableByKey.ContainsKey(key) && !_tableByKey[key].IsEmpty)
                {
                    return _tableByKey[key].Find(s => s.Endpoint.Equals(endpoint, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return null;
        }

        public IEnumerable<ServiceEndpointInfo> GetServiceEndpoints(string serviceKey)
        {
            var key = serviceKey.Trim().ToUpperInvariant();
            _lock.EnterReadLock();
            try
            {
                if (_tableByKey.ContainsKey(key) && !_tableByKey[key].IsEmpty)
                {
                    return _tableByKey[key].GenerateSeq();
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return _empty;
        }

        public IEnumerable<ServiceEndpointInfo> GetServiceEndpointsByGroup(string serviceGroup)
        {
            var group = serviceGroup.Trim().ToUpperInvariant();
            _lock.EnterReadLock();
            try
            {
                if (_tableByGroups.ContainsKey(group) && !_tableByGroups[group].IsEmpty)
                {
                    return _tableByGroups[group].GenerateSeq();
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return _empty;
        }

        public IEnumerable<ServiceEndpointInfo> GetServiceEndpointsByType(string serviceType)
        {
            var type = serviceType.Trim().ToUpperInvariant();
            _lock.EnterReadLock();
            try
            {
                if (_tableByTypes.ContainsKey(type) && !_tableByTypes[type].IsEmpty)
                {
                    return _tableByTypes[type].GenerateSeq();
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return _empty;
        }
    }


    public class DiscoveryClient
        : IDiscoveryClient
    {
        private readonly DCRouter _router = new DCRouter();
        private readonly IExClient _discoveryServerClient;

        public DiscoveryClient(string endpoint)
        {
            _discoveryServerClient = ExchangeTransportFactory.GetClient(endpoint);
            UpdateServiceListInfo();
            Sheduller.RemindEvery(TimeSpan.FromSeconds(30), UpdateServiceListInfo);
        }

        private void UpdateServiceListInfo()
        {
            _discoveryServerClient.ForceConnect();
            if (_discoveryServerClient.Status == ZTransportStatus.Working)
            {
                try
                {
                    var ir = _discoveryServerClient.Request<IEnumerable<ServiceEndpointsInfo>>("services", records => _router.Update(records));
                    if (!ir.Success)
                    {
                        Log.Warning($"[DiscoveryClient] UpdateServiceListInfo. Error request to inbox 'services'. {ir.Comment}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[DiscoveryClient] UpdateServiceListInfo. Discrovery service response is absent");
                }
            }
            else
            {
                Log.Warning("[DiscoveryClient] UpdateServiceListInfo. No connection to discovery server");
            }
        }

        public bool Register(ExServiceInfo info)
        {
            _discoveryServerClient.ForceConnect();
            if (_discoveryServerClient.Status == ZTransportStatus.Working)
            {
                bool result = false;
                try
                {
                    _discoveryServerClient.Request<ExServiceInfo, InvokeResult>("register", info, r =>
                    {
                        result = r.Success;
                        if (!result)
                        {
                            Log.Warning($"[DiscoveryClient] Register canceled. Discovery reason: {r.Comment}. Comment: {r.Comment}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[DiscoveryClient] Register fault");
                }
                return result;
            }
            else
            {
                Log.Warning("[DiscoveryClient] Register. No connection to discovery server");
                return false;
            }
        }

        public IEnumerable<ServiceEndpointInfo> GetServiceEndpoints(string serviceKey) => _router.GetServiceEndpoints(serviceKey);
        public IEnumerable<ServiceEndpointInfo> GetServiceEndpointsByGroup(string serviceGroup) => _router.GetServiceEndpointsByGroup(serviceGroup);
        public IEnumerable<ServiceEndpointInfo> GetServiceEndpointsByType(string serviceType) => _router.GetServiceEndpointsByType(serviceType);
        public ServiceEndpointInfo GetService(string serviceKey, string endpoint) => _router.GetService(serviceKey, endpoint);
    }
}