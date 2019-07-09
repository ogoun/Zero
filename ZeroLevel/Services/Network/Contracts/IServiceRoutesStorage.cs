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

        void Remove(IPEndPoint endpoint);

        InvokeResult<IPEndPoint> Get(string key);
        InvokeResult<IEnumerable<IPEndPoint>> GetAll(string key);
        InvokeResult<IPEndPoint> GetByType(string type);
        InvokeResult<IEnumerable<IPEndPoint>> GetAllByType(string type);
        InvokeResult<IPEndPoint> GetByGroup(string group);
        InvokeResult<IEnumerable<IPEndPoint>> GetAllByGroup(string group);
    }
}
