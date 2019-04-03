using System.Net;
using System.Net.Http;

namespace ZeroLevel.Discovery
{
    public static class HttpRequestMessagesExtensions
    {
        private const string HttpContext = "MS_HttpContext";
        private const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";
        private const string OwinContext = "MS_OwinContext";

        public static string GetClientIpAddress(HttpRequestMessage request)
        {
            //Web-hosting
            if (request.Properties.ContainsKey(HttpContext))
            {
                dynamic ctx = request.Properties[HttpContext];
                if (ctx != null)
                {
                    return ctx.Request.UserHostAddress;
                }
            }
            //Self-hosting
            if (request.Properties.ContainsKey(RemoteEndpointMessage))
            {
                dynamic remoteEndpoint = request.Properties[RemoteEndpointMessage];
                if (remoteEndpoint != null)
                {
                    return remoteEndpoint.Address;
                }
            }
            //Owin-hosting
            if (request.Properties.ContainsKey(OwinContext))
            {
                dynamic ctx = request.Properties[OwinContext];
                if (ctx != null)
                {
                    return ctx.Request.RemoteIpAddress;
                }
            }
            return null;
        }

        public static HttpResponseMessage CreateSelfDestroyingResponse(this HttpRequestMessage request, HttpStatusCode code = HttpStatusCode.OK)
        {
            var response = request.CreateResponse(code);
            request.RegisterForDispose(response);
            return response;
        }

        public static HttpResponseMessage CreateSelfDestroyingResponse<T>(this HttpRequestMessage request, T val, HttpStatusCode code = HttpStatusCode.OK)
        {
            var response = request.CreateResponse<T>(code, val);
            request.RegisterForDispose(response);
            return response;
        }

        public static HttpResponseMessage CreateSelfDestroyingResponse(this HttpRequestMessage request, HttpStatusCode code, string reasonPhrase)
        {
            var response = request.CreateResponse(code);
            response.ReasonPhrase = reasonPhrase;
            request.RegisterForDispose(response);
            return response;
        }

        public static HttpResponseMessage CreateSelfDestroyingResponse(this HttpRequestMessage request, HttpResponseMessage response)
        {
            request.RegisterForDispose(response);
            return response;
        }
    }
}