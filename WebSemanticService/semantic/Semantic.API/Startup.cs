using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Owin;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Routing;
using ZeroLevel;

namespace Semantic.API
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
            if (_log_request_response)
            {
                config.MessageHandlers.Add(new LogRequestAndResponseHandler());
            }
            if (ZeroLevel.Configuration.Default.FirstOrDefault<bool>("ntlmEnabled", false))
            {
                // Enable NTLM authentication
                ((HttpListener)appBuilder.Properties["System.Net.HttpListener"]).AuthenticationSchemes =
                   AuthenticationSchemes.IntegratedWindowsAuthentication;
            }
            appBuilder.UseWebApi(config);
            if (_enable_static_files)
            {
                var webdir = Path.Combine(ZeroLevel.Configuration.BaseDirectory, "web");
                if (false == Directory.Exists(webdir))
                {
                    Directory.CreateDirectory(webdir);
                }
                PhysicalFileSystem fileSystem = new PhysicalFileSystem(webdir);
                FileServerOptions options = new FileServerOptions
                {
                    EnableDefaultFiles = true,
                    FileSystem = fileSystem
                };
                options.StaticFileOptions.ServeUnknownFileTypes = true;
                appBuilder.UseFileServer(options);
            }
        }

        private static bool _log_request_response;
        private static bool _enable_static_files;
        public static void Run(
            bool log_request_response,
            bool enable_static_files = false)
        {
            _log_request_response = log_request_response;
            _enable_static_files = enable_static_files;
            string baseAddress = string.Format("http://*:{0}/",
                ZeroLevel.Configuration.Default.First<int>("webApiPort"));
            WebApp.Start<Startup>(url: baseAddress);
            Log.Info(string.Format("Web service url: {0}", baseAddress));
        }
    }
}
