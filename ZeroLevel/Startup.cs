using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;
using OwinTest.Middleware;
using System;
using System.Web.Http;
using ZeroLevel;

namespace OwinTest
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();
            config.MapHttpAttributeRoutes(new EnableInheritRoutingDirectRouteProvider());

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.MessageHandlers.Add(new LogRequestAndResponseHandler());
            // Require HTTPS
            // config.MessageHandlers.Add(new RequireHttpsMessageHandler());
            config.EnsureInitialized();

            app.UseWebApi(config);

            var contentFileServer = new FileServerOptions()
            {
                EnableDirectoryBrowsing = false,
                EnableDefaultFiles = true,
                DefaultFilesOptions = { DefaultFileNames = { "index.html" } },
                RequestPath = new PathString("/Content"),
                FileSystem = new PhysicalFileSystem(@"./Content"),
                StaticFileOptions = { ContentTypeProvider = new CustomTypeProvider() }
            };
            contentFileServer.StaticFileOptions.OnPrepareResponse = (context) =>
            {
                if (context.OwinContext.Authentication.User == null)
                {
                    context.OwinContext.Response.Redirect("/login");
                }
            };
            app.UseFileServer(contentFileServer);

            app.UseErrorPage();


            Log.Info("Server started");
        }
    }
}
