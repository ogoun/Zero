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
            _lock.EnterWriteLock();
            try
            {
                var key = $"{endpoint.Address}:{endpoint.Port}";
                if (_endpoints.ContainsKey(endpoint))
                {
                    if (_tableByKey.ContainsKey(key))
                    {
                        return;
                    }
                    Remove(endpoint);
                }
                AppendByKeys(key, endpoint);
                _endpoints.Add(endpoint, new string[3] { $"{endpoint.Address}:{endpoint.Port}", null, null });
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
                    Remove(endpoint);
                }
                AppendByKeys(key, endpoint);
                _endpoints.Add(endpoint, new string[3] { key, null, null });
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Set(string key, IEnumerable<IPEndPoint> endpoints)
        {
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
                        Remove(drop[i]);
                    }
                }
                foreach (var ep in endpoints)
                {
                    _endpoints.Add(ep, new string[3] { key, null, null });
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
            _lock.EnterWriteLock();
            try
            {
                Remove(endpoint);
                if (key == null)
                {
                    key = $"{endpoint.Address}:{endpoint.Port}";
                }
                AppendByKeys(key, endpoint);
                if (type != null)
                {
                    AppendByType(key, endpoint);
                }
                if (group != null)
                {
                    AppendByGroup(key, endpoint);
                }
                _endpoints.Add(endpoint, new string[3] { key, null, null });
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Set(string key, string type, string group, IEnumerable<IPEndPoint> endpoints)
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var ep in endpoints)
                {
                    Remove(ep);
                    Set(key, type, group, ep);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #region GET
        public InvokeResult<IPEndPoint> Get(string key)
        {
            if (_tableByKey.ContainsKey(key))
            {
                if (_tableByKey[key].MoveNext())
                    return InvokeResult.Succeeding(_tableByKey[key].Current);
            }
            return InvokeResult.Fault<IPEndPoint>($"No endpoints by key '{key}'");
        }
        public InvokeResult<IEnumerable<IPEndPoint>> GetAll(string key)
        {
            if (_tableByKey.ContainsKey(key))
            {
                if (_tableByKey[key].MoveNext())
                    return InvokeResult.Succeeding(_tableByKey[key].GetCurrentSeq());
            }
            return InvokeResult.Fault<IEnumerable<IPEndPoint>>($"No endpoints by key '{key}'");
        }
        public InvokeResult<IPEndPoint> GetByType(string type)
        {
            if (_tableByTypes.ContainsKey(type))
            {
                if (_tableByTypes[type].MoveNext())
                    return InvokeResult.Succeeding(_tableByTypes[type].Current);
            }
            return InvokeResult.Fault<IPEndPoint>($"No endpoints by type '{type}'");
        }
        public InvokeResult<IEnumerable<IPEndPoint>> GetAllByType(string type)
        {
            if (_tableByTypes.ContainsKey(type))
            {
                if (_tableByTypes[type].MoveNext())
                    return InvokeResult.Succeeding(_tableByTypes[type].GetCurrentSeq());
            }
            return InvokeResult.Fault<IEnumerable<IPEndPoint>>($"No endpoints by type '{type}'");
        }
        public InvokeResult<IPEndPoint> GetByGroup(string group)
        {
            if (_tableByGroups.ContainsKey(group))
            {
                if (_tableByGroups[group].MoveNext())
                    return InvokeResult.Succeeding(_tableByGroups[group].Current);
            }
            return InvokeResult.Fault<IPEndPoint>($"No endpoints by group '{group}'");
        }
        public InvokeResult<IEnumerable<IPEndPoint>> GetAllByGroup(string group)
        {
            if (_tableByGroups.ContainsKey(group))
            {
                if (_tableByGroups[group].MoveNext())
                    return InvokeResult.Succeeding(_tableByGroups[group].GetCurrentSeq());
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

        private void Remove(IPEndPoint endpoint)
        {
            var refs = _endpoints[endpoint];
            if (refs[0] != null && _tableByKey.ContainsKey(refs[0])) _tableByKey[refs[0]].Remove(endpoint);
            if (refs[1] != null && _tableByTypes.ContainsKey(refs[1])) _tableByTypes[refs[1]].Remove(endpoint);
            if (refs[2] != null && _tableByGroups.ContainsKey(refs[2])) _tableByGroups[refs[2]].Remove(endpoint);
            _endpoints.Remove(endpoint);
        }
        #endregion
    }
}
