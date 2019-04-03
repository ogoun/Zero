using System.Net;

namespace ZeroLevel.Services.Network
{
    public interface IZBackward
    {
        IPEndPoint Endpoint { get; }

        void SendBackward(Frame frame);
    }
}