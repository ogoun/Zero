using System;
using System.Globalization;
using System.Net;

namespace ZeroLevel.Services.Network
{
    public static class SocketExtensions
    {
        public static int Compare(this IPEndPoint x, IPEndPoint y)
        {
            var xx = x.Address.ToString();
            var yy = y.Address.ToString();
            var result = string.CompareOrdinal(xx, yy);
            return result == 0 ? x.Port.CompareTo(y.Port) : result;
        }

        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length < 2) throw new FormatException("Invalid endpoint format");
            IPAddress ip;
            if (ep.Length > 2)
            {
                if (!IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
                {
                    throw new FormatException("Invalid ip-adress");
                }
            }
            else
            {
                if (!IPAddress.TryParse(ep[0], out ip))
                {
                    throw new FormatException("Invalid ip-adress");
                }
            }
            int port;
            if (!int.TryParse(ep[ep.Length - 1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }
    }
}