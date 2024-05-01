using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using ZeroLevel.Models;
using ZeroLevel.Services.Collections;

namespace ZeroLevel.Network
{
    /*
   One IPEndpoint binded with one service.
   Service can have one key, one type, one group.
   Therefore IPEndpoint can be binded with one key, one type and one group.

   One key can refer to many IPEndPoints.
   One type can refer to many IPEndPoints.
   One group can refer to many IPEndPoints.
   */

    public sealed class ServiceRouteStorage
        : IServiceRoutesStorage
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        private Dictionary<string, RoundRobinCollection<IPEndPoint>> _tableByKey
            = new Dictionary<string, RoundRobinCollection<IPEndPoint>>();

        private Dictionary<string, RoundRobinCollection<IPEndPoint>> _tableByGroups
            = new Dictionary<string, RoundRobinCollection<IPEndPoint>>();

        private Dictionary<string, RoundRobinCollection<IPEndPoint>> _tableByTypes
            = new Dictionary<string, RoundRobinCollection<IPEndPoint>>();

        private Dictionary<IPEndPoint, string[]> _endpoints
            = new Dictionary<IPEndPoint, string[]>();

        public void Set(IPEndPoint endpoint)
        {
            var key = $"{endpoint.Address}:{endpoint.Port}";

            if (_in_transaction == 1)
            {
                TransAppendByKeys(key, endpoint);
                _tran_endpoints[endpoint] = new string[] { key, null!, null! };
                return;
            }

            _lock.EnterWriteLock();
            try
            {
                if (_endpoints.ContainsKey(endpoint))
                {
                    if (_tableByKey.ContainsKey(key))
                    {
                        return;
                    }
                    RemoveLocked(endpoint);
                }
                AppendByKeys(key, endpoint);
                _endpoints.Add(endpoint, new string[3] { $"{endpoint.Address}:{endpoint.Port}", null!, null! });
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Set(IEnumerable<IPEndPoint> endpoints)
        {
            foreach (var ep in endpoints)
            {
                Set(ep);
            }
        }

        public void Set(string key, IPEndPoint endpoint)
        {
            key = key.ToUpperInvariant();

            if (_in_transaction == 1)
            {
                TransAppendByKeys(key, endpoint);
                _tran_endpoints[endpoint] = new string[] { key, null!, null! };
                return;
            }

            _lock.EnterWriteLock();
            try
            {
                if (_endpoints.ContainsKey(endpoint))
                {
                    var exists = _endpoints[endpoint];
                    if (exists[0] != null
                        && _tableByKey.ContainsKey(exists[0])
                        && _tableByKey[exists[0]].Count == 1
                        && _tableByKey[exists[0]].Contains(endpoint))
                    {
                        return;
                    }
                    RemoveLocked(endpoint);
                }
                AppendByKeys(key, endpoint);
                _endpoints.Add(endpoint, new string[3] { key, null!, null! });
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Set(string key, IEnumerable<IPEndPoint> endpoints)
        {
            key = key.ToUpperInvariant();
            if (_in_transaction == 1)
            {
                foreach (var endpoint in endpoints)
                {
                    TransAppendByKeys(key, endpoint);
                    _tran_endpoints[endpoint] = new string[] { key, null!, null! };
                }
                return;
            }

            _lock.EnterWriteLock();
            try
            {
                if (_tableByKey.ContainsKey(key))
                {
                    if (_tableByKey[key].Source.OrderingEquals(endpoints))
                    {
                        return;
                    }
                    var drop = _tableByKey[key].Source.ToArray();
                    for (int i = 0; i < drop.Length; i++)
                    {
                        RemoveLocked(drop[i]);
                    }
                }
                foreach (var ep in endpoints)
                {
                    _endpoints.Add(ep, new string[3] { key.ToUpperInvariant(), null!, null! });
                    AppendByKeys(key, ep);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Set(string key, string type, string group, IPEndPoint endpoint)
        {
            if (key == null!)
            {
                key = $"{endpoint.Address}:{endpoint.Port}";
            }
            else
            {
                key = key.ToUpperInvariant();
            }
            type = type.ToUpperInvariant();
            group = group.ToUpperInvariant();

            if (_in_transaction == 1)
            {
                TransAppendByKeys(key, endpoint);
                if (type != null!)
                {
                    TransAppendByType(type, endpoint);
                }
                if (group != null!)
                {
                    TransAppendByGroup(group, endpoint);
                }
                _tran_endpoints[endpoint] = new string[] { key, type!, group! };
                return;
            }

            _lock.EnterWriteLock();
            try
            {
                RemoveLocked(endpoint);
                AppendByKeys(key, endpoint);
                if (type != null!)
                {
                    AppendByType(type, endpoint);
                }
                if (group != null!)
                {
                    AppendByGroup(group, endpoint);
                }
                _endpoints.Add(endpoint, new string[3] { key.ToUpperInvariant(), type?.ToUpperInvariant() ?? string.Empty, group?.ToUpperInvariant() ?? string.Empty });
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Set(string key, string type, string group, IEnumerable<IPEndPoint> endpoints)
        {
            if (_in_transaction == 1)
            {
                key = key.ToUpperInvariant();
                type = type.ToUpperInvariant();
                group = group.ToUpperInvariant();

                foreach (var endpoint in endpoints)
                {
                    TransAppendByKeys(key, endpoint);
                    if (type != null!)
                    {
                        TransAppendByType(type, endpoint);
                    }
                    if (group != null!)
                    {
                        TransAppendByGroup(group, endpoint);
                    }
                    _tran_endpoints[endpoint] = new string[] { key, type!, group! };
                }
                return;
            }

            foreach (var ep in endpoints)
            {
                RemoveLocked(ep);
                Set(key, type, group, ep);
            }
        }

        public bool ContainsKey(string key) => _tableByKey.ContainsKey(key);

        public bool ContainsType(string type) => _tableByTypes.ContainsKey(type);

        public bool ContainsGroup(string group) => _tableByGroups.ContainsKey(group);

        public void Remove(string key)
        {
            if (_tableByKey.ContainsKey(key))
            {
                var eps = _tableByKey[key].Source.ToList();
                foreach (var ep in eps)
                {
                    RemoveLocked(ep);
                }
            }
        }

        public void Remove(IPEndPoint endpoint)
        {
            _lock.EnterWriteLock();
            try
            {
                RemoveLocked(endpoint);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #region GET
        public IEnumerable<string> GetKeys()
        {
            _lock.EnterReadLock();
            try
            {
                return _tableByKey.Select(pair => pair.Key).ToArray();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public InvokeResult<IPEndPoint> Get(string key)
        {
            key = key.ToUpperInvariant();
            _lock.EnterReadLock();
            try
            {
                if (_tableByKey.ContainsKey(key))
                {
                    if (_tableByKey[key].MoveNext())
                        return InvokeResult.Succeeding(_tableByKey[key].Current);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return InvokeResult.Fault<IPEndPoint>($"No endpoints by key '{key}'");
        }

        public InvokeResult<IEnumerable<IPEndPoint>> GetAll(string key)
        {
            key = key.ToUpperInvariant();
            _lock.EnterReadLock();
            try
            {
                if (_tableByKey.ContainsKey(key))
                {
                    if (_tableByKey[key].MoveNext())
                        return InvokeResult.Succeeding(_tableByKey[key].GetCurrentSeq());
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return InvokeResult.Fault<IEnumerable<IPEndPoint>>($"No endpoints by key '{key}'");
        }


        public IEnumerable<KeyValuePair<string, IPEndPoint>> GetAll()
        {
            _lock.EnterReadLock();
            try
            {
                return _tableByKey.SelectMany(pair => pair.Value.Source.Select(s => new KeyValuePair<string, IPEndPoint>(pair.Key, s)));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public InvokeResult<IPEndPoint> GetByType(string type)
        {
            type = type.ToUpperInvariant();
            _lock.EnterReadLock();
            try
            {
                if (_tableByTypes.ContainsKey(type))
                {
                    if (_tableByTypes[type].MoveNext())
                        return InvokeResult.Succeeding(_tableByTypes[type].Current);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return InvokeResult.Fault<IPEndPoint>($"No endpoints by type '{type}'");
        }
        public InvokeResult<IEnumerable<IPEndPoint>> GetAllByType(string type)
        {
            type = type.ToUpperInvariant();
            _lock.EnterReadLock();
            try
            {
                if (_tableByTypes.ContainsKey(type))
                {
                    if (_tableByTypes[type].MoveNext())
                        return InvokeResult.Succeeding(_tableByTypes[type].GetCurrentSeq());
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return InvokeResult.Fault<IEnumerable<IPEndPoint>>($"No endpoints by type '{type}'");
        }
        public InvokeResult<IPEndPoint> GetByGroup(string group)
        {
            group = group.ToUpperInvariant();
            _lock.EnterReadLock();
            try
            {
                if (_tableByGroups.ContainsKey(group))
                {
                    if (_tableByGroups[group].MoveNext())
                        return InvokeResult.Succeeding(_tableByGroups[group].Current);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return InvokeResult.Fault<IPEndPoint>($"No endpoints by group '{group}'");
        }
        public InvokeResult<IEnumerable<IPEndPoint>> GetAllByGroup(string group)
        {
            group = group.ToUpperInvariant();
            _lock.EnterReadLock();
            try
            {
                if (_tableByGroups.ContainsKey(group))
                {
                    if (_tableByGroups[group].MoveNext())
                        return InvokeResult.Succeeding(_tableByGroups[group].GetCurrentSeq());
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            return InvokeResult.Fault<IEnumerable<IPEndPoint>>($"No endpoints by group '{group}'");
        }
        #endregion

        #region Private
        private void AppendByKeys(string key, IPEndPoint endpoint)
        {
            Append(key, endpoint, _tableByKey);
        }
        private void AppendByType(string type, IPEndPoint endpoint)
        {
            Append(type, endpoint, _tableByTypes);
        }
        private void AppendByGroup(string group, IPEndPoint endpoint)
        {
            Append(group, endpoint, _tableByGroups);
        }
        private void Append(string key, IPEndPoint value, Dictionary<string, RoundRobinCollection<IPEndPoint>> dict)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, new RoundRobinCollection<IPEndPoint>());
            }
            dict[key].Add(value);
        }

        private void RemoveLocked(IPEndPoint endpoint)
        {
            if (_endpoints.ContainsKey(endpoint))
            {
                var refs = _endpoints[endpoint];
                if (refs[0] != null && _tableByKey.ContainsKey(refs[0])) _tableByKey[refs[0]].Remove(endpoint);
                if (refs[1] != null && _tableByTypes.ContainsKey(refs[1])) _tableByTypes[refs[1]].Remove(endpoint);
                if (refs[2] != null && _tableByGroups.ContainsKey(refs[2])) _tableByGroups[refs[2]].Remove(endpoint);
                _endpoints.Remove(endpoint);
            }
        }
        #endregion

        #region Transactional
        private Dictionary<string, List<IPEndPoint>> _tran_tableByKey
            = new Dictionary<string, List<IPEndPoint>>();

        private Dictionary<string, List<IPEndPoint>> _tran_tableByGroups
            = new Dictionary<string, List<IPEndPoint>>();

        private Dictionary<string, List<IPEndPoint>> _tran_tableByTypes
            = new Dictionary<string, List<IPEndPoint>>();

        private Dictionary<IPEndPoint, string[]> _tran_endpoints
            = new Dictionary<IPEndPoint, string[]>();

        private int _in_transaction = 0;

        internal void BeginUpdate()
        {
            if (Interlocked.Exchange(ref _in_transaction, 1) == 0)
            {
                _tran_endpoints.Clear();
                _tran_tableByKey.Clear();
                _tran_tableByGroups.Clear();
                _tran_tableByTypes.Clear();
            }
            else
            {
                throw new System.Exception("Transaction started already");
            }
        }

        internal void Commit()
        {
            if (Interlocked.Exchange(ref _in_transaction, 0) == 1)
            {
                _lock.EnterWriteLock();
                try
                {
                    _endpoints = _tran_endpoints.Select(pair => pair).ToDictionary(p => p.Key, p => p.Value);
                    _tableByGroups = _tran_tableByGroups.Select(pair => pair).ToDictionary(p => p.Key, p => new RoundRobinCollection<IPEndPoint>(p.Value));
                    _tableByKey = _tran_tableByKey.Select(pair => pair).ToDictionary(p => p.Key, p => new RoundRobinCollection<IPEndPoint>(p.Value));
                    _tableByTypes = _tran_tableByTypes.Select(pair => pair).ToDictionary(p => p.Key, p => new RoundRobinCollection<IPEndPoint>(p.Value));
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
                _tran_endpoints.Clear();
                _tran_tableByKey.Clear();
                _tran_tableByGroups.Clear();
                _tran_tableByTypes.Clear();
            }
        }

        internal void Rollback()
        {
            if (Interlocked.Exchange(ref _in_transaction, 0) == 1)
            {
                _tran_endpoints.Clear();
                _tran_tableByKey.Clear();
                _tran_tableByGroups.Clear();
                _tran_tableByTypes.Clear();
            }
        }

        private void TransAppendByKeys(string key, IPEndPoint endpoint)
        {
            TransAppend(key.ToUpperInvariant(), endpoint, _tran_tableByKey);
        }
        private void TransAppendByType(string type, IPEndPoint endpoint)
        {
            TransAppend(type.ToUpperInvariant(), endpoint, _tran_tableByTypes);
        }
        private void TransAppendByGroup(string group, IPEndPoint endpoint)
        {
            TransAppend(group.ToUpperInvariant(), endpoint, _tran_tableByGroups);
        }
        private void TransAppend(string key, IPEndPoint value, Dictionary<string, List<IPEndPoint>> dict)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, new List<IPEndPoint>());
            }
            dict[key].Add(value);
        }
        #endregion
    }
}
