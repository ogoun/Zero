using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using ZeroLevel.Services.Web;

namespace ZeroLevel.WebAPI
{
    public abstract class BaseHtmlController : BaseController
    {
        protected readonly string _baseResourcesFolder;

        public BaseHtmlController(string baseResourcesFolder)
        {
            _baseResourcesFolder = baseResourcesFolder;
        }

        #region Resource responce
        /// <summary>
        /// Создает ответ с JS в качетве содержимого
        /// </summary>
        /// <param name="filename">Имя js файла</param>
        /// <returns>Готовый ответ</returns>
        private HttpResponseMessage CreateJSResponse(HttpRequestMessage request, string filename)
        {
            var absolutePath = Path.Combine(_baseResourcesFolder, "JS", filename);
            return CreateFromFileResponse(request, absolutePath, "application/javascript");
        }
        /// <summary>
        /// Создает ответ с CSS в качетве содержимого
        /// </summary>
        /// <param name="filename">Имя css файла</param>
        /// <returns>Готовый ответ</returns>
        private HttpResponseMessage CreateCSSResponse(HttpRequestMessage request, string filename)
        {
            var absolutePath = Path.Combine(_baseResourcesFolder, "CSS", filename);
            return CreateFromFileResponse(request, absolutePath, "text/css");
        }
        /// <summary>
        /// Создает ответ с фрагментом HTML в качетве содержимого
        /// </summary>
        /// <param name="filename">Имя файла с HTML фрагментом</param>
        /// <returns>Готовый ответ</returns>
        protected HttpResponseMessage CreateHTMLResponse(HttpRequestMessage request, string filename)
        {
            var absolutePath = Path.Combine(_baseResourcesFolder, filename);
            if (false == File.Exists(absolutePath))
            {
                absolutePath = Path.Combine(_baseResourcesFolder, "HTML", filename);
            }
            return CreateFromFileResponse(request, absolutePath, "text/html");
        }
        /// <summary>
        /// Создает ответ с изображением в качестве содержимого
        /// </summary>
        /// <param name="filename">Имя файла с изображением</param>
        /// <returns>Готовый ответ</returns>
        public HttpResponseMessage CreateImageResponse(HttpRequestMessage request, string filename)
        {
            var absolutePath = Path.Combine(_baseResourcesFolder, "images", filename);
            switch (Path.GetExtension(filename).ToLower())
            {
                case ".ico":
                    return CreateFromFileBinaryResponse(request, absolutePath, "image/x-icon");
                case ".png":
                    return CreateFromFileBinaryResponse(request, absolutePath, "image/png");
                case ".jpeg":
                case ".jpg":
                    return CreateFromFileBinaryResponse(request, absolutePath, "image/jpeg");
                case ".gif":
                    return CreateFromFileBinaryResponse(request, absolutePath, "image/gif");
            }
            return request.CreateSelfDestroyingResponse(HttpStatusCode.NotFound);
        }
        #endregion

        #region File system
        /// <summary>
        /// Создает ответ в текстовом представлении, из указанного файла
        /// </summary>
        /// <param name="filename">Путь к файлу</param>
        /// <param name="mediaType">Mime-тип содержимого файла</param>
        /// <returns>Результат</returns>
        private static HttpResponseMessage CreateFromFileResponse(HttpRequestMessage request, string filename, string mediaType)
        {
            try
            {
                var content = File.ReadAllText(
                    Path.Combine(ZeroLevel.Configuration.BaseDirectory, filename), Encoding.UTF8);
                var response = request.CreateSelfDestroyingResponse(HttpStatusCode.OK);
                response.Content = new StringContent(content);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
                return response;
            }
            catch (Exception ex)
            {
                Log.Warning(string.Format("Не удалось загрузить файл {0} в качестве ответа типа {1}", filename, mediaType), ex.ToString());
                return request.CreateSelfDestroyingResponse(HttpStatusCode.NotFound);
            }
        }
        /// <summary>
        /// Создает ответ в виде байт-массива из указанного файла
        /// </summary>
        /// <param name="filename">Путь к файлу</param>
        /// <param name="mediaType">Mime-тип содержимого файла</param>
        /// <returns>Результат</returns>
        private static HttpResponseMessage CreateFromFileBinaryResponse(HttpRequestMessage request, string filename, string mediaType)
        {
            try
            {
                var response = request.CreateSelfDestroyingResponse();
                response.Content = new ByteArrayContent(File.ReadAllBytes(
                    Path.Combine(ZeroLevel.Configuration.BaseDirectory, filename)));
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
                return response;
            }
            catch (Exception ex)
            {
                Log.Warning(string.Format("Не удалось загрузить файл {0} в качестве ответа типа {1}", filename, mediaType), ex.ToString());
                return request.CreateSelfDestroyingResponse(HttpStatusCode.NotFound);
            }
        }
        #endregion

        /// <summary>
        /// Выполняет поиск и возврат в качестве ответа ресурса сайта,
        /// javascript, css файла или файла с изображением
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <returns>Ответ</returns>
        protected HttpResponseMessage _GetResource(HttpRequestMessage request)
        {
            var keys = UrlUtility.ParseQueryString(request.RequestUri.Query);
            if (keys.ContainsKey("js"))
            {
                var filename = WepApiResourceRouter.GetJsFile(keys["js"]);
                return CreateJSResponse(request, filename);
            }
            else if (keys.ContainsKey("css"))
            {
                var filename = WepApiResourceRouter.GetCssFile(keys["css"]);
                return CreateCSSResponse(request, filename);
            }
            else if (keys.ContainsKey("html"))
            {
                var filename = WepApiResourceRouter.GetHtmlFile(keys["html"]);
                return CreateHTMLResponse(request, filename);
            }
            else if (keys.ContainsKey("img"))
            {
                return CreateImageResponse(request, keys["img"]);
            }
            else
            {
                var key = request.RequestUri.LocalPath.Trim('/');
                if (string.IsNullOrWhiteSpace(key)) key = "index";
                var filename = WepApiResourceRouter.GetHtmlFile(key);
                if (false == string.IsNullOrWhiteSpace(filename))
                {
                    return CreateHTMLResponse(request, filename);
                }
            }
            return request.CreateSelfDestroyingResponse(HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Создает ответ в виде фрагмента HTML
        /// </summary>
        /// <param name="text">HTML текст</param>
        /// <returns></returns>
        protected static HttpResponseMessage CreateHTMLFragmentResponse(HttpRequestMessage request, string text)
        {
            var response = request.CreateSelfDestroyingResponse();
            response.Content = new StringContent(text);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            return response;
        }

        #region Helpers
        /// <summary>
        /// Хак для JQuery UI
        /// </summary>
        [HttpGet]
        [Route("images/{id}")]
        [Route("api/web/images/{id}")]
        public HttpResponseMessage Images(HttpRequestMessage request, [FromUri]string id)
        {
            return CreateImageResponse(request, id);
        }
        /// <summary>
        /// Выполняет поиск и возврат в качестве ответа ресурса сайта,
        /// javascript, css файла или файла с изображением
        /// </summary>
        /// <param name="request">Запрос</param>
        /// <returns>Ответ</returns>
        [HttpGet]
        [Route("api/web/resources")]
        public HttpResponseMessage GetResource(HttpRequestMessage request)
        {
            return _GetResource(request);
        }
        #endregion
    }
}
