using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ZeroLevel.Models;
using ZeroLevel.Network;

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

        #endregion Snapshot

        private void Heartbeat(long taskid)
        {
            try
            {
                var removeEntities = new Dictionary<string, List<string>>();
                _lock.EnterReadLock();
                try
                {
                    foreach (var pair in _table)
                    {
                        var endpointsToRemove = new List<string>();
                        foreach (var e in pair.Value.Endpoints)
                        {
                            if (NetUtils.TestConnection(NetUtils.CreateIPEndPoint(e)) == false)
                            {
                                if (false == removeEntities.ContainsKey(pair.Key))
                                {
                                    removeEntities.Add(pair.Key, new List<string>());
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

        public InvokeResult Append(ExServiceInfo serviceInfo, ISocketClient client)
        {
            InvokeResult result = null;
            var endpoint = $"{client.Endpoint.Address}:{serviceInfo.Port}";
            Log.Info($"Regiter request from {endpoint}. Service {serviceInfo?.ServiceKey}");
            if (NetUtils.TestConnection(NetUtils.CreateIPEndPoint(endpoint)))
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
                            Endpoints = new List<string>()
                        });
                        _table[key].Endpoints.Add(endpoint);
                        Log.Info($"The service '{serviceInfo.ServiceKey}' registered on endpoint: {endpoint}");
                    }
                    else
                    {
                        var exists = _table[key];
                        if (exists.Endpoints.Contains(endpoint) == false)
                        {
                            Log.Info($"The service '{serviceInfo.ServiceKey}' register endpoint: {endpoint}");
                            exists.Endpoints.Add(endpoint);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Fault append service ({serviceInfo.ServiceKey} {serviceInfo.Version}) endpoint '{endpoint}'");
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
                result = InvokeResult.Fault($"Appending endpoint '{endpoint}' canceled for service {serviceInfo.ServiceKey} ({serviceInfo.Version}) because endpoind no avaliable");
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