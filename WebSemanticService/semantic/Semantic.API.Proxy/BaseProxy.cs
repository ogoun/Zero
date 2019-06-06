using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroLevel;

namespace Semantic.API.Proxy
{
    /// <summary>
    /// Base async rest client
    /// </summary>
    public abstract class BaseProxy
    {
        #region Fields
        private readonly string _baseUri;
        #endregion

        #region Ctors
        static BaseProxy()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 8;
        }

        public BaseProxy(string baseUri)
        {
            if (String.IsNullOrWhiteSpace(baseUri)) throw new ArgumentNullException("baseUri");
            _baseUri = baseUri;
        }
        #endregion

        #region Requests
        protected T Get<T>(string resource, NameValueCollection parameters = null)
        {
            return GET<T>(_baseUri, resource, parameters);
        }

        protected T Post<T>(string resource, object body, NameValueCollection parameters = null)
        {
            return POST<T>(_baseUri, resource, body, parameters);
        }

        protected T Put<T>(string resource, object body, NameValueCollection parameters = null)
        {
            return PUT<T>(_baseUri, resource, body, parameters);
        }

        protected T Delete<T>(string resource, NameValueCollection parameters = null)
        {
            return DELETE<T>(_baseUri, resource, parameters);
        }

        protected T Delete<T>(string resource, object body, NameValueCollection parameters = null)
        {
            return DELETE<T>(_baseUri, resource, body, parameters);
        }
        #endregion

        #region Helpers
        private Uri BuildRequestUrl(string baseUri, string resource, NameValueCollection parameters)
        {
            if (null == resource) throw new ArgumentNullException("resource");
            var stringBuilder = new StringBuilder(baseUri);
            if (baseUri[baseUri.Length - 1] != '/')
                stringBuilder.Append('/');
            stringBuilder.Append(resource);
            if (parameters != null && parameters.Count > 0)
            {
                stringBuilder.Append("?");
                foreach (string key in parameters.Keys)
                {
                    var val = parameters[key];
                    if (string.IsNullOrWhiteSpace(val))
                    {
                        stringBuilder.Append(key);
                    }
                    else
                    {
                        stringBuilder.AppendFormat("{0}={1}", key, val);
                    }
                    stringBuilder.Append("&");
                }
            }
            return new Uri(stringBuilder.ToString().TrimEnd('&'));
        }


        #region Requests
        private T SendRequest<T>(string baseUri, string resource, string method, object body, NameValueCollection parameters = null)
        {
            string statusCode = null;
            string reason = null;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(BuildRequestUrl(baseUri, resource, parameters));
                request.ContinueTimeout = 30000;
                request.ReadWriteTimeout = 30000;
                request.Timeout = Timeout.Infinite;
                request.MaximumResponseHeadersLength = int.MaxValue;
                request.Method = method;
                request.Proxy = null;
                request.UserAgent = "DocStream";
                request.AutomaticDecompression = DecompressionMethods.GZip;
                if (body != null)
                {
                    request.Accept = "application/json";
                    request.ContentType = "application/json";
                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        var json = JsonConvert.SerializeObject(body);
                        streamWriter.Write(json);
                        streamWriter.Flush();
                    }
                }
                var response = (HttpWebResponse)(request.GetResponse());
                using (response)
                {
                    statusCode = response.StatusCode.ToString();
                    reason = response.StatusDescription;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (var stream = new StreamReader(response.GetResponseStream()))
                        {
                            var json = stream.ReadToEnd();
                            return JsonConvert.DeserializeObject<T>(json);
                        }
                    }
                    else
                    {
                        Log.Warning($"[BaseAsyncProxy]\t{method}\t'{baseUri}/{resource}'. Status code: {statusCode ?? "Uncknown"}. Reason: {reason ?? string.Empty}");
                    }
                }
            }
            catch (WebException ex)
            {
                try
                {
                    if ((ex.Status == WebExceptionStatus.ProtocolError) && (ex.Response != null))
                    {
                        Log.Warning("[BaseAsyncProxy] WebAPI protocol error, try restore content");
                        HttpWebResponse response = ex.Response as HttpWebResponse;
                        statusCode = response.StatusCode.ToString();
                        {
                            using (var stream = new StreamReader(response.GetResponseStream()))
                            {
                                var json = stream.ReadToEnd();
                                return JsonConvert.DeserializeObject<T>(json);
                            }
                        }
                    }
                    else
                    {
                        Log.Error(ex, $"[BaseAsyncProxy]\t{method}\t'{baseUri}/{resource}'. Status code: {statusCode ?? "Uncknown"}. Reason: {reason ?? ex.Message}");
                    }
                }
                catch (Exception ex1)
                {
                    Log.Error(ex1, $"[BaseAsyncProxy]\t{method}\t'{baseUri}/{resource}'. Status code: {statusCode ?? "Uncknown"}. Reason: {reason ?? ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[BaseAsyncProxy]\t{method}\t'{baseUri}/{resource}'. Status code: {statusCode ?? "Uncknown"}. Reason: {reason ?? ex.Message}");
            }
            return default(T);
        }

        private T GET<T>(string baseUri, string resource, NameValueCollection parameters = null)
        {
            return SendRequest<T>(baseUri, resource, "GET", null, parameters);
        }

        private T POST<T>(string baseUri, string resource, object body, NameValueCollection parameters = null)
        {
            return SendRequest<T>(baseUri, resource, "POST", body, parameters);
        }

        private T PUT<T>(string baseUri, string resource, object body, NameValueCollection parameters = null)
        {
            return SendRequest<T>(baseUri, resource, "PUT", body, parameters);
        }

        private T DELETE<T>(string baseUri, string resource, NameValueCollection parameters = null)
        {
            return SendRequest<T>(baseUri, resource, "DELETE", null, parameters);
        }

        private T DELETE<T>(string baseUri, string resource, object body, NameValueCollection parameters = null)
        {
            return SendRequest<T>(baseUri, resource, "DELETE", body, parameters);
        }
        #endregion
        #endregion
    }
}
