using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ZeroLevel.Services;
using ZeroLevel.Services.Web;

namespace ZeroLevel.WebAPI
{
    public abstract class BaseController : ApiController
    {
        #region Responce create helpers
        public static HttpResponseMessage BadRequestAnswer(HttpRequestMessage request, string message)
        {
            return request.CreateSelfDestroyingResponse(HttpStatusCode.BadRequest,
                message.Replace("\r", " ").Replace("\n", " "));
        }

        public static HttpResponseMessage BadRequestAnswer(HttpRequestMessage request, Exception ex)
        {
            return request.CreateSelfDestroyingResponse(HttpStatusCode.BadRequest,
                ex.Message.Replace("\r", " ").Replace("\n", " "));
        }

        public static HttpResponseMessage SuccessAnswer(HttpRequestMessage request)
        {
            return request.CreateSelfDestroyingResponse(HttpStatusCode.OK);
        }

        public static HttpResponseMessage NotFoundAnswer(HttpRequestMessage request, string message)
        {
            return request.CreateSelfDestroyingResponse(HttpStatusCode.Conflict, 
                message.Replace("\r", " ").Replace("\n", " "));
        }

        public static HttpResponseMessage HttpActionResult<T>(HttpRequestMessage request, Func<T> responseBuilder)
        {
            try
            {
                var sw = new Stopwatch();
                sw.Start();
                var response = request.CreateSelfDestroyingResponse(responseBuilder(), HttpStatusCode.OK);
                sw.Stop();
                Log.Debug("Request URI: {0}. {1}ms", request.RequestUri.AbsolutePath, sw.ElapsedMilliseconds);
                return response;
            }
            catch (KeyNotFoundException knfEx)
            {
                return NotFoundAnswer(request, knfEx.Message);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Сбой при запросе {0}", request.RequestUri.PathAndQuery);
                return BadRequestAnswer(request, ex);
            }
        }

        protected static DateTime? ParseDateTime(string line)
        {
            var dateParts = line.Split('.', '/', '\\', '-').Select(p => p.Trim()).ToArray();
            if (dateParts.Last().Length == 4)
            {
                dateParts = dateParts.Reverse().ToArray();
            }
            if (dateParts.First().Length != 4) return null;
            int year, month = 1, day = 1;
            if (false == int.TryParse(dateParts.First(), out year))
            {
                return null;
            }
            if (dateParts.Count() > 1)
            {
                if (false == int.TryParse(dateParts[1], out month))
                {
                    return null;
                }
            }
            if (dateParts.Count() > 2)
            {
                if (false == int.TryParse(dateParts[2], out day))
                {
                    return null;
                }
            }
            return new DateTime(year, month, day);
        }

        protected static String GetParameter(HttpRequestMessage request, string name)
        {
            var keys = UrlUtility.ParseQueryString(request.RequestUri.Query);
            if (keys.ContainsKey(name))
            {
                return keys[name];
            }
            return null;
        }
        #endregion
    }
}
