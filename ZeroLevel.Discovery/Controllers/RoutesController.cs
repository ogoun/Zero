using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using ZeroLevel.Models;
using ZeroLevel.Network;

namespace ZeroLevel.Discovery
{
    public class RoutesController :
        BaseController
    {
        [HttpGet]
        [Route("favicon.ico")]
        public HttpResponseMessage favicon(HttpRequestMessage request)
        {
            return null;
        }

        [HttpGet]
        [Route("api/v0/routes")]
        [ResponseType(typeof(IEnumerable<ServiceEndpointsInfo>))]
        public HttpResponseMessage GetRoutes(HttpRequestMessage request)
        {
            try
            {
                return request.CreateSelfDestroyingResponse(Injector.Default.Resolve<RouteTable>().Get());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error with read records");
                return BadRequestAnswer(request, ex);
            }
        }
    }
}