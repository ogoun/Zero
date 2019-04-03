using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ZeroLevel.ProxyREST
{
    public abstract class BaseProxy
    {
        private readonly string _baseUrl;

        private Uri BuildRequestUrl(string baseUri, string resource, IDictionary<string, object> parameters)
        {
            if (null == resource) throw new ArgumentNullException("resource");
            var stringBuilder = new StringBuilder(baseUri);
            if (baseUri[baseUri.Length - 1] != '/')
                stringBuilder.Append('/');
            if (resource[0] != '/')
            {
                stringBuilder.Append(resource);
            }
            else
            {
                stringBuilder.Append(resource.Substring(1));
            }
            parameters.
                Do(list =>
                {
                    if (list.Count > 0)
                    {
                        stringBuilder.Append("?");
                        foreach (string key in list.Keys)
                        {
                            var val = list[key];
                            if (val == null)
                            {
                                stringBuilder.Append(key);
                            }
                            else
                            {
                                var vtype = val.GetType();
                                if (vtype.IsArray)
                                {
                                    if (vtype.GetElementType() == typeof(string))
                                    {
                                        var arr = (string[])val;
                                        stringBuilder.Append(string.Join("&", arr.Select(i => string.Format("{0}[]={1}", key, i))));
                                    }
                                    else
                                    {
                                        var arr = (object[])val;
                                        stringBuilder.Append(string.Join("&", arr.Select(i => string.Format("{0}[]={1}", key, JsonConvert.SerializeObject(i)))));
                                    }
                                }
                                else
                                {
                                    if (vtype == typeof(string))
                                    {
                                        stringBuilder.AppendFormat("{0}={1}", key, val);
                                    }
                                    else
                                    {
                                        stringBuilder.AppendFormat("{0}={1}", key, JsonConvert.SerializeObject(val));
                                    }
                                }
                            }
                            stringBuilder.Append("&");
                        }
                    }
                });
            return new Uri(stringBuilder.ToString().TrimEnd('&'));
        }

        protected T SendRequest<T>(string resource, string method, object body, IDictionary<string, object> parameters = null)
        {
            string statusCode = null;
            string reason = null;
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(BuildRequestUrl(_baseUrl, resource, parameters));
                request.UseDefaultCredentials = true;
                request.Method = method;
                request.Proxy = null;
                request.AutomaticDecompression = DecompressionMethods.GZip;
                if (body != null)
                {
                    request.Accept = "application/json";
                    request.ContentType = "application/json";
                    using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                    {
                        streamWriter.Write(JsonConvert.SerializeObject(body));
                        streamWriter.Flush();
                    }
                }
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    statusCode = response.StatusCode.ToString();
                    reason = response.StatusDescription;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (var stream = new StreamReader(response.GetResponseStream()))
                        {
                            string json = stream.ReadToEnd();
                            return JsonConvert.DeserializeObject<T>(json);
                        }
                    }
                    else
                    {
                        throw new Exception("Bad status code");
                    }
                }
            }
            catch (Exception ex)
            {
                var line = $"Resource request failed. [{method}] {resource}. Error code: {(statusCode ?? "Uncknown")}. Comment: {(reason ?? ex.Message)}";
                Log.Error(ex, line);
                throw new InvalidOperationException(line, ex);
            }
        }

        protected T GET<T>(string resource, IDictionary<string, object> parameters = null)
        {
            return SendRequest<T>(resource, "GET", null, parameters);
        }

        protected T POST<T>(string resource, object body, IDictionary<string, object> parameters = null)
        {
            return SendRequest<T>(resource, "POST", body, parameters);
        }

        protected T DELETE<T>(string resource, object body, IDictionary<string, object> parameters = null)
        {
            return SendRequest<T>(resource, "DELETE", body, parameters);
        }

        static BaseProxy()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 8;
        }

        public BaseProxy(string baseUri)
        {
            if (false == baseUri.EndsWith("/"))
                _baseUrl = baseUri + "/";
            else
                _baseUrl = baseUri;
        }
    }
}