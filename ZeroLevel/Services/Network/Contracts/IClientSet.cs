using System;
using System.Collections.Generic;
using ZeroLevel.Models;

namespace ZeroLevel.Network
{
    public interface IClientSet
    {
        InvokeResult Send<T>(string alias, T data);
        InvokeResult Send<T>(string alias, string inbox, T data);
        InvokeResult Request<Tresponse>(string alias, Action<Tresponse> callback);
        InvokeResult Request<Tresponse>(string alias, string inbox, Action<Tresponse> callback);
        InvokeResult Request<Trequest, Tresponse>(string alias, Trequest request, Action<Tresponse> callback);
        InvokeResult Request<Trequest, Tresponse>(string alias, string inbox, Trequest request, Action<Tresponse> callback);


        InvokeResult SendBroadcast<T>(string alias, T data);
        InvokeResult SendBroadcast<T>(string alias, string inbox, T data);

        InvokeResult SendBroadcastByType<T>(string serviceType, T data);
        InvokeResult SendBroadcastByType<T>(string serviceType, string inbox, T data);

        InvokeResult SendBroadcastByGroup<T>(string serviceGroup, T data);
        InvokeResult SendBroadcastByGroup<T>(string serviceGroup, string inbox, T data);

        InvokeResult RequestBroadcast<Tresponse>(string alias, Action<IEnumerable<Tresponse>> callback);
        InvokeResult RequestBroadcast<Tresponse>(string alias, string inbox, Action<IEnumerable<Tresponse>> callback);

        InvokeResult RequestBroadcast<Trequest, Tresponse>(string alias, Trequest data, Action<IEnumerable<Tresponse>> callback);
        InvokeResult RequestBroadcast<Trequest, Tresponse>(string alias, string inbox, Trequest data, Action<IEnumerable<Tresponse>> callback);

        InvokeResult RequestBroadcastByType<Tresponse>(string serviceType, Action<IEnumerable<Tresponse>> callback);
        InvokeResult RequestBroadcastByType<Tresponse>(string serviceType, string inbox, Action<IEnumerable<Tresponse>> callback);

        InvokeResult RequestBroadcastByType<Trequest, Tresponse>(string serviceType, Trequest data, Action<IEnumerable<Tresponse>> callback);
        InvokeResult RequestBroadcastByType<Trequest, Tresponse>(string serviceType, string inbox, Trequest data, Action<IEnumerable<Tresponse>> callback);

        InvokeResult RequestBroadcastByGroup<Tresponse>(string serviceGroup, Action<IEnumerable<Tresponse>> callback);
        InvokeResult RequestBroadcastByGroup<Tresponse>(string serviceGroup, string inbox, Action<IEnumerable<Tresponse>> callback);

        InvokeResult RequestBroadcastByGroup<Trequest, Tresponse>(string serviceGroup, Trequest data, Action<IEnumerable<Tresponse>> callback);
        InvokeResult RequestBroadcastByGroup<Trequest, Tresponse>(string serviceGroup, string inbox, Trequest data, Action<IEnumerable<Tresponse>> callback);
    }
}
