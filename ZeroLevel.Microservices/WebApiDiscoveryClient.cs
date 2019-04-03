using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ZeroLevel.Microservices.Contracts;
using ZeroLevel.Models;
using ZeroLevel.Network.Microservices;
using ZeroLevel.ProxyREST;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.Microservices
{
    public sealed class WebApiDiscoveryClient :
        BaseProxy, IDiscoveryClient
    {
        #region WebAPI

        private IEnumerable<ServiceEndpointsInfo> GetRecords()
        {
            return GET<IEnumerable<ServiceEndpointsInfo>>("api/v0/routes");
        }

        public InvokeResult Post(MicroserviceInfo info)
        {
            return POST<InvokeResult>("api/v0/routes", info);
        }

        #endregion WebAPI

        // Таблица по ключам
        private readonly ConcurrentDictionary<string, RoundRobinCollection<ServiceEndpointInfo>> _tableByKey =
            new ConcurrentDictionary<string, RoundRobinCollection<ServiceEndpointInfo>>();

        private readonly ConcurrentDictionary<string, RoundRobinCollection<ServiceEndpointInfo>> _tableByGroups =
            new ConcurrentDictionary<string, RoundRobinCollection<ServiceEndpointInfo>>();

        private readonly ConcurrentDictionary<string, RoundRobinCollection<ServiceEndpointInfo>> _tableByTypes =
            new ConcurrentDictionary<string, RoundRobinCollection<ServiceEndpointInfo>>();

        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public WebApiDiscoveryClient(string url)
            : base(url)
        {
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
            IEnumerable<ServiceEndpointsInfo> records;
            try
            {
                records = GetRecords();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[WebApiDiscoveryClient] Update service list error, discrovery service response is absent");
                return;
            }
            if (records == null)
            {
                Log.Warning("[WebApiDiscoveryClient] Update service list canceled, discrovery response is empty");
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
                RoundRobinCollection<ServiceEndpointInfo> removed;
                foreach (var key in keysToRemove)
                {
                    _tableByKey.TryRemove(key, out removed);
                    removed.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[WebApiDiscoveryClient] Update service list error");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Register(MicroserviceInfo info)
        {
            try
            {
                var result = Post(info);
                if (result.Success == false)
                {
                    Log.Warning($"[WebApiDiscoveryClient] Service can't register. Discovery reason: {result.Comment}. Comment: {result.Comment}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[WebApiDiscoveryClient] Fault register");
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