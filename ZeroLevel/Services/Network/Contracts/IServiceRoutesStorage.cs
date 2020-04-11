using System.Collections.Generic;
using System.Net;
using ZeroLevel.Models;

namespace ZeroLevel.Network
{
    public interface IServiceRoutesStorage
    {
        void Set(IPEndPoint endpoint);
        void Set(IEnumerable<IPEndPoint> endpoints);
        void Set(string key, IPEndPoint endpoint);
        void Set(string key, IEnumerable<IPEndPoint> endpoints);
        void Set(string key, string type, string group, IPEndPoint endpoint);
        void Set(string key, string type, string group, IEnumerable<IPEndPoint> endpoints);

        bool ContainsKey(string key);
        bool ContainsType(string type);
        bool ContainsGroup(string group);

        void Remove(string key);
        void Remove(IPEndPoint endpoint);

        IEnumerable<KeyValuePair<string, IPEndPoint>> GetAll();
        IEnumerable<string> GetKeys();

        InvokeResult<IPEndPoint> Get(string key);
        InvokeResult<IEnumerable<IPEndPoint>> GetAll(string key);
        InvokeResult<IPEndPoint> GetByType(string type);
        InvokeResult<IEnumerable<IPEndPoint>> GetAllByType(string type);
        InvokeResult<IPEndPoint> GetByGroup(string group);
        InvokeResult<IEnumerable<IPEndPoint>> GetAllByGroup(string group);
    }
}
