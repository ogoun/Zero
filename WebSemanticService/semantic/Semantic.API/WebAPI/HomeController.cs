using System.Net.Http;
using System.Web.Http;
using ZeroLevel.WebAPI;

namespace Semantic.API.WebAPI
{
    [RequestFirewall]
    public sealed class HomeController : BaseHtmlController
    {
        static HomeController()
        {
            WepApiResourceRouter.RegisterHTMLFile("index", "index.html");
            WepApiResourceRouter.RegisterCSSFile("jqueryui", "jquery-ui.min.css");
            WepApiResourceRouter.RegisterCSSFile("local", "local.css");
            WepApiResourceRouter.RegisterCSSFile("reset", "reset-min.css");
            WepApiResourceRouter.RegisterCSSFile("glitch", "glitch.css");
            WepApiResourceRouter.RegisterJavaScriptFile("api", "api.js");
            WepApiResourceRouter.RegisterJavaScriptFile("jquery", "jquery-3.1.1.min.js");
            WepApiResourceRouter.RegisterJavaScriptFile("jqueryui", "jquery-ui.min.js");
        }

        public HomeController() : base("Web") { }

        /// <summary>
        /// Основной лэндинг
        /// </summary>
        [HttpGet]
        [Route("")]
        public HttpResponseMessage GetHTML(HttpRequestMessage request)
        {
            return _GetResource(request);
        }
    }
}
