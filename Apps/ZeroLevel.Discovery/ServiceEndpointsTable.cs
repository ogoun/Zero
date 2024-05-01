using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using ZeroLevel.Models;
using ZeroLevel.Network;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Discovery
{
    public class ServiceEndpointsTable
    {
        private ConcurrentDictionary<string, ZeroServiceInfo> _records;

        public ServiceEndpointsTable()
        {
            if (!TryLoad())
            {
                _records = new ConcurrentDictionary<string, ZeroServiceInfo>();
            }
            Sheduller.RemindEvery(TimeSpan.FromSeconds(10), Heartbeat);
        }

        public InvokeResult AppendOrUpdate(ServiceRegisterInfo registerInfo, ISocketClient client)
        {
            if (registerInfo == null || registerInfo.ServiceInfo == null) return InvokeResult.Fault();

            var endpoint = $"{client.Endpoint.Address}:{registerInfo.Port}";
            Log.Info($"[ServiceEndpointsTable.AppendOrUpdate]\t{registerInfo.ServiceInfo.ServiceKey}\t{endpoint}");
            if (NetUtils.TestConnection(NetUtils.CreateIPEndPoint(endpoint)))
            {
                _records.AddOrUpdate(endpoint, registerInfo.ServiceInfo, (key, oldValue) => registerInfo.ServiceInfo);
                Save();
                return InvokeResult.Succeeding();
            }
            else
            {
                Log.Warning($"[ServiceEndpointsTable.AppendOrUpdate]\t{registerInfo.ServiceInfo.ServiceKey}\t{endpoint} no avaliable");
            }
            return InvokeResult.Fault();
        }

        public IEnumerable<ServiceEndpointInfo> GetRoutingTable()
        {
            foreach (var pair in _records) yield return new ServiceEndpointInfo { Endpoint = pair.Key, ServiceInfo = pair.Value };
        }

        #region Snapshot
        private void Save()
        {
            try
            {
                using (var fs = new FileStream(Path.Combine(Configuration.BaseDirectory, "snapshot.snp")
                    , FileMode.Create
                    , FileAccess.Write
                    , FileShare.None))
                {
                    using (var writer = new MemoryStreamWriter(fs))
                    {
                        writer.WriteDictionary(_records);
                        writer.Stream.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[ServiceEndpointsTable.Save]");
            }
        }

        private bool TryLoad()
        {
            try
            {
                var path = Path.Combine(Configuration.BaseDirectory, "snapshot.snp");
                if (File.Exists(path))
                {
                    using (var fs = new FileStream(path
                                   , FileMode.Open
                                   , FileAccess.Read
                                   , FileShare.None))
                    {
                        using (var reader = new MemoryStreamReader(fs))
                        {
                            _records = reader.ReadDictionaryAsConcurrent<string, ZeroServiceInfo>();
                            return _records != null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[ServiceEndpointsTable.Load]");
            }
            return false;
        }

        #endregion Snapshot

        #region Heartbeat
        private void Heartbeat(long taskid)
        {
            try
            {
                var toRemove = new List<string>();
                foreach (var pair in _records)
                {
                    if (NetUtils.TestConnection(NetUtils.CreateIPEndPoint(pair.Key)) == false)
                    {
                        toRemove.Add(pair.Key);
                    }
                }
                ZeroServiceInfo service;
                foreach (var key in toRemove)
                {
                    if (_records.TryRemove(key, out service))
                    {
                        Log.Info($"[ServiceEndpointsTable.Heartbeat] {service.ServiceKey} on {key} was removed because not answer for ping");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[ServiceEndpointsTable.Heartbeat]");
            }
            Save();
        }
        #endregion
    }
}
