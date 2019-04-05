using System.Net;

namespace ZeroLevel.Network
{
    public interface IZBackward
    {
        IPEndPoint Endpoint { get; }

        void SendBackward(Frame frame);
    }
}