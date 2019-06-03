using System;
using System.Net;
using ZeroLevel.Models;

namespace ZeroLevel.Network
{
    public interface IZBackward
    {
        IPEndPoint Endpoint { get; }

        void SendBackward(Frame frame);
        void SendBackward<T>(string inbox, T message);
    }
}