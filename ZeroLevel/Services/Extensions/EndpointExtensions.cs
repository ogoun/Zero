﻿using System.Net;

namespace ZeroLevel.Extensions
{
    public static class EndpointExtensions
    {
        public const string HTTP_SCHEMA = "http";
        public const string HTTPS_SCHEMA = "https";

        public static string ToHttpUrl(this EndPoint endPoint, string schema, string rawUrl = null!)
        {
            if (endPoint is IPEndPoint)
            {
                var ipEndPoint = endPoint as IPEndPoint;
                return CreateHttpUrl(schema, ipEndPoint?.Address?.ToString() ?? string.Empty, ipEndPoint?.Port ?? 0,
                    rawUrl != null ? rawUrl.TrimStart('/') : string.Empty);
            }

            if (endPoint is DnsEndPoint)
            {
                var dnsEndpoint = endPoint as DnsEndPoint;
                return CreateHttpUrl(schema, dnsEndpoint?.Host ?? string.Empty, dnsEndpoint?.Port ?? 0,
                    rawUrl != null ? rawUrl.TrimStart('/') : string.Empty);
            }

            return null!;
        }

        public static string ToHttpUrl(this EndPoint endPoint, string schema, string formatString,
            params object[] args)
        {
            if (endPoint is IPEndPoint)
            {
                var ipEndPoint = endPoint as IPEndPoint;
                return CreateHttpUrl(schema, ipEndPoint?.Address?.ToString() ?? string.Empty, ipEndPoint?.Port ?? 0,
                    string.Format(formatString.TrimStart('/'), args));
            }

            if (endPoint is DnsEndPoint)
            {
                var dnsEndpoint = endPoint as DnsEndPoint;
                return CreateHttpUrl(schema, dnsEndpoint?.Host ?? string.Empty, dnsEndpoint?.Port ?? 0,
                    string.Format(formatString.TrimStart('/'), args));
            }

            return null!;
        }

        private static string CreateHttpUrl(string schema, string host, int port, string path)
        {
            return $"{schema}://{host}:{port}/{path}";
        }
    }
}