using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ZeroLevel.Microservices;
using ZeroLevel.Models;
using ZeroLevel.Network.Microservices;

namespace ZeroLevel.Discovery
{
    public class RouteTable
       : IDisposable
    {
        private readonly Dictionary<string, ServiceEndpointsInfo> _table = new Dictionary<string, ServiceEndpointsInfo>();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public RouteTable()
        {
            Load();
            Sheduller.RemindEvery(TimeSpan.FromSeconds(10), Heartbeat);
        }

        #region Snapshot
        private static readonly object _snapshot_lock = new object();

        private void Save()
        {
            string snapshot;
            _lock.EnterReadLock();
            try
            {
                snapshot = JsonConvert.SerializeObject(_table);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fault make snapshot");
                return;
            }
            finally
            {
                _lock.ExitReadLock();
            }
            try
            {
                var snapshot_path = Path.Combine(Configuration.BaseDirectory, "snapshot.snp");
                lock (_snapshot_lock)
                {
                    File.WriteAllText(snapshot_path, snapshot);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fault save shapshot");
            }
        }

        private void Load()
        {
            try
            {
                var path = Path.Combine(Configuration.BaseDirectory, "snapshot.snp");
                if (File.Exists(path))
                {
                    var snapshot = File.ReadAllText(path);
                    if (string.IsNullOrWhiteSpace(snapshot) == false)
                    {
                        var restored = JsonConvert.DeserializeObject<Dictionary<string, ServiceEndpointsInfo>>(snapshot);
                        _lock.EnterWriteLock();
                        try
                        {
                            _table.Clear();
                            foreach (var r in restored)
                            {
                                _table.Add(r.Key, r.Value);
                            }
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fault load snapshot");
            }
        }
        #endregion

        private bool Ping(string protocol, string endpoint, string msg)
        {
            try
            {
                using (var client = ExchangeTransportFactory.GetClient(protocol, endpoint))
                {
                    return client.Status == Services.Network.ZTransportStatus.Working;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[RouteTable] Fault ping endpoint {endpoint}, protocol {protocol}");
                return false;
            }
        }

        private void Heartbeat(long taskid)
        {
            try
            {
                var removeEntities = new Dictionary<string, List<ServiceEndpointInfo>>();
                _lock.EnterReadLock();
                try
                {
                    foreach (var pair in _table)
                    {
                        var endpointsToRemove = new List<ServiceEndpointInfo>();
                        foreach (var e in pair.Value.Endpoints)
                        {
                            if (Ping(e.Protocol, e.Endpoint, "HELLO") == false)
                            {
                                if (false == removeEntities.ContainsKey(pair.Key))
                                {
                                    removeEntities.Add(pair.Key, new List<ServiceEndpointInfo>());
                                }
                                removeEntities[pair.Key].Add(e);
                            }
                        }
                    }
                }
                finally
                {
                    _lock.ExitReadLock();
                }
                _lock.EnterWriteLock();
                try
                {
                    foreach (var pair in removeEntities)
                    {
                        foreach (var ep in pair.Value)
                        {
                            _table[pair.Key].Endpoints.Remove(ep);
                        }
                    }
                    var badKeys = _table.Where(f => f.Value.Endpoints.Count == 0)
                        .Select(pair => pair.Key)
                        .ToList();
                    foreach (var badKey in badKeys)
                    {
                        _table.Remove(badKey);
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Fault heartbeat");
            }
            Save();
        }

        public InvokeResult Append(MicroserviceInfo serviceInfo)
        {
            InvokeResult result = null;
            if (Ping(serviceInfo.Protocol, serviceInfo.Endpoint, serviceInfo.ServiceKey))
            {
                var key = $"{serviceInfo.ServiceGroup}:{serviceInfo.ServiceType}:{serviceInfo.ServiceKey.Trim().ToLowerInvariant()}";
                _lock.EnterWriteLock();
                try
                {
                    if (false == _table.ContainsKey(key))
                    {
                        _table.Add(key, new ServiceEndpointsInfo
                        {
                            ServiceKey = serviceInfo.ServiceKey,
                            Version = serviceInfo.Version,
                            ServiceGroup = serviceInfo.ServiceGroup,
                            ServiceType = serviceInfo.ServiceType,
                            Endpoints = new List<ServiceEndpointInfo>()
                        });
                        _table[key].Endpoints.Add(new ServiceEndpointInfo
                        {
                            Endpoint = serviceInfo.Endpoint,
                            Protocol = serviceInfo.Protocol
                        });
                        Log.SystemInfo($"The service '{serviceInfo.ServiceKey}' registered on protocol {serviceInfo.Protocol}, endpoint: {serviceInfo.Endpoint}");
                    }
                    else
                    {
                        var exists = _table[key];
                        var endpoint = new ServiceEndpointInfo
                        {
                            Endpoint = serviceInfo.Endpoint,
                            Protocol = serviceInfo.Protocol
                        };
                        if (exists.Endpoints.Contains(endpoint) == false)
                        {
                            Log.Info($"The service '{serviceInfo.ServiceKey}' register endpoint: {serviceInfo.Endpoint} on protocol {serviceInfo.Protocol}");
                            exists.Endpoints.Add(endpoint);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Fault append service ({0} {1}) endpoint '{2}'", serviceInfo.ServiceKey, serviceInfo.Version, serviceInfo.Endpoint);
                    result = InvokeResult.Fault(ex.Message);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
                Save();
                result = InvokeResult.Succeeding();
            }
            else
            {
                result = InvokeResult.Fault($"Appending endpoint '{serviceInfo.Endpoint}' canceled for service {serviceInfo.ServiceKey} ({serviceInfo.Version}) because endpoind no avaliable");
            }
            return result;
        }

        public IEnumerable<ServiceEndpointsInfo> Get()
        {
            _lock.EnterReadLock();
            try
            {
                return _table.Values.ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            _lock.Dispose();
        }
    }
}
