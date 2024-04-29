using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace ZeroLevel.Services.Network.Utils
{
    /// <summary>
    /// Methods related to Network.
    /// </summary>
    public static class Network
    {
        /// <summary>
        /// Gets the external IP Address.
        /// </summary>
        /// <value>The external IP Address.</value>
        public static string ExternalIP
        {
            get
            {
                try
                {
                    using (var clientHandler = new HttpClientHandler())
                    {
                        clientHandler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                        clientHandler.UseCookies = true;
                        clientHandler.CookieContainer = new CookieContainer();
                        using (var client = new HttpClient(clientHandler))
                        {
                            var message = new HttpRequestMessage(HttpMethod.Get, "http://ipv4.icanhazip.com");
                            using (var response = client.SendAsync(message).Result)
                            {
                                return response.Content.ReadAsStringAsync().Result;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                    return "";
                }
            }
        }
    }
}
