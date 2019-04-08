using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZeroLevel.Models;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.Network
{
    public class DiscoveryClient
        : IDiscoveryClient
    {
        private readonly ConcurrentDictionary<string, RoundRobinCollection<ServiceEndpointInfo>> _tableByKey =
            new ConcurrentDictionary<string, RoundRobinCollection<ServiceEndpointInfo>>();

        private readonly ConcurrentDictionary<string, RoundRobinCollection<ServiceEndpointInfo>> _tableByGroups =
            new ConcurrentDictionary<string, RoundRobinCollection<ServiceEndpointInfo>>();

        private readonly ConcurrentDictionary<string, RoundRobinCollection<ServiceEndpointInfo>> _tableByTypes =
            new ConcurrentDictionary<string, RoundRobinCollection<ServiceEndpointInfo>>();

        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly IExClient _discoveryServerClient;

        public DiscoveryClient(string protocol, string endpoint)
        {
            _discoveryServerClient = ExchangeTransportFactory.GetClient(protocol, endpoint);
            UpdateServiceListInfo();
            Sheduller.RemindEvery(TimeSpan.FromSeconds(30), UpdateServiceListInfo);
        }

        private void UpdateOrAddRecord(string key, ServiceEndpointsInfo info)
        {
            var groupName = info.ServiceGroup.ToLowerInvariant();
            var typeName = info.ServiceType.ToLowerInvariant();
            if (_tableByKey.ContainsKey(key) == false)
            {
                _tableByKey.TryAdd(key, new RoundRobinCollection<ServiceEndpointInfo>());
            }
            else
            {
                _tableByKey[key].Clear();
            }
            if (_tableByGroups.ContainsKey(groupName) == false)
            {
                _tableByGroups.TryAdd(groupName, new RoundRobinCollection<ServiceEndpointInfo>());
            }
            if (_tableByTypes.ContainsKey(typeName) == false)
            {
                _tableByTypes.TryAdd(typeName, new RoundRobinCollection<ServiceEndpointInfo>());
            }
            foreach (var e in info.Endpoints)
            {
                if (false == _tableByKey[key].Contains(e))
                {
                    _tableByKey[key].Add(e);
                    _tableByGroups[groupName].Add(e);
                    _tableByTypes[typeName].Add(e);
                }
            }
        }

        private void UpdateServiceListInfo()
        {
            _discoveryServerClient.ForceConnect();
            if (_discoveryServerClient.Status == ZTransportStatus.Working)
            {
                try
                {
                    var ir = _discoveryServerClient.Request<IEnumerable<ServiceEndpointsInfo>>("services", records => 
                    {
                        if (records == null)
                        {
                            Log.Warning("[DiscoveryClient] UpdateServiceListInfo. Discrovery response is empty");
                            return;
                        }
                        _lock.EnterWriteLock();
                        try
                        {
                            _tableByGroups.Clear();
                            _tableByTypes.Clear();
                            var keysToRemove = new List<string>(_tableByKey.Keys);
                            foreach (var info in records)
                            {
                                var key = info.ServiceKey.Trim().ToLowerInvariant();
                                UpdateOrAddRecord(key, info);
                                keysToRemove.Remove(key);
                            }
                            foreach (var key in keysToRemove)
                            {
                                _tableByKey.TryRemove(key, out RoundRobinCollection<ServiceEndpointInfo> removed);
                                removed.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "[DiscoveryClient] UpdateServiceListInfo. Update local routing table error.");
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                    });
                    if (!ir.Success)
                    {
                        Log.Warning($"[DiscoveryClient] UpdateServiceListInfo. Error request to inbox 'services'. {ir.Comment}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "[DiscoveryClient] UpdateServiceListInfo. Discrovery service response is absent");
                    return;
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

        public ServiceEndpointInfo GetService(string serviceKey, string endpoint)
        {
            var key = serviceKey.Trim().ToLowerInvariant();
            if (_tableByKey.ContainsKey(key) && _tableByKey[key].MoveNext())
            {
                return _tableByKey[key].Find(s => s.Endpoint.Equals(endpoint, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            }
            return null;
        }

        public IEnumerable<ServiceEndpointInfo> GetServiceEndpoints(string serviceKey)
        {
            var key = serviceKey.Trim().ToLowerInvariant();
            if (_tableByKey.ContainsKey(key) && _tableByKey[key].MoveNext())
            {
                return _tableByKey[key].GetCurrentSeq();
            }
            return Enumerable.Empty<ServiceEndpointInfo>();
        }

        public IEnumerable<ServiceEndpointInfo> GetServiceEndpointsByGroup(string serviceGroup)
        {
            var group = serviceGroup.Trim().ToLowerInvariant();
            if (_tableByGroups.ContainsKey(group) && _tableByGroups[group].MoveNext())
            {
                return _tableByGroups[group].GetCurrentSeq();
            }
            return Enumerable.Empty<ServiceEndpointInfo>();
        }

        public IEnumerable<ServiceEndpointInfo> GetServiceEndpointsByType(string serviceType)
        {
            var type = serviceType.Trim().ToLowerInvariant();
            if (_tableByTypes.ContainsKey(type) && _tableByTypes[type].MoveNext())
            {
                return _tableByTypes[type].GetCurrentSeq();
            }
            return Enumerable.Empty<ServiceEndpointInfo>();
        }
    }
}