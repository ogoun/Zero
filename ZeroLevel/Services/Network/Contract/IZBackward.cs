using System.Net;
using ZeroLevel.Models;

namespace ZeroLevel.Network
{
    public interface IZBackward
    {
        IPEndPoint Endpoint { get; }

        InvokeResult SendBackward(Frame frame);
        InvokeResult SendBackward<T>(string inbox, T message);
    }
}