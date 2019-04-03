using Microsoft.Owin.Hosting;
using Owin;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;

namespace ZeroLevel.Discovery
{
    public class LogRequestAndResponseHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // log request body
            string requestBody = await request.Content.ReadAsStringAsync();
            Log.Debug(requestBody);

            // let other handlers process the request
            var result = await base.SendAsync(request, cancellationToken);

            if (result.Content != null)
            {
                //(result.Content as ObjectContent).Formatter.MediaTypeMappings.Clear();
                // once response body is ready, log it
                var responseBody = await result.Content.ReadAsStringAsync();
                Log.Debug(responseBody);
            }
            return result;
        }
    }

    public class EnableInheritRoutingDirectRouteProvider : DefaultDirectRouteProvider
    {
        protected override IReadOnlyList<IDirectRouteFactory> GetActionRouteFactories(HttpActionDescriptor actionDescriptor)
        {
            // inherit route attributes decorated on base class controller's actions
            return actionDescriptor.GetCustomAttributes<IDirectRouteFactory>(inherit: true);
        }
    }

    public class Startup
    {
        // This code configures Web API. The Startup class is specified as a type
        // parameter in the WebApp.Start method.
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host.
            HttpConfiguration config = new HttpConfiguration();
            config.MapHttpAttributeRoutes(new EnableInheritRoutingDirectRouteProvider());
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.EnsureInitialized();
            config.Formatters.Remove(config.Formatters.XmlFormatter);
            config.Formatters.Add(config.Formatters.JsonFormatter);
            //if (_log_request_response)
            {
                config.MessageHandlers.Add(new LogRequestAndResponseHandler());
            }
            appBuilder.UseWebApi(config);
        }

        private static bool _log_request_response;

        public static void StartWebPanel(int port,
            bool log_request_response)
        {
            _log_request_response = log_request_response;
            string baseAddress = string.Format("http://*:{0}/", port);
            WebApp.Start<Startup>(url: baseAddress);
            Log.Info(string.Format("Web panel url: {0}", baseAddress));
        }
    }
}