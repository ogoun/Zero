using System;
using ZeroLevel.Models;

namespace ZeroLevel.Network
{
    public interface IClient
    {
        InvokeResult Send(string inbox);
        InvokeResult Send(string inbox, byte[] data);
        InvokeResult Send<T>(string inbox, T message);

        InvokeResult Request(string inbox, Action<byte[]> callback);
        InvokeResult Request(string inbox, byte[] data, Action<byte[]> callback);
        InvokeResult Request<Tresponse>(string inbox, Action<Tresponse> callback);
        InvokeResult Request<Trequest, Tresponse>(string inbox, Trequest request, Action<Tresponse> callback);
    }
}
