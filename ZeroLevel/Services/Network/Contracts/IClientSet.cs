using System;
using System.Collections.Generic;
using ZeroLevel.Models;

namespace ZeroLevel.Network
{
    public interface IClientSet
    {
        bool Send<T>(string alias, T data);
        bool Send<T>(string alias, string inbox, T data);
        bool Request<Tresponse>(string alias, Action<Tresponse> callback);
        bool Request<Tresponse>(string alias, string inbox, Action<Tresponse> callback);
        bool Request<Trequest, Tresponse>(string alias, Trequest request, Action<Tresponse> callback);
        bool Request<Trequest, Tresponse>(string alias, string inbox, Trequest request, Action<Tresponse> callback);


        bool SendBroadcast<T>(string alias, T data);
        bool SendBroadcast<T>(string alias, string inbox, T data);
        
        bool SendBroadcastByType<T>(string serviceType, T data);
        bool SendBroadcastByType<T>(string serviceType, string inbox, T data);
        
        bool SendBroadcastByGroup<T>(string serviceGroup, T data);
        bool SendBroadcastByGroup<T>(string serviceGroup, string inbox, T data);
        
        bool RequestBroadcast<Tresponse>(string alias, Action<IEnumerable<Tresponse>> callback);
        bool RequestBroadcast<Tresponse>(string alias, string inbox, Action<IEnumerable<Tresponse>> callback);
        
        bool RequestBroadcast<Trequest, Tresponse>(string alias, Trequest data, Action<IEnumerable<Tresponse>> callback);
        bool RequestBroadcast<Trequest, Tresponse>(string alias, string inbox, Trequest data, Action<IEnumerable<Tresponse>> callback);
        
        bool RequestBroadcastByType<Tresponse>(string serviceType, Action<IEnumerable<Tresponse>> callback);
        bool RequestBroadcastByType<Tresponse>(string serviceType, string inbox, Action<IEnumerable<Tresponse>> callback);
        
        bool RequestBroadcastByType<Trequest, Tresponse>(string serviceType, Trequest data, Action<IEnumerable<Tresponse>> callback);
        bool RequestBroadcastByType<Trequest, Tresponse>(string serviceType, string inbox, Trequest data, Action<IEnumerable<Tresponse>> callback);
        
        bool RequestBroadcastByGroup<Tresponse>(string serviceGroup, Action<IEnumerable<Tresponse>> callback);
        bool RequestBroadcastByGroup<Tresponse>(string serviceGroup, string inbox, Action<IEnumerable<Tresponse>> callback);
        
        bool RequestBroadcastByGroup<Trequest, Tresponse>(string serviceGroup, Trequest data, Action<IEnumerable<Tresponse>> callback);
        bool RequestBroadcastByGroup<Trequest, Tresponse>(string serviceGroup, string inbox, Trequest data, Action<IEnumerable<Tresponse>> callback);
    }
}
