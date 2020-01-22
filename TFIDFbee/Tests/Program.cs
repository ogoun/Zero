using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using ZeroLevel.Services.Web;

namespace Tests
{
    class Program
    {
        public String responseToWords(String response)
        {
            response = response
                .ToLowerInvariant()
                .Replace("<title>", ",")
                .Replace("</title>", ",")
                .Replace("<meta name=\"description\" content=", ",")
                .Replace("<meta name=\"keywords\" content=", ",");
            response = response
                .Replace("[^a-zA-Zа-яА-Я\\w\\s]]*", ",")
                .Replace(" ", ",");
            var array = new List<string>();
            foreach (String word in response.Split(","))
            {
                if (!string.IsNullOrWhiteSpace(word) && word.Length > 1)
                {
                    array.Add(word);
                }
            }
            array.Sort();
            response = string.Join(' ', array);
            return response;
        }

        private HttpClient GetClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseDefaultCredentials = ZeroLevel.Configuration.Default.FirstOrDefault<bool>("useDefaultCredentianls"),
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; }
            };
            if (ZeroLevel.Configuration.Default.FirstOrDefault<bool>("useDefaultCredentianls"))
            {
                handler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            }
            var httpClient = new HttpClient(handler);
            httpClient.DefaultRequestHeaders.Add("user-agent", UserAgents.Next());
            return httpClient;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
